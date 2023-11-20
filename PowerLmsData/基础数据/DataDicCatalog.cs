using Microsoft.EntityFrameworkCore;
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
    /// 专门针对数据字典的目录。
    /// </summary>
    [Comment("专门针对数据字典的目录。")]
    [Index(nameof(Code), IsUnique = true)]
    public class DataDicCatalog : GuidKeyObjectBase
    {
        /// <summary>
        /// 数据字典的代码。
        /// </summary>
        [MaxLength(32)]
        [Comment("数据字典的代码。")]
        public string Code { get; set; }

        /// <summary>
        /// 显示名称。
        /// </summary>
        [Comment("显示名称")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 数据字典的类型。1=简单字典，其它值随后逐步定义。
        /// </summary>
        [Comment("数据字典的类型。1=简单字典，其它值随后逐步定义。")]
        public int DataDicType { get; set; }

    }
}
