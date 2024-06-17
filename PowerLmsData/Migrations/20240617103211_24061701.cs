using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24061701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "OwWfs",
                type: "tinyint",
                nullable: false,
                comment: "该工作流所处状态。0=流转中，1=成功完成，2=已被终止（失败）。未来可能有其它状态。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "该工作流所处状态。0=流转中，1=成功完成，2=已被终止。未来可能有其它状态。");

            migrationBuilder.AddColumn<Guid>(
                name: "FirstNodeId",
                table: "OwWfs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFeeRequisitions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "金额,所有子项的金额的求和。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "金额,可以不用实体字段，明细合计显示也行.");

            migrationBuilder.AlterColumn<Guid>(
                name: "SCurrencyId",
                table: "DD_PlExchangeRates",
                type: "uniqueidentifier",
                nullable: false,
                comment: "源币种.废弃，请使用SCurrency属性。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "源币种");

            migrationBuilder.AlterColumn<Guid>(
                name: "DCurrencyId",
                table: "DD_PlExchangeRates",
                type: "uniqueidentifier",
                nullable: false,
                comment: "宿币种。废弃，请使用 DCurrency 属性。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "宿币种");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstNodeId",
                table: "OwWfs");

            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "OwWfs",
                type: "tinyint",
                nullable: false,
                comment: "该工作流所处状态。0=流转中，1=成功完成，2=已被终止。未来可能有其它状态。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "该工作流所处状态。0=流转中，1=成功完成，2=已被终止（失败）。未来可能有其它状态。");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFeeRequisitions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "金额,可以不用实体字段，明细合计显示也行.",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "金额,所有子项的金额的求和。");

            migrationBuilder.AlterColumn<Guid>(
                name: "SCurrencyId",
                table: "DD_PlExchangeRates",
                type: "uniqueidentifier",
                nullable: false,
                comment: "源币种",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "源币种.废弃，请使用SCurrency属性。");

            migrationBuilder.AlterColumn<Guid>(
                name: "DCurrencyId",
                table: "DD_PlExchangeRates",
                type: "uniqueidentifier",
                nullable: false,
                comment: "宿币种",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "宿币种。废弃，请使用 DCurrency 属性。");
        }
    }
}
