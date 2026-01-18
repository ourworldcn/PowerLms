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
        /// - IsLoan: 系统会自动处理借款申请(IsLoan=true)生成收款凭证，报销申请(IsLoan=false)生成付款凭证
        /// - Status: 申请单状态过滤，只导出已确认状态的申请单
        /// - EmployeeId: 员工ID过滤
        /// - DepartmentId: 部门ID过滤
        /// - AmountRange: 金额范围过滤(如"100,1000")
        /// 以及其他OaExpenseRequisition、OaExpenseRequisitionItem相关字段的过滤条件
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
    #region 取消财务导出DTO
    /// <summary>
    /// 取消财务导出标记请求参数
    /// 基于导出时间范围进行批量取消，避免传递大量实体ID
    /// </summary>
    public class CancelFinancialExportParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出业务类型代码（必填）
        /// 支持的类型：
        /// - INVOICE: 发票导出
        /// - OA_EXPENSE: OA日常费用申请单导出
        /// - ARAB: A账应收本位币挂账导出（计提）
        /// - APAB: A账应付本位币挂账导出（计提）
        /// - SETTLEMENT_RECEIPT: 收款结算单导出
        /// - SETTLEMENT_PAYMENT: 付款结算单导出
        /// </summary>
        [Required(ErrorMessage = "导出业务类型代码不能为空")]
        public string ExportTypeCode { get; set; }
        /// <summary>
        /// 导出时间范围-开始时间（必填）
        /// 格式：ISO 8601 (如 "2025-01-01T00:00:00Z")
        /// 说明：将取消在此时间之后导出的数据
        /// 典型用法：
        /// - 取消本月导出：设为本月1号0点
        /// - 取消今天导出：设为今天0点
        /// </summary>
        [Required(ErrorMessage = "导出时间范围开始时间不能为空")]
        public DateTime ExportedDateTimeStart { get; set; }
        /// <summary>
        /// 导出时间范围-结束时间（必填）
        /// 格式：ISO 8601 (如 "2025-01-31T23:59:59Z")
        /// 说明：将取消在此时间之前导出的数据
        /// 典型用法：
        /// - 取消本月导出：设为本月最后一天23:59:59
        /// - 取消今天导出：设为今天23:59:59
        /// </summary>
        [Required(ErrorMessage = "导出时间范围结束时间不能为空")]
        public DateTime ExportedDateTimeEnd { get; set; }
        /// <summary>
        /// 额外的过滤条件（可选）
        /// 用于进一步缩小取消范围，支持EfHelper.GenerateWhereAnd的查询语法
        /// 常见用法：
        /// - 限定导出用户: { "ExportedUserId": "guid" }
        /// - 限定机构: { "OrgId": "guid" }
        /// 注意：此条件会与导出时间范围进行AND组合
        /// </summary>
        public Dictionary<string, string> AdditionalConditions { get; set; }
        /// <summary>
        /// 取消原因（可选）
        /// 用于审计追踪和日志记录
        /// </summary>
        public string Reason { get; set; }
    }
    /// <summary>
    /// 取消财务导出标记返回结果
    /// </summary>
    public class CancelFinancialExportReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 成功取消的记录数量
        /// </summary>
        public int SuccessCount { get; set; }
        /// <summary>
        /// 取消失败的记录数量
        /// </summary>
        public int FailedCount { get; set; }
        /// <summary>
        /// 取消失败的实体ID列表
        /// </summary>
        public List<Guid> FailedIds { get; set; } = new List<Guid>();
        /// <summary>
        /// 操作结果消息
        /// </summary>
        public string Message { get; set; }
    }
    #endregion
    // 注意：付款结算单导出的DTO已移至FinancialSystemExportController.SettlementPayment.Dto.cs文件
    // 旧的SettlementPaymentCalculationDto和SettlementPaymentItemDto已废弃
}
