using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23110801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "LanguageDataDics",
                comment: "语言字典，参见 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c。");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Multilinguals",
                type: "nvarchar(max)",
                nullable: true,
                comment: "内容。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Multilinguals",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                comment: "主键，也是语言的标准缩写名。",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Multilinguals",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "键值字符串。如:未登录.登录.标题。",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Multilinguals",
                type: "uniqueidentifier",
                nullable: false,
                comment: "主键。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<int>(
                name: "Lcid",
                table: "LanguageDataDics",
                type: "int",
                nullable: false,
                comment: "语言Id。",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "LanguageDataDics",
                type: "nvarchar(max)",
                nullable: true,
                comment: "语言名。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "LanguageDataDics",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                comment: "主键，也是语言的标准缩写名。",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AlterColumn<byte[]>(
                name: "PwdHash",
                table: "Accounts",
                type: "varbinary(32)",
                maxLength: 32,
                nullable: true,
                comment: "密码的Hash值",
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifyDateTimeUtc",
                table: "Accounts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "Timestamp",
                table: "Accounts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Token",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModifyDateTimeUtc",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "Accounts");

            migrationBuilder.AlterTable(
                name: "LanguageDataDics",
                oldComment: "语言字典，参见 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c。");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Multilinguals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "内容。");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Multilinguals",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12,
                oldComment: "主键，也是语言的标准缩写名。");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Multilinguals",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "键值字符串。如:未登录.登录.标题。");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Multilinguals",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "主键。");

            migrationBuilder.AlterColumn<int>(
                name: "Lcid",
                table: "LanguageDataDics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "语言Id。");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "LanguageDataDics",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "语言名。");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "LanguageDataDics",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12,
                oldComment: "主键，也是语言的标准缩写名。");

            migrationBuilder.AlterColumn<byte[]>(
                name: "PwdHash",
                table: "Accounts",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "密码的Hash值");
        }
    }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
