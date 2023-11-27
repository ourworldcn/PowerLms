using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DD_SimpleDataDics_DataDicId",
                table: "DD_SimpleDataDics");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "DD_SimpleDataDics",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "显示的名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "显示的名称");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "DD_BusinessTypeDataDics",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "显示的名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "显示的名称");

            migrationBuilder.CreateIndex(
                name: "IX_DD_SimpleDataDics_DataDicId_Code",
                table: "DD_SimpleDataDics",
                columns: new[] { "DataDicId", "Code" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DD_SimpleDataDics_DataDicId_Code",
                table: "DD_SimpleDataDics");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "DD_SimpleDataDics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "显示的名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "显示的名称");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "DD_BusinessTypeDataDics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "显示的名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "显示的名称");

            migrationBuilder.CreateIndex(
                name: "IX_DD_SimpleDataDics_DataDicId",
                table: "DD_SimpleDataDics",
                column: "DataDicId");
        }
    }
}
