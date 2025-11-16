using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25111601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationNumber",
                table: "OaExpenseRequisitions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "申请编号。唯一标识申请单的编号");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationNumber",
                table: "OaExpenseRequisitions");
        }
    }
}
