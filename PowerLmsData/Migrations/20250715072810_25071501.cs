using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25071501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubjectNumber",
                table: "SubjectConfigurations",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                comment: "会计科目编码",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldComment: "科目号（会计科目编号）");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "SubjectConfigurations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                comment: "显示名称。",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldComment: "显示名称");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SubjectConfigurations",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                comment: "配置项编码",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldComment: "科目编码");

            migrationBuilder.AddColumn<string>(
                name: "Preparer",
                table: "SubjectConfigurations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "制单人（金蝶制单人名称）");

            migrationBuilder.AddColumn<string>(
                name: "AAccountSubjectCode",
                table: "BankInfos",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                comment: "A账财务科目代码");

            migrationBuilder.AddColumn<string>(
                name: "BAccountSubjectCode",
                table: "BankInfos",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                comment: "B账财务科目代码");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preparer",
                table: "SubjectConfigurations");

            migrationBuilder.DropColumn(
                name: "AAccountSubjectCode",
                table: "BankInfos");

            migrationBuilder.DropColumn(
                name: "BAccountSubjectCode",
                table: "BankInfos");

            migrationBuilder.AlterColumn<string>(
                name: "SubjectNumber",
                table: "SubjectConfigurations",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                comment: "科目号（会计科目编号）",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldComment: "会计科目编码");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "SubjectConfigurations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                comment: "显示名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldComment: "显示名称。");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SubjectConfigurations",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                comment: "科目编码",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldComment: "配置项编码");
        }
    }
}
