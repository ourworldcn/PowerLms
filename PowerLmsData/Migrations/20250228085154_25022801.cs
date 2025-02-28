using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25022801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceChannel",
                table: "PlCustomerTaxInfos");

            migrationBuilder.DropColumn(
                name: "InvoiceChannelParams",
                table: "PlCustomerTaxInfos");

            migrationBuilder.CreateTable(
                name: "TaxInvoiceChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构的Id。"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名称"),
                    InvoiceChannel = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "开票渠道。仅仅是一个标记，服务器通过改标识来决定调用什么接口。"),
                    InvoiceChannelParams = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true, comment: "开票渠道参数。Json格式的字符串。包含敏感信息。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInvoiceChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxInvoiceInfoItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "客户税务信息/开票信息Id。"),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "商品名称"),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "数量"),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "单价（不含税）"),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "税率")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInvoiceInfoItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxInvoiceInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxInvoiceChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAuto = table.Column<bool>(type: "bit", nullable: false, comment: "自动或手动。true=自动，false=手动。"),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构的Id"),
                    DocFeeRequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "费用申请单Id"),
                    SendTime = table.Column<DateTime>(type: "DATETIME2(3)", nullable: true, comment: "发送时间"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "发票号"),
                    ReturnInvoiceTime = table.Column<DateTime>(type: "DATETIME2(3)", nullable: true, comment: "返回发票号时间"),
                    Mobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "推送手机号"),
                    Mail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "推送Mail"),
                    InvoiceItemName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "开票项目名（产品）"),
                    SellerInvoiceData = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "销方开票数据"),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "备注"),
                    InvoiceSerialNum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "发票流水号"),
                    AuditDateTime = table.Column<DateTime>(type: "DATETIME2(3)", nullable: true, comment: "审核时间"),
                    AuditorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "审核人Id"),
                    SellerTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "销方抬头"),
                    SellerTaxNum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "销方税号"),
                    SellerBank = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "销方开户行"),
                    SellerAccount = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "销方账号"),
                    SellerAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "销方地址"),
                    SellerTel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "销方电话"),
                    BuyerTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "购方抬头"),
                    BuyerTaxNum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "购方税号"),
                    BuyerBank = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "购方开户行"),
                    BuyerAccount = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "购方账号"),
                    BuyerAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "购方地址"),
                    BuyerTel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "购方电话")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInvoiceInfos", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxInvoiceChannels");

            migrationBuilder.DropTable(
                name: "TaxInvoiceInfoItems");

            migrationBuilder.DropTable(
                name: "TaxInvoiceInfos");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceChannel",
                table: "PlCustomerTaxInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "开票渠道。仅仅是一个标记，服务器通过改标识来决定调用什么接口。");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceChannelParams",
                table: "PlCustomerTaxInfos",
                type: "varchar(max)",
                unicode: false,
                nullable: true,
                comment: "开票渠道参数。Json格式的字符串。包含敏感信息。");
        }
    }
}
