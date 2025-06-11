using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25061101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwTaskStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceTypeName = table.Column<string>(type: "nvarchar(450)", nullable: true, comment: "要执行的服务类型的完整名称"),
                    MethodName = table.Column<string>(type: "nvarchar(450)", nullable: true, comment: "要执行的方法名称"),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "任务参数，JSON格式的字符串"),
                    StatusValue = table.Column<byte>(type: "tinyint", nullable: false, comment: "任务当前执行状态值"),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "任务创建时间，UTC格式，精确到毫秒"),
                    StartUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "任务开始执行时间，UTC格式，精确到毫秒"),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "任务完成时间，UTC格式，精确到毫秒"),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "任务执行结果，JSON格式的字符串"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "任务执行失败时的错误信息"),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "创建此任务的用户ID"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "任务所属的租户ID")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwTaskStores", x => x.Id);
                },
                comment: "长时间运行任务的存储实体");

            migrationBuilder.CreateIndex(
                name: "IX_OwTaskStores_CreatorId",
                table: "OwTaskStores",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_OwTaskStores_ServiceTypeName_MethodName",
                table: "OwTaskStores",
                columns: new[] { "ServiceTypeName", "MethodName" });

            migrationBuilder.CreateIndex(
                name: "IX_OwTaskStores_StatusValue",
                table: "OwTaskStores",
                column: "StatusValue");

            migrationBuilder.CreateIndex(
                name: "IX_OwTaskStores_TenantId",
                table: "OwTaskStores",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwTaskStores");
        }
    }
}
