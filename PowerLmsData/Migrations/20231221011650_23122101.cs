using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23122101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "PlCustomers",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构的Id。");

            migrationBuilder.CreateTable(
                name: "PlFileInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "文件类型Id。关联字典FileType。"),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "文件类型Id。关联字典FileType。"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "文件的显示名称"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "上传时的文件名"),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属实体的Id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlFileInfos", x => x.Id);
                },
                comment: "文件信息表");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlFileInfos");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "PlCustomers");
        }
    }
}
