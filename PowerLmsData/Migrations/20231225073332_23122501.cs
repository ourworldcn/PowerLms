using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23122501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BusinessTypeld",
                table: "DD_JobNumberRules",
                newName: "BusinessTypeId");

            migrationBuilder.CreateTable(
                name: "PlAccountRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "账号Id。"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "角色Id。"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者的唯一标识"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建的时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlAccountRoles", x => new { x.UserId, x.RoleId });
                },
                comment: "记录账号与角色关系的类。");

            migrationBuilder.CreateTable(
                name: "PlPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "正式名称，拥有相对稳定性"),
                    Name_ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "正式简称，对正式的组织机构通常简称也是规定的"),
                    Name_DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名，有时它是昵称或简称(系统内)的意思")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlPermissions", x => x.Id);
                },
                comment: "权限类。");

            migrationBuilder.CreateTable(
                name: "PlRolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "角色Id。"),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "权限Id。"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者的唯一标识"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建的时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlRolePermissions", x => new { x.RoleId, x.PermissionId });
                },
                comment: "记录角色和权限的关系类。");

            migrationBuilder.CreateTable(
                name: "PlRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id。"),
                    Name_Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "正式名称，拥有相对稳定性"),
                    Name_ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "正式简称，对正式的组织机构通常简称也是规定的"),
                    Name_DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名，有时它是昵称或简称(系统内)的意思"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者的唯一标识"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建的时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlRoles", x => x.Id);
                },
                comment: "角色类。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlAccountRoles");

            migrationBuilder.DropTable(
                name: "PlPermissions");

            migrationBuilder.DropTable(
                name: "PlRolePermissions");

            migrationBuilder.DropTable(
                name: "PlRoles");

            migrationBuilder.RenameColumn(
                name: "BusinessTypeId",
                table: "DD_JobNumberRules",
                newName: "BusinessTypeld");
        }
    }
}
