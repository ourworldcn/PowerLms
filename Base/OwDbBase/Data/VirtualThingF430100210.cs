/*
 * 项目：OwDbBase | 模块：通用虚拟实体
 * 功能：通用虚拟实体，字段组成由类名后缀编码描述，供上层应用继承使用
 * 命名规则：VirtualThingF[N][U][A][D][F][I][G][T][B]
 *   N  = 支持的字段类型种数（十六进制，0~F）
 *   U  = Unicode字符串字段数（十六进制，0~F）
 *   A  = 非Unicode字符串字段数（十六进制，0~F）
 *   D  = 定点数（decimal）字段数（十六进制，0~F）
 *   F  = 浮点数字段数（十六进制，0~F）
 *   I  = 整型字段数（十六进制，0~F）
 *   G  = Guid字段数（十六进制，0~F）
 *   T  = 日期时间字段数（十六进制，0~F）
 *   B  = 布尔字段数（十六进制，0~F）
 * 本类：VirtualThingF430100210
 *   N=4：支持Unicode字符串、定点数、Guid、日期时间 共4种类型
 *   U=3：JsonObjectString、ExtraString、ExtraString2
 *   A=0，D=1：ExtraDecimal，F=0，I=0
 *   G=2：ParentId、ExtraGuid
 *   T=1：ExtraDateTime，B=0
 * 作者：zc | 创建：2026-03
 */
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace OW.Data
{
    /// <summary>
    /// 通用虚拟实体。命名规则：VirtualThingF[N][U][A][D][F][I][G][T][B]，
    /// N = 支持字段类型种数，其余各位依次为Unicode字符串数、非Unicode字符串数、定点数、浮点数、整型数、Guid数、日期数、布尔数，均为十六进制（0~F）。
    /// 本类（VirtualThingF430100210）支持4种类型，包含：Unicode字符串×3（JsonObjectString、ExtraString、ExtraString2）、定点×1（ExtraDecimal）、Guid×2（ParentId、ExtraGuid）、日期×1（ExtraDateTime）。
    /// 上层应用应继承此类并映射到具体业务表，而非直接使用本类。
    /// </summary>
    [Comment("通用虚拟实体：支持4种类型，Unicode字符串×3，定点×1，Guid×2，日期×1")]
    public class VirtualThingF430100210 : VirtualThingF4
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public VirtualThingF430100210()
        {
        }

        /// <summary>
        /// 扩展字符串二（Unicode字符串字段第3个，最长64字符）；含义由调用方约定，可用于存储附加分类或标识信息。
        /// </summary>
        [Comment("扩展字符串二；含义由调用方约定，可用于存储附加分类或标识信息。")]
        [MaxLength(64)]
        public string ExtraString2 { get; set; }
    }
}
