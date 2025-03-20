using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 开票渠道。其中Id属性是对应处理服务的GUID属性(如typeof(NuoNuoManager).GUID)。
    /// </summary>
    public class TaxInvoiceChannel : GuidKeyObjectBase
    {
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

    /// <summary>
    /// 开票渠道账号表。
    /// </summary>
    public class TaxInvoiceChannelAccount : JsonDynamicPropertyBase
    {
        /// <summary>
        /// 渠道Id。关联<see cref="TaxInvoiceChannel"/>。
        /// </summary>
        [Comment("渠道Id")]
        public Guid? ParentlId { get; set; }

    }
}
