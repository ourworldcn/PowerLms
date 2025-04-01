using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25040101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "接收用户ID"),
                    Title = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "消息标题。最长64字符。"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "消息内容。HTML格式"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者ID"),
                    CreateUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, comment: "创建时间"),
                    ReadUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true, comment: "读取时间"),
                    IsSystemMessage = table.Column<bool>(type: "bit", nullable: false, comment: "是否是系统消息")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwMessages", x => x.Id);
                },
                comment: "系统内消息");

            migrationBuilder.CreateIndex(
                name: "IX_OwMessages_UserId_CreateUtc",
                table: "OwMessages",
                columns: new[] { "UserId", "CreateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OwMessages_UserId_ReadUtc",
                table: "OwMessages",
                columns: new[] { "UserId", "ReadUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwMessages");
        }
    }
}
