using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25042701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailReason",
                table: "TaxInvoiceInfos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                comment: "开票失败原因");

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "TaxInvoiceInfos",
                type: "DATETIME2(3)",
                nullable: true,
                comment: "开票日期");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceTypeCode",
                table: "TaxInvoiceInfos",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "发票类型代码");

            migrationBuilder.AddColumn<string>(
                name: "PdfUrl",
                table: "TaxInvoiceInfos",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                comment: "PDF文件下载地址");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailReason",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "InvoiceTypeCode",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "PdfUrl",
                table: "TaxInvoiceInfos");
        }
    }
}
