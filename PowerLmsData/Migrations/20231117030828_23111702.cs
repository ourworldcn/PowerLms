using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23111702 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Name = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "正式名称，拥有相对稳定性"),
                    Name_ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "正式简称，对正式的组织机构通常简称也是规定的"),
                    Name_DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名，有时它是昵称或简称(系统内)的意思"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "描述"),
                    ShortcutCode = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。"),
                    Address_Tel = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "电话"),
                    Address_Fax = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "传真"),
                    Address_FullAddress = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "详细地址")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                },
                comment: "商户");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Merchants");
        }
    }
}
