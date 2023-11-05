using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsServer.Migrations
{
    public partial class _23110503 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Multilinguals",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals",
                columns: new[] { "LanguageTag", "Key" },
                unique: true,
                filter: "[Key] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Multilinguals",
                type: "varchar(12)",
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12);

            migrationBuilder.CreateIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals",
                columns: new[] { "LanguageTag", "Key" },
                unique: true,
                filter: "[LanguageTag] IS NOT NULL AND [Key] IS NOT NULL");
        }
    }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
