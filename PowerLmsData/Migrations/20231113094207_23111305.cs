using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111305 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AccountPlOrganizations",
                table: "AccountPlOrganizations");

            migrationBuilder.DropColumn(
                name: "LanguageTag",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OriId",
                table: "AccountPlOrganizations");

            migrationBuilder.AddColumn<string>(
                name: "CurrentLanguageTag",
                table: "Accounts",
                type: "varchar(12)",
                maxLength: 12,
                nullable: true,
                comment: "使用的首选语言标准缩写。如:zh-CN");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "AccountPlOrganizations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "直属组织机构Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountPlOrganizations",
                table: "AccountPlOrganizations",
                columns: new[] { "UserId", "OrgId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AccountPlOrganizations",
                table: "AccountPlOrganizations");

            migrationBuilder.DropColumn(
                name: "CurrentLanguageTag",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "AccountPlOrganizations");

            migrationBuilder.AddColumn<string>(
                name: "LanguageTag",
                table: "Accounts",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OriId",
                table: "AccountPlOrganizations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "所属组织机构Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountPlOrganizations",
                table: "AccountPlOrganizations",
                columns: new[] { "UserId", "OriId" });
        }
    }
}
