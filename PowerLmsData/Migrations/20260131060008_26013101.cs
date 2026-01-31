using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26013101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Airlines_ServiceLevel",
                table: "PlCustomers",
                type: "nvarchar(max)",
                nullable: true,
                comment: "航空公司服务等级。大文本字段，存储多行文本内容。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Airlines_ServiceLevel",
                table: "PlCustomers");
        }
    }
}
