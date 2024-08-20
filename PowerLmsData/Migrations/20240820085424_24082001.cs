using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24082001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifyDate",
                table: "PlJobs");

            migrationBuilder.RenameColumn(
                name: "CheckDate",
                table: "DocFees",
                newName: "AuditDateTime");

            migrationBuilder.RenameColumn(
                name: "ChechManId",
                table: "DocFees",
                newName: "AuditOperatorId");

            migrationBuilder.AddColumn<DateTime>(
                name: "AuditDateTime",
                table: "PlJobs",
                type: "datetime2(2)",
                nullable: true,
                comment: "审核日期,未审核则为空");

            migrationBuilder.AddColumn<Guid>(
                name: "AuditOperatorId",
                table: "PlJobs",
                type: "uniqueidentifier",
                nullable: true,
                comment: "审核人Id，为空则未审核");

            migrationBuilder.AlterColumn<string>(
                name: "ActionId",
                table: "OwSystemLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "行为Id。如操作名.实体名.Id",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "行为Id。如Logined , ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditDateTime",
                table: "PlJobs");

            migrationBuilder.DropColumn(
                name: "AuditOperatorId",
                table: "PlJobs");

            migrationBuilder.RenameColumn(
                name: "AuditOperatorId",
                table: "DocFees",
                newName: "ChechManId");

            migrationBuilder.RenameColumn(
                name: "AuditDateTime",
                table: "DocFees",
                newName: "CheckDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifyDate",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "审核日期");

            migrationBuilder.AlterColumn<string>(
                name: "ActionId",
                table: "OwSystemLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "行为Id。如Logined , ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==。",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "行为Id。如操作名.实体名.Id");
        }
    }
}
