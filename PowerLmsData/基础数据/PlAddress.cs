using Microsoft.EntityFrameworkCore;
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
    public class PlComplexAddress
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
}
