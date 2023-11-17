using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111704 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreateAccountId",
                table: "SimpleDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建人账号Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "SimpleDataDics",
                type: "datetime2",
                nullable: true,
                comment: "创建时间");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "SimpleDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id。通常这里为null则有不同解释，如通用的模板或超管使用的数据字典。");

            migrationBuilder.AlterColumn<int>(
                name: "Otc",
                table: "PlOrganizations",
                type: "int",
                nullable: false,
                comment: "机构类型，2公司，4下属机构",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "机构类型，1商户，2公司，4下属机构");

            migrationBuilder.AddColumn<string>(
                name: "ContractName",
                table: "PlOrganizations",
                type: "nvarchar(max)",
                nullable: true,
                comment: "联系人名字");

            migrationBuilder.AddColumn<string>(
                name: "EMail",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true,
                comment: "eMail地址");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateAccountId",
                table: "SimpleDataDics");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "SimpleDataDics");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "SimpleDataDics");

            migrationBuilder.DropColumn(
                name: "ContractName",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "EMail",
                table: "Accounts");

            migrationBuilder.AlterColumn<int>(
                name: "Otc",
                table: "PlOrganizations",
                type: "int",
                nullable: false,
                comment: "机构类型，1商户，2公司，4下属机构",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "机构类型，2公司，4下属机构");
        }
    }
}
