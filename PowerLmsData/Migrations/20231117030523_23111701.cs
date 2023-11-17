using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23111701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortcutName",
                table: "PlOrganizations");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "PlOrganizations",
                newName: "Name_Name");

            migrationBuilder.AlterColumn<string>(
                name: "Name_Name",
                table: "PlOrganizations",
                type: "nvarchar(max)",
                nullable: true,
                comment: "正式名称，拥有相对稳定性",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "组织机构名称");

            migrationBuilder.AddColumn<Guid>(
                name: "MerchantId",
                table: "PlOrganizations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name_DisplayName",
                table: "PlOrganizations",
                type: "nvarchar(max)",
                nullable: true,
                comment: "显示名，有时它是昵称或简称(系统内)的意思");

            migrationBuilder.AddColumn<string>(
                name: "Name_ShortName",
                table: "PlOrganizations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "正式简称，对正式的组织机构通常简称也是规定的");

            migrationBuilder.AddColumn<string>(
                name: "ShortcutCode",
                table: "PlOrganizations",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "Name_DisplayName",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "Name_ShortName",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "ShortcutCode",
                table: "PlOrganizations");

            migrationBuilder.RenameColumn(
                name: "Name_Name",
                table: "PlOrganizations",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PlOrganizations",
                type: "nvarchar(max)",
                nullable: true,
                comment: "组织机构名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "正式名称，拥有相对稳定性");

            migrationBuilder.AddColumn<string>(
                name: "ShortcutName",
                table: "PlOrganizations",
                type: "nvarchar(max)",
                nullable: true,
                comment: "机构编码");
        }
    }
}
