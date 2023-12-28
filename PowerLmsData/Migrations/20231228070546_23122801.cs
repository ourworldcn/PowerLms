using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23122801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlPermissions",
                table: "PlPermissions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PlPermissions");

            migrationBuilder.RenameColumn(
                name: "Name_ShortName",
                table: "PlPermissions",
                newName: "ShortName");

            migrationBuilder.RenameColumn(
                name: "Name_Name",
                table: "PlPermissions",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Name_DisplayName",
                table: "PlPermissions",
                newName: "DisplayName");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionId",
                table: "PlRolePermissions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "权限Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "权限Id。");

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "PlPermissions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "简称",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "正式简称，对正式的组织机构通常简称也是规定的");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PlPermissions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                comment: "正式名称，唯一Id",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "正式名称，拥有相对稳定性");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "PlPermissions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "简称",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "显示名，有时它是昵称或简称(系统内)的意思");

            migrationBuilder.AddColumn<string>(
                name: "ParentId",
                table: "PlPermissions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "所属许可对象的Id。");

            migrationBuilder.AddColumn<string>(
                name: "PlPermissionName",
                table: "PlOrganizations",
                type: "nvarchar(64)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions",
                column: "RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlPermissions",
                table: "PlPermissions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PlPermissions_ParentId",
                table: "PlPermissions",
                column: "ParentId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_PlPermissions_PlPermissions_ParentId",
                table: "PlPermissions",
                column: "ParentId",
                principalTable: "PlPermissions",
                principalColumn: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlOrganizations_PlPermissions_PlPermissionName",
                table: "PlOrganizations");

            migrationBuilder.DropForeignKey(
                name: "FK_PlPermissions_PlPermissions_ParentId",
                table: "PlPermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlPermissions",
                table: "PlPermissions");

            migrationBuilder.DropIndex(
                name: "IX_PlPermissions_ParentId",
                table: "PlPermissions");

            migrationBuilder.DropIndex(
                name: "IX_PlOrganizations_PlPermissionName",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "PlPermissions");

            migrationBuilder.DropColumn(
                name: "PlPermissionName",
                table: "PlOrganizations");

            migrationBuilder.RenameColumn(
                name: "ShortName",
                table: "PlPermissions",
                newName: "Name_ShortName");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "PlPermissions",
                newName: "Name_DisplayName");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "PlPermissions",
                newName: "Name_Name");

            migrationBuilder.AlterColumn<Guid>(
                name: "PermissionId",
                table: "PlRolePermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "权限Id。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "权限Id。");

            migrationBuilder.AlterColumn<string>(
                name: "Name_ShortName",
                table: "PlPermissions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "正式简称，对正式的组织机构通常简称也是规定的",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "简称");

            migrationBuilder.AlterColumn<string>(
                name: "Name_DisplayName",
                table: "PlPermissions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "显示名，有时它是昵称或简称(系统内)的意思",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "简称");

            migrationBuilder.AlterColumn<string>(
                name: "Name_Name",
                table: "PlPermissions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "正式名称，拥有相对稳定性",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldComment: "正式名称，唯一Id");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "PlPermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlRolePermissions",
                table: "PlRolePermissions",
                columns: new[] { "RoleId", "PermissionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlPermissions",
                table: "PlPermissions",
                column: "Id");
        }
    }
}
