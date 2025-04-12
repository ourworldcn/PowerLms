using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25040201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CustomsCode",
                table: "DD_SimpleDataDics",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                comment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "DD_FeesTypes",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                comment: "币种代码");

            migrationBuilder.AddColumn<string>(
                name: "FeeGroupCode",
                table: "DD_FeesTypes",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                comment: "费用组代码");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "DD_FeesTypes");

            migrationBuilder.DropColumn(
                name: "FeeGroupCode",
                table: "DD_FeesTypes");

            migrationBuilder.AlterColumn<string>(
                name: "CustomsCode",
                table: "DD_SimpleDataDics",
                type: "nvarchar(max)",
                nullable: true,
                comment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。");
        }
    }
}
