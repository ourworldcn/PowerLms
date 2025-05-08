using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25050801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChargeWeight",
                table: "DocBills",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                comment: "计费重量，单位Kg，3位小数。");

            migrationBuilder.AddColumn<Guid>(
                name: "PackTypeId",
                table: "DocBills",
                type: "uniqueidentifier",
                nullable: true,
                comment: "包装类型Id。关联简单字典PackType。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeWeight",
                table: "DocBills");

            migrationBuilder.DropColumn(
                name: "PackTypeId",
                table: "DocBills");
        }
    }
}
