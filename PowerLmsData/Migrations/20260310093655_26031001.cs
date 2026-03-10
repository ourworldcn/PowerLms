using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26031001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CdDomesticPorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "编码"),
                    Cname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "中文名"),
                    Ename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "英文名")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdDomesticPorts", x => x.Id);
                },
                comment: "国内口岸代码表");

            migrationBuilder.CreateTable(
                name: "CdGoodsVsCiqCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    HsCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "HS编码"),
                    CiqName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "CIQ分类名称"),
                    HsName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "HS名称")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdGoodsVsCiqCodes", x => x.Id);
                },
                comment: "报关CIQCODE检疫代码表");

            migrationBuilder.CreateTable(
                name: "CdHsCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    HsCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "货物编号（HSCODE）"),
                    GoodsDesc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "货物描述"),
                    ControlMa = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "监管条件"),
                    NoteS = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    Unit1 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "单位1"),
                    Unit2 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "单位2"),
                    Remark = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "申报要素备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdHsCodes", x => x.Id);
                },
                comment: "报关HSCODE基础表");

            migrationBuilder.CreateTable(
                name: "CdInspectionPlaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "编码"),
                    Cname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "中文名"),
                    Ename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "英文名")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdInspectionPlaces", x => x.Id);
                },
                comment: "国内地区代码（检疫用）表");

            migrationBuilder.CreateTable(
                name: "CdPlaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "编码"),
                    Cname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "中文名"),
                    Ename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "英文名")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdPlaces", x => x.Id);
                },
                comment: "国内行政区划表");

            migrationBuilder.CreateTable(
                name: "CdPorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "编码"),
                    Cname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "中文名"),
                    Ename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "英文名"),
                    CountryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "国家代码")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CdPorts", x => x.Id);
                },
                comment: "报关专用港口表，使用海关特定代码体系");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CdDomesticPorts");

            migrationBuilder.DropTable(
                name: "CdGoodsVsCiqCodes");

            migrationBuilder.DropTable(
                name: "CdHsCodes");

            migrationBuilder.DropTable(
                name: "CdInspectionPlaces");

            migrationBuilder.DropTable(
                name: "CdPlaces");

            migrationBuilder.DropTable(
                name: "CdPorts");
        }
    }
}
