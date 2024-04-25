using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24042503 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OwWfNodeItems_OwWfNodes_OwWfNodeId",
                table: "OwWfNodeItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OwWfNodeItems_OwWfs_ParentId",
                table: "OwWfNodeItems");

            migrationBuilder.DropIndex(
                name: "IX_OwWfNodeItems_OwWfNodeId",
                table: "OwWfNodeItems");

            migrationBuilder.DropColumn(
                name: "OwWfNodeId",
                table: "OwWfNodeItems");

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "OwWfNodes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "节点模板的Id。");

            migrationBuilder.AddForeignKey(
                name: "FK_OwWfNodeItems_OwWfNodes_ParentId",
                table: "OwWfNodeItems",
                column: "ParentId",
                principalTable: "OwWfNodes",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OwWfNodeItems_OwWfNodes_ParentId",
                table: "OwWfNodeItems");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "OwWfNodes");

            migrationBuilder.AddColumn<Guid>(
                name: "OwWfNodeId",
                table: "OwWfNodeItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwWfNodeItems_OwWfNodeId",
                table: "OwWfNodeItems",
                column: "OwWfNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OwWfNodeItems_OwWfNodes_OwWfNodeId",
                table: "OwWfNodeItems",
                column: "OwWfNodeId",
                principalTable: "OwWfNodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OwWfNodeItems_OwWfs_ParentId",
                table: "OwWfNodeItems",
                column: "ParentId",
                principalTable: "OwWfs",
                principalColumn: "Id");
        }
    }
}
