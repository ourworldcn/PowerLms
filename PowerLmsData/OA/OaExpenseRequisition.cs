/*
 * 数据访问层
 * OA费用申请单模块 - 数据实体和业务扩展方法
 * 
 * 功能说明：
 * - OA日常费用申请单数据模型定义
 * - 申请单明细项数据模型定义
 * - 结算与确认流程状态管理
 * - 编辑权限控制扩展方法
 * 
 * 技术特点：
 * - 基于Entity Framework Core的数据建模
 * - 多租户数据隔离支持
 * - 二进制位状态枚举设计
 * - 状态驱动的编辑权限控制
 * 
 * 作者：PowerLms团队
 * 创建时间：2024
 * 最后修改：2025-01-27 - 新增结算确认流程状态管理和编辑权限控制
 */
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
    /// OA费用申请单状态枚举。采用二进制位值设计，支持位运算操作。
    /// 可选值说明：
    /// 0=草稿（可完全编辑）, 
    /// 1=审批中（不能修改金额汇率等主要字段）, 
    /// 2=审批完成待结算, 
    /// 4=已结算待确认（明细项不可修改）, 
    /// 8=已确认可导入财务（总单和明细完全锁定）, 
    /// 16=已导入财务（完全锁定）, 
    /// 32=审批被拒绝（可重新编辑并再次提交审批）
    /// </summary>
    public enum OaExpenseStatus : byte
    {
        /// <summary>
        /// 0 - 草稿状态，可完全编辑
        /// </summary>
        Draft = 0,
        
        /// <summary>
        /// 1 - 审批中，不能修改金额汇率等主要字段
        /// </summary>
        InApproval = 1,
        
        /// <summary>
        /// 2 - 审批完成，待结算
        /// </summary>
        ApprovedPendingSettlement = 2,
        
        /// <summary>
        /// 4 - 已结算，待确认。明细项不可修改
        /// </summary>
        SettledPendingConfirm = 4,
        
        /// <summary>
        /// 8 - 已确认，可导入财务。总单和明细完全锁定
        /// </summary>
        ConfirmedReadyForExport = 8,
        
        /// <summary>
        /// 16 - 已导入财务，完全锁定
        /// </summary>
        ExportedToFinance = 16,
        
        /// <summary>
        /// 32 - 审批被拒绝。可重新编辑并再次提交审批
        /// </summary>
        Rejected = 32
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
            Status = OaExpenseStatus.Draft; // 默认为草稿状态
        }

        /// <summary>
        /// 所属机构Id。设置数据隔离的机构Id,即记录当前记录的机构Id，运行时确定，不可修改。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 申请编号。唯一标识申请单的编号，由系统根据"其他编码规则"自动生成。
        /// 用于区分同一人提交的多笔金额相同的申请。
        /// </summary>
        [Comment("申请编号。唯一标识申请单的编号")]
        [MaxLength(128)]
        public string ApplicationNumber { get; set; }

        /// <summary>
        /// 申请人Id（员工账号Id）。
        /// 已废弃：请使用 CreateBy 字段记录创建人/登记人/申请人信息。
        /// </summary>
        [Comment("申请人Id（员工账号Id）。已废弃字段，请使用CreateBy")]
        [Obsolete("已废弃原申请人字段，后续不再使用。费用申请单的逻辑调整为：创建人、登记人、申请人三者都使用CreateBy字段记录。")]
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
        /// 客户Id。关联客户资料表，用于选择具体的客户/公司。
        /// </summary>
        [Comment("客户Id。关联客户资料表，用于选择具体的客户/公司")]
        public Guid? CustomerId { get; set; }

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
        /// 申请单状态。采用二进制位值，支持位运算操作。
        /// </summary>
        [Comment("申请单状态，采用二进制位值")]
        public OaExpenseStatus Status { get; set; } = OaExpenseStatus.Draft;

        #region 结算相关字段（第一步：出纳操作）
        
        /// <summary>
        /// 结算操作人Id。执行结算操作的出纳人员Id。
        /// </summary>
        [Comment("结算操作人Id，执行结算操作的出纳人员Id")]
        public Guid? SettlementOperatorId { get; set; }
        
        /// <summary>
        /// 结算时间。出纳执行结算操作的时间。
        /// </summary>
        [Comment("结算时间，出纳执行结算操作的时间")]
        [Precision(3)]
        public DateTime? SettlementDateTime { get; set; }
        
        /// <summary>
        /// 结算方式。现金/银行转账等结算方式说明。
        /// </summary>
        [Comment("结算方式，现金或银行转账等结算方式说明")]
        [MaxLength(50)]
        public string SettlementMethod { get; set; }
        
        /// <summary>
        /// 结算备注。结算相关的备注说明。
        /// </summary>
        [Comment("结算备注，结算相关的备注说明")]
        [MaxLength(500)]
        public string SettlementRemark { get; set; }

        #endregion

        #region 确认相关字段（第二步：会计操作）
        
        /// <summary>
        /// 确认操作人Id。执行确认操作的会计人员Id。
        /// </summary>
        [Comment("确认操作人Id，执行确认操作的会计人员Id")]
        public Guid? ConfirmOperatorId { get; set; }
        
        /// <summary>
        /// 确认时间。会计执行确认操作的时间。
        /// </summary>
        [Comment("确认时间，会计执行确认操作的时间")]
        [Precision(3)]
        public DateTime? ConfirmDateTime { get; set; }
        
        /// <summary>
        /// 银行流水号。用于确认的银行流水号。
        /// </summary>
        [Comment("银行流水号，用于确认的银行流水号")]
        [MaxLength(100)]
        public string BankFlowNumber { get; set; }
        
        /// <summary>
        /// 确认备注。确认相关的备注说明。
        /// </summary>
        [Comment("确认备注，确认相关的备注说明")]
        [MaxLength(500)]
        public string ConfirmRemark { get; set; }

        #endregion

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
        /// 创建者Id。统一记录创建人、登记人、申请人信息。
        /// 创建人：记录谁发起了申请单。
        /// 登记人：实际登记费用的人。
        /// 申请人：保留为业务上的申请主体，如有特殊场景区分。
        /// </summary>
        [Comment("创建者Id。统一记录创建人、登记人、申请人信息")]
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
        /// 注意：ApplicantId字段已废弃，此方法保留仅为向后兼容。
        /// 推荐使用 GetRegistrar() 方法获取创建人/登记人/申请人信息。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>申请人账号对象</returns>
        [Obsolete("ApplicantId字段已废弃，请使用CreateBy统一记录创建人/登记人/申请人信息，推荐使用GetRegistrar()方法")]
        public static Account GetApplicant(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.ApplicantId.HasValue ? context.Set<Account>().Find(requisition.ApplicantId.Value) : null;
        }

        /// <summary>
        /// 获取创建人/登记人/申请人信息。CreateBy统一记录这些角色信息。
        /// 创建人：记录谁发起了申请单。
        /// 登记人：实际登记费用的人。  
        /// 申请人：保留为业务上的申请主体，如有特殊场景区分。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>创建人账号对象</returns>
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
        /// 获取结算操作人信息。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>结算操作人账号对象</returns>
        public static Account GetSettlementOperator(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.SettlementOperatorId.HasValue ? context.Set<Account>().Find(requisition.SettlementOperatorId.Value) : null;
        }

        /// <summary>
        /// 获取确认操作人信息。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>确认操作人账号对象</returns>
        public static Account GetConfirmOperator(this OaExpenseRequisition requisition, DbContext context)
        {
            return requisition.ConfirmOperatorId.HasValue ? context.Set<Account>().Find(requisition.ConfirmOperatorId.Value) : null;
        }

        #region 编辑权限控制扩展方法

        /// <summary>
        /// 判断申请单是否可以编辑。基于状态的全面编辑权限控制。
        /// 草稿状态和被拒绝状态可以完全编辑。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文（可选）</param>
        /// <returns>可以编辑返回true，否则返回false</returns>
        public static bool CanEdit(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return requisition.Status == OaExpenseStatus.Draft || 
                   requisition.Status == OaExpenseStatus.Rejected; // 草稿和被拒绝状态可以编辑
        }

        /// <summary>
        /// 判断申请单主要字段（金额、汇率、币种）是否可以编辑。
        /// 进入审批工作流后不能修改总单上的金额与汇率，但被拒绝后可以修改。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文（可选）</param>
        /// <returns>可以编辑主要字段返回true，否则返回false</returns>
        public static bool CanEditMainFields(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return requisition.Status == OaExpenseStatus.Draft || 
                   requisition.Status == OaExpenseStatus.Rejected; // 草稿和被拒绝状态可以修改主要字段
        }

        /// <summary>
        /// 判断申请单明细是否可以编辑。
        /// 结算后不能修改明细项。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文（可选）</param>
        /// <returns>可以编辑明细返回true，否则返回false</returns>
        public static bool CanEditItems(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return requisition.Status < OaExpenseStatus.SettledPendingConfirm; // 结算后不能修改明细项
        }

        /// <summary>
        /// 判断申请单是否完全不可编辑。
        /// 确认后总单和明细都不能修改。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文（可选）</param>
        /// <returns>完全锁定返回true，否则返回false</returns>
        public static bool IsCompletelyLocked(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return requisition.Status >= OaExpenseStatus.ConfirmedReadyForExport; // 确认后完全不可编辑
        }

        #endregion

        /// <summary>
        /// 判断申请单是否已审核。兼容原有逻辑。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <returns>已审核返回true，否则返回false</returns>
        public static bool IsAudited(this OaExpenseRequisition requisition)
        {
            return requisition.AuditDateTime.HasValue;
        }

        /// <summary>
        /// 获取申请单的审批状态。基于新状态枚举的状态显示。
        /// </summary>
        /// <param name="requisition">申请单</param>
        /// <param name="context">数据库上下文</param>
        /// <returns>审批状态描述</returns>
        public static string GetApprovalStatus(this OaExpenseRequisition requisition, DbContext context = null)
        {
            return requisition.Status switch
            {
                OaExpenseStatus.Draft => "草稿",
                OaExpenseStatus.InApproval => "审批中",
                OaExpenseStatus.ApprovedPendingSettlement => "待结算",
                OaExpenseStatus.SettledPendingConfirm => "待确认",
                OaExpenseStatus.ConfirmedReadyForExport => "可导入财务",
                OaExpenseStatus.ExportedToFinance => "已导入财务",
                OaExpenseStatus.Rejected => "审批被拒绝",
                _ => "未知状态"
            };
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

    /// <summary>
    /// OA费用申请单状态流转控制静态类。
    /// </summary>
    public static class OaExpenseStatusTransition
    {
        /// <summary>
        /// 判断申请单是否可以执行结算操作。
        /// </summary>
        /// <param name="requisition">申请单实体</param>
        /// <returns>可以结算返回true，否则返回false</returns>
        public static bool CanSettle(this OaExpenseRequisition requisition)
        {
            // 1. 状态必须是 ApprovedPendingSettlement
            // 2. 工作流完成状态检查由控制器层负责
            // 3. 权限检查由控制器层负责
            return requisition.Status == OaExpenseStatus.ApprovedPendingSettlement;
        }
        
        /// <summary>
        /// 判断申请单是否可以执行确认操作。
        /// </summary>
        /// <param name="requisition">申请单实体</param>
        /// <param name="currentUserId">当前用户Id</param>
        /// <returns>可以确认返回true，否则返回false</returns>
        public static bool CanConfirm(this OaExpenseRequisition requisition, Guid currentUserId)
        {
            // 1. 状态必须是 SettledPendingConfirm
            // 2. 确认人不能是结算人（职责分离）
            // 3. 权限检查由控制器层负责
            return requisition.Status == OaExpenseStatus.SettledPendingConfirm 
                && currentUserId != requisition.SettlementOperatorId;
        }
    }
}