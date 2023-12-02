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
    /// 单位换算。
    /// </summary>
    public class UnitConversion : SpecialDataDicBase, IMarkDelete
    {
        /// <summary>
        /// 基单位。
        /// </summary>
        [MaxLength(32)]
        [Comment("基单位")]
        public string Basic { get; set; }

        /// <summary>
        /// 宿单位。
        /// </summary>
        [MaxLength(32)]
        [Comment("宿单位")]
        public string Rim { get; set; }

        /// <summary>
        /// 换算率。
        /// </summary>
        [Comment("换算率")]
        public float Rate { get; set; }

    }
}
