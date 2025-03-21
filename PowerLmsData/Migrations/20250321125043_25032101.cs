using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25032101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "TaxInvoiceChannelAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "显示名称。");

            migrationBuilder.AddColumn<Guid>(
                name: "MerchantId",
                table: "TaxInvoiceChannelAccounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: " 商户Id。");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "OrgTaxChannelAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "显示名称。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "TaxInvoiceChannelAccounts");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "TaxInvoiceChannelAccounts");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "OrgTaxChannelAccounts");
        }
    }
}
