using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLms.Data.Finance;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Text.Json;
using OW.Data;
using DotNetDBF;

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
        /// <param name="fileService">文件服务，提供文件系统操作的统一接口</param>
        /// <param name="merchantManager">商户管理器，用于商户相关操作</param>
        /// <param name="organizationManager">机构管理器，用于机构相关操作</param>
        public FinancialSystemExportController(AccountManager accountManager, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory,
            PowerLmsUserDbContext dbContext, ILogger<FinancialSystemExportController> logger, OwFileService<PowerLmsUserDbContext> fileService,
            MerchantManager merchantManager, OrganizationManager organizationManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _ServiceScopeFactory = serviceScopeFactory;
            _DbContext = dbContext;
            _Logger = logger;
            _FileService = fileService;
            _MerchantManager = merchantManager;
            _OrganizationManager = organizationManager;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider; // 保留，用于当前请求作用域的服务解析
        private readonly IServiceScopeFactory _ServiceScopeFactory; // 用于后台任务的独立作用域创建
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly ILogger<FinancialSystemExportController> _Logger;
        private readonly OwFileService<PowerLmsUserDbContext> _FileService;
        private readonly MerchantManager _MerchantManager;
        private readonly OrganizationManager _OrganizationManager;

        #region HTTP接口

        /// <summary>
        /// 导出发票数据为金蝶DBF格式文件。
        /// 使用改进后的OwTaskService统一任务调度机制。
        /// </summary>
        /// <param name="model">导出参数，包含查询条件和用户令牌</param>
        /// <returns>导出任务信息，包含任务ID用于跟踪进度</returns>
        [HttpPost]
        public ActionResult<ExportInvoiceToDbfReturnDto> ExportInvoiceToDbf(ExportInvoiceToDbfParamsDto model)
        {
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

                // 预检查发票数量
                var invoicesQuery = _DbContext.TaxInvoiceInfos.Where(c => c.ExportedDateTime == null).AsQueryable();
                if (model.ExportConditions != null && model.ExportConditions.Any())
                {
                    invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, model.ExportConditions);
                }
                invoicesQuery = ApplyOrganizationFilter(invoicesQuery, context.User);

                var invoiceCount = invoicesQuery.Count();
                if (invoiceCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "没有找到符合条件的发票数据，请调整查询条件";
                    return result;
                }

                // 使用OwFileService创建任务（同步方式）
                var taskService = _ServiceProvider.GetRequiredService<OwTaskService<PowerLmsUserDbContext>>();
                var exportDateTime = DateTime.UtcNow;
                
                var taskParameters = new Dictionary<string, string>
                {
                    ["ExportConditions"] = JsonSerializer.Serialize(model.ExportConditions),
                    ["UserId"] = context.User.Id.ToString(),
                    ["OrgId"] = context.User.OrgId?.ToString() ?? "",
                    ["ExpectedCount"] = invoiceCount.ToString(),
                    ["ExportDateTime"] = exportDateTime.ToString("O")
                };

                var taskId = taskService.CreateTask(typeof(FinancialSystemExportController),
                    nameof(ProcessInvoiceDbfExportTask), 
                    taskParameters, 
                    context.User.Id, 
                    context.User.OrgId);

                result.TaskId = taskId;
                result.DebugMessage = $"导出任务已创建，预计处理 {invoiceCount} 张发票。可通过系统任务状态查询接口跟踪进度。";
                result.ExpectedInvoiceCount = invoiceCount;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = ex.Message;
            }
            return result;
        }

        #endregion

        #region 静态任务处理方法

        /// <summary>
        /// 处理发票DBF导出任务（静态方法，由OwTaskService调用）
        /// 保持方法名不变以确保向后兼容性
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="parameters">任务参数</param>
        /// <param name="serviceProvider">服务提供者（由OwTaskService自动注入）</param>
        /// <returns>任务执行结果</returns>
        public static object ProcessInvoiceDbfExportTask(Guid taskId, Dictionary<string, string> parameters, IServiceProvider serviceProvider)
        {
            string currentStep = "参数验证";
            var stepStartTime = DateTime.UtcNow;
            
            try
            {
                // 详细的参数验证和日志记录
                if (serviceProvider == null)
                    throw new ArgumentNullException(nameof(serviceProvider), "服务提供者不能为空");
                
                if (parameters == null)
                    throw new ArgumentNullException(nameof(parameters), "任务参数不能为空");

                // 记录任务开始信息
                var logger = serviceProvider.GetService<ILogger<FinancialSystemExportController>>();
                logger?.LogInformation("开始处理发票DBF导出任务 {TaskId}，参数数量: {ParamCount}", taskId, parameters.Count);

                currentStep = "解析服务依赖";
                stepStartTime = DateTime.UtcNow;
                
                // 获取必要的服务，添加空值检查
                var dbContextFactory = serviceProvider.GetService<IDbContextFactory<PowerLmsUserDbContext>>();
                if (dbContextFactory == null)
                    throw new InvalidOperationException("无法获取数据库上下文工厂 - 请检查服务注册");

                var fileService = serviceProvider.GetService<OwFileService<PowerLmsUserDbContext>>();
                if (fileService == null)
                    throw new InvalidOperationException("无法获取文件服务 - 请检查服务注册");

                logger?.LogDebug("任务 {TaskId} 服务依赖解析完成，耗时: {Duration}ms", taskId, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "解析任务参数";
                stepStartTime = DateTime.UtcNow;
                
                // 解析任务参数，增加详细验证
                if (!parameters.TryGetValue("ExportConditions", out var exportConditionsJson))
                    throw new InvalidOperationException($"任务参数缺少 'ExportConditions'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");
                
                if (!parameters.TryGetValue("UserId", out var userIdStr))
                    throw new InvalidOperationException($"任务参数缺少 'UserId'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");
                
                if (!parameters.TryGetValue("ExportDateTime", out var exportDateTimeStr))
                    throw new InvalidOperationException($"任务参数缺少 'ExportDateTime'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");

                currentStep = "解析参数值";
                stepStartTime = DateTime.UtcNow;
                
                Dictionary<string, string> conditions = null;
                if (!string.IsNullOrEmpty(exportConditionsJson))
                {
                    try
                    {
                        conditions = JsonSerializer.Deserialize<Dictionary<string, string>>(exportConditionsJson);
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException($"无法反序列化导出条件JSON: {exportConditionsJson}", ex);
                    }
                }
                conditions ??= new Dictionary<string, string>();

                if (!Guid.TryParse(userIdStr, out var userId))
                    throw new InvalidOperationException($"无效的用户ID格式: {userIdStr}");

                Guid? orgId = null;
                if (parameters.TryGetValue("OrgId", out var orgIdStr) && !string.IsNullOrEmpty(orgIdStr))
                {
                    if (!Guid.TryParse(orgIdStr, out var parsedOrgId))
                        throw new InvalidOperationException($"无效的组织ID格式: {orgIdStr}");
                    orgId = parsedOrgId;
                }

                if (!int.TryParse(parameters.GetValueOrDefault("ExpectedCount", "0"), out var expectedCount))
                    expectedCount = 0;

                if (!DateTime.TryParse(exportDateTimeStr, out var exportDateTime))
                    throw new InvalidOperationException($"无效的导出时间格式: {exportDateTimeStr}");

                logger?.LogDebug("任务 {TaskId} 参数解析完成，用户ID: {UserId}, 组织ID: {OrgId}, 预期数量: {ExpectedCount}", 
                    taskId, userId, orgId, expectedCount);

                currentStep = "创建数据库上下文";
                stepStartTime = DateTime.UtcNow;
                
                using var dbContext = dbContextFactory.CreateDbContext();
                if (dbContext == null)
                    throw new InvalidOperationException("创建数据库上下文失败");

                logger?.LogDebug("任务 {TaskId} 数据库上下文创建完成，耗时: {Duration}ms", taskId, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "加载科目配置";
                stepStartTime = DateTime.UtcNow;
                
                // 加载科目配置
                var subjectConfigs = LoadSubjectConfigurations(dbContext, orgId);
                if (subjectConfigs == null)
                    throw new InvalidOperationException("LoadSubjectConfigurations 返回 null");
                
                if (!subjectConfigs.Any())
                    throw new InvalidOperationException($"科目配置未找到，无法生成凭证。组织ID: {orgId}，任务ID: {taskId}");

                logger?.LogDebug("任务 {TaskId} 科目配置加载完成，配置数量: {ConfigCount}，耗时: {Duration}ms", 
                    taskId, subjectConfigs.Count, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "构建发票查询";
                stepStartTime = DateTime.UtcNow;
                
                // 查询发票数据
                var invoicesQuery = dbContext.TaxInvoiceInfos?.Where(i => i.ExportedDateTime == null);
                if (invoicesQuery == null)
                    throw new InvalidOperationException("无法访问TaxInvoiceInfos数据集 - 请检查数据库连接");

                if (conditions != null && conditions.Any())
                {
                    try
                    {
                        invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, conditions);
                        if (invoicesQuery == null)
                            throw new InvalidOperationException("EfHelper.GenerateWhereAnd 返回 null");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"应用查询条件失败，条件: {string.Join(", ", conditions.Select(kv => $"{kv.Key}={kv.Value}"))}", ex);
                    }
                }

                logger?.LogDebug("任务 {TaskId} 查询条件构建完成，条件数量: {ConditionCount}，耗时: {Duration}ms", 
                    taskId, conditions?.Count ?? 0, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "应用权限过滤";
                stepStartTime = DateTime.UtcNow;
                
                // 应用权限过滤
                var taskUser = dbContext.Accounts?.Find(userId);
                if (taskUser == null)
                    throw new InvalidOperationException($"未找到用户 {userId}，无法应用权限过滤");

                try
                {
                    invoicesQuery = ApplyOrganizationFilterStatic(invoicesQuery, taskUser, dbContext, serviceProvider);
                    if (invoicesQuery == null)
                        throw new InvalidOperationException("ApplyOrganizationFilterStatic 返回 null");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"应用组织权限过滤失败，用户ID: {userId}", ex);
                }

                logger?.LogDebug("任务 {TaskId} 权限过滤完成，耗时: {Duration}ms", taskId, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "查询发票数据";
                stepStartTime = DateTime.UtcNow;
                
                List<TaxInvoiceInfo> invoices;
                try
                {
                    invoices = invoicesQuery.ToList();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("执行发票数据查询失败", ex);
                }

                if (invoices == null)
                    throw new InvalidOperationException("发票查询返回 null");

                if (!invoices.Any())
                    throw new InvalidOperationException($"没有找到符合条件的发票数据。任务ID: {taskId}，预期数量: {expectedCount}，实际数量: 0");

                logger?.LogInformation("任务 {TaskId} 发票数据查询完成，发票数量: {InvoiceCount}，耗时: {Duration}ms", 
                    taskId, invoices.Count, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "查询发票明细";
                stepStartTime = DateTime.UtcNow;
                
                // 查询发票明细数据
                var invoiceIds = invoices.Select(i => i.Id).ToList();
                List<TaxInvoiceInfoItem> invoiceItems;
                try
                {
                    invoiceItems = dbContext.TaxInvoiceInfoItems
                        ?.Where(item => invoiceIds.Contains(item.ParentId.Value))
                        ?.ToList();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("查询发票明细失败", ex);
                }

                if (invoiceItems == null)
                    invoiceItems = new List<TaxInvoiceInfoItem>();

                var invoiceItemsDict = invoiceItems.GroupBy(item => item.ParentId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                logger?.LogDebug("任务 {TaskId} 发票明细查询完成，明细数量: {ItemCount}，耗时: {Duration}ms", 
                    taskId, invoiceItems.Count, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "转换为金蝶凭证格式";
                stepStartTime = DateTime.UtcNow;
                
                // 转换为金蝶凭证格式
                List<KingdeeVoucher> kingdeeVouchers;
                try
                {
                    kingdeeVouchers = ConvertInvoicesToKingdeeVouchersWithConfig(invoices, invoiceItemsDict, subjectConfigs);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"转换为金蝶凭证格式失败，发票数量: {invoices.Count}", ex);
                }

                if (kingdeeVouchers == null)
                    throw new InvalidOperationException("ConvertInvoicesToKingdeeVouchersWithConfig 返回 null");

                if (!kingdeeVouchers.Any())
                    throw new InvalidOperationException($"生成的金蝶凭证记录为空。任务ID: {taskId}，发票数量: {invoices.Count}");

                logger?.LogInformation("任务 {TaskId} 凭证转换完成，凭证数量: {VoucherCount}，耗时: {Duration}ms", 
                    taskId, kingdeeVouchers.Count, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);

                currentStep = "生成DBF文件";
                stepStartTime = DateTime.UtcNow;
                
                // 优化的DBF文件生成和保存流程
                var fileName = $"Invoice_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
                
                // 为金蝶凭证创建特定的字段映射，只包含必要的字段
                var kingdeeFieldMappings = new Dictionary<string, string>
                {
                    {"FDATE", "FDATE"},
                    {"FTRANSDATE", "FTRANSDATE"}, 
                    {"FPERIOD", "FPERIOD"},
                    {"FGROUP", "FGROUP"},
                    {"FNUM", "FNUM"},
                    {"FENTRYID", "FENTRYID"},
                    {"FEXP", "FEXP"},
                    {"FACCTID", "FACCTID"},
                    {"FCLSNAME1", "FCLSNAME1"},
                    {"FOBJID1", "FOBJID1"},
                    {"FOBJNAME1", "FOBJNAME1"},
                    {"FTRANSID", "FTRANSID"},
                    {"FCYID", "FCYID"},
                    {"FEXCHRATE", "FEXCHRATE"},
                    {"FDC", "FDC"},
                    {"FFCYAMT", "FFCYAMT"},
                    {"FDEBIT", "FDEBIT"},
                    {"FCREDIT", "FCREDIT"},
                    {"FPREPARE", "FPREPARE"},
                    {"FMODULE", "FMODULE"},
                    {"FDELETED", "FDELETED"}
                };
                
                // 为特定字段指定类型
                var customFieldTypes = new Dictionary<string, NativeDbType>
                {
                    {"FDATE", NativeDbType.Date},
                    {"FTRANSDATE", NativeDbType.Date},
                    {"FPERIOD", NativeDbType.Numeric},
                    {"FGROUP", NativeDbType.Char},
                    {"FNUM", NativeDbType.Numeric},
                    {"FENTRYID", NativeDbType.Numeric},
                    {"FEXP", NativeDbType.Char},
                    {"FACCTID", NativeDbType.Char},
                    {"FCLSNAME1", NativeDbType.Char},
                    {"FOBJID1", NativeDbType.Char},
                    {"FOBJNAME1", NativeDbType.Char},
                    {"FTRANSID", NativeDbType.Char},
                    {"FCYID", NativeDbType.Char},
                    {"FEXCHRATE", NativeDbType.Numeric},
                    {"FDC", NativeDbType.Numeric},
                    {"FFCYAMT", NativeDbType.Numeric},
                    {"FDEBIT", NativeDbType.Numeric},
                    {"FCREDIT", NativeDbType.Numeric},
                    {"FPREPARE", NativeDbType.Char},
                    {"FMODULE", NativeDbType.Char},
                    {"FDELETED", NativeDbType.Logical}
                };
                
                // 生成临时文件路径，使用WriteLargeFile方法处理大文件
                var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
                
                try
                {
                    // 使用WriteLargeFile方法写入DBF文件，适用于大数据量的写入操作
                    DotNetDbfUtil.WriteLargeFile(kingdeeVouchers, tempFilePath, kingdeeFieldMappings, customFieldTypes);
                    
                    // 验证文件是否成功生成
                    if (!System.IO.File.Exists(tempFilePath))
                    {
                        throw new InvalidOperationException($"DBF文件生成失败，文件不存在: {tempFilePath}");
                    }
                    
                    var fileInfo = new FileInfo(tempFilePath);
                    if (fileInfo.Length == 0)
                    {
                        throw new InvalidOperationException($"DBF文件生成失败，文件为空: {tempFilePath}");
                    }
                    
                    logger?.LogDebug("任务 {TaskId} DBF文件生成完成，文件大小: {FileSize} bytes，文件路径: {FilePath}，耗时: {Duration}ms", 
                        taskId, fileInfo.Length, tempFilePath, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    // 清理临时文件
                    try
                    {
                        if (System.IO.File.Exists(tempFilePath))
                            System.IO.File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // 忽略清理时的异常
                    }
                    
                    throw new InvalidOperationException($"生成DBF文件失败，凭证数量: {kingdeeVouchers.Count}，文件名: {fileName}", ex);
                }

                currentStep = "创建文件记录";
                stepStartTime = DateTime.UtcNow;
                
                // 从临时文件创建文件记录
                PlFileInfo fileInfoRecord;
                long tempFileSize = 0; // 记录临时文件大小用于验证
                
                try
                {
                    // 在创建文件记录前记录临时文件信息
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        tempFileSize = new FileInfo(tempFilePath).Length;
                        logger?.LogDebug("任务 {TaskId} 准备创建文件记录，临时文件大小: {TempFileSize} bytes", taskId, tempFileSize);
                    }
                    else
                    {
                        throw new InvalidOperationException($"临时文件不存在，无法创建文件记录: {tempFilePath}");
                    }

                    using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                    {
                        fileInfoRecord = fileService.CreateFile(
                            fileStream: fileStream,
                            fileName: fileName,
                            displayName: $"发票导出-{DateTime.Now:yyyy年MM月dd日}",
                            parentId: taskId,
                            creatorId: userId,
                            remark: $"发票DBF导出文件，共{invoices.Count}张发票，{kingdeeVouchers.Count}条会计分录，导出时间：{exportDateTime:yyyy-MM-dd HH:mm:ss}",
                            subDirectory: "FinancialExports",
                            skipValidation: true // DBF文件特殊，跳过文件大小和扩展名验证
                        );
                    }
                    
                    // 验证文件记录创建成功
                    if (fileInfoRecord == null)
                        throw new InvalidOperationException("fileService.CreateFile 返回 null");
                    
                    logger?.LogDebug("任务 {TaskId} 文件记录创建完成，文件ID: {FileId}，目标路径: {FilePath}，耗时: {Duration}ms", 
                        taskId, fileInfoRecord.Id, fileInfoRecord.FilePath, (DateTime.UtcNow - stepStartTime).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"创建文件记录失败，文件名: {fileName}，临时文件: {tempFilePath}", ex);
                }
                finally
                {
                    // 清理临时文件
                    try
                    {
                        if (System.IO.File.Exists(tempFilePath))
                        {
                            System.IO.File.Delete(tempFilePath);
                            logger?.LogDebug("任务 {TaskId} 临时文件已清理: {TempFilePath}", taskId, tempFilePath);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        logger?.LogWarning(cleanupEx, "任务 {TaskId} 清理临时文件失败: {TempFilePath}", taskId, tempFilePath);
                    }
                }

                currentStep = "验证最终文件并返回结果";
                
                // 验证最终文件是否存在并获取大小
                long actualFileSize = 0;
                bool fileExists = false;
                
                try
                {
                    if (System.IO.File.Exists(fileInfoRecord.FilePath))
                    {
                        actualFileSize = new FileInfo(fileInfoRecord.FilePath).Length;
                        fileExists = true;
                        logger?.LogDebug("任务 {TaskId} 最终文件验证成功，大小: {FileSize} bytes", taskId, actualFileSize);
                        
                        // 验证文件大小是否合理
                        if (actualFileSize != tempFileSize && tempFileSize > 0)
                        {
                            logger?.LogWarning("任务 {TaskId} 文件大小不匹配，临时文件: {TempSize}, 最终文件: {FinalSize}", 
                                taskId, tempFileSize, actualFileSize);
                        }
                    }
                    else
                    {
                        logger?.LogError("任务 {TaskId} 最终文件不存在: {FilePath}", taskId, fileInfoRecord.FilePath);
                        // 不抛出异常，因为文件记录已创建，任务基本成功
                    }
                }
                catch (Exception sizeEx)
                {
                    logger?.LogWarning(sizeEx, "任务 {TaskId} 无法访问最终文件: {FilePath}", taskId, fileInfoRecord.FilePath);
                }
                
                // 返回任务执行结果
                var result = new
                {
                    FileId = fileInfoRecord.Id,
                    FileName = fileName,
                    InvoiceCount = invoices.Count,
                    VoucherCount = kingdeeVouchers.Count,
                    FilePath = fileInfoRecord.FilePath,
                    ExportDateTime = exportDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileSize = actualFileSize,
                    FileExists = fileExists,
                    TempFileSize = tempFileSize
                };

                logger?.LogInformation("任务 {TaskId} 执行成功完成，发票数量: {InvoiceCount}，凭证数量: {VoucherCount}，文件存在: {FileExists}，文件大小: {FileSize} bytes", 
                    taskId, invoices.Count, kingdeeVouchers.Count, fileExists, actualFileSize);

                return result;
            }
            catch (Exception ex)
            {
                // 获取日志记录器（容错处理）
                var logger = serviceProvider?.GetService<ILogger<FinancialSystemExportController>>();
                
                var contextualError = $"发票DBF导出任务失败，当前步骤: {currentStep}, 任务ID: {taskId}";
                if (parameters != null)
                {
                    contextualError += $"\n任务参数: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}";
                }
                
                // 记录详细错误信息
                logger?.LogError(ex, "任务 {TaskId} 在步骤 '{CurrentStep}' 执行失败，错误类型: {ExceptionType}", 
                    taskId, currentStep, ex.GetType().Name);
                
                // 使用ExceptionDispatchInfo保留原始堆栈信息，或者直接重新抛出原异常
                if (ex is InvalidOperationException || ex is ArgumentException || ex is JsonException)
                {
                    // 对于已知的业务异常，添加上下文信息但保留原始异常
                    throw new InvalidOperationException(contextualError, ex);
                }
                else
                {
                    // 对于其他异常，直接重新抛出以保留完整的堆栈信息
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
                    throw; // 这行永远不会执行，但编译器需要
                }
            }
        }

        #endregion

        #region 静态辅助方法

        /// <summary>
        /// 加载科目配置（静态版本）
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "PBI_ACC_RECEIVABLE",   // 应收账款
                "PBI_SALES_REVENUE",    // 主营业务收入
                "PBI_TAX_PAYABLE",      // 应交税金
                "GEN_PREPARER",         // 制单人
                "GEN_VOUCHER_GROUP"     // 凭证类别字
            };

            var configs = dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToList();

            return configs.ToDictionary(c => c.Code, c => c);
        }

        /// <summary>
        /// 将发票数据转换为金蝶凭证格式（静态版本）
        /// 增强数据验证和安全性
        /// </summary>
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
                    
                    // 安全地处理字符串，确保不会超过DBF字段限制
                    var buyerTitle = (invoice.BuyerTitle ?? "未知客户").Length > 200 ? 
                        (invoice.BuyerTitle ?? "未知客户").Substring(0, 200) : 
                        (invoice.BuyerTitle ?? "未知客户");
                    
                    var invoiceItemName = (invoice.InvoiceItemName ?? "未知项目").Length > 200 ? 
                        (invoice.InvoiceItemName ?? "未知项目").Substring(0, 200) : 
                        (invoice.InvoiceItemName ?? "未知项目");
                        
                    var buyerTaxNum = (invoice.BuyerTaxNum ?? "").Length > 50 ? 
                        (invoice.BuyerTaxNum ?? "").Substring(0, 50) : 
                        (invoice.BuyerTaxNum ?? "");
                    
                    var description = $"{buyerTitle}*{invoiceItemName}*{buyerTaxNum}";
                    if (description.Length > 500)
                    {
                        description = description.Substring(0, 500);
                    }
                    
                    var customerCode = string.IsNullOrEmpty(buyerTaxNum) ? 
                        "CUSTOMER" : 
                        buyerTaxNum.Substring(0, Math.Min(10, buyerTaxNum.Length));

                    // 借方：应收账款（PBI_ACC_RECEIVABLE）
                    if (subjectConfigs.TryGetValue("PBI_ACC_RECEIVABLE", out var accReceivableConfig) && accReceivableConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup,
                            FNUM = voucherNumber,
                            FENTRYID = 0,
                            FEXP = description,
                            FACCTID = accReceivableConfig.SubjectNumber ?? "122101",
                            FCLSNAME1 = accReceivableConfig.AccountingCategory ?? "客户",
                            FOBJID1 = customerCode,
                            FOBJNAME1 = buyerTitle,
                            FTRANSID = buyerTaxNum,
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 0, // 借方
                            FFCYAMT = invoice.TaxInclusiveAmount,
                            FDEBIT = invoice.TaxInclusiveAmount,
                            FCREDIT = 0,
                            FPREPARE = preparerName,
                            // 确保可选字段有默认值
                            FQTY = null,
                            FPRICE = null,
                            FSETTLCODE = null,
                            FSETTLENO = null,
                            FPAY = null,
                            FCASH = null,
                            FPOSTER = null,
                            FCHECKER = null,
                            FATTCHMENT = null,
                            FPOSTED = null,
                            FMODULE = "GL",
                            FDELETED = false,
                            FSERIALNO = null,
                            FUNITNAME = null,
                            FREFERENCE = null,
                            FCASHFLOW = null,
                            FHANDLER = null
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
                            FGROUP = voucherGroup,
                            FNUM = voucherNumber,
                            FENTRYID = 1,
                            FEXP = description,
                            FACCTID = salesRevenueConfig.SubjectNumber ?? "601001",
                            FCLSNAME1 = salesRevenueConfig.AccountingCategory ?? "客户",
                            FOBJID1 = customerCode,
                            FOBJNAME1 = buyerTitle,
                            FTRANSID = buyerTaxNum,
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 1, // 贷方
                            FFCYAMT = netAmount,
                            FDEBIT = 0,
                            FCREDIT = netAmount,
                            FPREPARE = preparerName,
                            // 确保可选字段有默认值
                            FQTY = null,
                            FPRICE = null,
                            FSETTLCODE = null,
                            FSETTLENO = null,
                            FPAY = null,
                            FCASH = null,
                            FPOSTER = null,
                            FCHECKER = null,
                            FATTCHMENT = null,
                            FPOSTED = null,
                            FMODULE = "GL",
                            FDELETED = false,
                            FSERIALNO = null,
                            FUNITNAME = null,
                            FREFERENCE = null,
                            FCASHFLOW = null,
                            FHANDLER = null
                        });
                    }

                    // 贷方：应交税金（PBI_TAX_PAYABLE）
                    if (taxAmount > 0 && subjectConfigs.TryGetValue("PBI_TAX_PAYABLE", out var taxPayableConfig) && taxPayableConfig != null)
                    {
                        vouchers.Add(new KingdeeVoucher
                        {
                            Id = Guid.NewGuid(),
                            FDATE = invoiceDate,
                            FTRANSDATE = invoiceDate,
                            FPERIOD = invoiceDate.Month,
                            FGROUP = voucherGroup,
                            FNUM = voucherNumber,
                            FENTRYID = 2,
                            FEXP = description,
                            FACCTID = taxPayableConfig.SubjectNumber ?? "221001",
                            FCLSNAME1 = taxPayableConfig.AccountingCategory ?? "客户",
                            FOBJID1 = customerCode,
                            FOBJNAME1 = buyerTitle,
                            FTRANSID = buyerTaxNum,
                            FCYID = "RMB",
                            FEXCHRATE = 1.0000000m,
                            FDC = 1, // 贷方
                            FFCYAMT = taxAmount,
                            FDEBIT = 0,
                            FCREDIT = taxAmount,
                            FPREPARE = preparerName,
                            // 确保可选字段有默认值
                            FQTY = null,
                            FPRICE = null,
                            FSETTLCODE = null,
                            FSETTLENO = null,
                            FPAY = null,
                            FCASH = null,
                            FPOSTER = null,
                            FCHECKER = null,
                            FATTCHMENT = null,
                            FPOSTED = null,
                            FMODULE = "GL",
                            FDELETED = false,
                            FSERIALNO = null,
                            FUNITNAME = null,
                            FREFERENCE = null,
                            FCASHFLOW = null,
                            FHANDLER = null
                        });
                    }
                    voucherNumber++;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"处理发票 {invoice.Id} 时发生错误: {ex.Message}", ex);
                }
            }
            
            if (vouchers.Count == 0)
            {
                throw new InvalidOperationException("未能生成任何金蝶凭证记录，请检查科目配置或发票数据");
            }
            
            return vouchers;
        }

        /// <summary>
        /// 静态版本的组织权限过滤方法
        /// </summary>
        private static IQueryable<TaxInvoiceInfo> ApplyOrganizationFilterStatic(IQueryable<TaxInvoiceInfo> invoicesQuery, Account user, 
            PowerLmsUserDbContext dbContext, IServiceProvider serviceProvider)
        {
            if (user == null)
            {
                return invoicesQuery.Where(i => false);
            }

            if (user.IsSuperAdmin)
            {
                return invoicesQuery;
            }

            var merchantManager = serviceProvider.GetRequiredService<MerchantManager>();
            var organizationManager = serviceProvider.GetRequiredService<OrganizationManager>();

            // 获取用户关联的所有组织机构ID
            var userOrgIds = dbContext.AccountPlOrganizations
                .Where(apo => apo.UserId == user.Id)
                .Select(apo => apo.OrgId)
                .ToList();

            if (!userOrgIds.Any())
            {
                return invoicesQuery.Where(i => false);
            }

            // 获取用户关联的所有商户ID
            var merchantIds = dbContext.PlOrganizations
                .Where(o => userOrgIds.Contains(o.Id) && o.MerchantId.HasValue)
                .Select(o => o.MerchantId.Value)
                .Distinct()
                .ToList();

            if (!merchantIds.Any())
            {
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
                return invoicesQuery.Where(i => false);
            }

            HashSet<Guid?> allowedOrgIds;

            if (user.IsMerchantAdmin)
            {
                allowedOrgIds = new HashSet<Guid?>();
                
                foreach (var merchantId in merchantIds)
                {
                    allowedOrgIds.Add(merchantId);
                    
                    var merchantOrgIds = dbContext.PlOrganizations
                        .Where(o => o.MerchantId == merchantId)
                        .Select(o => (Guid?)o.Id)
                        .ToList();
                    
                    foreach (var orgId in merchantOrgIds)
                    {
                        allowedOrgIds.Add(orgId);
                    }
                }
            }
            else
            {
                allowedOrgIds = userOrgIds.Select(id => (Guid?)id).ToHashSet();
                
                foreach (var merchantId in merchantIds)
                {
                    allowedOrgIds.Add(merchantId);
                }
            }

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
                               where allowedOrgIds.Contains(req.OrgId) ||
                                     allowedOrgIds.Contains(plJob.OrgId)
                               select invoice;

            return filteredQuery.Distinct();
        }

        #endregion

        #region 实例方法

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
            };

            var existingCodes = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();

            return requiredCodes.Except(existingCodes).ToList();
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
            var context = dbContext ?? _DbContext;
            
            if (user.IsSuperAdmin)
            {
                return invoicesQuery;
            }

            if (!_MerchantManager.GetIdByUserId(user.Id, out var userMerchantId) || !userMerchantId.HasValue)
            {
                return invoicesQuery.Where(i => false);
            }

            if (user.IsMerchantAdmin)
            {
                var merchantOrgs = _OrganizationManager.GetOrLoadByMerchantId(userMerchantId.Value);
                var merchantOrgIds = merchantOrgs.Keys.Select(id => (Guid?)id).ToHashSet();
                merchantOrgIds.Add(userMerchantId.Value);
                
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
                                           where merchantOrgIds.Contains(req.OrgId) ||
                                                 merchantOrgIds.Contains(plJob.OrgId)
                                           select invoice;
                
                return merchantFilteredQuery.Distinct();
            }
            else
            {
                var userOrganizations = _OrganizationManager.GetOrLoadCurrentOrgsByUser(user);
                var userOrgIds = userOrganizations.Keys.Select(id => (Guid?)id).ToHashSet();
                userOrgIds.Add(userMerchantId.Value);
                
                if (!userOrgIds.Any())
                {
                    return invoicesQuery.Where(i => false);
                }

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
                                       where userOrgIds.Contains(req.OrgId) ||
                                             userOrgIds.Contains(plJob.OrgId)
                                       select invoice;
                
                return userFilteredQuery.Distinct();
            }
        }

        #endregion
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
