using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23121801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShortName",
                table: "PlCustomers",
                newName: "Name_ShortName");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "PlCustomers",
                newName: "Name_Name");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "PlCustomers",
                newName: "Name_DisplayName");

            migrationBuilder.AlterColumn<string>(
                name: "Name_Name",
                table: "PlOrganizations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "正式名称，拥有相对稳定性",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "正式名称，拥有相对稳定性");

            migrationBuilder.AlterColumn<string>(
                name: "Name_ShortName",
                table: "PlCustomers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "正式简称，对正式的组织机构通常简称也是规定的",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "简称");

            migrationBuilder.AlterColumn<string>(
                name: "Name_Name",
                table: "PlCustomers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "正式名称，拥有相对稳定性",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "正式名");

            migrationBuilder.AlterColumn<string>(
                name: "Name_DisplayName",
                table: "PlCustomers",
                type: "nvarchar(max)",
                nullable: true,
                comment: "显示名，有时它是昵称或简称(系统内)的意思",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "显示名");

            migrationBuilder.AlterColumn<string>(
                name: "Name_Name",
                table: "Merchants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "正式名称，拥有相对稳定性",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "正式名称，拥有相对稳定性");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name_ShortName",
                table: "PlCustomers",
                newName: "ShortName");

            migrationBuilder.RenameColumn(
                name: "Name_Name",
                table: "PlCustomers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Name_DisplayName",
                table: "PlCustomers",
                newName: "DisplayName");

            migrationBuilder.AlterColumn<string>(
                name: "Name_Name",
                table: "PlOrganizations",
                type: "nvarchar(max)",
                nullable: true,
                comment: "正式名称，拥有相对稳定性",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "正式名称，拥有相对稳定性");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "PlCustomers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "简称",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "正式简称，对正式的组织机构通常简称也是规定的");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PlCustomers",
                type: "nvarchar(max)",
                nullable: true,
                comment: "正式名",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "正式名称，拥有相对稳定性");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "PlCustomers",
                type: "nvarchar(max)",
                nullable: true,
                comment: "显示名",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "显示名，有时它是昵称或简称(系统内)的意思");

            migrationBuilder.AlterColumn<string>(
                name: "Name_Name",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true,
                comment: "正式名称，拥有相对稳定性",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "正式名称，拥有相对稳定性");
        }
    }
}
