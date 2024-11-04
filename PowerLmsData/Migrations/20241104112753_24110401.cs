using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24110401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaptchaInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifyDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DownloadDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FullPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptchaInfos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaptchaInfos_DownloadDateTime",
                table: "CaptchaInfos",
                column: "DownloadDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_CaptchaInfos_VerifyDateTime",
                table: "CaptchaInfos",
                column: "VerifyDateTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaptchaInfos");
        }
    }
}
