using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24070201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OperatingDateTime",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "操作时间。");

            migrationBuilder.AddColumn<Guid>(
                name: "OperatorId",
                table: "PlJobs",
                type: "uniqueidentifier",
                nullable: true,
                comment: "操作人Id。");

            migrationBuilder.AddColumn<Guid>(
                name: "CreateBy",
                table: "PlInvoicess",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者的唯一标识。");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlInvoicess",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "创建的时间。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatingDateTime",
                table: "PlJobs");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                table: "PlJobs");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "PlInvoicess");
        }
    }
}
