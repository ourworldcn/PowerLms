using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26030401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomsDeclarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    AgentCode = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "申报单位代码"),
                    AgentName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true, comment: "申报单位名称"),
                    ApprNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "批准文号，实填外汇核销单号"),
                    BillNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "提单号"),
                    ContrNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "合同号"),
                    CopCode = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true, comment: "录入单位代码，必填"),
                    CopName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true, comment: "录入单位名称，必填"),
                    CustomMaster = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true, comment: "主管海关（申报地海关），简单字典CustomMaster"),
                    CutMode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "征免性质"),
                    DataSource = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "数据来源，空值预留字段"),
                    DeclTrnRel = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "报关/转关关系标志。0：一般报关单；1：转关提前报关单"),
                    DistinatePort = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true, comment: "经停港/指运港"),
                    EdiId = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "报关标志。1普通报关，默认1"),
                    EntryId = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "海关编号"),
                    EntryType = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "报关单类型。0普通报关单，默认0"),
                    FeeCurr = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "运费币制，币种代码"),
                    FeeMark = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "运费标记。1：率，2：单价，3：总价"),
                    FeeRate = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "运费/率"),
                    GrossWet = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "毛重"),
                    IEDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "进出口日期"),
                    IEFlag = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "进出口标志。I进口，E出口"),
                    IEPort = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true, comment: "进出口岸，简单字典IEPORT"),
                    InputerName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "录入员名称，导入暂存时必填"),
                    InsurCurr = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "保险费币制，币种代码"),
                    InsurMark = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "保险费标记。1：率，2：单价，3：总价"),
                    InsurRate = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "保险费/率"),
                    LicenseNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "许可证编号"),
                    ManualNo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true, comment: "备案号"),
                    NetWt = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "净重"),
                    NoteS = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    OtherCurr = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "杂费币制"),
                    OtherMark = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "杂费标志。1：率，2：单价，3：总价"),
                    OtherRate = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "杂费/率"),
                    OwnerCode = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "消费使用/生产销售单位代码"),
                    OwnerName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true, comment: "消费使用/生产销售单位名称"),
                    PackNo = table.Column<int>(type: "int", nullable: true, comment: "件数"),
                    PartenerID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "申报人标识（申报人姓名）"),
                    PDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "打印日期（预录入日期）"),
                    PreEntryId = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true, comment: "预录入编号"),
                    Risk = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "风险评估参数，空值上海专用"),
                    SeqNo = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "数据中心统一编号，首次导入传空值由系统生成"),
                    TgdNo = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "关联单据号，空值预留字段"),
                    TradeCountry = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "启运国/运抵国，国家3位code"),
                    TradeMode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true, comment: "监管方式，简单字典TradeMode海关编码"),
                    TradeCode = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "境内收发货人编号，私有通道导入时必填"),
                    TrafMode = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "运输方式代码，字典Custom-TrafMode海关编码"),
                    TrafName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "运输工具代码及名称"),
                    TradeName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true, comment: "境内收发货人名称，私有通道导入时必填"),
                    TransMode = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "成交方式，简单字典TransMode海关编码"),
                    Type = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true, comment: "单据类型"),
                    TypistNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true, comment: "录入员IC卡号，导入暂存时必填"),
                    WrapType = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "包装种类"),
                    ChkSurety = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "担保验放标志。1是，0否"),
                    BillType = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "备案清单类型"),
                    CopCodeScc = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "录入单位统一编码"),
                    TradeCoScc = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "收发货人统一编码"),
                    AgentCodeScc = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "申报单位统一编码"),
                    OwnerCodeScc = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true, comment: "消费使用/生产销售单位统一编码"),
                    TradeAreaCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "贸易国别，国家字典3位code"),
                    CheckFlow = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "查验分流。1查验分流，0不是查验分流"),
                    TaxAaminMark = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "税收征管标记。0无，1有"),
                    MarkNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "唛码，标记及号码"),
                    DespPortCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true, comment: "启运港代码，港口字典海关代码"),
                    EntyPortCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true, comment: "入境口岸代码"),
                    GoodsPlace = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "存放地点"),
                    BLNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "B/L号，提货单号"),
                    InspOrgCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "口岸检验检疫机关，简单字典InspOrgCode"),
                    SpecDeclFlag = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "特种业务标识，10位0或1字符"),
                    PurpOrgCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "目的地检验检疫机关，简单字典InspOrgCode"),
                    DespDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "启运日期，发货日期"),
                    CmplDschrgDt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "卸毕日期"),
                    CorrelationReasonFlag = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "关联理由"),
                    VsaOrgCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "领证机关，简单字典InspOrgCode"),
                    OrigBoxFlag = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "原集装箱标识。1是，0否"),
                    DeclareName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "申报人员姓名"),
                    NoOtherPack = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "无其他包装。0未选，1选中"),
                    OrgCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "检验检疫受理机关，简单字典InspOrgCode"),
                    OverseasConsignorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "境外发货人代码"),
                    OverseasConsignorCname = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, comment: "境外收发货人中文名称"),
                    OverseasConsignorEname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "境外发货人名称（外文）"),
                    OverseasConsignorAddr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "境外收发货人地址"),
                    OverseasConsigneeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "境外收货人编码"),
                    OverseasConsigneeEname = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true, comment: "境外收货人名称（外文）"),
                    DomesticConsigneeEname = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true, comment: "境内收发货人名称（外文）"),
                    CorrelationNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "关联号码"),
                    EDIRemark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "EDI申报备注"),
                    EDIRemark2 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "EDI申报备注2"),
                    TradeCiqCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "境内收发货人检验检疫编码"),
                    OwnerCiqCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "消费使用/生产销售单位检验检疫编码"),
                    DeclCiqCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "申报单位检验检疫编码"),
                    PROMISE_ITMES1 = table.Column<bool>(type: "bit", nullable: false, comment: "承诺事项_特殊关系确认"),
                    PROMISE_ITMES2 = table.Column<bool>(type: "bit", nullable: false, comment: "承诺事项_价格影响确认"),
                    PROMISE_ITMES3 = table.Column<bool>(type: "bit", nullable: false, comment: "承诺事项_支付特许权使用费确认"),
                    PROMISE_ITMES4 = table.Column<bool>(type: "bit", nullable: false, comment: "承诺事项_公式定价确认"),
                    PROMISE_ITMES5 = table.Column<bool>(type: "bit", nullable: false, comment: "承诺事项_暂定价格确认"),
                    bTwoevidence = table.Column<bool>(type: "bit", nullable: false, comment: "两步申报_涉证"),
                    bTwoinspection = table.Column<bool>(type: "bit", nullable: false, comment: "两步申报_涉检"),
                    bTwoTax = table.Column<bool>(type: "bit", nullable: false, comment: "两步申报_涉税"),
                    ContriNO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "保证金_缴款书号"),
                    ContriDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "保证金_押宝日期"),
                    ContriNum = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "保证金_保证金金额"),
                    ContriJaDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "保证金_结案时间"),
                    Tax_Tax = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "税单_税金"),
                    Tax_TaxNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "税单_税号"),
                    Tax_Addedtax = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "税单_增值税"),
                    Tax_AddedtaxNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "税单_增值税号"),
                    Tax_Overdue = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "税单_滞纳金"),
                    Tax_OverdueNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "税单_滞纳金号"),
                    VoyageNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "运输工具编码"),
                    RelatedCustomsNO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "关联报关单号"),
                    AssRecordNO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "关联备案号"),
                    TaxCompany = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "纳税单位。001经营单位，002收货单位，003申报单位"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "创建日期"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建人Id"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "附加说明（备注）")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomsDeclarations", x => x.Id);
                },
                comment: "报关单主表");

            migrationBuilder.CreateTable(
                name: "CustomsGoodsLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属报关单主表Id"),
                    ClassMark = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "归类标志，空值"),
                    CodeTS = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "商品编号（HS编码）"),
                    ContrItem = table.Column<long>(type: "bigint", nullable: true, comment: "备案序号，程序控制9位"),
                    DeclPrice = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "申报单价"),
                    DeclTotal = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "申报总价"),
                    DutyMode = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "征减免税方式，简单字典TradeMode海关编码"),
                    ExgNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true, comment: "货号"),
                    ExgVersion = table.Column<int>(type: "int", nullable: true, comment: "版本号"),
                    Factor = table.Column<decimal>(type: "decimal(11,3)", precision: 11, scale: 3, nullable: true, comment: "申报计量单位与法定单位比例因子"),
                    FirstUnit = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "第一计量单位（法定单位），简单字典UNIT海关编码"),
                    FirstQty = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "第一法定数量"),
                    GUnit = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "成交计量单位，简单字典UNIT海关编码"),
                    GModel = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "商品规格、型号，申报要素"),
                    GName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "商品名称"),
                    GNo = table.Column<long>(type: "bigint", nullable: true, comment: "商品序号"),
                    GQty = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "成交数量"),
                    OriginCountry = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "原产国，国家字典3位code"),
                    SecondUnit = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "第二计量单位，简单字典UNIT海关编码"),
                    SecondQty = table.Column<decimal>(type: "decimal(19,5)", precision: 19, scale: 5, nullable: true, comment: "第二法定数量"),
                    TradeCurr = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "成交币制"),
                    UseTo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "报关用途/生产厂家，简单字典Useage海关代码"),
                    WorkUsd = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true, comment: "工缴费"),
                    DestinationCountry = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "最终目的国（地区），国家字典3位code"),
                    CiqCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "检验检疫编码，3位检验检疫编码"),
                    DeclGoodsEname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "商品英文名称"),
                    OrigPlaceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "报检原产国，国家3位代码"),
                    Purpose = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true, comment: "报检用途，简单字典Useage海关代码"),
                    ProdValidDt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "产品有效期"),
                    ProdQgp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "产品保质期"),
                    GoodsAttr = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "货物属性代码，简单字典GoodsAttr"),
                    Stuff = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true, comment: "成份/原料/组份"),
                    Uncode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "UN编码，危险品UN编码"),
                    DangName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true, comment: "危险货物名称"),
                    DangPackType = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true, comment: "危包类别"),
                    DangPackSpec = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true, comment: "危包规格"),
                    EngManEntCnm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "境外生产企业名称"),
                    NoDangFlag = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true, comment: "非危险化学品标识"),
                    DestCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true, comment: "目的地代码，国内行政区划code"),
                    GoodsSpec = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "检验检疫货物规格"),
                    GoodsModel = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "货物型号"),
                    GoodsBrand = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "货物品牌"),
                    ProduceDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "生产日期"),
                    ProdBatchNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "生产批号"),
                    DistrictCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "境内目的地/境内货源地，国内地区代码"),
                    CiqName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "检验检疫名称，CIQ代码对应商品描述"),
                    MnufctrRegno = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "生产单位注册号，出口独有"),
                    MnufctrRegName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, comment: "生产单位名称，出口独有"),
                    InspectionWeightUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "重量单位，简单字典UNIT海关编码"),
                    KindPackages = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "包装种类，简单字典PackTypeCustom海关编码"),
                    RcepOrigPlaceCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "优惠贸易协定项下原产地，国家3位code"),
                    PartNo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, comment: "零件号"),
                    CONTROL_MA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "报关监管条件"),
                    JY_CONTROL_MA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "报检监管条件")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomsGoodsLists", x => x.Id);
                },
                comment: "报关单货物明细子表");

            migrationBuilder.CreateIndex(
                name: "IX_CustomsDeclarations_OrgId_SeqNo",
                table: "CustomsDeclarations",
                columns: new[] { "OrgId", "SeqNo" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomsGoodsLists_ParentId",
                table: "CustomsGoodsLists",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomsDeclarations");

            migrationBuilder.DropTable(
                name: "CustomsGoodsLists");
        }
    }
}
