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

                // 使用OwTaskService创建任务（同步方式）
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

                var taskId = taskService.CreateTask<FinancialSystemExportController>(
                    nameof(ProcessInvoiceDbfExportTask), 
                    taskParameters, 
                    context.User.Id, 
                    context.User.OrgId);

                result.TaskId = taskId;
                result.DebugMessage = $"导出任务已创建，预计处理 {invoiceCount} 张发票。可通过系统任务状态查询接口跟踪进度。";
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
        /// 处理发票DBF导出任务（由OwTaskService调用）
        /// 这个方法会被OwTaskService通过反射调用，不应暴露为HTTP端点
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>任务执行结果</returns>
        [NonAction] // 防止Swagger将此方法识别为HTTP端点
        public object ProcessInvoiceDbfExportTask(Guid taskId, Dictionary<string, string> parameters)
        {
            try
            {
                _Logger.LogInformation("开始处理发票DBF导出任务 {TaskId}", taskId);

                // 解析任务参数
                if (!parameters.TryGetValue("ExportConditions", out var exportConditionsJson) ||
                    !parameters.TryGetValue("UserId", out var userIdStr) ||
                    !parameters.TryGetValue("ExportDateTime", out var exportDateTimeStr))
                {
                    throw new InvalidOperationException($"任务 {taskId} 参数不完整");
                }

                var conditions = string.IsNullOrEmpty(exportConditionsJson) ? 
                    new Dictionary<string, string>() : 
                    JsonSerializer.Deserialize<Dictionary<string, string>>(exportConditionsJson);
                
                var userId = Guid.Parse(userIdStr);
                var orgId = parameters.TryGetValue("OrgId", out var orgIdStr) && !string.IsNullOrEmpty(orgIdStr) ? 
                    Guid.Parse(orgIdStr) : (Guid?)null;
                var expectedCount = int.Parse(parameters.GetValueOrDefault("ExpectedCount", "0"));
                var exportDateTime = DateTime.Parse(exportDateTimeStr);

                // 加载科目配置
                var subjectConfigs = LoadSubjectConfigurations(orgId);
                if (!subjectConfigs.Any())
                {
                    throw new InvalidOperationException($"科目配置未找到，无法生成凭证。组织ID: {orgId}");
                }

                // 查询发票数据
                var invoicesQuery = _DbContext.TaxInvoiceInfos.Where(i => i.ExportedDateTime == null).AsQueryable();
                if (conditions != null && conditions.Any())
                {
                    invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, conditions);
                }

                // 应用权限过滤
                var taskUser = _DbContext.Accounts.Find(userId);
                if (taskUser != null)
                {
                    invoicesQuery = ApplyOrganizationFilter(invoicesQuery, taskUser);
                }

                var invoices = invoicesQuery.ToList();
                _Logger.LogInformation("任务 {TaskId} 实际查询到 {ActualCount} 张发票，预期 {ExpectedCount} 张",
                    taskId, invoices.Count, expectedCount);

                if (!invoices.Any())
                {
                    throw new InvalidOperationException("没有找到符合条件的发票数据");
                }

                // 查询发票明细数据
                var invoiceIds = invoices.Select(i => i.Id).ToList();
                var invoiceItems = _DbContext.TaxInvoiceInfoItems
                    .Where(item => invoiceIds.Contains(item.ParentId.Value))
                    .ToList();

                var invoiceItemsDict = invoiceItems.GroupBy(item => item.ParentId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 转换为金蝶凭证格式
                var kingdeeVouchers = ConvertInvoicesToKingdeeVouchersWithConfig(invoices, invoiceItemsDict, subjectConfigs);
                _Logger.LogInformation("任务 {TaskId} 生成了 {VoucherCount} 条金蝶凭证记录", taskId, kingdeeVouchers.Count);

                if (!kingdeeVouchers.Any())
                {
                    throw new InvalidOperationException("生成的金蝶凭证记录为空");
                }

                // 生成DBF文件
                var fileName = $"Invoice_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
                
                // 使用 OwFileManager 创建文件记录 - 先生成文件内容到内存
                byte[] dbfFileContent;
                using (var memoryStream = new MemoryStream())
                {
                    DotNetDbfUtil.WriteToStream(kingdeeVouchers, memoryStream);
                    dbfFileContent = memoryStream.ToArray();
                }

                _Logger.LogInformation("任务 {TaskId} 成功生成DBF文件内容，大小：{FileSize} 字节", taskId, dbfFileContent.Length);

                // 更新发票导出时间
                foreach (var invoice in invoices)
                {
                    invoice.ExportedDateTime = exportDateTime;
                }

                // 使用 OwFileManager 创建文件记录 - 大大简化了文件创建逻辑
                var fileInfo = _FileManager.CreateFileFromBytes(
                    fileContent: dbfFileContent,
                    fileName: fileName,
                    displayName: $"发票导出-{DateTime.Now:yyyy年MM月dd日}",
                    parentId: taskId,
                    creatorId: userId,
                    remark: $"发票DBF导出文件，共{invoices.Count}张发票，{kingdeeVouchers.Count}条会计分录，导出时间：{exportDateTime:yyyy-MM-dd HH:mm:ss}",
                    subDirectory: "FinancialExports"
                );

                _DbContext.PlFileInfos.Add(fileInfo);
                _DbContext.SaveChanges();

                _Logger.LogInformation("任务 {TaskId} 成功完成，文件：{FileName}，发票数：{InvoiceCount}，凭证数：{VoucherCount}",
                    taskId, fileName, invoices.Count, kingdeeVouchers.Count);

                // 返回任务执行结果
                return new
                {
                    FileId = fileInfo.Id,
                    FileName = fileName,
                    InvoiceCount = invoices.Count,
                    VoucherCount = kingdeeVouchers.Count,
                    FilePath = fileInfo.FilePath,
                    ExportDateTime = exportDateTime.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "处理发票DBF导出任务 {TaskId} 时发生错误", taskId);
                throw; // 让OwTaskService处理异常和更新任务状态
            }
        }

        /// <summary>
        /// 加载科目配置（同步版本，用于任务处理）
        /// </summary>
        private Dictionary<string, SubjectConfiguration> LoadSubjectConfigurations(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "PBI_ACC_RECEIVABLE",   // 应收账款
                "PBI_SALES_REVENUE",    // 主营业务收入
                "PBI_TAX_PAYABLE",      // 应交税金
                "GEN_PREPARER",         // 制单人
                "GEN_VOUCHER_GROUP"     // 凭证类别字
            };

            var configs = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToList();

            return configs.ToDictionary(c => c.Code, c => c);
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
