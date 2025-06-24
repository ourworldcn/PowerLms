using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25062401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndData",
                table: "DD_PlExchangeRates",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                comment: "失效时点",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "失效时点");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BeginDate",
                table: "DD_PlExchangeRates",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                comment: "生效时点",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "生效时点");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndData",
                table: "DD_PlExchangeRates",
                type: "datetime2",
                nullable: false,
                comment: "失效时点",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldPrecision: 0,
                oldComment: "失效时点");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BeginDate",
                table: "DD_PlExchangeRates",
                type: "datetime2",
                nullable: false,
                comment: "生效时点",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(0)",
                oldPrecision: 0,
                oldComment: "生效时点");
        }
    }
}
