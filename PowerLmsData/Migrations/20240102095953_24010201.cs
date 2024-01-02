using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _24010201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TacCountNo",
                table: "PlCustomers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "财务编码");

            migrationBuilder.AddColumn<byte>(
                name: "JobPermission",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "报表权限。1=个人，2=组织，4=公司。");

            migrationBuilder.AddColumn<byte>(
                name: "ReportPermission",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "报表权限。1=个人，2=组织，4=公司，8=商户。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TacCountNo",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "JobPermission",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ReportPermission",
                table: "Accounts");
        }
    }
}
