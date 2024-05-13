using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24051301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientString",
                table: "WfTemplates",
                type: "nvarchar(max)",
                nullable: true,
                comment: "客户端记录一些必要信息，服务器不使用。");

            migrationBuilder.AddColumn<string>(
                name: "ClientString",
                table: "WfTemplateNodes",
                type: "nvarchar(max)",
                nullable: true,
                comment: "客户端记录一些必要信息，服务器不使用。");

            migrationBuilder.AddColumn<string>(
                name: "ClientString",
                table: "WfTemplateNodeItems",
                type: "nvarchar(max)",
                nullable: true,
                comment: "客户端记录一些必要信息，服务器不使用。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientString",
                table: "WfTemplates");

            migrationBuilder.DropColumn(
                name: "ClientString",
                table: "WfTemplateNodes");

            migrationBuilder.DropColumn(
                name: "ClientString",
                table: "WfTemplateNodeItems");
        }
    }
}
