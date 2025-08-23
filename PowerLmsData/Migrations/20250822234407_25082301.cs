using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25082301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "PlCustomerTidans",
                comment: "客户提单内容表");

            migrationBuilder.AlterTable(
                name: "PlCustomers",
                comment: "客户资料");

            migrationBuilder.AlterTable(
                name: "PlCustomerLoadingAddrs",
                comment: "装货地址");

            migrationBuilder.AlterTable(
                name: "PlCustomerContacts",
                comment: "客户资料的联系人");

            migrationBuilder.AlterTable(
                name: "PlCustomerBusinessHeaders",
                comment: "业务负责人表");

            migrationBuilder.AlterTable(
                name: "DD_UnitConversions",
                comment: "单位换算");

            migrationBuilder.AlterTable(
                name: "CustomerBlacklists",
                comment: "黑名单客户跟踪表");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "PlCustomerTidans",
                oldComment: "客户提单内容表");

            migrationBuilder.AlterTable(
                name: "PlCustomers",
                oldComment: "客户资料");

            migrationBuilder.AlterTable(
                name: "PlCustomerLoadingAddrs",
                oldComment: "装货地址");

            migrationBuilder.AlterTable(
                name: "PlCustomerContacts",
                oldComment: "客户资料的联系人");

            migrationBuilder.AlterTable(
                name: "PlCustomerBusinessHeaders",
                oldComment: "业务负责人表");

            migrationBuilder.AlterTable(
                name: "DD_UnitConversions",
                oldComment: "单位换算");

            migrationBuilder.AlterTable(
                name: "CustomerBlacklists",
                oldComment: "黑名单客户跟踪表");
        }
    }
}
