using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23120501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomsCode",
                table: "DD_PlCurrencys",
                type: "nvarchar(max)",
                nullable: true,
                comment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。");

            migrationBuilder.AddColumn<string>(
                name: "CustomsCode",
                table: "DD_PlCountrys",
                type: "nvarchar(max)",
                nullable: true,
                comment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomsCode",
                table: "DD_PlCurrencys");

            migrationBuilder.DropColumn(
                name: "CustomsCode",
                table: "DD_PlCountrys");
        }
    }
}
