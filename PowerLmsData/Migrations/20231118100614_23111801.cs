using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataDicCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "数据字典的代码。"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名称"),
                    DataDicType = table.Column<int>(type: "int", nullable: false, comment: "数据字典的类型。1=简单字典，其它值随后逐步定义。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDicCatalogs", x => x.Id);
                },
                comment: "专门针对数据字典的目录。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataDicCatalogs");
        }
    }
}
