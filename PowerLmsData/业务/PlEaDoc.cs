using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
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
    /// 空运出口单。
    /// </summary>
    [Comment("空运出口单")]
    [Index(nameof(JobId))]
    public class PlEaDoc : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 所属业务Id。
        /// </summary>
        [Comment("所属业务Id")]
        public Guid? JobId { get; set; }

        /// <summary>
        /// 单号.
        /// </summary>
        [Comment("单号")]
        public string DocNo { get; set; }

        /// <summary>
        /// 业务编号。
        /// </summary>
        [Comment("业务编号")]
        public string JobNo { get; set; }

        /// <summary>
        /// 制单人，建立时系统默认，可以更改相当于工作号的所有者。
        /// </summary>
        [Comment("操作员，可以更改相当于工作号的所有者")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 制单时间,系统默认，不能更改
        /// </summary>
        [Comment("新建时间,系统默认，不能更改。")]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 中转港1港口Code,显示三字码即可。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("中转港1港口Code,显示三字码即可。")]
        public string To1Code { get; set; }

        /// <summary>
        /// 中转港航班1
        /// </summary>
        [Comment("中转港航班1。")]
        public string By1 { get; set; }

        /// <summary>
        /// 中转港2港口Code,显示三字码即可。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("中转港2港口Code,显示三字码即可。")]
        public string To2Code { get; set; }

        /// <summary>
        /// 中转港航班2
        /// </summary>
        [Comment("中转港航班2。")]
        public string By2 { get; set; }

        /// <summary>
        /// 中转港3港口Code,显示三字码即可。
        /// </summary>
        [MaxLength(4), Unicode(false)]
        [Comment("中转港3港口Code,显示三字码即可。")]
        public string To3Code { get; set; }

        /// <summary>
        /// 中转港航班3
        /// </summary>
        [Comment("中转港航班3。")]
        public string By3 { get; set; }

        /// <summary>
        /// 主件单数。
        /// </summary>
        [Comment("主件单数。")]
        public int M_PkgsCount { get; set; }

        /// <summary>
        /// 主单重量，3位小数。
        /// </summary>
        [Comment("主单重量，3位小数")]
        [Precision(18, 3)]
        public decimal M_weight { get; set; }

        /// <summary>
        /// 主单计费重量，3位小数。
        /// </summary>
        [Comment("主单计费重量，3位小数")]
        [Precision(18, 3)]
        public decimal M_Netweigh { get; set; }

        /// <summary>
        /// 包装件单数。结算件数。
        /// </summary>
        [Comment("包装件单数。结算件数。")]
        public int C_PkgsCount { get; set; }

        /// <summary>
        /// 结算重量，3位小数。
        /// </summary>
        [Comment("结算重量，3位小数")]
        [Precision(18, 3)]
        public decimal C_Weight { get; set; }

        /// <summary>
        /// 结算计费重量，3位小数。
        /// </summary>
        [Comment("结算计费重量，3位小数")]
        [Precision(18, 3)]
        public decimal C_Netweigh { get; set; }

        /// <summary>
        /// 结算体积，3位小数。
        /// </summary>
        [Comment("结算体积，3位小数")]
        [Precision(18, 3)]
        public decimal C_MeasureMent { get; set; }

        /// <summary>
        /// 操作地Id,简单字典goodsstation
        /// </summary>
        [Comment("操作地Id,简单字典goodsstation")]
        public Guid? GoodsStationId { get; set; }

        /// <summary>
        /// 外包装状态,简单字典packState
        /// </summary>
        [Comment("外包装状态,简单字典packState")]
        public Guid? PackStateId { get; set; }
    }

    /// <summary>
    /// 货场出重子表。
    /// </summary>
    [Index(nameof(EaDocId))]
    public class HuochangChuchong : GuidKeyObjectBase
    {
        /// <summary>
        /// 运单Id。
        /// </summary>
        [Comment("运单Id。")]
        public Guid? EaDocId { get; set; }

        /// <summary>
        /// 分单号。
        /// </summary>
        [Comment("分单号。")]
        public string HblNo { get; set; }

        /// <summary>
        /// 件数。
        /// </summary>
        [Comment("件数。")]
        public int PkgsCount { get; set; }

        /// <summary>
        /// 重量，3位小数。
        /// </summary>
        [Comment("结算计费重量，3位小数")]
        [Precision(18, 3)]
        public decimal Weight { get; set; }

        /// <summary>
        /// 重量，3位小数。
        /// </summary>
        [Comment("体积，3位小数")]
        [Precision(18, 3)]
        public decimal MeasureMent { get; set; }

        /// <summary>
        /// 尺寸。
        /// </summary>
        [Comment("体积，字符串")]
        [MaxLength(64)]
        public string CargoSize { get; set; }
    }

}
