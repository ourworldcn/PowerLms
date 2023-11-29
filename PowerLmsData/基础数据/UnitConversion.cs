using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 单位换算。
    /// </summary>
    public class UnitConversion : GuidKeyObjectBase,IMarkDelete
    {
        /// <summary>
        /// 所属组织机构Id。
        /// </summary>
        [Comment("所属组织机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 基单位。
        /// </summary>
        [MaxLength(32)]
        [Comment("基单位")]
        public string Basic { get; set; }

        /// <summary>
        /// 宿单位。
        /// </summary>
        [MaxLength(32)]
        [Comment("宿单位")]
        public string Rim { get; set; }

        /// <summary>
        /// 换算率。
        /// </summary>
        [Comment("换算率")]
        public float Rate { get; set; }

        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }

    }
}
