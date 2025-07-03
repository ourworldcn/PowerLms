using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25070301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExportedDateTime",
                table: "TaxInvoiceInfos",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "已导出时间,未导出则为null.");

            migrationBuilder.AddColumn<string>(
                name: "AccountingCategory",
                table: "SubjectConfigurations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "核算类别");

            migrationBuilder.AddColumn<string>(
                name: "VoucherGroup",
                table: "SubjectConfigurations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                comment: "凭证类别字");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportedDateTime",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "AccountingCategory",
                table: "SubjectConfigurations");

            migrationBuilder.DropColumn(
                name: "VoucherGroup",
                table: "SubjectConfigurations");
        }
    }
}
