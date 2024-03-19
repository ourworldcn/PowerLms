using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24031901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HblNoString",
                table: "PlJobs",
                type: "nvarchar(max)",
                nullable: true,
                comment: "分单号字符串，/分隔多个分单号",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "分单号字符串，/分隔多个分单号");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "HblNoString",
                table: "PlJobs",
                type: "uniqueidentifier",
                nullable: true,
                comment: "分单号字符串，/分隔多个分单号",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "分单号字符串，/分隔多个分单号");
        }
    }
}
