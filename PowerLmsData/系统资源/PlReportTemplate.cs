/*
 * 项目：PowerLmsData | 模块：系统资源
 * 功能：报表模板实体，继承通用虚拟实体基类 VirtualThingF430100210
 * 技术要点：EF Core实体，继承 OW.Data.VirtualThingF430100210，映射到报表模板表
 * 作者：zc | 创建：2026-03
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 报表模板。继承自通用虚拟实体基类 <see cref="VirtualThingF430100210"/>，
    /// 字段含义：ParentId=商户Id，ExtraString2=模板分类，JsonObjectString=模板内容（JSON），
    /// ExtraGuid=创建人Id，ExtraString=模板名称，ExtraDateTime=创建时间，ExtraDecimal=扩展数值。
    /// </summary>
    [Comment("报表模板")]
    [Index(nameof(ExtraString), IsUnique = false)]
    [Index(nameof(ExtraString2), IsUnique = false)]
    [Index(nameof(ExtraGuid), IsUnique = false)]
    public class PlReportTemplate : VirtualThingF430100210
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlReportTemplate()
        {
        }

        /// <summary>
        /// 创建时间（精度到毫秒）；重写基类字段以限定存储精度。
        /// </summary>
        [Precision(3)]
        public override DateTime? ExtraDateTime { get; set; }

        /// <summary>
        /// 扩展数值（精度18位，保留4位小数）；重写基类字段以限定存储精度。
        /// </summary>
        [Precision(18, 4)]
        public override decimal? ExtraDecimal { get; set; }
    }
}
