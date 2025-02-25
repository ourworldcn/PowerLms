using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25022501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceChannel",
                table: "PlCustomerTaxInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "开票渠道。仅仅是一个标记，服务器通过改标识来决定调用什么接口。");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceChannelParams",
                table: "PlCustomerTaxInfos",
                type: "varchar(max)",
                unicode: false,
                nullable: true,
                comment: "开票渠道参数。Json格式的字符串。包含敏感信息。");

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "PlCustomerTaxInfos",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "手机号");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceChannel",
                table: "PlCustomerTaxInfos");

            migrationBuilder.DropColumn(
                name: "InvoiceChannelParams",
                table: "PlCustomerTaxInfos");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "PlCustomerTaxInfos");
        }
    }
}
