using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _250112401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstNodeId",
                table: "OwWfs");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaxInvoiceInfos",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlOrganizations",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlJobs",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlInvoicess",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlInvoicesItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlCustomers",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "并发控制行版本号");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlCustomerContacts",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "并发控制行版本号");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OwWfs",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "并发控制版本号");

            migrationBuilder.AlterColumn<string>(
                name: "OpertorDisplayName",
                table: "OwWfNodeItems",
                type: "nvarchar(max)",
                nullable: true,
                comment: "操作人的显示名称快照。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "这里冗余额外记录一个操作人的显示名称。可随时更改。");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OwWfNodeItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "并发控制版本号");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Merchants",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DocFeeRequisitions",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaxInvoiceInfos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlJobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlInvoicess");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlInvoicesItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlCustomerContacts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OwWfs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OwWfNodeItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DocFeeRequisitions");

            migrationBuilder.AddColumn<Guid>(
                name: "FirstNodeId",
                table: "OwWfs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OpertorDisplayName",
                table: "OwWfNodeItems",
                type: "nvarchar(max)",
                nullable: true,
                comment: "这里冗余额外记录一个操作人的显示名称。可随时更改。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "操作人的显示名称快照。");
        }
    }
}
