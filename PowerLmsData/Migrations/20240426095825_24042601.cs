using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24042601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsSuccess",
                table: "OwWfNodeItems",
                type: "bit",
                nullable: true,
                comment: "是否审核通过",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否审核通过");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsSuccess",
                table: "OwWfNodeItems",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否审核通过",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldComment: "是否审核通过");
        }
    }
}
