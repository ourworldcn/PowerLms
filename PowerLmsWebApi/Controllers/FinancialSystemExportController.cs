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
            if (serviceScopeFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceScopeFactory), "必须提供服务作用域工厂以确保后台任务的独立性");
            }

            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
                var fileManager = scope.ServiceProvider.GetRequiredService<OwFileManager>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<FinancialSystemExportController>>();

                var task = await dbContext.OwTaskStores.FindAsync(taskId);
                if (task == null)
                {
                    logger.LogWarning("任务 {TaskId} 不存在", taskId);
                    return;
                }

                // 更新任务状态为执行中
                task.Status = OwTaskStatus.Running;
                await dbContext.SaveChangesAsync();
                logger.LogDebug("任务 {TaskId} 开始执行", taskId);

                // 解析任务参数
                var conditions = JsonSerializer.Deserialize<Dictionary<string, string>>(task.Parameters["ExportConditions"]);
                var userId = Guid.Parse(task.Parameters["UserId"]);
                var orgId = !string.IsNullOrEmpty(task.Parameters["OrgId"]) ? Guid.Parse(task.Parameters["OrgId"]) : (Guid?)null;
                var expectedCount = int.Parse(task.Parameters.GetValueOrDefault("ExpectedCount", "0"));
                var exportDateTime = DateTime.Parse(task.Parameters["ExportDateTime"]);

                // 加载科目配置
                var subjectConfigs = await LoadSubjectConfigurations(dbContext, orgId);
                if (!subjectConfigs.Any())
                {
                    task.Status = OwTaskStatus.Failed;
                    task.CompletedUtc = DateTime.UtcNow;
                    task.ErrorMessage = "科目配置未找到，无法生成凭证";
                    await dbContext.SaveChangesAsync();
                    logger.LogError("任务 {TaskId} 失败：科目配置未找到", taskId);
                    return;
                }

                // 在后台任务中重新生成发票集合
                var invoicesQuery = dbContext.TaxInvoiceInfos.AsQueryable();
                if (conditions != null && conditions.Any())
                {
                    invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, conditions);
                }

                // 重新应用组织权限过滤（后台任务中也需要权限过滤）
                if (!string.IsNullOrEmpty(task.Parameters["UserId"]))
                {
                    var taskUserId = Guid.Parse(task.Parameters["UserId"]);
                    var taskUser = await dbContext.Accounts.FindAsync(taskUserId);
                    if (taskUser != null)
                    {
                        // 在静态方法中调用权限过滤
                        invoicesQuery = ApplyOrganizationFilterStatic(invoicesQuery, taskUser, dbContext, logger);
                    }
                }

                var invoices = await invoicesQuery.ToListAsync();
                logger.LogDebug("任务 {TaskId} 实际查询到 {ActualCount} 张发票，预期 {ExpectedCount} 张",
                    taskId, invoices.Count, expectedCount);

                if (!invoices.Any())
                {
                    task.Status = OwTaskStatus.Completed;
                    task.CompletedUtc = DateTime.UtcNow;
                    task.ErrorMessage = "没有找到符合条件的发票数据";
                    await dbContext.SaveChangesAsync();
                    logger.LogWarning("任务 {TaskId} 完成，但未找到符合条件的发票", taskId);
                    return;
                }

                // 查询发票明细数据
                var invoiceIds = invoices.Select(i => i.Id).ToList();
                var invoiceItems = await dbContext.TaxInvoiceInfoItems
                    .Where(item => invoiceIds.Contains(item.ParentId.Value))
                    .ToListAsync();

                var invoiceItemsDict = invoiceItems.GroupBy(item => item.ParentId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 转换为金蝶凭证格式（使用科目配置）
                var kingdeeVouchers = ConvertInvoicesToKingdeeVouchersWithConfig(invoices, invoiceItemsDict, subjectConfigs);
                logger.LogDebug("任务 {TaskId} 生成了 {VoucherCount} 条金蝶凭证记录", taskId, kingdeeVouchers.Count);

                // 生成文件路径
                var fileName = $"Invoice_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
                var relativePath = Path.Combine("FinancialExports", fileName);
                var fullPath = Path.Combine(fileManager.GetDirectory(), relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                // 使用专门的大文件写入函数生成DBF文件
                DotNetDbfUtil.WriteLargeFile(kingdeeVouchers, fullPath);
                logger.LogDebug("任务 {TaskId} 成功生成DBF文件 {FilePath}", taskId, fullPath);

                // 重要：更新导出的发票记录的导出时间
                foreach (var invoice in invoices)
                {
                    invoice.ExportedDateTime = exportDateTime;
                }
                logger.LogDebug("任务 {TaskId} 已更新 {Count} 张发票的导出时间", taskId, invoices.Count);

                // 创建文件信息记录并绑定到任务
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
                using var scope = serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<FinancialSystemExportController>>();

                logger.LogError(ex, "处理发票DBF导出任务 {TaskId} 时发生错误", taskId);

                try
                {
                    var task = await dbContext.OwTaskStores.FindAsync(taskId);
                    if (task != null)
                    {
                        task.Status = OwTaskStatus.Failed;
                        task.CompletedUtc = DateTime.UtcNow;
                        task.ErrorMessage = ex.Message;
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception updateEx)
                {
                    logger.LogError(updateEx, "更新任务状态失败");
                }
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
            var vouchers = new List<KingdeeVoucher>();
            var voucherNumber = 1;

            // 获取通用配置信息
            var preparerName = subjectConfigs.ContainsKey("GEN_PREPARER") ?
                subjectConfigs["GEN_PREPARER"].DisplayName : "系统导出";

            var voucherGroup = subjectConfigs.ContainsKey("GEN_VOUCHER_GROUP") ?
                subjectConfigs["GEN_VOUCHER_GROUP"].VoucherGroup ?? "转" : "转"; // 默认为转账凭证

            foreach (var invoice in invoices)
            {
                var items = invoiceItemsDict.ContainsKey(invoice.Id) ?
                    invoiceItemsDict[invoice.Id] : new List<TaxInvoiceInfoItem>();

                var taxRate = items.FirstOrDefault()?.TaxRate ?? 0.06m;
                var netAmount = invoice.TaxInclusiveAmount / (1 + taxRate);
                var taxAmount = invoice.TaxInclusiveAmount - netAmount;
                var invoiceDate = invoice.InvoiceDate ?? DateTime.Now;
                var description = $"{invoice.BuyerTitle}*{invoice.InvoiceItemName}*{invoice.BuyerTaxNum}";
                var customerCode = invoice.BuyerTaxNum?.Substring(0, Math.Min(10, invoice.BuyerTaxNum.Length)) ?? "CUSTOMER";

                // 借方：应收账款（PBI_ACC_RECEIVABLE）
                if (subjectConfigs.TryGetValue("PBI_ACC_RECEIVABLE", out var accReceivableConfig))
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
                        FACCTID = accReceivableConfig.SubjectNumber, // 从配置表获取科目号
                        FCLSNAME1 = accReceivableConfig.AccountingCategory ?? "客户", // 从配置表获取核算类别
                        FOBJID1 = customerCode,
                        FOBJNAME1 = invoice.BuyerTitle,
                        FTRANSID = invoice.BuyerTaxNum,
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
                if (subjectConfigs.TryGetValue("PBI_SALES_REVENUE", out var salesRevenueConfig))
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
                        FACCTID = salesRevenueConfig.SubjectNumber, // 从配置表获取科目号
                        FCLSNAME1 = salesRevenueConfig.AccountingCategory ?? "客户", // 从配置表获取核算类别
                        FOBJID1 = customerCode,
                        FOBJNAME1 = invoice.BuyerTitle,
                        FTRANSID = invoice.BuyerTaxNum,
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
                if (taxAmount > 0 && subjectConfigs.TryGetValue("PBI_TAX_PAYABLE", out var taxPayableConfig))
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
                        FACCTID = taxPayableConfig.SubjectNumber, // 从配置表获取科目号
                        FCLSNAME1 = taxPayableConfig.AccountingCategory ?? "客户", // 从配置表获取核算类别
                        FOBJID1 = customerCode,
                        FOBJNAME1 = invoice.BuyerTitle,
                        FTRANSID = invoice.BuyerTaxNum,
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
                if (user.IsSuperAdmin)
                {
                    // 超级管理员可以导出所有发票
                    logger.LogDebug("超级管理员 {UserId} 可以导出所有发票", user.Id);
                    return invoicesQuery;
                }

                // 获取用户所属商户
                var userOrgIds = dbContext.AccountPlOrganizations
                    .Where(apo => apo.UserId == user.Id)
                    .Select(apo => apo.OrgId)
                    .FirstOrDefault();

                if (userOrgIds == Guid.Empty)
                {
                    logger.LogWarning("无法确定用户 {UserId} 所属机构，返回空结果", user.Id);
                    return invoicesQuery.Where(i => false);
                }

                // 确定商户ID - 简化逻辑，直接使用组织机构关联
                var merchantId = dbContext.Merchants
                    .Where(m => m.Id == userOrgIds)
                    .Select(m => m.Id)
                    .FirstOrDefault();

                if (merchantId == Guid.Empty)
                {
                    // 如果不是直接商户关联，查找机构所属商户
                    merchantId = dbContext.PlOrganizations
                        .Where(o => o.Id == userOrgIds)
                        .Select(o => o.MerchantId)
                        .FirstOrDefault() ?? Guid.Empty;
                }

                if (merchantId == Guid.Empty)
                {
                    logger.LogWarning("无法确定用户 {UserId} 所属商户，返回空结果", user.Id);
                    return invoicesQuery.Where(i => false);
                }

                HashSet<Guid?> allowedOrgIds;

                if (user.IsMerchantAdmin)
                {
                    // 商户管理员可以导出本商户的所有发票
                    allowedOrgIds = new HashSet<Guid?> { merchantId };
                    var merchantOrgIds = dbContext.PlOrganizations
                        .Where(o => o.MerchantId == merchantId)
                        .Select(o => (Guid?)o.Id)
                        .ToHashSet();
                    allowedOrgIds.UnionWith(merchantOrgIds);

                    logger.LogDebug("商户管理员 {UserId} 可以导出商户 {MerchantId} 下 {OrgCount} 个机构的发票", 
                        user.Id, merchantId, allowedOrgIds.Count);
                }
                else
                {
                    // 普通用户只能导出与其登录机构相关的发票
                    var userOrganizations = dbContext.AccountPlOrganizations
                        .Where(apo => apo.UserId == user.Id)
                        .Select(apo => (Guid?)apo.OrgId)
                        .ToHashSet();

                    if (!userOrganizations.Any())
                    {
                        logger.LogWarning("用户 {UserId} 未关联任何机构，返回空结果", user.Id);
                        return invoicesQuery.Where(i => false);
                    }

                    allowedOrgIds = userOrganizations;
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
                logger.LogError(ex, "应用组织权限过滤时发生错误，用户: {UserId}", user.Id);
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
