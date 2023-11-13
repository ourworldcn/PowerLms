using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataDicCatalogs");

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "SystemResources",
                type: "nvarchar(max)",
                nullable: true,
                comment: "说明",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "SystemResources",
                type: "uniqueidentifier",
                nullable: true,
                comment: "父资源的Id。可能分类用",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SystemResources",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "编码，对本系统有一定意义的编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "SystemResources",
                type: "nvarchar(max)",
                nullable: true,
                comment: "显示的名称");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "SystemResources");

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "SystemResources",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "说明");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "SystemResources",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "父资源的Id。可能分类用");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SystemResources",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "编码，对本系统有一定意义的编码");

            migrationBuilder.CreateTable(
                name: "DataDicCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示的名称")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDicCatalogs", x => x.Id);
                });
        }
    }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
