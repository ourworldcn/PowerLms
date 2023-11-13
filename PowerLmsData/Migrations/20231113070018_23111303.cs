using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111303 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriId",
                table: "SimpleDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "当前使用的组织机构Id。在登陆后要首先设置",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属组织机构Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriId",
                table: "SimpleDataDics");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "当前使用的组织机构Id。在登陆后要首先设置");
        }
    }
}
