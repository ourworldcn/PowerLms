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
    /// <summary>
    /// 编码规则。
    /// </summary>
    [Comment("业务编码规则")]
    public class JobNumberRule : NamedSpecialDataDicBase
    {
        /// <summary>
        /// 规则字符串，用于定义生成格式，可以包含前缀和后缀文本。
        /// <para>以下控制代码区分大小写,用尖括号括住控制代码：</para>
        /// <list type="bullet">
        ///   <item>yy  年份的后两位（例如：25 代表 2025 年）。</item>
        ///   <item>yyyy  四位数字年份（例如：2025）。</item>
        ///   <item>M  月份，数字表示（1 到 12）。</item>
        ///   <item>MM  月份，两位数字表示（01 到 12）。</item>
        ///   <item>d  月份中的日期，数字表示（1 到 31）。</item>
        ///   <item>dd  月份中的日期，两位数字表示（01 到 31）。</item>
        ///   <item>hh  小时，24小时制，两位数字表示（00 到 23）。</item>
        ///   <item>0  序号占位符（使用零）。用对应数字替换每个 '0'。如果数字位数不足，则在前面补零。例如：序号为 123，格式为 0000，结果为 "0123"。</item>
        ///   <item>#  序号占位符（使用井号）。用对应数字替换每个 '#'。如果数字位数不足，则不显示前导占位符。例如：序号为 3，格式为 ####，结果为 "3"。</item>
        ///   <item>XXX...  员工编号占位符。使用 'X' 的数量指定要从员工编号中提取的字符位数。(具体行为如截取方向、填充等取决于实现逻辑)。</item>
        /// </list>
        /// </summary>
        [MaxLength(64)]
        public string RuleString { get; set; }
        /// <summary>
        /// 当前未用的最小编号。
        /// </summary>
        [Comment("当前未用的最小编号")]
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
        /// <summary>
        /// 业务类型Id。链接到业务大类表。
        /// </summary>
        [Comment("业务类型Id，链接到业务大类表")]
        public Guid? BusinessTypeId { get; set; }
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
