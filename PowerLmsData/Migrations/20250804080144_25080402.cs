using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25080402 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoucherSequences",
                columns: table => new
                {
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "组织机构ID，多租户隔离"),
                    Month = table.Column<int>(type: "int", nullable: false, comment: "月份"),
                    VoucherCharacter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "凭证字，直接存储避免关联银行信息"),
                    MaxSequence = table.Column<int>(type: "int", nullable: false, comment: "当前最大序号"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true, comment: "行版本，用于乐观锁控制"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建时间"),
                    LastUpdateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "最后更新时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoucherSequences", x => new { x.OrgId, x.Month, x.VoucherCharacter });
                },
                comment: "凭证序号管理表");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoucherSequences");
        }
    }
}
