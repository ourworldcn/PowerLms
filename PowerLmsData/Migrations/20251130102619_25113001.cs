using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25113001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Multilinguals",
                table: "Multilinguals");

            migrationBuilder.DropIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Multilinguals");

            migrationBuilder.AddColumn<string>(
                name: "EnglishAddress",
                table: "PlCustomers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                comment: "英文地址");

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "PlCustomers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "英文名称");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Multilinguals",
                type: "nvarchar(max)",
                nullable: true,
                comment: "资源内容。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "内容。");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Multilinguals",
                type: "varchar(8)",
                unicode: false,
                maxLength: 8,
                nullable: false,
                comment: "语言的标准缩写名。遵循 IETF BCP 47 标准，如：zh-CN、en-US、ja-JP。",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12,
                oldComment: "主键，也是语言的标准缩写名。");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Multilinguals",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                comment: "资源键。支持分层结构，如：Login.Title、China.Login.Title",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "键值字符串。如:未登录.登录.标题。");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Multilinguals",
                table: "Multilinguals",
                columns: new[] { "Key", "LanguageTag" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Multilinguals",
                table: "Multilinguals");

            migrationBuilder.DropColumn(
                name: "EnglishAddress",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "PlCustomers");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Multilinguals",
                type: "nvarchar(max)",
                nullable: true,
                comment: "内容。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "资源内容。");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Multilinguals",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                comment: "主键，也是语言的标准缩写名。",
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldUnicode: false,
                oldMaxLength: 8,
                oldComment: "语言的标准缩写名。遵循 IETF BCP 47 标准，如：zh-CN、en-US、ja-JP。");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Multilinguals",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "键值字符串。如:未登录.登录.标题。",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldComment: "资源键。支持分层结构，如：Login.Title、China.Login.Title");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Multilinguals",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "主键。")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Multilinguals",
                table: "Multilinguals",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals",
                columns: new[] { "LanguageTag", "Key" },
                unique: true,
                filter: "[Key] IS NOT NULL");
        }
    }
}
