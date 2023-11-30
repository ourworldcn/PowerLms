using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23113003 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CurrencyTypeId",
                table: "DD_FeesTypes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "币种Id",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "币种Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "CurrencyTypeId",
                table: "DD_FeesTypes",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "币种Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "币种Id");
        }
    }
}
