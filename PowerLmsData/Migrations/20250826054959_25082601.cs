using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25082601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountDate",
                table: "PlJobs");

            migrationBuilder.CreateTable(
                name: "PlOrganizationParameters",
                columns: table => new
                {
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "机构ID，主键"),
                    CurrentAccountingPeriod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true, comment: "当前账期，格式YYYYMM，只读，由关闭账期操作更新"),
                    BillHeader1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "账单抬头1，用于报表打印"),
                    BillHeader2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "账单抬头2，用于报表打印"),
                    BillFooter = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "账单落款，用于报表打印")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlOrganizationParameters", x => x.OrgId);
                },
                comment: "机构参数表，存储机构级别的配置信息");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlOrganizationParameters");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountDate",
                table: "PlJobs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "出口默认出港日期，进口默认出库日期。");
        }
    }
}
