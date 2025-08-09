using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23120201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataDicId",
                table: "DD_PlPorts");

            migrationBuilder.DropColumn(
                name: "DataDicId",
                table: "DD_PlCargoRoutes");

            migrationBuilder.DropColumn(
                name: "DataDicId",
                table: "DD_JobNumberRules");

            migrationBuilder.DropColumn(
                name: "DataDicId",
                table: "DD_FeesTypes");

            migrationBuilder.DropColumn(
                name: "DataDicType",
                table: "DD_DataDicCatalogs");

            migrationBuilder.DropColumn(
                name: "DataDicId",
                table: "DD_BusinessTypeDataDics");

            migrationBuilder.AddColumn<string>(
                name: "ShortcutName",
                table: "DD_UnitConversions",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "DD_PlPorts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id");

            migrationBuilder.AddColumn<bool>(
                name: "IsDelete",
                table: "DD_PlExchangeRates",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。");

            migrationBuilder.AddColumn<string>(
                name: "ShortcutName",
                table: "DD_PlExchangeRates",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "DD_PlCargoRoutes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "DD_JobNumberRules",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "DD_FeesTypes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id");

            migrationBuilder.CreateTable(
                name: "DD_PlCountrys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id"),
                    ShortcutName = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true, comment: "快捷输入名"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_PlCountrys", x => x.Id);
                },
                comment: "国家");

            migrationBuilder.CreateTable(
                name: "DD_PlCurrencys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id"),
                    ShortcutName = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true, comment: "快捷输入名"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_PlCurrencys", x => x.Id);
                },
                comment: "币种");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_PlCountrys");

            migrationBuilder.DropTable(
                name: "DD_PlCurrencys");

            migrationBuilder.DropColumn(
                name: "ShortcutName",
                table: "DD_UnitConversions");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "DD_PlPorts");

            migrationBuilder.DropColumn(
                name: "IsDelete",
                table: "DD_PlExchangeRates");

            migrationBuilder.DropColumn(
                name: "ShortcutName",
                table: "DD_PlExchangeRates");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "DD_PlCargoRoutes");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "DD_JobNumberRules");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "DD_FeesTypes");

            migrationBuilder.AddColumn<Guid>(
                name: "DataDicId",
                table: "DD_PlPorts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典目录的Id");

            migrationBuilder.AddColumn<Guid>(
                name: "DataDicId",
                table: "DD_PlCargoRoutes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典目录的Id");

            migrationBuilder.AddColumn<Guid>(
                name: "DataDicId",
                table: "DD_JobNumberRules",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典目录的Id");

            migrationBuilder.AddColumn<Guid>(
                name: "DataDicId",
                table: "DD_FeesTypes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典目录的Id");

            migrationBuilder.AddColumn<int>(
                name: "DataDicType",
                table: "DD_DataDicCatalogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "数据字典的类型。1=简单字典；2=复杂字典；3=这是简单字典，但UI需要作为复杂字典处理（实际是掩码D0+D1）；其它值随后逐步定义。");

            migrationBuilder.AddColumn<Guid>(
                name: "DataDicId",
                table: "DD_BusinessTypeDataDics",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属数据字典目录的Id");
        }
    }
}
