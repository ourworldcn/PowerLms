using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25021901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IO",
                table: "DocFees",
                type: "bit",
                nullable: false,
                comment: "收入或指出，true收入，false为支出。",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "收入或指出，true支持，false为收入。");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRequestedAmount",
                table: "DocFees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "已经申请的合计金额。计算属性。");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSettledAmount",
                table: "DocFees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "已经结算的金额。计算属性。");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSettledAmount",
                table: "DocFeeRequisitions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "已经结算的金额。计算属性。");

            migrationBuilder.AlterColumn<string>(
                name: "CurrTypeId",
                table: "DocBills",
                type: "nvarchar(max)",
                nullable: true,
                comment: "币种码",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "币种Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalRequestedAmount",
                table: "DocFees");

            migrationBuilder.DropColumn(
                name: "TotalSettledAmount",
                table: "DocFees");

            migrationBuilder.DropColumn(
                name: "TotalSettledAmount",
                table: "DocFeeRequisitions");

            migrationBuilder.AlterColumn<bool>(
                name: "IO",
                table: "DocFees",
                type: "bit",
                nullable: false,
                comment: "收入或指出，true支持，false为收入。",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "收入或指出，true收入，false为支出。");

            migrationBuilder.AlterColumn<string>(
                name: "CurrTypeId",
                table: "DocBills",
                type: "nvarchar(max)",
                nullable: true,
                comment: "币种Id",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "币种码");
        }
    }
}
