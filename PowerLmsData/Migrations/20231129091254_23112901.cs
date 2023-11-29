using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23112901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "DD_PlExchangeRates",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessTypeId",
                table: "DD_PlExchangeRates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "业务类型Id");

            migrationBuilder.CreateTable(
                name: "DD_FeesTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyTypeId = table.Column<bool>(type: "bit", nullable: false, comment: "币种Id"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "默认单价"),
                    FeeGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "费用组Id"),
                    IsPay = table.Column<bool>(type: "bit", nullable: false, comment: "是否应付。true是应付。"),
                    IsGather = table.Column<bool>(type: "bit", nullable: false, comment: "是否应收。true是应收。"),
                    IsCommission = table.Column<bool>(type: "bit", nullable: false, comment: "是否佣金,True是佣金。"),
                    IsDaiDian = table.Column<bool>(type: "bit", nullable: false, comment: "是否代垫费用,true垫付。"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    ShortcutName = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true, comment: "快捷输入名"),
                    DataDicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属数据字典目录的Id"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_FeesTypes", x => x.Id);
                },
                comment: "费用种类");

            migrationBuilder.CreateTable(
                name: "DD_UnitConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id"),
                    Basic = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "基单位"),
                    Rim = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "宿单位"),
                    Rate = table.Column<float>(type: "real", nullable: false, comment: "换算率"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_UnitConversions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_FeesTypes");

            migrationBuilder.DropTable(
                name: "DD_UnitConversions");

            migrationBuilder.DropColumn(
                name: "BusinessTypeId",
                table: "DD_PlExchangeRates");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "DD_PlExchangeRates",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属组织机构Id");
        }
    }
}
