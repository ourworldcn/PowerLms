using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24032801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DD_OtherNumberRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true, comment: "规则编码（标准英文名）"),
                    DisplayName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "规则显示名称"),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false, comment: "当前未用的最小编号"),
                    RuleString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "规则字符串。包含前缀，后缀。"),
                    RepeatMode = table.Column<short>(type: "smallint", nullable: false, comment: "归零方式，0不归零，1按年，2按月，3按日"),
                    StartValue = table.Column<int>(type: "int", nullable: false, comment: "\"归零\"后的起始值"),
                    RepeatDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "记录最后一次归零的日期"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_OtherNumberRules", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_OtherNumberRules");
        }
    }
}
