using Microsoft.EntityFrameworkCore;
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
    /// 地址类。
    /// </summary>
    public class PlAddress : GuidKeyObjectBase
    {
        /// <summary>
        /// 电话。
        /// </summary>
        [MaxLength(28)]
        [Comment("电话")]
        public string Tel { get; set; }

        /// <summary>
        /// 传真。
        /// </summary>
        [Comment("传真")]
        [MaxLength(28)]
        public string Fax { get; set; }

        /// <summary>
        /// 详细地址。
        /// </summary>
        [Comment("详细地址")]
        public string FullAddress { get; set; }
    }

    /// <summary>
    /// 嵌套在其他类中的地址类。
    /// </summary>
    [ComplexType]
    [Owned]
    public class PlSimpleOwnedAddress
    {
        /// <summary>
        /// 电话。
        /// </summary>
        [Comment("电话")]
        [MaxLength(28)]
        public string Tel { get; set; }

        /// <summary>
        /// 传真。
        /// </summary>
        [Comment("传真")]
        [MaxLength(28)]
        public string Fax { get; set; }

        /// <summary>
        /// 详细地址。
        /// </summary>
        [Comment("详细地址")]
        public string FullAddress { get; set; }
    }

    /// <summary>
    /// 嵌套在其他类中的完整的地址类。
    /// </summary>
    [ComplexType]
    [Owned]
    public class PlOwnedAddress
    {
        /// <summary>
        /// 国家编码Id。
        /// </summary>
        [Comment("国家编码Id")]
        public Guid? CountryId { get; set; }

        /// <summary>
        /// 省。
        /// </summary>
        [Comment("省")]
        [MaxLength(64)]
        public string Province { get; set; }

        /// <summary>
        /// 地市。
        /// </summary>
        [Comment("地市")]
        [MaxLength(64)]
        public string City { get; set; }

        /// <summary>
        /// 详细地址。
        /// </summary>
        [Comment("详细地址")]
        [MaxLength(64)]
        public string Address { get; set; }

        /// <summary>
        /// 邮政编码。
        /// </summary>
        [Comment("邮政编码")]
        [MaxLength(8)]
        public string ZipCode { get; set; }
    }

    /// <summary>
    /// 嵌套在其他类中的联系方式类。
    /// </summary>
    [ComplexType]
    [Owned]
    public class PlOwnedContact
    {
        /// <summary>
        /// 电话。
        /// </summary>
        [Comment("电话")]
        [MaxLength(32), Phone]
        public string Tel { get; set; }

        /// <summary>
        /// 传真。
        /// </summary>
        [Comment("传真")]
        [MaxLength(32), Phone]
        public string Fax { get; set; }

        /// <summary>
        /// 电子邮件。
        /// </summary>
        [Comment("电子邮件")]
        [MaxLength(128), EmailAddress]
        public string EMail { get; set; }

    }

    /// <summary>
    /// 对实体名称描述的自含复杂类。此类嵌入在不同实体中有不同的解释。
    /// </summary>
    [ComplexType]
    [Owned]
    public class PlOwnedName
    {
        /// <summary>
        /// 正式名称，拥有相对稳定性。
        /// </summary>
        [Comment("正式名称，拥有相对稳定性")]
        [MaxLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// 正式简称。对正式的组织机构通常简称也是规定的。
        /// </summary>
        [Comment("正式简称，对正式的组织机构通常简称也是规定的")]
        [MaxLength(32)]
        public string ShortName { get; set; }

        /// <summary>
        /// 显示名，有时它是昵称或简称(系统内)的意思。
        /// </summary>
        [Comment("显示名，有时它是昵称或简称(系统内)的意思")]
        public string DisplayName { get; set; }

    }

    /// <summary>
    /// 结算方式封装类。
    /// </summary>
    [ComplexType]
    [Owned]
    public class PlBillingInfo
    {
        /// <summary>
        /// 是否应收结算单位
        /// </summary>
        [Comment("是否应收结算单位")]
        public bool? IsExesGather { get; set; }

        /// <summary>
        /// 是否应付结算单位
        /// </summary>
        [Comment("是否应付结算单位")]
        public bool? IsExesPayer { get; set; }

        /// <summary>
        /// 信用期限天数
        /// </summary>
        [Comment("信用期限天数")]
        public int Dayslimited { get; set; }

        /// <summary>
        /// 拖欠限额币种Id
        /// </summary>
        [Comment("拖欠限额币种Id")]
        public Guid? CurrtypeId { get; set; }

        /// <summary>
        /// 拖欠金额。
        /// </summary>
        [Comment("拖欠金额")]
        public decimal AmountLimited { get; set; }

        /// <summary>
        /// 付费方式Id。
        /// </summary>
        [Comment("付费方式Id")]
        public Guid? AmountTypeId { get; set; }

        /// <summary>
        /// 是否超额黑名单
        /// </summary>
        [Comment("是否超额黑名单")]
        public bool IsCEBlack { get; set; }

        /// <summary>
        /// 是否超期黑名单
        /// </summary>
        [Comment("是否超期黑名单")]
        public bool IsBlack { get; set; }

        /// <summary>
        /// 是否特别注意
        /// </summary>
        [Comment("是否特别注意")]
        public bool IsNeedTrace { get; set; }

    }
}
