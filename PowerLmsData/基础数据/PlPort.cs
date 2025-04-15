using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 港口类。CustomId 对应原系统三字码（海关码）。DisplayName 对应原有 中英文名称（看看就好）。Code 复制自原有三字码。Id 取代原有 ZDBH。
    /// </summary>
    [Comment("港口")]
    public class PlPort : NamedSpecialDataDicBase
    {
        /// <summary>
        /// 海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。
        /// </summary>
        [Comment("海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。")]
        public string CustomsCode { get; set; }

        /// <summary>
        /// 国家Id。建议使用CountryCode属性替代。
        /// </summary>
        [Comment("国家Id。建议使用CountryCode属性替代。")]
        [Obsolete("请使用CountryCode属性替代")]
        public Guid? CountryId { get; set; }

        /// <summary>
        /// 国家代码。使用标准的国家代码（例如：CN-中国，US-美国）。
        /// </summary>
        [Comment("国家代码。使用标准的国家代码。")]
        [MaxLength(3)]  //放宽到3个字符，原有是2个字符
        [Unicode(false)] // 使用ASCII编码存储
        public string CountryCode { get; set; }

        /// <summary>
        /// 省。
        /// </summary>
        [Comment("省")]
        public string Province { get; set; }

        /// <summary>
        /// 数字码.可空。
        /// </summary>
        [Comment("数字码.可空")]
        public int? NumCode { get; set; }

        /// <summary>
        /// 所属航线Id。
        /// </summary>
        [Comment("所属航线Id")]
        public Guid? PlCargoRouteId { get; set; }
    }
    /// <summary>
    /// 航线类。
    /// </summary>
    [Comment("航线")]
    public class PlCargoRoute : NamedSpecialDataDicBase
    {
        /// <summary>
        /// CAF比率，取%值。
        /// </summary>
        [Comment("CAF比率，取%值。")]
        public int? CAFRate { get; set; }
    }
}
