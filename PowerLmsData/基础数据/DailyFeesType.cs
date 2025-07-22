using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 日常费用种类字典。用于OA费用申请单的费用分类，如差旅费、办公费、电话费等。
    /// 与主营业务费用种类（FeesType）分开管理。
    /// Code是费用代码，DisplayName是费用名称，ShortName是英文名称，Remark是附加说明。
    /// </summary>
    [Comment("日常费用种类字典")]
    public class DailyFeesType : NamedSpecialDataDicBase, IMarkDelete, ICloneable
    {
        /// <summary>
        /// 会计科目代码。用于财务核算和金蝶导入。
        /// </summary>
        [Comment("会计科目代码")]
        [Unicode(false), MaxLength(32)]
        public string SubjectCode { get; set; }
    }

    /// <summary>
    /// 日常费用种类字典扩展方法。
    /// </summary>
    public static class DailyFeesTypeExtensions
    {
        /// <summary>
        /// 获取费用种类的完整显示名称。
        /// </summary>
        /// <param name="dailyFeesType">日常费用种类</param>
        /// <returns>格式："代码-显示名称"</returns>
        public static string GetFullDisplayName(this DailyFeesType dailyFeesType)
        {
            if (string.IsNullOrEmpty(dailyFeesType.Code) || string.IsNullOrEmpty(dailyFeesType.DisplayName))
                return dailyFeesType.DisplayName ?? dailyFeesType.Code ?? string.Empty;

            return $"{dailyFeesType.Code}-{dailyFeesType.DisplayName}";
        }
    }
}