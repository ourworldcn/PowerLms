using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25022302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Type",
                table: "PlCustomerTaxInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "纳税人种类Id。简单字典AddedTaxType。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "PlCustomerTaxInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "抬头名称");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "PlCustomerTaxInfos");

            migrationBuilder.AlterColumn<Guid>(
                name: "Type",
                table: "PlCustomerTaxInfos",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "纳税人种类Id。简单字典AddedTaxType。");
        }
    }
}
