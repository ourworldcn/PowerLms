using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24042502 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwWfs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属模板的Id。"),
                    DocId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "流程文档Id。"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwWfs", x => x.Id);
                },
                comment: "流程实例顶层类。");

            migrationBuilder.CreateTable(
                name: "OwWfNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "流程Id"),
                    ArrivalDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "到达此节点的时间，如果是第一个节点则是创建并保存节点的时间。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwWfNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwWfNodes_OwWfs_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OwWfs",
                        principalColumn: "Id");
                },
                comment: "记录工作流实例节点的表");

            migrationBuilder.CreateTable(
                name: "OwWfNodeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "流程节点Id"),
                    OpertorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "文档当前操作人的Id。"),
                    OpertorDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "这里冗余额外记录一个操作人的显示名称。可随时更改。"),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "审核批示。对非审批人，则是意见。"),
                    OperationKind = table.Column<byte>(type: "tinyint", nullable: false, comment: "操作人类型，目前保留为0(审批者)。预计1=抄送人。"),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, comment: "是否审核通过"),
                    OwWfNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwWfNodeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwWfNodeItems_OwWfNodes_OwWfNodeId",
                        column: x => x.OwWfNodeId,
                        principalTable: "OwWfNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OwWfNodeItems_OwWfs_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OwWfs",
                        principalColumn: "Id");
                },
                comment: "工作流实例节点详细信息。");

            migrationBuilder.CreateIndex(
                name: "IX_OwWfNodeItems_OpertorId",
                table: "OwWfNodeItems",
                column: "OpertorId");

            migrationBuilder.CreateIndex(
                name: "IX_OwWfNodeItems_OwWfNodeId",
                table: "OwWfNodeItems",
                column: "OwWfNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_OwWfNodeItems_ParentId",
                table: "OwWfNodeItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OwWfNodes_ParentId",
                table: "OwWfNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OwWfs_DocId",
                table: "OwWfs",
                column: "DocId");

            migrationBuilder.CreateIndex(
                name: "IX_OwWfs_TemplateId",
                table: "OwWfs",
                column: "TemplateId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwWfNodeItems");

            migrationBuilder.DropTable(
                name: "OwWfNodes");

            migrationBuilder.DropTable(
                name: "OwWfs");
        }
    }
}
