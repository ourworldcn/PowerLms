using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24032601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNo",
                table: "DocFees");

            migrationBuilder.AlterColumn<byte>(
                name: "JobState",
                table: "PlJobs",
                type: "tinyint",
                nullable: false,
                comment: "工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "工作状态。NewJob初始=0，Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.");

            migrationBuilder.AddColumn<Guid>(
                name: "BillId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "账单表中的id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillId",
                table: "DocFees");

            migrationBuilder.AlterColumn<byte>(
                name: "JobState",
                table: "PlJobs",
                type: "tinyint",
                nullable: false,
                comment: "工作状态。NewJob初始=0，Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "工作状态。Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.");

            migrationBuilder.AddColumn<string>(
                name: "AccountNo",
                table: "DocFees",
                type: "nvarchar(max)",
                nullable: true,
                comment: "账单号，账单表中的id");
        }
    }
}
