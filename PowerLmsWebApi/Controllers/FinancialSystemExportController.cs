using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLms.Data.Finance;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using OW.Data;
using OwDbBase.Tasks;
using System.Text.Json;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务系统接口导出功能控制器。
    /// 提供发票数据导出为金蝶DBF格式文件的功能。
    /// 严格按照SubjectConfiguration表中的科目配置进行凭证生成。
    /// </summary>
    public class FinancialSystemExportController : PlControllerBase
    {
        /// <summary>
        /// 构造函数，初始化财务系统导出控制器。
        /// </summary>
        /// <param name="accountManager">账户管理器，提供用户身份验证和令牌管理功能</param>
        /// <param name="serviceProvider">服务提供者，用于当前请求作用域内的服务解析</param>
        /// <param name="serviceScopeFactory">服务作用域工厂，用于创建独立的服务作用域</param>
        /// <param name="dbContext">PowerLMS用户数据库上下文，提供对数据库的直接访问能力</param>
        /// <param name="logger">日志记录器，用于记录控制器运行过程中的各种信息</param>
        /// <param name="fileManager">文件管理器，提供文件系统操作的统一接口</param>
        /// <param name="merchantManager">商户管理器，用于商户相关操作</param>
        /// <param name="organizationManager">机构管理器，用于机构相关操作</param>
        public FinancialSystemExportController(AccountManager accountManager, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory,
            PowerLmsUserDbContext dbContext, ILogger<FinancialSystemExportController> logger, OwFileManager fileManager,
            MerchantManager merchantManager, OrganizationManager organizationManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _ServiceScopeFactory = serviceScopeFactory;
            _DbContext = dbContext;
            _Logger = logger;
            _FileManager = fileManager;
            _MerchantManager = merchantManager;
            _OrganizationManager = organizationManager;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider; // 保留，用于当前请求作用域的服务解析
        private readonly IServiceScopeFactory _ServiceScopeFactory; // 用于后台任务的独立作用域创建
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly ILogger<FinancialSystemExportController> _Logger;
        private readonly OwFileManager _FileManager;
        private readonly MerchantManager _MerchantManager;
        private readonly OrganizationManager _OrganizationManager;

        /// <summary>
        /// 导出发票数据为金蝶DBF格式文件。
        /// 根据指定的查询条件，查询符合条件的发票集合，并转换为金蝶凭证格式导出为DBF文件。
        /// 使用通用的OwTaskStore任务管理机制，可通过系统的任务状态查询接口跟踪进度。
        /// </summary>
        /// <param name="model">导出参数，包含查询条件和用户令牌</param>
        /// <returns>导出任务信息，包含任务ID用于跟踪进度</returns>
        /// <response code="200">任务创建成功，返回任务ID</response>
        /// <response code="401">无效令牌，用户未认证</response>
        /// <response code="404">没有找到符合条件的发票数据</response>
        /// <response code="500">科目配置不完整，无法生成凭证</response>
        [HttpPost]
        public ActionResult<ExportInvoiceToDbfReturnDto> ExportInvoiceToDbf(ExportInvoiceToDbfParamsDto model)
        {
            // 使用当前请求作用域的服务提供者进行令牌验证
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ExportInvoiceToDbfReturnDto();
            try
            {
                // 预检查科目配置是否完整
                var missingConfigs = ValidateSubjectConfiguration(context.User.OrgId);
                if (missingConfigs.Any())
                {
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = $"科目配置不完整，缺少以下配置：{string.Join(", ", missingConfigs)}";
                    return result;
                }

                // 在方法内直接生成发票集合进行预检查
                var invoicesQuery = _DbContext.TaxInvoiceInfos.Where(c => c.ExportedDateTime == null) //需求强制导出未导出过的发票
                    .AsQueryable();

                // 应用查询条件
                if (model.ExportConditions != null && model.ExportConditions.Any())
                {
                    invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, model.ExportConditions);
                }

                // 添加组织权限过滤 - 根据用户权限限制导出范围
                invoicesQuery = ApplyOrganizationFilter(invoicesQuery, context.User);

                // 预检查：统计符合条件的发票数量
                var invoiceCount = invoicesQuery.Count();
                if (invoiceCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "没有找到符合条件的发票数据，请调整查询条件";
                    return result;
                }

                // 创建后台任务，使用通用的OwTaskStore机制
                var taskId = Guid.NewGuid();
                var exportDateTime = DateTime.UtcNow;
                var task = new OwTaskStore
                {
                    Id = taskId,
                    ServiceTypeName = nameof(FinancialSystemExportController),
                    MethodName = nameof(ProcessInvoiceDbfExportAsync),
                    Parameters = new Dictionary<string, string>
                    {
                        ["ExportConditions"] = JsonSerializer.Serialize(model.ExportConditions),
                        ["UserId"] = context.User.Id.ToString(),
                        ["OrgId"] = context.User.OrgId?.ToString() ?? "",
                        ["ExpectedCount"] = invoiceCount.ToString(),
                        ["ExportDateTime"] = exportDateTime.ToString("O") // ISO 8601格式
                    },
                    Status = OwTaskStatus.Pending,
                    CreatedUtc = exportDateTime,
                    CreatorId = context.User.Id,
                    TenantId = context.User.OrgId
                };

                _DbContext.OwTaskStores.Add(task);
                _DbContext.SaveChanges();

                // 启动后台处理 - 使用独立的服务作用域工厂以避免Controller生命周期依赖
                var serviceScopeFactory = _ServiceScopeFactory; // 捕获服务作用域工厂引用（单例）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessInvoiceDbfExportAsync(taskId, serviceScopeFactory);
                    }
                    catch (Exception ex)
                    {
                        // 确保异常不会导致应用程序崩溃
                        using var scope = serviceScopeFactory.CreateScope();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FinancialSystemExportController>>();
                        logger.LogError(ex, "后台任务 {TaskId} 执行时发生未处理异常", taskId);
                    }
                });

                result.TaskId = taskId;
                result.DebugMessage = $"导出任务已创建，预计处理 {invoiceCount} 张发票，将使用DotNetDbfUtil.WriteLargeFile处理大文件。可通过系统任务状态查询接口跟踪进度。";
                result.ExpectedInvoiceCount = invoiceCount;

                _Logger.LogInformation("用户 {UserId} 创建了发票DBF导出任务 {TaskId}，预计处理 {Count} 张发票",
                    context.User.Id, taskId, invoiceCount);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建发票DBF导出任务时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 验证科目配置是否完整。
        /// 检查发票挂账(PBI)流程所需的所有科目是否已配置。
        /// </summary>
        /// <param name="orgId">组织ID，用于查询对应组织的科目配置</param>
        /// <returns>缺失的科目配置代码列表</returns>
        private List<string> ValidateSubjectConfiguration(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "PBI_ACC_RECEIVABLE",   // 应收账款
                "PBI_SALES_REVENUE",    // 主营业务收入
                "PBI_TAX_PAYABLE",      // 应交税金
                //"GEN_PREPARER",         // 制单人，暂无
                //"GEN_VOUCHER_GROUP"     // 凭证类别字，暂无
            };

            var existingCodes = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();

            return requiredCodes.Except(existingCodes).ToList();
        }

        /// <summary>
        /// 异步处理发票DBF导出任务。
        /// 完整实现：查询发票集合→查询明细→从科目配置表获取配置→转换为金蝶格式→生成DBF文件→更新导出时间→绑定任务。
        /// 确保正确更新任务状态，记录导出时间到发票记录中。
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="serviceScopeFactory">服务作用域工厂，用于创建独立的服务作用域</param>
        private static async Task ProcessInvoiceDbfExportAsync(Guid taskId, IServiceScopeFactory serviceScopeFactory)
        {
            ILogger logger = null;
            PowerLmsUserDbContext dbContext = null;
            IServiceScope scope = null;
            
            try
            {
                if (serviceScopeFactory == null)
                {
                    throw new ArgumentNullException(nameof(serviceScopeFactory), "必须提供服务作用域工厂以确保后台任务的独立性");
                }

                scope = serviceScopeFactory.CreateScope();
                dbContext = scope.ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
                var fileManager = scope.ServiceProvider.GetRequiredService<OwFileManager>();
                logger = scope.ServiceProvider.GetRequiredService<ILogger<FinancialSystemExportController>>();

                logger.LogInformation("开始处理发票DBF导出任务 {TaskId}", taskId);

                var task = await dbContext.OwTaskStores.FindAsync(taskId);
                if (task == null)
                {
                    logger.LogWarning("任务 {TaskId} 不存在", taskId);
                    return;
                }

                // 更新任务状态为执行中
                task.Status = OwTaskStatus.Running;
                await dbContext.SaveChangesAsync();
                logger.LogInformation("任务 {TaskId} 开始执行", taskId);

                // 解析任务参数 - 添加空值检查
                logger.LogDebug("解析任务参数，任务ID: {TaskId}", taskId);
                
                if (task.Parameters == null)
                {
                    throw new InvalidOperationException($"任务 {taskId} 的参数为空");
                }

                if (!task.Parameters.TryGetValue("ExportConditions", out var exportConditionsJson))
                {
                    throw new InvalidOperationException($"任务 {taskId} 缺少ExportConditions参数");
                }

                if (!task.Parameters.TryGetValue("UserId", out var userIdStr))
                {
                    throw new InvalidOperationException($"任务 {taskId} 缺少UserId参数");
                }

                if (!task.Parameters.TryGetValue("ExportDateTime", out var exportDateTimeStr))
                {
                    throw new InvalidOperationException($"任务 {taskId} 缺少ExportDateTime参数");
                }

                var conditions = string.IsNullOrEmpty(exportConditionsJson) ? 
                    new Dictionary<string, string>() : 
                    JsonSerializer.Deserialize<Dictionary<string, string>>(exportConditionsJson);
                
                var userId = Guid.Parse(userIdStr);
                var orgId = task.Parameters.TryGetValue("OrgId", out var orgIdStr) && !string.IsNullOrEmpty(orgIdStr) ? 
                    Guid.Parse(orgIdStr) : (Guid?)null;
                var expectedCount = int.Parse(task.Parameters.GetValueOrDefault("ExpectedCount", "0"));
                var exportDateTime = DateTime.Parse(exportDateTimeStr);

                logger.LogDebug("任务参数解析完成：UserId={UserId}, OrgId={OrgId}, ExpectedCount={ExpectedCount}", 
                    userId, orgId, expectedCount);

                // 加载科目配置
                logger.LogDebug("开始加载科目配置，OrgId: {OrgId}", orgId);
                var subjectConfigs = await LoadSubjectConfigurations(dbContext, orgId);
                if (!subjectConfigs.Any())
                {
                    var errorMessage = $"科目配置未找到，无法生成凭证。组织ID: {orgId}";
                    logger.LogError(errorMessage);
                    
                    task.Status = OwTaskStatus.Failed;
                    task.CompletedUtc = DateTime.UtcNow;
                    task.ErrorMessage = errorMessage;
                    await dbContext.SaveChangesAsync();
                    return;
                }
                logger.LogDebug("科目配置加载完成，共 {Count} 个配置", subjectConfigs.Count);

                // 在后台任务中重新生成发票集合
                logger.LogDebug("开始构建发票查询");
                var invoicesQuery = dbContext.TaxInvoiceInfos.Where(i => i.ExportedDateTime == null).AsQueryable();
                
                if (conditions != null && conditions.Any())
                {
                    logger.LogDebug("应用查询条件，条件数量: {Count}", conditions.Count);
                    try
                    {
                        invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, conditions);
                        logger.LogDebug("查询条件应用成功");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "应用查询条件时发生错误，条件: {@Conditions}", conditions);
                        throw;
                    }
                }

                // 重新应用组织权限过滤（后台任务中也需要权限过滤）
                logger.LogDebug("开始应用组织权限过滤");
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    var taskUserId = Guid.Parse(userIdStr);
                    var taskUser = await dbContext.Accounts.FindAsync(taskUserId);
                    if (taskUser != null)
                    {
                        logger.LogDebug("找到任务用户，应用权限过滤。UserId: {UserId}, UserName: {UserName}", 
                            taskUser.Id, taskUser.LoginName);
                        
                        try
                        {
                            // 在静态方法中调用权限过滤
                            invoicesQuery = ApplyOrganizationFilterStatic(invoicesQuery, taskUser, dbContext, logger);
                            logger.LogDebug("权限过滤应用成功");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "应用组织权限过滤时发生错误");
                            throw;
                        }
                    }
                    else
                    {
                        logger.LogWarning("未找到任务用户，UserId: {UserId}", taskUserId);
                    }
                }

                logger.LogDebug("开始执行发票查询");
                var invoices = await invoicesQuery.ToListAsync();
                logger.LogInformation("任务 {TaskId} 实际查询到 {ActualCount} 张发票，预期 {ExpectedCount} 张",
                    taskId, invoices.Count, expectedCount);

                if (!invoices.Any())
                {
                    var message = "没有找到符合条件的发票数据";
                    logger.LogWarning("任务 {TaskId}: {Message}", taskId, message);
                    
                    task.Status = OwTaskStatus.Completed;
                    task.CompletedUtc = DateTime.UtcNow;
                    task.ErrorMessage = message;
                    await dbContext.SaveChangesAsync();
                    return;
                }

                // 查询发票明细数据
                logger.LogDebug("开始查询发票明细数据");
                var invoiceIds = invoices.Select(i => i.Id).ToList();
                var invoiceItems = await dbContext.TaxInvoiceInfoItems
                    .Where(item => invoiceIds.Contains(item.ParentId.Value))
                    .ToListAsync();

                var invoiceItemsDict = invoiceItems.GroupBy(item => item.ParentId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                logger.LogDebug("发票明细查询完成，共 {ItemCount} 条明细，涉及 {InvoiceCount} 张发票", 
                    invoiceItems.Count, invoiceItemsDict.Count);

                // 转换为金蝶凭证格式（使用科目配置）
                logger.LogDebug("开始转换为金蝶凭证格式");
                try
                {
                    var kingdeeVouchers = ConvertInvoicesToKingdeeVouchersWithConfig(invoices, invoiceItemsDict, subjectConfigs);
                    logger.LogInformation("任务 {TaskId} 生成了 {VoucherCount} 条金蝶凭证记录", taskId, kingdeeVouchers.Count);

                    if (!kingdeeVouchers.Any())
                    {
                        throw new InvalidOperationException("生成的金蝶凭证记录为空");
                    }

                    // 生成文件路径
                    logger.LogDebug("开始生成DBF文件");
                    var fileName = $"Invoice_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
                    var relativePath = Path.Combine("FinancialExports", fileName);
                    
                    if (fileManager == null)
                    {
                        throw new InvalidOperationException("文件管理器为空");
                    }
                    
                    var fileManagerDirectory = fileManager.GetDirectory();
                    if (string.IsNullOrEmpty(fileManagerDirectory))
                    {
                        throw new InvalidOperationException("文件管理器目录为空");
                    }
                    
                    var fullPath = Path.Combine(fileManagerDirectory, relativePath);
                    var directoryPath = Path.GetDirectoryName(fullPath);
                    
                    logger.LogDebug("文件路径: {FullPath}, 目录: {Directory}", fullPath, directoryPath);

                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                        logger.LogDebug("目录创建成功: {Directory}", directoryPath);
                    }

                    // 使用专门的大文件写入函数生成DBF文件
                    try
                    {
                        logger.LogDebug("开始调用DotNetDbfUtil.WriteLargeFile，凭证数量: {Count}", kingdeeVouchers.Count);
                        DotNetDbfUtil.WriteLargeFile(kingdeeVouchers, fullPath);
                        logger.LogInformation("任务 {TaskId} 成功生成DBF文件 {FilePath}", taskId, fullPath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "生成DBF文件时发生错误，文件路径: {FullPath}", fullPath);
                        throw;
                    }

                    // 重要：更新导出的发票记录的导出时间
                    logger.LogDebug("开始更新发票导出时间");
                    foreach (var invoice in invoices)
                    {
                        invoice.ExportedDateTime = exportDateTime;
                    }
                    logger.LogDebug("任务 {TaskId} 已更新 {Count} 张发票的导出时间", taskId, invoices.Count);

                    // 创建文件信息记录并绑定到任务
                    logger.LogDebug("开始创建文件信息记录");
                    var fileInfo = new PlFileInfo
                    {
                        Id = Guid.NewGuid(),
                        ParentId = taskId, // 关键：绑定到任务ID
                        DisplayName = $"发票导出-{DateTime.Now:yyyy年MM月dd日}",
                        FileName = fileName,
                        FilePath = relativePath,
                        CreateBy = userId,
                        CreateDateTime = DateTime.Now,
                        Remark = $"发票DBF导出文件，共{invoices.Count}张发票，{kingdeeVouchers.Count}条会计分录，导出时间：{exportDateTime:yyyy-MM-dd HH:mm:ss}"
                    };

                    dbContext.PlFileInfos.Add(fileInfo);

                    // 更新任务状态为完成
                    task.Status = OwTaskStatus.Completed;
                    task.CompletedUtc = DateTime.UtcNow;
                    task.ErrorMessage = null; // 清除错误信息
                    task.Result = new Dictionary<string, string>
                    {
                        ["FileId"] = fileInfo.Id.ToString(),
                        ["FileName"] = fileName,
                        ["InvoiceCount"] = invoices.Count.ToString(),
                        ["VoucherCount"] = kingdeeVouchers.Count.ToString(),
                        ["FilePath"] = relativePath,
                        ["ExportDateTime"] = exportDateTime.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    await dbContext.SaveChangesAsync();

                    logger.LogInformation("任务 {TaskId} 成功完成，文件：{FileName}，发票数：{InvoiceCount}，凭证数：{VoucherCount}，导出时间：{ExportDateTime}",
                        taskId, fileName, invoices.Count, kingdeeVouchers.Count, exportDateTime);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "转换为金蝶凭证格式时发生错误");
                    throw;
                }
            }
            catch (Exception ex)
            {
                // 异常处理逻辑
                try
                {
                    // 确保我们有有效的logger和dbContext来记录错误
                    if (logger == null || dbContext == null)
                    {
                        // 如果当前scope有问题，创建新的scope
                        scope?.Dispose();
                        scope = serviceScopeFactory.CreateScope();
                        logger ??= scope.ServiceProvider.GetRequiredService<ILogger<FinancialSystemExportController>>();
                        dbContext ??= scope.ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
                    }

                    logger.LogError(ex, "处理发票DBF导出任务 {TaskId} 时发生错误", taskId);

                    if (dbContext != null)
                    {
                        // 使用新的上下文查找任务，避免disposed context错误
                        OwTaskStore task = null;
                        try
                        {
                            task = await dbContext.OwTaskStores.FindAsync(taskId);
                        }
                        catch (ObjectDisposedException)
                        {
                            // 如果上下文被释放，创建新的scope
                            scope?.Dispose();
                            scope = serviceScopeFactory.CreateScope();
                            dbContext = scope.ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
                            logger = scope.ServiceProvider.GetRequiredService<ILogger<FinancialSystemExportController>>();
                            task = await dbContext.OwTaskStores.FindAsync(taskId);
                        }
                        
                        if (task != null)
                        {
                            task.Status = OwTaskStatus.Failed;
                            task.CompletedUtc = DateTime.UtcNow;
                            task.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                            
                            // 如果有内部异常，也记录下来
                            if (ex.InnerException != null)
                            {
                                task.ErrorMessage += $" | InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                            }
                            
                            await dbContext.SaveChangesAsync();
                            logger.LogInformation("任务 {TaskId} 状态已更新为失败，错误信息: {ErrorMessage}", taskId, task.ErrorMessage);
                        }
                        else
                        {
                            logger.LogError("无法找到任务 {TaskId} 来更新失败状态", taskId);
                        }
                    }
                    else
                    {
                        logger?.LogError("数据库上下文为空，无法更新任务 {TaskId} 状态", taskId);
                    }
                }
                catch (Exception updateEx)
                {
                    logger?.LogError(updateEx, "更新任务 {TaskId} 失败状态时发生错误", taskId);
                }
            }
            finally
            {
                // 确保scope被正确释放
                scope?.Dispose();
            }
        }

        /// <summary>
        /// 加载科目配置。
        /// 从SubjectConfiguration表中加载发票挂账流程所需的科目配置。
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>科目配置字典，键为科目代码，值为科目配置对象</returns>
        private static async Task<Dictionary<string, SubjectConfiguration>> LoadSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "PBI_ACC_RECEIVABLE",   // 应收账款
                "PBI_SALES_REVENUE",    // 主营业务收入
                "PBI_TAX_PAYABLE",      // 应交税金
                "GEN_PREPARER",         // 制单人
                "GEN_VOUCHER_GROUP"     // 凭证类别字
            };

            var configs = await dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToListAsync();

            return configs.ToDictionary(c => c.Code, c => c);
        }

        /// <summary>
        /// 将发票数据转换为金蝶凭证格式（使用科目配置）。
        /// 根据发票信息和SubjectConfiguration表中的配置生成借贷平衡的会计分录。
        /// 按照PBI（发票挂账）流程：借方-应收账款，贷方-主营业务收入和应交税金。
        /// </summary>
        /// <param name="invoices">发票数据列表</param>
        /// <param name="invoiceItemsDict">发票明细数据字典，按发票ID分组</param>
        /// <param name="subjectConfigs">科目配置字典</param>
        /// <returns>金蝶凭证记录列表</returns>
        private static List<KingdeeVoucher> ConvertInvoicesToKingdeeVouchersWithConfig(
            List<TaxInvoiceInfo> invoices,
            Dictionary<Guid, List<TaxInvoiceInfoItem>> invoiceItemsDict,
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            if (invoices == null)
                throw new ArgumentNullException(nameof(invoices));
            if (subjectConfigs == null)
                throw new ArgumentNullException(nameof(subjectConfigs));
            
            invoiceItemsDict ??= new Dictionary<Guid, List<TaxInvoiceInfoItem>>();

            var vouchers = new List<KingdeeVoucher>();
            var voucherNumber = 1;

            // 获取通用配置信息
            var preparerName = subjectConfigs.ContainsKey("GEN_PREPARER") ?
                (subjectConfigs["GEN_PREPARER"]?.DisplayName ?? "系统导出") : "系统导出";

            var voucherGroup = subjectConfigs.ContainsKey("GEN_VOUCHER_GROUP") ?
                (subjectConfigs["GEN_VOUCHER_GROUP"]?.VoucherGroup ?? "转") : "转"; // 默认为转账凭证

            foreach (var invoice in invoices)
            {
                if (invoice == null) continue; // 跳过空发票
                
                try
                {
                    var items = invoiceItemsDict.ContainsKey(invoice.Id) ?
                        invoiceItemsDict[invoice.Id] : new List<TaxInvoiceInfoItem>();

                    var taxRate = items.FirstOrDefault()?.TaxRate ?? 0.06m;
                    var netAmount = invoice.TaxInclusiveAmount / (1 + taxRate);
                    var taxAmount = invoice.TaxInclusiveAmount - netAmount;
                    var invoiceDate = invoice.InvoiceDate ?? DateTime.Now;
                    var description = $"{invoice.BuyerTitle ?? "未知客户"}*{invoice.InvoiceItemName ?? "未知项目"}*{invoice.BuyerTaxNum ?? ""}";
                    var customerCode = string.IsNullOrEmpty(invoice.BuyerTaxNum) ? 
                        "CUSTOMER" : 
                        invoice.BuyerTaxNum.Substring(0, Math.Min(10, invoice.BuyerTaxNum.Length));

                    // 借方：应收账款（PBI_ACC_RECEIVABLE）
                    if (subjectConfigs.TryGetValue("PBI_ACC_RECEIVABLE", out var accReceivableConfig) && accReceivableConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup, // 从配置表获取凭证类别字
                            FNUM = voucherNumber,
                            FENTRYID = 0,
                            FEXP = description,
                            FACCTID = accReceivableConfig.SubjectNumber ?? "122101", // 从配置表获取科目号，如果为空则使用默认值
                            FCLSNAME1 = accReceivableConfig.AccountingCategory ?? "客户", // 从配置表获取核算类别
                            FOBJID1 = customerCode,
                            FOBJNAME1 = invoice.BuyerTitle ?? "未知客户",
                            FTRANSID = invoice.BuyerTaxNum ?? "",
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 0, // 借方
                            FFCYAMT = invoice.TaxInclusiveAmount,
                            FDEBIT = invoice.TaxInclusiveAmount,
                            FCREDIT = 0,
                            FPREPARE = preparerName // 从配置表获取制单人
                        });
                    }

                    // 贷方：主营业务收入（PBI_SALES_REVENUE）
                    if (subjectConfigs.TryGetValue("PBI_SALES_REVENUE", out var salesRevenueConfig) && salesRevenueConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup, // 从配置表获取凭证类别字
                            FNUM = voucherNumber,
                            FENTRYID = 1,
                            FEXP = description,
                            FACCTID = salesRevenueConfig.SubjectNumber ?? "601001", // 从配置表获取科目号，如果为空则使用默认值
                            FCLSNAME1 = salesRevenueConfig.AccountingCategory ?? "客户", // 从配置表获取核算类别
                            FOBJID1 = customerCode,
                            FOBJNAME1 = invoice.BuyerTitle ?? "未知客户",
                            FTRANSID = invoice.BuyerTaxNum ?? "",
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 1, // 贷方
                            FFCYAMT = netAmount,
                            FDEBIT = 0,
                            FCREDIT = netAmount,
                            FPREPARE = preparerName
                        });
                    }

                    // 贷方：应交税金（PBI_TAX_PAYABLE）（如果有税额）
                    if (taxAmount > 0 && subjectConfigs.TryGetValue("PBI_TAX_PAYABLE", out var taxPayableConfig) && taxPayableConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup, // 从配置表获取凭证类别字
                            FNUM = voucherNumber,
                            FENTRYID = 2,
                            FEXP = description,
                            FACCTID = taxPayableConfig.SubjectNumber ?? "221001", // 从配置表获取科目号，如果为空则使用默认值
                            FCLSNAME1 = taxPayableConfig.AccountingCategory ?? "客户", // 从配置表获取核算类别
                            FOBJID1 = customerCode,
                            FOBJNAME1 = invoice.BuyerTitle ?? "未知客户",
                            FTRANSID = invoice.BuyerTaxNum ?? "",
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 1, // 贷方
                            FFCYAMT = taxAmount,
                            FDEBIT = 0,
                            FCREDIT = taxAmount,
                            FPREPARE = preparerName
                        });
                    }
                    voucherNumber++;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"处理发票 {invoice.Id} 时发生错误: {ex.Message}", ex);
                }
            }
            
            // 添加验证，确保生成的凭证不为空
            if (vouchers.Count == 0)
            {
                throw new InvalidOperationException("未能生成任何金蝶凭证记录，请检查科目配置或发票数据");
            }
            
            return vouchers;
        }

        /// <summary>
        /// 根据用户权限过滤发票查询，确保用户只能导出有权限访问的发票。
        /// 超级管理员可以导出所有发票，商户管理员可以导出本商户的所有发票，
        /// 普通用户只能导出与其登录机构相关的发票。
        /// </summary>
        /// <param name="invoicesQuery">发票查询对象</param>
        /// <param name="user">当前用户</param>
        /// <param name="dbContext">数据库上下文（可选，用于后台任务）</param>
        /// <returns>过滤后的发票查询对象</returns>
        private IQueryable<TaxInvoiceInfo> ApplyOrganizationFilter(IQueryable<TaxInvoiceInfo> invoicesQuery, Account user, PowerLmsUserDbContext dbContext = null)
        {
            // 使用传入的数据库上下文，如果没有则使用实例字段
            var context = dbContext ?? _DbContext;
            
            try
            {
                if (user.IsSuperAdmin)
                {
                    // 超级管理员可以导出所有发票
                    _Logger.LogDebug("超级管理员 {UserId} 可以导出所有发票", user.Id);
                    return invoicesQuery;
                }

                // 获取用户所属商户 - 使用实例字段
                if (!_MerchantManager.GetIdByUserId(user.Id, out var userMerchantId) || !userMerchantId.HasValue)
                {
                    _Logger.LogWarning("无法确定用户 {UserId} 所属商户，返回空结果", user.Id);
                    return invoicesQuery.Where(i => false); // 返回空查询
                }

                if (user.IsMerchantAdmin)
                {
                    // 商户管理员可以导出本商户的所有发票
                    var merchantOrgs = _OrganizationManager.GetOrLoadByMerchantId(userMerchantId.Value);
                    var merchantOrgIds = merchantOrgs.Keys.Select(id => (Guid?)id).ToHashSet();
                    merchantOrgIds.Add(userMerchantId.Value); // 添加商户ID本身
                    
                    // 通过发票的关联对象确定发票所属机构
                    var merchantFilteredQuery = from invoice in invoicesQuery
                                               join requisition in context.DocFeeRequisitions 
                                                   on invoice.DocFeeRequisitionId equals requisition.Id into reqGroup
                                               from req in reqGroup.DefaultIfEmpty()
                                               join reqItem in context.DocFeeRequisitionItems
                                                   on req.Id equals reqItem.ParentId into reqItemGroup
                                               from reqItemDetail in reqItemGroup.DefaultIfEmpty()
                                               join fee in context.DocFees 
                                                   on reqItemDetail.FeeId equals fee.Id into feeGroup
                                               from docFee in feeGroup.DefaultIfEmpty()
                                               join job in context.PlJobs 
                                                   on docFee.JobId equals job.Id into jobGroup
                                               from plJob in jobGroup.DefaultIfEmpty()
                                               where merchantOrgIds.Contains(req.OrgId) ||       // 申请单机构过滤
                                                     merchantOrgIds.Contains(plJob.OrgId)         // 业务单机构过滤
                                               select invoice;

                    _Logger.LogDebug("商户管理员 {UserId} 可以导出商户 {MerchantId} 下 {OrgCount} 个机构的发票", 
                        user.Id, userMerchantId.Value, merchantOrgIds.Count);
                    
                    return merchantFilteredQuery.Distinct();
                }
                else
                {
                    // 普通用户只能导出与其登录机构相同的发票
                    var userOrganizations = _OrganizationManager.GetOrLoadCurrentOrgsByUser(user);
                    var userOrgIds = userOrganizations.Keys.Select(id => (Guid?)id).ToHashSet();
                    userOrgIds.Add(userMerchantId.Value); // 添加商户ID以支持直接归属商户的发票
                    
                    if (!userOrgIds.Any())
                    {
                        _Logger.LogWarning("用户 {UserId} 未关联任何机构，返回空结果", user.Id);
                        return invoicesQuery.Where(i => false); // 返回空查询
                    }

                    // 通过发票的关联对象确定发票所属机构
                    var userFilteredQuery = from invoice in invoicesQuery
                                           join requisition in context.DocFeeRequisitions 
                                               on invoice.DocFeeRequisitionId equals requisition.Id into reqGroup
                                           from req in reqGroup.DefaultIfEmpty()
                                           join reqItem in context.DocFeeRequisitionItems
                                               on req.Id equals reqItem.ParentId into reqItemGroup
                                           from reqItemDetail in reqItemGroup.DefaultIfEmpty()
                                           join fee in context.DocFees 
                                               on reqItemDetail.FeeId equals fee.Id into feeGroup
                                           from docFee in feeGroup.DefaultIfEmpty()
                                           join job in context.PlJobs 
                                               on docFee.JobId equals job.Id into jobGroup
                                           from plJob in jobGroup.DefaultIfEmpty()
                                           where userOrgIds.Contains(req.OrgId) ||     // 申请单机构过滤
                                                 userOrgIds.Contains(plJob.OrgId)       // 业务单机构过滤
                                           select invoice;

                    _Logger.LogDebug("普通用户 {UserId} 可以导出其关联的 {OrgCount} 个机构的发票", 
                        user.Id, userOrgIds.Count);
                    
                    return userFilteredQuery.Distinct();
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "应用组织权限过滤时发生错误，用户: {UserId}", user.Id);
                // 发生错误时返回空查询以确保安全
                return invoicesQuery.Where(i => false);
            }
        }

        /// <summary>
        /// 静态版本的组织权限过滤方法，用于后台任务中调用。
        /// </summary>
        /// <param name="invoicesQuery">发票查询对象</param>
        /// <param name="user">当前用户</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>过滤后的发票查询对象</returns>
        private static IQueryable<TaxInvoiceInfo> ApplyOrganizationFilterStatic(IQueryable<TaxInvoiceInfo> invoicesQuery, Account user, PowerLmsUserDbContext dbContext, ILogger logger)
        {
            try
            {
                if (user == null)
                {
                    logger.LogWarning("用户对象为null，返回空结果");
                    return invoicesQuery.Where(i => false);
                }

                if (user.IsSuperAdmin)
                {
                    // 超级管理员可以导出所有发票
                    logger.LogDebug("超级管理员 {UserId} 可以导出所有发票", user.Id);
                    return invoicesQuery;
                }

                // 获取用户关联的所有组织机构ID
                var userOrgIds = dbContext.AccountPlOrganizations
                    .Where(apo => apo.UserId == user.Id)
                    .Select(apo => apo.OrgId)
                    .ToList();

                if (!userOrgIds.Any())
                {
                    logger.LogWarning("用户 {UserId} 未关联任何机构，返回空结果", user.Id);
                    return invoicesQuery.Where(i => false);
                }

                // 获取用户关联的所有商户ID（通过组织机构）
                var merchantIds = dbContext.PlOrganizations
                    .Where(o => userOrgIds.Contains(o.Id) && o.MerchantId.HasValue)
                    .Select(o => o.MerchantId.Value)
                    .Distinct()
                    .ToList();

                // 如果没有找到商户ID，检查用户是否直接关联到商户
                if (!merchantIds.Any())
                {
                    // 检查用户关联的组织机构ID是否直接是商户ID
                    var directMerchantIds = dbContext.Merchants
                        .Where(m => userOrgIds.Contains(m.Id))
                        .Select(m => m.Id)
                        .ToList();
                    
                    if (directMerchantIds.Any())
                    {
                        merchantIds.AddRange(directMerchantIds);
                    }
                }

                if (!merchantIds.Any())
                {
                    logger.LogWarning("无法确定用户 {UserId} 所属商户，返回空结果", user.Id);
                    return invoicesQuery.Where(i => false);
                }

                HashSet<Guid?> allowedOrgIds;

                if (user.IsMerchantAdmin)
                {
                    // 商户管理员可以导出本商户的所有发票
                    allowedOrgIds = new HashSet<Guid?>();
                    
                    // 添加所有相关商户ID
                    foreach (var merchantId in merchantIds)
                    {
                        allowedOrgIds.Add(merchantId);
                        
                        // 添加该商户下的所有组织机构ID
                        var merchantOrgIds = dbContext.PlOrganizations
                            .Where(o => o.MerchantId == merchantId)
                            .Select(o => (Guid?)o.Id)
                            .ToList();
                        
                        foreach (var orgId in merchantOrgIds)
                        {
                            allowedOrgIds.Add(orgId);
                        }
                    }

                    logger.LogDebug("商户管理员 {UserId} 可以导出 {MerchantCount} 个商户下 {OrgCount} 个机构的发票", 
                        user.Id, merchantIds.Count, allowedOrgIds.Count);
                }
                else
                {
                    // 普通用户只能导出与其登录机构相关的发票
                    allowedOrgIds = userOrgIds.Select(id => (Guid?)id).ToHashSet();
                    
                    // 添加相关商户ID以支持直接归属商户的发票
                    foreach (var merchantId in merchantIds)
                    {
                        allowedOrgIds.Add(merchantId);
                    }

                    logger.LogDebug("普通用户 {UserId} 可以导出其关联的 {OrgCount} 个机构的发票", 
                        user.Id, allowedOrgIds.Count);
                }

                // 通过发票的关联对象确定发票所属机构并过滤
                var filteredQuery = from invoice in invoicesQuery
                                   join requisition in dbContext.DocFeeRequisitions 
                                       on invoice.DocFeeRequisitionId equals requisition.Id into reqGroup
                                   from req in reqGroup.DefaultIfEmpty()
                                   join reqItem in dbContext.DocFeeRequisitionItems
                                       on req.Id equals reqItem.ParentId into reqItemGroup
                                   from reqItemDetail in reqItemGroup.DefaultIfEmpty()
                                   join fee in dbContext.DocFees 
                                       on reqItemDetail.FeeId equals fee.Id into feeGroup
                                   from docFee in feeGroup.DefaultIfEmpty()
                                   join job in dbContext.PlJobs 
                                       on docFee.JobId equals job.Id into jobGroup
                                   from plJob in jobGroup.DefaultIfEmpty()
                                   where allowedOrgIds.Contains(req.OrgId) ||     // 申请单机构过滤
                                         allowedOrgIds.Contains(plJob.OrgId)       // 业务单机构过滤
                                   select invoice;

                return filteredQuery.Distinct();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "应用组织权限过滤时发生错误，用户: {UserId}", user?.Id);
                // 发生错误时返回空查询以确保安全
                return invoicesQuery.Where(i => false);
            }
        }
    }

    #region DTO定义

    /// <summary>
    /// 发票导出为DBF文件的请求参数。
    /// </summary>
    public class ExportInvoiceToDbfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出条件字典，键为字段名，值为条件值。
        /// 用于筛选要导出的发票数据，支持EfHelper.GenerateWhereAnd的所有查询操作。
        /// </summary>
        public Dictionary<string, string> ExportConditions { get; set; } = new();
    }

    /// <summary>
    /// 发票导出为DBF文件的返回结果。
    /// </summary>
    public class ExportInvoiceToDbfReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 导出任务的唯一标识ID。
        /// 可通过系统的通用任务状态查询接口跟踪进度和获取生成的文件。
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// 任务创建成功的提示消息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 预计要处理的发票数量。
        /// </summary>
        public int ExpectedInvoiceCount { get; set; }
    }

    #endregion
}
