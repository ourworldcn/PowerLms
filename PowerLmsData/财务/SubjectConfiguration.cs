using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 财务科目设置表
    /// </summary>
    [Comment("财务科目设置表")]
    [Index(nameof(OrgId), nameof(Code), IsUnique = true)]
    public class SubjectConfiguration : GuidKeyObjectBase, ISpecificOrg, IMarkDelete, ICreatorInfo
    {
        /// <summary>
        /// 所属组织机构Id
        /// </summary>
        [Comment("所属组织机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 服务器用此编码来标识该数据用于什么地方。支持以下配置项编码（可能持续增加）：
        /// 
        /// 通用科目 (GEN):
        /// GEN_COGS - 主营业务成本
        /// GEN_ADVANCE_PAYMENT - 代垫项（代收代付科目）
        /// GEN_PREPARER - 制单人（金蝶制单人名称）
        /// GEN_VOUCHER_GROUP - 凭证类别字（如：转、收、付、记）
        /// 
        /// 发票挂账科目 (PBI):
        /// PBI_SALES_REVENUE - 主营业务收入
        /// PBI_TAX_PAYABLE - 应交税金
        /// PBI_ACC_RECEIVABLE - 应收账款
        /// 
        /// 实收科目 (RF):
        /// RF_BANK_DEPOSIT - 银行存款（收款银行存款）
        /// RF_ACC_RECEIVABLE - 应收账款（冲销应收）
        /// 
        /// 实付科目 (PF):
        /// PF_BANK_DEPOSIT - 银行存款（付款银行存款）
        /// PF_ACC_PAYABLE - 应付账款
        /// 
        /// A账应收计提科目 (ARAB):
        /// ARAB_TOTAL - 计提总应收
        /// ARAB_IN_CUS - 计提应收国内-客户
        /// ARAB_IN_TAR - 计提应收国内-关税
        /// ARAB_OUT_CUS - 计提应收国外-客户
        /// ARAB_OUT_TAR - 计提应收国外-关税
        /// 
        /// A账应付计提科目 (APAB):
        /// APAB_TOTAL - 计提总应付
        /// APAB_IN_SUP - 计提应付国内-供应商
        /// APAB_IN_TAR - 计提应付国内-关税
        /// APAB_OUT_SUP - 计提应付国外-供应商
        /// APAB_OUT_TAR - 计提应付国外-关税
        /// </summary>
        [Comment("配置项编码")]
        [MaxLength(32), Unicode(false)]
        [Required(AllowEmptyStrings = false)]
        public string Code { get; set; }

        /// <summary>
        /// 会计科目编码。
        /// </summary>
        [Comment("会计科目编码")]
        [MaxLength(32), Unicode(false)]
        public string SubjectNumber { get; set; }

        /// <summary>
        /// 显示名称。
        /// </summary>
        [Comment("显示名称。")]
        [MaxLength(128)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 凭证类别字。
        /// 常用值：转（转账凭证）、收（收款凭证）、付（付款凭证）、记（记账凭证）
        /// </summary>
        [Comment("凭证类别字")]
        [MaxLength(8)]
        public string VoucherGroup { get; set; }

        /// <summary>
        /// 核算类别。
        /// 常用值：客户、供应商、部门、员工、项目、地区等
        /// </summary>
        [Comment("核算类别")]
        [MaxLength(8)]
        public string AccountingCategory { get; set; }

        /// <summary>
        /// 制单人（金蝶制单人名称）
        /// </summary>
        [Comment("制单人（金蝶制单人名称）")]
        [MaxLength(64)]
        public string Preparer { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        #region IMarkDelete
        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }
        #endregion

        #region ICreatorInfo
        /// <summary>
        /// 创建者的唯一标识
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间
        /// </summary>
        [Comment("创建的时间")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;
        #endregion IMarkDelete
    }
}