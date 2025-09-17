using PowerLmsWebApi.Dto;
using PowerLms.Data;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers.Financial
{
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

        /// <summary>
        /// 文件显示名称。
        /// 用于fileService.CreateFile中的displayName参数，如果不指定则使用默认格式。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 文件备注信息。
        /// 用于fileService.CreateFile中的remark参数，如果不指定则使用默认格式。
        /// </summary>
        public string Remark { get; set; }
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

    /// <summary>
    /// OA日常费用申请单导出为DBF文件的请求参数。
    /// </summary>
    public class ExportOaExpenseToDbfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出条件字典，键为字段名，值为条件值。
        /// 用于筛选要导出的OA申请单数据，支持EfHelper.GenerateWhereAnd的所有查询操作。
        /// 支持的过滤条件包括：
        /// - StartDate: 统计开始日期(yyyy-MM-dd格式)，默认为当月月初
        /// - EndDate: 统计结束日期(yyyy-MM-dd格式)，默认为操作当日
        /// - IsLoan: 是否借款申请(true/false)，用于区分收款/付款凭证
        /// - Status: 申请单状态过滤，只导出已确认状态的申请单
        /// - EmployeeId: 员工ID过滤
        /// - DepartmentId: 部门ID过滤
        /// - AmountRange: 金额范围过滤(如"100,1000")
        /// 以及其他OaExpenseRequisition、OaExpenseRequisitionItem相关字段的过滤条件
        /// 
        /// 注意：系统会自动处理借款申请(IsLoan=true)生成收款凭证，报销申请(IsLoan=false)生成付款凭证
        /// </summary>
        public Dictionary<string, string> ExportConditions { get; set; } = new();

        /// <summary>
        /// 文件显示名称。
        /// 用于fileService.CreateFile中的displayName参数，如果不指定则使用默认格式。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 文件备注信息。
        /// 用于fileService.CreateFile中的remark参数，如果不指定则使用默认格式。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// OA日常费用申请单导出为DBF文件的返回结果。
    /// </summary>
    public class ExportOaExpenseToDbfReturnDto : ReturnDtoBase
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
        /// 预计要处理的申请单数量。
        /// </summary>
        public int ExpectedRequisitionCount { get; set; }

        /// <summary>
        /// 预计生成的凭证分录数量。
        /// </summary>
        public int ExpectedVoucherEntryCount { get; set; }
    }

    /// <summary>
    /// A账应收本位币挂账(ARAB)导出为DBF文件的请求参数。
    /// </summary>
    public class ExportArabToDbfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出条件字典，键为字段名，值为条件值。
        /// 用于筛选要导出的费用数据，支持EfHelper.GenerateWhereAnd的所有查询操作。
        /// 支持的过滤条件包括：
        /// - StartDate: 统计开始日期(yyyy-MM-dd格式)，默认为当月月初
        /// - EndDate: 统计结束日期(yyyy-MM-dd格式)，默认为操作当日
        /// - JobNumber: 工作号过滤条件
        /// - AccountingDate: 记账日期(yyyy-MM-dd格式)，默认为当前日期
        /// - DocFee.IO: 收支类型过滤，true为收入，false为支出
        /// - DocFee.BalanceId: 结算单位ID过滤
        /// - PlJob.JobNo: 工作号过滤
        /// 以及其他DocFee、PlJob、PlCustomer相关字段的过滤条件
        /// </summary>
        public Dictionary<string, string> ExportConditions { get; set; } = new();

        /// <summary>
        /// 文件显示名称。
        /// 用于fileService.CreateFile中的displayName参数，如果不指定则使用默认格式。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 文件备注信息。
        /// 用于fileService.CreateFile中的remark参数，如果不指定则使用默认格式。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// A账应收本位币挂账(ARAB)导出为DBF文件的返回结果。
    /// </summary>
    public class ExportArabToDbfReturnDto : ReturnDtoBase
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
        /// 预计要处理的费用记录数量。
        /// </summary>
        public int ExpectedFeeCount { get; set; }

        /// <summary>
        /// 预计生成的凭证分录数量。
        /// </summary>
        public int ExpectedVoucherEntryCount { get; set; }
    }

    /// <summary>
    /// A账应付本位币挂账(APAB)导出为DBF文件的请求参数。
    /// </summary>
    public class ExportApabToDbfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出条件字典，键为字段名，值为条件值。
        /// 用于筛选要导出的费用数据，支持EfHelper.GenerateWhereAnd的所有查询操作。
        /// 支持的过滤条件包括：
        /// - StartDate: 统计开始日期(yyyy-MM-dd格式)，默认为当月月初
        /// - EndDate: 统计结束日期(yyyy-MM-dd格式)，默认为操作当日
        /// - JobNumber: 工作号过滤条件
        /// - AccountingDate: 记账日期(yyyy-MM-dd格式)，默认为当前日期
        /// - DocFee.IO: 收支类型过滤，true为收入，false为支出
        /// - DocFee.BalanceId: 结算单位ID过滤
        /// - PlJob.JobNo: 工作号过滤
        /// 以及其他DocFee、PlJob、PlCustomer相关字段的过滤条件
        /// </summary>
        public Dictionary<string, string> ExportConditions { get; set; } = new();

        /// <summary>
        /// 文件显示名称。
        /// 用于fileService.CreateFile中的displayName参数，如果不指定则使用默认格式。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 文件备注信息。
        /// 用于fileService.CreateFile中的remark参数，如果不指定则使用默认格式。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// A账应付本位币挂账(APAB)导出为DBF文件的返回结果。
    /// </summary>
    public class ExportApabToDbfReturnDto : ReturnDtoBase
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
        /// 预计要处理的费用记录数量。
        /// </summary>
        public int ExpectedFeeCount { get; set; }

        /// <summary>
        /// 预计生成的凭证分录数量。
        /// </summary>
        public int ExpectedVoucherEntryCount { get; set; }
    }

    /// <summary>
    /// OA日常费用付款导出为DBF文件的请求参数。
    /// </summary>
    public class ExportOaDailyPaymentToDbfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出条件字典，键为字段名，值为条件值。
        /// 用于筛选要导出的OA日常费用付款明细数据，支持EfHelper.GenerateWhereAnd的所有查询操作。
        /// 支持的过滤条件包括：
        /// - StartDate: 统计开始日期(yyyy-MM-dd格式)，默认为当月月初
        /// - EndDate: 统计结束日期(yyyy-MM-dd格式)，默认为操作当日
        /// - Status: 申请单状态过滤，只导出已确认状态的申请单
        /// - EmployeeId: 员工ID过滤
        /// - DepartmentId: 部门ID过滤
        /// - DailyFeesTypeId: 日常费用种类ID过滤
        /// - SettlementAccountId: 结算账号ID过滤
        /// - AmountRange: 金额范围过滤(如"100,1000")
        /// 以及其他OaExpenseRequisition、OaExpenseRequisitionItem相关字段的过滤条件
        /// 注意：系统自动排除借款申请(IsLoan=true)，只处理付款申请
        /// </summary>
        public Dictionary<string, string> ExportConditions { get; set; } = new();

        /// <summary>
        /// 文件显示名称。
        /// 用于fileService.CreateFile中的displayName参数，如果不指定则使用默认格式。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 文件备注信息。
        /// 用于fileService.CreateFile中的remark参数，如果不指定则使用默认格式。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// OA日常费用付款导出为DBF文件的返回结果。
    /// </summary>
    public class ExportOaDailyPaymentToDbfReturnDto : ReturnDtoBase
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
        /// 预计要处理的付款明细项数量。
        /// </summary>
        public int ExpectedItemCount { get; set; }

        /// <summary>
        /// 预计生成的凭证分录数量。
        /// </summary>
        public int ExpectedVoucherEntryCount { get; set; }
    }

    #endregion

    #region 付款结算单导出DTO

    /// <summary>
    /// 导出付款结算单为金蝶DBF格式文件参数DTO
    /// </summary>
    public class ExportSettlementPaymentParamsDto : TokenDtoBase
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
        /// 预计生成的凭证分录数量（基于六种分录规则）
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
    /// 用于在生成凭证分录前进行复杂的金额计算和业务逻辑判断
    /// </summary>
    public class SettlementPaymentCalculationDto
    {
        /// <summary>
        /// 付款结算单ID
        /// </summary>
        public Guid SettlementPaymentId { get; set; }

        /// <summary>
        /// 往来单位名称（供应商名称）
        /// </summary>
        public string SupplierName { get; set; }

        /// <summary>
        /// 往来单位财务编码（供应商应付财务编码）
        /// </summary>
        public string SupplierFinanceCode { get; set; }

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
        /// 应付合计本位币金额
        /// 计算公式：sum(支出明细本次结算金额 × 明细原费用汇率)
        /// </summary>
        public decimal PayableTotalBaseCurrency { get; set; }

        /// <summary>
        /// 应收合计本位币金额（混合业务中的收入部分）
        /// 计算公式：sum(收入明细本次结算金额 × 明细原费用汇率)
        /// </summary>
        public decimal ReceivableTotalBaseCurrency { get; set; }

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
        /// 是否混合业务（既有收入又有支出）
        /// </summary>
        public bool IsMixedBusiness { get; set; }

        /// <summary>
        /// 是否存在多笔付款
        /// </summary>
        public bool HasMultiplePayments { get; set; }

        /// <summary>
        /// 付款明细信息（多笔付款时使用）
        /// </summary>
        public List<SettlementPaymentItemDto> Items { get; set; } = new List<SettlementPaymentItemDto>();

        /// <summary>
        /// 银行信息（用于获取凭证字和科目代码）
        /// </summary>
        public BankInfo BankInfo { get; set; }
    }

    /// <summary>
    /// 付款结算单明细DTO
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
