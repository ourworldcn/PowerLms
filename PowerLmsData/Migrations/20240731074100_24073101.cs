using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24073101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlEsDocs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属业务Id"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "操作状态。0=初始化单据但尚未操作，1=已报价,2=已订舱,4=已配舱,8=已装箱，16=已申报,32=已出提单,128=已放货。"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "操作员，可以更改相当于工作号的所有者"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "新建时间,系统默认，不能更改。"),
                    IsSpecifyLoad = table.Column<bool>(type: "bit", nullable: false, comment: "是否指装"),
                    IsPartial = table.Column<bool>(type: "bit", nullable: false, comment: "是否分批"),
                    IsFumigation = table.Column<bool>(type: "bit", nullable: false, comment: "是否熏蒸"),
                    IsTranshipment = table.Column<bool>(type: "bit", nullable: false, comment: "是否转运"),
                    MerchantStyleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "贸易条款Id。"),
                    LoadStyle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "装运条款。"),
                    SeaPortAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "装船港港区Id。"),
                    DelegationKind = table.Column<byte>(type: "tinyint", nullable: false, comment: "委托类型。FCL=1、LCL=2、BULK=4。"),
                    TransTermId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "运输条款Id"),
                    BookingsRemark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "订舱要求"),
                    BookingsDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "订舱日期"),
                    BillPaymentModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "付款方式Id"),
                    BillPaymentPlace = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "付款地点"),
                    SoNumber = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "S/O编号"),
                    WarehousingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "进仓编号"),
                    GoodsReleaseModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "放货方式"),
                    WarehousingDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "放舱日期"),
                    Voyage = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "航次"),
                    CargoRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "航线字典ID"),
                    TransitPort = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "中转港"),
                    CutOffGoodsDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "截货日期"),
                    CutOffDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "截关日期"),
                    SeaborneRemark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "海运说明"),
                    BargeName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "驳船船名"),
                    BargeVoyage = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "驳船船名"),
                    BargeSailDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "驳船开航日期"),
                    BargeArrivalDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "驳船到港日期"),
                    BargeLoadingHarbor = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "驳船装船港"),
                    BargeDestinationHarbor = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "驳船目的港"),
                    Consigner = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "发货人"),
                    ConsignerTitle = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "发货人抬头"),
                    Informers = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "通知人"),
                    InformersTitle = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "通知人抬头"),
                    MarkHeader = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "唛头"),
                    ContainerKindCountString = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "箱量。"),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "品名。"),
                    Total1 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "总计1,箱。"),
                    Total2 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "总计2,货。"),
                    Total3 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "总计3,合计。"),
                    BookingGoodsTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "货种Id。"),
                    DangerousLevel = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "危险级别。"),
                    DangerousPage = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "危规页码。"),
                    Features = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "特征。"),
                    UnNumber = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "UN No。"),
                    FlashPoint = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "闪点。"),
                    RefrigerationTemperature = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "冷藏温度。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlEsDocs", x => x.Id);
                },
                comment: "海运出口单");

            migrationBuilder.CreateIndex(
                name: "IX_PlEsDocs_JobId",
                table: "PlEsDocs",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlEsDocs");
        }
    }
}
