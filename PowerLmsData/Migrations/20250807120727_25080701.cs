using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25080701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualReceivedAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "实收金额，2位小数");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualReceivedBaseCurrencyAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "实收金额本位币金额，2位小数，实收金额（主币种）*收付汇率");

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceOffsetReceivableAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "预收/付冲应收金额，2位小数");

            migrationBuilder.AddColumn<decimal>(
                name: "AdvancePaymentAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "预收/付金额金额，2位小数，收付金额-核销金额（主币种）");

            migrationBuilder.AddColumn<decimal>(
                name: "AdvancePaymentBaseCurrencyAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "预收/付金额本位币金额，2位小数，预收金额（主币种）*收付汇率");

            migrationBuilder.AddColumn<decimal>(
                name: "AdvancePaymentFromPreviousAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "预收/付款金额，2位小数，从以前的预收中获取");

            migrationBuilder.AddColumn<string>(
                name: "FinancialInformation",
                table: "PlInvoicess",
                type: "nvarchar(max)",
                nullable: true,
                comment: "财务信息，string类型");

            migrationBuilder.AddColumn<bool>(
                name: "FinancialPaymentConfirmed",
                table: "PlInvoicess",
                type: "bit",
                nullable: true,
                comment: "财务支付确认，对账需要");

            migrationBuilder.AddColumn<string>(
                name: "FinancialVoucherNumber",
                table: "PlInvoicess",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "财务凭证号，支付账号关联的凭证字自动生成");

            migrationBuilder.AddColumn<bool>(
                name: "IsExportToFinancialSoftware",
                table: "PlInvoicess",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否导出到财务软件，true允许导出，默认true");

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentExchangeRate",
                table: "PlInvoicess",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                comment: "收/付汇率（主汇率），4位小数，收付金额对应的汇率");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "PlInvoicess",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "支付方法，简单字典ApplyType");

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentTotalBaseCurrencyAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "收/付款合计本位币金额，2位小数，收付金额*收付汇率");

            migrationBuilder.AddColumn<Guid>(
                name: "RefundUnitId",
                table: "PlInvoicess",
                type: "uniqueidentifier",
                nullable: true,
                comment: "回款单位，选择客户资料中的结算单位");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFeeAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "手续费金额，2位小数，收付金额-实收金额");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFeeBaseCurrencyAmount",
                table: "PlInvoicess",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                comment: "手续费本位币金额，2位小数，手续费（主币种）*收付汇率");

            migrationBuilder.CreateTable(
                name: "ActualFinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "挂靠的父单据Id，通用设计，当前主要关联到结算单"),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "收付款日期，实际发生收付款的业务日期，精确到毫秒"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "实收付金额，本次实际收付的金额，2位小数精度"),
                    ServiceFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "手续费，本次收付产生的手续费，2位小数精度"),
                    BankFlowNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "银行流水号(水单号)，银行转账的流水号，用于对账和确认"),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "结算账号Id，本公司信息中的银行账号ID，关联到BankInfo表"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注，记录收付款的备注信息"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者Id，记录创建这条收付记录的操作员"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建时间，记录创建的时间，精确到毫秒"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualFinancialTransactions", x => x.Id);
                },
                comment: "实际收付记录表");

            migrationBuilder.CreateIndex(
                name: "IX_ActualFinancialTransactions_ParentId",
                table: "ActualFinancialTransactions",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActualFinancialTransactions");

            migrationBuilder.DropColumn(
                name: "ActualReceivedAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "ActualReceivedBaseCurrencyAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "AdvanceOffsetReceivableAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "AdvancePaymentAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "AdvancePaymentBaseCurrencyAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "AdvancePaymentFromPreviousAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "FinancialInformation",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "FinancialPaymentConfirmed",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "FinancialVoucherNumber",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "IsExportToFinancialSoftware",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "PaymentExchangeRate",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "PaymentTotalBaseCurrencyAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "RefundUnitId",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "ServiceFeeAmount",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "ServiceFeeBaseCurrencyAmount",
                table: "PlInvoicess");
        }
    }
}
