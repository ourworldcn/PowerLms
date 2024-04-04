using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24040401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocFees_DocId",
                table: "DocFees");

            migrationBuilder.DropColumn(
                name: "DocId",
                table: "DocFees");

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "业务Id");

            migrationBuilder.CreateIndex(
                name: "IX_DocFees_BillId",
                table: "DocFees",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_DocFees_JobId",
                table: "DocFees",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocFees_BillId",
                table: "DocFees");

            migrationBuilder.DropIndex(
                name: "IX_DocFees_JobId",
                table: "DocFees");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "DocFees");

            migrationBuilder.AddColumn<Guid>(
                name: "DocId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "业务单的Id");

            migrationBuilder.CreateIndex(
                name: "IX_DocFees_DocId",
                table: "DocFees",
                column: "DocId");
        }
    }
}
