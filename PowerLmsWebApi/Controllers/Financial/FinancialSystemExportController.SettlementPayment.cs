/*
* 项目：PowerLms财务系统 | 模块：付款结算单导出金蝶功能
* 功能：将付款结算单转换为符合金蝶财务软件要求的会计凭证分录
* 技术要点：六种凭证分录规则、多币种处理、混合业务识别、多笔付款优先处理、手续费双分录、导出防重机制
* 作者：zc | 创建：2025-01 | 修改：2025-12-14 集成导出防重机制
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
    /// - 导出防重机制（基于IFinancialExportable接口）
    /// - 权限控制和数据隔离
    /// - 异步任务处理机制
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region HTTP接口 - 付款结算单导出

        /// <summary>
        /// 导出付款结算单为金蝶DBF格式文件
        /// 支持六种凭证分录规则的完整实现，处理复杂的多币种和混合业务场景
        /// 注意：自动过滤已导出的数据（ExportedDateTime不为null），导出后自动标记导出时间和用户
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
                
                // 强制限制为付款结算单
                conditions["IO"] = "false";

                // 4. 预检查付款结算单数量 - 使用Manager方法过滤未导出数据
                var exportManager = _ServiceProvider.GetRequiredService<FinancialSystemExportManager>();
                var baseQuery = _DbContext.PlInvoicess.AsQueryable();
                var settlementPaymentsQuery = exportManager.FilterUnexported(baseQuery);
                
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
                var exportManager = serviceProvider.GetRequiredService<FinancialSystemExportManager>();
                var baseQuery = dbContext.PlInvoicess.AsQueryable();
                var settlementPaymentsQuery = exportManager.FilterUnexported(baseQuery);
                
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

                // 🔧 修复LINQ翻译问题：在数据库层面完成分组，避免客户端GroupBy
                var itemsDict = items
                    .ToLookup(item => item.ParentId.Value)
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

                currentStep = "标记付款结算单为已导出";
                var markedCount = exportManager.MarkAsExported(settlementPayments, userId);
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
                    OriginalFileSize = fileSize,
                    MarkedCount = markedCount
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
        /// 计算付款结算单业务数据（12分录模型）
        /// 包括应付应收金额计算（按国内外+代垫分类）、混合业务识别、多笔付款检测、本位币转换等
        /// </summary>
        private static List<SettlementPaymentCalculationDto> CalculateSettlementPaymentData(
            List<PlInvoices> settlementPayments,
            Dictionary<Guid, List<PlInvoicesItem>> itemsDict,
            PowerLmsUserDbContext dbContext,
            BusinessLogicManager businessLogicManager)
        {
            var results = new List<SettlementPaymentCalculationDto>();
            
            // 批量加载所有关联数据
            var settlementPaymentIds = settlementPayments.Select(r => r.Id).ToList();
            
            // 修复：先ToList()再ToDictionary()
            var customerIds = settlementPayments.Select(r => r.JiesuanDanweiId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allCustomers = dbContext.PlCustomers.Where(c => customerIds.Contains(c.Id)).ToList().ToDictionary(c => c.Id);
            
            var allRequisitionItemIds = itemsDict.Values.SelectMany(items => items.Select(i => i.RequisitionItemId)).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allRequisitionItems = dbContext.DocFeeRequisitionItems.Where(ri => allRequisitionItemIds.Contains(ri.Id)).ToList().ToDictionary(ri => ri.Id);
            
            var allFeeIds = allRequisitionItems.Values.Select(ri => ri.FeeId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allFees = dbContext.DocFees.Where(f => allFeeIds.Contains(f.Id)).ToList().ToDictionary(f => f.Id);
            
            var allFeeTypeIds = allFees.Values.Select(f => f.FeeTypeId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allFeeTypes = dbContext.DD_FeesTypes.Where(ft => allFeeTypeIds.Contains(ft.Id)).ToList().ToDictionary(ft => ft.Id);
            
            // 修复：先ToList()再分组、转字典
            var allTransactions = dbContext.ActualFinancialTransactions
                .Where(t => settlementPaymentIds.Contains(t.ParentId.Value) && !t.IsDelete)
                .OrderBy(t => t.TransactionDate)
                .ToList();
            var transactionsGrouped = allTransactions.GroupBy(t => t.ParentId.Value).ToDictionary(g => g.Key, g => g.ToList());
            
            var allBankAccountIds = allTransactions.Select(t => t.BankAccountId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allBankAccounts = dbContext.BankInfos.Where(b => allBankAccountIds.Contains(b.Id)).ToList().ToDictionary(b => b.Id);
            
            var allBankIds = settlementPayments.Select(r => r.BankId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allBanks = dbContext.BankInfos.Where(b => allBankIds.Contains(b.Id)).ToList().ToDictionary(b => b.Id);
            
            foreach (var payment in settlementPayments)
            {
                var items = itemsDict.GetValueOrDefault(payment.Id, new List<PlInvoicesItem>());
                var baseCurrency = businessLogicManager.GetBaseCurrencyCode(payment, dbContext) ?? "CNY";
                var customer = allCustomers.GetValueOrDefault(payment.JiesuanDanweiId ?? Guid.Empty);
                var customerName = customer?.Name_Name ?? "未知供应商";
                var customerFinanceCode = customer?.FinanceCodeAP ?? "";
                var isDomestic = customer?.IsDomestic ?? true;
                var bankInfo = allBanks.GetValueOrDefault(payment.BankId ?? Guid.Empty);
                var actualTransactions = transactionsGrouped.GetValueOrDefault(payment.Id, new List<ActualFinancialTransaction>());
                
                var (payableForeign, payableDomesticCustomer, payableDomesticTariff, 
                     receivableForeign, receivableDomesticCustomer, receivableDomesticTariff, 
                     isMixed, itemCalculations) = CalculatePaymentAmountsWithClassification(items, allRequisitionItems, allFees, allFeeTypes, isDomestic);
                
                var actualTransactionDtos = actualTransactions.Select(t => new ActualFinancialTransactionDto
                {
                    TransactionId = t.Id,
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    BankAccountId = t.BankAccountId,
                    BankSubjectCode = t.BankAccountId.HasValue && allBankAccounts.ContainsKey(t.BankAccountId.Value) ? allBankAccounts[t.BankAccountId.Value].AAccountSubjectCode : null,
                    ServiceFee = t.ServiceFee
                }).ToList();
                
                var calculation = new SettlementPaymentCalculationDto
                {
                    SettlementPaymentId = payment.Id,
                    CustomerName = customerName,
                    CustomerFinanceCode = customerFinanceCode,
                    IsDomestic = isDomestic,
                    PaymentNumber = payment.IoPingzhengNo ?? $"SP{payment.Id.ToString("N")[..8]}",
                    PaymentDate = payment.IoDateTime ?? DateTime.Now,
                    SettlementCurrency = payment.Currency ?? baseCurrency,
                    BaseCurrency = baseCurrency,
                    SettlementExchangeRate = payment.PaymentExchangeRate ?? 1.0m,
                    PayableForeignBaseCurrency = payableForeign,
                    PayableDomesticCustomerBaseCurrency = payableDomesticCustomer,
                    PayableDomesticTariffBaseCurrency = payableDomesticTariff,
                    ReceivableForeignBaseCurrency = receivableForeign,
                    ReceivableDomesticCustomerBaseCurrency = receivableDomesticCustomer,
                    ReceivableDomesticTariffBaseCurrency = receivableDomesticTariff,
                    ExchangeLoss = payment.ExchangeLoss,
                    ServiceFeeAmount = payment.ServiceFeeAmount ?? 0m,
                    ServiceFeeBaseCurrency = payment.ServiceFeeBaseCurrencyAmount ?? 0m,
                    AdvancePaymentAmount = payment.AdvancePaymentAmount ?? 0m,
                    AdvancePaymentBaseCurrency = payment.AdvancePaymentBaseCurrencyAmount ?? 0m,
                    IsMixedBusiness = isMixed,
                    Items = itemCalculations,
                    ActualTransactions = actualTransactionDtos,
                    BankInfo = bankInfo
                };
                results.Add(calculation);
            }
            return results;
        }

        /// <summary>
        /// 计算应付应收金额并按国内外客户、代垫属性分类（12分录模型）
        /// </summary>
        private static (decimal PayableForeign, decimal PayableDomesticCustomer, decimal PayableDomesticTariff, 
                       decimal ReceivableForeign, decimal ReceivableDomesticCustomer, decimal ReceivableDomesticTariff, 
                       bool IsMixed, List<SettlementPaymentItemDto> ItemCalculations) 
            CalculatePaymentAmountsWithClassification(List<PlInvoicesItem> items, 
                Dictionary<Guid, DocFeeRequisitionItem> requisitionItemDict, 
                Dictionary<Guid, DocFee> feeDict, 
                Dictionary<Guid, FeesType> feeTypeDict, 
                bool isDomestic)
        {
            var itemCalculations = new List<SettlementPaymentItemDto>();
            var incomeCount = 0;
            var expenseCount = 0;
            var payableForeign = 0m;
            var payableDomesticCustomer = 0m;
            var payableDomesticTariff = 0m;
            var receivableForeign = 0m;
            var receivableDomesticCustomer = 0m;
            var receivableDomesticTariff = 0m;
            
            foreach (var item in items)
            {
                var requisitionItem = item.RequisitionItemId.HasValue ? requisitionItemDict.GetValueOrDefault(item.RequisitionItemId.Value) : null;
                var fee = requisitionItem != null && requisitionItem.FeeId.HasValue && feeDict.ContainsKey(requisitionItem.FeeId.Value) ? feeDict[requisitionItem.FeeId.Value] : null;
                var feeType = fee != null && fee.FeeTypeId.HasValue && feeTypeDict.ContainsKey(fee.FeeTypeId.Value) ? feeTypeDict[fee.FeeTypeId.Value] : null;
                var isIncome = fee?.IO ?? false;
                var originalFeeExchangeRate = fee?.ExchangeRate ?? 1.0m;
                var isAdvanceFee = feeType?.IsDaiDian ?? false;
                if (isIncome) incomeCount++;
                else expenseCount++;
                var settlementAmountBaseCurrency = item.Amount * originalFeeExchangeRate;
                if (isDomestic)
                {
                    if (isAdvanceFee)
                    {
                        if (isIncome) receivableDomesticTariff += settlementAmountBaseCurrency;
                        else payableDomesticTariff += settlementAmountBaseCurrency;
                    }
                    else
                    {
                        if (isIncome) receivableDomesticCustomer += settlementAmountBaseCurrency;
                        else payableDomesticCustomer += settlementAmountBaseCurrency;
                    }
                }
                else
                {
                    if (isIncome) receivableForeign += settlementAmountBaseCurrency;
                    else payableForeign += settlementAmountBaseCurrency;
                }
                itemCalculations.Add(new SettlementPaymentItemDto
                {
                    ItemId = item.Id,
                    Amount = item.Amount,
                    ExchangeRate = item.ExchangeRate,
                    SettlementAmountBaseCurrency = settlementAmountBaseCurrency,
                    OriginalFeeIO = isIncome,
                    OriginalFeeExchangeRate = originalFeeExchangeRate,
                    IsAdvanceFee = isAdvanceFee,
                    RequisitionItemId = item.RequisitionItemId
                });
            }
            var isMixed = incomeCount > 0 && expenseCount > 0;
            return (payableForeign, payableDomesticCustomer, payableDomesticTariff, 
                   receivableForeign, receivableDomesticCustomer, receivableDomesticTariff, 
                   isMixed, itemCalculations);
        }

        /// <summary>
        /// 生成付款结算单金蝶凭证分录（12分录模型静态方法）
        /// 实现11个独立函数的完整业务逻辑
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
                vouchers.AddRange(GeneratePaymentBankVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                vouchers.AddRange(GeneratePayableDebitVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                if (calculation.IsMixedBusiness)
                {
                    vouchers.AddRange(GenerateReceivableCreditVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                }
                vouchers.AddRange(GeneratePaymentOtherVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                voucherNumber++;
            }
            return vouchers;
        }

        /// <summary>
        /// 规则1：生成银行付款贷方分录（1~N条必生成）
        /// 优先使用ActualFinancialTransaction表，无记录则用结算单金额
        /// </summary>
        private static List<KingdeeVoucher> GeneratePaymentBankVouchers(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            ref int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.ActualTransactions.Any())
            {
                foreach (var transaction in calculation.ActualTransactions)
                {
                    var bankSubject = GetValidSubjectCode(
                        transaction.BankSubjectCode,
                        calculation.BankInfo?.AAccountSubjectCode,
                        subjectConfigs.GetValueOrDefault("SP_BANK_CREDIT")?.SubjectNumber,
                        "1001001");
                    var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    voucher.FACCTID = bankSubject;
                    voucher.FDC = 1;
                    voucher.FFCYAMT = transaction.Amount;
                    voucher.FDEBIT = 0;
                    voucher.FCREDIT = transaction.Amount * calculation.SettlementExchangeRate;
                    voucher.FCYID = calculation.SettlementCurrency;
                    voucher.FEXCHRATE = calculation.SettlementExchangeRate;
                    vouchers.Add(voucher);
                }
            }
            else
            {
                var bankSubject = GetValidSubjectCode(
                    calculation.BankInfo?.AAccountSubjectCode,
                    subjectConfigs.GetValueOrDefault("SP_BANK_CREDIT")?.SubjectNumber,
                    "1001001");
                var totalPaymentAmountBaseCurrency = calculation.PayableTotalBaseCurrency;
                var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                voucher.FACCTID = bankSubject;
                voucher.FDC = 1;
                voucher.FFCYAMT = totalPaymentAmountBaseCurrency / calculation.SettlementExchangeRate;
                voucher.FDEBIT = 0;
                voucher.FCREDIT = totalPaymentAmountBaseCurrency;
                voucher.FCYID = calculation.SettlementCurrency;
                voucher.FEXCHRATE = calculation.SettlementExchangeRate;
                vouchers.Add(voucher);
            }
            return vouchers;
        }

        /// <summary>
        /// 规则2：生成应付账款冲销借方分录（1~3条必生成）
        /// </summary>
        private static List<KingdeeVoucher> GeneratePayableDebitVouchers(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            ref int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.IsDomestic)
            {
                if (calculation.PayableDomesticCustomerBaseCurrency > 0)
                {
                    vouchers.Add(GeneratePayableDebitDomesticCustomerVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
                if (calculation.PayableDomesticTariffBaseCurrency > 0)
                {
                    vouchers.Add(GeneratePayableDebitDomesticTariffVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            else
            {
                if (calculation.PayableForeignBaseCurrency > 0)
                {
                    vouchers.Add(GeneratePayableDebitForeignVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            return vouchers;
        }

        /// <summary>
        /// 规则2A：应付账款-国外供应商
        /// </summary>
        private static KingdeeVoucher GeneratePayableDebitForeignVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_PAYABLE_DEBIT_OUT_CUS")?.SubjectNumber,
                subjectConfigs.GetValueOrDefault("SP_PAYABLE_DEBIT")?.SubjectNumber,
                "203001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 0;
            voucher.FFCYAMT = calculation.PayableForeignBaseCurrency;
            voucher.FDEBIT = calculation.PayableForeignBaseCurrency;
            voucher.FCREDIT = 0;
            voucher.FCYID = calculation.BaseCurrency;
            voucher.FEXCHRATE = 1.0000000m;
            return voucher;
        }

        /// <summary>
        /// 规则2B：应付账款-国内客户（非代垫）
        /// </summary>
        private static KingdeeVoucher GeneratePayableDebitDomesticCustomerVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_PAYABLE_DEBIT_IN_CUS")?.SubjectNumber,
                subjectConfigs.GetValueOrDefault("SP_PAYABLE_DEBIT")?.SubjectNumber,
                "203001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 0;
            voucher.FFCYAMT = calculation.PayableDomesticCustomerBaseCurrency;
            voucher.FDEBIT = calculation.PayableDomesticCustomerBaseCurrency;
            voucher.FCREDIT = 0;
            voucher.FCYID = calculation.BaseCurrency;
            voucher.FEXCHRATE = 1.0000000m;
            return voucher;
        }

        /// <summary>
        /// 规则2C：应付账款-国内关税（代垫）
        /// </summary>
        private static KingdeeVoucher GeneratePayableDebitDomesticTariffVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_PAYABLE_DEBIT_IN_TAR")?.SubjectNumber,
                subjectConfigs.GetValueOrDefault("SP_PAYABLE_DEBIT")?.SubjectNumber,
                "203001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 0;
            voucher.FFCYAMT = calculation.PayableDomesticTariffBaseCurrency;
            voucher.FDEBIT = calculation.PayableDomesticTariffBaseCurrency;
            voucher.FCREDIT = 0;
            voucher.FCYID = calculation.BaseCurrency;
            voucher.FEXCHRATE = 1.0000000m;
            return voucher;
        }

        /// <summary>
        /// 规则3：生成应收账款增加贷方分录（0~3条混合业务生成）
        /// </summary>
        private static List<KingdeeVoucher> GenerateReceivableCreditVouchers(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            ref int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.IsDomestic)
            {
                if (calculation.ReceivableDomesticCustomerBaseCurrency > 0)
                {
                    vouchers.Add(GenerateReceivableCreditDomesticCustomerVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
                if (calculation.ReceivableDomesticTariffBaseCurrency > 0)
                {
                    vouchers.Add(GenerateReceivableCreditDomesticTariffVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            else
            {
                if (calculation.ReceivableForeignBaseCurrency > 0)
                {
                    vouchers.Add(GenerateReceivableCreditForeignVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            return vouchers;
        }

        /// <summary>
        /// 规则3A：应收账款-国外客户（混合业务）
        /// </summary>
        private static KingdeeVoucher GenerateReceivableCreditForeignVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_RECEIVABLE_CREDIT_OUT_CUS")?.SubjectNumber,
                subjectConfigs.GetValueOrDefault("SP_RECEIVABLE_CREDIT")?.SubjectNumber,
                "113001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 1;
            voucher.FFCYAMT = calculation.ReceivableForeignBaseCurrency;
            voucher.FDEBIT = 0;
            voucher.FCREDIT = calculation.ReceivableForeignBaseCurrency;
            voucher.FCYID = calculation.BaseCurrency;
            voucher.FEXCHRATE = 1.0000000m;
            return voucher;
        }

        /// <summary>
        /// 规则3B：应收账款-国内客户（非代垫，混合业务）
        /// </summary>
        private static KingdeeVoucher GenerateReceivableCreditDomesticCustomerVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_RECEIVABLE_CREDIT_IN_CUS")?.SubjectNumber,
                subjectConfigs.GetValueOrDefault("SP_RECEIVABLE_CREDIT")?.SubjectNumber,
                "113001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 1;
            voucher.FFCYAMT = calculation.ReceivableDomesticCustomerBaseCurrency;
            voucher.FDEBIT = 0;
            voucher.FCREDIT = calculation.ReceivableDomesticCustomerBaseCurrency;
            voucher.FCYID = calculation.BaseCurrency;
            voucher.FEXCHRATE = 1.0000000m;
            return voucher;
        }

        /// <summary>
        /// 规则3C：应收账款-国内关税（代垫，混合业务）
        /// </summary>
        private static KingdeeVoucher GenerateReceivableCreditDomesticTariffVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_RECEIVABLE_CREDIT_IN_TAR")?.SubjectNumber,
                subjectConfigs.GetValueOrDefault("SP_RECEIVABLE_CREDIT")?.SubjectNumber,
                "113001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 1;
            voucher.FFCYAMT = calculation.ReceivableDomesticTariffBaseCurrency;
            voucher.FDEBIT = 0;
            voucher.FCREDIT = calculation.ReceivableDomesticTariffBaseCurrency;
            voucher.FCYID = calculation.BaseCurrency;
            voucher.FEXCHRATE = 1.0000000m;
            return voucher;
        }

        /// <summary>
        /// 规则4-7：生成其他科目分录（条件生成）
        /// </summary>
        private static List<KingdeeVoucher> GeneratePaymentOtherVouchers(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            ref int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.ExchangeLoss != 0)
            {
                vouchers.Add(GeneratePaymentExchangeLossVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
            }
            if (calculation.ServiceFeeAmount > 0 || calculation.ServiceFeeBaseCurrency > 0)
            {
                vouchers.Add(GeneratePaymentServiceFeeDebitVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                vouchers.Add(GeneratePaymentServiceFeeCreditVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
            }
            if (calculation.AdvancePaymentAmount > 0)
            {
                vouchers.Add(GenerateAdvancePaymentVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
            }
            return vouchers;
        }

        /// <summary>
        /// 规则4：汇兑损益分录
        /// </summary>
        private static KingdeeVoucher GeneratePaymentExchangeLossVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_EXCHANGE_LOSS")?.SubjectNumber,
                "603001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FDC = calculation.ExchangeLoss > 0 ? 0 : 1;
            voucher.FFCYAMT = Math.Abs(calculation.ExchangeLoss);
            voucher.FDEBIT = calculation.ExchangeLoss > 0 ? Math.Abs(calculation.ExchangeLoss) : 0;
            voucher.FCREDIT = calculation.ExchangeLoss < 0 ? Math.Abs(calculation.ExchangeLoss) : 0;
            voucher.FCYID = calculation.BaseCurrency;
            voucher.FEXCHRATE = 1.0000000m;
            return voucher;
        }

        /// <summary>
        /// 规则5：手续费支出借方分录
        /// </summary>
        private static KingdeeVoucher GeneratePaymentServiceFeeDebitVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_SERVICE_FEE_DEBIT")?.SubjectNumber,
                "603002");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FDC = 0;
            if (calculation.ServiceFeeBaseCurrency > 0 && calculation.ServiceFeeAmount > 0)
            {
                voucher.FFCYAMT = calculation.ServiceFeeAmount;
                voucher.FCYID = calculation.SettlementCurrency;
                voucher.FEXCHRATE = calculation.SettlementExchangeRate;
            }
            else
            {
                voucher.FFCYAMT = calculation.ServiceFeeBaseCurrency;
                voucher.FCYID = calculation.BaseCurrency;
                voucher.FEXCHRATE = 1.0000000m;
            }
            voucher.FDEBIT = calculation.ServiceFeeBaseCurrency;
            voucher.FCREDIT = 0;
            return voucher;
        }

        /// <summary>
        /// 规则6：手续费银行扣款贷方分录（与规则5配对）
        /// </summary>
        private static KingdeeVoucher GeneratePaymentServiceFeeCreditVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                calculation.BankInfo?.AAccountSubjectCode,
                subjectConfigs.GetValueOrDefault("SP_BANK_CREDIT")?.SubjectNumber,
                "1001001");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FDC = 1;
            if (calculation.ServiceFeeBaseCurrency > 0 && calculation.ServiceFeeAmount > 0)
            {
                voucher.FFCYAMT = calculation.ServiceFeeAmount;
                voucher.FCYID = calculation.SettlementCurrency;
                voucher.FEXCHRATE = calculation.SettlementExchangeRate;
            }
            else
            {
                voucher.FFCYAMT = calculation.ServiceFeeBaseCurrency;
                voucher.FCYID = calculation.BaseCurrency;
                voucher.FEXCHRATE = 1.0000000m;
            }
            voucher.FDEBIT = 0;
            voucher.FCREDIT = calculation.ServiceFeeBaseCurrency;
            return voucher;
        }

        /// <summary>
        /// 规则7：预付款借方分录
        /// </summary>
        private static KingdeeVoucher GenerateAdvancePaymentVoucher(
            SettlementPaymentCalculationDto calculation, 
            int voucherNumber, 
            int entryId, 
            string voucherGroup, 
            string preparerName, 
            Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(
                subjectConfigs.GetValueOrDefault("SP_ADVANCE_CREDIT")?.SubjectNumber,
                "123101");
            var voucher = CreateBasePaymentVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 0;
            voucher.FFCYAMT = calculation.AdvancePaymentAmount;
            voucher.FDEBIT = calculation.AdvancePaymentBaseCurrency;
            voucher.FCREDIT = 0;
            voucher.FCYID = calculation.SettlementCurrency;
            voucher.FEXCHRATE = calculation.SettlementExchangeRate;
            return voucher;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建付款单基础凭证对象
        /// </summary>
        private static KingdeeVoucher CreateBasePaymentVoucher(SettlementPaymentCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName)
        {
            var summary = $"{calculation.CustomerName}【支出】{calculation.PaymentNumber}";
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

        #endregion

        #region 配置验证和组织权限辅助方法

        /// <summary>
        /// 验证付款结算单科目配置完整性
        /// </summary>
        private List<string> ValidateSettlementPaymentSubjectConfiguration(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SP_BANK_CREDIT", "SP_PAYABLE_DEBIT", "SP_PAYABLE_DEBIT_IN_CUS", "SP_PAYABLE_DEBIT_IN_TAR", "SP_PAYABLE_DEBIT_OUT_CUS",
                "SP_RECEIVABLE_CREDIT", "SP_RECEIVABLE_CREDIT_IN_CUS", "SP_RECEIVABLE_CREDIT_IN_TAR", "SP_RECEIVABLE_CREDIT_OUT_CUS",
                "SP_EXCHANGE_LOSS", "SP_SERVICE_FEE_DEBIT", "SP_SERVICE_FEE_CREDIT", "SP_ADVANCE_CREDIT",
                "SP_PREPARER", "SP_VOUCHER_GROUP"
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
        private IQueryable<PlInvoices> ApplyOrganizationFilterForSettlementPayments(IQueryable<PlInvoices> query, Account user)
        {
            if (user == null) return query.Where(i => false);
            if (user.IsSuperAdmin) return query;
            var merchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue) return query.Where(i => false);
            HashSet<Guid?> allowedOrgIds;
            if (user.IsMerchantAdmin)
            {
                var allOrgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                allowedOrgIds = new HashSet<Guid?>(allOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }
            else
            {
                var companyId = user.OrgId.HasValue ? _OrgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue) return query.Where(i => false);
                var companyOrgIds = _OrgManager.GetOrgIdsByCompanyId(companyId.Value).ToList();
                allowedOrgIds = new HashSet<Guid?>(companyOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }
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

        /// <summary>
        /// 加载付款结算单科目配置（静态版本）
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadSettlementPaymentSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SP_BANK_CREDIT", "SP_PAYABLE_DEBIT", "SP_PAYABLE_DEBIT_IN_CUS", "SP_PAYABLE_DEBIT_IN_TAR", "SP_PAYABLE_DEBIT_OUT_CUS",
                "SP_RECEIVABLE_CREDIT", "SP_RECEIVABLE_CREDIT_IN_CUS", "SP_RECEIVABLE_CREDIT_IN_TAR", "SP_RECEIVABLE_CREDIT_OUT_CUS",
                "SP_EXCHANGE_LOSS", "SP_SERVICE_FEE_DEBIT", "SP_SERVICE_FEE_CREDIT", "SP_ADVANCE_CREDIT",
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
    }
}