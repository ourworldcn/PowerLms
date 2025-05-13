using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25051301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "DocFeeRequisitions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "发票号");

            migrationBuilder.AlterColumn<string>(
                name: "RuleString",
                table: "DD_JobNumberRules",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "规则字符串。包含前缀，后缀。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "DocFeeRequisitions");

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
                oldNullable: true);
        }
    }
}
