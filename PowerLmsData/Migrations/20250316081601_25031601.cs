using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25031601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAuto",
                table: "TaxInvoiceInfos");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicantId",
                table: "TaxInvoiceInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "申请人Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplyDateTime",
                table: "TaxInvoiceInfos",
                type: "DATETIME2(3)",
                nullable: true,
                comment: "申请时间");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceType",
                table: "TaxInvoiceInfos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "发票类型");

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "TaxInvoiceInfos",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.UpdateData(
                table: "OwAppLogStores",
                keyColumn: "Id",
                keyValue: new Guid("e410bc88-71b2-4530-9993-c0c0b1105617"),
                column: "FormatString",
                value: "用户:{LoginName}({CompanyName}){OperationType}成功");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicantId",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "ApplyDateTime",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "InvoiceType",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TaxInvoiceInfos");

            migrationBuilder.AddColumn<bool>(
                name: "IsAuto",
                table: "TaxInvoiceInfos",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "自动或手动。true=自动，false=手动。");

            migrationBuilder.UpdateData(
                table: "OwAppLogStores",
                keyColumn: "Id",
                keyValue: new Guid("e410bc88-71b2-4530-9993-c0c0b1105617"),
                column: "FormatString",
                value: "用户:{LoginName}({CompanyName}){OperatorName}成功");
        }
    }
}
