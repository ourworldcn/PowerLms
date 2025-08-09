using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23122802 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionId",
                table: "PlRolePermissions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                comment: "权限Id。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "权限Id。");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions",
                columns: new[] { "RoleId", "PermissionId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionId",
                table: "PlRolePermissions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "权限Id。",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldComment: "权限Id。");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions",
                column: "RoleId");
        }
    }
}
