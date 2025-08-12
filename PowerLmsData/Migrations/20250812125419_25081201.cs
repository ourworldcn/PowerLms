using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25081201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClosedBy",
                table: "PlJobs",
                type: "uniqueidentifier",
                nullable: true,
                comment: "关闭人Id");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PlInvoicess",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "金额。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "金额。下属结算单明细的合计。");

            migrationBuilder.AlterColumn<string>(
                name: "Contact",
                table: "PlCustomerLoadingAddrs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "联系人",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "所属客户Id");

            migrationBuilder.AddColumn<byte>(
                name: "PortType",
                table: "DD_PlPorts",
                type: "tinyint",
                nullable: true,
                comment: "港口类型。1=空运，2=海运");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "PlJobs");

            migrationBuilder.DropColumn(
                name: "PortType",
                table: "DD_PlPorts");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PlInvoicess",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "金额。下属结算单明细的合计。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "金额。");

            migrationBuilder.AlterColumn<string>(
                name: "Contact",
                table: "PlCustomerLoadingAddrs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "所属客户Id",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "联系人");
        }
    }
}
