using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25121501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExportedUserId",
                table: "TaxInvoiceInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "导出用户ID，用于审计和权限验证");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedDateTime",
                table: "PlInvoicess",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "导出时间，null表示未导出");

            migrationBuilder.AddColumn<Guid>(
                name: "ExportedUserId",
                table: "PlInvoicess",
                type: "uniqueidentifier",
                nullable: true,
                comment: "导出用户ID，用于审计和权限验证");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "导出时间，null表示未导出");

            migrationBuilder.AddColumn<Guid>(
                name: "ExportedUserId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "导出用户ID，用于审计和权限验证");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OaExpenseRequisitions",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedDateTime",
                table: "OaExpenseRequisitionItems",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "导出时间，null表示未导出");

            migrationBuilder.AddColumn<Guid>(
                name: "ExportedUserId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "导出用户ID，用于审计和权限验证");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OaExpenseRequisitionItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedDateTime",
                table: "DocFees",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "导出时间，null表示未导出，非null表示已导出ARAB或APAB");

            migrationBuilder.AddColumn<Guid>(
                name: "ExportedUserId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "导出用户ID，用于审计和权限验证");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportedUserId",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "ExportedDateTime",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "ExportedUserId",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "ExportedDateTime",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "ExportedUserId",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "ExportedDateTime",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "ExportedUserId",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "ExportedDateTime",
                table: "DocFees");

            migrationBuilder.DropColumn(
                name: "ExportedUserId",
                table: "DocFees");
        }
    }
}
