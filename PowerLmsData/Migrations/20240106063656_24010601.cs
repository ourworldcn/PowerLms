using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _24010601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "PlCustomerBusinessHeaders",
                newName: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "PlCustomerBusinessHeaders",
                newName: "AccountId");
        }
    }
}
