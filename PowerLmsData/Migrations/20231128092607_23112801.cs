using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_SimpleDataDics",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_PlPorts",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_PlCargoRoutes",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_BusinessTypeDataDics",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");

            migrationBuilder.CreateTable(
                name: "DD_PlExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "源币种"),
                    DCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "宿币种"),
                    Radix = table.Column<float>(type: "real", nullable: false, comment: "基准，此处默认为100"),
                    Exchange = table.Column<float>(type: "real", nullable: false, comment: "兑换率"),
                    BeginDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "生效时点"),
                    EndData = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "失效时点")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_PlExchangeRates", x => x.Id);
                },
                comment: "汇率");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_PlExchangeRates");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_SimpleDataDics",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_PlPorts",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_PlCargoRoutes",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutName",
                table: "DD_BusinessTypeDataDics",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入名",
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入名");
        }
    }
}
