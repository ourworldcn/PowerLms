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
    public class InvoiceInfo : GuidKeyObjectBase
    {
        /// <summary>
        /// 自动或手动。true=自动，false=手动。
        /// </summary>
        [Comment("自动或手动。true=自动，false=手动。")]
        public bool IsAuto{ get; set; }

        /// <summary>
        /// 所属组织机构的Id。
        /// </summary>
        [Comment("所属组织机构的Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 申请单号。
        /// </summary>
        [Comment("申请单号")]
        [MaxLength(64)]
        public string ApplicationNumber { get; set; }

        /// <summary>
        /// 发送时间。
        /// </summary>
        [Comment("发送时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? SendTime { get; set; }

        /// <summary>
        /// 发票号。
        /// </summary>
        [Comment("发票号")]
        [MaxLength(64)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// 返回发票号时间。
        /// </summary>
        [Comment("返回发票号时间")]
        public DateTime? ReturnInvoiceTime { get; set; }

        /// <summary>
        /// 推送手机号。
        /// </summary>
        [Comment("推送手机号")]
        [MaxLength(32)]
        public string Mobile { get; set; }

        /// <summary>
        /// 推送Mail。
        /// </summary>
        [Comment("推送Mail")]
        [MaxLength(256), EmailAddress]
        public string Mail { get; set; }

        /// <summary>
        /// 开票项目名（产品）。
        /// </summary>
        [Comment("开票项目名（产品）")]
        [MaxLength(256)]
        public string InvoiceItemName { get; set; }

        /// <summary>
        /// 销方开票数据。
        /// </summary>
        [Comment("销方开票数据")]
        public string SellerInvoiceData { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        [MaxLength(256)]
        public string Remark { get; set; }

        /// <summary>
        /// 发票流水号。
        /// </summary>
        [Comment("发票流水号")]
        [MaxLength(64)]
        public string InvoiceSerialNum { get; set; }

        /// <summary>
        /// 审核时间。
        /// </summary>
        [Comment("审核时间")]
        [Column(TypeName = "DATETIME2(3)")]
        public DateTime? AuditDateTime { get; set; }

        /// <summary>
        /// 审核人。
        /// </summary>
        [Comment("审核人")]
        [MaxLength(64)]
        public string Auditor { get; set; }
    }
}
