using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24031301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "FileTypeId",
                table: "PlFileInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "文件类型Id。关联字典FileType。可能是null，表示这是一个通用文件。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "文件类型Id。关联字典FileType。");

            migrationBuilder.CreateTable(
                name: "HuochangChuchongs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EaDocId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "运单Id。"),
                    HblNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "分单号。"),
                    PkgsCount = table.Column<int>(type: "int", nullable: false, comment: "件数。"),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "结算计费重量，3位小数"),
                    MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "体积，3位小数"),
                    CargoSize = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "体积，字符串")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuochangChuchongs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HuochangChuchongs_EaDocId",
                table: "HuochangChuchongs",
                column: "EaDocId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HuochangChuchongs");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileTypeId",
                table: "PlFileInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "文件类型Id。关联字典FileType。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "文件类型Id。关联字典FileType。可能是null，表示这是一个通用文件。");
        }
    }
}
