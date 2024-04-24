using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24042401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwWfKindCodeDics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false, comment: "文档类型Id。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个字符，仅支持英文。"),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "此流程的显示名。"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwWfKindCodeDics", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwWfKindCodeDics");
        }
    }
}
