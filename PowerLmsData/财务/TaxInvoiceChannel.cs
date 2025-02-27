using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 开票渠道。
    /// </summary>
    public class TaxInvoiceChannel : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属组织机构的Id。
        /// </summary>
        [Comment("所属组织机构的Id。")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 显示名称。
        /// </summary>
        [Comment("显示名称")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 开票渠道。仅仅是一个标记，服务器通过改标识来决定调用什么接口。
        /// </summary>
        [Comment("开票渠道。仅仅是一个标记，服务器通过改标识来决定调用什么接口。")]
        public string InvoiceChannel { get; set; }

        /// <summary>
        /// 开票渠道参数。Json格式的字符串。包含敏感信息。
        /// </summary>
        [Comment("开票渠道参数。Json格式的字符串。包含敏感信息。")]
        [JsonIgnore]
        [Unicode(false)]
        public string InvoiceChannelParams { get; set; }
    }
}
