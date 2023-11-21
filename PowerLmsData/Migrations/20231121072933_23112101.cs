using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SimpleDataDics_OrgId_DataDicId",
                table: "SimpleDataDics",
                columns: new[] { "OrgId", "DataDicId" });

            migrationBuilder.CreateIndex(
                name: "IX_DataDicCatalogs_Code",
                table: "DataDicCatalogs",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SimpleDataDics_OrgId_DataDicId",
                table: "SimpleDataDics");

            migrationBuilder.DropIndex(
                name: "IX_DataDicCatalogs_Code",
                table: "DataDicCatalogs");
        }
    }
}
