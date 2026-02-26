using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _27022601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EsHbls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属海运出口工作号"),
                    BillStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "提单状态"),
                    HBLNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "货代提单编号"),
                    SeaPayMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "付款方式"),
                    SeaPayPlace = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "付款地点"),
                    SeaTerms = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "运输条款"),
                    PreCarriage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "前程运输"),
                    Receipt = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "收货地"),
                    Loading = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "装船港"),
                    LoadingDesc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "装船港描述"),
                    Discharge = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "中转港"),
                    DischargeDesc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "中转港描述"),
                    OceanDestination = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "目的港"),
                    OceanDestinationDesc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "目的港描述"),
                    ShipOwner = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, comment: "船公司"),
                    Vessel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "船名"),
                    Voyage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "航次"),
                    HblFreight = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "费用描述"),
                    SaillingDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "装船日期"),
                    ETD = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "开船日期"),
                    ETA = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "到港日期"),
                    PPD_AMT = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "预付运费"),
                    CCT_AMT = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "到付运费"),
                    Consignor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "发货人名称"),
                    ConsignorHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "发货人抬头"),
                    Consignee = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "收货人名称"),
                    ConsigneeHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "收货人抬头"),
                    Notify = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "通知人名称"),
                    NotifyHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "通知人抬头"),
                    AlsoNotify = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "第二通知人抬头"),
                    ContainerNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "箱号"),
                    ContainerNum = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "箱量"),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "品名"),
                    Marks = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "唛头"),
                    PkgsNum = table.Column<int>(type: "int", nullable: true, comment: "件数"),
                    PkgsType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "包装类型"),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true, comment: "毛重(KGS)"),
                    MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true, comment: "体积(CBM)"),
                    BookingTotal = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, comment: "总计1-箱量的英文合计"),
                    TotalGoods = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, comment: "总计2-件数的英文合计"),
                    HBLIssueNumber = table.Column<int>(type: "int", nullable: true, comment: "正本张数"),
                    HBLIssueMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "签发人"),
                    HBLIssueDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "签发时间"),
                    HBLIssuePlace = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "签发地点"),
                    MblGoodsMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "放货方式"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    HBLRemark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "提单附页")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EsHbls", x => x.Id);
                },
                comment: "海运出口分提单");

            migrationBuilder.CreateTable(
                name: "EsMbls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属海运出口工作号"),
                    IsDirectorder = table.Column<bool>(type: "bit", nullable: false, comment: "是否直单"),
                    BillStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "提单状态"),
                    MBLNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "船东提单编号"),
                    SeaPayMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "付款方式"),
                    SeaPayPlace = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "付款地点"),
                    SeaTerms = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "运输条款"),
                    PreCarriage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "前程运输"),
                    Receipt = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "收货地"),
                    Loading = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "装船港"),
                    LoadingDesc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "装船港描述"),
                    Discharge = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "中转港"),
                    DischargeDesc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "中转港描述"),
                    OceanDestination = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "目的港"),
                    OceanDestinationDesc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "目的港描述"),
                    ShipOwner = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, comment: "船公司"),
                    Vessel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "船名"),
                    Voyage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "航次"),
                    MblFreight = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "费用描述"),
                    SaillingDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "装船日期"),
                    ETD = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "开船日期"),
                    ETA = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "到港日期"),
                    Consignor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "发货人名称"),
                    ConsignorHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "发货人抬头"),
                    Consignee = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "收货人名称"),
                    ConsigneeHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "收货人抬头"),
                    Notify = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "通知人名称"),
                    NotifyHead = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "通知人抬头"),
                    AlsoNotify = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "第二通知人抬头"),
                    ContainerNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "箱号"),
                    ContainerNum = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "箱量"),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "品名"),
                    Marks = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "唛头"),
                    PkgsNum = table.Column<int>(type: "int", nullable: true, comment: "件数"),
                    PkgsType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "包装类型"),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true, comment: "毛重(KGS)"),
                    MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true, comment: "体积(CBM)"),
                    BookingTotal = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, comment: "总计1-箱量的英文合计"),
                    TotalGoods = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true, comment: "总计2-件数的英文合计"),
                    MBLIssueNumber = table.Column<int>(type: "int", nullable: true, comment: "正本张数"),
                    MBLIssueMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "签发人"),
                    MBLIssueDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "签发时间"),
                    MBLIssuePlace = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "签发地点"),
                    MblGoodsMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "放货方式"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    MBLRemark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "提单附页")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EsMbls", x => x.Id);
                },
                comment: "海运出口主提单");

            migrationBuilder.CreateIndex(
                name: "IX_EsHbls_JobId",
                table: "EsHbls",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_EsMbls_JobId",
                table: "EsMbls",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EsHbls");

            migrationBuilder.DropTable(
                name: "EsMbls");
        }
    }
}
