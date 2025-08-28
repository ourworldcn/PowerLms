using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25082801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "客户Id。关联客户资料表，用于选择具体的客户/公司");

            migrationBuilder.AddColumn<bool>(
                name: "IO",
                table: "DocBills",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "收支方向。false=支出（付款），true=收入（收款）");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "IO",
                table: "DocBills");
        }
    }
}
