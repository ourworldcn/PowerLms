using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 财务科目设置表
    /// </summary>
    [Comment("财务科目设置表")]
    [Index(nameof(OrgId), nameof(Code), IsUnique = true)]
    public class SubjectConfiguration : GuidKeyObjectBase, ISpecificOrg, IMarkDelete, ICreatorInfo
    {
        /// <summary>
        /// 所属组织机构Id
        /// </summary>
        [Comment("所属组织机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 服务器用此编码来标识该数据用于什么地方。只能是以下几种之一（可能持续增加）：
        /// 
        /// </summary>
        [Comment("科目编码")]
        [MaxLength(32), Unicode(false)]
        [Required(AllowEmptyStrings = false)]
        public string Code { get; set; }

        /// <summary>
        /// 科目号（会计科目编号）
        /// </summary>
        [Comment("科目号（会计科目编号）")]
        [MaxLength(32), Unicode(false)]
        [Required(AllowEmptyStrings = false)]
        public string SubjectNumber { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [Comment("显示名称")]
        [MaxLength(128)]
        [Required(AllowEmptyStrings = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        #region IMarkDelete
        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }
        #endregion

        #region ICreatorInfo
        /// <summary>
        /// 创建者的唯一标识
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间
        /// </summary>
        [Comment("创建的时间")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;
        #endregion
    }
}