using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "MerchantId",
                table: "PlOrganizations",
                type: "uniqueidentifier",
                nullable: true,
                comment: "商户Id。仅总公司(ParentId 是null)需要此字段指向所属商户，其它情况忽略此字段。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessTypeDataDics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    ShortcutName = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入名")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessTypeDataDics", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessTypeDataDics");

            migrationBuilder.AlterColumn<Guid>(
                name: "MerchantId",
                table: "PlOrganizations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "商户Id。仅总公司(ParentId 是null)需要此字段指向所属商户，其它情况忽略此字段。");
        }
    }
}
