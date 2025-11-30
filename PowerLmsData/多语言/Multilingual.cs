using Microsoft.EntityFrameworkCore;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 多语言资源表。
    /// </summary>
    public class Multilingual
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public Multilingual()
        {
        }

        /// <summary>
        /// 资源键。支持分层结构，使用点号分隔。
        /// 示例：
        /// - "Login.Title" (全局资源)
        /// - "China.Login.Title" (中国区资源)
        /// - "USA.Login.Title" (美国区资源)
        /// 建议第一级作为区域标识，后续级别为功能路径。
        /// </summary>
        [MaxLength(128)]
        [Comment("资源键。支持分层结构，如：Login.Title、China.Login.Title")]
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// 语言的标准缩写名。遵循 IETF BCP 47 标准（RFC 5646），仅包含 ASCII 字符。
        /// 常用格式：zh-CN、en-US、ja-JP 等（通常5字符）。
        /// </summary>
        [MaxLength(8)]
        [Unicode(false)]
        [Required]
        [Comment("语言的标准缩写名。遵循 IETF BCP 47 标准，如：zh-CN、en-US、ja-JP。")]
        public string LanguageTag { get; set; }

        /// <summary>
        /// 资源内容。
        /// </summary>
        [Comment("资源内容。")]
        public string Text { get; set; }
    }

    /// <summary>
    /// 语言字典。
    /// 语言标签遵循 IETF BCP 47 标准（RFC 5646）。
    /// 参见 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
    /// </summary>
    [Comment("语言字典，参见 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c。")]
    public class LanguageDataDic
    {
        /// <summary>
        /// 主键，也是语言的标准缩写名。遵循 IETF BCP 47 标准，仅包含 ASCII 字符。
        /// 常用格式：zh-CN、en-US、ja-JP 等。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [MaxLength(8)]
        [Unicode(false)]
        [Comment("主键，也是语言的标准缩写名。遵循 IETF BCP 47 标准。")]
        public string LanguageTag { get; set; }

        /// <summary>
        /// 语言Id（LCID - Locale Identifier）。
        /// </summary>
        [Comment("语言Id。")]
        public int Lcid { get; set; }

        /// <summary>
        /// 语言显示名称。
        /// </summary>
        [Comment("语言名。")]
        public string DisplayName { get; set; }

    }
}
