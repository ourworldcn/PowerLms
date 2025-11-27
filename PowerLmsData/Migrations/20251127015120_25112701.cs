using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25112701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlFileInfos",
                type: "datetime2",
                nullable: false,
                comment: "新建时间,系统默认,不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AddColumn<string>(
                name: "ClientString",
                table: "PlFileInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "客户端字符串，客户端可写入，服务端不使用");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PlFileInfos",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                comment: "行版本号，用于开放式并发控制");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientString",
                table: "PlFileInfos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PlFileInfos");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlFileInfos",
                type: "datetime2",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "新建时间,系统默认,不能更改。");
        }
    }
}
