using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25043001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChargeWeight",
                table: "PlJobs",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                comment: "计费重量,单位Kg,三位小数。委托计费重量，海运显示为净重。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeWeight",
                table: "PlJobs");
        }
    }
}
