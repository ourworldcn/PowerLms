using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25042201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PlCargoRouteId",
                table: "DD_PlPorts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属航线Id。不推荐使用，请使用CargoRouteCode替代。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属航线Id");

            migrationBuilder.AddColumn<string>(
                name: "CargoRouteCode",
                table: "DD_PlPorts",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                comment: "所属航线编码。可为空表示该港口不属于任何航线。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoRouteCode",
                table: "DD_PlPorts");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlCargoRouteId",
                table: "DD_PlPorts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属航线Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属航线Id。不推荐使用，请使用CargoRouteCode替代。");
        }
    }
}
