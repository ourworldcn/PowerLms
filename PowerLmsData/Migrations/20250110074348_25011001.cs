using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25011001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "BillingInfo_IsNeedTrace",
                table: "PlCustomers",
                type: "bit",
                nullable: true,
                comment: "是否特别注意",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否特别注意");

            migrationBuilder.AlterColumn<bool>(
                name: "BillingInfo_IsCEBlack",
                table: "PlCustomers",
                type: "bit",
                nullable: true,
                comment: "是否超额黑名单",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否超额黑名单");

            migrationBuilder.AlterColumn<bool>(
                name: "BillingInfo_IsBlack",
                table: "PlCustomers",
                type: "bit",
                nullable: true,
                comment: "是否超期黑名单",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否超期黑名单");

            migrationBuilder.AlterColumn<int>(
                name: "BillingInfo_Dayslimited",
                table: "PlCustomers",
                type: "int",
                nullable: true,
                comment: "信用期限天数",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "信用期限天数");

            migrationBuilder.AlterColumn<decimal>(
                name: "BillingInfo_AmountLimited",
                table: "PlCustomers",
                type: "decimal(18,2)",
                nullable: true,
                comment: "拖欠金额",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComment: "拖欠金额");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "BillingInfo_IsNeedTrace",
                table: "PlCustomers",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否特别注意",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldComment: "是否特别注意");

            migrationBuilder.AlterColumn<bool>(
                name: "BillingInfo_IsCEBlack",
                table: "PlCustomers",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否超额黑名单",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldComment: "是否超额黑名单");

            migrationBuilder.AlterColumn<bool>(
                name: "BillingInfo_IsBlack",
                table: "PlCustomers",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否超期黑名单",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldComment: "是否超期黑名单");

            migrationBuilder.AlterColumn<int>(
                name: "BillingInfo_Dayslimited",
                table: "PlCustomers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "信用期限天数",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "信用期限天数");

            migrationBuilder.AlterColumn<decimal>(
                name: "BillingInfo_AmountLimited",
                table: "PlCustomers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "拖欠金额",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true,
                oldComment: "拖欠金额");
        }
    }
}
