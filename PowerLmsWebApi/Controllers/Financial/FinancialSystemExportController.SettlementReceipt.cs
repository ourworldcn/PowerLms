/*
 * 项目：PowerLms财务系统 | 模块：收款结算单导出金蝶功能
 * 功能：将收款结算单转换为符合金蝶财务软件要求的会计凭证分录
 * 技术要点：七种凭证分录规则、多币种处理、混合业务识别、汇率计算
 * 作者：zc | 创建：2025-01 | 修改：2025-01-31 收款结算单导出功能实施
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
    /// - 权限控制和数据隔离
    /// - 异步任务处理机制
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region HTTP接口 - 收款结算单导出

        /// <summary>
        /// 导出收款结算单为金蝶DBF格式文件
        /// 支持七种凭证分录规则的完整实现，处理复杂的多币种和混合业务场景
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
                var conditions = model.ExportConditions ?? new Dictionary<string, string>();
                
                // 强制限制为收款结算单且未导出
                conditions["IO"] = "true";
                if (!conditions.ContainsKey("ConfirmDateTime"))
                {
                    conditions["ConfirmDateTime"] = "null"; // 只导出未确认（未导出）的结算单
                }

                // 4. 预检查收款结算单数量
                var settlementReceiptsQuery = _DbContext.PlInvoicess.AsQueryable();
                
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
                var settlementReceiptsQuery = dbContext.PlInvoicess.AsQueryable();
                
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

                currentStep = "更新导出状态";
                // 标记收款结算单为已导出
                var now = DateTime.UtcNow;
                foreach (var receipt in settlementReceipts)
                {
                    receipt.ConfirmDateTime = now;
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
                    SettlementReceiptCount = settlementReceipts.Count,
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
        /// 计算收款结算单业务数据
        /// 包括应收应付金额计算、混合业务识别、本位币转换等
        /// </summary>
        private static List<SettlementReceiptCalculationDto> CalculateSettlementReceiptData(
            List<PlInvoices> settlementReceipts,
            Dictionary<Guid, List<PlInvoicesItem>> itemsDict,
            PowerLmsUserDbContext dbContext,
            BusinessLogicManager businessLogicManager)
        {
            var results = new List<SettlementReceiptCalculationDto>();

            foreach (var receipt in settlementReceipts)
            {
                var items = itemsDict.GetValueOrDefault(receipt.Id, new List<PlInvoicesItem>());
                
                // 获取本位币代码
                var baseCurrency = businessLogicManager.GetBaseCurrencyCode(receipt, dbContext) ?? "CNY";
                
                // 获取往来单位信息
                var customer = dbContext.PlCustomers.Find(receipt.JiesuanDanweiId);
                var customerName = customer?.Name_Name ?? "未知客户";
                var customerFinanceCode = customer?.FinanceCodeAR ?? "";

                // 获取银行信息
                var bankInfo = dbContext.BankInfos.Find(receipt.BankId);

                // 修复：计算应收应付金额和识别混合业务，使用正确的汇率计算
                var (receivableTotal, payableTotal, isMixed, itemCalculations) = CalculateAmountsAndIdentifyMixedBusiness(items, dbContext, receipt.PaymentExchangeRate ?? 1.0m);

                var calculation = new SettlementReceiptCalculationDto
                {
                    SettlementReceiptId = receipt.Id,
                    CustomerName = customerName,
                    CustomerFinanceCode = customerFinanceCode,
                    ReceiptNumber = receipt.IoPingzhengNo ?? $"SR{receipt.Id.ToString("N")[..8]}",
                    ReceiptDate = receipt.IoDateTime ?? DateTime.Now,
                    SettlementCurrency = receipt.Currency ?? baseCurrency,
                    BaseCurrency = baseCurrency,
                    SettlementExchangeRate = receipt.PaymentExchangeRate ?? 1.0m,
                    ReceivableTotalBaseCurrency = receivableTotal,
                    PayableTotalBaseCurrency = payableTotal,
                    AdvancePaymentAmount = receipt.AdvancePaymentAmount ?? 0,
                    AdvancePaymentBaseCurrency = receipt.AdvancePaymentBaseCurrencyAmount ?? 0,
                    ExchangeLoss = receipt.ExchangeLoss,
                    ServiceFeeAmount = receipt.ServiceFeeAmount ?? 0,
                    ServiceFeeBaseCurrency = receipt.ServiceFeeBaseCurrencyAmount ?? 0,
                    AdvanceOffsetReceivableAmount = receipt.AdvanceOffsetReceivableAmount ?? 0,
                    AdvanceOffsetReceivableBaseCurrency = (receipt.AdvanceOffsetReceivableAmount ?? 0) * (receipt.PaymentExchangeRate ?? 1.0m),
                    IsMixedBusiness = isMixed,
                    BankInfo = bankInfo,
                    Items = itemCalculations
                };

                results.Add(calculation);
            }

            return results;
        }

        /// <summary>
        /// 计算应收应付金额并识别混合业务
        /// </summary>
        private static (decimal ReceivableTotal, decimal PayableTotal, bool IsMixed, List<SettlementReceiptItemDto> ItemCalculations) 
            CalculateAmountsAndIdentifyMixedBusiness(List<PlInvoicesItem> items, PowerLmsUserDbContext dbContext, decimal settlementExchangeRate)
        {
            var itemCalculations = new List<SettlementReceiptItemDto>();
            var incomeCount = 0;
            var expenseCount = 0;
            var receivableTotal = 0m;
            var payableTotal = 0m;

            foreach (var item in items)
            {
                // 通过申请单明细获取原费用信息
                var requisitionItem = dbContext.DocFeeRequisitionItems.Find(item.RequisitionItemId);
                var fee = requisitionItem != null ? dbContext.DocFees.Find(requisitionItem.FeeId) : null;

                var isIncome = fee?.IO ?? true; // 默认认为是收入
                var originalFeeExchangeRate = fee?.ExchangeRate ?? 1.0m;

                // 统计收入支出数量
                if (isIncome) incomeCount++;
                else expenseCount++;

                // 修复：使用结算汇率进行本位币金额计算
                var settlementAmountBaseCurrency = item.Amount * settlementExchangeRate;
                
                if (isIncome)
                    receivableTotal += settlementAmountBaseCurrency;
                else
                    payableTotal += settlementAmountBaseCurrency;

                itemCalculations.Add(new SettlementReceiptItemDto
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

            return (receivableTotal, payableTotal, isMixed, itemCalculations);
        }

        /// <summary>
        /// 生成收款结算单金蝶凭证分录（静态方法）
        /// 实现七种凭证分录规则的完整逻辑
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

                // 规则1：主营业务收款借方 (银行收款) - 必定生成
                foreach (var item in calculation.Items)
                {
                    // 修复：增强银行科目代码获取逻辑
                    var bankSubject = GetValidSubjectCode(
                        calculation.BankInfo?.AAccountSubjectCode,
                        subjectConfigs.GetValueOrDefault("SR_BANK_DEBIT")?.SubjectNumber,
                        "1001001"); // 默认银行存款科目
                    
                    var bankVoucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    bankVoucher.FACCTID = bankSubject;
                    bankVoucher.FDC = 0; // 借方
                    bankVoucher.FFCYAMT = item.Amount;
                    bankVoucher.FDEBIT = item.SettlementAmountBaseCurrency;
                    bankVoucher.FCREDIT = 0;
                    bankVoucher.FCYID = calculation.SettlementCurrency;
                    bankVoucher.FEXCHRATE = calculation.SettlementExchangeRate;

                    vouchers.Add(bankVoucher);
                }

                // 规则2：主营业务收款贷方 (应收冲抵) - 必定生成
                if (calculation.ReceivableTotalBaseCurrency > 0)
                {
                    var receivableSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SR_RECEIVABLE_CREDIT")?.SubjectNumber,
                        null,
                        "113001"); // 默认应收账款科目
                    
                    var receivableVoucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    receivableVoucher.FACCTID = receivableSubject;
                    receivableVoucher.FCLSNAME1 = "客户";
                    receivableVoucher.FOBJID1 = calculation.CustomerFinanceCode;
                    receivableVoucher.FOBJNAME1 = calculation.CustomerName;
                    receivableVoucher.FTRANSID = calculation.CustomerFinanceCode;
                    receivableVoucher.FDC = 1; // 贷方
                    receivableVoucher.FFCYAMT = calculation.ReceivableTotalBaseCurrency;
                    receivableVoucher.FDEBIT = 0;
                    receivableVoucher.FCREDIT = calculation.ReceivableTotalBaseCurrency;
                    receivableVoucher.FCYID = calculation.BaseCurrency;
                    receivableVoucher.FEXCHRATE = 1.0000000m;

                    vouchers.Add(receivableVoucher);
                }

                // 规则3：主营业务收款借方 (应付冲抵) - 条件生成
                if (calculation.IsMixedBusiness && calculation.PayableTotalBaseCurrency > 0)
                {
                    var payableSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SR_PAYABLE_DEBIT")?.SubjectNumber,
                        null,
                        "203001"); // 默认应付账款科目
                    
                    var payableVoucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    payableVoucher.FACCTID = payableSubject;
                    payableVoucher.FCLSNAME1 = "客户";
                    payableVoucher.FOBJID1 = calculation.CustomerFinanceCode;
                    payableVoucher.FOBJNAME1 = calculation.CustomerName;
                    payableVoucher.FTRANSID = calculation.CustomerFinanceCode;
                    payableVoucher.FDC = 0; // 借方
                    payableVoucher.FFCYAMT = calculation.PayableTotalBaseCurrency;
                    payableVoucher.FDEBIT = calculation.PayableTotalBaseCurrency;
                    payableVoucher.FCREDIT = 0;
                    payableVoucher.FCYID = calculation.BaseCurrency;
                    payableVoucher.FEXCHRATE = 1.0000000m;

                    vouchers.Add(payableVoucher);
                }

                // 规则4：主营业务收款预收贷方 (预收款) - 条件生成
                if (calculation.AdvancePaymentAmount > 0)
                {
                    var advanceSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SR_ADVANCE_CREDIT")?.SubjectNumber,
                        null,
                        "203101"); // 默认预收账款科目
                    
                    var advanceVoucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    advanceVoucher.FACCTID = advanceSubject;
                    advanceVoucher.FCLSNAME1 = "客户";
                    advanceVoucher.FOBJID1 = calculation.CustomerFinanceCode;
                    advanceVoucher.FOBJNAME1 = calculation.CustomerName;
                    advanceVoucher.FTRANSID = calculation.CustomerFinanceCode;
                    advanceVoucher.FDC = 1; // 贷方
                    advanceVoucher.FFCYAMT = calculation.AdvancePaymentAmount;
                    advanceVoucher.FDEBIT = 0;
                    advanceVoucher.FCREDIT = calculation.AdvancePaymentBaseCurrency;
                    advanceVoucher.FCYID = calculation.SettlementCurrency;
                    advanceVoucher.FEXCHRATE = calculation.SettlementExchangeRate;

                    vouchers.Add(advanceVoucher);
                }

                // 规则5：主营业务收款汇兑损益 - 条件生成
                if (calculation.ExchangeLoss != 0)
                {
                    var exchangeLossSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SR_EXCHANGE_LOSS")?.SubjectNumber,
                        null,
                        "603001"); // 默认汇兑损益科目
                    
                    var exchangeVoucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    exchangeVoucher.FACCTID = exchangeLossSubject;
                    exchangeVoucher.FDC = calculation.ExchangeLoss > 0 ? 0 : 1; // 正数借方，负数贷方
                    exchangeVoucher.FFCYAMT = Math.Abs(calculation.ExchangeLoss);
                    exchangeVoucher.FDEBIT = calculation.ExchangeLoss > 0 ? Math.Abs(calculation.ExchangeLoss) : 0;
                    exchangeVoucher.FCREDIT = calculation.ExchangeLoss < 0 ? Math.Abs(calculation.ExchangeLoss) : 0;
                    exchangeVoucher.FCYID = calculation.BaseCurrency;
                    exchangeVoucher.FEXCHRATE = 1.0000000m;

                    vouchers.Add(exchangeVoucher);
                }

                // 规则6：主营业务收款手续费借方 - 条件生成
                if (calculation.ServiceFeeAmount > 0)
                {
                    var serviceFeeSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SR_SERVICE_FEE_DEBIT")?.SubjectNumber,
                        null,
                        "603002"); // 默认财务费用科目
                    
                    var serviceFeeVoucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
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

                // 规则7：主营业务收款预收冲应收借方 - 条件生成
                if (calculation.AdvanceOffsetReceivableAmount > 0)
                {
                    var advanceOffsetSubject = GetValidSubjectCode(
                        subjectConfigs.GetValueOrDefault("SR_ADVANCE_OFFSET_DEBIT")?.SubjectNumber,
                        null,
                        "203101"); // 默认预收账款科目
                    
                    var advanceOffsetVoucher = CreateBaseVoucher(calculation, voucherNumber, entryId++, voucherGroup, preparerName);
                    advanceOffsetVoucher.FACCTID = advanceOffsetSubject;
                    advanceOffsetVoucher.FCLSNAME1 = "客户";
                    advanceOffsetVoucher.FOBJID1 = calculation.CustomerFinanceCode;
                    advanceOffsetVoucher.FOBJNAME1 = calculation.CustomerName;
                    advanceOffsetVoucher.FTRANSID = calculation.CustomerFinanceCode;
                    advanceOffsetVoucher.FDC = 0; // 借方
                    advanceOffsetVoucher.FFCYAMT = calculation.AdvanceOffsetReceivableAmount;
                    advanceOffsetVoucher.FDEBIT = calculation.AdvanceOffsetReceivableBaseCurrency;
                    advanceOffsetVoucher.FCREDIT = 0;
                    advanceOffsetVoucher.FCYID = calculation.SettlementCurrency;
                    advanceOffsetVoucher.FEXCHRATE = calculation.SettlementExchangeRate;

                    vouchers.Add(advanceOffsetVoucher);
                }

                voucherNumber++;
            }

            return vouchers;
        }

        /// <summary>
        /// 获取有效的科目代码，确保不为空且符合格式要求
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
            
            // 如果所有候选项都无效，抛出异常
            throw new InvalidOperationException("无法获取有效的科目代码，请检查科目配置");
        }

        /// <summary>
        /// 创建基础凭证对象
        /// </summary>
        private static KingdeeVoucher CreateBaseVoucher(SettlementReceiptCalculationDto calculation, int voucherNumber, 
            int entryId, string voucherGroup, string preparerName)
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
                FEXP = summary.Length > 500 ? summary.Substring(0, 500) : summary,
                FCYID = "RMB",
                FEXCHRATE = 1.0000000m,
                FPREPARE = preparerName,
                FMODULE = "GL",
                FDELETED = false
            };
        }

        /// <summary>
        /// 验证凭证数据完整性
        /// </summary>
        private static void ValidateVoucherDataIntegrity(List<KingdeeVoucher> vouchers)
        {
            var errors = new List<string>();
            
            // 按凭证号分组检查借贷平衡
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

            // 检查必需字段
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

        #endregion

        #region 收款结算单导出辅助方法

        /// <summary>
        /// 验证收款结算单科目配置完整性
        /// </summary>
        /// <param name="orgId">组织ID</param>
        /// <returns>缺失的科目配置代码列表</returns>
        private List<string> ValidateSettlementReceiptSubjectConfiguration(Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SR_BANK_DEBIT",           // 规则1：银行收款借方科目
                "SR_RECEIVABLE_CREDIT",    // 规则2：应收账款贷方科目
                "SR_PAYABLE_DEBIT",        // 规则3：应付账款借方科目（混合业务）
                "SR_ADVANCE_CREDIT",       // 规则4：预收款贷方科目
                "SR_EXCHANGE_LOSS",        // 规则5：汇兑损益科目
                "SR_SERVICE_FEE_DEBIT",    // 规则6：手续费借方科目
                "SR_ADVANCE_OFFSET_DEBIT", // 规则7：预收冲应收借方科目
                "SR_PREPARER",             // 制单人
                "SR_VOUCHER_GROUP"         // 凭证字
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
        /// <param name="query">查询对象</param>
        /// <param name="user">用户账号</param>
        /// <returns>过滤后的查询</returns>
        private IQueryable<PlInvoices> ApplyOrganizationFilterForSettlementReceipts(IQueryable<PlInvoices> query, Account user)
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

        #region 收款结算单导出静态辅助方法

        /// <summary>
        /// 加载收款结算单科目配置（静态版本）
        /// </summary>
        private static Dictionary<string, SubjectConfiguration> LoadSettlementReceiptSubjectConfigurations(PowerLmsUserDbContext dbContext, Guid? orgId)
        {
            var requiredCodes = new List<string>
            {
                "SR_BANK_DEBIT", "SR_RECEIVABLE_CREDIT", "SR_PAYABLE_DEBIT", "SR_ADVANCE_CREDIT",
                "SR_EXCHANGE_LOSS", "SR_SERVICE_FEE_DEBIT", "SR_ADVANCE_OFFSET_DEBIT",
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

        #endregion
    }
}