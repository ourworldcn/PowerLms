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
    /// 机构实体，包括下属机构，公司。
    /// </summary>
    /// <remarks> 通过 <see cref="Otc"/> 字段来区分机构类型。
    /// 下属机构指：某个公司类型的机构的子孙机构，但排除其他公司（公司子孙中可能有子公司）和其下属机构。</remarks>
    [Comment("机构实体，包括下属机构，公司。")]
    public class PlOrganization : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 商户Id。仅总公司(ParentId 是null)需要此字段指向所属商户，其它情况忽略此字段。关联到 <see cref="PlMerchant"/> 实体。
        /// </summary>
        [Comment("商户Id。仅总公司(ParentId 是null)需要此字段指向所属商户，其它情况忽略此字段。")]
        public Guid? MerchantId { get; set; }

        /// <summary>
        /// 正式名称，拥有相对稳定性。
        /// </summary>
        [Comment("正式名称，拥有相对稳定性")]
        [MaxLength(64)]
        public string Name_Name { get; set; }

        /// <summary>
        /// 正式简称。对正式的组织机构通常简称也是规定的。
        /// </summary>
        [Comment("正式简称，对正式的组织机构通常简称也是规定的")]
        [MaxLength(32)]
        public string Name_ShortName { get; set; }

        /// <summary>
        /// 显示名，有时它是昵称或简称(系统内)的意思。
        /// </summary>
        [Comment("显示名，有时它是昵称或简称(系统内)的意思")]
        public string Name_DisplayName { get; set; }

        /// <summary>
        /// 组织机构描述。
        /// </summary>
        [Comment("组织机构描述")]
        public string Description { get; set; }

        /// <summary>
        /// 快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。
        /// </summary>
        [Unicode(false), MaxLength(8)]
        [Comment("快捷输入码。服务器不使用。")]
        public string ShortcutCode { get; set; }

        /// <summary>
        /// 机构类型。2公司；4普通机构，此时祖先机构中必有公司类型的机构。
        /// </summary>
        [Comment("机构类型，2公司；4机构，此时祖先机构中必有公司类型的机构。")]
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

        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        [Comment("创建的时间")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 备注.
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        #region 补充字段

        /// <summary>
        /// 法人代表。
        /// </summary>
        [Comment("法人代表")]
        [MaxLength(64)]
        public string LegalRepresentative { get; set; }

        /// <summary>
        /// 本位币Id。
        /// </summary>
        [Comment("本位币Id")]
        public Guid? BaseCurrencyId { get; set; }

        /// <summary>
        /// 本位币编码。
        /// </summary>
        [Comment("本位币编码")]
        [Unicode(false), MaxLength(4), Required]
        public string BaseCurrencyCode { get; set; } = "CNY";

        /// <summary>
        /// 海关编码。
        /// </summary>
        [Comment("海关编码。")]
        [MaxLength(32)]
        public string CustomCode { get; set; }

        /// <summary>
        /// 工商登记号码（信用证号）。这应该是两个东西，暂定这么写。
        /// </summary>
        [Comment("工商登记号码（信用证号）。")]
        [MaxLength(64)]
        public string UnknowNumber { get; set; }
        #endregion 补充字段

        #region 导航属性

        private PlOrganization _Parent;
        /// <summary>
        /// 所属组织机构的导航属性。没有父的组织机构是顶层节点即"商户"。
        /// </summary>
        [JsonIgnore]
        public virtual PlOrganization Parent { get => _Parent; set => _Parent = value; }

        /// <summary>
        /// 所属组织机构Id。没有父组织机构是顶层节点总公司，它的父是商户(MerchantId)
        /// </summary>
        [ForeignKey(nameof(Parent))]
        [Comment("所属组织机构Id。没有父组织机构是顶层节点总公司，它的父是商户(MerchantId)")]
        public virtual Guid? ParentId { get; set; }

        List<PlOrganization> _Children;
        /// <summary>
        /// 拥有的子组织机构。
        /// </summary>
        public virtual List<PlOrganization> Children { get => _Children ??= new List<PlOrganization>(); set => _Children = value; }

        #endregion 导航属性

        /// <summary>
        /// 行版本号。用于开放式并发控制，防止并发更新时的数据覆盖问题。
        /// EF Core会在更新时自动检查此字段，如果值不匹配则抛出DbUpdateConcurrencyException。
        /// SQL Server自动维护此字段，每次更新时自动递增。
        /// </summary>
        [Timestamp]
        [Comment("行版本号，用于开放式并发控制")]
        public byte[] RowVersion { get; set; }
    }

    /// <summary>
    /// 机构开户行信息。
    /// </summary>
    public class BankInfo : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属实体Id。关联到 <see cref="PlOrganization"/> 实体。
        /// </summary>
        [Comment("所属实体Id。")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 开户名。
        /// </summary>
        [MaxLength(32)]
        [Comment("开户名。")]
        public string Name { get; set; }

        /// <summary>
        /// 开户行。
        /// </summary>
        [MaxLength(32)]
        [Comment("开户行。")]
        public string Bank { get; set; }

        /// <summary>
        /// 账号。
        /// </summary>
        [MaxLength(32)]
        [Comment("账号。")]
        public string Account { get; set; }

        /// <summary>
        /// 币种Id。
        /// </summary>
        [Comment("币种Id。")]
        public Guid CurrencyId { get; set; }

        /// <summary>
        /// A账财务科目代码
        /// </summary>
        [MaxLength(32), Unicode(false)]
        [Comment("A账财务科目代码")]
        public string AAccountSubjectCode { get; set; }

        /// <summary>
        /// B账财务科目代码
        /// </summary>
        [MaxLength(32), Unicode(false)]
        [Comment("B账财务科目代码")]
        public string BAccountSubjectCode { get; set; }

        /// <summary>
        /// 凭证字。专用于OA日常费用申请单的凭证号生成，如：银、现、转、记等。
        /// </summary>
        [MaxLength(4)]
        [Comment("凭证字。专用于OA日常费用申请单的凭证号生成，如：银、现、转、记等。")]
        public string VoucherCharacter { get; set; }
    }
}
