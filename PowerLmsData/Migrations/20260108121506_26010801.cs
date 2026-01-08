using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26010801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EnglishAddress",
                table: "PlCustomers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                comment: "英文地址",
                oldClrType: typeof(string),
                oldType: "varchar(256)",
                oldUnicode: false,
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "英文地址");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EnglishAddress",
                table: "PlCustomers",
                type: "varchar(256)",
                unicode: false,
                maxLength: 256,
                nullable: true,
                comment: "英文地址",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "英文地址");
        }
    }
}
