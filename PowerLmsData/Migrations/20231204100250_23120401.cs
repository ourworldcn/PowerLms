using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23120401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobNumberReusables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "规则Id"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "回收的时间"),
                    Seq = table.Column<int>(type: "int", nullable: false, comment: "可重用的序列号")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobNumberReusables", x => x.Id);
                },
                comment: "可重用的序列号");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobNumberReusables");
        }
    }
}
