using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25041503 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CountryId",
                table: "DD_PlPorts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "国家Id。建议使用CountryCode属性替代。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "国家Id。");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "DD_PlPorts",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true,
                comment: "国家代码。使用标准的国家代码。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "DD_PlPorts");

            migrationBuilder.AlterColumn<Guid>(
                name: "CountryId",
                table: "DD_PlPorts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "国家Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "国家Id。建议使用CountryCode属性替代。");
        }
    }
}
