using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24122301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfitBriefing",
                table: "PlJobs",
                type: "nvarchar(max)",
                nullable: true,
                comment: "利润说明");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "PlJobs",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfitBriefing",
                table: "PlJobs");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "PlJobs");
        }
    }
}
