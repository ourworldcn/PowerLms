using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112403 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "DD_SimpleDataDics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "显示的名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "显示的名称");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "DD_SimpleDataDics",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "DD_BusinessTypeDataDics",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "显示的名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "显示的名称");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remark",
                table: "DD_SimpleDataDics");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "DD_SimpleDataDics",
                type: "nvarchar(max)",
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
                type: "nvarchar(max)",
                nullable: true,
                comment: "显示的名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "显示的名称");
        }
    }
}
