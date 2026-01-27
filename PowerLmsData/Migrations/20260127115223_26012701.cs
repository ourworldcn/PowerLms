using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26012701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EaContainers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MawbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "主表id"),
                    ContainerNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "集装器号"),
                    ContainerName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "集装器名称"),
                    ContainerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "集装器规格"),
                    CurrType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "币种"),
                    PkgNum = table.Column<int>(type: "int", nullable: false, comment: "件数"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "金额"),
                    NetWt = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "净重(KG)"),
                    CubageWt = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "泡重(KG)"),
                    BoardWt = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "箱板重(KG)"),
                    GrossWt = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "毛重(KG)"),
                    SplintWt = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "托重(KG)"),
                    OP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "上架操作员"),
                    PlanceNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "货位号"),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaContainers", x => x.Id);
                },
                comment: "空运出口主单集装器表");

            migrationBuilder.CreateTable(
                name: "EaCubages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MawbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "主表id"),
                    Length = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "货物长CM"),
                    Width = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "货物宽CM"),
                    Height = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "货物高CM"),
                    PkgNum = table.Column<int>(type: "int", nullable: false, comment: "件数"),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "重量"),
                    Measurement = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "体积"),
                    Cubagewt = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "总泡重")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaCubages", x => x.Id);
                },
                comment: "空运出口主单委托明细表");

            migrationBuilder.CreateTable(
                name: "EaGoodsDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MawbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "主表id"),
                    GoodsName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "中文品名"),
                    GoodsEnglishName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "英文品名"),
                    Expertconclusion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "鉴定结论")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaGoodsDetails", x => x.Id);
                },
                comment: "空运出口主单品名明细表");

            migrationBuilder.CreateTable(
                name: "EaMawbOtherCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MawbId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "主表id"),
                    ExesNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "费用代码"),
                    ExesCount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "数量"),
                    ExesUnit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "单价"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "金额"),
                    ChageMode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "到付预付"),
                    DueCarrier = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "收款方"),
                    ExesTYpe = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "费用种类名称")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaMawbOtherCharges", x => x.Id);
                },
                comment: "空运出口主单其他费用表");

            migrationBuilder.CreateTable(
                name: "EaMawbs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "机构id"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "创建时间"),
                    MawbNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "主单号"),
                    IsDirectOrder = table.Column<bool>(type: "bit", nullable: false, comment: "是否直单"),
                    EdischargeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "电放类型DisplayName"),
                    BillStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "主单状态"),
                    ConsignorHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "发货人抬头"),
                    Consignor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "发货人"),
                    CnrAccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "发货人账号"),
                    CnrAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "发货人地址"),
                    CnrCueCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "发货人企业税号"),
                    CnrCity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "发货人城市"),
                    CnrProvince = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "发货人省份"),
                    CnrPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "发货人邮编"),
                    CnrCountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "发货人国家代码"),
                    CnrTel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "发货人电话"),
                    CnrFAX = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "发货人传真"),
                    CnrLinkMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "发货人联系人"),
                    CnrLType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "发货人联系人类型"),
                    CnrEmail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "发货人邮箱"),
                    ConsigneeHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "收货人抬头"),
                    Consignee = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "收货人"),
                    CneAccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "收货人账号"),
                    CneAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "收货人地址"),
                    CneCueCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "收货人企业税号"),
                    CneCity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "收货人城市"),
                    CneProvince = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "收货人省份"),
                    CnePostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "收货人邮编"),
                    CneCountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "收货人国家代码"),
                    CneTel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "收货人电话"),
                    CneFAX = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "收货人传真"),
                    CneLinkMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "收货人联系人"),
                    CneLType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "收货人联系人类型"),
                    CneEmail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "收货人邮箱"),
                    NotifyHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "通知货人抬头"),
                    Notify = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "通知货人"),
                    NtAccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "通知货人账号"),
                    NtAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "通知货人地址"),
                    NtCueCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "通知货人企业税号"),
                    NtCity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "通知货人城市"),
                    NtProvince = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "通知货人省份"),
                    NtPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "通知货人邮编"),
                    NtCountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "通知货人国家代码"),
                    NtTel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "通知货人电话"),
                    NtFAX = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "通知货人传真"),
                    NtLinkMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "通知货人联系人"),
                    NtLType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "通知货人联系人类型"),
                    NtEmail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "通知货人邮箱"),
                    AgentAccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "代理公司账号"),
                    Agent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "代理公司"),
                    AgentHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "代理公司抬头"),
                    AgAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "代理公司地址"),
                    AirAccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "代理公司A/C账号"),
                    AgentIATANo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "代理公司Iata账号"),
                    HSCODE = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "HSCODE"),
                    IsPrintHSCODE = table.Column<bool>(type: "bit", nullable: false, comment: "运单上是否显示Hscode"),
                    AccInfo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "支付信息"),
                    HandingInfo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "操作信息"),
                    OnboardFile = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "随机文件"),
                    LoadingCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "起运港三字码"),
                    Loading = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "起运港名称"),
                    Carrier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "承运人"),
                    CarrierCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "承运人二字代码"),
                    To1 = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "1程港口"),
                    By1 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "1程承运人代码"),
                    To2 = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "2程港口"),
                    By2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "2程承运人代码"),
                    To3 = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "3程港口"),
                    By3 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "3程承运人代码"),
                    SPHCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "特货代码"),
                    CHGSCurr = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, comment: "运费币种"),
                    ExchangeRate = table.Column<float>(type: "real", nullable: false, comment: "汇率"),
                    CHGSCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "费用代码"),
                    WTPayMode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "重量运价付费方式"),
                    OTPayMode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "其他费用付费方式"),
                    Flight = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "头程航班号"),
                    ETD = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "头程航班日期"),
                    SEDFt = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "二程航班号"),
                    SEDFtDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "二程航班日期"),
                    ForCarrige = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "运费申明价值"),
                    ForCustom = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "海关申明价值"),
                    Insurance = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "保险金额"),
                    FreightClass = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "服务等级"),
                    DestinationCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "最终目的港代码"),
                    Destination = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "最终目的港"),
                    DestCountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "最终目的港国家"),
                    PkgsNum = table.Column<int>(type: "int", nullable: false, comment: "件数"),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "重量（指毛重）"),
                    KGS_LBS = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "重量单位"),
                    RateClass = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "运价等级"),
                    SLAC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Shipper Loading Count"),
                    ChargeableWeight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "计费重量"),
                    ChargeableRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "计费价"),
                    ChargeableTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "总运价"),
                    PkgsType = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "包装方式"),
                    MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "体积"),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "中文品名"),
                    GoodsEnglishName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "英文品名"),
                    WTPP = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "重量计算总运费PP"),
                    WTCC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "重量计算总运费CC"),
                    VLPP = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "申明价值附加费PP"),
                    VLCC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "申明价值附加费CC"),
                    TXPP = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Tax PP"),
                    TXCC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "Tax CC"),
                    CAPP = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "应付承运人其它费用PP"),
                    CACC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "应付承运人其它费用CC"),
                    ATPP = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "应付代理其它费用PP"),
                    ATCC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "应付代理其它费用CC"),
                    OTPP = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "其它费用PP"),
                    OTCC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "其它费用CC"),
                    TTPP = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "总的费用PP"),
                    TTCC = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "总的费用CC"),
                    IssuedDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "签发日期"),
                    IssuedMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "签发人"),
                    IssuedPlace = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "签发地点"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    Remark2 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注2")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EaMawbs", x => x.Id);
                },
                comment: "空运出口主单表");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EaContainers");

            migrationBuilder.DropTable(
                name: "EaCubages");

            migrationBuilder.DropTable(
                name: "EaGoodsDetails");

            migrationBuilder.DropTable(
                name: "EaMawbOtherCharges");

            migrationBuilder.DropTable(
                name: "EaMawbs");
        }
    }
}
