/*
* 项目：PowerLms财务系统 | 模块：收款结算单导出金蝶功能
* 功能：将收款结算单转换为符合金蝶财务软件要求的会计凭证分录
* 技术要点：七种凭证分录规则、多币种处理、混合业务识别、汇率计算、导出防重机制
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
    /// 财务系统导出控制器 - 收款结算单导出金蝶功能模块
    /// 实现收款结算单的财务导出功能，支持：
    /// - 七种凭证分录规则的完整实现
    /// - 多币种和汇率处理
    /// - 混合业务识别（既有收入又有支出的结算单）
    /// - 手续费、预收款、汇兑损益等复杂业务场景
    /// - 导出防重机制（基于IFinancialExportable接口）
    /// - 权限控制和数据隔离
    /// - 异步任务处理机制
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region HTTP接口 - 收款结算单导出

        /// <summary>
        /// 导出收款结算单为金蝶DBF格式文件
        /// 支持七种凭证分录规则的完整实现，处理复杂的多币种和混合业务场景
        /// 注意：自动过滤已导出的数据（ExportedDateTime不为null），导出后自动标记导出时间和用户
        /// </summary>
        /// <param name="model">导出参数，包含查询条件和用户令牌</param>
        /// <returns>导出任务信息，包含任务ID用于跟踪进度</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<ExportSettlementReceiptReturnDto> ExportSettlementReceipt(ExportSettlementReceiptParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ExportSettlementReceiptReturnDto();
            try
            {
                _Logger.LogInformation("开始处理收款结算单导出请求，用户: {UserId}, 组织: {OrgId}",
                    context.User.Id, context.User.OrgId);

                // 1. 权限验证：使用财务接口权限 - 修复：通过ServiceProvider获取AuthorizationManager
                var authorizationManager = _ServiceProvider.GetRequiredService<AuthorizationManager>();
                if (!authorizationManager.Demand(out var err, "F.6"))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = $"权限不足：{err}";
                    return result;
                }

                // 2. 预检查科目配置完整性
                var missingConfigs = ValidateSettlementReceiptSubjectConfiguration(context.User.OrgId);
                if (missingConfigs.Any())
                {
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = $"科目配置不完整，缺少以下配置：{string.Join(", ", missingConfigs)}";
                    return result;
                }

                // 3. 构建查询条件 - 只查询收款结算单（IO=true）且未导出的
                var exportManager = _ServiceProvider.GetRequiredService<FinancialSystemExportManager>();
                var conditions = model.ExportConditions ?? new Dictionary<string, string>();

                // 强制限制为收款结算单
                conditions["IO"] = "true";

                // 4. 预检查收款结算单数量 - 使用Manager方法过滤未导出数据
                var baseQuery = _DbContext.PlInvoicess.AsQueryable();
                var settlementReceiptsQuery = exportManager.FilterUnexported(baseQuery);

                // 应用查询条件
                if (conditions.Any())
                {
                    settlementReceiptsQuery = EfHelper.GenerateWhereAnd(settlementReceiptsQuery, conditions);
                }

                // 应用组织权限过滤
                settlementReceiptsQuery = ApplyOrganizationFilterForSettlementReceipts(settlementReceiptsQuery, context.User);

                var settlementReceiptCount = settlementReceiptsQuery.Count();
                if (settlementReceiptCount == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "没有找到符合条件的收款结算单数据，请调整查询条件";
                    return result;
                }

                // 5. 预估凭证分录数量（基于七种分录规则，至少2个必须分录，最多7个）
                var estimatedVoucherEntryCount = settlementReceiptCount * 3; // 平均每个结算单3个分录

                // 6. 创建异步导出任务
                var taskService = _ServiceProvider.GetRequiredService<OwTaskService<PowerLmsUserDbContext>>();
                var exportDateTime = DateTime.UtcNow;

                var taskParameters = new Dictionary<string, string>
                {
                    ["ExportConditions"] = JsonSerializer.Serialize(conditions),
                    ["UserId"] = context.User.Id.ToString(),
                    ["OrgId"] = context.User.OrgId?.ToString() ?? "",
                    ["ExpectedSettlementReceiptCount"] = settlementReceiptCount.ToString(),
                    ["ExpectedVoucherEntryCount"] = estimatedVoucherEntryCount.ToString(),
                    ["ExportDateTime"] = exportDateTime.ToString("O"),
                    ["DisplayName"] = model.DisplayName ?? "",
                    ["Remark"] = model.Remark ?? ""
                };

                var taskId = taskService.CreateTask(typeof(FinancialSystemExportController),
                    nameof(ProcessSettlementReceiptDbfExportTask),
                    taskParameters,
                    context.User.Id,
                    context.User.OrgId);

                // 7. 返回成功结果
                result.TaskId = taskId;
                result.Message = $"收款结算单导出任务已创建成功";
                result.DebugMessage = $"导出任务已创建，预计处理 {settlementReceiptCount} 个收款结算单，生成 {estimatedVoucherEntryCount} 条凭证分录。可通过系统任务状态查询接口跟踪进度。";
                result.ExpectedSettlementReceiptCount = settlementReceiptCount;
                result.ExpectedVoucherEntryCount = estimatedVoucherEntryCount;

                _Logger.LogInformation("收款结算单导出任务创建成功，任务ID: {TaskId}, 结算单数量: {ReceiptCount}",
                    taskId, settlementReceiptCount);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "处理收款结算单导出请求时发生错误，用户: {UserId}", context.User.Id);

                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"导出请求处理失败: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region 静态任务处理方法 - 收款结算单导出

        /// <summary>
        /// 处理收款结算单DBF导出任务（静态方法，由OwTaskService调用）
        /// 实现七种凭证分录规则的完整业务逻辑
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="parameters">任务参数</param>
        /// <param name="serviceProvider">服务提供者（由OwTaskService自动注入）</param>
        /// <returns>任务执行结果</returns>
        public static object ProcessSettlementReceiptDbfExportTask(Guid taskId, Dictionary<string, string> parameters, IServiceProvider serviceProvider)
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
                var subjectConfigs = LoadSettlementReceiptSubjectConfigurations(dbContext, orgId);
                if (!subjectConfigs.Any())
                {
                    throw new InvalidOperationException("收款结算单科目配置不完整，无法生成凭证");
                }

                currentStep = "查询用户信息";
                var taskUser = dbContext.Accounts.Find(userId) ??
                    throw new InvalidOperationException($"未找到用户 {userId}");

                currentStep = "构建收款结算单查询";
                var exportManager = serviceProvider.GetRequiredService<FinancialSystemExportManager>();
                var baseQuery = dbContext.PlInvoicess.AsQueryable();
                var settlementReceiptsQuery = exportManager.FilterUnexported(baseQuery);

                if (conditions.Any())
                {
                    settlementReceiptsQuery = EfHelper.GenerateWhereAnd(settlementReceiptsQuery, conditions);
                }

                // 应用组织权限过滤
                settlementReceiptsQuery = ApplyOrganizationFilterForSettlementReceiptsStatic(settlementReceiptsQuery, taskUser, dbContext, serviceProvider);

                currentStep = "查询收款结算单数据";
                var settlementReceipts = settlementReceiptsQuery.ToList();
                if (!settlementReceipts.Any())
                {
                    throw new InvalidOperationException("没有找到符合条件的收款结算单数据");
                }

                currentStep = "查询收款结算单明细";
                var settlementReceiptIds = settlementReceipts.Select(r => r.Id).ToList();
                var items = dbContext.PlInvoicesItems
                    .Where(item => settlementReceiptIds.Contains(item.ParentId.Value))
                    .ToList();

                if (!items.Any())
                {
                    throw new InvalidOperationException("没有找到收款结算单明细项");
                }

                // 按收款结算单ID分组明细项
                var itemsDict = items.GroupBy(item => item.ParentId.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                currentStep = "计算收款结算单业务数据";
                var calculationResults = CalculateSettlementReceiptData(settlementReceipts, itemsDict, dbContext, businessLogicManager);

                currentStep = "生成金蝶凭证分录";
                var allVouchers = GenerateSettlementReceiptVouchersStatic(calculationResults, subjectConfigs, dbContext);

                if (!allVouchers.Any())
                {
                    throw new InvalidOperationException("没有生成任何凭证记录");
                }

                currentStep = "验证凭证数据完整性";
                ValidateVoucherDataIntegrity(allVouchers);

                currentStep = "生成DBF文件";
                var fileName = $"SettlementReceipt_Export_{DateTime.Now:yyyyMMdd_HHmmss}.dbf";

                var kingdeeFieldMappings = GetSettlementReceiptKingdeeFieldMappings();
                var customFieldTypes = GetSettlementReceiptKingdeeFieldTypes();

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
                        displayName : $"收款结算单导出-{DateTime.Now:yyyy年MM月dd日}";
                    var finalRemark = !string.IsNullOrWhiteSpace(remark) ?
                        remark : $"收款结算单DBF导出文件，共{settlementReceipts.Count}个收款结算单，{allVouchers.Count}条会计分录，导出时间：{exportDateTime:yyyy-MM-dd HH:mm:ss}";

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

                currentStep = "标记收款结算单为已导出";
                // 使用FinancialSystemExportManager的标准方法标记已导出
                var markedCount = exportManager.MarkAsExported(settlementReceipts, userId);
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
                    SettlementReceiptCount = settlementReceipts.Count,
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
                var contextualError = $"收款结算单DBF导出任务失败，当前步骤: {currentStep}, 任务ID: {taskId}";
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

        #region 收款结算单核心业务逻辑

        /// <summary>
        /// 计算收款结算单业务数据（12分录模型）
        /// 支持国内外客户分类、代垫费用区分、多笔收款处理
        /// </summary>
        private static List<SettlementReceiptCalculationDto> CalculateSettlementReceiptData(
            List<PlInvoices> settlementReceipts,
            Dictionary<Guid, List<PlInvoicesItem>> itemsDict,
            PowerLmsUserDbContext dbContext,
            BusinessLogicManager businessLogicManager)
        {
            var results = new List<SettlementReceiptCalculationDto>();

            // 批量加载所有关联数据
            var settlementReceiptIds = settlementReceipts.Select(r => r.Id).ToList();

            // 修复：先ToList()再ToDictionary()
            var customerIds = settlementReceipts.Select(r => r.JiesuanDanweiId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allCustomers = dbContext.PlCustomers.Where(c => customerIds.Contains(c.Id)).ToList().ToDictionary(c => c.Id);

            var allRequisitionItemIds = itemsDict.Values.SelectMany(items => items.Select(i => i.RequisitionItemId)).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allRequisitionItems = dbContext.DocFeeRequisitionItems.Where(ri => allRequisitionItemIds.Contains(ri.Id)).ToList().ToDictionary(ri => ri.Id);

            var allFeeIds = allRequisitionItems.Values.Select(ri => ri.FeeId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allFees = dbContext.DocFees.Where(f => allFeeIds.Contains(f.Id)).ToList().ToDictionary(f => f.Id);

            var allFeeTypeIds = allFees.Values.Select(f => f.FeeTypeId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allFeeTypes = dbContext.DD_FeesTypes.Where(ft => allFeeTypeIds.Contains(ft.Id)).ToList().ToDictionary(ft => ft.Id);

            // 修复：先ToList()再分组、转字典
            var allTransactions = dbContext.ActualFinancialTransactions
                .Where(t => settlementReceiptIds.Contains(t.ParentId.Value) && !t.IsDelete)
                .OrderBy(t => t.TransactionDate)
                .ToList();
            var transactionsGrouped = allTransactions.GroupBy(t => t.ParentId.Value).ToDictionary(g => g.Key, g => g.ToList());

            var allBankAccountIds = allTransactions.Select(t => t.BankAccountId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allBankAccounts = dbContext.BankInfos.Where(b => allBankAccountIds.Contains(b.Id)).ToList().ToDictionary(b => b.Id);

            var allBankIds = settlementReceipts.Select(r => r.BankId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            var allBanks = dbContext.BankInfos.Where(b => allBankIds.Contains(b.Id)).ToList().ToDictionary(b => b.Id);

            foreach (var receipt in settlementReceipts)
            {
                var items = itemsDict.GetValueOrDefault(receipt.Id, new List<PlInvoicesItem>());
                var baseCurrency = businessLogicManager.GetBaseCurrencyCode(receipt, dbContext) ?? "CNY";
                var customer = allCustomers.GetValueOrDefault(receipt.JiesuanDanweiId ?? Guid.Empty);
                var customerName = customer?.Name_Name ?? "未知客户";
                var customerFinanceCode = customer?.FinanceCodeAR ?? "";
                var isDomestic = customer?.IsDomestic ?? true;
                var bankInfo = allBanks.GetValueOrDefault(receipt.BankId ?? Guid.Empty);
                var actualTransactions = transactionsGrouped.GetValueOrDefault(receipt.Id, new List<ActualFinancialTransaction>());
                var (receivableForeign, receivableDomesticCustomer, receivableDomesticTariff, payableForeign, payableDomesticCustomer, payableDomesticTariff, isMixed, itemCalculations) = CalculateAmountsWithClassification(items, allRequisitionItems, allFees, allFeeTypes, isDomestic);
                var actualTransactionDtos = actualTransactions.Select(t => new ActualFinancialTransactionDto
                {
                    TransactionId = t.Id,
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    BankAccountId = t.BankAccountId,
                    BankSubjectCode = t.BankAccountId.HasValue && allBankAccounts.ContainsKey(t.BankAccountId.Value) ? allBankAccounts[t.BankAccountId.Value].AAccountSubjectCode : null,
                    ServiceFee = t.ServiceFee
                }).ToList();
                var calculation = new SettlementReceiptCalculationDto
                {
                    SettlementReceiptId = receipt.Id,
                    CustomerName = customerName,
                    CustomerFinanceCode = customerFinanceCode,
                    IsDomestic = isDomestic,
                    ReceiptNumber = receipt.IoPingzhengNo ?? $"SR{receipt.Id.ToString("N")[..8]}",
                    ReceiptDate = receipt.IoDateTime ?? DateTime.Now,
                    SettlementCurrency = receipt.Currency ?? baseCurrency,
                    BaseCurrency = baseCurrency,
                    SettlementExchangeRate = receipt.PaymentExchangeRate ?? 1.0m,
                    ReceivableForeignBaseCurrency = receivableForeign,
                    ReceivableDomesticCustomerBaseCurrency = receivableDomesticCustomer,
                    ReceivableDomesticTariffBaseCurrency = receivableDomesticTariff,
                    PayableForeignBaseCurrency = payableForeign,
                    PayableDomesticCustomerBaseCurrency = payableDomesticCustomer,
                    PayableDomesticTariffBaseCurrency = payableDomesticTariff,
                    AdvancePaymentAmount = receipt.AdvancePaymentAmount ?? 0m,
                    AdvancePaymentBaseCurrency = receipt.AdvancePaymentBaseCurrencyAmount ?? 0m,
                    ExchangeLoss = receipt.ExchangeLoss,
                    ServiceFeeAmount = receipt.ServiceFeeAmount ?? 0m,
                    ServiceFeeBaseCurrency = receipt.ServiceFeeBaseCurrencyAmount ?? 0m,
                    AdvanceOffsetReceivableAmount = receipt.AdvanceOffsetReceivableAmount ?? 0m,
                    AdvanceOffsetReceivableBaseCurrency = (receipt.AdvanceOffsetReceivableAmount ?? 0m) * (receipt.PaymentExchangeRate ?? 1.0m),
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
        /// 计算应收应付金额并按国内外客户、代垊属性分类
        /// </summary>
        private static (decimal ReceivableForeign, decimal ReceivableDomesticCustomer, decimal ReceivableDomesticTariff, decimal PayableForeign, decimal PayableDomesticCustomer, decimal PayableDomesticTariff, bool IsMixed, List<SettlementReceiptItemDto> ItemCalculations) CalculateAmountsWithClassification(List<PlInvoicesItem> items, Dictionary<Guid, DocFeeRequisitionItem> requisitionItemDict, Dictionary<Guid, DocFee> feeDict, Dictionary<Guid, FeesType> feeTypeDict, bool isDomestic)
        {
            var itemCalculations = new List<SettlementReceiptItemDto>();
            var incomeCount = 0;
            var expenseCount = 0;
            var receivableForeign = 0m;
            var receivableDomesticCustomer = 0m;
            var receivableDomesticTariff = 0m;
            var payableForeign = 0m;
            var payableDomesticCustomer = 0m;
            var payableDomesticTariff = 0m;
            foreach (var item in items)
            {
                var requisitionItem = item.RequisitionItemId.HasValue ? requisitionItemDict.GetValueOrDefault(item.RequisitionItemId.Value) : null;
                var fee = requisitionItem != null && requisitionItem.FeeId.HasValue && feeDict.ContainsKey(requisitionItem.FeeId.Value) ? feeDict[requisitionItem.FeeId.Value] : null;
                var feeType = fee != null && fee.FeeTypeId.HasValue && feeTypeDict.ContainsKey(fee.FeeTypeId.Value) ? feeTypeDict[fee.FeeTypeId.Value] : null;
                var isIncome = fee?.IO ?? true;
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
                itemCalculations.Add(new SettlementReceiptItemDto
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
            return (receivableForeign, receivableDomesticCustomer, receivableDomesticTariff, payableForeign, payableDomesticCustomer, payableDomesticTariff, isMixed, itemCalculations);
        }

        /// <summary>
        /// 生成收款结算单金蝶凭证分录（12分录模型静态方法）
        /// 实现11个独立函数的完整业务逻辑
        /// </summary>
        private static List<KingdeeVoucher> GenerateSettlementReceiptVouchersStatic(
            List<SettlementReceiptCalculationDto> calculations,
            Dictionary<string, SubjectConfiguration> subjectConfigs,
            PowerLmsUserDbContext dbContext)
        {
            var vouchers = new List<KingdeeVoucher>();
            var voucherNumber = 1;
            var preparerName = subjectConfigs.GetValueOrDefault("SR_PREPARER")?.DisplayName ?? "系统导出";
            var voucherGroup = subjectConfigs.GetValueOrDefault("SR_VOUCHER_GROUP")?.VoucherGroup ?? "银";
            foreach (var calculation in calculations)
            {
                var entryId = 0;
                vouchers.AddRange(GenerateBankReceiptVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                vouchers.AddRange(GenerateReceivableVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                if (calculation.IsMixedBusiness)
                {
                    vouchers.AddRange(GeneratePayableVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                }
                vouchers.AddRange(GenerateOtherVouchers(calculation, voucherNumber, ref entryId, voucherGroup, preparerName, subjectConfigs));
                voucherNumber++;
            }
            return vouchers;
        }

        #region 收款单12分录生成函数

        /// <summary>
        /// 规则1：生成银行收款借方分录（1~N条必生成）
        /// 优先使用ActualFinancialTransaction表，无记录则用PlInvoicesItem
        /// </summary>
        private static List<KingdeeVoucher> GenerateBankReceiptVouchers(SettlementReceiptCalculationDto calculation, int voucherNumber, ref int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.ActualTransactions.Any())
            {
                foreach (var transaction in calculation.ActualTransactions)
                {
                    var bankSubject = GetValidSubjectCode(transaction.BankSubjectCode, calculation.BankInfo?.AAccountSubjectCode, subjectConfigs.GetValueOrDefault("SR_BANK_DEBIT")?.SubjectNumber, "1001001");
                    var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    voucher.FACCTID = bankSubject;
                    voucher.FDC = 0;
                    voucher.FFCYAMT = transaction.Amount;
                    voucher.FDEBIT = transaction.Amount * calculation.SettlementExchangeRate;
                    voucher.FCREDIT = 0;
                    voucher.FCYID = calculation.SettlementCurrency;
                    voucher.FEXCHRATE = calculation.SettlementExchangeRate;
                    vouchers.Add(voucher);
                }
            }
            else
            {
                foreach (var item in calculation.Items)
                {
                    var bankSubject = GetValidSubjectCode(calculation.BankInfo?.AAccountSubjectCode, subjectConfigs.GetValueOrDefault("SR_BANK_DEBIT")?.SubjectNumber, "1001001");
                    var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    voucher.FACCTID = bankSubject;
                    voucher.FDC = 0;
                    voucher.FFCYAMT = item.Amount;
                    voucher.FDEBIT = item.SettlementAmountBaseCurrency;
                    voucher.FCREDIT = 0;
                    voucher.FCYID = calculation.SettlementCurrency;
                    voucher.FEXCHRATE = calculation.SettlementExchangeRate;
                    vouchers.Add(voucher);
                }
            }
            return vouchers;
        }

        /// <summary>
        /// 规则2：生成应收账款冲抵贷方分录（1~3条必生成）
        /// </summary>
        private static List<KingdeeVoucher> GenerateReceivableVouchers(SettlementReceiptCalculationDto calculation, int voucherNumber, ref int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.IsDomestic)
            {
                if (calculation.ReceivableDomesticCustomerBaseCurrency > 0)
                {
                    vouchers.Add(GenerateReceivableDomesticCustomerVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
                if (calculation.ReceivableDomesticTariffBaseCurrency > 0)
                {
                    vouchers.Add(GenerateReceivableDomesticTariffVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            else
            {
                if (calculation.ReceivableForeignBaseCurrency > 0)
                {
                    vouchers.Add(GenerateReceivableForeignVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            return vouchers;
        }

        /// <summary>
        /// 规则2A：应收账款-国外客户
        /// </summary>
        private static KingdeeVoucher GenerateReceivableForeignVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_RECEIVABLE_CREDIT_OUT_CUS")?.SubjectNumber, subjectConfigs.GetValueOrDefault("SR_RECEIVABLE_CREDIT")?.SubjectNumber, "113001");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则2B：应收账款-国内客户（非代垫）
        /// </summary>
        private static KingdeeVoucher GenerateReceivableDomesticCustomerVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_RECEIVABLE_CREDIT_IN_CUS")?.SubjectNumber, subjectConfigs.GetValueOrDefault("SR_RECEIVABLE_CREDIT")?.SubjectNumber, "113001");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则2C：应收账款-国内关税（代垫）
        /// </summary>
        private static KingdeeVoucher GenerateReceivableDomesticTariffVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_RECEIVABLE_CREDIT_IN_TAR")?.SubjectNumber, subjectConfigs.GetValueOrDefault("SR_RECEIVABLE_CREDIT")?.SubjectNumber, "113001");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则3：生成应付账款冲抵借方分录（0~3条混合业务生成）
        /// </summary>
        private static List<KingdeeVoucher> GeneratePayableVouchers(SettlementReceiptCalculationDto calculation, int voucherNumber, ref int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.IsDomestic)
            {
                if (calculation.PayableDomesticCustomerBaseCurrency > 0)
                {
                    vouchers.Add(GeneratePayableDomesticCustomerVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
                if (calculation.PayableDomesticTariffBaseCurrency > 0)
                {
                    vouchers.Add(GeneratePayableDomesticTariffVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            else
            {
                if (calculation.PayableForeignBaseCurrency > 0)
                {
                    vouchers.Add(GeneratePayableForeignVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
                }
            }
            return vouchers;
        }

        /// <summary>
        /// 规则3A：应付账款-国外客户（混合业务）
        /// </summary>
        private static KingdeeVoucher GeneratePayableForeignVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_PAYABLE_DEBIT_OUT_CUS")?.SubjectNumber, subjectConfigs.GetValueOrDefault("SR_PAYABLE_DEBIT")?.SubjectNumber, "203001");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则3B：应付账款-国内客户（非代垫，混合业务）
        /// </summary>
        private static KingdeeVoucher GeneratePayableDomesticCustomerVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_PAYABLE_DEBIT_IN_CUS")?.SubjectNumber, subjectConfigs.GetValueOrDefault("SR_PAYABLE_DEBIT")?.SubjectNumber, "203001");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则3C：应付账款-国内关税（代垫，混合业务）
        /// </summary>
        private static KingdeeVoucher GeneratePayableDomesticTariffVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_PAYABLE_DEBIT_IN_TAR")?.SubjectNumber, subjectConfigs.GetValueOrDefault("SR_PAYABLE_DEBIT")?.SubjectNumber, "203001");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则4-7：生成其他科目分录（条件生成）
        /// </summary>
        private static List<KingdeeVoucher> GenerateOtherVouchers(SettlementReceiptCalculationDto calculation, int voucherNumber, ref int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var vouchers = new List<KingdeeVoucher>();
            if (calculation.AdvancePaymentAmount > 0)
            {
                vouchers.Add(GenerateAdvanceReceiptVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
            }
            if (calculation.ExchangeLoss != 0)
            {
                vouchers.Add(GenerateExchangeLossVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
            }
            if (calculation.ServiceFeeAmount > 0 || calculation.ServiceFeeBaseCurrency > 0)
            {
                vouchers.Add(GenerateServiceFeeVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
            }
            if (calculation.AdvanceOffsetReceivableAmount > 0)
            {
                vouchers.Add(GenerateAdvanceOffsetVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName, subjectConfigs));
            }
            return vouchers;
        }

        /// <summary>
        /// 规则4：预收款贷方分录
        /// </summary>
        private static KingdeeVoucher GenerateAdvanceReceiptVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_ADVANCE_CREDIT")?.SubjectNumber, "203101");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 1;
            voucher.FFCYAMT = calculation.AdvancePaymentAmount;
            voucher.FDEBIT = 0;
            voucher.FCREDIT = calculation.AdvancePaymentBaseCurrency;
            voucher.FCYID = calculation.SettlementCurrency;
            voucher.FEXCHRATE = calculation.SettlementExchangeRate;
            return voucher;
        }

        /// <summary>
        /// 规则5：汇兑损益分录
        /// </summary>
        private static KingdeeVoucher GenerateExchangeLossVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_EXCHANGE_LOSS")?.SubjectNumber, "603001");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则6：手续费借方分录
        /// </summary>
        private static KingdeeVoucher GenerateServiceFeeVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_SERVICE_FEE_DEBIT")?.SubjectNumber, "603002");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
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
        /// 规则7：预收冲应收借方分录
        /// </summary>
        private static KingdeeVoucher GenerateAdvanceOffsetVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName, Dictionary<string, SubjectConfiguration> subjectConfigs)
        {
            var subject = GetValidSubjectCode(subjectConfigs.GetValueOrDefault("SR_ADVANCE_OFFSET_DEBIT")?.SubjectNumber, "203101");
            var voucher = CreateBaseVoucher(calculation, voucherNumber, entryId, voucherGroup, preparerName);
            voucher.FACCTID = subject;
            voucher.FCLSNAME1 = "客户";
            voucher.FOBJID1 = calculation.CustomerFinanceCode;
            voucher.FOBJNAME1 = calculation.CustomerName;
            voucher.FTRANSID = calculation.CustomerFinanceCode;
            voucher.FDC = 0;
            voucher.FFCYAMT = calculation.AdvanceOffsetReceivableAmount;
            voucher.FDEBIT = calculation.AdvanceOffsetReceivableBaseCurrency;
            voucher.FCREDIT = 0;
            voucher.FCYID = calculation.SettlementCurrency;
            voucher.FEXCHRATE = calculation.SettlementExchangeRate;
            return voucher;
        }

        #endregion 收款单12分录生成函数

        #region 收款单辅助方法

        /// <summary>
        /// 创建收款单基础凭证对象
        /// </summary>
        private static KingdeeVoucher CreateBaseVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, int entryId, string voucherGroup, string preparerName)
        {
            var summary = $"{calculation.CustomerName}【收入】{calculation.ReceiptNumber}";
            return new KingdeeVoucher
            {
                Id = Guid.NewGuid(),
                FDATE = calculation.ReceiptDate,
                FTRANSDATE = calculation.ReceiptDate,
                FPERIOD = calculation.ReceiptDate.Month,
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

        #endregion 收款单辅助方法

        #region 配置验证和组织权限辅助方法

        /// <summary>
        /// 验证收款结算单科目配置完整性
        /// </summary>
        private List<string> ValidateSettlementReceiptSubjectConfiguration(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SR_RECEIVABLE_CREDIT", "SR_RECEIVABLE_CREDIT_IN_CUS", "SR_RECEIVABLE_CREDIT_IN_TAR", "SR_RECEIVABLE_CREDIT_OUT_CUS",
                "SR_PAYABLE_DEBIT", "SR_PAYABLE_DEBIT_IN_CUS", "SR_PAYABLE_DEBIT_IN_TAR", "SR_PAYABLE_DEBIT_OUT_CUS",
                "SR_ADVANCE_CREDIT", "SR_EXCHANGE_LOSS", "SR_SERVICE_FEE_DEBIT", "SR_ADVANCE_OFFSET_DEBIT",
                "SR_PREPARER", "SR_VOUCHER_GROUP"
            };
            var existingCodes = _DbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();
            return requiredCodes.Except(existingCodes).ToList();
        }

        /// <summary>
        /// 收款结算单组织权限过滤
        /// </summary>
        private IQueryable<PlInvoices> ApplyOrganizationFilterForSettlementReceipts(IQueryable<PlInvoices> query, Account user)
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
        /// 加载收款结算单科目配置（静态版本）
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadSettlementReceiptSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SR_RECEIVABLE_CREDIT", "SR_RECEIVABLE_CREDIT_IN_CUS", "SR_RECEIVABLE_CREDIT_IN_TAR", "SR_RECEIVABLE_CREDIT_OUT_CUS",
                "SR_PAYABLE_DEBIT", "SR_PAYABLE_DEBIT_IN_CUS", "SR_PAYABLE_DEBIT_IN_TAR", "SR_PAYABLE_DEBIT_OUT_CUS",
                "SR_ADVANCE_CREDIT", "SR_EXCHANGE_LOSS", "SR_SERVICE_FEE_DEBIT", "SR_ADVANCE_OFFSET_DEBIT",
                "SR_PREPARER", "SR_VOUCHER_GROUP"
            };
            var configs = dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .ToList();
            return configs.ToDictionary(c => c.Code, c => c);
        }

        /// <summary>
        /// 收款结算单组织权限过滤（静态版本）
        /// </summary>
        private static IQueryable<PlInvoices> ApplyOrganizationFilterForSettlementReceiptsStatic(IQueryable<PlInvoices> query, Account user,
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
        /// 获取收款结算单专用的金蝶字段映射
        /// </summary>
        private static Dictionary<string, string> GetSettlementReceiptKingdeeFieldMappings()
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
        /// 获取收款结算单专用的金蝶字段类型定义
        /// </summary>
        private static Dictionary<string, NativeDbType> GetSettlementReceiptKingdeeFieldTypes()
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

        #endregion 配置验证和组织权限辅助方法

        #endregion 收款结算单核心业务逻辑
    }
}
