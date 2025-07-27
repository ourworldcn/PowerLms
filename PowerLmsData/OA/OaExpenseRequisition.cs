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
        /// 收款
        /// </summary>
        Income = 0,

        /// <summary>
        /// 付款
        /// </summary>
        Expense = 1
    }

    /// <summary>
    /// OA日常费用申请单主表。
    /// 用于处理日常费用（报销、借款）申请，独立于主营业务的费用申请。
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
        /// 所属机构Id。设置数据隔离的机构Id,即记录当前记录的机构Id，运行时确定，不可修改。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 申请人Id（员工账号Id）。
        /// </summary>
        [Comment("申请人Id（员工账号Id）")]
        public Guid? ApplicantId { get; set; }

        /// <summary>
        /// 是否借款。true表示借款申请，false表示报销申请。
        /// </summary>
        [Comment("是否借款。true表示借款申请，false表示报销申请")]
        public bool IsLoan { get; set; }

        /// <summary>
        /// 是否导入财务软件。作为后期导入财务软件的导入条件。
        /// </summary>
        [Comment("是否导入财务软件。作为后期导入财务软件的导入条件")]
        public bool IsImportFinancialSoftware { get; set; }

        /// <summary>
        /// 相关客户。字符串填写，可以从客户列表中选择。
        /// </summary>
        [Comment("相关客户。字符串填写，可以从客户列表中选择")]
        [MaxLength(128)]
        public string RelatedCustomer { get; set; }

        /// <summary>
        /// 收款银行。
        /// </summary>
        [Comment("收款银行")]
        [MaxLength(128)]
        public string ReceivingBank { get; set; }

        /// <summary>
        /// 收款户名
        /// </summary>
        [Comment("收款户名")]
        [MaxLength(128)]
        public string ReceivingAccountName { get; set; }

        /// <summary>
        /// 收款人账号
        /// </summary>
        [Comment("收款人账号")]
        [MaxLength(64)]
        public string ReceivingAccountNumber { get; set; }

        /// <summary>
        /// 洽谈事项。长文本。
        /// </summary>
        [Comment("洽谈事项。长文本")]
        public string DiscussedMatters { get; set; }

        /// <summary>
        /// 备注。长文本。
        /// </summary>
        [Comment("备注。长文本")]
        public string Remark { get; set; }

        /// <summary>
        /// 收支类型。收款/付款
        /// </summary>
        [Comment("收支类型（收款/付款）")]
        public IncomeExpenseType? IncomeExpenseType { get; set; }

        /// <summary>
        /// 费用类别。选择日常费用的类别，由后台费用类别管理配置维护，可以类别编码项。
        /// </summary>
        [Comment("费用类别。选择日常费用的类别，由后台费用类别管理配置维护，可以类别编码项")]
        [MaxLength(128)]
        public string ExpenseCategory { get; set; }

        /// <summary>
        /// 币种代码。
        /// </summary>
        [Comment("币种代码")]
        [MaxLength(4)]
        [Unicode(false)]
        public string CurrencyCode { get; set; } = "CNY";

        /// <summary>
        /// 汇率。两位小数。
        /// </summary>
        [Comment("汇率（两位小数）")]
        [Precision(18, 4)]
        public decimal ExchangeRate { get; set; } = 1.0000m;

        /// <summary>
        /// 金额。两位小数。
        /// </summary>
        [Comment("金额（两位小数）")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 审核时间。为空表示未审核。一般通过后台填写审核时间。数据库迁移后不可修改。
        /// </summary>
        [Comment("审核时间。为空表示未审核。一般通过后台填写审核时间")]
        [Precision(3)]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// 审核操作员Id。为空表示未审核。数据库迁移后不可修改。
        /// </summary>
        [Comment("审核操作员Id。为空表示未审核")]
        public Guid? AuditOperatorId { get; set; }

        #region ICreatorInfo

        /// <summary>
        /// 创建者Id（即登记人Id）。申请人申请时，记录人=申请人（申请人）；登记人不选择为申请人，申请时登记人选择申请人，登记人为申请人。
        /// </summary>
        [Comment("创建者Id（即登记人Id）")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建时间（即申请时间）。系统自动记录申请单的创建时间作为申请时间。
        /// </summary>
        [Comment("创建时间（即申请时间）")]
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
        /// 财务部门Id。关联简单字典finance-depart类型，用于金蝶核算部门（不代表真实的组织架构）。
        /// 
        /// 背景：财务核算需要的部门概念与实际的组织架构部门可能不同，财务部门是虚拟的核算维度。
        /// 设计：关联SimpleDataDic的finance-depart类型字典，由用户手工创建和维护财务部门数据。
        /// 用途：在生成金蝶凭证时提供部门核算信息，支持费用的部门维度分摊和统计。
        /// </summary>
        [Comment("财务部门Id。关联简单字典finance-depart类型，用于金蝶核算部门")]
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
        /// 获取明细项的财务部门信息。
        /// </summary>
        /// <param name="item">明细项</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>财务部门信息</returns>
        public static SimpleDataDic GetFinanceDepartment(this OaExpenseRequisitionItem item, DbContext context)
        {
            return item.DepartmentId.HasValue ? context.Set<SimpleDataDic>().Find(item.DepartmentId.Value) : null;
        }
    }
}