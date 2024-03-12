using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24031201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "PlFileInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注");

            migrationBuilder.CreateTable(
                name: "PlEaDocs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属业务Id"),
                    DocNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "单号"),
                    JobNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "业务编号"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "操作员，可以更改相当于工作号的所有者"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "新建时间,系统默认，不能更改。"),
                    To1Code = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "中转港1港口Code,显示三字码即可。"),
                    By1 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "中转港航班1。"),
                    To2Code = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "中转港2港口Code,显示三字码即可。"),
                    By2 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "中转港航班2。"),
                    To3Code = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "中转港3港口Code,显示三字码即可。"),
                    By3 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "中转港航班3。"),
                    M_PkgsCount = table.Column<int>(type: "int", nullable: false, comment: "主件单数。"),
                    M_weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "主单重量，3位小数"),
                    M_Netweigh = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "主单计费重量，3位小数"),
                    C_PkgsCount = table.Column<int>(type: "int", nullable: false, comment: "包装件单数。结算件数。"),
                    C_Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "结算重量，3位小数"),
                    C_Netweigh = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "结算计费重量，3位小数"),
                    C_MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "结算体积，3位小数"),
                    GoodsStationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "操作地Id,简单字典goodsstation"),
                    PackStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "外包装状态,简单字典packState")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlEaDocs", x => x.Id);
                },
                comment: "空运出口单");

            migrationBuilder.CreateTable(
                name: "PlJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    JobNo = table.Column<string>(type: "nvarchar(450)", nullable: true, comment: "工作号"),
                    JobTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "业务种类id"),
                    CustomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "客户Id"),
                    LinkMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "客户联系人"),
                    LinkTel = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true, comment: "联系人电话"),
                    LinkFax = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true, comment: "联系人传真"),
                    Consignor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "发货人"),
                    Consignee = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "收货人"),
                    Notify = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "通知人"),
                    Agent = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "代理人"),
                    MblNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "主单号"),
                    HblNoString = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "分单号字符串，/分隔多个分单号"),
                    HoldtypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "揽货类型,简单字典HoldType"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "操作员，可以更改相当于工作号的所有者"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "新建时间,系统默认，不能更改。"),
                    JobState = table.Column<byte>(type: "tinyint", nullable: false, comment: "工作状态。NewJob初始=0，Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16."),
                    OperateState = table.Column<byte>(type: "tinyint", nullable: false, comment: "操作状态。NewJob初始=0,Arrived 已到货=2,Declared 已申报=4,Delivered 已配送=8,Submitted 已交单=16,Notified 已通知=32"),
                    AccountDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "出口默认出港日期，进口默认出库日期。"),
                    Etd = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "开航日期。"),
                    ETA = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "到港日期"),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "提送货日期"),
                    SalesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "业务员Id"),
                    CustomerServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "客服Id"),
                    BusinessManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "业务负责人Id"),
                    ShippingLineManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "航线负责人Id"),
                    ContractNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "合同号"),
                    VerifyDate = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "审核日期"),
                    CloseDate = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "关闭日期"),
                    SpecialAgent = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "订舱代理。选择客户资料是订舱代理的客户"),
                    OpCompany = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "操作公司。选择客户资料是订舱代理的客户"),
                    LoadingCode = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "起始港，港口代码，显示三字码即可。"),
                    DestinationCode = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "目的港，港口代码，显示三字码即可"),
                    CarrieCode = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "承运人，船公司或航空公司或，二字码"),
                    CarrierNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "运输工具号，空运显示为航班号，海运显示为船名、陆运显示为卡车号"),
                    GoodsName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "货物名称"),
                    CargoType = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "货物类型.简单字典CARGOTYPE"),
                    PackType = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "包装方式,简单字典PackType"),
                    PkgsCount = table.Column<int>(type: "int", nullable: true, comment: "包装件数"),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true, comment: "毛重,单位Kg,三位小数。委托重量KG数，海运显示为毛重"),
                    MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true, comment: "体积,三位小数,委托体积立方"),
                    GoodsSize = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "尺寸,字符串表达.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlEaDocs_JobId",
                table: "PlEaDocs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_PlJobs_OrgId_JobNo",
                table: "PlJobs",
                columns: new[] { "OrgId", "JobNo" },
                unique: true,
                filter: "[OrgId] IS NOT NULL AND [JobNo] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlEaDocs");

            migrationBuilder.DropTable(
                name: "PlJobs");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "PlFileInfos");
        }
    }
}
