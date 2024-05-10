using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24051001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "State",
                table: "OwWfs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "该工作流所处状态。0=0流转中，1=成功完成，2=已被终止。未来可能有其它状态。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "OwWfs");
        }
    }
}
