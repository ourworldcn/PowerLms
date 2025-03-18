using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25031801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "TaxInvoiceInfos");

            migrationBuilder.AlterColumn<Guid>(
                name: "TaxInvoiceChannelId",
                table: "TaxInvoiceInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "开票渠道Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "State",
                table: "TaxInvoiceInfos",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "发票状态。0：创建后待审核；1：已审核开票中；2：已开票");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "TaxInvoiceInfoItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "客户税务信息/开票信息Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "客户税务信息/开票信息Id。");

            migrationBuilder.AddColumn<Guid>(
                name: "TaxInvoiceId",
                table: "DocFeeRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "关联的发票Id，冗余属性");

            migrationBuilder.CreateTable(
                name: "OrgTaxChannelAccounts",
                columns: table => new
                {
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "机构Id"),
                    ChannelAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "渠道账号Id"),
                    InvoiceHeader = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "作为销方时发票抬头"),
                    TaxpayerNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "税务登记号（纳税人识别号）"),
                    Address = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "地址"),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "电话"),
                    BankName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "开户银行名称"),
                    BankAccount = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "银行账号"),
                    Email = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "电子邮件"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, comment: "是否为该机构的默认销方信息"),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "备注说明"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建时间(UTC)"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建人Id"),
                    LastModifyBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "最后修改人Id"),
                    LastModifyUtc = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "最后修改时间(UTC)。"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgTaxChannelAccounts", x => new { x.OrgId, x.ChannelAccountId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgTaxChannelAccounts");

            migrationBuilder.DropColumn(
                name: "State",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "TaxInvoiceId",
                table: "DocFeeRequisitions");

            migrationBuilder.AlterColumn<Guid>(
                name: "TaxInvoiceChannelId",
                table: "TaxInvoiceInfos",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "开票渠道Id");

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "TaxInvoiceInfos",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "TaxInvoiceInfoItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "客户税务信息/开票信息Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "客户税务信息/开票信息Id");
        }
    }
}
