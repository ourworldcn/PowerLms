/*
 * 项目:PowerLms | 模块:海运出口
 * 功能:海运出口分提单(货代提单)实体
 * 技术要点:EF Core实体,字段大多为字符串类型且可为空
 * 作者:zc | 创建:2026-02 | 修改:2026-02-23 初始创建
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 海运出口分提单(货代提单)。
    /// </summary>
    [Comment("海运出口分提单")]
    [Index(nameof(JobId), IsUnique = false)]
    public class EsHbl : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public EsHbl()
        {
        }
        /// <summary>
        /// 所属海运出口工作号。
        /// </summary>
        [Comment("所属海运出口工作号")]
        public Guid? JobId { get; set; }
        /// <summary>
        /// 提单状态。
        /// </summary>
        [Comment("提单状态")]
        [MaxLength(20)]
        public string BillStatus { get; set; }
        /// <summary>
        /// 货代提单编号。
        /// </summary>
        [Comment("货代提单编号")]
        public string HBLNo { get; set; }
        /// <summary>
        /// 付款方式。
        /// </summary>
        [Comment("付款方式")]
        [MaxLength(32)]
        public string SeaPayMode { get; set; }
        /// <summary>
        /// 付款地点。
        /// </summary>
        [Comment("付款地点")]
        [MaxLength(50)]
        public string SeaPayPlace { get; set; }
        /// <summary>
        /// 运输条款。
        /// </summary>
        [Comment("运输条款")]
        [MaxLength(32)]
        public string SeaTerms { get; set; }
        /// <summary>
        /// 前程运输。
        /// </summary>
        [Comment("前程运输")]
        [MaxLength(32)]
        public string PreCarriage { get; set; }
        /// <summary>
        /// 收货地。
        /// </summary>
        [Comment("收货地")]
        [MaxLength(50)]
        public string Receipt { get; set; }
        /// <summary>
        /// 装船港。
        /// </summary>
        [Comment("装船港")]
        [MaxLength(50)]
        public string Loading { get; set; }
        /// <summary>
        /// 装船港描述。
        /// </summary>
        [Comment("装船港描述")]
        [MaxLength(100)]
        public string LoadingDesc { get; set; }
        /// <summary>
        /// 中转港。
        /// </summary>
        [Comment("中转港")]
        [MaxLength(50)]
        public string Discharge { get; set; }
        /// <summary>
        /// 中转港描述。
        /// </summary>
        [Comment("中转港描述")]
        [MaxLength(100)]
        public string DischargeDesc { get; set; }
        /// <summary>
        /// 目的港。
        /// </summary>
        [Comment("目的港")]
        [MaxLength(50)]
        public string OceanDestination { get; set; }
        /// <summary>
        /// 目的港描述。
        /// </summary>
        [Comment("目的港描述")]
        [MaxLength(100)]
        public string OceanDestinationDesc { get; set; }
        /// <summary>
        /// 船公司。
        /// </summary>
        [Comment("船公司")]
        [MaxLength(250)]
        public string ShipOwner { get; set; }
        /// <summary>
        /// 船名。
        /// </summary>
        [Comment("船名")]
        [MaxLength(50)]
        public string Vessel { get; set; }
        /// <summary>
        /// 航次。
        /// </summary>
        [Comment("航次")]
        [MaxLength(32)]
        public string Voyage { get; set; }
        /// <summary>
        /// 费用描述。
        /// </summary>
        [Comment("费用描述")]
        public string HblFreight { get; set; }
        /// <summary>
        /// 装船日期。
        /// </summary>
        [Comment("装船日期")]
        [Precision(3)]
        public DateTime? SaillingDate { get; set; }
        /// <summary>
        /// 开船日期。
        /// </summary>
        [Comment("开船日期")]
        [Precision(3)]
        public DateTime? ETD { get; set; }
        /// <summary>
        /// 到港日期。
        /// </summary>
        [Comment("到港日期")]
        [Precision(3)]
        public DateTime? ETA { get; set; }
        /// <summary>
        /// 预付运费。
        /// </summary>
        [Comment("预付运费")]
        [MaxLength(50)]
        public string PPD_AMT { get; set; }
        /// <summary>
        /// 到付运费。
        /// </summary>
        [Comment("到付运费")]
        [MaxLength(50)]
        public string CCT_AMT { get; set; }
        /// <summary>
        /// 发货人名称。
        /// </summary>
        [Comment("发货人名称")]
        [MaxLength(100)]
        public string Consignor { get; set; }
        /// <summary>
        /// 发货人抬头。
        /// </summary>
        [Comment("发货人抬头")]
        public string ConsignorHead { get; set; }
        /// <summary>
        /// 收货人名称。
        /// </summary>
        [Comment("收货人名称")]
        [MaxLength(100)]
        public string Consignee { get; set; }
        /// <summary>
        /// 收货人抬头。
        /// </summary>
        [Comment("收货人抬头")]
        public string ConsigneeHead { get; set; }
        /// <summary>
        /// 通知人名称。
        /// </summary>
        [Comment("通知人名称")]
        [MaxLength(100)]
        public string Notify { get; set; }
        /// <summary>
        /// 通知人抬头。
        /// </summary>
        [Comment("通知人抬头")]
        public string NotifyHead { get; set; }
        /// <summary>
        /// 第二通知人抬头。
        /// </summary>
        [Comment("第二通知人抬头")]
        public string AlsoNotify { get; set; }
        /// <summary>
        /// 箱号。
        /// </summary>
        [Comment("箱号")]
        public string ContainerNo { get; set; }
        /// <summary>
        /// 箱量。
        /// </summary>
        [Comment("箱量")]
        public string ContainerNum { get; set; }
        /// <summary>
        /// 品名。
        /// </summary>
        [Comment("品名")]
        public string GoodsName { get; set; }
        /// <summary>
        /// 唛头。
        /// </summary>
        [Comment("唛头")]
        public string Marks { get; set; }
        /// <summary>
        /// 件数。
        /// </summary>
        [Comment("件数")]
        public int? PkgsNum { get; set; }
        /// <summary>
        /// 包装类型。
        /// </summary>
        [Comment("包装类型")]
        [MaxLength(32)]
        public string PkgsType { get; set; }
        /// <summary>
        /// 毛重(KGS)。
        /// </summary>
        [Comment("毛重(KGS)")]
        [Precision(18, 3)]
        public decimal? Weight { get; set; }
        /// <summary>
        /// 体积(CBM)。
        /// </summary>
        [Comment("体积(CBM)")]
        [Precision(18, 3)]
        public decimal? MeasureMent { get; set; }
        /// <summary>
        /// 总计1-箱量的英文合计。
        /// </summary>
        [Comment("总计1-箱量的英文合计")]
        [MaxLength(250)]
        public string BookingTotal { get; set; }
        /// <summary>
        /// 总计2-件数的英文合计。
        /// </summary>
        [Comment("总计2-件数的英文合计")]
        [MaxLength(250)]
        public string TotalGoods { get; set; }
        /// <summary>
        /// 正本张数。
        /// </summary>
        [Comment("正本张数")]
        public int? HBLIssueNumber { get; set; }
        /// <summary>
        /// 签发人。
        /// </summary>
        [Comment("签发人")]
        [MaxLength(32)]
        public string HBLIssueMan { get; set; }
        /// <summary>
        /// 签发时间。
        /// </summary>
        [Comment("签发时间")]
        [Precision(3)]
        public DateTime? HBLIssueDate { get; set; }
        /// <summary>
        /// 签发地点。
        /// </summary>
        [Comment("签发地点")]
        [MaxLength(32)]
        public string HBLIssuePlace { get; set; }
        /// <summary>
        /// 放货方式。
        /// </summary>
        [Comment("放货方式")]
        [MaxLength(32)]
        public string MblGoodsMode { get; set; }
        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }
        /// <summary>
        /// 提单附页。
        /// </summary>
        [Comment("提单附页")]
        public string HBLRemark { get; set; }
    }
}
