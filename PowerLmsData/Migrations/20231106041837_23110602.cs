using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23110602 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PwdHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LanguageTag = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NodeNum = table.Column<int>(type: "int", nullable: true),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Timeout = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
