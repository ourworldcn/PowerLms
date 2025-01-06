using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25010601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "DocFeeTemplates");

            migrationBuilder.AlterColumn<byte>(
                name: "JobState",
                table: "PlJobs",
                type: "tinyint",
                nullable: false,
                comment: "工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已完成=16.",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "PlIaDocs",
                type: "tinyint",
                nullable: false,
                comment: "操作状态。0=初始化单据但尚未操作，,已调单=1,已申报=2,已出税=4,海关已放行=8,已入库=16,仓库已放行=128(视同已通知财务)。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "操作状态。0=初始化单据但尚未操作，,已调单=1,已申报=2,已出税=4,海关已放行=8,已入库=16,仓库已放行=128。");

            migrationBuilder.AddColumn<Guid>(
                name: "JobTypeId",
                table: "DocFeeTemplates",
                type: "uniqueidentifier",
                nullable: true,
                comment: "业务种类id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobTypeId",
                table: "DocFeeTemplates");

            migrationBuilder.AlterColumn<byte>(
                name: "JobState",
                table: "PlJobs",
                type: "tinyint",
                nullable: false,
                comment: "工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已完成=16.");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "PlIaDocs",
                type: "tinyint",
                nullable: false,
                comment: "操作状态。0=初始化单据但尚未操作，,已调单=1,已申报=2,已出税=4,海关已放行=8,已入库=16,仓库已放行=128。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "操作状态。0=初始化单据但尚未操作，,已调单=1,已申报=2,已出税=4,海关已放行=8,已入库=16,仓库已放行=128(视同已通知财务)。");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "DocFeeTemplates",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
