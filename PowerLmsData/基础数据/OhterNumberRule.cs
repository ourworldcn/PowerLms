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
    /// 其它编码规则类。
    /// </summary>
    [Comment("其他编码规则")]
    [Index(nameof(OrgId), nameof(Code))]
    public class OtherNumberRule : GuidKeyObjectBase, IMarkDelete
    {
        /*
         * [XMMC] [varchar](20) NOT NULL,
	[ZWMC] [varchar](50) NOT NULL,
	[ZDBH] [int] NOT NULL,
	[BHGZ] [varchar](100) NULL,
	[GLFS] [char](1) NULL,
	[GLZ] [int] NULL,
	[LJRQ] [char](10) NULL,
         * */

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OtherNumberRule() { }

        /// <summary>
        /// 所属商户/机构Id。
        /// </summary>
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 规则编码（标准英文名）。
        /// </summary>
        [Comment("规则编码（标准英文名）")]
        [MaxLength(32), Unicode(false)]
        public string Code { get; set; }

        /// <summary>
        /// 规则显示名称。
        /// </summary>
        [Comment("规则显示名称")]
        [MaxLength(32)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 当前未用的最小编号。
        /// </summary>
        [Comment("当前未用的最小编号")]
        public int CurrentNumber { get; set; }

        /// <summary>
        /// 规则字符串。包含前缀，后缀。
        /// 以下控制代码区分大小写。
        /// &lt;yy&gt;	年份（00 到 99）
        /// &lt;yyyy&gt;	由四位数字表示的年份。
        /// &lt;M&gt;	月份（1 到 12）。
        /// &lt;MM&gt;	月份（01 到 12）。
        /// &lt;d&gt;	一个月中的某一天（1 到 31）。
        /// &lt;dd&gt;	一个月中的某一天（01 到 31）。
        /// &lt;hh&gt;	采用 24 小时制的小时（从 00 到 23）
        /// &lt;0&gt;	序号 用对应的数字（如果存在）替换零；否则，将在结果字符串中显示零。如123(&lt;0000&gt;) -&gt; 0123。
        /// &lt;#&gt;	序号	用对应的数字（如果存在）替换&lt;#&gt;符号；否则，不会在结果字符串中显示任何数字。请注意，如果输入字符串中的相应数字是无意义的 0，则在结果字符串中不会出现任何数字。 例如，0003 (&lt;####&gt;) -&gt; 3。
        /// &lt;XXX&gt;   员工编号 此编号取几位就加入几个X。
        /// </summary>
        [Comment("规则字符串。包含前缀，后缀。")]
        [MaxLength(64)]
        public string RuleString { get; set; }

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
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }

    }
}
