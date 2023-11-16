using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LanguageDataDics");

            migrationBuilder.DropColumn(
                name: "OriId",
                table: "SimpleDataDics");

            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "SimpleDataDics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "缩写名");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "SimpleDataDics");

            migrationBuilder.AddColumn<Guid>(
                name: "OriId",
                table: "SimpleDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id");

            migrationBuilder.CreateTable(
                name: "LanguageDataDics",
                columns: table => new
                {
                    LanguageTag = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, comment: "主键，也是语言的标准缩写名。"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "语言名。"),
                    Lcid = table.Column<int>(type: "int", nullable: false, comment: "语言Id。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanguageDataDics", x => x.LanguageTag);
                },
                comment: "语言字典，参见 https://learn.microsoft.com/zh-cn/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c。");
        }
    }
}
