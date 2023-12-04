using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

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
