using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112702 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DataDicId",
                table: "DD_SimpleDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典目录的Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属数据字典的的Id");

            migrationBuilder.AddColumn<Guid>(
                name: "DataDicId",
                table: "DD_BusinessTypeDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典目录的Id");

            migrationBuilder.AddColumn<bool>(
                name: "IsDelete",
                table: "DD_BusinessTypeDataDics",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "DD_BusinessTypeDataDics",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注");

            migrationBuilder.CreateTable(
                name: "DD_PlCargoRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CAFRate = table.Column<int>(type: "int", nullable: true, comment: "CAF比率，取%值。"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    ShortcutName = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入名"),
                    DataDicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属数据字典目录的Id"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_PlCargoRoutes", x => x.Id);
                },
                comment: "航线");

            migrationBuilder.CreateTable(
                name: "DD_PlPorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomsCode = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。"),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "国家"),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "省"),
                    NumCode = table.Column<int>(type: "int", nullable: true, comment: "数字码.可空"),
                    PlCargoRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属航线Id"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    ShortcutName = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入名"),
                    DataDicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属数据字典目录的Id"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_PlPorts", x => x.Id);
                },
                comment: "港口");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_PlCargoRoutes");

            migrationBuilder.DropTable(
                name: "DD_PlPorts");

            migrationBuilder.DropColumn(
                name: "DataDicId",
                table: "DD_BusinessTypeDataDics");

            migrationBuilder.DropColumn(
                name: "IsDelete",
                table: "DD_BusinessTypeDataDics");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "DD_BusinessTypeDataDics");

            migrationBuilder.AlterColumn<Guid>(
                name: "DataDicId",
                table: "DD_SimpleDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典的的Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属数据字典目录的Id");
        }
    }
}
