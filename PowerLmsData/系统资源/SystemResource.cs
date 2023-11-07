using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 系统资源。
    /// </summary>
    [Index(nameof(Name),IsUnique =true)]
    public class SystemResource : GuidKeyObjectBase
    {
        /// <summary>
        /// 系统资源名称。
        /// </summary>
        [MaxLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// 说明。
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 父资源的Id。可能分类用。
        /// </summary>
        public virtual Guid? ParentId  { get; set; }
    }
}
