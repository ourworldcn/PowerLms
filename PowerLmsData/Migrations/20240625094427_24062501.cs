using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24062501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlInvoicesItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次核销（结算）金额。"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "结算汇率"),
                    RequisitionItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "申请单明细id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlInvoicesItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlInvoicess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IoPingzhengNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "收付凭证号"),
                    IoDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "首付日期"),
                    BankId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "收付银行账号,本公司信息中银行id"),
                    Currency = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "币种。标准货币缩写。"),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "金额。"),
                    JiesuanDanweiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "结算单位Id。客户资料的id."),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "摘要。"),
                    IsYushoufu = table.Column<bool>(type: "bit", nullable: false, comment: "是否预收付。"),
                    Surplus = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "余额。"),
                    FinanceFee = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "财务费用。"),
                    ExchangeLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "汇差损益。外币结算时损失的部分"),
                    Remark2 = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "附加说明。"),
                    BankSerialNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "银行流水号。"),
                    FinanceDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "财务日期。"),
                    ConfirmDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "确认时间。"),
                    ConfirmId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "确认人Id。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlInvoicess", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlInvoicesItems_ParentId",
                table: "PlInvoicesItems",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlInvoicesItems");

            migrationBuilder.DropTable(
                name: "PlInvoicess");
        }
    }
}
