using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24042301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectOpertion",
                table: "WfTemplateNodes");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "WfTemplateNodeItems");

            migrationBuilder.RenameColumn(
                name: "DocTypeCode",
                table: "WfTemplates",
                newName: "KindCode");

            migrationBuilder.AlterTable(
                name: "WfTemplates",
                comment: "流程模板总表");

            migrationBuilder.AlterTable(
                name: "WfTemplateNodes",
                comment: "工作流模板内节点表");

            migrationBuilder.AlterTable(
                name: "WfTemplateNodeItems",
                comment: "节点详细信息类");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "WfTemplateNodes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "流程模板Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "流程Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "NextId",
                table: "WfTemplateNodes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "下一个操作人的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。为null标识最后一个节点。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "下一个节点的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。");

            migrationBuilder.AddColumn<byte>(
                name: "RejectOperation",
                table: "WfTemplateNodes",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "拒绝后的操作，1 = 终止,2=回退");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "WfTemplateNodeItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属节点Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "所属节点Id。");

            migrationBuilder.AddColumn<Guid>(
                name: "OpertorId",
                table: "WfTemplateNodeItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "操作人Id。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectOperation",
                table: "WfTemplateNodes");

            migrationBuilder.DropColumn(
                name: "OpertorId",
                table: "WfTemplateNodeItems");

            migrationBuilder.RenameColumn(
                name: "KindCode",
                table: "WfTemplates",
                newName: "DocTypeCode");

            migrationBuilder.AlterTable(
                name: "WfTemplates",
                oldComment: "流程模板总表");

            migrationBuilder.AlterTable(
                name: "WfTemplateNodes",
                oldComment: "工作流模板内节点表");

            migrationBuilder.AlterTable(
                name: "WfTemplateNodeItems",
                oldComment: "节点详细信息类");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "WfTemplateNodes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "流程Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "流程模板Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "NextId",
                table: "WfTemplateNodes",
                type: "uniqueidentifier",
                nullable: true,
                comment: "下一个节点的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "下一个操作人的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。为null标识最后一个节点。");

            migrationBuilder.AddColumn<byte>(
                name: "RejectOpertion",
                table: "WfTemplateNodes",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "WfTemplateNodeItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "所属节点Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属节点Id。");

            migrationBuilder.AddColumn<Guid>(
                name: "ActorId",
                table: "WfTemplateNodeItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "参与者(员工)Id。");
        }
    }
}
