using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25020701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwAppLoggerItemStores");

            migrationBuilder.DropTable(
                name: "OwAppLoggerStores");

            migrationBuilder.CreateTable(
                name: "OwAppLogItemStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParamstersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtraBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLogItemStores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwAppLogStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormatString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLogStores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwAppLogItemStores_CreateUtc",
                table: "OwAppLogItemStores",
                column: "CreateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OwAppLogItemStores_MerchantId_CreateUtc",
                table: "OwAppLogItemStores",
                columns: new[] { "MerchantId", "CreateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OwAppLogItemStores_ParentId",
                table: "OwAppLogItemStores",
                column: "ParentId");

            try
            {
                migrationBuilder.Sql(@"IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'OwAppLogVO'))
            BEGIN
                EXEC('CREATE VIEW OwAppLogVO
                AS
                SELECT 
                    ali.Id AS Id, 
                    ali.ParentId AS TypeId, 
                    als.FormatString AS Message, 
                    ali.CreateUtc, 
                    ali.MerchantId,
                    ali.ExtraBytes
                FROM 
                    OwAppLogItemStores ali
                JOIN 
                    OwAppLogStores als ON ali.ParentId = als.Id;')
            END");
            }
            catch (Exception)
            {
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS OwAppLogVO");

            migrationBuilder.DropTable(
                name: "OwAppLogItemStores");

            migrationBuilder.DropTable(
                name: "OwAppLogStores");

            migrationBuilder.CreateTable(
                name: "OwAppLoggerItemStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParamstersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLoggerItemStores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwAppLoggerStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormatString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLoggerStores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwAppLoggerItemStores_CreateUtc",
                table: "OwAppLoggerItemStores",
                column: "CreateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OwAppLoggerItemStores_ParentId",
                table: "OwAppLoggerItemStores",
                column: "ParentId");
        }
    }
}
