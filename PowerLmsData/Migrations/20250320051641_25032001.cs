using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25032001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "TaxInvoiceInfos");

            migrationBuilder.RenameColumn(
                name: "TaxInvoiceChannelId",
                table: "TaxInvoiceInfos",
                newName: "TaxInvoiceChannelAccountlId");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "TaxInvoiceInfos",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "推送手机号。设置为空则不推送。",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "推送手机号");

            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                table: "TaxInvoiceInfos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                comment: "推送Mail。设置为空则不推送。",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "推送Mail");

            migrationBuilder.CreateTable(
                name: "TaxInvoiceChannelAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "varchar(max)", nullable: true),
                    ParentlId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "渠道Id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxInvoiceChannelAccounts", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxInvoiceChannelAccounts");

            migrationBuilder.RenameColumn(
                name: "TaxInvoiceChannelAccountlId",
                table: "TaxInvoiceInfos",
                newName: "TaxInvoiceChannelId");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "TaxInvoiceInfos",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "推送手机号",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "推送手机号。设置为空则不推送。");

            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                table: "TaxInvoiceInfos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                comment: "推送Mail",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "推送Mail。设置为空则不推送。");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "TaxInvoiceInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构的Id");
        }
    }
}
