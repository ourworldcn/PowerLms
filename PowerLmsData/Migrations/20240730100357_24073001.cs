using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24073001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperateState",
                table: "PlJobs");

            migrationBuilder.DropColumn(
                name: "OperationalStatus",
                table: "PlIaDocs");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "PlIsDocs",
                type: "tinyint",
                nullable: false,
                comment: "操作状态。0=初始化单据但尚未操作，1=已换单,2=已申报,4=海关已放行,8=已提箱，128=已提货。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "操作状态。1=已换单,2=船已到港,4=卸货完成,8=已提货。");

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "PlIaDocs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "操作状态。0=初始化单据但尚未操作，,已调单=1,已申报=2,已出税=4,海关已放行=8,已入库=16,仓库已放行=128。");

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "PlEaDocs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "操作状态。NewJob初始=0,Arrived 已到货=1,Declared 已申报=2,Delivered 已配送=4,Submitted 已交单=8,Notified 已通知=128");

            migrationBuilder.CreateTable(
                name: "ContainerKindCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属业务单据Id。"),
                    Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "箱型。"),
                    Count = table.Column<int>(type: "int", nullable: false, comment: "数量。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerKindCounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerKindCounts_ParentId",
                table: "ContainerKindCounts",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerKindCounts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PlIaDocs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PlEaDocs");

            migrationBuilder.AddColumn<byte>(
                name: "OperateState",
                table: "PlJobs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "操作状态。NewJob初始=0,Arrived 已到货=2,Declared 已申报=4,Delivered 已配送=8,Submitted 已交单=16,Notified 已通知=32");

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "PlIsDocs",
                type: "tinyint",
                nullable: false,
                comment: "操作状态。1=已换单,2=船已到港,4=卸货完成,8=已提货。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "操作状态。0=初始化单据但尚未操作，1=已换单,2=已申报,4=海关已放行,8=已提箱，128=已提货。");

            migrationBuilder.AddColumn<byte>(
                name: "OperationalStatus",
                table: "PlIaDocs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "操作状态。初始化单据（此时未经任何操作）=0,已调单=1,已申报=2,已出税=3,海关已放行=4,已入库=5,仓库已放行=6。");
        }
    }
}
