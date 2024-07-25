using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24072501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DD_ShippingContainersKinds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", maxLength: 64, nullable: false, comment: "箱型"),
                    Size = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "尺寸"),
                    Teu = table.Column<byte>(type: "tinyint", nullable: false, comment: "TEU"),
                    Kgs = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "皮重。单位Kg。"),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id"),
                    ShortcutName = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true, comment: "快捷输入名"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_ShippingContainersKinds", x => x.Id);
                },
                comment: "集装箱类型");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_ShippingContainersKinds");
        }
    }
}
