using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PowerLms.Data.OA
{
    /// <summary>
    /// 收支类型枚举。
    /// </summary>
    public enum IncomeExpenseType : byte
    {
        /// <summary>
        /// 收款。
        /// </summary>
        Income = 0,

        /// <summary>
        /// 付款。
        /// </summary>
        Expense = 1
    }

    /// <summary>
    /// OA日常费用申请单主表。
    /// 用于处理日常费用（如差旅、报销），独立于主营业务的费用申请。
    /// </summary>
    [Comment("OA日常费用申请单主表")]
    [Index(nameof(OrgId), IsUnique = false)]
    public class OaExpenseRequisition : GuidKeyObjectBase, ISpecificOrg, ICreatorInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OaExpenseRequisition()
        {
            CreateDateTime = OwHelper.WorldNow;
            CurrencyCode = "CNY";
            ExchangeRate = 1.0000m;
        }

        /// <summary>
        /// 所属机构Id。该单据归属的机构Id,即登记人当前登录的机构Id。增加时确定，不可修改。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 申请人Id。员工账号Id。
        /// </summary>
        [Comment("申请人Id，员工账号Id")]
        public Guid? ApplicantId { get; set; }

        /// <summary>
        /// 申请时间。
        /// </summary>
        [Comment("申请时间")]
        [Precision(3)]
        public DateTime? ApplyDateTime { get; set; }

        /// <summary>
        /// 是否借款。true表示借款申请，false表示报销申请。
        /// </summary>
        [Comment("是否借款，true表示借款申请，false表示报销申请")]
        public bool IsLoan { get; set; }

        /// <summary>
        /// 是否导入财务软件。作为后期金蝶财务软件导入条件。
        /// </summary>
        [Comment("是否导入财务软件，作为后期金蝶财务软件导入条件")]
        public bool IsImportFinancialSoftware { get; set; }

        /// <summary>
        /// 相关客户。字符串即可，不用从客户资料中选择。
        /// </summary>
        [Comment("相关客户，字符串即可，不用从客户资料中选择")]
        [MaxLength(128)]
        public string RelatedCustomer { get; set; }

        /// <summary>
        /// 收款银行。
        /// </summary>
        [Comment("收款银行")]
        [MaxLength(128)]
        public string ReceivingBank { get; set; }

        /// <summary>
        /// 收款户名。
        /// </summary>
        [Comment("收款户名")]
        [MaxLength(128)]
        public string ReceivingAccountName { get; set; }

        /// <summary>
        /// 收款账户。
        /// </summary>
        [Comment("收款账户")]
        [MaxLength(64)]
        public string ReceivingAccountNumber { get; set; }

        /// <summary>
        /// 所谈事项。大文本。
        /// </summary>
        [Comment("所谈事项，大文本")]
        public string DiscussedMatters { get; set; }

        /// <summary>
        /// 备注。大文本。
        /// </summary>
        [Comment("备注，大文本")]
        public string Remark { get; set; }

        /// <summary>
        /// 收支类型。收款/付款。
        /// </summary>
        [Comment("收支类型，收款/付款")]
        public IncomeExpenseType? IncomeExpenseType { get; set; }

        /// <summary>
        /// 费用种类。选择日常费用种类，申请的费用种类申请人填写，不关联科目代码。
        /// </summary>
        [Comment("费用种类，选择日常费用种类，申请的费用种类申请人填写，不关联科目代码")]
        [MaxLength(128)]
        public string ExpenseCategory { get; set; }

        /// <summary>
        /// 币种代码。币种code。
        /// </summary>
        [Comment("币种代码")]
        [MaxLength(4)]
        [Unicode(false)]
        public string CurrencyCode { get; set; } = "CNY";

        /// <summary>
        /// 汇率。四位小数。
        /// </summary>
        [Comment("汇率，四位小数")]
        [Precision(18, 4)]
        public decimal ExchangeRate { get; set; } = 1.0000m;

        /// <summary>
        /// 金额。两位小数。
        /// </summary>
        [Comment("金额，两位小数")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 审核时间。为空则表示未审核。审核通过后填写审核时间。在审批流转中不可修改。
        /// </summary>
        [Comment("审核时间。为空则表示未审核。审核通过后填写审核时间。")]
        [Precision(3)]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// 审核操作者Id。为空则表示未审核。在审批流转中不可修改。
        /// </summary>
        [Comment("审核操作者Id。为空则表示未审核。")]
        public Guid? AuditOperatorId { get; set; }

        #region ICreatorInfo

        /// <summary>
        /// 创建者Id。登记人Id。主动申请时，登记人=申请人（申请人、登记人不可选择，为登录人），财务帮登记时选择申请人，登记人为登录人。
        /// </summary>
        [Comment("创建者Id，即登记人Id")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        [Comment("创建的时间")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; }

        #endregion
    }

    /// <summary>
    /// OA费用申请单明细表。
    /// 此表由财务人员填写，用于专业拆分费用。支持多条明细记录。
    /// </summary>
    [Comment("OA费用申请单明细表")]
    [Index(nameof(ParentId), IsUnique = false)]
    public class OaExpenseRequisitionItem : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OaExpenseRequisitionItem()
        {
            SettlementDateTime = DateTime.Today;
        }

        /// <summary>
        /// 申请单Id。所属申请单Id，关联到 <see cref="OaExpenseRequisition"/> 的Id。
        /// </summary>
        [Comment("申请单Id，所属申请单Id，关联到OaExpenseRequisition的Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 序号。用于明细表的排序显示。
        /// </summary>
        [Comment("序号，用于明细表的排序显示")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 结算时间。财务人员处理时的结算时间，可控制凭证期间。
        /// </summary>
        [Comment("结算时间，财务人员处理时的结算时间，可控制凭证期间")]
        public DateTime SettlementDateTime { get; set; }

        /// <summary>
        /// 结算账号Id。关联到 <see cref="BankInfo"/> 的Id，统一的账号选择。
        /// </summary>
        [Comment("结算账号Id，关联到BankInfo的Id，统一的账号选择")]
        public Guid? SettlementAccountId { get; set; }

        /// <summary>
        /// 日常费用种类Id。关联到 <see cref="DailyFeesType"/> 的Id，财务选择正确的费用种类。
        /// </summary>
        [Comment("日常费用种类Id，关联到DailyFeesType的Id，财务选择正确的费用种类")]
        public Guid? DailyFeesTypeId { get; set; }

        /// <summary>
        /// 金额。此明细项的金额，两位小数。
        /// </summary>
        [Comment("金额，此明细项的金额，两位小数")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 员工Id。费用可能核算到不同员工名下（非仅申请人），关联到 <see cref="Account"/> 的Id。
        /// </summary>
        [Comment("员工Id，费用可能核算到不同员工名下，关联到Account的Id")]
        public Guid? EmployeeId { get; set; }

        /// <summary>
        /// 部门Id。选择系统中的组织架构部门，关联到 <see cref="PlOrganization"/> 的Id。
        /// </summary>
        [Comment("部门Id，选择系统中的组织架构部门，关联到PlOrganization的Id")]
        public Guid? DepartmentId { get; set; }

        /// <summary>
        /// 凭证号。后台自动生成，格式：期间-凭证字-序号（如：7-银-1）。
        /// </summary>
        [Comment("凭证号，后台自动生成，格式：期间-凭证字-序号")]
        [MaxLength(32)]
        public string VoucherNumber { get; set; }

        /// <summary>
        /// 摘要。财务填写的费用摘要说明，如"请客吃饭"。
        /// </summary>
        [Comment("摘要，财务填写的费用摘要说明")]
        [MaxLength(256)]
        public string Summary { get; set; }

        /// <summary>
        /// 发票号。
        /// </summary>
        [Comment("发票号")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        [MaxLength(512)]
        public string Remark { get; set; }
    }

    /// <summary>
    /// OA费用申请单扩展方法。
    /// </summary>
    public static class OaExpenseRequisitionExtensions
    {
        /// <summary>
        /// 获取申请单的费用明细项。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>费用明细项查询</returns>
        public static IQueryable<OaExpenseRequisitionItem> GetItems(this OaExpenseRequisition requisition, DbContext context)
        {
            return context.Set<OaExpenseRequisitionItem>().Where(x => x.ParentId == requisition.Id);
        }

        /// <summary>
        /// 获取申请人信息。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>申请人账号对象</returns>
        public static Account GetApplicant(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.ApplicantId.HasValue ? context.Set<Account>().Find(requisition.ApplicantId.Value) : null;
        }

        /// <summary>
        /// 获取登记人信息。CreateBy就是登记人Id。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>登记人账号对象</returns>
        public static Account GetRegistrar(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.CreateBy.HasValue ? context.Set<Account>().Find(requisition.CreateBy.Value) : null;
        }

        /// <summary>
        /// 获取审核人信息。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>审核人账号对象</returns>
        public static Account GetAuditor(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.AuditOperatorId.HasValue ? context.Set<Account>().Find(requisition.AuditOperatorId.Value) : null;
        }

        /// <summary>
        /// 判断申请单是否可以编辑。已审核的申请单不可编辑。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文（可选）</param>
        /// <returns>可以编辑返回true，否则返回false</returns>
        public static bool CanEdit(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return !requisition.AuditDateTime.HasValue; // 未审核的可以编辑
        }

        /// <summary>
        /// 判断申请单是否已审核。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <returns>已审核返回true，否则返回false</returns>
        public static bool IsAudited(this OaExpenseRequisition requisition)
        {
            return requisition.AuditDateTime.HasValue;
        }

        /// <summary>
        /// 获取申请单的审批状态。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>审批状态描述</returns>
        public static string GetApprovalStatus(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return requisition.AuditDateTime.HasValue ? "已审核" : "草稿";
        }

        /// <summary>
        /// 获取收支类型的显示名称。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <returns>收支类型显示名称</returns>
        public static string GetIncomeExpenseTypeDisplayName(this OaExpenseRequisition requisition)
        {
            return requisition.IncomeExpenseType switch
            {
                Data.OA.IncomeExpenseType.Income => "收款",
                Data.OA.IncomeExpenseType.Expense => "付款",
                _ => "未设置"
            };
        }

        /// <summary>
        /// 验证明细金额合计是否与主单金额一致。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>金额一致返回true，否则返回false</returns>
        public static bool ValidateAmountConsistency(this OaExpenseRequisition requisition, DbContext context)
        {
            var itemsSum = requisition.GetItems(context).Sum(x => x.Amount);
            return Math.Abs(itemsSum - requisition.Amount) < 0.01m; // 允许0.01的误差
        }

        /// <summary>
        /// 获取明细金额合计。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>明细金额合计</returns>
        public static decimal GetItemsAmountSum(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.GetItems(context).Sum(x => x.Amount);
        }
    }

    /// <summary>
    /// OA费用申请单明细扩展方法。
    /// </summary>
    public static class OaExpenseRequisitionItemExtensions
    {
        /// <summary>
        /// 获取明细项的结算账号信息。
        /// </summary>
        /// <param name="item">明细项</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>结算账号信息</returns>
        public static BankInfo GetSettlementAccount(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.SettlementAccountId.HasValue ? context.Set<BankInfo>().Find(item.SettlementAccountId.Value) : null;
        }

        /// <summary>
        /// 获取明细项的费用种类信息。
        /// </summary>
        /// <param name="item">明细项</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>费用种类信息</returns>
        public static DailyFeesType GetDailyFeesType(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.DailyFeesTypeId.HasValue ? context.Set<DailyFeesType>().Find(item.DailyFeesTypeId.Value) : null;
        }

        /// <summary>
        /// 获取明细项的员工信息。
        /// </summary>
        /// <param name="item">明细项</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>员工信息</returns>
        public static Account GetEmployee(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.EmployeeId.HasValue ? context.Set<Account>().Find(item.EmployeeId.Value) : null;
        }

        /// <summary>
        /// 获取明细项的部门信息。
        /// </summary>
        /// <param name="item">明细项</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>部门信息</returns>
        public static PlOrganization GetDepartment(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.DepartmentId.HasValue ? context.Set<PlOrganization>().Find(item.DepartmentId.Value) : null;
        }
    }
}