using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25111901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DocFees",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DocFeeRequisitionItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DocFees");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DocFeeRequisitionItems");
        }
    }
}
