using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25092601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "DD_OtherNumberRules",
                comment: "其他编码规则");

            migrationBuilder.CreateTable(
                name: "OwForumCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwForumCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Titel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsTop = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentReplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwReplies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwForumCategories_AuthorId",
                table: "OwForumCategories",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_OwForumCategories_ParentId",
                table: "OwForumCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OwPosts_AuthorId",
                table: "OwPosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_OwPosts_CreatedAt",
                table: "OwPosts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OwPosts_ParentId",
                table: "OwPosts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OwReplies_AuthorId",
                table: "OwReplies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_OwReplies_CreatedAt",
                table: "OwReplies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OwReplies_ParentId",
                table: "OwReplies",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OwReplies_ParentReplyId",
                table: "OwReplies",
                column: "ParentReplyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwForumCategories");

            migrationBuilder.DropTable(
                name: "OwPosts");

            migrationBuilder.DropTable(
                name: "OwReplies");

            migrationBuilder.AlterTable(
                name: "DD_OtherNumberRules",
                oldComment: "其他编码规则");
        }
    }
}
