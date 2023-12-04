using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /*
     * <yy>	年份（00 到 99）
     * <yyyy>	由四位数字表示的年份。
     * <M>	月份（1 到 12）。
     * <MM>	月份（01 到 12）。
     * <d>	一个月中的某一天（1 到 31）。
     * <dd>	一个月中的某一天（01 到 31）。
     * <H>	采用 24 小时制的小时（从 0 到 23）
     * <HH>	采用 24 小时制的小时（从 00 到 23）
     * <0>	序号	用对应的数字（如果存在）替换零；否则，将在结果字符串中显示零。如123(<0000>) -> 0123。
     * <#>	序号	用对应的数字（如果存在）替换<#>符号；否则，不会在结果字符串中显示任何数字。请注意，如果输入字符串中的相应数字是无意义的 0，则在结果字符串中不会出现任何数字。 例如，0003 (<####>) -> 3。
    */
    /// <summary>
    /// 编码规则。
    /// </summary>
    [Comment("业务编码规则")]
    public class JobNumberRule : NamedSpecialDataDicBase
    {
        /// <summary>
        /// 前缀。这个属性不是Code。
        /// </summary>
        [Comment("前缀")]
        [MaxLength(8)]
        public string Prefix { get; set; }

        /// <summary>
        /// 规则字符串。不包含前缀。
        /// </summary>
        [Comment("规则字符串")]
        [MaxLength(64)]
        public string RuleString { get; set; }

        /// <summary>
        /// 当前已用的最大编号。
        /// </summary>
        [Comment("当前编号")]
        public int CurrentNumber { get; set; }

        /// <summary>
        /// 归零方式，0不归零，1按年，2按月，3按日
        /// </summary>
        [Comment("归零方式，0不归零，1按年，2按月，3按日")]
        public short RepeatMode { get; set; }

        /// <summary>
        /// "归零"后的起始值。
        /// </summary>
        [Comment("\"归零\"后的起始值")]
        public int StartValue { get; set; }

        /// <summary>
        /// 记录最后一次归零的日期。
        /// </summary>
        [Comment("记录最后一次归零的日期")]
        public DateTime RepeatDate { get; set; }
    }

    /// <summary>
    /// 可重用的序列号。
    /// </summary>
    [Comment("可重用的序列号")]
    public class JobNumberReusable : GuidKeyObjectBase
    {
        /// <summary>
        /// 规则Id。
        /// </summary>
        [Comment("规则Id")]
        public Guid RuleId { get; set; }

        /// <summary>
        /// 回收的时间。
        /// </summary>
        [Comment("回收的时间")]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 可重用的序列号。
        /// </summary>
        [Comment("可重用的序列号")]
        public int Seq { get; set; }
    }
}
