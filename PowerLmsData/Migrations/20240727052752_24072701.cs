using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24072701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "DD_ShippingContainersKinds",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "箱型",
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 64,
                oldComment: "箱型");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Kind",
                table: "DD_ShippingContainersKinds",
                type: "int",
                maxLength: 64,
                nullable: false,
                defaultValue: 0,
                comment: "箱型",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "箱型");
        }
    }
}
