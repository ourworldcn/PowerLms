using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112402 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SystemResources",
                table: "SystemResources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SimpleDataDics",
                table: "SimpleDataDics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataDicCatalogs",
                table: "DataDicCatalogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessTypeDataDics",
                table: "BusinessTypeDataDics");

            migrationBuilder.RenameTable(
                name: "SystemResources",
                newName: "DD_SystemResources");

            migrationBuilder.RenameTable(
                name: "SimpleDataDics",
                newName: "DD_SimpleDataDics");

            migrationBuilder.RenameTable(
                name: "DataDicCatalogs",
                newName: "DD_DataDicCatalogs");

            migrationBuilder.RenameTable(
                name: "BusinessTypeDataDics",
                newName: "DD_BusinessTypeDataDics");

            migrationBuilder.RenameIndex(
                name: "IX_SystemResources_Name",
                table: "DD_SystemResources",
                newName: "IX_DD_SystemResources_Name");

            migrationBuilder.RenameIndex(
                name: "IX_SimpleDataDics_DataDicId",
                table: "DD_SimpleDataDics",
                newName: "IX_DD_SimpleDataDics_DataDicId");

            migrationBuilder.RenameIndex(
                name: "IX_DataDicCatalogs_OrgId_Code",
                table: "DD_DataDicCatalogs",
                newName: "IX_DD_DataDicCatalogs_OrgId_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DD_SystemResources",
                table: "DD_SystemResources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DD_SimpleDataDics",
                table: "DD_SimpleDataDics",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DD_DataDicCatalogs",
                table: "DD_DataDicCatalogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DD_BusinessTypeDataDics",
                table: "DD_BusinessTypeDataDics",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DD_SystemResources",
                table: "DD_SystemResources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DD_SimpleDataDics",
                table: "DD_SimpleDataDics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DD_DataDicCatalogs",
                table: "DD_DataDicCatalogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DD_BusinessTypeDataDics",
                table: "DD_BusinessTypeDataDics");

            migrationBuilder.RenameTable(
                name: "DD_SystemResources",
                newName: "SystemResources");

            migrationBuilder.RenameTable(
                name: "DD_SimpleDataDics",
                newName: "SimpleDataDics");

            migrationBuilder.RenameTable(
                name: "DD_DataDicCatalogs",
                newName: "DataDicCatalogs");

            migrationBuilder.RenameTable(
                name: "DD_BusinessTypeDataDics",
                newName: "BusinessTypeDataDics");

            migrationBuilder.RenameIndex(
                name: "IX_DD_SystemResources_Name",
                table: "SystemResources",
                newName: "IX_SystemResources_Name");

            migrationBuilder.RenameIndex(
                name: "IX_DD_SimpleDataDics_DataDicId",
                table: "SimpleDataDics",
                newName: "IX_SimpleDataDics_DataDicId");

            migrationBuilder.RenameIndex(
                name: "IX_DD_DataDicCatalogs_OrgId_Code",
                table: "DataDicCatalogs",
                newName: "IX_DataDicCatalogs_OrgId_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SystemResources",
                table: "SystemResources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SimpleDataDics",
                table: "SimpleDataDics",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataDicCatalogs",
                table: "DataDicCatalogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessTypeDataDics",
                table: "BusinessTypeDataDics",
                column: "Id");
        }
    }
}
