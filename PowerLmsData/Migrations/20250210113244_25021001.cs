using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25021001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Exchange",
                table: "DD_PlExchangeRates",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "兑换率",
                oldClrType: typeof(float),
                oldType: "real",
                oldComment: "兑换率");

            migrationBuilder.CreateTable(
                name: "OwAppLogVO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormatString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParamstersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLogVO", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwAppLogVO");

            migrationBuilder.AlterColumn<float>(
                name: "Exchange",
                table: "DD_PlExchangeRates",
                type: "real",
                nullable: false,
                comment: "兑换率",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "兑换率");
        }
    }
}
