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
    /// 港口类型枚举。
    /// 用于区分空运港口（IATA三字码）和海运港口（船公司四/五位码）。
    /// </summary>
    public enum PortType : byte
    {
        /// <summary>
        /// 空运港口，使用IATA三字码（如LAX）。
        /// </summary>
        [Display(Name = "空运")]
        Air = 1,

        /// <summary>
        /// 海运港口，使用船公司四/五位码（如USLAX）。
        /// </summary>
        [Display(Name = "海运")]
        Sea = 2
    }

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
        /// 所属航线Id。不推荐使用，请使用CargoRouteCode替代。
        /// </summary>
        [Comment("所属航线Id。不推荐使用，请使用CargoRouteCode替代。")]
        [Obsolete("请使用CargoRouteCode属性替代")]
        public Guid? PlCargoRouteId { get; set; }

        /// <summary>
        /// 所属航线编码。关联到PlCargoRoute.Code，可为空表示该港口不属于任何航线。
        /// </summary>
        [Comment("所属航线编码。可为空表示该港口不属于任何航线。")]
        [MaxLength(32)]
        [Unicode(false)] // 使用ASCII编码存储
        public string CargoRouteCode { get; set; }

        /// <summary>
        /// 港口类型。用于区分空运港口（IATA三字码）和海运港口（船公司四/五位码）。
        /// 空运港口使用三字码（如LAX），海运港口使用四/五位码（如USLAX）。
        /// 同一地点的空港和海港需要分别建立两条记录。
        /// </summary>
        [Comment("港口类型。1=空运，2=海运")]
        public PortType? PortType { get; set; }
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
