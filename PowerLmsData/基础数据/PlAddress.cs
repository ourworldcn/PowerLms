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
        public string Name { get; set; }

        /// <summary>
        /// 正式简称。对正式的组织机构通常简称也是规定的。
        /// </summary>
        [Comment("正式简称，对正式的组织机构通常简称也是规定的")]
        [MaxLength (32)]
        public string ShortName { get; set; }

        /// <summary>
        /// 显示名，有时它是昵称或简称(系统内)的意思。
        /// </summary>
        [Comment("显示名，有时它是昵称或简称(系统内)的意思")]
        public string DisplayName { get; set; }

    }
}
