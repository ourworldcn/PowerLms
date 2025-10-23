/*
 * 项目：PowerLms财务系统 | 模块：付款结算单导出金蝶功能
 * 功能：付款结算单导出金蝶功能的DTO定义分部类
 * 技术要点：分部类模式、复杂业务DTO设计、12分录规则支持、国内外客户分类、代垫费用区分
 * 作者：zc | 创建：2025-01-31 | 修改：2025-01-31 付款结算单12分录模型实施
 */

using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers.Financial
{
    /// <summary>
    /// 财务系统导出控制器 - 付款结算单导出功能DTO定义
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region 付款结算单导出DTO

        /// <summary>
        /// 导出付款结算单为金蝶DBF格式文件参数DTO
        /// </summary>
        public class ExportSettlementPaymentParamsDto : TokenDtoBase
        {
            /// <summary>
            /// 导出查询条件。支持结算日期、币种、金额范围等过滤条件。
            /// </summary>
            public Dictionary<string, string> ExportConditions { get; set; }

            /// <summary>
            /// 导出格式，默认DBF
            /// </summary>
            public string ExportFormat { get; set; } = "DBF";

            /// <summary>
            /// 显示名称，可选，用于文件记录
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// 备注信息，可选，用于文件记录
            /// </summary>
            public string Remark { get; set; }
        }

        /// <summary>
        /// 导出付款结算单为金蝶DBF格式文件返回值DTO
        /// </summary>
        public class ExportSettlementPaymentReturnDto : ReturnDtoBase
        {
            /// <summary>
            /// 异步任务ID，用于跟踪导出进度
            /// </summary>
            public Guid? TaskId { get; set; }

            /// <summary>
            /// 预计导出的付款结算单数量
            /// </summary>
            public int ExpectedSettlementPaymentCount { get; set; }

            /// <summary>
            /// 预计生成的凭证分录数量（基于12分录规则）
            /// </summary>
            public int ExpectedVoucherEntryCount { get; set; }

            /// <summary>
            /// 操作结果消息
            /// </summary>
            public string Message { get; set; }
        }

        #endregion

        #region 付款结算单导出内部DTO

        /// <summary>
        /// 付款结算单导出计算结果DTO
        /// 支持12分录模型，包含国内外客户分类、代垫费用区分、多笔付款处理
        /// </summary>
        public class SettlementPaymentCalculationDto
        {
            /// <summary>
            /// 付款结算单ID
            /// </summary>
            public Guid SettlementPaymentId { get; set; }

            /// <summary>
            /// 往来单位名称（供应商/客户）
            /// </summary>
            public string CustomerName { get; set; }

            /// <summary>
            /// 往来单位财务编码（应付科目用）
            /// </summary>
            public string CustomerFinanceCode { get; set; }

            /// <summary>
            /// 往来单位国别属性（true=国内，false=国外，null按国内处理）
            /// </summary>
            public bool IsDomestic { get; set; }

            /// <summary>
            /// 付款单号
            /// </summary>
            public string PaymentNumber { get; set; }

            /// <summary>
            /// 付款日期
            /// </summary>
            public DateTime PaymentDate { get; set; }

            /// <summary>
            /// 结算币种
            /// </summary>
            public string SettlementCurrency { get; set; }

            /// <summary>
            /// 本位币代码
            /// </summary>
            public string BaseCurrency { get; set; }

            /// <summary>
            /// 结算单汇率
            /// </summary>
            public decimal SettlementExchangeRate { get; set; }

            /// <summary>
            /// 应付-国外供应商本位币金额
            /// 计算公式：sum(if 国外客户，明细.支出=true，明细本次结算金额 × 明细原费用汇率)
            /// </summary>
            public decimal PayableForeignBaseCurrency { get; set; }

            /// <summary>
            /// 应付-国内客户（非代垫）本位币金额
            /// 计算公式：sum(if 国内客户且非代垫，明细.支出=true，明细本次结算金额 × 明细原费用汇率)
            /// </summary>
            public decimal PayableDomesticCustomerBaseCurrency { get; set; }

            /// <summary>
            /// 应付-国内关税（代垫）本位币金额
            /// 计算公式：sum(if 国内客户且代垫，明细.支出=true，明细本次结算金额 × 明细原费用汇率)
            /// </summary>
            public decimal PayableDomesticTariffBaseCurrency { get; set; }

            /// <summary>
            /// 应收-国外客户本位币金额（混合业务）
            /// 计算公式：sum(if 国外客户，明细.收入=true，明细本次结算金额 × 明细原费用汇率)
            /// </summary>
            public decimal ReceivableForeignBaseCurrency { get; set; }

            /// <summary>
            /// 应收-国内客户（非代垫）本位币金额（混合业务）
            /// 计算公式：sum(if 国内客户且非代垫，明细.收入=true，明细本次结算金额 × 明细原费用汇率)
            /// </summary>
            public decimal ReceivableDomesticCustomerBaseCurrency { get; set; }

            /// <summary>
            /// 应收-国内关税（代垫）本位币金额（混合业务）
            /// 计算公式：sum(if 国内客户且代垫，明细.收入=true，明细本次结算金额 × 明细原费用汇率)
            /// </summary>
            public decimal ReceivableDomesticTariffBaseCurrency { get; set; }

            /// <summary>
            /// 汇兑损益（本位币）
            /// </summary>
            public decimal ExchangeLoss { get; set; }

            /// <summary>
            /// 手续费金额（原币种）
            /// </summary>
            public decimal ServiceFeeAmount { get; set; }

            /// <summary>
            /// 手续费本位币金额
            /// </summary>
            public decimal ServiceFeeBaseCurrency { get; set; }

            /// <summary>
            /// 预付金额（原币种）
            /// </summary>
            public decimal AdvancePaymentAmount { get; set; }

            /// <summary>
            /// 预付金额本位币
            /// </summary>
            public decimal AdvancePaymentBaseCurrency { get; set; }

            /// <summary>
            /// 是否混合业务（既有收入又有支出）
            /// </summary>
            public bool IsMixedBusiness { get; set; }

            /// <summary>
            /// 付款结算单明细信息（用于计算应付应收分类金额）
            /// </summary>
            public List<SettlementPaymentItemDto> Items { get; set; } = new List<SettlementPaymentItemDto>();

            /// <summary>
            /// 实际付款记录（优先使用，支持多笔付款）
            /// </summary>
            public List<ActualFinancialTransactionDto> ActualTransactions { get; set; } = new List<ActualFinancialTransactionDto>();

            /// <summary>
            /// 银行信息（当无实际付款记录时使用）
            /// </summary>
            public BankInfo BankInfo { get; set; }

            #region 向后兼容属性（用于旧凭证生成逻辑）

            /// <summary>
            /// 供应商名称（向后兼容，映射到CustomerName）
            /// </summary>
            public string SupplierName => CustomerName;

            /// <summary>
            /// 供应商财务编码（向后兼容，映射到CustomerFinanceCode）
            /// </summary>
            public string SupplierFinanceCode => CustomerFinanceCode;

            /// <summary>
            /// 应付总额本位币（向后兼容，汇总所有应付金额）
            /// </summary>
            public decimal PayableTotalBaseCurrency => 
                PayableForeignBaseCurrency + PayableDomesticCustomerBaseCurrency + PayableDomesticTariffBaseCurrency;

            /// <summary>
            /// 应收总额本位币（向后兼容，汇总所有应收金额）
            /// </summary>
            public decimal ReceivableTotalBaseCurrency => 
                ReceivableForeignBaseCurrency + ReceivableDomesticCustomerBaseCurrency + ReceivableDomesticTariffBaseCurrency;

            /// <summary>
            /// 是否多笔付款（向后兼容，通过ActualTransactions判断）
            /// </summary>
            public bool HasMultiplePayments => ActualTransactions?.Count > 1;

            #endregion
        }

        /// <summary>
        /// 付款结算单明细DTO
        /// 增强支持国内外客户分类和代垫费用判断
        /// </summary>
        public class SettlementPaymentItemDto
        {
            /// <summary>
            /// 明细ID
            /// </summary>
            public Guid ItemId { get; set; }

            /// <summary>
            /// 本次结算金额
            /// </summary>
            public decimal Amount { get; set; }

            /// <summary>
            /// 明细结算汇率
            /// </summary>
            public decimal ExchangeRate { get; set; }

            /// <summary>
            /// 结算金额本位币（使用原费用汇率计算）
            /// 计算公式：明细本次结算金额 × 明细原费用汇率
            /// </summary>
            public decimal SettlementAmountBaseCurrency { get; set; }

            /// <summary>
            /// 原费用IO（收入true，支出false）
            /// </summary>
            public bool OriginalFeeIO { get; set; }

            /// <summary>
            /// 原费用汇率（DocFee.ExchangeRate）
            /// </summary>
            public decimal OriginalFeeExchangeRate { get; set; }

            /// <summary>
            /// 费用种类是否代垫（FeesType.IsDaiDian）
            /// </summary>
            public bool IsAdvanceFee { get; set; }

            /// <summary>
            /// 申请单明细ID
            /// </summary>
            public Guid? RequisitionItemId { get; set; }
        }

        #endregion
    }
}
