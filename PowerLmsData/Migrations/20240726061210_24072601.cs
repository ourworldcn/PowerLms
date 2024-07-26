using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24072601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlIsDocs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属业务Id"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "操作员，可以更改相当于工作号的所有者"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "新建时间,系统默认，不能更改。"),
                    ShipSNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "船次。"),
                    CargoRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "航线字典id。"),
                    PlaceModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "放货方式字典Id。"),
                    BargeName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "驳船船名。"),
                    BargeSNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "驳船班次。"),
                    BargeStartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "驳船开航日期。"),
                    AnticipateBillDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "预计换单日期。"),
                    BillDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "实际换单日期。"),
                    ArrivedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "进口日期。"),
                    DeliveryDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "提货日期。"),
                    UpToDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "提货日期。"),
                    BillModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "提单方式Id。"),
                    ContainerFreeDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "免箱期。"),
                    ContainerNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "箱号。"),
                    SpeelContainerNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "箱封号。"),
                    MerchantStyleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "贸易条款Id。"),
                    DestPortId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "目的港港区Id。"),
                    DelegationKind = table.Column<byte>(type: "tinyint", nullable: false, comment: "委托类型。FCL=1、LCL=2、BULK=4。"),
                    TransTermId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "运输条款Id。"),
                    BillPaymentModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "付款方式Id。"),
                    FileStrings = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "随船文件。服务器不解析，逗号分隔。"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "操作状态。1=已换单,2=船已到港,4=卸货完成,8=已提货。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlIsDocs", x => x.Id);
                },
                comment: "海运进口单");

            migrationBuilder.CreateIndex(
                name: "IX_PlIsDocs_JobId",
                table: "PlIsDocs",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlIsDocs");
        }
    }
}
