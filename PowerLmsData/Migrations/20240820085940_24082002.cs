using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24082002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OwSystemLogs_ActionId_WorldDateTime",
                table: "OwSystemLogs");

            migrationBuilder.DropIndex(
                name: "IX_OwSystemLogs_WorldDateTime_ActionId",
                table: "OwSystemLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "OwSystemLogs",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属机构Id");

            migrationBuilder.CreateIndex(
                name: "IX_OwSystemLogs_OrgId_ActionId_WorldDateTime",
                table: "OwSystemLogs",
                columns: new[] { "OrgId", "ActionId", "WorldDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_OwSystemLogs_OrgId_WorldDateTime_ActionId",
                table: "OwSystemLogs",
                columns: new[] { "OrgId", "WorldDateTime", "ActionId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OwSystemLogs_OrgId_ActionId_WorldDateTime",
                table: "OwSystemLogs");

            migrationBuilder.DropIndex(
                name: "IX_OwSystemLogs_OrgId_WorldDateTime_ActionId",
                table: "OwSystemLogs");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "OwSystemLogs");

            migrationBuilder.CreateIndex(
                name: "IX_OwSystemLogs_ActionId_WorldDateTime",
                table: "OwSystemLogs",
                columns: new[] { "ActionId", "WorldDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_OwSystemLogs_WorldDateTime_ActionId",
                table: "OwSystemLogs",
                columns: new[] { "WorldDateTime", "ActionId" });
        }
    }
}
