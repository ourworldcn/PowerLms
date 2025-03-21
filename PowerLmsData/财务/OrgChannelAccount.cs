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
    /// 机构渠道账号表。
    /// </summary>
    public class OrgTaxChannelAccount : IMarkDelete, ICreatorInfo
    {
        #region 主键与关联字段

        /// <summary>
        /// 机构Id。关联<see cref="PlOrganization"/>。
        /// </summary>
        [Key, Column(Order = 0)]
        [Comment("机构Id")]
        public Guid OrgId { get; set; }

        /// <summary>
        /// 渠道账号Id。关联<see cref="TaxInvoiceChannelAccount"/>。
        /// </summary>
        [Key, Column(Order = 1)]
        [Comment("渠道账号Id")]
        public Guid ChannelAccountId { get; set; }

        #endregion

        #region 销方基本信息

        /// <summary>
        /// 显示名称。
        /// </summary>
        [Comment("显示名称。")]
        [MaxLength(64)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 作为销方时发票抬头。
        /// </summary>
        [Comment("作为销方时发票抬头")]
        [MaxLength(64)]
        public string InvoiceHeader { get; set; }

        /// <summary>
        /// 税务登记号（纳税人识别号）。
        /// </summary>
        [Comment("税务登记号（纳税人识别号）")]
        [MaxLength(32)]
        public string TaxpayerNumber { get; set; }

        /// <summary>
        /// 地址。
        /// </summary>
        [Comment("地址")]
        [MaxLength(128)]
        public string Address { get; set; }

        /// <summary>
        /// 电话。
        /// </summary>
        [Comment("电话")]
        [MaxLength(32)]
        public string Phone { get; set; }

        #endregion

        #region 银行信息

        /// <summary>
        /// 开户银行名称。
        /// </summary>
        [Comment("开户银行名称")]
        [MaxLength(64)]
        public string BankName { get; set; }

        /// <summary>
        /// 银行账号。
        /// </summary>
        [Comment("银行账号")]
        [MaxLength(32)]
        public string BankAccount { get; set; }

        #endregion

        #region 联系方式与附加信息

        /// <summary>
        /// 电子邮件。
        /// </summary>
        [Comment("电子邮件")]
        [MaxLength(64)]
        public string Email { get; set; }

        /// <summary>
        /// 是否为该机构的默认销方信息。
        /// </summary>
        [Comment("是否为该机构的默认销方信息")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// 备注说明。
        /// </summary>
        [Comment("备注说明")]
        [MaxLength(256)]
        public string Remark { get; set; }

        #endregion

        #region 系统记录字段

        /// <summary>
        /// 创建时间(UTC)。
        /// </summary>
        [Comment("创建时间(UTC)")]
        public DateTime CreateDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建人Id。创建时自动记录当前登录用户Id。
        /// </summary>
        [Comment("创建人Id")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 最后修改人Id。创建时自动记录当前时间。
        /// </summary>
        [Comment("最后修改人Id")]
        public Guid? LastModifyBy { get; set; }

        /// <summary>
        /// 最后修改时间(UTC)。
        /// </summary>
        [Comment("最后修改时间(UTC)。")]
        public DateTime? LastModifyUtc { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }


        #endregion
    }
}
