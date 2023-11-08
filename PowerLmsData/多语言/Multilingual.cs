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
    [Index(nameof(LanguageTag), nameof(Key), IsUnique = true)]
    public class Multilingual
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public Multilingual()
        {
        }

        /// <summary>
        /// 主键。
        /// </summary>
        [Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Comment("主键。")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 主键，也是语言的标准缩写名。
        /// </summary>
        [MaxLength(12)]
        [Column(TypeName = "varchar")]
        [Required]
        [Comment("主键，也是语言的标准缩写名。")]
        public string LanguageTag { get; set; }

        /// <summary>
        /// 键值字符串。如:未登录.登录.标题。
        /// </summary>
        [MaxLength(64)]
        [Comment("键值字符串。如:未登录.登录.标题。")]
        public string Key { get; set; }

        /// <summary>
        /// 内容。
        /// </summary>
        [Comment("内容。")]
        public string Text { get; set; }
    }

    /// <summary>
    /// 语言字典。
    /// 参见 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c，可能需要翻译。
    /// </summary>
    [Comment("语言字典，参见 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c。")]
    public class LanguageDataDic
    {
        /// <summary>
        /// 主键，也是语言的标准缩写名。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [MaxLength(12)]
        [Column(TypeName = "varchar")]
        [Comment("主键，也是语言的标准缩写名。")]
        public string LanguageTag { get; set; }

        /// <summary>
        /// 语言Id。
        /// </summary>
        [Comment("语言Id。")]
        public int Lcid { get; set; }

        /// <summary>
        /// 语言名。
        /// </summary>
        [Comment("语言名。")]
        public string DisplayName { get; set; }

    }
}
