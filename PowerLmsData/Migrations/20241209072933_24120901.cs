using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24120901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseCurrencyCode",
                table: "PlOrganizations",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "本位币编码");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "当前登录的组织机构Id（仅能是公司）。在登陆后要首先设置",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "当前使用的组织机构Id。在登陆后要首先设置");

            migrationBuilder.AlterColumn<byte>(
                name: "JobPermission",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                comment: "业务权限。1=个人，2=组织，4=公司。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "报表权限。1=个人，2=组织，4=公司。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseCurrencyCode",
                table: "PlOrganizations");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "当前使用的组织机构Id。在登陆后要首先设置",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "当前登录的组织机构Id（仅能是公司）。在登陆后要首先设置");

            migrationBuilder.AlterColumn<byte>(
                name: "JobPermission",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                comment: "报表权限。1=个人，2=组织，4=公司。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "业务权限。1=个人，2=组织，4=公司。");
        }
    }
}
