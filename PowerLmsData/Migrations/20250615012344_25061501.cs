using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25061501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SendTime",
                table: "TaxInvoiceInfos",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "发送时间",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME2(3)",
                oldNullable: true,
                oldComment: "发送时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReturnInvoiceTime",
                table: "TaxInvoiceInfos",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "返回发票号时间",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME2(3)",
                oldNullable: true,
                oldComment: "返回发票号时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "InvoiceDate",
                table: "TaxInvoiceInfos",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "开票日期",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME2(3)",
                oldNullable: true,
                oldComment: "开票日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "TaxInvoiceInfos",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "审核时间",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME2(3)",
                oldNullable: true,
                oldComment: "审核时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplyDateTime",
                table: "TaxInvoiceInfos",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "申请时间",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME2(3)",
                oldNullable: true,
                oldComment: "申请时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDateTime",
                table: "ShippingLanes",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "生效日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "生效日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDateTime",
                table: "ShippingLanes",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "终止日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "终止日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "审核日期,未审核则为空",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "审核日期,未审核则为空");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WorldDateTime",
                table: "OwSystemLogs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "这个行为发生的世界时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "这个行为发生的世界时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PreclearDate",
                table: "DocFees",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "预计结算日期，客户资料中信用日期自动计算出。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "预计结算日期，客户资料中信用日期自动计算出。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocFees",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建时间，系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "创建时间，系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "DocFees",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "审核日期，为空则未审核。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "审核日期，为空则未审核。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReturnDate",
                table: "DocFeeRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "实际回款时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "实际回款时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PreReturnDate",
                table: "DocFeeRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "预计回款时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "预计回款时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "MakeDateTime",
                table: "DocFeeRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "制单时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "制单时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "IODate",
                table: "DocBills",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "审核日期，为空则未审核",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "审核日期，为空则未审核");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Etd",
                table: "DocBills",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "开航日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "开航日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Eta",
                table: "DocBills",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "到港日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "到港日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocBills",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CheckDate",
                table: "DocBills",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "审核日期，为空则未审核",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "审核日期，为空则未审核");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SendTime",
                table: "TaxInvoiceInfos",
                type: "DATETIME2(3)",
                nullable: true,
                comment: "发送时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "发送时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReturnInvoiceTime",
                table: "TaxInvoiceInfos",
                type: "DATETIME2(3)",
                nullable: true,
                comment: "返回发票号时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "返回发票号时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "InvoiceDate",
                table: "TaxInvoiceInfos",
                type: "DATETIME2(3)",
                nullable: true,
                comment: "开票日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "开票日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "TaxInvoiceInfos",
                type: "DATETIME2(3)",
                nullable: true,
                comment: "审核时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplyDateTime",
                table: "TaxInvoiceInfos",
                type: "DATETIME2(3)",
                nullable: true,
                comment: "申请时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "申请时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDateTime",
                table: "ShippingLanes",
                type: "datetime2(2)",
                nullable: false,
                comment: "生效日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "生效日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDateTime",
                table: "ShippingLanes",
                type: "datetime2(2)",
                nullable: false,
                comment: "终止日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "终止日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "PlJobs",
                type: "datetime2(2)",
                nullable: true,
                comment: "审核日期,未审核则为空",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核日期,未审核则为空");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WorldDateTime",
                table: "OwSystemLogs",
                type: "datetime2",
                nullable: false,
                comment: "这个行为发生的世界时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "这个行为发生的世界时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PreclearDate",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: false,
                comment: "预计结算日期，客户资料中信用日期自动计算出。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "预计结算日期，客户资料中信用日期自动计算出。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: false,
                comment: "创建时间，系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建时间，系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: true,
                comment: "审核日期，为空则未审核。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核日期，为空则未审核。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReturnDate",
                table: "DocFeeRequisitions",
                type: "datetime2(2)",
                nullable: true,
                comment: "实际回款时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "实际回款时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PreReturnDate",
                table: "DocFeeRequisitions",
                type: "datetime2(2)",
                nullable: true,
                comment: "预计回款时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "预计回款时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "MakeDateTime",
                table: "DocFeeRequisitions",
                type: "datetime2(2)",
                nullable: true,
                comment: "制单时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "制单时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "IODate",
                table: "DocBills",
                type: "datetime2(2)",
                nullable: true,
                comment: "审核日期，为空则未审核",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核日期，为空则未审核");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Etd",
                table: "DocBills",
                type: "datetime2(2)",
                nullable: false,
                comment: "开航日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "开航日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Eta",
                table: "DocBills",
                type: "datetime2(2)",
                nullable: false,
                comment: "到港日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "到港日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocBills",
                type: "datetime2(2)",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CheckDate",
                table: "DocBills",
                type: "datetime2(2)",
                nullable: true,
                comment: "审核日期，为空则未审核",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核日期，为空则未审核");
        }
    }
}
