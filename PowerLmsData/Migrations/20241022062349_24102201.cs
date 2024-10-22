using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24102201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DirectionOfIE",
                table: "PlInvoicess",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "收支方向。true=收入，false=支出(默认)。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirectionOfIE",
                table: "PlInvoicess");
        }
    }
}
