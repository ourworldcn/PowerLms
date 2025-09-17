/*
 * 项目：PowerLms财务系统 | 模块：付款结算单导出金蝶功能
 * 功能：将付款结算单转换为符合金蝶财务软件要求的会计凭证分录
 * 技术要点：六种凭证分录规则、多币种处理、混合业务识别、多笔付款优先处理、手续费双分录
 * 作者：zc | 创建：2025-01 | 修改：2025-01-31 付款结算单导出功能实施
 */

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLms.Data.Finance;
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
    /// 财务系统导出控制器 - 付款结算单导出金蝶功能模块
    /// 实现付款结算单的财务导出功能，支持：
    /// - 六种凭证分录规则的完整实现
    /// - 多币种和汇率处理
    /// - 混合业务识别（既有收入又有支出的结算单）
    /// - 多笔付款优先处理逻辑
    /// - 手续费双分录自平衡机制
    /// - 权限控制和数据隔离
    /// - 异步任务处理机制
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region HTTP接口 - 付款结算单导出

        /// <summary>
        /// 导出付款结算单为金蝶DBF格式文件
        /// 支持六种凭证分录规则的完整实现，处理复杂的多币种和混合业务场景
        /// </summary>
        /// <param name="model">导出参数，包含查询条件和用户令牌</param>
        /// <returns>导出任务信息，包含任务ID用于跟踪进度</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<ExportSettlementPaymentReturnDto> ExportSettlementPayment(ExportSettlementPaymentParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ExportSettlementPaymentReturnDto();
            try
            {
                _Logger.LogInformation("开始处理付款结算单导出请求，用户: {UserId}, 组织: {OrgId}", 
                    context.User.Id, context.User.OrgId);

                // 1. 权限验证：复用收款的F.6财务接口权限
                var authorizationManager = _ServiceProvider.GetRequiredService<AuthorizationManager>();
                if (!authorizationManager.Demand(out var err, "F.6"))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = $"权限不足：{err}";
                    return result;
                }

                // 2. 预检查科目配置完整性
                var missingConfigs = ValidateSettlementPaymentSubjectConfiguration(context.User.OrgId);
                if (missingConfigs.Any())
                {
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = $"科目配置不完整，缺少以下配置：{string.Join(", ", missingConfigs)}";
                    return result;
                }

                // 3. 构建查询条件 - 只查询付款结算单（IO=false）且未导出的
                var conditions = model.ExportConditions ?? new Dictionary<string, string>();
                
                // 强制限制为付款结算单且未导出
                conditions["IO"] = "false";
                if (!conditions.ContainsKey("ConfirmDateTime"))
                {
                    conditions["ConfirmDateTime"] = "null"; // 只导出未确认（未导出）的结算单
                }

                // 4. 预检查付款结算单数量
                var settlementPaymentsQuery = _DbContext.PlInvoicess.AsQueryable();
                
                // 应用查询条件
                if (conditions.Any())
                {
                    settlementPaymentsQuery = EfHelper.GenerateWhereAnd(settlementPaymentsQuery, conditions);
                }
                
                // 应用组织权限过滤
                settlementPaymentsQuery = ApplyOrganizationFilterForSettlementPayments(settlementPaymentsQuery, context.User);

                var settlementPaymentCount = settlementPaymentsQuery.Count();
                if (settlementPaymentCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "没有找到符合条件的付款结算单数据，请调整查询条件";
                    return result;
                }

                // 5. 预估凭证分录数量（基于六种分录规则，至少2个必须分录，最多8个含手续费）
                var estimatedVoucherEntryCount = settlementPaymentCount * 4; // 平均每个结算单4个分录

                // 6. 创建异步导出任务
                var taskService = _ServiceProvider.GetRequiredService<OwTaskService<PowerLmsUserDbContext>>();
                var exportDateTime = DateTime.UtcNow;

                var taskParameters = new Dictionary<string, string>
                {
                    ["ExportConditions"] = JsonSerializer.Serialize(conditions),
                    ["UserId"] = context.User.Id.ToString(),
                    ["OrgId"] = context.User.OrgId?.ToString() ?? "",
                    ["ExpectedSettlementPaymentCount"] = settlementPaymentCount.ToString(),
                    ["ExpectedVoucherEntryCount"] = estimatedVoucherEntryCount.ToString(),
                    ["ExportDateTime"] = exportDateTime.ToString("O"),
                    ["DisplayName"] = model.DisplayName ?? "",
                    ["Remark"] = model.Remark ?? ""
                };

                var taskId = taskService.CreateTask(typeof(FinancialSystemExportController),
                    nameof(ProcessSettlementPaymentDbfExportTask),
                    taskParameters,
                    context.User.Id,
                    context.User.OrgId);

                // 7. 返回成功结果
                result.TaskId = taskId;
                result.Message = $"付款结算单导出任务已创建成功";
                result.DebugMessage = $"导出任务已创建，预计处理 {settlementPaymentCount} 个付款结算单，生成 {estimatedVoucherEntryCount} 条凭证分录。可通过系统任务状态查询接口跟踪进度。";
                result.ExpectedSettlementPaymentCount = settlementPaymentCount;
                result.ExpectedVoucherEntryCount = estimatedVoucherEntryCount;

                _Logger.LogInformation("付款结算单导出任务创建成功，任务ID: {TaskId}, 结算单数量: {PaymentCount}", 
                    taskId, settlementPaymentCount);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "处理付款结算单导出请求时发生错误，用户: {UserId}", context.User.Id);
                
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"导出请求处理失败: {ex.Message}";
            }
            
            return result;
        }

        #endregion

        #region 静态任务处理方法 - 付款结算单导出

        /// <summary>
        /// 处理付款结算单DBF导出任务（静态方法，由OwTaskService调用）
        /// 实现六种凭证分录规则的完整业务逻辑
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="parameters">任务参数</param>
        /// <param name="serviceProvider">服务提供者（由OwTaskService自动注入）</param>
        /// <returns>任务执行结果</returns>
        public static object ProcessSettlementPaymentDbfExportTask(Guid taskId, Dictionary<string, string> parameters, IServiceProvider serviceProvider)
        {
            string currentStep = "参数验证";
            try
            {
                if (serviceProvider == null)
                    throw new ArgumentNullException(nameof(serviceProvider), "服务提供者不能为空");
                if (parameters == null)
                    throw new ArgumentNullException(nameof(parameters), "任务参数不能为空");

                currentStep = "解析服务依赖";
                var dbContextFactory = serviceProvider.GetService<IDbContextFactory<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("无法获取数据库上下文工厂 - 请检查服务注册");
                var fileService = serviceProvider.GetService<OwFileService<PowerLmsUserDbContext>>() ??
                    throw new InvalidOperationException("无法获取文件服务 - 请检查服务注册");
                var businessLogicManager = serviceProvider.GetService<BusinessLogicManager>() ??
                    throw new InvalidOperationException("无法获取业务逻辑管理器 - 请检查服务注册");

                currentStep = "解析任务参数";
                if (!parameters.TryGetValue("ExportConditions", out var exportConditionsJson))
                    throw new InvalidOperationException($"任务参数缺少 'ExportConditions'。任务ID: {taskId}");
                if (!parameters.TryGetValue("UserId", out var userIdStr))
                    throw new InvalidOperationException($"任务参数缺少 'UserId'。任务ID: {taskId}");
                if (!parameters.TryGetValue("ExportDateTime", out var exportDateTimeStr))
                    throw new InvalidOperationException($"任务参数缺少 'ExportDateTime'。任务ID: {taskId}");

                var conditions = !string.IsNullOrEmpty(exportConditionsJson) ? 
                    JsonSerializer.Deserialize<Dictionary<string, string>>(exportConditionsJson) : 
                    new Dictionary<string, string>();
                
                if (!Guid.TryParse(userIdStr, out var userId))
                    throw new InvalidOperationException($"无效的用户ID格式: {userIdStr}");
                
                Guid? orgId = null;
                if (parameters.TryGetValue("OrgId", out var orgIdStr) && !string.IsNullOrEmpty(orgIdStr))
                {
                    if (Guid.TryParse(orgIdStr, out var parsedOrgId))
                        orgId = parsedOrgId;
                }

                if (!DateTime.TryParse(exportDateTimeStr, out var exportDateTime))
                    throw new InvalidOperationException($"无效的导出时间格式: {exportDateTimeStr}");

                var displayName = parameters.GetValueOrDefault("DisplayName", "");
                var remark = parameters.GetValueOrDefault("Remark", "");

                currentStep = "创建数据库上下文";
                using var dbContext = dbContextFactory.CreateDbContext();

                currentStep = "验证科目配置";
                var subjectConfigs = LoadSettlementPaymentSubjectConfigurations(dbContext, orgId);
                if (!subjectConfigs.Any())
                {
                    throw new InvalidOperationException("付款结算单科目配置不完整，无法生成凭证");
                }

                currentStep = "查询用户信息";
                var taskUser = dbContext.Accounts.Find(userId) ??
                    throw new InvalidOperationException($"未找到用户 {userId}");

                currentStep = "构建付款结算单查询";
                var settlementPaymentsQuery = dbContext.PlInvoicess.AsQueryable();
                
                if (conditions.Any())
                {
                    settlementPaymentsQuery = EfHelper.GenerateWhereAnd(settlementPaymentsQuery, conditions);
                }
                
                // 应用组织权限过滤
                settlementPaymentsQuery = ApplyOrganizationFilterForSettlementPaymentsStatic(settlementPaymentsQuery, taskUser, dbContext, serviceProvider);

                currentStep = "查询付款结算单数据";
                var settlementPayments = settlementPaymentsQuery.ToList();
                if (!settlementPayments.Any())
                {
                    throw new InvalidOperationException("没有找到符合条件的付款结算单数据");
                }

                currentStep = "查询付款结算单明细";
                var settlementPaymentIds = settlementPayments.Select(r => r.Id).ToList();
                var items = dbContext.PlInvoicesItems
                    .Where(item => settlementPaymentIds.Contains(item.ParentId.Value))
                    .ToList();

                if (!items.Any())
                {
                    throw new InvalidOperationException("没有找到付款结算单明细项");
                }

                // 按付款结算单ID分组明细项
                var itemsDict = items.GroupBy(item => item.ParentId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                currentStep = "计算付款结算单业务数据";
                var calculationResults = CalculateSettlementPaymentData(settlementPayments, itemsDict, dbContext, businessLogicManager);

                currentStep = "生成金蝶凭证分录";
                var allVouchers = GenerateSettlementPaymentVouchersStatic(calculationResults, subjectConfigs, dbContext);

                if (!allVouchers.Any())
                {
                    throw new InvalidOperationException("没有生成任何凭证记录");
                }

                currentStep = "验证凭证数据完整性";
                ValidateVoucherDataIntegrity(allVouchers);

                currentStep = "生成DBF文件";
                var fileName = $"SettlementPayment_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";
                
                var kingdeeFieldMappings = GetSettlementPaymentKingdeeFieldMappings();
                var customFieldTypes = GetSettlementPaymentKingdeeFieldTypes();

                PlFileInfo fileInfoRecord;
                long fileSize;
                var memoryStream = new MemoryStream(1024 * 1024 * 1024); // 预分配1GB内存流
                try
                {
                    DotNetDbfUtil.WriteToStream(allVouchers, memoryStream, kingdeeFieldMappings, customFieldTypes);
                    fileSize = memoryStream.Length;
                    if (fileSize == 0)
                        throw new InvalidOperationException($"DBF文件生成失败，文件为空");
                    memoryStream.Position = 0;

                    // 构建最终的显示名称和备注
                    var finalDisplayName = !string.IsNullOrWhiteSpace(displayName) ?
                        displayName : $"付款结算单导出-{DateTime.Now:yyyy年MM月dd日}";
                    var finalRemark = !string.IsNullOrWhiteSpace(remark) ?
                        remark : $"付款结算单DBF导出文件，共{settlementPayments.Count}个付款结算单，{allVouchers.Count}条会计分录，导出时间：{exportDateTime:yyyy-MM-dd HH:mm:ss}";

                    fileInfoRecord = fileService.CreateFile(
                        fileStream: memoryStream,
                        fileName: fileName,
                        displayName: finalDisplayName,
                        parentId: taskId,
                        creatorId: userId,
                        remark: finalRemark,
                        subDirectory: "FinancialExports",
                        skipValidation: true
                    );
                }
                finally
                {
                    OwHelper.DisposeAndRelease(ref memoryStream);
                }

                if (fileInfoRecord == null)
                    throw new InvalidOperationException("文件保存失败");

                currentStep = "更新导出状态";
                // 标记付款结算单为已导出
                var now = DateTime.UtcNow;
                foreach (var payment in settlementPayments)
                {
                    payment.ConfirmDateTime = now;
                }
                dbContext.SaveChanges();

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
                catch { } // 忽略验证异常

                return new
                {
                    FileId = fileInfoRecord.Id,
                    FileName = fileName,
                    SettlementPaymentCount = settlementPayments.Count,
                    ItemCount = items.Count,
                    VoucherCount = allVouchers.Count,
                    FilePath = fileInfoRecord.FilePath,
                    ExportDateTime = exportDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileSize = actualFileSize,
                    FileExists = fileExists,
                    OriginalFileSize = fileSize
                };
            }
            catch (Exception ex)
            {
                var contextualError = $"付款结算单DBF导出任务失败，当前步骤: {currentStep}, 任务ID: {taskId}";
                if (parameters != null)
                    contextualError += $"\n任务参数: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}";

                if (ex is InvalidOperationException || ex is ArgumentException || ex is JsonException)
                    throw new InvalidOperationException(contextualError, ex);
                else
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                    throw; // 这行永远不会执行，但编译器需要
                }
            }
        }

        #endregion

        #region 付款结算单核心业务逻辑

        /// <summary>
        /// 计算付款结算单业务数据
        /// 包括应付应收金额计算、混合业务识别、多笔付款检测、本位币转换等
        /// </summary>
        private static List<SettlementPaymentCalculationDto> CalculateSettlementPaymentData(
            List<PlInvoices> settlementPayments,
            Dictionary<Guid, List<PlInvoicesItem>> itemsDict,
            PowerLmsUserDbContext dbContext,
            BusinessLogicManager businessLogicManager)
        {
            var results = new List<SettlementPaymentCalculationDto>();

            foreach (var payment in settlementPayments)
            {
                var items = itemsDict.GetValueOrDefault(payment.Id, new List<PlInvoicesItem>());
                
                // 获取本位币代码
                var baseCurrency = businessLogicManager.GetBaseCurrencyCode(payment, dbContext) ?? "CNY";
                
                // 获取往来单位信息（供应商）
                var supplier = dbContext.PlCustomers.Find(payment.JiesuanDanweiId);
                var supplierName = supplier?.Name_Name ?? "未知供应商";
                var supplierFinanceCode = supplier?.FinanceCodeAP ?? "";

                // 获取银行信息
                var bankInfo = dbContext.BankInfos.Find(payment.BankId);

                // 计算应付应收金额和识别混合业务，使用正确的汇率计算
                var (payableTotal, receivableTotal, isMixed, itemCalculations) = CalculatePaymentAmountsAndIdentifyMixedBusiness(items, dbContext, payment.PaymentExchangeRate ?? 1.0m);

                // 检测多笔付款：如果明细项数量大于1，则认为是多笔付款
                var hasMultiplePayments = items.Count > 1;

                var calculation = new SettlementPaymentCalculationDto
                {
                    SettlementPaymentId = payment.Id,
                    SupplierName = supplierName,
                    SupplierFinanceCode = supplierFinanceCode,
                    PaymentNumber = payment.IoPingzhengNo ?? $"SP{payment.Id.ToString("N")[..8]}",
                    PaymentDate = payment.IoDateTime ?? DateTime.Now,
                    SettlementCurrency = payment.Currency ?? baseCurrency,
                    BaseCurrency = baseCurrency,
                    SettlementExchangeRate = payment.PaymentExchangeRate ?? 1.0m,
                    PayableTotalBaseCurrency = payableTotal,
                    ReceivableTotalBaseCurrency = receivableTotal,
                    ExchangeLoss = payment.ExchangeLoss,
                    ServiceFeeAmount = payment.ServiceFeeAmount ?? 0,
                    ServiceFeeBaseCurrency = payment.ServiceFeeBaseCurrencyAmount ?? 0,
                    IsMixedBusiness = isMixed,
                    HasMultiplePayments = hasMultiplePayments,
                    BankInfo = bankInfo,
                    Items = itemCalculations
                };

                results.Add(calculation);
            }

            return results;
        }

        /// <summary>
        /// 计算付款金额并识别混合业务
        /// </summary>
        private static (decimal PayableTotal, decimal ReceivableTotal, bool IsMixed, List<SettlementPaymentItemDto> ItemCalculations) 
            CalculatePaymentAmountsAndIdentifyMixedBusiness(List<PlInvoicesItem> items, PowerLmsUserDbContext dbContext, decimal settlementExchangeRate)
        {
            var itemCalculations = new List<SettlementPaymentItemDto>();
            var incomeCount = 0;
            var expenseCount = 0;
            var payableTotal = 0m;
            var receivableTotal = 0m;

            foreach (var item in items)
            {
                // 通过申请单明细获取原费用信息
                var requisitionItem = dbContext.DocFeeRequisitionItems.Find(item.RequisitionItemId);
                var fee = requisitionItem != null ? dbContext.DocFees.Find(requisitionItem.FeeId) : null;

                var isIncome = fee?.IO ?? false; // 付款单中默认认为是支出
                var originalFeeExchangeRate = fee?.ExchangeRate ?? 1.0m;

                // 统计收入支出数量
                if (isIncome) incomeCount++;
                else expenseCount++;

                // 使用结算汇率进行本位币金额计算
                var settlementAmountBaseCurrency = item.Amount * settlementExchangeRate;
                
                if (isIncome)
                    receivableTotal += settlementAmountBaseCurrency;
                else
                    payableTotal += settlementAmountBaseCurrency;

                itemCalculations.Add(new SettlementPaymentItemDto
                {
                    ItemId = item.Id,
                    Amount = item.Amount,
                    ExchangeRate = item.ExchangeRate,
                    SettlementAmountBaseCurrency = settlementAmountBaseCurrency,
                    OriginalFeeIO = isIncome,
                    OriginalFeeExchangeRate = originalFeeExchangeRate,
                    RequisitionItemId = item.RequisitionItemId
                });
            }

            // 混合业务判断：既有收入又有支出
            var isMixed = incomeCount > 0 && expenseCount > 0;

            return (payableTotal, receivableTotal, isMixed, itemCalculations);
        }

        /// <summary>
        /// 生成付款结算单金蝶凭证分录（静态方法）
        /// 实现六种凭证分录规则的完整逻辑
        /// </summary>
        private static List<KingdeeVoucher> GenerateSettlementPaymentVouchersStatic(
            List<SettlementPaymentCalculationDto> calculations,
            Dictionary<string, SubjectConfiguration> subjectConfigs,
            PowerLmsUserDbContext dbContext)
        {
            var vouchers = new List<KingdeeVoucher>();
            var voucherNumber = 1;

            var preparerName = subjectConfigs.GetValueOrDefault("SP_PREPARER")?.DisplayName ?? "系统导出";
            var voucherGroup = subjectConfigs.GetValueOrDefault("SP_VOUCHER_GROUP")?.VoucherGroup ?? "银";

            foreach (var calculation in calculations)
            {
                var entryId = 0;

                // 规则1：银行付款（贷方）- 必须生成
                // 多笔付款优先逻辑：如果存在多笔付款，为每笔生成分录；否则使用结算单总金额
                if (calculation.HasMultiplePayments)
                {
                    // 多笔付款：为每笔付款生成独立的银行分录
                    foreach (var item in calculation.Items)
                    {
                        var bankSubject = GetValidSubjectCode(
                            calculation.BankInfo?.AAccountSubjectCode,
                            subjectConfigs.GetValueOrDefault("SP_BANK_CREDIT")?.SubjectNumber,
                            "1001001"); // 默认银行存款科目
                        
                        var bankVoucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                        bankVoucher.FACCTID = bankSubject;
                        bankVoucher.FDC = 1; // 贷方
                        bankVoucher.FFCYAMT = item.Amount;
                        bankVoucher.FDEBIT = 0;
                        bankVoucher.FCREDIT = item.SettlementAmountBaseCurrency;
                        bankVoucher.FCYID = calculation.SettlementCurrency;
                        bankVoucher.FEXCHRATE = calculation.SettlementExchangeRate;

                        vouchers.Add(bankVoucher);
                    }
                }
                else
                {
                    // 单笔付款：使用结算单的总付款金额
                    var bankSubject = GetValidSubjectCode(
                        calculation.BankInfo?.AAccountSubjectCode,
                        subjectConfigs.GetValueOrDefault("SP_BANK_CREDIT")?.SubjectNumber,
                        "1001001"); // 默认银行存款科目
                    
                    var totalPaymentAmount = calculation.PayableTotalBaseCurrency; // 使用应付总额作为银行付款金额
                    
                    var bankVoucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    bankVoucher.FACCTID = bankSubject;
                    bankVoucher.FDC = 1; // 贷方
                    bankVoucher.FFCYAMT = totalPaymentAmount / calculation.SettlementExchangeRate; // 转换为原币
                    bankVoucher.FDEBIT = 0;
                    bankVoucher.FCREDIT = totalPaymentAmount;
                    bankVoucher.FCYID = calculation.SettlementCurrency;
                    bankVoucher.FEXCHRATE = calculation.SettlementExchangeRate;

                    vouchers.Add(bankVoucher);
                }

                // 规则2：应付账款冲销（借方）- 必须生成
                if (calculation.PayableTotalBaseCurrency > 0)
                {
                    var payableSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SP_PAYABLE_DEBIT")?.SubjectNumber,
                        null,
                        "203001"); // 默认应付账款科目
                    
                    var payableVoucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    payableVoucher.FACCTID = payableSubject;
                    payableVoucher.FCLSNAME1 = "客户";
                    payableVoucher.FOBJID1 = calculation.SupplierFinanceCode;
                    payableVoucher.FOBJNAME1 = calculation.SupplierName;
                    payableVoucher.FTRANSID = calculation.SupplierFinanceCode;
                    payableVoucher.FDC = 0; // 借方
                    payableVoucher.FFCYAMT = calculation.PayableTotalBaseCurrency;
                    payableVoucher.FDEBIT = calculation.PayableTotalBaseCurrency;
                    payableVoucher.FCREDIT = 0;
                    payableVoucher.FCYID = calculation.BaseCurrency;
                    payableVoucher.FEXCHRATE = 1.0000000m;

                    vouchers.Add(payableVoucher);
                }

                // 规则3：应收账款增加（贷方）- 混合业务时生成
                if (calculation.IsMixedBusiness && calculation.ReceivableTotalBaseCurrency > 0)
                {
                    var receivableSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SP_RECEIVABLE_CREDIT")?.SubjectNumber,
                        null,
                        "113001"); // 默认应收账款科目
                    
                    var receivableVoucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    receivableVoucher.FACCTID = receivableSubject;
                    receivableVoucher.FCLSNAME1 = "客户";
                    receivableVoucher.FOBJID1 = calculation.SupplierFinanceCode;
                    receivableVoucher.FOBJNAME1 = calculation.SupplierName;
                    receivableVoucher.FTRANSID = calculation.SupplierFinanceCode;
                    receivableVoucher.FDC = 1; // 贷方
                    receivableVoucher.FFCYAMT = calculation.ReceivableTotalBaseCurrency;
                    receivableVoucher.FDEBIT = 0;
                    receivableVoucher.FCREDIT = calculation.ReceivableTotalBaseCurrency;
                    receivableVoucher.FCYID = calculation.BaseCurrency;
                    receivableVoucher.FEXCHRATE = 1.0000000m;

                    vouchers.Add(receivableVoucher);
                }

                // 规则4：汇兑损益处理（借方）- 有汇率差异时生成
                if (calculation.ExchangeLoss != 0)
                {
                    var exchangeLossSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SP_EXCHANGE_LOSS")?.SubjectNumber,
                        null,
                        "603001"); // 默认汇兑损益科目
                    
                    var exchangeVoucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    exchangeVoucher.FACCTID = exchangeLossSubject;
                    exchangeVoucher.FDC = calculation.ExchangeLoss > 0 ? 0 : 1; // 与收款相反：正数借方，负数贷方
                    exchangeVoucher.FFCYAMT = Math.Abs(calculation.ExchangeLoss);
                    exchangeVoucher.FDEBIT = calculation.ExchangeLoss > 0 ? Math.Abs(calculation.ExchangeLoss) : 0;
                    exchangeVoucher.FCREDIT = calculation.ExchangeLoss < 0 ? Math.Abs(calculation.ExchangeLoss) : 0;
                    exchangeVoucher.FCYID = calculation.BaseCurrency;
                    exchangeVoucher.FEXCHRATE = 1.0000000m;

                    vouchers.Add(exchangeVoucher);
                }

                // 规则5：手续费支出（借方）- 有手续费时生成
                if (calculation.ServiceFeeAmount > 0 || calculation.ServiceFeeBaseCurrency > 0)
                {
                    var serviceFeeSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SP_SERVICE_FEE_DEBIT")?.SubjectNumber,
                        null,
                        "603002"); // 默认财务费用科目
                    
                    var serviceFeeVoucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    serviceFeeVoucher.FACCTID = serviceFeeSubject;
                    serviceFeeVoucher.FDC = 0; // 借方
                    
                    // 手续费汇率特殊处理
                    if (calculation.ServiceFeeBaseCurrency > 0 && calculation.ServiceFeeAmount > 0)
                    {
                        // 原币+本位币同时存在：币种选结算币种，汇率取结算汇率
                        serviceFeeVoucher.FFCYAMT = calculation.ServiceFeeAmount;
                        serviceFeeVoucher.FCYID = calculation.SettlementCurrency;
                        serviceFeeVoucher.FEXCHRATE = calculation.SettlementExchangeRate;
                    }
                    else
                    {
                        // 仅本位币手续费：币种选本位币，汇率为1
                        serviceFeeVoucher.FFCYAMT = calculation.ServiceFeeBaseCurrency;
                        serviceFeeVoucher.FCYID = calculation.BaseCurrency;
                        serviceFeeVoucher.FEXCHRATE = 1.0000000m;
                    }
                    
                    serviceFeeVoucher.FDEBIT = calculation.ServiceFeeBaseCurrency;
                    serviceFeeVoucher.FCREDIT = 0;

                    vouchers.Add(serviceFeeVoucher);
                }

                // 规则6：手续费银行扣款（贷方）- 与规则5配对生成
                if (calculation.ServiceFeeAmount > 0 || calculation.ServiceFeeBaseCurrency > 0)
                {
                    var bankSubject = GetValidSubjectCode(
                        calculation.BankInfo?.AAccountSubjectCode,
                        subjectConfigs.GetValueOrDefault("SP_BANK_CREDIT")?.SubjectNumber,
                        "1001001"); // 默认银行存款科目
                    
                    var serviceFeeBankVoucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    serviceFeeBankVoucher.FACCTID = bankSubject;
                    serviceFeeBankVoucher.FDC = 1; // 贷方
                    
                    // 手续费汇率特殊处理（与规则5保持一致）
                    if (calculation.ServiceFeeBaseCurrency > 0 && calculation.ServiceFeeAmount > 0)
                    {
                        // 原币+本位币同时存在：币种选结算币种，汇率取结算汇率
                        serviceFeeBankVoucher.FFCYAMT = calculation.ServiceFeeAmount;
                        serviceFeeBankVoucher.FCYID = calculation.SettlementCurrency;
                        serviceFeeBankVoucher.FEXCHRATE = calculation.SettlementExchangeRate;
                    }
                    else
                    {
                        // 仅本位币手续费：币种选本位币，汇率为1
                        serviceFeeBankVoucher.FFCYAMT = calculation.ServiceFeeBaseCurrency;
                        serviceFeeBankVoucher.FCYID = calculation.BaseCurrency;
                        serviceFeeBankVoucher.FEXCHRATE = 1.0000000m;
                    }
                    
                    serviceFeeBankVoucher.FDEBIT = 0;
                    serviceFeeBankVoucher.FCREDIT = calculation.ServiceFeeBaseCurrency;

                    vouchers.Add(serviceFeeBankVoucher);
                }

                voucherNumber++;
            }

            return vouchers;
        }

        /// <summary>
        /// 创建基础付款凭证对象
        /// </summary>
        private static KingdeeVoucher CreateBasePaymentVoucher(SettlementPaymentCalculationDto calculation, int voucherNumber, 
            int entryId, string voucherGroup, string preparerName)
        {
            var summary = $"{calculation.SupplierName}【支出】{calculation.PaymentNumber}";
            
            return new KingdeeVoucher
            {
                Id = Guid.NewGuid(),
                FDATE = calculation.PaymentDate,
                FTRANSDATE = calculation.PaymentDate,
                FPERIOD = calculation.PaymentDate.Month,
                FGROUP = voucherGroup,
                FNUM = voucherNumber,
                FENTRYID = entryId,
                FEXP = summary.Length > 500 ? summary[..500] : summary,
                FCYID = "RMB",
                FEXCHRATE = 1.0000000m,
                FPREPARE = preparerName,
                FMODULE = "GL",
                FDELETED = false
            };
        }

        /// <summary>
        /// 获取付款结算单专用的金蝶字段映射
        /// </summary>
        private static Dictionary<string, string> GetSettlementPaymentKingdeeFieldMappings()
        {
            return new Dictionary<string, string>
            {
                {"FDATE", "FDATE"}, {"FTRANSDATE", "FTRANSDATE"}, {"FPERIOD", "FPERIOD"}, {"FGROUP", "FGROUP"},
                {"FNUM", "FNUM"}, {"FENTRYID", "FENTRYID"}, {"FEXP", "FEXP"}, {"FACCTID", "FACCTID"},
                {"FCLSNAME1", "FCLSNAME1"}, {"FOBJID1", "FOBJID1"}, {"FOBJNAME1", "FOBJNAME1"}, {"FTRANSID", "FTRANSID"},
                {"FCYID", "FCYID"}, {"FEXCHRATE", "FEXCHRATE"}, {"FDC", "FDC"}, {"FFCYAMT", "FFCYAMT"},
                {"FDEBIT", "FDEBIT"}, {"FCREDIT", "FCREDIT"}, {"FPREPARE", "FPREPARE"}, {"FMODULE", "FMODULE"}, {"FDELETED", "FDELETED"}
            };
        }

        /// <summary>
        /// 获取付款结算单专用的金蝶字段类型定义
        /// </summary>
        private static Dictionary<string, NativeDbType> GetSettlementPaymentKingdeeFieldTypes()
        {
            return new Dictionary<string, NativeDbType>
            {
                {"FDATE", NativeDbType.Date}, {"FTRANSDATE", NativeDbType.Date}, {"FPERIOD", NativeDbType.Numeric}, 
                {"FGROUP", NativeDbType.Char}, {"FNUM", NativeDbType.Numeric}, {"FENTRYID", NativeDbType.Numeric}, 
                {"FEXP", NativeDbType.Char}, {"FACCTID", NativeDbType.Char}, {"FCLSNAME1", NativeDbType.Char}, 
                {"FOBJID1", NativeDbType.Char}, {"FOBJNAME1", NativeDbType.Char}, {"FTRANSID", NativeDbType.Char},
                {"FCYID", NativeDbType.Char}, {"FEXCHRATE", NativeDbType.Numeric}, {"FDC", NativeDbType.Numeric}, 
                {"FFCYAMT", NativeDbType.Numeric}, {"FDEBIT", NativeDbType.Numeric}, {"FCREDIT", NativeDbType.Numeric}, 
                {"FPREPARE", NativeDbType.Char}, {"FMODULE", NativeDbType.Char}, {"FDELETED", NativeDbType.Logical}
            };
        }

        #endregion

        #region 付款结算单导出辅助方法

        /// <summary>
        /// 验证付款结算单科目配置完整性
        /// </summary>
        /// <param name="orgId">组织ID</param>
        /// <returns>缺失的科目配置代码列表</returns>
        private List<string> ValidateSettlementPaymentSubjectConfiguration(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SP_PAYABLE_DEBIT",        // 规则2：应付账款借方科目
                "SP_RECEIVABLE_CREDIT",    // 规则3：应收账款贷方科目（混合业务）
                "SP_EXCHANGE_LOSS",        // 规则4：汇兑损益科目
                "SP_SERVICE_FEE_DEBIT",    // 规则5：手续费借方科目
                "SP_PREPARER",             // 制单人
                "SP_VOUCHER_GROUP"         // 凭证字
            };

            var existingCodes = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();

            return requiredCodes.Except(existingCodes).ToList();
        }

        /// <summary>
        /// 付款结算单组织权限过滤
        /// </summary>
        /// <param name="query">查询对象</param>
        /// <param name="user">用户账号</param>
        /// <returns>过滤后的查询</returns>
        private IQueryable<PlInvoices> ApplyOrganizationFilterForSettlementPayments(IQueryable<PlInvoices> query, Account user)
        {
            if (user == null) return query.Where(i => false);
            if (user.IsSuperAdmin) return query;

            var merchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue) return query.Where(i => false);

            HashSet<Guid?> allowedOrgIds;

            if (user.IsMerchantAdmin)
            {
                // 商户管理员可以访问整个商户下的所有组织机构
                var allOrgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                allowedOrgIds = new HashSet<Guid?>(allOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }
            else
            {
                // 普通用户只能访问其当前登录的公司及下属机构
                var companyId = user.OrgId.HasValue ? _OrgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue) return query.Where(i => false);

                var companyOrgIds = _OrgManager.GetOrgIdsByCompanyId(companyId.Value).ToList();
                allowedOrgIds = new HashSet<Guid?>(companyOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }

            // 通过关联申请单和费用来进行组织权限过滤
            var filteredQuery = from invoice in query
                                join invoiceItem in _DbContext.PlInvoicesItems
                                    on invoice.Id equals invoiceItem.ParentId into itemGroup
                                from item in itemGroup.DefaultIfEmpty()
                                join requisitionItem in _DbContext.DocFeeRequisitionItems
                                    on item.RequisitionItemId equals requisitionItem.Id into reqItemGroup
                                from reqItem in reqItemGroup.DefaultIfEmpty()
                                join requisition in _DbContext.DocFeeRequisitions
                                    on reqItem.ParentId equals requisition.Id into reqGroup
                                from req in reqGroup.DefaultIfEmpty()
                                where allowedOrgIds.Contains(req.OrgId)
                                select invoice;

            return filteredQuery.Distinct();
        }

        #endregion

        #region 付款结算单导出静态辅助方法

        /// <summary>
        /// 加载付款结算单科目配置（静态版本）
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadSettlementPaymentSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SP_PAYABLE_DEBIT", "SP_RECEIVABLE_CREDIT", "SP_EXCHANGE_LOSS", "SP_SERVICE_FEE_DEBIT",
                "SP_PREPARER", "SP_VOUCHER_GROUP"
            };

            var configs = dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToList();

            return configs.ToDictionary(c => c.Code, c => c);
        }

        /// <summary>
        /// 付款结算单组织权限过滤（静态版本）
        /// </summary>
        private static IQueryable<PlInvoices> ApplyOrganizationFilterForSettlementPaymentsStatic(IQueryable<PlInvoices> query, Account user,
            PowerLmsUserDbContext dbContext, IServiceProvider serviceProvider)
        {
            if (user == null) return query.Where(i => false);
            if (user.IsSuperAdmin) return query;

            var orgManager = serviceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>();
            var merchantId = orgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue) return query.Where(i => false);

            HashSet<Guid?> allowedOrgIds;

            if (user.IsMerchantAdmin)
            {
                var allOrgIds = orgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                allowedOrgIds = new HashSet<Guid?>(allOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }
            else
            {
                var companyId = user.OrgId.HasValue ? orgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue) return query.Where(i => false);

                var companyOrgIds = orgManager.GetOrgIdsByCompanyId(companyId.Value).ToList();
                allowedOrgIds = new HashSet<Guid?>(companyOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }

            var filteredQuery = from invoice in query
                                join invoiceItem in dbContext.PlInvoicesItems
                                    on invoice.Id equals invoiceItem.ParentId into itemGroup
                                from item in itemGroup.DefaultIfEmpty()
                                join requisitionItem in dbContext.DocFeeRequisitionItems
                                    on item.RequisitionItemId equals requisitionItem.Id into reqItemGroup
                                from reqItem in reqItemGroup.DefaultIfEmpty()
                                join requisition in dbContext.DocFeeRequisitions
                                    on reqItem.ParentId equals requisition.Id into reqGroup
                                from req in reqGroup.DefaultIfEmpty()
                                where allowedOrgIds.Contains(req.OrgId)
                                select invoice;

            return filteredQuery.Distinct();
        }

        #endregion
    }
}