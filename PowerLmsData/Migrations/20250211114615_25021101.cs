using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25021101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BaseCurrencyCode",
                table: "PlOrganizations",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: false,
                defaultValue: "",
                comment: "本位币编码",
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldUnicode: false,
                oldMaxLength: 4,
                oldNullable: true,
                oldComment: "本位币编码");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BaseCurrencyCode",
                table: "PlOrganizations",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "本位币编码",
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldUnicode: false,
                oldMaxLength: 4,
                oldComment: "本位币编码");
        }
    }
}
