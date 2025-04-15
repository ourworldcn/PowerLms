using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25041501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRedLetter",
                table: "TaxInvoiceInfos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRedLetter",
                table: "TaxInvoiceInfos");
        }
    }
}
