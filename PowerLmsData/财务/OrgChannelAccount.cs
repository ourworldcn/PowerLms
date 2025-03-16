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
    public class OrgTaxChannelAccount
    {
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

        /// <summary>
        /// 作为销方时发票抬头。
        /// </summary>
        [Comment("作为销方时发票抬头")]
        [MaxLength(64)]
        public string InvoiceHeader { get; set; }
    }
}
