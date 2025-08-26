using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25082602 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccountDate",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "财务日期，前端自动计算设置，后端直接使用");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountDate",
                table: "PlJobs");
        }
    }
}
