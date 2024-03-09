using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24022402 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ArrivalTimeId",
                table: "ShippingLanes",
                newName: "ArrivalTimeInDay");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ArrivalTimeInDay",
                table: "ShippingLanes",
                newName: "ArrivalTimeId");
        }
    }
}
