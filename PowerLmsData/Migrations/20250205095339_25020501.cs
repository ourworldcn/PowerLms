using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25020501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "PlInvoicesItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "结算汇率，用户手工填写。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "结算汇率");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFeeRequisitions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "金额,所有子项的金额的求和（需要换算币种）。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "金额,所有子项的金额的求和。");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFeeRequisitionItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "本次申请金额",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "本次申请金额");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocBills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "金额。冗余字段，所属费用的合计。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "金额");

            migrationBuilder.CreateTable(
                name: "OwAppLoggerItemStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParamstersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLoggerItemStores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwAppLoggerStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormatString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLoggerStores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwAppLoggerItemStores_CreateUtc",
                table: "OwAppLoggerItemStores",
                column: "CreateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OwAppLoggerItemStores_ParentId",
                table: "OwAppLoggerItemStores",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwAppLoggerItemStores");

            migrationBuilder.DropTable(
                name: "OwAppLoggerStores");

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

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "PlInvoicesItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "结算汇率",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "结算汇率，用户手工填写。");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFeeRequisitions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "金额,所有子项的金额的求和。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "金额,所有子项的金额的求和（需要换算币种）。");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFeeRequisitionItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "本次申请金额",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "本次申请金额");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocBills",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "金额",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "金额。冗余字段，所属费用的合计。");
        }
    }
}
