using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsServer.Migrations
{
    public partial class _23110501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Multilinguals_Key",
                table: "Multilinguals");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Multilinguals",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddColumn<string>(
                name: "LanguageTag",
                table: "Multilinguals",
                type: "varchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals",
                columns: new[] { "LanguageTag", "Key" },
                unique: true,
                filter: "[LanguageTag] IS NOT NULL AND [Key] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals");

            migrationBuilder.DropColumn(
                name: "LanguageTag",
                table: "Multilinguals");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Multilinguals",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier")
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.CreateIndex(
                name: "IX_Multilinguals_Key",
                table: "Multilinguals",
                column: "Key",
                unique: true,
                filter: "[Key] IS NOT NULL");
        }
    }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
