using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLms.Data.Finance;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Text.Json;
using System.Runtime.ExceptionServices;
using OW.Data;
using DotNetDBF;
using SysIO = System.IO;
namespace PowerLmsWebApi.Controllers.Financial
{
    /// <summary>
    /// 财务系统接口导出功能控制器。
    /// 提供发票数据导出为金蝶DBF格式文件的功能。
    /// 严格按照SubjectConfiguration表中的科目配置进行凭证生成。
    /// </summary>
    public partial class FinancialSystemExportController : PlControllerBase
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
        /// <param name="orgManager">组织管理器，用于商户和机构相关操作</param>
        public FinancialSystemExportController(AccountManager accountManager, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory,
            PowerLmsUserDbContext dbContext, ILogger<FinancialSystemExportController> logger, OwFileService<PowerLmsUserDbContext> fileService,
            OrgManager<PowerLmsUserDbContext> orgManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _ServiceScopeFactory = serviceScopeFactory;
            _DbContext = dbContext;
            _Logger = logger;
            _FileService = fileService;
            _OrgManager = orgManager;
        }
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider; // 保留，用于当前请求作用域的服务解析
        readonly IServiceScopeFactory _ServiceScopeFactory; // 用于后台任务的独立作用域创建
        readonly PowerLmsUserDbContext _DbContext;
        readonly ILogger<FinancialSystemExportController> _Logger;
        readonly OwFileService<PowerLmsUserDbContext> _FileService;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        #region HTTP接口
        /// <summary>
        /// 统一的取消财务导出标记接口（基于导出时间范围）
        /// 支持取消已标记为导出状态的财务单据，使其可以重新导出
        /// 
        /// 支持的导出业务类型代码（exportTypeCode参数）：
        /// - INVOICE: 发票导出（TaxInvoiceInfo）
        /// - OA_EXPENSE: OA日常费用申请单导出（OaExpenseRequisition、OaExpenseRequisitionItem）
        /// - ARAB: A账应收本位币挂账导出（DocFee，收入类）
        /// - APAB: A账应付本位币挂账导出（DocFee，支出类）
        /// - SETTLEMENT_RECEIPT: 收款结算单导出（PlInvoices，IO=true）
        /// - SETTLEMENT_PAYMENT: 付款结算单导出（PlInvoices，IO=false）
        /// 
        /// 权限要求：F.6（财务接口权限）
        /// 业务规则：
        /// 1. 基于导出时间范围批量取消，避免传递大量实体ID
        /// 2. 取消后，ExportedDateTime和ExportedUserId字段将被清空
        /// 3. 支持额外的过滤条件（如限定导出用户、机构等）
        /// 4. 自动应用组织权限过滤，确保数据隔离
        /// 5. 设置安全上限（默认10000条），防止误操作
        /// 6. 可选的取消原因记录（reason参数）
        /// 
        /// 典型使用场景：
        /// - 取消本月所有导出：设置导出时间为本月1号到最后一天
        /// - 取消今天的导出：设置导出时间为今天0点到23:59:59
        /// - 取消上月导出：设置导出时间为上月1号到最后一天
        /// </summary>
        /// <param name="model">取消导出参数（必须包含导出时间范围）</param>
        /// <returns>取消结果，包含成功数量和详细信息</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误（如无效的exportTypeCode或导出时间范围无效）。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<CancelFinancialExportReturnDto> CancelFinancialExport(CancelFinancialExportParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new CancelFinancialExportReturnDto();
            try
            {
                // 1. 权限验证：使用财务接口权限
                var authorizationManager = _ServiceProvider.GetRequiredService<AuthorizationManager>();
                if (!authorizationManager.Demand(out var err, "F.6"))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = $"权限不足：{err}";
                    return result;
                }
                // 2. 参数验证
                if (string.IsNullOrWhiteSpace(model.ExportTypeCode))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "导出业务类型代码（ExportTypeCode）不能为空";
                    return result;
                }
                if (model.ExportedDateTimeStart == default || model.ExportedDateTimeEnd == default)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "导出时间范围（ExportedDateTimeStart和ExportedDateTimeEnd）不能为空";
                    return result;
                }
                if (model.ExportedDateTimeStart > model.ExportedDateTimeEnd)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "导出时间范围开始时间不能大于结束时间";
                    return result;
                }
                // 3. 根据导出业务类型代码调用对应的取消逻辑
                var exportManager = _ServiceProvider.GetRequiredService<FinancialSystemExportManager>();
                int successCount = 0;
                const int MAX_CANCEL_COUNT = 10000; // 安全上限
                switch (model.ExportTypeCode.ToUpperInvariant())
                {
                    case "INVOICE":
                        {
                            var query = _DbContext.TaxInvoiceInfos.AsQueryable();
                            // 应用组织权限过滤
                            query = ApplyOrganizationFilter(query, context.User);
                            successCount = CancelExportByDateRange(query, model, context.User, exportManager, MAX_CANCEL_COUNT, out var errorMsg);
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                result.HasError = true;
                                result.ErrorCode = 400;
                                result.DebugMessage = errorMsg;
                                return result;
                            }
                            break;
                        }
                    case "OA_EXPENSE":
                        {
                            var query = _DbContext.OaExpenseRequisitions.AsQueryable();
                            // 应用OA申请单的组织权限过滤
                            query = ApplyOrganizationFilterForOaExpense(query, context.User);
                            successCount = CancelExportByDateRange(query, model, context.User, exportManager, MAX_CANCEL_COUNT, out var errorMsg);
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                result.HasError = true;
                                result.ErrorCode = 400;
                                result.DebugMessage = errorMsg;
                                return result;
                            }
                            // 同时取消关联的子表记录（导出标记在主表，但子表也有ExportedDateTime字段）
                            // 注意：只处理成功取消的主表记录关联的子表
                            if (successCount > 0)
                            {
                                var canceledParentIds = exportManager.FilterExported(_DbContext.OaExpenseRequisitions.AsQueryable())
                                    .Where(e => e.ExportedDateTime >= model.ExportedDateTimeStart 
                                           && e.ExportedDateTime <= model.ExportedDateTimeEnd)
                                    .Select(e => e.Id)
                                    .ToList();
                                if (canceledParentIds.Any())
                                {
                                    var items = _DbContext.OaExpenseRequisitionItems
                                        .Where(item => canceledParentIds.Contains(item.ParentId.Value) 
                                               && item.ExportedDateTime.HasValue)
                                        .ToList();
                                    if (items.Any())
                                    {
                                        exportManager.UnmarkExported(items, context.User.Id);
                                    }
                                }
                            }
                            break;
                        }
                    case "ARAB":
                        {
                            var query = _DbContext.DocFees.Where(e => e.IO == true);
                            // 应用DocFee的组织权限过滤（通过Job关联）
                            query = ApplyOrganizationFilterForFees(query, context.User);
                            successCount = CancelExportByDateRange(query, model, context.User, exportManager, MAX_CANCEL_COUNT, out var errorMsg);
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                result.HasError = true;
                                result.ErrorCode = 400;
                                result.DebugMessage = errorMsg;
                                return result;
                            }
                            break;
                        }
                    case "APAB":
                        {
                            var query = _DbContext.DocFees.Where(e => e.IO == false);
                            // 应用DocFee的组织权限过滤（通过Job关联）
                            query = ApplyOrganizationFilterForFees(query, context.User);
                            successCount = CancelExportByDateRange(query, model, context.User, exportManager, MAX_CANCEL_COUNT, out var errorMsg);
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                result.HasError = true;
                                result.ErrorCode = 400;
                                result.DebugMessage = errorMsg;
                                return result;
                            }
                            break;
                        }
                    case "SETTLEMENT_RECEIPT":
                        {
                            var query = _DbContext.PlInvoicess.Where(e => e.IO == true);
                            // 应用收款结算单的组织权限过滤
                            query = ApplyOrganizationFilterForSettlementReceipts(query, context.User);
                            successCount = CancelExportByDateRange(query, model, context.User, exportManager, MAX_CANCEL_COUNT, out var errorMsg);
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                result.HasError = true;
                                result.ErrorCode = 400;
                                result.DebugMessage = errorMsg;
                                return result;
                            }
                            break;
                        }
                    case "SETTLEMENT_PAYMENT":
                        {
                            var query = _DbContext.PlInvoicess.Where(e => e.IO == false);
                            // 应用付款结算单的组织权限过滤
                            query = ApplyOrganizationFilterForSettlementPayments(query, context.User);
                            successCount = CancelExportByDateRange(query, model, context.User, exportManager, MAX_CANCEL_COUNT, out var errorMsg);
                            if (!string.IsNullOrEmpty(errorMsg))
                            {
                                result.HasError = true;
                                result.ErrorCode = 400;
                                result.DebugMessage = errorMsg;
                                return result;
                            }
                            break;
                        }
                    default:
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"不支持的导出业务类型代码：{model.ExportTypeCode}。" +
                            $"支持的类型：INVOICE, OA_EXPENSE, ARAB, APAB, SETTLEMENT_RECEIPT, SETTLEMENT_PAYMENT";
                        return result;
                }
                // 4. 保存更改
                _DbContext.SaveChanges();
                // 5. 返回结果
                result.SuccessCount = successCount;
                result.FailedCount = 0;
                result.Message = $"成功取消{successCount}条记录的导出标记";
                result.DebugMessage = !string.IsNullOrWhiteSpace(model.Reason)
                    ? $"取消原因：{model.Reason}，导出时间范围：{model.ExportedDateTimeStart:yyyy-MM-dd HH:mm:ss} ~ {model.ExportedDateTimeEnd:yyyy-MM-dd HH:mm:ss}"
                    : $"导出时间范围：{model.ExportedDateTimeStart:yyyy-MM-dd HH:mm:ss} ~ {model.ExportedDateTimeEnd:yyyy-MM-dd HH:mm:ss}";
                _Logger.LogInformation("取消财务导出标记完成，类型={ExportType}, 数量={Count}, 时间范围={Start}~{End}, 用户={UserId}",
                    model.ExportTypeCode, successCount, model.ExportedDateTimeStart, model.ExportedDateTimeEnd, context.User.Id);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "取消财务导出标记时发生错误，类型={ExportType}, 用户={UserId}",
                    model.ExportTypeCode, context.User.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"取消导出标记失败: {ex.Message}";
            }
            return result;
        }
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
                // 预检查发票数量 - 使用Manager方法过滤未导出数据
                var exportManager = _ServiceProvider.GetRequiredService<FinancialSystemExportManager>();
                var invoicesQuery = exportManager.FilterUnexported(_DbContext.TaxInvoiceInfos.AsQueryable());
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
                    ["ExportDateTime"] = exportDateTime.ToString("O"),
                    ["DisplayName"] = model.DisplayName ?? "",
                    ["Remark"] = model.Remark ?? ""
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
        #endregion HTTP接口
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
            try
            {
                _ = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider), "服务提供者不能为空");
                _ = parameters ?? throw new ArgumentNullException(nameof(parameters), "任务参数不能为空");
                currentStep = "解析服务依赖";
                var dbContextFactory = serviceProvider.GetService<IDbContextFactory<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("无法获取数据库上下文工厂 - 请检查服务注册");
                var fileService = serviceProvider.GetService<OwFileService<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("无法获取文件服务 - 请检查服务注册");
                currentStep = "解析任务参数";
                if (!parameters.TryGetValue("ExportConditions", out var exportConditionsJson))
                    throw new InvalidOperationException($"任务参数缺少 'ExportConditions'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");
                if (!parameters.TryGetValue("UserId", out var userIdStr))
                    throw new InvalidOperationException($"任务参数缺少 'UserId'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");
                if (!parameters.TryGetValue("ExportDateTime", out var exportDateTimeStr))
                    throw new InvalidOperationException($"任务参数缺少 'ExportDateTime'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");
                if (!parameters.TryGetValue("DisplayName", out var displayName))
                    throw new InvalidOperationException($"任务参数缺少 'DisplayName'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");
                if (!parameters.TryGetValue("Remark", out var remark))
                    throw new InvalidOperationException($"任务参数缺少 'Remark'。任务ID: {taskId}，现有参数: {string.Join(", ", parameters.Keys)}");
                currentStep = "解析参数值";
                Dictionary<string, string> conditions = null;
                if (!string.IsNullOrEmpty(exportConditionsJson))
                    conditions = JsonSerializer.Deserialize<Dictionary<string, string>>(exportConditionsJson);
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
                // 解析文件显示名称和备注参数
                displayName = parameters.GetValueOrDefault("DisplayName", "");
                remark = parameters.GetValueOrDefault("Remark", "");
                currentStep = "创建数据库上下文";
                using var dbContext = dbContextFactory.CreateDbContext() ??
                    throw new InvalidOperationException("创建数据库上下文失败");
                currentStep = "加载科目配置";
                var subjectConfigs = LoadSubjectConfigurations(dbContext, orgId) ??
                    throw new InvalidOperationException("LoadSubjectConfigurations 返回 null");
                if (!subjectConfigs.Any())
                    throw new InvalidOperationException($"科目配置未找到，无法生成凭证。组织ID: {orgId}，任务ID: {taskId}");
                currentStep = "构建发票查询";
                IQueryable<TaxInvoiceInfo> invoicesQuery = dbContext.TaxInvoiceInfos ?? throw new InvalidOperationException("无法访问TaxInvoiceInfos数据集 - 请检查数据库连接");
                currentStep = "获取导出管理器并过滤未导出数据";
                var exportManager = serviceProvider.GetRequiredService<FinancialSystemExportManager>();
                invoicesQuery = exportManager.FilterUnexported(invoicesQuery);
                if (conditions != null && conditions.Any())
                    invoicesQuery = EfHelper.GenerateWhereAnd(invoicesQuery, conditions) ??
                        throw new InvalidOperationException("EfHelper.GenerateWhereAnd 返回 null");
                currentStep = "应用权限过滤";
                var taskUser = dbContext.Accounts?.Find(userId) ??
                    throw new InvalidOperationException($"未找到用户 {userId}，无法应用权限过滤");
                invoicesQuery = ApplyOrganizationFilterStatic(invoicesQuery, taskUser, dbContext, serviceProvider) ??
                    throw new InvalidOperationException("ApplyOrganizationFilterStatic 返回 null");
                currentStep = "查询发票数据";
                var invoices = invoicesQuery.ToList() ??
                    throw new InvalidOperationException("发票查询返回 null");
                if (!invoices.Any())
                    throw new InvalidOperationException($"没有找到符合条件的发票数据。任务ID: {taskId}，预期数量: {expectedCount}，实际数量: 0");
                currentStep = "查询发票明细";
                var invoiceIds = invoices.Select(i => i.Id).ToList();
                var invoiceItems = dbContext.TaxInvoiceInfoItems
                    ?.Where(item => invoiceIds.Contains(item.ParentId.Value))
                    ?.ToList() ?? new List<TaxInvoiceInfoItem>();
                var invoiceItemsDict = invoiceItems.GroupBy(item => item.ParentId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());
                currentStep = "转换为金蝶凭证格式";
                var kingdeeVouchers = ConvertInvoicesToKingdeeVouchersWithConfig(invoices, invoiceItemsDict, subjectConfigs, dbContext) ??
                    throw new InvalidOperationException("ConvertInvoicesToKingdeeVouchersWithConfig 返回 null");
                if (!kingdeeVouchers.Any())
                    throw new InvalidOperationException($"生成的金蝶凭证记录为空。任务ID: {taskId}，发票数量: {invoices.Count}");
                currentStep = "生成DBF文件";
                var fileName = $"Invoice_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
                var kingdeeFieldMappings = new Dictionary<string, string>
                {
                    {"FDATE", "FDATE"}, {"FTRANSDATE", "FTRANSDATE"}, {"FPERIOD", "FPERIOD"}, {"FGROUP", "FGROUP"}, {"FNUM", "FNUM"},
                    {"FENTRYID", "FENTRYID"}, {"FEXP", "FEXP"}, {"FACCTID", "FACCTID"}, {"FCLSNAME1", "FCLSNAME1"}, {"FOBJID1", "FOBJID1"},
                    {"FOBJNAME1", "FOBJNAME1"}, {"FTRANSID", "FTRANSID"}, {"FCYID", "FCYID"}, {"FEXCHRATE", "FEXCHRATE"}, {"FDC", "FDC"},
                    {"FFCYAMT", "FFCYAMT"}, {"FDEBIT", "FDEBIT"}, {"FCREDIT", "FCREDIT"}, {"FPREPARE", "FPREPARE"}, {"FMODULE", "FMODULE"}, {"FDELETED", "FDELETED"}
                };
                var customFieldTypes = new Dictionary<string, NativeDbType>
                {
                    {"FDATE", NativeDbType.Date}, {"FTRANSDATE", NativeDbType.Date}, {"FPERIOD", NativeDbType.Numeric}, {"FGROUP", NativeDbType.Char},
                    {"FNUM", NativeDbType.Numeric}, {"FENTRYID", NativeDbType.Numeric}, {"FEXP", NativeDbType.Char}, {"FACCTID", NativeDbType.Char},
                    {"FCLSNAME1", NativeDbType.Char}, {"FOBJID1", NativeDbType.Char}, {"FOBJNAME1", NativeDbType.Char}, {"FTRANSID", NativeDbType.Char},
                    {"FCYID", NativeDbType.Char}, {"FEXCHRATE", NativeDbType.Numeric}, {"FDC", NativeDbType.Numeric}, {"FFCYAMT", NativeDbType.Numeric},
                    {"FDEBIT", NativeDbType.Numeric}, {"FCREDIT", NativeDbType.Numeric}, {"FPREPARE", NativeDbType.Char}, {"FMODULE", NativeDbType.Char}, {"FDELETED", NativeDbType.Logical}
                };
                currentStep = "创建文件记录";
                PlFileInfo fileInfoRecord;
                long fileSize;
                var memoryStream = new MemoryStream(1024 * 1024 * 1024); // 使用大型内存流避免临时文件，提高性能
                try
                {
                    DotNetDbfUtil.WriteToStream(kingdeeVouchers, memoryStream, kingdeeFieldMappings, customFieldTypes);
                    fileSize = memoryStream.Length;
                    if (fileSize == 0)
                        throw new InvalidOperationException($"DBF文件生成失败，文件为空");
                    memoryStream.Position = 0; // 重置流位置以便读取
                    // 构建最终的显示名称和备注
                    var finalDisplayName = !string.IsNullOrWhiteSpace(displayName) ?
                        displayName : $"发票导出-{DateTime.Now:yyyy年MM月dd日}";
                    var finalRemark = !string.IsNullOrWhiteSpace(remark) ?
                        remark : $"发票DBF导出文件，共{invoices.Count}张发票，{kingdeeVouchers.Count}条会计分录，导出时间：{exportDateTime:yyyy-MM-dd HH:mm:ss}";
                    fileInfoRecord = fileService.CreateFile(
                        fileStream: memoryStream,
                        fileName: fileName,
                        displayName: finalDisplayName,
                        parentId: taskId,
                        creatorId: userId,
                        remark: finalRemark,
                        subDirectory: "FinancialExports",
                        skipValidation: true // DBF文件特殊，跳过文件大小和扩展名验证
                    );
                }
                finally
                {
                    OwHelper.DisposeAndRelease(ref memoryStream);
                }
                _ = fileInfoRecord ?? throw new InvalidOperationException("fileService.CreateFile 返回 null");
                currentStep = "标记发票为已导出";
                var markedCount = exportManager.MarkAsExported(invoices, userId);
                dbContext.SaveChanges(); // 保存导出标记
                currentStep = "验证最终文件并返回结果";
                long actualFileSize = 0;
                bool fileExists = false;
                try
                {
                    if (SysIO.File.Exists(fileInfoRecord.FilePath))
                    {
                        actualFileSize = new SysIO.FileInfo(fileInfoRecord.FilePath).Length;
                        fileExists = true;
                    }
                }
                catch { } // 忽略验证时的异常，不影响主要功能
                return new
                {
                    FileId = fileInfoRecord.Id,
                    FileName = fileName,
                    InvoiceCount = invoices.Count,
                    VoucherCount = kingdeeVouchers.Count,
                    FilePath = fileInfoRecord.FilePath,
                    ExportDateTime = exportDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileSize = actualFileSize,
                    FileExists = fileExists,
                    OriginalFileSize = fileSize,
                    MarkedCount = markedCount
                };
            }
            catch (Exception ex)
            {
                var contextualError = $"发票DBF导出任务失败，当前步骤: {currentStep}, 任务ID: {taskId}";
                if (parameters != null)
                    contextualError += $"\n任务参数: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}";
                if (ex is InvalidOperationException || ex is ArgumentException || ex is JsonException)
                    throw new InvalidOperationException(contextualError, ex); // 对于已知的业务异常，添加上下文信息但保留原始异常
                else
                {
                    ExceptionDispatchInfo.Capture(ex).Throw(); // 使用全局using解决命名空间冲突
                    throw; // 这行永远不会执行，但编译器需要
                }
            }
        }
        #endregion 静态任务处理方法
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
            Dictionary<string, SubjectConfiguration> subjectConfigs,
            PowerLmsUserDbContext dbContext)
        {
            _ = invoices ?? throw new ArgumentNullException(nameof(invoices));
            _ = subjectConfigs ?? throw new ArgumentNullException(nameof(subjectConfigs));
            _ = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
                    // 根据需求：客户财务编码应该是 发票.申请单号→申请单结算单位id→客户资料.TacCountNo
                    // 摘要取开票项目第一个Goodsname
                    string customerFinancialCode = "";
                    string customerName = invoice.BuyerTitle ?? "未知客户";
                    string summary = "";
                    // 查询申请单信息
                    if (invoice.DocFeeRequisitionId.HasValue)
                    {
                        var requisition = dbContext.DocFeeRequisitions
                            .FirstOrDefault(r => r.Id == invoice.DocFeeRequisitionId.Value);
                        if (requisition?.BalanceId.HasValue == true)
                        {
                            // 根据申请单的结算单位ID查询客户资料的财务编码
                            var customer = dbContext.PlCustomers
                                .FirstOrDefault(c => c.Id == requisition.BalanceId.Value);
                            if (customer != null)
                            {
                                customerFinancialCode = customer.TacCountNo ?? "";
                                customerName = customer.Name_DisplayName ?? customer.Name_Name ?? "未知客户";
                            }
                        }
                    }
                    // 获取开票项目第一个GoodsName作为摘要
                    var firstItem = items.FirstOrDefault();
                    if (firstItem != null)
                    {
                        summary = firstItem.GoodsName ?? "未知项目";
                    }
                    else
                    {
                        summary = invoice.InvoiceItemName ?? "未知项目";
                    }
                    // 安全地处理字符串，确保不会超过DBF字段限制
                    customerName = customerName.Length > 200 ? customerName[..200] : customerName;
                    summary = summary.Length > 200 ? summary[..200] : summary;
                    customerFinancialCode = customerFinancialCode.Length > 50 ? customerFinancialCode[..50] : customerFinancialCode;
                    // 构建完整描述：客户名+摘要+客户财务编码
                    var description = $"{customerName}*{summary}*{customerFinancialCode}";
                    if (description.Length > 500)
                    {
                        description = description[..500];
                    }
                    var customerCode = string.IsNullOrEmpty(customerFinancialCode) ?
                        "CUSTOMER" : customerFinancialCode;
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
                            FOBJNAME1 = customerName,
                            FTRANSID = customerFinancialCode,
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
                            FOBJNAME1 = customerName,
                            FTRANSID = customerFinancialCode,
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
                            FOBJNAME1 = customerName,
                            FTRANSID = customerFinancialCode,
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
            var orgManager = serviceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>();
            // 获取用户关联的所有组织机构ID
            var userOrgIds = dbContext.AccountPlOrganizations
                .Where(apo => apo.UserId == user.Id)
                .Select(apo => apo.OrgId)
                .ToList();
            if (!userOrgIds.Any())
            {
                return invoicesQuery.Where(i => false);
            }
            // 获取用户所属商户ID
            var merchantId = orgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue)
            {
                return invoicesQuery.Where(i => false);
            }
            HashSet<Guid?> allowedOrgIds;
            if (user.IsMerchantAdmin)
            {
                // 商户管理员可以访问整个商户下的所有组织机构
                var allOrgIds = orgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                allowedOrgIds = new HashSet<Guid?>(allOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value); // 添加商户ID本身
            }
            else
            {
                // 普通用户只能访问其当前登录的公司及下属机构
                var companyId = user.OrgId.HasValue ? orgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue)
                {
                    return invoicesQuery.Where(i => false);
                }
                var companyOrgIds = orgManager.GetOrgIdsByCompanyId(companyId.Value).ToList();
                allowedOrgIds = new HashSet<Guid?>(companyOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value); // 添加商户ID本身
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
        #endregion 静态辅助方法
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
                "PBI_TAX_PAYABLE"      // 应交税金
            };
            var existingCodes = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();
            return requiredCodes.Except(existingCodes).ToList();
        }
        /// <summary>
        /// 获取用户有权访问的组织机构ID集合（通用辅助方法）
        /// </summary>
        /// <param name="user">当前用户</param>
        /// <returns>组织机构ID集合</returns>
        private HashSet<Guid> GetOrgIdsForCurrentUser(Account user)
        {
            if (user.IsSuperAdmin)
            {
                return null; // null表示不需要过滤（超管可访问所有数据）
            }
            var merchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue)
            {
                return new HashSet<Guid>(); // 空集合表示无权访问任何数据
            }
            if (user.IsMerchantAdmin)
            {
                // 商户管理员：整个商户下的所有组织机构
                var allOrgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToHashSet();
                allOrgIds.Add(merchantId.Value); // 添加商户ID本身
                return allOrgIds;
            }
            else
            {
                // 普通用户：当前登录的公司及下属所有非公司机构
                var companyId = user.OrgId.HasValue ? _OrgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue)
                {
                    return new HashSet<Guid>(); // 空集合表示无权访问任何数据
                }
                var companyOrgIds = _OrgManager.GetOrgIdsByCompanyId(companyId.Value).ToHashSet();
                companyOrgIds.Add(merchantId.Value); // 添加商户ID本身
                return companyOrgIds;
            }
        }
        /// <summary>
        /// 根据用户权限过滤OA费用申请单查询。
        /// OA费用申请单有直接的OrgId字段，过滤逻辑相对简单。
        /// </summary>
        /// <param name="query">OA费用申请单查询对象</param>
        /// <param name="user">当前用户</param>
        /// <returns>过滤后的查询对象</returns>
        private IQueryable<OaExpenseRequisition> ApplyOrganizationFilterForOaExpense(IQueryable<OaExpenseRequisition> query, Account user)
        {
            if (user.IsSuperAdmin)
            {
                return query; // 超管可访问所有数据
            }
            var allowedOrgIds = GetOrgIdsForCurrentUser(user);
            if (allowedOrgIds == null || allowedOrgIds.Count == 0)
            {
                return query.Where(r => false); // 无权访问任何数据
            }
            return query.Where(r => r.OrgId.HasValue && allowedOrgIds.Contains(r.OrgId.Value));
        }
        /// <summary>
        /// 根据用户权限过滤DocFee查询（实例方法版本）。
        /// DocFee没有直接的OrgId字段，需要通过PlJob.OrgId关联过滤。
        /// </summary>
        /// <param name="query">DocFee查询对象</param>
        /// <param name="user">当前用户</param>
        /// <returns>过滤后的查询对象</returns>
        private IQueryable<DocFee> ApplyOrganizationFilterForFees(IQueryable<DocFee> query, Account user)
        {
            if (user.IsSuperAdmin)
            {
                return query; // 超管可访问所有数据
            }
            var allowedOrgIds = GetOrgIdsForCurrentUser(user);
            if (allowedOrgIds == null || allowedOrgIds.Count == 0)
            {
                return query.Where(f => false); // 无权访问任何数据
            }
            // DocFee通过Job.OrgId关联组织机构
            var allowedJobIds = _DbContext.PlJobs
                .Where(j => j.OrgId.HasValue && allowedOrgIds.Contains(j.OrgId.Value))
                .Select(j => j.Id)
                .ToList();
            return query.Where(f => f.JobId.HasValue && allowedJobIds.Contains(f.JobId.Value));
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
            var merchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue)
            {
                return invoicesQuery.Where(i => false);
            }
            if (user.IsMerchantAdmin)
            {
                // 商户管理员可以访问整个商户下的所有组织机构
                var merchantOrgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys
                    .Select(id => (Guid?)id).ToHashSet();
                merchantOrgIds.Add(merchantId.Value);
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
                // 普通用户只能访问其当前登录的公司及下属机构
                var companyId = user.OrgId.HasValue ? _OrgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue)
                {
                    return invoicesQuery.Where(i => false);
                }
                var userOrgIds = _OrgManager.GetOrgIdsByCompanyId(companyId.Value)
                    .Select(id => (Guid?)id).ToHashSet();
                userOrgIds.Add(merchantId.Value);
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
        #endregion 实例方法
        #region 共享辅助方法
        /// <summary>
        /// 获取有效的科目代码（支持可变参数回退）
        /// </summary>
        private static string GetValidSubjectCode(params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && candidate.Trim().Length > 0)
                {
                    return candidate.Trim();
                }
            }
            throw new InvalidOperationException("无法获取有效的科目代码，请检查科目配置");
        }
        /// <summary>
        /// 验证凭证数据完整性
        /// </summary>
        private static void ValidateVoucherDataIntegrity(List<KingdeeVoucher> vouchers)
        {
            var errors = new List<string>();
            var voucherGroups = vouchers.GroupBy(v => v.FNUM);
            foreach (var group in voucherGroups)
            {
                var totalDebit = group.Sum(v => v.FDEBIT ?? 0);
                var totalCredit = group.Sum(v => v.FCREDIT ?? 0);
                if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                {
                    errors.Add($"凭证号 {group.Key} 借贷不平衡，借方: {totalDebit}, 贷方: {totalCredit}");
                }
            }
            foreach (var voucher in vouchers)
            {
                if (string.IsNullOrEmpty(voucher.FACCTID))
                    errors.Add($"凭证 {voucher.FNUM}-{voucher.FENTRYID} 缺少科目代码");
                if (string.IsNullOrEmpty(voucher.FEXP))
                    errors.Add($"凭证 {voucher.FNUM}-{voucher.FENTRYID} 缺少摘要");
            }
            if (errors.Any())
            {
                throw new InvalidOperationException($"凭证数据验证失败：{string.Join("; ", errors)}");
            }
        }
        #endregion 共享辅助方法
        #region 取消导出辅助方法
        /// <summary>
        /// 基于导出时间范围取消导出标记（通用方法）
        /// 注意：组织权限过滤必须由调用方在传入baseQuery之前完成
        /// 因为不同实体的组织归属是通过不同的关联查询确定的，无法在泛型方法中统一处理
        /// </summary>
        /// <typeparam name="T">实体类型（只需实现IFinancialExportable）</typeparam>
        /// <param name="baseQuery">基础查询（调用方必须已应用组织权限过滤）</param>
        /// <param name="model">取消参数</param>
        /// <param name="user">当前用户（用于日志记录）</param>
        /// <param name="exportManager">导出管理器</param>
        /// <param name="maxCount">最大允许取消数量</param>
        /// <param name="errorMessage">错误信息（如果有）</param>
        /// <returns>成功取消的数量</returns>
        private int CancelExportByDateRange<T>(
            IQueryable<T> baseQuery,
            CancelFinancialExportParamsDto model,
            Account user,
            FinancialSystemExportManager exportManager,
            int maxCount,
            out string errorMessage) where T : class, IFinancialExportable
        {
            errorMessage = null;
            // 1. 过滤已导出的数据
            var query = exportManager.FilterExported(baseQuery);
            // 2. 应用导出时间范围过滤
            query = query.Where(e => e.ExportedDateTime >= model.ExportedDateTimeStart 
                                  && e.ExportedDateTime <= model.ExportedDateTimeEnd);
            // 3. 应用额外的过滤条件（如果有）
            if (model.AdditionalConditions != null && model.AdditionalConditions.Any())
            {
                query = EfHelper.GenerateWhereAnd(query, model.AdditionalConditions);
                if (query == null)
                {
                    errorMessage = "额外过滤条件格式错误";
                    return 0;
                }
            }
            // 4. 检查数量上限
            var count = query.Count();
            if (count > maxCount)
            {
                errorMessage = $"匹配到{count}条记录，超过安全上限({maxCount})，请缩小时间范围或添加过滤条件";
                return 0;
            }
            if (count == 0)
            {
                return 0;
            }
            // 5. 执行取消操作
            var entities = query.ToList();
            var unmarkedCount = exportManager.UnmarkExported(entities, user.Id);
            return unmarkedCount;
        }
        #endregion 取消导出辅助方法
    }
}
