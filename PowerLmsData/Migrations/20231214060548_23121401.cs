using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23121401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_LoginName",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "DD_JobNumberRules");

            migrationBuilder.AlterColumn<string>(
                name: "RuleString",
                table: "DD_JobNumberRules",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "规则字符串。包含前缀，后缀。",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "规则字符串");

            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                comment: "用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码。D2=1标识该用户是全系统超管，D3=1标识该用户是某个商户超管",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码。");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "Accounts",
                type: "nvarchar(450)",
                nullable: true,
                comment: "移动电话号码",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "移动电话号码");

            migrationBuilder.AlterColumn<string>(
                name: "EMail",
                table: "Accounts",
                type: "nvarchar(450)",
                nullable: true,
                comment: "eMail地址",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "eMail地址");

            migrationBuilder.AddColumn<int>(
                name: "JobNumber",
                table: "Accounts",
                type: "int",
                nullable: true,
                comment: "工号。做业务的人员必须有。");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_EMail",
                table: "Accounts",
                column: "EMail");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LoginName",
                table: "Accounts",
                column: "LoginName",
                unique: true,
                filter: "[LoginName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Mobile",
                table: "Accounts",
                column: "Mobile");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_EMail",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_LoginName",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Mobile",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "JobNumber",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "RuleString",
                table: "DD_JobNumberRules",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "规则字符串",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "规则字符串。包含前缀，后缀。");

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "DD_JobNumberRules",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "前缀");

            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                comment: "用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码。D2=1标识该用户是全系统超管，D3=1标识该用户是某个商户超管");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true,
                comment: "移动电话号码",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true,
                oldComment: "移动电话号码");

            migrationBuilder.AlterColumn<string>(
                name: "EMail",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true,
                comment: "eMail地址",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true,
                oldComment: "eMail地址");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LoginName",
                table: "Accounts",
                column: "LoginName");
        }
    }
}
