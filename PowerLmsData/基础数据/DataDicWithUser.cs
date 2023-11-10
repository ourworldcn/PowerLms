/*
 * 与人员相关的字典表
 */
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 简单数据字典条目类。
    /// </summary>
    public class SimpleDataDic : DataDicBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SimpleDataDic()
        {
            
        }

        /// <summary>
        /// 海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。
        /// </summary>
        [Comment("海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。")]
        public string CustomsCode { get; set; }

        /// <summary>
        /// 所属数据字典的的Id。
        /// </summary>
        [Comment("所属数据字典的的Id")]
        public virtual Guid? DataDicId { get; set; }
    }

    /// <summary>
    /// 数据字典目录类。
    /// </summary>
    public class DataDicCatalog : GuidKeyObjectBase
    {
        /// <summary>
        /// 编码。对本系统有一定意义的编码。
        /// </summary>
        [Comment("编码，对本系统有一定意义的编码")]
        [Column(TypeName = "varchar"), MaxLength(32)]   //最多32个ASCII字符
        public virtual string Code { get; set; }

        /// <summary>
        /// 显示的名称。
        /// </summary>
        [Comment("显示的名称")]
        public virtual string DisplayName { get; set; }

    }
}
