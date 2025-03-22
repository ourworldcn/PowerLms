using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25032201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrgTaxChannelAccounts",
                table: "OrgTaxChannelAccounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChannelAccountId",
                table: "OrgTaxChannelAccounts",
                type: "uniqueidentifier",
                nullable: false,
                comment: "渠道账号Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "渠道账号Id")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "OrgTaxChannelAccounts",
                type: "uniqueidentifier",
                nullable: false,
                comment: "机构Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "机构Id")
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "OrgTaxChannelAccounts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrgTaxChannelAccounts",
                table: "OrgTaxChannelAccounts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrgTaxChannelAccounts_OrgId_ChannelAccountId",
                table: "OrgTaxChannelAccounts",
                columns: new[] { "OrgId", "ChannelAccountId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrgTaxChannelAccounts",
                table: "OrgTaxChannelAccounts");

            migrationBuilder.DropIndex(
                name: "IX_OrgTaxChannelAccounts_OrgId_ChannelAccountId",
                table: "OrgTaxChannelAccounts");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "OrgTaxChannelAccounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "OrgTaxChannelAccounts",
                type: "uniqueidentifier",
                nullable: false,
                comment: "机构Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "机构Id")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ChannelAccountId",
                table: "OrgTaxChannelAccounts",
                type: "uniqueidentifier",
                nullable: false,
                comment: "渠道账号Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "渠道账号Id")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrgTaxChannelAccounts",
                table: "OrgTaxChannelAccounts",
                columns: new[] { "OrgId", "ChannelAccountId" });
        }
    }
}
