using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25060501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "TaxInvoiceInfoItems",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                comment: "单价（不含税）",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "单价（不含税）");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "TaxInvoiceInfoItems",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                comment: "数量",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComment: "数量");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceInclusiveTax",
                table: "TaxInvoiceInfoItems",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m,
                comment: "含税单价,服务器不使用");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitPriceInclusiveTax",
                table: "TaxInvoiceInfoItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "TaxInvoiceInfoItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "单价（不含税）",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldComment: "单价（不含税）");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "TaxInvoiceInfoItems",
                type: "decimal(18,2)",
                nullable: false,
                comment: "数量",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldComment: "数量");
        }
    }
}
