using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25041502 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxInclusiveAmount",
                table: "TaxInvoiceInfos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "含税总金额。由关联的TaxInvoiceInfoItem.TaxInclusiveAmount 合计计算得到。");

            migrationBuilder.AddColumn<bool>(
                name: "WithTax",
                table: "TaxInvoiceInfos",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否含税。服务器不使用。");

            migrationBuilder.AlterColumn<string>(
                name: "GoodsName",
                table: "TaxInvoiceInfoItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                comment: "商品名称。必填。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "商品名称");

            migrationBuilder.AddColumn<string>(
                name: "SpecType",
                table: "TaxInvoiceInfoItems",
                type: "nvarchar(max)",
                nullable: true,
                comment: "规格型号,可选");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxInclusiveAmount",
                table: "TaxInvoiceInfoItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "含税金额。计算公式：税额 = 单价 * 数量 * 税率。计算结果保留两位小数。");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TaxInvoiceInfoItems",
                type: "nvarchar(max)",
                nullable: true,
                comment: "单位,可选");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxInclusiveAmount",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "WithTax",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "SpecType",
                table: "TaxInvoiceInfoItems");

            migrationBuilder.DropColumn(
                name: "TaxInclusiveAmount",
                table: "TaxInvoiceInfoItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TaxInvoiceInfoItems");

            migrationBuilder.AlterColumn<string>(
                name: "GoodsName",
                table: "TaxInvoiceInfoItems",
                type: "nvarchar(max)",
                nullable: true,
                comment: "商品名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "商品名称。必填。");
        }
    }
}
