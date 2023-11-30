using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23113002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DD_JobNumberRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true, comment: "前缀"),
                    RuleString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "规则字符串"),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false, comment: "当前编号"),
                    RepeatMode = table.Column<short>(type: "smallint", nullable: false, comment: "归零方式，0不归零，1按年，2按月，3按日"),
                    StartValue = table.Column<int>(type: "int", nullable: false, comment: "\"归零\"后的起始值"),
                    RepeatDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "记录最后一次归零的日期"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    ShortcutName = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true, comment: "快捷输入名"),
                    DataDicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属数据字典目录的Id"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_JobNumberRules", x => x.Id);
                },
                comment: "业务编码规则");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_JobNumberRules");
        }
    }
}
