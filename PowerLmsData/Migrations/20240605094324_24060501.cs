using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24060501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "OwWfs",
                type: "tinyint",
                nullable: false,
                comment: "该工作流所处状态。0=流转中，1=成功完成，2=已被终止。未来可能有其它状态。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "该工作流所处状态。0=0流转中，1=成功完成，2=已被终止。未来可能有其它状态。");

            migrationBuilder.AddColumn<string>(
                name: "DCurrency",
                table: "DD_PlExchangeRates",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "宿币种码");

            migrationBuilder.AddColumn<string>(
                name: "SCurrency",
                table: "DD_PlExchangeRates",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "源币种码");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DCurrency",
                table: "DD_PlExchangeRates");

            migrationBuilder.DropColumn(
                name: "SCurrency",
                table: "DD_PlExchangeRates");

            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "OwWfs",
                type: "tinyint",
                nullable: false,
                comment: "该工作流所处状态。0=0流转中，1=成功完成，2=已被终止。未来可能有其它状态。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "该工作流所处状态。0=流转中，1=成功完成，2=已被终止。未来可能有其它状态。");
        }
    }
}
