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
    /// 组织机构。
    /// </summary>
    public class PlOrganization : GuidKeyObjectBase
    {
        /// <summary>
        /// 商户Id。仅总公司(ParentId 是null)需要此字段指向所属商户，其它情况忽略此字段。
        /// </summary>
        [Comment("商户Id。仅总公司(ParentId 是null)需要此字段指向所属商户，其它情况忽略此字段。")]
        public Guid? MerchantId { get; set; }

        /// <summary>
        /// 名称类。
        /// </summary>
        [Comment("名称嵌入类")]
        public PlOwnedName Name { get; set; }

        /// <summary>
        /// 组织机构描述。
        /// </summary>
        [Comment("组织机构描述")]
        public string Description { get; set; }

        /// <summary>
        /// 快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。
        /// </summary>
        [Column(TypeName = "char"), MaxLength(8)]
        [Comment("快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。")]
        public string ShortcutCode { get; set; }

        /// <summary>
        /// 机构类型。2公司，4下属机构。
        /// </summary>
        [Comment("机构类型，2公司，4下属机构")]
        public int Otc { get; set; }

        /// <summary>
        /// 机构地址。
        /// </summary>
        public PlSimpleOwnedAddress Address { get; set; }

        /// <summary>
        /// 联系人名字。
        /// </summary>
        [Comment("联系人名字")]
        public string ContractName { get; set; }

        #region 导航属性

        private PlOrganization _Parent;
        /// <summary>
        /// 所属组织机构的导航属性。没有父的组织机构是顶层节点即"商户"。
        /// </summary>
        [JsonIgnore]
        public virtual PlOrganization Parent { get => _Parent; set => _Parent = value; }

        /// <summary>
        /// 所属组织机构Id。
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
