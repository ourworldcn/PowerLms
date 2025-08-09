using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24011501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlOrganizations_PlPermissions_PlPermissionName",
                table: "PlOrganizations");

            migrationBuilder.DropIndex(
                name: "IX_PlOrganizations_PlPermissionName",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "PlPermissionName",
                table: "PlOrganizations");

            migrationBuilder.AddColumn<string>(
                name: "EMail",
                table: "PlCustomerTaxInfos",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                comment: "电子邮件地址");

            migrationBuilder.CreateTable(
                name: "BankInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属实体Id。"),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "开户名。"),
                    Bank = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "开户行。"),
                    Account = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "账号。"),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "币种Id。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankInfos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlRolePermissions_PermissionId_RoleId",
                table: "PlRolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankInfos");

            migrationBuilder.DropIndex(
                name: "IX_PlRolePermissions_PermissionId_RoleId",
                table: "PlRolePermissions");

            migrationBuilder.DropColumn(
                name: "EMail",
                table: "PlCustomerTaxInfos");

            migrationBuilder.AddColumn<string>(
                name: "PlPermissionName",
                table: "PlOrganizations",
                type: "nvarchar(64)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlOrganizations_PlPermissionName",
                table: "PlOrganizations",
                column: "PlPermissionName");

            migrationBuilder.AddForeignKey(
                name: "FK_PlOrganizations_PlPermissions_PlPermissionName",
                table: "PlOrganizations",
                column: "PlPermissionName",
                principalTable: "PlPermissions",
                principalColumn: "Name");
        }
    }
}
