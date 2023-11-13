using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 商户及组织机构。
    /// </summary>
    public class PlOrganization : GuidKeyObjectBase
    {
        /// <summary>
        /// 组织机构名称。这个有一定的正式的意义。
        /// </summary>
        [Comment("组织机构名称")]
        public string Name { get; set; }

        /// <summary>
        /// 组织机构描述。
        /// </summary>
        [Comment("组织机构描述")]
        public string Description { get; set; }

        /// <summary>
        /// 机构编码。
        /// </summary>
        [Comment("机构编码")]
        public string ShortcutName { get; set; }

        /// <summary>
        /// 机构类型。1商户，2公司，4下属机构
        /// </summary>
        [Comment("机构类型，1商户，2公司，4下属机构")]
        public int Otc { get; set; }

        /// <summary>
        /// 机构地址。
        /// </summary>
        public  PlComplexAddress Address { get; set; }

        #region 导航属性

        private PlOrganization _Parent;
        /// <summary>
        /// 所属组织机构的导航属性。没有父的组织机构是顶层节点即"商户"。
        /// </summary>
        [JsonIgnore]
        public virtual PlOrganization Parent { get => _Parent; set => _Parent = value; }

        /// <summary>
        /// 所属组织机构Id。没有父的组织机构是顶层节点即"商户"
        /// </summary>
        [ForeignKey(nameof(Parent))]
        [Comment("所属组织机构Id。没有父的组织机构是顶层节点即\"商户\"。")]
        public virtual Guid? ParentId { get; set; }

        List<PlOrganization> _Children;
        /// <summary>
        /// 拥有的子组织机构。
        /// </summary>
        public virtual List<PlOrganization> Children { get => _Children ??= new List<PlOrganization>(); set => _Children = value; }

        #endregion 导航属性
    }
}
