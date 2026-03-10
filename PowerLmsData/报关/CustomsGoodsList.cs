/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关单货物明细子表实体（对应海关EDI报文字段）
 * 技术要点：EF Core实体，外键关联报关单主表，日期均可为空
 * 作者：zc | 创建：2026-02
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 报关单货物明细子表。
    /// </summary>
    [Comment("报关单货物明细子表")]
    [Index(nameof(ParentId), IsUnique = false)]
    public class CustomsGoodsList : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomsGoodsList()
        {
        }
        /// <summary>
        /// 所属报关单主表Id。
        /// </summary>
        [Comment("所属报关单主表Id")]
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 归类标志。空值。
        /// </summary>
        [Comment("归类标志，空值")]
        [MaxLength(1)]
        public string ClassMark { get; set; }
        /// <summary>
        /// 商品编号（HS编码）。从HSCODE读取。
        /// </summary>
        [Comment("商品编号（HS编码）")]
        [MaxLength(10)]
        public string CodeTS { get; set; }
        /// <summary>
        /// 备案序号。程序控制9位。
        /// </summary>
        [Comment("备案序号，程序控制9位")]
        public long? ContrItem { get; set; }
        /// <summary>
        /// 申报单价。海关精度z(15).z(4)。
        /// </summary>
        [Comment("申报单价")]
        [Precision(19, 5)]
        public decimal? DeclPrice { get; set; }
        /// <summary>
        /// 申报总价。海关精度z(17).z(2)。
        /// </summary>
        [Comment("申报总价")]
        [Precision(19, 5)]
        public decimal? DeclTotal { get; set; }
        /// <summary>
        /// 征减免税方式。简单字典TradeMode海关编码[0~9]。
        /// </summary>
        [Comment("征减免税方式，简单字典TradeMode海关编码")]
        [MaxLength(1)]
        public string DutyMode { get; set; }
        /// <summary>
        /// 货号。
        /// </summary>
        [Comment("货号")]
        [MaxLength(30)]
        public string ExgNo { get; set; }
        /// <summary>
        /// 版本号。
        /// </summary>
        [Comment("版本号")]
        public int? ExgVersion { get; set; }
        /// <summary>
        /// 申报计量单位与法定单位比例因子。
        /// </summary>
        [Comment("申报计量单位与法定单位比例因子")]
        [Precision(11, 3)]
        public decimal? Factor { get; set; }
        /// <summary>
        /// 第一计量单位（法定单位）。简单字典UNIT海关编码。
        /// </summary>
        [Comment("第一计量单位（法定单位），简单字典UNIT海关编码")]
        [MaxLength(3)]
        public string FirstUnit { get; set; }
        /// <summary>
        /// 第一法定数量。
        /// </summary>
        [Comment("第一法定数量")]
        [Precision(19, 5)]
        public decimal? FirstQty { get; set; }
        /// <summary>
        /// 成交计量单位。简单字典UNIT海关编码。
        /// </summary>
        [Comment("成交计量单位，简单字典UNIT海关编码")]
        [MaxLength(3)]
        public string GUnit { get; set; }
        /// <summary>
        /// 商品规格、型号。申报要素。大文本，海关精度50。
        /// </summary>
        [Comment("商品规格、型号，申报要素")]
        public string GModel { get; set; }
        /// <summary>
        /// 商品名称。海关精度50。
        /// </summary>
        [Comment("商品名称")]
        [MaxLength(255)]
        public string GName { get; set; }
        /// <summary>
        /// 商品序号。海关精度9。
        /// </summary>
        [Comment("商品序号")]
        public long? GNo { get; set; }
        /// <summary>
        /// 成交数量。
        /// </summary>
        [Comment("成交数量")]
        [Precision(19, 5)]
        public decimal? GQty { get; set; }
        /// <summary>
        /// 原产国。国家字典3位code。
        /// 出口报关单：originalCountry填目的国；进口报关单：originalCountry填原产国。
        /// </summary>
        [Comment("原产国，国家字典3位code")]
        [MaxLength(3)]
        public string OriginCountry { get; set; }
        /// <summary>
        /// 第二计量单位。简单字典UNIT海关编码。
        /// </summary>
        [Comment("第二计量单位，简单字典UNIT海关编码")]
        [MaxLength(3)]
        public string SecondUnit { get; set; }
        /// <summary>
        /// 第二法定数量。
        /// </summary>
        [Comment("第二法定数量")]
        [Precision(19, 5)]
        public decimal? SecondQty { get; set; }
        /// <summary>
        /// 成交币制。
        /// </summary>
        [Comment("成交币制")]
        [MaxLength(3)]
        public string TradeCurr { get; set; }
        /// <summary>
        /// 报关用途/生产厂家。简单字典Useage的海关代码。
        /// </summary>
        [Comment("报关用途/生产厂家，简单字典Useage海关代码")]
        [MaxLength(2)]
        public string UseTo { get; set; }
        /// <summary>
        /// 工缴费。海关精度z(17).z(2)。
        /// </summary>
        [Comment("工缴费")]
        [Precision(19, 4)]
        public decimal? WorkUsd { get; set; }
        /// <summary>
        /// 最终目的国（地区）。国家字典3位code。
        /// 出口报关单：destinationCountry填原产国；进口报关单：destinationCountry填目的国。
        /// </summary>
        [Comment("最终目的国（地区），国家字典3位code")]
        [MaxLength(3)]
        public string DestinationCountry { get; set; }
        /// <summary>
        /// 检验检疫编码。3位检验检疫编码。
        /// </summary>
        [Comment("检验检疫编码，3位检验检疫编码")]
        [MaxLength(20)]
        public string CiqCode { get; set; }
        /// <summary>
        /// 商品英文名称（申报货物名称外文）。
        /// </summary>
        [Comment("商品英文名称")]
        [MaxLength(100)]
        public string DeclGoodsEname { get; set; }
        /// <summary>
        /// 报检原产国。国家3位代码。
        /// </summary>
        [Comment("报检原产国，国家3位代码")]
        [MaxLength(50)]
        public string OrigPlaceCode { get; set; }
        /// <summary>
        /// 报检用途。简单字典Useage的海关代码。
        /// </summary>
        [Comment("报检用途，简单字典Useage海关代码")]
        [MaxLength(4)]
        public string Purpose { get; set; }
        /// <summary>
        /// 产品有效期。质量保障的截止日期。
        /// </summary>
        [Comment("产品有效期")]
        [Precision(3)]
        public DateTime? ProdValidDt { get; set; }
        /// <summary>
        /// 产品保质期。质量保障的月数或天数。
        /// </summary>
        [Comment("产品保质期")]
        [MaxLength(20)]
        public string ProdQgp { get; set; }
        /// <summary>
        /// 货物属性代码。简单字典GoodsAttr货物属性的海关代码。
        /// </summary>
        [Comment("货物属性代码，简单字典GoodsAttr")]
        [MaxLength(20)]
        public string GoodsAttr { get; set; }
        /// <summary>
        /// 成份_原料/组份。本项货物含有的成份或原料，或化学品组份。
        /// </summary>
        [Comment("成份/原料/组份")]
        [MaxLength(400)]
        public string Stuff { get; set; }
        /// <summary>
        /// UN编码。危险品货物对应《危险化学品目录》中的UN编码。
        /// </summary>
        [Comment("UN编码，危险品UN编码")]
        [MaxLength(20)]
        public string Uncode { get; set; }
        /// <summary>
        /// 危险货物名称。危险品对应《危险化学品目录》中的名称。
        /// </summary>
        [Comment("危险货物名称")]
        [MaxLength(80)]
        public string DangName { get; set; }
        /// <summary>
        /// 危包类别。一类/二类/三类。
        /// </summary>
        [Comment("危包类别")]
        [MaxLength(4)]
        public string DangPackType { get; set; }
        /// <summary>
        /// 危包规格。危险化学品包装规格。
        /// </summary>
        [Comment("危包规格")]
        [MaxLength(24)]
        public string DangPackSpec { get; set; }
        /// <summary>
        /// 境外生产企业名称。
        /// </summary>
        [Comment("境外生产企业名称")]
        [MaxLength(100)]
        public string EngManEntCnm { get; set; }
        /// <summary>
        /// 非危险化学品标识。
        /// </summary>
        [Comment("非危险化学品标识")]
        [MaxLength(1)]
        public string NoDangFlag { get; set; }
        /// <summary>
        /// 目的地代码。货物在境内预定最终抵达的交货地。国内行政区划表的code。
        /// </summary>
        [Comment("目的地代码，国内行政区划code")]
        [MaxLength(8)]
        public string DestCode { get; set; }
        /// <summary>
        /// 检验检疫货物规格。原检验检疫货物规格。
        /// </summary>
        [Comment("检验检疫货物规格")]
        [MaxLength(2000)]
        public string GoodsSpec { get; set; }
        /// <summary>
        /// 货物型号。本项货物的所有型号。
        /// </summary>
        [Comment("货物型号")]
        [MaxLength(2000)]
        public string GoodsModel { get; set; }
        /// <summary>
        /// 货物品牌。本项货物的品牌。
        /// </summary>
        [Comment("货物品牌")]
        [MaxLength(2000)]
        public string GoodsBrand { get; set; }
        /// <summary>
        /// 生产日期。货物的生产日期或生产批号。
        /// </summary>
        [Comment("生产日期")]
        [Precision(3)]
        public DateTime? ProduceDate { get; set; }
        /// <summary>
        /// 生产批号。大文本。
        /// </summary>
        [Comment("生产批号")]
        public string ProdBatchNo { get; set; }
        /// <summary>
        /// 境内目的地/境内货源地。进口指境内目的地，出口指境内货源地。国内地区代码的code。
        /// </summary>
        [Comment("境内目的地/境内货源地，国内地区代码")]
        [MaxLength(5)]
        public string DistrictCode { get; set; }
        /// <summary>
        /// 检验检疫名称。CIQ代码对应的商品描述。
        /// </summary>
        [Comment("检验检疫名称，CIQ代码对应商品描述")]
        [MaxLength(50)]
        public string CiqName { get; set; }
        /// <summary>
        /// 生产单位注册号。出口独有。
        /// </summary>
        [Comment("生产单位注册号，出口独有")]
        [MaxLength(20)]
        public string MnufctrRegno { get; set; }
        /// <summary>
        /// 生产单位名称。出口独有。
        /// </summary>
        [Comment("生产单位名称，出口独有")]
        [MaxLength(150)]
        public string MnufctrRegName { get; set; }
        /// <summary>
        /// 重量单位。简单字典UNIT海关编码。
        /// </summary>
        [Comment("重量单位，简单字典UNIT海关编码")]
        [MaxLength(20)]
        public string InspectionWeightUnit { get; set; }
        /// <summary>
        /// 包装种类。简单字典PackTypeCustom的海关编码。
        /// </summary>
        [Comment("包装种类，简单字典PackTypeCustom海关编码")]
        [MaxLength(20)]
        public string KindPackages { get; set; }
        /// <summary>
        /// 优惠贸易协定项下原产地。国家3位code。
        /// </summary>
        [Comment("优惠贸易协定项下原产地，国家3位code")]
        [MaxLength(3)]
        public string RcepOrigPlaceCode { get; set; }
        /// <summary>
        /// 零件号。
        /// </summary>
        [Comment("零件号")]
        [MaxLength(150)]
        public string PartNo { get; set; }
        /// <summary>
        /// 报关监管条件。
        /// </summary>
        [Comment("报关监管条件")]
        [MaxLength(20)]
        public string CONTROL_MA { get; set; }
        /// <summary>
        /// 报检监管条件。
        /// </summary>
        [Comment("报检监管条件")]
        [MaxLength(20)]
        public string JY_CONTROL_MA { get; set; }
    }
}
