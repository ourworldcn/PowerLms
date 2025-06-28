using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25062801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubjectConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id"),
                    Code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false, comment: "科目编码"),
                    SubjectNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false, comment: "科目号（会计科目编号）"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "显示名称"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者的唯一标识"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "创建的时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectConfigurations", x => x.Id);
                },
                comment: "财务科目设置表");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectConfigurations_OrgId_Code",
                table: "SubjectConfigurations",
                columns: new[] { "OrgId", "Code" },
                unique: true,
                filter: "[OrgId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectConfigurations");
        }
    }
}
