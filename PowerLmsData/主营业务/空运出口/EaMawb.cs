/*
 * 项目：PowerLms | 模块：主营业务-空运出口
 * 功能：空运出口主单实体
 * 作者：zc | 创建：2026-01-26
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 空运出口主单表
    /// </summary>
    [Comment("空运出口主单表")]
    public class EaMawb : GuidKeyObjectBase, ISpecificOrg, ICreatorInfo
    {
        /// <summary>
        /// 机构id
        /// </summary>
        [Comment("机构id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 创建者
        /// </summary>
        [Comment("创建者")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Comment("创建时间")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 主单号
        /// </summary>
        [Comment("主单号")]
        [StringLength(20)]
        [Required]
        public string MawbNo { get; set; }

        /// <summary>
        /// 是否直单
        /// </summary>
        [Comment("是否直单")]
        public bool IsDirectOrder { get; set; }

        /// <summary>
        /// 电放类型DisplayName
        /// </summary>
        [Comment("电放类型DisplayName")]
        [StringLength(20)]
        public string EdischargeType { get; set; }

        /// <summary>
        /// 主单状态
        /// </summary>
        [Comment("主单状态")]
        [StringLength(20)]
        public string BillStatus { get; set; }

        /// <summary>
        /// 发货人抬头
        /// </summary>
        [Comment("发货人抬头")]
        public string ConsignorHead { get; set; }

        /// <summary>
        /// 发货人
        /// </summary>
        [Comment("发货人")]
        [StringLength(100)]
        public string Consignor { get; set; }

        /// <summary>
        /// 发货人账号
        /// </summary>
        [Comment("发货人账号")]
        [StringLength(20)]
        public string CnrAccountNo { get; set; }

        /// <summary>
        /// 发货人地址
        /// </summary>
        [Comment("发货人地址")]
        [StringLength(128)]
        public string CnrAddress { get; set; }

        /// <summary>
        /// 发货人企业税号
        /// </summary>
        [Comment("发货人企业税号")]
        [StringLength(32)]
        public string CnrCueCode { get; set; }

        /// <summary>
        /// 发货人城市
        /// </summary>
        [Comment("发货人城市")]
        [StringLength(20)]
        public string CnrCity { get; set; }

        /// <summary>
        /// 发货人省份
        /// </summary>
        [Comment("发货人省份")]
        [StringLength(20)]
        public string CnrProvince { get; set; }

        /// <summary>
        /// 发货人邮编
        /// </summary>
        [Comment("发货人邮编")]
        [StringLength(10)]
        public string CnrPostalCode { get; set; }

        /// <summary>
        /// 发货人国家代码
        /// </summary>
        [Comment("发货人国家代码")]
        [StringLength(5)]
        public string CnrCountryCode { get; set; }

        /// <summary>
        /// 发货人电话
        /// </summary>
        [Comment("发货人电话")]
        [StringLength(32)]
        public string CnrTel { get; set; }

        /// <summary>
        /// 发货人传真
        /// </summary>
        [Comment("发货人传真")]
        [StringLength(32)]
        public string CnrFAX { get; set; }

        /// <summary>
        /// 发货人联系人
        /// </summary>
        [Comment("发货人联系人")]
        [StringLength(32)]
        public string CnrLinkMan { get; set; }

        /// <summary>
        /// 发货人联系人类型
        /// </summary>
        [Comment("发货人联系人类型")]
        [StringLength(5)]
        public string CnrLType { get; set; }

        /// <summary>
        /// 发货人邮箱
        /// </summary>
        [Comment("发货人邮箱")]
        [StringLength(128)]
        public string CnrEmail { get; set; }

        /// <summary>
        /// 收货人抬头
        /// </summary>
        [Comment("收货人抬头")]
        public string ConsigneeHead { get; set; }

        /// <summary>
        /// 收货人
        /// </summary>
        [Comment("收货人")]
        [StringLength(100)]
        public string Consignee { get; set; }

        /// <summary>
        /// 收货人账号
        /// </summary>
        [Comment("收货人账号")]
        [StringLength(20)]
        public string CneAccountNo { get; set; }

        /// <summary>
        /// 收货人地址
        /// </summary>
        [Comment("收货人地址")]
        [StringLength(128)]
        public string CneAddress { get; set; }

        /// <summary>
        /// 收货人企业税号
        /// </summary>
        [Comment("收货人企业税号")]
        [StringLength(32)]
        public string CneCueCode { get; set; }

        /// <summary>
        /// 收货人城市
        /// </summary>
        [Comment("收货人城市")]
        [StringLength(20)]
        public string CneCity { get; set; }

        /// <summary>
        /// 收货人省份
        /// </summary>
        [Comment("收货人省份")]
        [StringLength(20)]
        public string CneProvince { get; set; }

        /// <summary>
        /// 收货人邮编
        /// </summary>
        [Comment("收货人邮编")]
        [StringLength(10)]
        public string CnePostalCode { get; set; }

        /// <summary>
        /// 收货人国家代码
        /// </summary>
        [Comment("收货人国家代码")]
        [StringLength(5)]
        public string CneCountryCode { get; set; }

        /// <summary>
        /// 收货人电话
        /// </summary>
        [Comment("收货人电话")]
        [StringLength(32)]
        public string CneTel { get; set; }

        /// <summary>
        /// 收货人传真
        /// </summary>
        [Comment("收货人传真")]
        [StringLength(32)]
        public string CneFAX { get; set; }

        /// <summary>
        /// 收货人联系人
        /// </summary>
        [Comment("收货人联系人")]
        [StringLength(32)]
        public string CneLinkMan { get; set; }

        /// <summary>
        /// 收货人联系人类型
        /// </summary>
        [Comment("收货人联系人类型")]
        [StringLength(5)]
        public string CneLType { get; set; }

        /// <summary>
        /// 收货人邮箱
        /// </summary>
        [Comment("收货人邮箱")]
        [StringLength(128)]
        public string CneEmail { get; set; }

        /// <summary>
        /// 通知货人抬头
        /// </summary>
        [Comment("通知货人抬头")]
        public string NotifyHead { get; set; }

        /// <summary>
        /// 通知货人
        /// </summary>
        [Comment("通知货人")]
        [StringLength(100)]
        public string Notify { get; set; }

        /// <summary>
        /// 通知货人账号
        /// </summary>
        [Comment("通知货人账号")]
        [StringLength(20)]
        public string NtAccountNo { get; set; }

        /// <summary>
        /// 通知货人地址
        /// </summary>
        [Comment("通知货人地址")]
        [StringLength(128)]
        public string NtAddress { get; set; }

        /// <summary>
        /// 通知货人企业税号
        /// </summary>
        [Comment("通知货人企业税号")]
        [StringLength(32)]
        public string NtCueCode { get; set; }

        /// <summary>
        /// 通知货人城市
        /// </summary>
        [Comment("通知货人城市")]
        [StringLength(20)]
        public string NtCity { get; set; }

        /// <summary>
        /// 通知货人省份
        /// </summary>
        [Comment("通知货人省份")]
        [StringLength(20)]
        public string NtProvince { get; set; }

        /// <summary>
        /// 通知货人邮编
        /// </summary>
        [Comment("通知货人邮编")]
        [StringLength(10)]
        public string NtPostalCode { get; set; }

        /// <summary>
        /// 通知货人国家代码
        /// </summary>
        [Comment("通知货人国家代码")]
        [StringLength(5)]
        public string NtCountryCode { get; set; }

        /// <summary>
        /// 通知货人电话
        /// </summary>
        [Comment("通知货人电话")]
        [StringLength(32)]
        public string NtTel { get; set; }

        /// <summary>
        /// 通知货人传真
        /// </summary>
        [Comment("通知货人传真")]
        [StringLength(32)]
        public string NtFAX { get; set; }

        /// <summary>
        /// 通知货人联系人
        /// </summary>
        [Comment("通知货人联系人")]
        [StringLength(32)]
        public string NtLinkMan { get; set; }

        /// <summary>
        /// 通知货人联系人类型
        /// </summary>
        [Comment("通知货人联系人类型")]
        [StringLength(5)]
        public string NtLType { get; set; }

        /// <summary>
        /// 通知货人邮箱
        /// </summary>
        [Comment("通知货人邮箱")]
        [StringLength(128)]
        public string NtEmail { get; set; }

        /// <summary>
        /// 代理公司账号
        /// </summary>
        [Comment("代理公司账号")]
        [StringLength(20)]
        public string AgentAccountNo { get; set; }

        /// <summary>
        /// 代理公司
        /// </summary>
        [Comment("代理公司")]
        [StringLength(100)]
        public string Agent { get; set; }

        /// <summary>
        /// 代理公司抬头
        /// </summary>
        [Comment("代理公司抬头")]
        public string AgentHead { get; set; }

        /// <summary>
        /// 代理公司地址
        /// </summary>
        [Comment("代理公司地址")]
        [StringLength(128)]
        public string AgAddress { get; set; }

        /// <summary>
        /// 代理公司A/C账号
        /// </summary>
        [Comment("代理公司A/C账号")]
        [StringLength(20)]
        public string AirAccountNo { get; set; }

        /// <summary>
        /// 代理公司Iata账号
        /// </summary>
        [Comment("代理公司Iata账号")]
        [StringLength(20)]
        public string AgentIATANo { get; set; }

        /// <summary>
        /// HSCODE
        /// </summary>
        [Comment("HSCODE")]
        public string HSCODE { get; set; }

        /// <summary>
        /// 运单上是否显示Hscode
        /// </summary>
        [Comment("运单上是否显示Hscode")]
        public bool IsPrintHSCODE { get; set; }

        /// <summary>
        /// 支付信息
        /// </summary>
        [Comment("支付信息")]
        public string AccInfo { get; set; }

        /// <summary>
        /// 操作信息
        /// </summary>
        [Comment("操作信息")]
        public string HandingInfo { get; set; }

        /// <summary>
        /// 随机文件
        /// </summary>
        [Comment("随机文件")]
        [StringLength(128)]
        public string OnboardFile { get; set; }

        /// <summary>
        /// 起运港三字码
        /// </summary>
        [Comment("起运港三字码")]
        [StringLength(5)]
        public string LoadingCode { get; set; }

        /// <summary>
        /// 起运港名称
        /// </summary>
        [Comment("起运港名称")]
        [StringLength(20)]
        public string Loading { get; set; }

        /// <summary>
        /// 承运人
        /// </summary>
        [Comment("承运人")]
        [StringLength(100)]
        public string Carrier { get; set; }

        /// <summary>
        /// 承运人二字代码
        /// </summary>
        [Comment("承运人二字代码")]
        [StringLength(2)]
        public string CarrierCode { get; set; }

        /// <summary>
        /// 1程港口
        /// </summary>
        [Comment("1程港口")]
        [StringLength(5)]
        public string To1 { get; set; }

        /// <summary>
        /// 1程承运人代码
        /// </summary>
        [Comment("1程承运人代码")]
        [StringLength(2)]
        public string By1 { get; set; }

        /// <summary>
        /// 2程港口
        /// </summary>
        [Comment("2程港口")]
        [StringLength(5)]
        public string To2 { get; set; }

        /// <summary>
        /// 2程承运人代码
        /// </summary>
        [Comment("2程承运人代码")]
        [StringLength(2)]
        public string By2 { get; set; }

        /// <summary>
        /// 3程港口
        /// </summary>
        [Comment("3程港口")]
        [StringLength(5)]
        public string To3 { get; set; }

        /// <summary>
        /// 3程承运人代码
        /// </summary>
        [Comment("3程承运人代码")]
        [StringLength(2)]
        public string By3 { get; set; }

        /// <summary>
        /// 特货代码
        /// </summary>
        [Comment("特货代码")]
        [StringLength(100)]
        public string SPHCode { get; set; }

        /// <summary>
        /// 运费币种
        /// </summary>
        [Comment("运费币种")]
        [StringLength(3)]
        public string CHGSCurr { get; set; }

        /// <summary>
        /// 汇率
        /// </summary>
        [Comment("汇率")]
        public float ExchangeRate { get; set; }

        /// <summary>
        /// 费用代码
        /// </summary>
        [Comment("费用代码")]
        [StringLength(10)]
        public string CHGSCode { get; set; }

        /// <summary>
        /// 重量运价付费方式
        /// </summary>
        [Comment("重量运价付费方式")]
        [StringLength(2)]
        public string WTPayMode { get; set; }

        /// <summary>
        /// 其他费用付费方式
        /// </summary>
        [Comment("其他费用付费方式")]
        [StringLength(2)]
        public string OTPayMode { get; set; }

        /// <summary>
        /// 头程航班号
        /// </summary>
        [Comment("头程航班号")]
        [StringLength(10)]
        public string Flight { get; set; }

        /// <summary>
        /// 头程航班日期
        /// </summary>
        [Comment("头程航班日期")]
        [Precision(3)]
        public DateTime? ETD { get; set; }

        /// <summary>
        /// 二程航班号
        /// </summary>
        [Comment("二程航班号")]
        [StringLength(10)]
        public string SEDFt { get; set; }

        /// <summary>
        /// 二程航班日期
        /// </summary>
        [Comment("二程航班日期")]
        [Precision(3)]
        public DateTime? SEDFtDate { get; set; }

        /// <summary>
        /// 运费申明价值
        /// </summary>
        [Comment("运费申明价值")]
        [StringLength(32)]
        public string ForCarrige { get; set; }

        /// <summary>
        /// 海关申明价值
        /// </summary>
        [Comment("海关申明价值")]
        [StringLength(32)]
        public string ForCustom { get; set; }

        /// <summary>
        /// 保险金额
        /// </summary>
        [Comment("保险金额")]
        [StringLength(32)]
        public string Insurance { get; set; }

        /// <summary>
        /// 服务等级
        /// </summary>
        [Comment("服务等级")]
        [StringLength(20)]
        public string FreightClass { get; set; }

        /// <summary>
        /// 最终目的港代码
        /// </summary>
        [Comment("最终目的港代码")]
        [StringLength(5)]
        public string DestinationCode { get; set; }

        /// <summary>
        /// 最终目的港
        /// </summary>
        [Comment("最终目的港")]
        [StringLength(20)]
        public string Destination { get; set; }

        /// <summary>
        /// 最终目的港国家
        /// </summary>
        [Comment("最终目的港国家")]
        [StringLength(2)]
        public string DestCountryCode { get; set; }

        /// <summary>
        /// 件数
        /// </summary>
        [Comment("件数")]
        public int PkgsNum { get; set; }

        /// <summary>
        /// 重量（指毛重）
        /// </summary>
        [Comment("重量（指毛重）")]
        [Precision(18, 3)]
        public decimal Weight { get; set; }

        /// <summary>
        /// 重量单位
        /// </summary>
        [Comment("重量单位")]
        [StringLength(10)]
        public string KGS_LBS { get; set; }

        /// <summary>
        /// 运价等级
        /// </summary>
        [Comment("运价等级")]
        [StringLength(2)]
        public string RateClass { get; set; }

        /// <summary>
        /// Shipper Loading Count
        /// </summary>
        [Comment("Shipper Loading Count")]
        [StringLength(20)]
        public string SLAC { get; set; }

        /// <summary>
        /// 计费重量
        /// </summary>
        [Comment("计费重量")]
        [Precision(18, 3)]
        public decimal ChargeableWeight { get; set; }

        /// <summary>
        /// 计费价
        /// </summary>
        [Comment("计费价")]
        [Precision(18, 4)]
        public decimal ChargeableRate { get; set; }

        /// <summary>
        /// 总运价
        /// </summary>
        [Comment("总运价")]
        [Precision(18, 2)]
        public decimal ChargeableTotal { get; set; }

        /// <summary>
        /// 包装方式
        /// </summary>
        [Comment("包装方式")]
        public string PkgsType { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        [Comment("体积")]
        [Precision(18, 3)]
        public decimal MeasureMent { get; set; }

        /// <summary>
        /// 中文品名
        /// </summary>
        [Comment("中文品名")]
        public string GoodsName { get; set; }

        /// <summary>
        /// 英文品名
        /// </summary>
        [Comment("英文品名")]
        public string GoodsEnglishName { get; set; }

        /// <summary>
        /// 重量计算总运费PP
        /// </summary>
        [Comment("重量计算总运费PP")]
        [Precision(18, 2)]
        public decimal WTPP { get; set; }

        /// <summary>
        /// 重量计算总运费CC
        /// </summary>
        [Comment("重量计算总运费CC")]
        [Precision(18, 2)]
        public decimal WTCC { get; set; }

        /// <summary>
        /// 申明价值附加费PP
        /// </summary>
        [Comment("申明价值附加费PP")]
        [Precision(18, 2)]
        public decimal VLPP { get; set; }

        /// <summary>
        /// 申明价值附加费CC
        /// </summary>
        [Comment("申明价值附加费CC")]
        [Precision(18, 2)]
        public decimal VLCC { get; set; }

        /// <summary>
        /// Tax PP
        /// </summary>
        [Comment("Tax PP")]
        [Precision(18, 2)]
        public decimal TXPP { get; set; }

        /// <summary>
        /// Tax CC
        /// </summary>
        [Comment("Tax CC")]
        [Precision(18, 2)]
        public decimal TXCC { get; set; }

        /// <summary>
        /// 应付承运人其它费用PP
        /// </summary>
        [Comment("应付承运人其它费用PP")]
        [Precision(18, 2)]
        public decimal CAPP { get; set; }

        /// <summary>
        /// 应付承运人其它费用CC
        /// </summary>
        [Comment("应付承运人其它费用CC")]
        [Precision(18, 2)]
        public decimal CACC { get; set; }

        /// <summary>
        /// 应付代理其它费用PP
        /// </summary>
        [Comment("应付代理其它费用PP")]
        [Precision(18, 2)]
        public decimal ATPP { get; set; }

        /// <summary>
        /// 应付代理其它费用CC
        /// </summary>
        [Comment("应付代理其它费用CC")]
        [Precision(18, 2)]
        public decimal ATCC { get; set; }

        /// <summary>
        /// 其它费用PP
        /// </summary>
        [Comment("其它费用PP")]
        [Precision(18, 2)]
        public decimal OTPP { get; set; }

        /// <summary>
        /// 其它费用CC
        /// </summary>
        [Comment("其它费用CC")]
        [Precision(18, 2)]
        public decimal OTCC { get; set; }

        /// <summary>
        /// 总的费用PP
        /// </summary>
        [Comment("总的费用PP")]
        [StringLength(32)]
        public string TTPP { get; set; }

        /// <summary>
        /// 总的费用CC
        /// </summary>
        [Comment("总的费用CC")]
        [StringLength(32)]
        public string TTCC { get; set; }

        /// <summary>
        /// 签发日期
        /// </summary>
        [Comment("签发日期")]
        [Precision(3)]
        public DateTime? IssuedDate { get; set; }

        /// <summary>
        /// 签发人
        /// </summary>
        [Comment("签发人")]
        [StringLength(32)]
        public string IssuedMan { get; set; }

        /// <summary>
        /// 签发地点
        /// </summary>
        [Comment("签发地点")]
        [StringLength(32)]
        public string IssuedPlace { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 备注2
        /// </summary>
        [Comment("备注2")]
        public string Remark2 { get; set; }
    }

    /// <summary>
    /// 空运出口主单其他费用表
    /// </summary>
    [Comment("空运出口主单其他费用表")]
    public class EaMawbOtherCharge : GuidKeyObjectBase
    {
        /// <summary>
        /// 主表id
        /// </summary>
        [Comment("主表id")]
        public Guid MawbId { get; set; }

        /// <summary>
        /// 费用代码
        /// </summary>
        [Comment("费用代码")]
        [StringLength(20)]
        public string ExesNo { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Comment("数量")]
        [Precision(18, 2)]
        public decimal ExesCount { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [Comment("单价")]
        [Precision(18, 2)]
        public decimal ExesUnit { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        [Comment("金额")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 到付预付
        /// </summary>
        [Comment("到付预付")]
        [StringLength(5)]
        public string ChageMode { get; set; }

        /// <summary>
        /// 收款方
        /// </summary>
        [Comment("收款方")]
        [StringLength(2)]
        public string DueCarrier { get; set; }

        /// <summary>
        /// 费用种类名称
        /// </summary>
        [Comment("费用种类名称")]
        [StringLength(32)]
        public string ExesTYpe { get; set; }
    }

    /// <summary>
    /// 空运出口主单委托明细表
    /// </summary>
    [Comment("空运出口主单委托明细表")]
    public class EaCubage : GuidKeyObjectBase
    {
        /// <summary>
        /// 主表id
        /// </summary>
        [Comment("主表id")]
        public Guid MawbId { get; set; }

        /// <summary>
        /// 货物长CM
        /// </summary>
        [Comment("货物长CM")]
        [Precision(18, 2)]
        public decimal Length { get; set; }

        /// <summary>
        /// 货物宽CM
        /// </summary>
        [Comment("货物宽CM")]
        [Precision(18, 2)]
        public decimal Width { get; set; }

        /// <summary>
        /// 货物高CM
        /// </summary>
        [Comment("货物高CM")]
        [Precision(18, 2)]
        public decimal Height { get; set; }

        /// <summary>
        /// 件数
        /// </summary>
        [Comment("件数")]
        public int PkgNum { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        [Comment("重量")]
        [Precision(18, 3)]
        public decimal Weight { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        [Comment("体积")]
        [Precision(18, 3)]
        public decimal Measurement { get; set; }

        /// <summary>
        /// 总泡重
        /// </summary>
        [Comment("总泡重")]
        [Precision(18, 3)]
        public decimal Cubagewt { get; set; }
    }

    /// <summary>
    /// 空运出口主单品名明细表
    /// </summary>
    [Comment("空运出口主单品名明细表")]
    public class EaGoodsDetail : GuidKeyObjectBase
    {
        /// <summary>
        /// 主表id
        /// </summary>
        [Comment("主表id")]
        public Guid MawbId { get; set; }

        /// <summary>
        /// 中文品名
        /// </summary>
        [Comment("中文品名")]
        [StringLength(100)]
        public string GoodsName { get; set; }

        /// <summary>
        /// 英文品名
        /// </summary>
        [Comment("英文品名")]
        [StringLength(100)]
        public string GoodsEnglishName { get; set; }

        /// <summary>
        /// 鉴定结论
        /// </summary>
        [Comment("鉴定结论")]
        [StringLength(100)]
        public string Expertconclusion { get; set; }
    }

    /// <summary>
    /// 空运出口主单集装器表
    /// </summary>
    [Comment("空运出口主单集装器表")]
    public class EaContainer : GuidKeyObjectBase
    {
        /// <summary>
        /// 主表id
        /// </summary>
        [Comment("主表id")]
        public Guid MawbId { get; set; }

        /// <summary>
        /// 集装器号
        /// </summary>
        [Comment("集装器号")]
        [StringLength(20)]
        public string ContainerNo { get; set; }

        /// <summary>
        /// 集装器名称
        /// </summary>
        [Comment("集装器名称")]
        [StringLength(50)]
        public string ContainerName { get; set; }

        /// <summary>
        /// 集装器规格
        /// </summary>
        [Comment("集装器规格")]
        [StringLength(20)]
        public string ContainerType { get; set; }

        /// <summary>
        /// 币种
        /// </summary>
        [Comment("币种")]
        [StringLength(5)]
        public string CurrType { get; set; }

        /// <summary>
        /// 件数
        /// </summary>
        [Comment("件数")]
        public int PkgNum { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        [Comment("金额")]
        [Precision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// 净重(KG)
        /// </summary>
        [Comment("净重(KG)")]
        [Precision(18, 3)]
        public decimal NetWt { get; set; }

        /// <summary>
        /// 泡重(KG)
        /// </summary>
        [Comment("泡重(KG)")]
        [Precision(18, 3)]
        public decimal CubageWt { get; set; }

        /// <summary>
        /// 箱板重(KG)
        /// </summary>
        [Comment("箱板重(KG)")]
        [Precision(18, 3)]
        public decimal BoardWt { get; set; }

        /// <summary>
        /// 毛重(KG)
        /// </summary>
        [Comment("毛重(KG)")]
        [Precision(18, 3)]
        public decimal GrossWt { get; set; }

        /// <summary>
        /// 托重(KG)
        /// </summary>
        [Comment("托重(KG)")]
        [Precision(18, 3)]
        public decimal SplintWt { get; set; }

        /// <summary>
        /// 上架操作员
        /// </summary>
        [Comment("上架操作员")]
        [StringLength(20)]
        public string OP { get; set; }

        /// <summary>
        /// 货位号
        /// </summary>
        [Comment("货位号")]
        [StringLength(20)]
        public string PlanceNo { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Comment("备注")]
        [StringLength(256)]
        public string Remark { get; set; }
    }
}




