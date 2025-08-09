using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23122701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlAccountRoles_RoleId_UserId",
                table: "PlAccountRoles",
                columns: new[] { "RoleId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlAccountRoles_RoleId_UserId",
                table: "PlAccountRoles");
        }
    }
}
