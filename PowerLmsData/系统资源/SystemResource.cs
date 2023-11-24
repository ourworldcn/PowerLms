using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OW.Data;
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
    /// 系统资源。
    /// </summary>
    [Index(nameof(Name), IsUnique = true)]
    public class SystemResource : GuidKeyObjectBase
    {
        /// <summary>
        /// 系统资源名称。包含各种数据字典。
        /// </summary>
        [Comment("编码，对本系统有一定意义的编码")]
        [MaxLength(32)]   //最多32个ASCII字符
        public string Name { get; set; }

        /// <summary>
        /// 显示的名称。
        /// </summary>
        [Comment("显示的名称")]
        public virtual string DisplayName { get; set; }

        /// <summary>
        /// 说明。
        /// </summary>
        [Comment("说明")]
        public string Remark { get; set; }

        /// <summary>
        /// 父资源的Id。可能分类用。
        /// </summary>
        [Comment("父资源的Id。可能分类用")]
        public virtual Guid? ParentId { get; set; }
    }
}
