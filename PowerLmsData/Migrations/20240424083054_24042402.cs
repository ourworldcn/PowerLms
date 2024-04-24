using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24042402 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OwWfKindCodeDics",
                table: "OwWfKindCodeDics");

            migrationBuilder.RenameTable(
                name: "OwWfKindCodeDics",
                newName: "WfKindCodeDics");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WfKindCodeDics",
                table: "WfKindCodeDics",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WfKindCodeDics",
                table: "WfKindCodeDics");

            migrationBuilder.RenameTable(
                name: "WfKindCodeDics",
                newName: "OwWfKindCodeDics");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OwWfKindCodeDics",
                table: "OwWfKindCodeDics",
                column: "Id");
        }
    }
}
