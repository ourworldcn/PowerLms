using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23122301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessTypeld",
                table: "DD_JobNumberRules",
                type: "uniqueidentifier",
                nullable: true,
                comment: "业务类型Id，链接到业务大类表");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "AccountPlOrganizations",
                type: "uniqueidentifier",
                nullable: false,
                comment: "直属组织机构Id或商户Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "直属组织机构Id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPlOrganizations_OrgId_UserId",
                table: "AccountPlOrganizations",
                columns: new[] { "OrgId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountPlOrganizations_OrgId_UserId",
                table: "AccountPlOrganizations");

            migrationBuilder.DropColumn(
                name: "BusinessTypeld",
                table: "DD_JobNumberRules");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "AccountPlOrganizations",
                type: "uniqueidentifier",
                nullable: false,
                comment: "直属组织机构Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "直属组织机构Id或商户Id");
        }
    }
}
