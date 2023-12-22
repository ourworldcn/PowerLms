using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23122201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "PlOrganizations",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id。没有父组织机构是顶层节点总公司，它的父是商户(MerchantId)",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属组织机构Id。没有父的组织机构是顶层节点即\"商户\"。");

            migrationBuilder.AddColumn<Guid>(
                name: "CreateBy",
                table: "PlOrganizations",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者的唯一标识");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlOrganizations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "创建的时间");

            migrationBuilder.AddColumn<string>(
                name: "Airlines_AirlineCode",
                table: "PlCustomers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true,
                comment: "航空公司2位代码（如国航为CA）");

            migrationBuilder.AddColumn<Guid>(
                name: "Airlines_DocumentsPlaceId",
                table: "PlCustomers",
                type: "uniqueidentifier",
                nullable: true,
                comment: "交单地，简单字典DocumentsPlace");

            migrationBuilder.AddColumn<string>(
                name: "Airlines_NumberCode",
                table: "PlCustomers",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true,
                comment: "3位，如国航999");

            migrationBuilder.AddColumn<Guid>(
                name: "Airlines_PayModeId",
                table: "PlCustomers",
                type: "uniqueidentifier",
                nullable: true,
                comment: "付款方式Id，关联简单字典BillPaymentMode");

            migrationBuilder.AddColumn<string>(
                name: "Airlines_PaymentPlace",
                table: "PlCustomers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "付款地点");

            migrationBuilder.AddColumn<bool>(
                name: "Airlines_SettlementModes",
                table: "PlCustomers",
                type: "bit",
                nullable: true,
                comment: "结算方式，cass=true/非Cass=false/空=null");

            migrationBuilder.AddColumn<Guid>(
                name: "CreateBy",
                table: "PlCustomers",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者的唯一标识");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlCustomers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "创建的时间");

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomsQuarantine",
                table: "PlCustomers",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否海关检疫");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "PlCustomers",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注");

            migrationBuilder.AddColumn<Guid>(
                name: "CreateBy",
                table: "Merchants",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者的唯一标识");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "Merchants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "创建的时间");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "DD_FeesTypes",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "默认单价",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComment: "默认单价");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "PlOrganizations");

            migrationBuilder.DropColumn(
                name: "Airlines_AirlineCode",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "Airlines_DocumentsPlaceId",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "Airlines_NumberCode",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "Airlines_PayModeId",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "Airlines_PaymentPlace",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "Airlines_SettlementModes",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "IsCustomsQuarantine",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "Merchants");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "PlOrganizations",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id。没有父的组织机构是顶层节点即\"商户\"。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属组织机构Id。没有父组织机构是顶层节点总公司，它的父是商户(MerchantId)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "DD_FeesTypes",
                type: "decimal(18,2)",
                nullable: false,
                comment: "默认单价",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "默认单价");
        }
    }
}
