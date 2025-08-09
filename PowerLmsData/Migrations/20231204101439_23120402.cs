using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23120402 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "DD_BusinessTypeDataDics",
                comment: "业务大类");

            migrationBuilder.AddColumn<short>(
                name: "OrderNumber",
                table: "DD_BusinessTypeDataDics",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                comment: "排序序号。越小的越靠前");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "DD_BusinessTypeDataDics");

            migrationBuilder.AlterTable(
                name: "DD_BusinessTypeDataDics",
                oldComment: "业务大类");
        }
    }
}
