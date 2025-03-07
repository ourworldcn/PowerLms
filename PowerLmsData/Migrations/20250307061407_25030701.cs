using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25030701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwAppLogVO");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "TaxInvoiceChannels");

            migrationBuilder.AlterColumn<string>(
                name: "FormatString",
                table: "OwAppLogStores",
                type: "nvarchar(max)",
                nullable: true,
                comment: "格式字符串。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LogLevel",
                table: "OwAppLogStores",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Trace (0)：包含最详细消息的日志，可能包含敏感数据，默认禁用，不应在生产环境中启用。Debug (1)：用于开发过程中的交互式调查日志，包含对调试有用的信息，无长期价值。Information (2)：跟踪应用程序常规流的日志，具有长期价值。Warning (3)：突出显示异常或意外事件的日志，不会导致应用程序停止。Error (4)：当前执行流因故障而停止时的日志，指示当前活动中的故障。Critical (5)：描述不可恢复的应用程序/系统崩溃或需要立即注意的灾难性故障的日志。None (6)：不用于写入日志消息，指定日志记录类别不应写入任何消息。");

            migrationBuilder.InsertData(
                table: "OwAppLogStores",
                columns: new[] { "Id", "FormatString", "LogLevel" },
                values: new object[] { new Guid("e410bc88-71b2-4530-9993-c0c0b1105617"), "用户:{LoginName}({CompanyName}){OperatorName}成功", 2 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OwAppLogStores",
                keyColumn: "Id",
                keyValue: new Guid("e410bc88-71b2-4530-9993-c0c0b1105617"));

            migrationBuilder.DropColumn(
                name: "LogLevel",
                table: "OwAppLogStores");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "TaxInvoiceChannels",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构的Id。");

            migrationBuilder.AlterColumn<string>(
                name: "FormatString",
                table: "OwAppLogStores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "格式字符串。");

            migrationBuilder.CreateTable(
                name: "OwAppLogVO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtraBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FormatString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParamstersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwAppLogVO", x => x.Id);
                });
        }
    }
}
