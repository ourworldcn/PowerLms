using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23120101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DataDicType",
                table: "DD_DataDicCatalogs",
                type: "int",
                nullable: false,
                comment: "数据字典的类型。1=简单字典；2=复杂字典；3=这是简单字典，但UI需要作为复杂字典处理（实际是掩码D0+D1）；其它值随后逐步定义。",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "数据字典的类型。1=简单字典，其它值随后逐步定义。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DataDicType",
                table: "DD_DataDicCatalogs",
                type: "int",
                nullable: false,
                comment: "数据字典的类型。1=简单字典，其它值随后逐步定义。",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "数据字典的类型。1=简单字典；2=复杂字典；3=这是简单字典，但UI需要作为复杂字典处理（实际是掩码D0+D1）；其它值随后逐步定义。");
        }
    }
}
