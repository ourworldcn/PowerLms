using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24042501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WfTemplates_OrgId",
                table: "WfTemplates");

            migrationBuilder.DropColumn(
                name: "RejectOperation",
                table: "WfTemplateNodes");

            migrationBuilder.CreateIndex(
                name: "IX_WfTemplates_OrgId_KindCode",
                table: "WfTemplates",
                columns: new[] { "OrgId", "KindCode" });

            migrationBuilder.AddForeignKey(
                name: "FK_WfTemplateNodeItems_WfTemplateNodes_ParentId",
                table: "WfTemplateNodeItems",
                column: "ParentId",
                principalTable: "WfTemplateNodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WfTemplateNodes_WfTemplates_ParentId",
                table: "WfTemplateNodes",
                column: "ParentId",
                principalTable: "WfTemplates",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WfTemplateNodeItems_WfTemplateNodes_ParentId",
                table: "WfTemplateNodeItems");

            migrationBuilder.DropForeignKey(
                name: "FK_WfTemplateNodes_WfTemplates_ParentId",
                table: "WfTemplateNodes");

            migrationBuilder.DropIndex(
                name: "IX_WfTemplates_OrgId_KindCode",
                table: "WfTemplates");

            migrationBuilder.AddColumn<byte>(
                name: "RejectOperation",
                table: "WfTemplateNodes",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "拒绝后的操作，1 = 终止,2=回退");

            migrationBuilder.CreateIndex(
                name: "IX_WfTemplates_OrgId",
                table: "WfTemplates",
                column: "OrgId");
        }
    }
}
