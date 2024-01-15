using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 权限类。
    /// </summary>
    [Comment("权限类。")]
    public class PlPermission : INamed
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlPermission()
        {

        }

        /// <summary>
        /// 显示名称。
        /// </summary>
        [Comment("简称")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 简称。
        /// </summary>
        [Comment("简称")]
        [MaxLength(32)]
        public string ShortName { get; set; }

        /// <summary>
        /// 正式名称，唯一Id。
        /// </summary>
        [Comment("正式名称，唯一Id")]
        [Key, MaxLength(64)]
        public string Name { get; set; }

        #region 导航属性

        /// <summary>
        /// 父节点。
        /// </summary>
        [JsonIgnore]
        public virtual PlPermission Parent { get; set; }

        /// <summary>
        /// 父节点Id。
        /// </summary>
        [MaxLength(64), ForeignKey(nameof(Parent))]
        [Comment("所属许可对象的Id。")]
        public string ParentId { get; set; }

        List<PlPermission> _Children;
        /// <summary>
        /// 拥有的子权限对象。
        /// </summary>
        public virtual List<PlPermission> Children { get => _Children ??= new List<PlPermission>(); set => _Children = value; }

        #endregion 导航属性
    }
}
