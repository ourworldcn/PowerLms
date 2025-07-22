using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PowerLms.Data.OA
{
    /// <summary>
    /// 结算方式枚举。
    /// </summary>
    public enum SettlementMethodType : byte
    {
        /// <summary>
        /// 现金结算。
        /// </summary>
        Cash = 0,

        /// <summary>
        /// 银行转账。
        /// </summary>
        Bank = 1
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
            ApplyDateTime = OwHelper.WorldNow;
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
        [Required]
        public Guid ApplicantId { get; set; }

        /// <summary>
        /// 申请时间。
        /// </summary>
        [Comment("申请时间")]
        [Precision(3)]
        public DateTime ApplyDateTime { get; set; }

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
        /// 结算方式。现金或银行转账，审批流程成功完成后通过单独结算接口处理。
        /// </summary>
        [Comment("结算方式，现金或银行转账，审批流程成功完成后通过单独结算接口处理")]
        public SettlementMethodType? SettlementMethod { get; set; }

        /// <summary>
        /// 银行账户Id。当结算方式是银行时，选择本公司信息中的银行账户id，审批流程成功完成后通过单独结算接口处理。
        /// </summary>
        [Comment("银行账户Id，当结算方式是银行时选择本公司信息中的银行账户id，审批流程成功完成后通过单独结算接口处理")]
        public Guid? BankAccountId { get; set; }

        /// <summary>
        /// 审核时间。
        /// </summary>
        [Comment("审核时间")]
        [Precision(3)]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// 审核操作者Id。
        /// </summary>
        [Comment("审核操作者Id")]
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
    /// 文件明细，id挂申请单id下的文件对象。
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
            ExpenseDate = DateTime.Today;
            ExchangeRate = 1.0m;
            Currency = "CNY";
        }

        /// <summary>
        /// 申请单Id。所属申请单Id，关联到 <see cref="OaExpenseRequisition"/> 的Id。
        /// </summary>
        [Comment("申请单Id，所属申请单Id，关联到OaExpenseRequisition的Id")]
        [Required]
        public Guid ParentId { get; set; }

        /// <summary>
        /// 日常费用种类Id。关联到 <see cref="DailyFeesType"/> 的Id。
        /// </summary>
        [Comment("日常费用种类Id，关联到DailyFeesType的Id")]
        [Required]
        public Guid DailyFeesTypeId { get; set; }

        /// <summary>
        /// 费用发生时间。
        /// </summary>
        [Comment("费用发生时间")]
        public DateTime ExpenseDate { get; set; }

        /// <summary>
        /// 发票号。
        /// </summary>
        [Comment("发票号")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// 币种。标准货币缩写。
        /// </summary>
        [Comment("币种，标准货币缩写")]
        [MaxLength(4), Unicode(false)]
        public string Currency { get; set; } = "CNY";

        /// <summary>
        /// 汇率。
        /// </summary>
        [Comment("汇率")]
        [Precision(18, 6)]
        public decimal ExchangeRate { get; set; } = 1.0m;

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
            return context.Set<Account>().Find(requisition.ApplicantId);
        }

        /// <summary>
        /// 获取登记人信息。CreateBy就是登记人Id。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>登记人账号对象</returns>
        public static Account GetRegistrar(this OaExpenseRequisition requisition, DbContext context)
        {
            return context.Set<Account>().Find(requisition.CreateBy);
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
        /// 获取结算方式的显示名称。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <returns>结算方式显示名称</returns>
        public static string GetSettlementMethodDisplayName(this OaExpenseRequisition requisition)
        {
            return requisition.SettlementMethod switch
            {
                SettlementMethodType.Cash => "现金",
                SettlementMethodType.Bank => "银行转账",
                _ => "未设置"
            };
        }
    }
}