/*
 * 项目：OwDbBase | 模块：通用虚拟实体
 * 功能：通用虚拟实体抽象基类，供具体字段组合的虚拟实体类继承
 * 命名规则：VirtualThingF[N]，N = 该类及其派生类所支持的字段类型种数（十六进制，0~F）
 * 本类：VirtualThingF4，支持4种字段类型：Unicode字符串、定点数（decimal）、Guid、日期时间
 * 作者：zc | 创建：2026-03
 */
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OW.Data
{
    /// <summary>
    /// 通用虚拟实体抽象基类。命名规则：VirtualThingF[N]，N 为该类族支持的字段类型种数（十六进制，0~F）。
    /// 本类（VirtualThingF4）支持 4 种字段类型：Unicode字符串、定点数（decimal）、Guid、日期时间。
    /// 具体字段数量由派生类的名称后缀编码描述，参见派生类注释。
    /// </summary>
    [Index(nameof(ParentId), IsUnique = false)]
    public class VirtualThingF4 : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public VirtualThingF4()
        {
        }
        /// <summary>
        /// 副ID（Guid字段第1个）；含义由调用方约定，可指向任意关联对象Id。
        /// </summary>
        [Comment("副ID；含义由调用方约定，可指向任意关联对象Id。")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// JSON对象字符串（Unicode字符串字段第1个）；前端解析定义，含义由调用方约定。
        /// </summary>
        [Comment("JSON对象字符串（前端解析定义）")]
        public string JsonObjectString { get; set; }

        /// <summary>
        /// 额外Guid（Guid字段第2个）；含义由调用方约定，可用于关联扩展对象或存储附加标识。
        /// </summary>
        [Comment("额外Guid；含义由调用方约定，可用于关联扩展对象或存储附加标识。")]
        public Guid? ExtraGuid { get; set; }

        /// <summary>
        /// 额外字符串（Unicode字符串字段第2个，最长128字符）；含义由调用方约定，可用于存储附加文本信息。
        /// </summary>
        [Comment("额外字符串；含义由调用方约定，可用于存储附加文本信息。")]
        [MaxLength(128)]
        public string ExtraString { get; set; }

        /// <summary>
        /// 额外日期时间（日期字段第1个）；含义由调用方约定，可用于存储附加时间信息。
        /// </summary>
        [Comment("额外日期时间；含义由调用方约定，可用于存储附加时间信息。")]
        public virtual DateTime? ExtraDateTime { get; set; }

        /// <summary>
        /// 扩展定点数（定点字段第1个）；含义由调用方约定，可用于存储附加数值信息，如金额等。
        /// </summary>
        [Comment("扩展定点数；含义由调用方约定，可用于存储附加数值信息，如金额等。")]
        public virtual decimal? ExtraDecimal { get; set; }
    }
}
