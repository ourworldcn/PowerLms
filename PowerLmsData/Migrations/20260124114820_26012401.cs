using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26012401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MawbNoDisplay",
                table: "PlEaMawbInbounds",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true,
                comment: "主单号（显示格式，保留原始格式）",
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25,
                oldNullable: true,
                oldComment: "主单号（显示格式，保留空格）");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MawbNoDisplay",
                table: "PlEaMawbInbounds",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true,
                comment: "主单号（显示格式，保留空格）",
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25,
                oldNullable: true,
                oldComment: "主单号（显示格式，保留原始格式）");
        }
    }
}
