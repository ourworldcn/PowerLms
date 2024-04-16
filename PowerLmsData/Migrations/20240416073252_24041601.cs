using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24041601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WfTemplateNodeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属节点Id。"),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "参与者(员工)Id。"),
                    OperationKind = table.Column<byte>(type: "tinyint", nullable: false, comment: "参与者类型，目前保留为0。预计1=抄送人，此时则无视优先度——任意一个审批人得到文档时，所有抄送人同时得到此文档。"),
                    Priority = table.Column<int>(type: "int", nullable: false, comment: "优先级。0最高，1其次，以此类推...。仅当节点类型0时有效。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WfTemplateNodeItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WfTemplateNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "流程Id"),
                    NextId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "下一个节点的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。"),
                    RejectOpertion = table.Column<byte>(type: "tinyint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "此节点的显示名。"),
                    GuardJsonString = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true, comment: " 前/后置守卫条件的Json字符串。暂未启用。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WfTemplateNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WfTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    DocTypeCode = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: true, comment: "文档类型Id。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个字符，仅支持英文。"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "此流程的显示名。"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者的唯一标识"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建的时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WfTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WfTemplateNodeItems_ParentId",
                table: "WfTemplateNodeItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WfTemplateNodes_NextId",
                table: "WfTemplateNodes",
                column: "NextId");

            migrationBuilder.CreateIndex(
                name: "IX_WfTemplateNodes_ParentId",
                table: "WfTemplateNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WfTemplates_OrgId",
                table: "WfTemplates",
                column: "OrgId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WfTemplateNodeItems");

            migrationBuilder.DropTable(
                name: "WfTemplateNodes");

            migrationBuilder.DropTable(
                name: "WfTemplates");
        }
    }
}
