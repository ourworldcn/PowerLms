using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 开票信息。记录开的发票实质填写的信息。
    /// </summary>
    public class TaxInvoiceInfo : GuidKeyObjectBase
    {
        #region 基本信息
        /// <summary>开票渠道Id。关联到<see cref="TaxInvoiceChannel"/>。</summary>
        [Comment("开票渠道Id")]
        public Guid? TaxInvoiceChannelId { get; set; }

        /// <summary>发票状态。0：创建后待审核；1：已审核开票中；2：已开票。</summary>
        [Comment("发票状态。0：创建后待审核；1：已审核开票中；2：已开票")]
        public byte Status { get; set; }

        /// <summary>所属组织机构的Id。冗余字段。</summary>
        [Comment("所属组织机构的Id")]
        public Guid? OrgId { get; set; }

        /// <summary>费用申请单Id。关联到<see cref="DocFeeRequisition"/>。</summary>
        [Comment("费用申请单Id")]
        public Guid? DocFeeRequisitionId { get; set; }

        /// <summary>发票号。</summary>
        [Comment("发票号")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }

        /// <summary>发票流水号。</summary>
        [Comment("发票流水号")]
        [MaxLength(64)]
        public string InvoiceSerialNum { get; set; }

        /// <summary>发票类型。如：增值税专用发票、增值税普通发票等。</summary>
        [Comment("发票类型")]
        [MaxLength(64)]
        public string InvoiceType { get; set; }

        /// <summary>开票项目名（产品）。</summary>
        [Comment("开票项目名（产品）")]
        [MaxLength(256)]
        public string InvoiceItemName { get; set; }

        /// <summary>备注。</summary>
        [Comment("备注")]
        [MaxLength(256)]
        public string Remark { get; set; }
        #endregion

        #region 时间信息
        /// <summary>申请时间。</summary>
        [Comment("申请时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? ApplyDateTime { get; set; }

        /// <summary>审核时间。</summary>
        [Comment("审核时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>发送时间。</summary>
        [Comment("发送时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? SendTime { get; set; }

        /// <summary>返回发票号时间。</summary>
        [Comment("返回发票号时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? ReturnInvoiceTime { get; set; }
        #endregion

        #region 人员信息
        /// <summary>申请人Id。</summary>
        [Comment("申请人Id")]
        public Guid? ApplicantId { get; set; }

        /// <summary>审核人Id。</summary>
        [Comment("审核人Id")]
        public Guid? AuditorId { get; set; }
        #endregion

        #region 联系方式
        /// <summary>推送手机号。</summary>
        [Comment("推送手机号")]
        [MaxLength(32)]
        public string Mobile { get; set; }

        /// <summary>推送Mail。</summary>
        [Comment("推送Mail")]
        [MaxLength(256), EmailAddress]
        public string Mail { get; set; }
        #endregion

        #region 销方信息
        /// <summary>销方开票数据。</summary>
        [Comment("销方开票数据")]
        public string SellerInvoiceData { get; set; }

        /// <summary>销方抬头。</summary>
        [Comment("销方抬头")]
        [MaxLength(256)]
        public string SellerTitle { get; set; }

        /// <summary>销方税号。</summary>
        [Comment("销方税号")]
        [MaxLength(64)]
        public string SellerTaxNum { get; set; }

        /// <summary>销方开户行。</summary>
        [Comment("销方开户行")]
        [MaxLength(64)]
        public string SellerBank { get; set; }

        /// <summary>销方账号。</summary>
        [Comment("销方账号")]
        [MaxLength(64)]
        public string SellerAccount { get; set; }

        /// <summary>销方地址。</summary>
        [Comment("销方地址")]
        [MaxLength(256)]
        public string SellerAddress { get; set; }

        /// <summary>销方电话。</summary>
        [Comment("销方电话")]
        [MaxLength(32)]
        public string SellerTel { get; set; }
        #endregion

        #region 购方信息
        /// <summary>购方抬头。</summary>
        [Comment("购方抬头")]
        [MaxLength(256)]
        public string BuyerTitle { get; set; }

        /// <summary>购方税号。</summary>
        [Comment("购方税号")]
        [MaxLength(64)]
        public string BuyerTaxNum { get; set; }

        /// <summary>购方开户行。</summary>
        [Comment("购方开户行")]
        [MaxLength(64)]
        public string BuyerBank { get; set; }

        /// <summary>购方账号。</summary>
        [Comment("购方账号")]
        [MaxLength(64)]
        public string BuyerAccount { get; set; }

        /// <summary>购方地址。</summary>
        [Comment("购方地址")]
        [MaxLength(256)]
        public string BuyerAddress { get; set; }

        /// <summary>购方电话。</summary>
        [Comment("购方电话")]
        [MaxLength(32)]
        public string BuyerTel { get; set; }
        #endregion
    }

    /// <summary>
    /// 客户税务信息/开票信息细项。
    /// </summary>
    public class TaxInvoiceInfoItem : GuidKeyObjectBase
    {
        #region 基本信息
        /// <summary>客户税务信息/开票信息Id，关联<see cref="TaxInvoiceInfo"/>。</summary>
        [Comment("客户税务信息/开票信息Id")]
        public Guid? ParentId { get; set; }

        /// <summary>商品名称。</summary>
        [Comment("商品名称")]
        public string GoodsName { get; set; }
        #endregion

        #region 金额信息
        /// <summary>数量。</summary>
        [Comment("数量")]
        public decimal Quantity { get; set; }

        /// <summary>单价（不含税）。</summary>
        [Comment("单价（不含税）")]
        [Precision(18, 2)]
        public decimal UnitPrice { get; set; }

        /// <summary>税率。</summary>
        [Comment("税率")]
        [Precision(18, 2)]
        public decimal TaxRate { get; set; }
        #endregion
    }


}
