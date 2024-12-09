using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24120902 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "DocFees",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "本位币汇率,默认从汇率表调取,机构本位币",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComment: "本位币汇率,默认从汇率表调取,机构本位币");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "DocFees",
                type: "decimal(18,2)",
                nullable: false,
                comment: "本位币汇率,默认从汇率表调取,机构本位币",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "本位币汇率,默认从汇率表调取,机构本位币");
        }
    }
}
