using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _24011901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BaseCurrencyId",
                table: "PlOrganizations",
                type: "uniqueidentifier",
                nullable: true,
                comment: "本位币Id");

            migrationBuilder.AddColumn<string>(
                name: "CustomCode",
                table: "PlOrganizations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "海关编码。");

            migrationBuilder.AddColumn<string>(
                name: "LegalRepresentative",
                table: "PlOrganizations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "法人代表");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "PlOrganizations",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注");

            migrationBuilder.AddColumn<string>(
                name: "UnknowNumber",
                table: "PlOrganizations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "工商登记号码（信用证号）。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseCurrencyId",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "CustomCode",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "LegalRepresentative",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "UnknowNumber",
                table: "PlOrganizations");
        }
    }
}
