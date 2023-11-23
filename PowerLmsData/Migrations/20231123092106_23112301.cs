using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataDicCatalogs_Code",
                table: "DataDicCatalogs");

            migrationBuilder.CreateIndex(
                name: "IX_DataDicCatalogs_OrgId_Code",
                table: "DataDicCatalogs",
                columns: new[] { "OrgId", "Code" },
                unique: true,
                filter: "[OrgId] IS NOT NULL AND [Code] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataDicCatalogs_OrgId_Code",
                table: "DataDicCatalogs");

            migrationBuilder.CreateIndex(
                name: "IX_DataDicCatalogs_Code",
                table: "DataDicCatalogs",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");
        }
    }
}
