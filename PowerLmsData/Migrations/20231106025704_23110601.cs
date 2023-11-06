using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23110601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LanguageDataDics",
                columns: table => new
                {
                    LanguageTag = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                    Lcid = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageDataDics", x => x.LanguageTag);
                });

            migrationBuilder.CreateTable(
                name: "Multilinguals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageTag = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Multilinguals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals",
                columns: new[] { "LanguageTag", "Key" },
                unique: true,
                filter: "[Key] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LanguageDataDics");

            migrationBuilder.DropTable(
                name: "Multilinguals");
        }
    }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
