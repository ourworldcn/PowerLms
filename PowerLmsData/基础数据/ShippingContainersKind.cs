using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 集装箱类型。
    /// Code 是代码，DisplayName是标题，Remark是描述
    /// </summary>
    [Comment("集装箱类型")]
    public class ShippingContainersKind : NamedSpecialDataDicBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ShippingContainersKind()
        {

        }

        /// <summary>
        /// 箱型。
        /// </summary>
        [Comment("箱型")]
        [MaxLength(64)]
        public int Kind { get; set; }

        /// <summary>
        /// 尺寸。
        /// </summary>
        [Comment("尺寸")]
        [MaxLength(64)]
        public string Size { get; set; }

        /// <summary>
        /// TEU。
        /// </summary>
        [Comment("TEU")]
        public byte Teu { get; set; }

        /// <summary>
        /// 皮重。单位Kg。
        /// </summary>
        [Comment("皮重。单位Kg。")]
        [Precision(18, 4)]
        public decimal Kgs { get; set; }
    }
}
