using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24070901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlIaDocs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属业务Id"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "操作员，可以更改相当于工作号的所有者"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "新建时间,系统默认，不能更改。"),
                    PositionNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "仓位号。"),
                    GoodssSatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "货物状态字典Id。"),
                    ShishouCount = table.Column<int>(type: "int", nullable: false, comment: "实收件数。"),
                    RukuDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "入库日期。"),
                    CargoTypeIdString = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "货物类型字典id的字符串集合，逗号分隔。"),
                    FollowAircraftFilesIsString = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "简单字典进口随机文件FollowAircraftFiles字典Id的字符串集合，逗号分隔。"),
                    PickUpPlace = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "提货地。"),
                    PickUpCo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "提货公司。"),
                    PickUpPerson = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "提货人。"),
                    PickUpDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "提货时间。"),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "放行人Id。"),
                    IsInspection = table.Column<bool>(type: "bit", nullable: true, comment: "是否海关查验。"),
                    IsQuarantine = table.Column<bool>(type: "bit", nullable: true, comment: "是否检疫查验。"),
                    TradeModeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "贸易方式Id。"),
                    YujiXiaobaoDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "预计消保日期。"),
                    XiaobaoDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "实际消保日期，null表示未消保。"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "特别说明。"),
                    OperationalStatus = table.Column<byte>(type: "tinyint", nullable: false, comment: "操作状态。初始化单据（此时未经任何操作）=0,已调单=1,已申报=2,已出税=3,海关已放行=4,已入库=5,仓库已放行=6。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlIaDocs", x => x.Id);
                },
                comment: "空运进口单");

            migrationBuilder.CreateIndex(
                name: "IX_PlIaDocs_JobId",
                table: "PlIaDocs",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlIaDocs");
        }
    }
}
