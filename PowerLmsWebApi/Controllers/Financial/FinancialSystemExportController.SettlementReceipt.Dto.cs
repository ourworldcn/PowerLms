/*
 * 项目：PowerLms财务系统 | 模块：收款结算单导出金蝶功能
 * 功能：收款结算单导出金蝶功能的DTO定义分部类
 * 技术要点：分部类模式、复杂业务DTO设计、七种凭证分录规则支持
 * 作者：zc | 创建：2025-01 | 修改：2025-01-31 收款结算单导出功能实施
 */

using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers.Financial
{
    /// <summary>
    /// 财务系统导出控制器 - 收款结算单导出功能DTO定义
    /// </summary>
    public partial class FinancialSystemExportController
    {
        #region 收款结算单导出DTO

        /// <summary>
        /// 导出收款结算单为金蝶DBF格式文件参数DTO
        /// </summary>
        public class ExportSettlementReceiptParamsDto : TokenDtoBase
        {
            /// <summary>
            /// 导出查询条件。支持结算日期、币种、金额范围等过滤条件。
            /// 示例：
            /// - 结算日期范围：conditional["FinanceDateTime"] = "2024-1-1,2024-12-31"
            /// - 币种过滤：conditional["Currency"] = "USD"
            /// - 金额范围：conditional["Amount"] = "100,1000"
            /// - 未导出状态：conditional["ConfirmDateTime"] = "null"
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
        /// 导出收款结算单为金蝶DBF格式文件返回值DTO
        /// </summary>
        public class ExportSettlementReceiptReturnDto : ReturnDtoBase
        {
            /// <summary>
            /// 异步任务ID，用于跟踪导出进度
            /// </summary>
            public Guid? TaskId { get; set; }

            /// <summary>
            /// 预计导出的收款结算单数量
            /// </summary>
            public int ExpectedSettlementReceiptCount { get; set; }

            /// <summary>
            /// 预计生成的凭证分录数量（基于七种分录规则）
            /// </summary>
            public int ExpectedVoucherEntryCount { get; set; }

            /// <summary>
            /// 操作结果消息
            /// </summary>
            public string Message { get; set; }
        }

        #endregion

        #region 收款结算单导出内部DTO

        /// <summary>
        /// 收款结算单导出计算结果DTO
        /// 用于在生成凭证分录前进行复杂的金额计算和业务逻辑判断
        /// </summary>
        public class SettlementReceiptCalculationDto
        {
            /// <summary>
            /// 收款结算单ID
            /// </summary>
            public Guid SettlementReceiptId { get; set; }

            /// <summary>
            /// 往来单位名称
            /// </summary>
            public string CustomerName { get; set; }

            /// <summary>
            /// 往来单位财务编码
            /// </summary>
            public string CustomerFinanceCode { get; set; }

            /// <summary>
            /// 收款单号
            /// </summary>
            public string ReceiptNumber { get; set; }

            /// <summary>
            /// 收款日期
            /// </summary>
            public DateTime ReceiptDate { get; set; }

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
            /// 应收合计本位币金额
            /// 计算公式：sum(收入明细本次结算金额 × 明细原费用本位币汇率)
            /// </summary>
            public decimal ReceivableTotalBaseCurrency { get; set; }

            /// <summary>
            /// 应付合计本位币金额
            /// 计算公式：sum(支出明细本次结算金额 × 明细原费用本位币汇率)
            /// </summary>
            public decimal PayableTotalBaseCurrency { get; set; }

            /// <summary>
            /// 预收金额（原币种）
            /// </summary>
            public decimal AdvancePaymentAmount { get; set; }

            /// <summary>
            /// 预收金额本位币
            /// </summary>
            public decimal AdvancePaymentBaseCurrency { get; set; }

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
            /// 预收冲应收金额（原币种）
            /// </summary>
            public decimal AdvanceOffsetReceivableAmount { get; set; }

            /// <summary>
            /// 预收冲应收本位币金额
            /// </summary>
            public decimal AdvanceOffsetReceivableBaseCurrency { get; set; }

            /// <summary>
            /// 是否混合业务（既有收入又有支出）
            /// </summary>
            public bool IsMixedBusiness { get; set; }

            /// <summary>
            /// 多笔收款明细信息
            /// </summary>
            public List<SettlementReceiptItemDto> Items { get; set; } = new List<SettlementReceiptItemDto>();

            /// <summary>
            /// 银行信息（用于获取凭证字和科目代码）
            /// </summary>
            public BankInfo BankInfo { get; set; }
        }

        /// <summary>
        /// 收款结算单明细DTO
        /// </summary>
        public class SettlementReceiptItemDto
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
            /// 结算金额本位币
            /// </summary>
            public decimal SettlementAmountBaseCurrency { get; set; }

            /// <summary>
            /// 原费用IO（收入true，支出false）
            /// </summary>
            public bool OriginalFeeIO { get; set; }

            /// <summary>
            /// 原费用汇率
            /// </summary>
            public decimal OriginalFeeExchangeRate { get; set; }

            /// <summary>
            /// 申请单明细ID
            /// </summary>
            public Guid? RequisitionItemId { get; set; }
        }

        #endregion
    }
}