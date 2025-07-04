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
    /// 角色类。
    /// </summary>
    [Comment("角色类。")]
    public class PlRole : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlRole()
        {

        }

        /// <summary>
        /// 所属组织机构Id或商户Id。关联 PlOrganization 或 PlMerchant。
        /// 当角色归属于机构时，此字段存储机构ID；
        /// 当角色直接归属于商户时，此字段存储商户ID。
        /// </summary>
        [Comment("所属组织机构Id或商户Id。")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 封装名称的对象。
        /// </summary>
        public PlOwnedName Name { get; set; }

        #region ICreatorInfo接口相关

        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        [Comment("创建的时间")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        #endregion ICreatorInfo接口相关
    }

    /// <summary>
    /// 记录账号与角色关系的类。
    /// </summary>
    [Comment("记录账号与角色关系的类。")]
    [Index(nameof(RoleId), nameof(UserId), IsUnique = true)]
    public class AccountRole : ICreatorInfo
    {
        /// <summary>
        /// 账号Id。
        /// </summary>
        [Comment("账号Id。")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 角色Id。
        /// </summary>
        [Comment("角色Id。")]
        public Guid RoleId { get; set; }

        #region ICreatorInfo接口相关

        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        [Comment("创建的时间")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        #endregion ICreatorInfo接口相关
    }

    /// <summary>
    /// 记录角色和权限的关系类。
    /// </summary>
    [Comment("记录角色和权限的关系类。")]
    [Index(nameof(PermissionId), nameof(RoleId), IsUnique = true)]
    public class RolePermission : ICreatorInfo
    {
        /// <summary>
        /// 角色Id。
        /// </summary>
        [Comment("角色Id。")]
        public Guid RoleId { get; set; }

        /// <summary>
        /// 权限Id。
        /// </summary>
        [Comment("权限Id。")]
        [MaxLength(64)]
        public string PermissionId { get; set; }

        #region ICreatorInfo接口相关

        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        [Comment("创建的时间")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        #endregion ICreatorInfo接口相关
    }
}
