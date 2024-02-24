using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24022401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "ShippingLanes");

            migrationBuilder.AddColumn<decimal>(
                name: "ArrivalTimeId",
                table: "ShippingLanes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "到达时长。单位:天。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivalTimeId",
                table: "ShippingLanes");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ArrivalTime",
                table: "ShippingLanes",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                comment: "到达时长");
        }
    }
}
