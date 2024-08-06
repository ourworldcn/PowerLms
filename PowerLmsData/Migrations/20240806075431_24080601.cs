using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24080601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwSystemLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonObjectString = table.Column<string>(type: "varchar(max)", nullable: true),
                    ActionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "行为Id。如Logined , ShoppingBuy.xxxxxxxxxxxxxxxxxxxx==。"),
                    WorldDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "这个行为发生的世界时间。"),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "额外Guid。"),
                    ExtraString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "额外的字符串，通常行为Id，最长64字符。"),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "额外数字，具体意义取决于该条记录的类型。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwSystemLogs", x => x.Id);
                },
                comment: "通用数据库存储的日志实体对象。");

            migrationBuilder.CreateIndex(
                name: "IX_OwSystemLogs_ActionId_WorldDateTime",
                table: "OwSystemLogs",
                columns: new[] { "ActionId", "WorldDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_OwSystemLogs_WorldDateTime_ActionId",
                table: "OwSystemLogs",
                columns: new[] { "WorldDateTime", "ActionId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwSystemLogs");
        }
    }
}
