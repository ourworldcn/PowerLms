/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关单主表实体（对应海关EDI报文字段）
 * 技术要点：EF Core实体，字段与海关标准报文保持一致，日期均可为空
 * 作者：zc | 创建：2026-02
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 报关单主表。
    /// </summary>
    [Comment("报关单主表")]
    [Index(nameof(OrgId), nameof(SeqNo), IsUnique = false)]
    public class CustomsDeclaration : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomsDeclaration()
        {
        }
        /// <summary>
        /// 所属机构Id（多租户）。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// 申报单位代码。
        /// </summary>
        [Comment("申报单位代码")]
        [MaxLength(32)]
        public string AgentCode { get; set; }
        /// <summary>
        /// 申报单位名称。
        /// </summary>
        [Comment("申报单位名称")]
        [MaxLength(128)]
        public string AgentName { get; set; }
        /// <summary>
        /// 批准文号。实填"外汇核销单号"。
        /// </summary>
        [Comment("批准文号，实填外汇核销单号")]
        [MaxLength(32)]
        public string ApprNo { get; set; }
        /// <summary>
        /// 提单号。
        /// </summary>
        [Comment("提单号")]
        [MaxLength(32)]
        public string BillNo { get; set; }
        /// <summary>
        /// 合同号。
        /// </summary>
        [Comment("合同号")]
        [MaxLength(32)]
        public string ContrNo { get; set; }
        /// <summary>
        /// 录入单位代码。必填。
        /// </summary>
        [Comment("录入单位代码，必填")]
        [MaxLength(9)]
        public string CopCode { get; set; }
        /// <summary>
        /// 录入单位名称。必填。
        /// </summary>
        [Comment("录入单位名称，必填")]
        [MaxLength(128)]
        public string CopName { get; set; }
        /// <summary>
        /// 主管海关（申报地海关）。简单字典CustomMaster。
        /// </summary>
        [Comment("主管海关（申报地海关），简单字典CustomMaster")]
        [MaxLength(4)]
        public string CustomMaster { get; set; }
        /// <summary>
        /// 征免性质。
        /// </summary>
        [Comment("征免性质")]
        [MaxLength(3)]
        public string CutMode { get; set; }
        /// <summary>
        /// 数据来源。空值，预留字段，4位代码。
        /// </summary>
        [Comment("数据来源，空值预留字段")]
        [MaxLength(5)]
        public string DataSource { get; set; }
        /// <summary>
        /// 报关/转关关系标志。0：一般报关单；1：转关提前报关单，前端默认0。
        /// </summary>
        [Comment("报关/转关关系标志。0：一般报关单；1：转关提前报关单")]
        [MaxLength(1)]
        public string DeclTrnRel { get; set; }
        /// <summary>
        /// 经停港/指运港。
        /// </summary>
        [Comment("经停港/指运港")]
        [MaxLength(6)]
        public string DistinatePort { get; set; }
        /// <summary>
        /// 报关标志。1：普通报关，3：北方转关提前，5：南方转关提前，6：普通报关（运输工具名称以'◎'开头），默认1。
        /// </summary>
        [Comment("报关标志。1普通报关，默认1")]
        [MaxLength(1)]
        public string EdiId { get; set; }
        /// <summary>
        /// 海关编号。
        /// </summary>
        [Comment("海关编号")]
        [MaxLength(32)]
        public string EntryId { get; set; }
        /// <summary>
        /// 报关单类型。0普通，L带清单，W无纸，D清单且无纸，M无纸化通关，默认0。
        /// </summary>
        [Comment("报关单类型。0普通报关单，默认0")]
        [MaxLength(1)]
        public string EntryType { get; set; }
        /// <summary>
        /// 运费币制。币种代码。
        /// </summary>
        [Comment("运费币制，币种代码")]
        [MaxLength(3)]
        public string FeeCurr { get; set; }
        /// <summary>
        /// 运费标记。1：率，2：单价，3：总价。
        /// </summary>
        [Comment("运费标记。1：率，2：单价，3：总价")]
        [MaxLength(1)]
        public string FeeMark { get; set; }
        /// <summary>
        /// 运费／率。海关精度Z(12).Z(7)。
        /// </summary>
        [Comment("运费/率")]
        [Precision(19, 5)]
        public decimal? FeeRate { get; set; }
        /// <summary>
        /// 毛重。
        /// </summary>
        [Comment("毛重")]
        [Precision(19, 5)]
        public decimal? GrossWet { get; set; }
        /// <summary>
        /// 进出口日期。
        /// </summary>
        [Comment("进出口日期")]
        [Precision(3)]
        public DateTime? IEDate { get; set; }
        /// <summary>
        /// 进出口标志。"I"：进口。"E"：出口。
        /// </summary>
        [Comment("进出口标志。I进口，E出口")]
        [MaxLength(1)]
        public string IEFlag { get; set; }
        /// <summary>
        /// 进出口岸。简单字典IEPORT，需要代码转换。
        /// </summary>
        [Comment("进出口岸，简单字典IEPORT")]
        [MaxLength(4)]
        public string IEPort { get; set; }
        /// <summary>
        /// 录入员名称。导入暂存时必填。
        /// </summary>
        [Comment("录入员名称，导入暂存时必填")]
        [MaxLength(32)]
        public string InputerName { get; set; }
        /// <summary>
        /// 保险费币制。币种代码。
        /// </summary>
        [Comment("保险费币制，币种代码")]
        [MaxLength(3)]
        public string InsurCurr { get; set; }
        /// <summary>
        /// 保险费标记。1：率，2：单价，3：总价。
        /// </summary>
        [Comment("保险费标记。1：率，2：单价，3：总价")]
        [MaxLength(1)]
        public string InsurMark { get; set; }
        /// <summary>
        /// 保险费／率。海关精度Z(12).Z(7)。
        /// </summary>
        [Comment("保险费/率")]
        [Precision(19, 5)]
        public decimal? InsurRate { get; set; }
        /// <summary>
        /// 许可证编号。
        /// </summary>
        [Comment("许可证编号")]
        [MaxLength(32)]
        public string LicenseNo { get; set; }
        /// <summary>
        /// 备案号。
        /// </summary>
        [Comment("备案号")]
        [MaxLength(12)]
        public string ManualNo { get; set; }
        /// <summary>
        /// 净重。
        /// </summary>
        [Comment("净重")]
        [Precision(19, 5)]
        public decimal? NetWt { get; set; }
        /// <summary>
        /// 备注。大文本。
        /// </summary>
        [Comment("备注")]
        public string NoteS { get; set; }
        /// <summary>
        /// 杂费币制。
        /// </summary>
        [Comment("杂费币制")]
        [MaxLength(3)]
        public string OtherCurr { get; set; }
        /// <summary>
        /// 杂费标志。1：率，2：单价，3：总价。
        /// </summary>
        [Comment("杂费标志。1：率，2：单价，3：总价")]
        [MaxLength(1)]
        public string OtherMark { get; set; }
        /// <summary>
        /// 杂费／率。海关精度Z(12).Z(7)。
        /// </summary>
        [Comment("杂费/率")]
        [Precision(19, 5)]
        public decimal? OtherRate { get; set; }
        /// <summary>
        /// 消费使用/生产销售单位代码。10位或9位，或NO。
        /// </summary>
        [Comment("消费使用/生产销售单位代码")]
        [MaxLength(32)]
        public string OwnerCode { get; set; }
        /// <summary>
        /// 消费使用/生产销售单位名称。
        /// </summary>
        [Comment("消费使用/生产销售单位名称")]
        [MaxLength(128)]
        public string OwnerName { get; set; }
        /// <summary>
        /// 件数。
        /// </summary>
        [Comment("件数")]
        public int? PackNo { get; set; }
        /// <summary>
        /// 申报人标识（申报人姓名）。
        /// </summary>
        [Comment("申报人标识（申报人姓名）")]
        [MaxLength(32)]
        public string PartenerID { get; set; }
        /// <summary>
        /// 打印日期（预录入日期）。
        /// </summary>
        [Comment("打印日期（预录入日期）")]
        [Precision(3)]
        public DateTime? PDate { get; set; }
        /// <summary>
        /// 预录入编号。
        /// </summary>
        [Comment("预录入编号")]
        [MaxLength(9)]
        public string PreEntryId { get; set; }
        /// <summary>
        /// 风险评估参数。空值（上海专用）。
        /// </summary>
        [Comment("风险评估参数，空值上海专用")]
        [MaxLength(10)]
        public string Risk { get; set; }
        /// <summary>
        /// 数据中心统一编号。首次导入传空值，由系统自动生成并返回客户端。
        /// </summary>
        [Comment("数据中心统一编号，首次导入传空值由系统生成")]
        [MaxLength(32)]
        public string SeqNo { get; set; }
        /// <summary>
        /// 关联单据号。空值，预留字段。
        /// </summary>
        [Comment("关联单据号，空值预留字段")]
        [MaxLength(32)]
        public string TgdNo { get; set; }
        /// <summary>
        /// 启运国/运抵国。国家3位code（进口：起运国；出口：运抵国）。
        /// </summary>
        [Comment("启运国/运抵国，国家3位code")]
        [MaxLength(3)]
        public string TradeCountry { get; set; }
        /// <summary>
        /// 监管方式。简单字典TradeMode海关编码。
        /// </summary>
        [Comment("监管方式，简单字典TradeMode海关编码")]
        [MaxLength(4)]
        public string TradeMode { get; set; }
        /// <summary>
        /// 境内收发货人编号。私有通道导入时必填。
        /// </summary>
        [Comment("境内收发货人编号，私有通道导入时必填")]
        [MaxLength(32)]
        public string TradeCode { get; set; }
        /// <summary>
        /// 运输方式代码。字典运输方式Custom-TrafMode海关编码。
        /// </summary>
        [Comment("运输方式代码，字典Custom-TrafMode海关编码")]
        [MaxLength(1)]
        public string TrafMode { get; set; }
        /// <summary>
        /// 运输工具代码及名称。
        /// </summary>
        [Comment("运输工具代码及名称")]
        [MaxLength(64)]
        public string TrafName { get; set; }
        /// <summary>
        /// 境内收发货人名称。私有通道导入时必填。
        /// </summary>
        [Comment("境内收发货人名称，私有通道导入时必填")]
        [MaxLength(128)]
        public string TradeName { get; set; }
        /// <summary>
        /// 成交方式。简单字典TransMode海关编码。
        /// </summary>
        [Comment("成交方式，简单字典TransMode海关编码")]
        [MaxLength(1)]
        public string TransMode { get; set; }
        /// <summary>
        /// 单据类型。1：一般报关单，2：属地申报口岸验放，3：保税区进出境备案清单，4：两单一审备案清单。
        /// </summary>
        [Comment("单据类型")]
        [MaxLength(6)]
        public string Type { get; set; }
        /// <summary>
        /// 录入员IC卡号。导入暂存时必填。
        /// </summary>
        [Comment("录入员IC卡号，导入暂存时必填")]
        [MaxLength(32)]
        public string TypistNo { get; set; }
        /// <summary>
        /// 包装种类。
        /// </summary>
        [Comment("包装种类")]
        [MaxLength(2)]
        public string WrapType { get; set; }
        /// <summary>
        /// 担保验放标志。1：是；0：否。
        /// </summary>
        [Comment("担保验放标志。1是，0否")]
        [MaxLength(1)]
        public string ChkSurety { get; set; }
        /// <summary>
        /// 备案清单类型。1普通，2先进区后报关，3分送集报备案清单，4分送集报报关单。
        /// </summary>
        [Comment("备案清单类型")]
        [MaxLength(1)]
        public string BillType { get; set; }
        /// <summary>
        /// 录入单位统一编码。
        /// </summary>
        [Comment("录入单位统一编码")]
        [MaxLength(32)]
        public string CopCodeScc { get; set; }
        /// <summary>
        /// 收发货人统一编码。
        /// </summary>
        [Comment("收发货人统一编码")]
        [MaxLength(32)]
        public string TradeCoScc { get; set; }
        /// <summary>
        /// 申报单位统一编码。
        /// </summary>
        [Comment("申报单位统一编码")]
        [MaxLength(32)]
        public string AgentCodeScc { get; set; }
        /// <summary>
        /// 消费使用/生产销售单位统一编码。
        /// </summary>
        [Comment("消费使用/生产销售单位统一编码")]
        [MaxLength(32)]
        public string OwnerCodeScc { get; set; }
        /// <summary>
        /// 贸易国别。国家字典3位code。
        /// </summary>
        [Comment("贸易国别，国家字典3位code")]
        [MaxLength(3)]
        public string TradeAreaCode { get; set; }
        /// <summary>
        /// 查验分流。1：查验分流；0：不是查验分流。
        /// </summary>
        [Comment("查验分流。1查验分流，0不是查验分流")]
        [MaxLength(1)]
        public string CheckFlow { get; set; }
        /// <summary>
        /// 税收征管标记。0无，1有。
        /// </summary>
        [Comment("税收征管标记。0无，1有")]
        [MaxLength(1)]
        public string TaxAaminMark { get; set; }
        /// <summary>
        /// 唛码。标记及号码【本批货物的标记和号码】。大文本。
        /// </summary>
        [Comment("唛码，标记及号码")]
        public string MarkNo { get; set; }
        /// <summary>
        /// 启运港代码。港口字典海关代码。
        /// </summary>
        [Comment("启运港代码，港口字典海关代码")]
        [MaxLength(8)]
        public string DespPortCode { get; set; }
        /// <summary>
        /// 入境口岸代码。货物从运输工具卸离的第一个境内口岸。
        /// </summary>
        [Comment("入境口岸代码")]
        [MaxLength(8)]
        public string EntyPortCode { get; set; }
        /// <summary>
        /// 存放地点。货物存放地点【报检时货物的存放地点】。
        /// </summary>
        [Comment("存放地点")]
        [MaxLength(128)]
        public string GoodsPlace { get; set; }
        /// <summary>
        /// B/L号。提货单号【本批货物的提货单或出库单号码】。
        /// </summary>
        [Comment("B/L号，提货单号")]
        [MaxLength(64)]
        public string BLNo { get; set; }
        /// <summary>
        /// 口岸检验检疫机关。简单字典InspOrgCode海关编码。
        /// </summary>
        [Comment("口岸检验检疫机关，简单字典InspOrgCode")]
        [MaxLength(10)]
        public string InspOrgCode { get; set; }
        /// <summary>
        /// 特种业务标识。10位字符，每位0或1，依次对应：国际赛事/特殊军工/国际援助/国际会议/直通放行/外交礼遇/转关。
        /// </summary>
        [Comment("特种业务标识，10位0或1字符")]
        [MaxLength(10)]
        public string SpecDeclFlag { get; set; }
        /// <summary>
        /// 目的地检验检疫机关。简单字典InspOrgCode。
        /// </summary>
        [Comment("目的地检验检疫机关，简单字典InspOrgCode")]
        [MaxLength(10)]
        public string PurpOrgCode { get; set; }
        /// <summary>
        /// 启运日期（发货日期）。
        /// </summary>
        [Comment("启运日期，发货日期")]
        [Precision(3)]
        public DateTime? DespDate { get; set; }
        /// <summary>
        /// 卸毕日期。本批货物全部卸离运输工具的日期。
        /// </summary>
        [Comment("卸毕日期")]
        [Precision(3)]
        public DateTime? CmplDschrgDt { get; set; }
        /// <summary>
        /// 关联理由。关联报检号的关联理由。
        /// </summary>
        [Comment("关联理由")]
        [MaxLength(2)]
        public string CorrelationReasonFlag { get; set; }
        /// <summary>
        /// 领证机关。简单字典InspOrgCode海关编码。
        /// </summary>
        [Comment("领证机关，简单字典InspOrgCode")]
        [MaxLength(10)]
        public string VsaOrgCode { get; set; }
        /// <summary>
        /// 原集装箱标识。入境原集装箱装载直接到目的机构。1是，0否。
        /// </summary>
        [Comment("原集装箱标识。1是，0否")]
        [MaxLength(1)]
        public string OrigBoxFlag { get; set; }
        /// <summary>
        /// 申报人员姓名。
        /// </summary>
        [Comment("申报人员姓名")]
        [MaxLength(64)]
        public string DeclareName { get; set; }
        /// <summary>
        /// 无其他包装。0未选/有其他包装；1选中/无其他包装。
        /// </summary>
        [Comment("无其他包装。0未选，1选中")]
        [MaxLength(1)]
        public string NoOtherPack { get; set; }
        /// <summary>
        /// 检验检疫受理机关。简单字典InspOrgCode海关编码。
        /// </summary>
        [Comment("检验检疫受理机关，简单字典InspOrgCode")]
        [MaxLength(10)]
        public string OrgCode { get; set; }
        /// <summary>
        /// 境外发货人代码。
        /// </summary>
        [Comment("境外发货人代码")]
        [MaxLength(64)]
        public string OverseasConsignorCode { get; set; }
        /// <summary>
        /// 境外收发货人中文名称。
        /// </summary>
        [Comment("境外收发货人中文名称")]
        [MaxLength(256)]
        public string OverseasConsignorCname { get; set; }
        /// <summary>
        /// 境外发货人名称（外文）。
        /// </summary>
        [Comment("境外发货人名称（外文）")]
        [MaxLength(128)]
        public string OverseasConsignorEname { get; set; }
        /// <summary>
        /// 境外收发货人地址。
        /// </summary>
        [Comment("境外收发货人地址")]
        [MaxLength(128)]
        public string OverseasConsignorAddr { get; set; }
        /// <summary>
        /// 境外收货人编码。
        /// </summary>
        [Comment("境外收货人编码")]
        [MaxLength(64)]
        public string OverseasConsigneeCode { get; set; }
        /// <summary>
        /// 境外收货人名称（外文）。
        /// </summary>
        [Comment("境外收货人名称（外文）")]
        [MaxLength(512)]
        public string OverseasConsigneeEname { get; set; }
        /// <summary>
        /// 境内收发货人名称（外文）。
        /// </summary>
        [Comment("境内收发货人名称（外文）")]
        [MaxLength(512)]
        public string DomesticConsigneeEname { get; set; }
        /// <summary>
        /// 关联号码。
        /// </summary>
        [Comment("关联号码")]
        [MaxLength(512)]
        public string CorrelationNo { get; set; }
        /// <summary>
        /// EDI申报备注。大文本。
        /// </summary>
        [Comment("EDI申报备注")]
        public string EDIRemark { get; set; }
        /// <summary>
        /// EDI申报备注2。大文本。
        /// </summary>
        [Comment("EDI申报备注2")]
        public string EDIRemark2 { get; set; }
        /// <summary>
        /// 境内收发货人检验检疫编码。
        /// </summary>
        [Comment("境内收发货人检验检疫编码")]
        [MaxLength(10)]
        public string TradeCiqCode { get; set; }
        /// <summary>
        /// 消费使用/生产销售单位检验检疫编码。
        /// </summary>
        [Comment("消费使用/生产销售单位检验检疫编码")]
        [MaxLength(10)]
        public string OwnerCiqCode { get; set; }
        /// <summary>
        /// 申报单位检验检疫编码。
        /// </summary>
        [Comment("申报单位检验检疫编码")]
        [MaxLength(10)]
        public string DeclCiqCode { get; set; }
        /// <summary>
        /// 承诺事项_特殊关系确认。是1，否0。
        /// </summary>
        [Comment("承诺事项_特殊关系确认")]
        public bool PROMISE_ITMES1 { get; set; }
        /// <summary>
        /// 承诺事项_价格影响确认。是1，否0。
        /// </summary>
        [Comment("承诺事项_价格影响确认")]
        public bool PROMISE_ITMES2 { get; set; }
        /// <summary>
        /// 承诺事项_支付特许权使用费确认。是1，否0。
        /// </summary>
        [Comment("承诺事项_支付特许权使用费确认")]
        public bool PROMISE_ITMES3 { get; set; }
        /// <summary>
        /// 承诺事项_公式定价确认。是1，否0。
        /// </summary>
        [Comment("承诺事项_公式定价确认")]
        public bool PROMISE_ITMES4 { get; set; }
        /// <summary>
        /// 承诺事项_暂定价格确认。是1，否0。
        /// </summary>
        [Comment("承诺事项_暂定价格确认")]
        public bool PROMISE_ITMES5 { get; set; }
        /// <summary>
        /// 两步申报_涉证。是1，否0。
        /// </summary>
        [Comment("两步申报_涉证")]
        public bool bTwoevidence { get; set; }
        /// <summary>
        /// 两步申报_涉检。是1，否0。
        /// </summary>
        [Comment("两步申报_涉检")]
        public bool bTwoinspection { get; set; }
        /// <summary>
        /// 两步申报_涉税。是1，否0。
        /// </summary>
        [Comment("两步申报_涉税")]
        public bool bTwoTax { get; set; }
        /// <summary>
        /// 保证金_缴款书号。
        /// </summary>
        [Comment("保证金_缴款书号")]
        [MaxLength(32)]
        public string ContriNO { get; set; }
        /// <summary>
        /// 保证金_押宝日期。
        /// </summary>
        [Comment("保证金_押宝日期")]
        [Precision(3)]
        public DateTime? ContriDate { get; set; }
        /// <summary>
        /// 保证金_保证金金额。
        /// </summary>
        [Comment("保证金_保证金金额")]
        [Precision(19, 5)]
        public decimal? ContriNum { get; set; }
        /// <summary>
        /// 保证金_结案时间。
        /// </summary>
        [Comment("保证金_结案时间")]
        [Precision(3)]
        public DateTime? ContriJaDate { get; set; }
        /// <summary>
        /// 税单_税金。
        /// </summary>
        [Comment("税单_税金")]
        [Precision(19, 5)]
        public decimal? Tax_Tax { get; set; }
        /// <summary>
        /// 税单_税号。
        /// </summary>
        [Comment("税单_税号")]
        [MaxLength(32)]
        public string Tax_TaxNo { get; set; }
        /// <summary>
        /// 税单_增值税。
        /// </summary>
        [Comment("税单_增值税")]
        [Precision(19, 5)]
        public decimal? Tax_Addedtax { get; set; }
        /// <summary>
        /// 税单_增值税号。
        /// </summary>
        [Comment("税单_增值税号")]
        [MaxLength(32)]
        public string Tax_AddedtaxNo { get; set; }
        /// <summary>
        /// 税单_滞纳金。
        /// </summary>
        [Comment("税单_滞纳金")]
        [Precision(19, 5)]
        public decimal? Tax_Overdue { get; set; }
        /// <summary>
        /// 税单_滞纳金号。
        /// </summary>
        [Comment("税单_滞纳金号")]
        [MaxLength(32)]
        public string Tax_OverdueNo { get; set; }
        /// <summary>
        /// 运输工具编码。
        /// </summary>
        [Comment("运输工具编码")]
        [MaxLength(32)]
        public string VoyageNumber { get; set; }
        /// <summary>
        /// 关联报关单号。
        /// </summary>
        [Comment("关联报关单号")]
        [MaxLength(64)]
        public string RelatedCustomsNO { get; set; }
        /// <summary>
        /// 关联备案号。
        /// </summary>
        [Comment("关联备案号")]
        [MaxLength(64)]
        public string AssRecordNO { get; set; }
        /// <summary>
        /// 纳税单位。001：经营单位，002：收货单位，003：申报单位。
        /// </summary>
        [Comment("纳税单位。001经营单位，002收货单位，003申报单位")]
        [MaxLength(3)]
        public string TaxCompany { get; set; }
        /// <summary>
        /// 创建日期（即 ICreatorInfo.CreateDateTime）。修改时由 EntityManager.Modify 自动保护，不会被前端覆盖。
        /// </summary>
        [Comment("创建日期")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; }
        /// <summary>
        /// 创建人Id（即 ICreatorInfo.CreateBy）。修改时由 EntityManager.Modify 自动保护，不会被前端覆盖。
        /// </summary>
        [Comment("创建人Id")]
        public Guid? CreateBy { get; set; }
        /// <summary>
        /// 附加说明（备注）。大文本。
        /// </summary>
        [Comment("附加说明（备注）")]
        public string Remark { get; set; }
        /// <summary>
        /// 两步申报任务编号。
        /// </summary>
        [Comment("两步申报任务编号")]
        [MaxLength(64)]
        public string TaskNumber2 { get; set; }
    }
}
