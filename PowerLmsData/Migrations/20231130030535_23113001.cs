using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23113001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "DD_PlPorts");

            migrationBuilder.AddColumn<Guid>(
                name: "CountryId",
                table: "DD_PlPorts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "国家Id。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "DD_PlPorts");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "DD_PlPorts",
                type: "nvarchar(max)",
                nullable: true,
                comment: "国家");
        }
    }
}
