using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111304 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountPlOrganizations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "用户Id"),
                    OriId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属组织机构Id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountPlOrganizations", x => new { x.UserId, x.OriId });
                },
                comment: "账号所属组织机构多对多表");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountPlOrganizations");
        }
    }
}
