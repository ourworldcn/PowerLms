using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26021101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IaManifestDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "主表Id"),
                    MawbNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "主单号。11位纯数字，原样记录"),
                    HBLNO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "分单号。为空表示主单行，原样记录"),
                    MawbId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "关联的主单或分单Id"),
                    Quantity = table.Column<int>(type: "int", nullable: false, comment: "委托件数"),
                    TotalGross = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "重量。单位：千克"),
                    PaymentCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "付款方式。PP=预付，CC=到付"),
                    CargoDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "货物描述"),
                    Consignor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "发货人"),
                    Consignee = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "收货人"),
                    PkgsType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "包装方式。关联简单字典PackType的code"),
                    MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "体积。单位：立方米"),
                    LoadingCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "起运港。港口三字码"),
                    DestinationCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, comment: "目的港。港口三字码"),
                    HtallyDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "分单理货时间"),
                    HInWareDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "分单入库时间"),
                    HarrivalDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "分单运抵时间"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IaManifestDetails", x => x.Id);
                },
                comment: "空运进口舱单明细表");

            migrationBuilder.CreateTable(
                name: "IaManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    MawbNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "主单号。11位纯数字，无横杠"),
                    FlightNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "航班号"),
                    FlightDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "航班日期"),
                    TypeCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true, comment: "运输方式代码。空运默认为4"),
                    ReceiverID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "收货地代码"),
                    ExitCustomsOffice = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "申报地海关"),
                    ArrivalDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "到达卸货地日期"),
                    TralTools = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "运输工具名称"),
                    TralToolsCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "运输工具代码"),
                    ActualDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "理货开始时间"),
                    CompletedDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "理货完成时间"),
                    ActualManagerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "理货管理部门代码"),
                    MtallyDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "主单理货时间"),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IaManifests", x => x.Id);
                },
                comment: "空运进口舱单主表");

            migrationBuilder.CreateIndex(
                name: "IX_IaManifestDetails_MawbId",
                table: "IaManifestDetails",
                column: "MawbId");

            migrationBuilder.CreateIndex(
                name: "IX_IaManifestDetails_MawbNo_HBLNO",
                table: "IaManifestDetails",
                columns: new[] { "MawbNo", "HBLNO" });

            migrationBuilder.CreateIndex(
                name: "IX_IaManifestDetails_ParentId",
                table: "IaManifestDetails",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_IaManifests_OrgId_MawbNo",
                table: "IaManifests",
                columns: new[] { "OrgId", "MawbNo" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IaManifestDetails");

            migrationBuilder.DropTable(
                name: "IaManifests");
        }
    }
}
