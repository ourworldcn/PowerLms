using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24040301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MblNo",
                table: "PlJobs",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "主单号",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "主单号");

            migrationBuilder.AddColumn<decimal>(
                name: "M_MeasureMent",
                table: "PlEaDocs",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                comment: "主单体积，3位小数");

            migrationBuilder.CreateTable(
                name: "DocFeeRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "申请单Id"),
                    FeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "绑定的费用Id"),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次申请金额")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocFeeRequisitionItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocFeeRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FrNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "申请单号,其他编码规则中【申请单号】自动生成"),
                    IO = table.Column<bool>(type: "bit", nullable: false, comment: "收付，false支出，true收入。"),
                    MakerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "制单人Id。员工Id。"),
                    MakeDateTime = table.Column<DateTime>(type: "datetime2(2)", nullable: true, comment: "制单时间"),
                    ApplyTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "结算类型。简单字典ApplyType"),
                    BalanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "结算单位Id。客户资料中选择"),
                    BlanceAccountNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "结算单位账号,选择后可修改"),
                    Bank = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "结算单位开户行，选择后可修改"),
                    Contact = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "结算单位联系人，选择后可修改"),
                    Tel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "结算单位联系人电话"),
                    IsNeedInvoice = table.Column<bool>(type: "bit", nullable: false, comment: "要求开发票,true=要求，false=未要求。"),
                    InvoiceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "发票类型Id，简单字典InvoiceType"),
                    InvoiceTitle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "发票抬头"),
                    PreReturnDate = table.Column<DateTime>(type: "datetime2(2)", nullable: true, comment: "预计回款时间"),
                    ReturnDate = table.Column<DateTime>(type: "datetime2(2)", nullable: true, comment: "实际回款时间"),
                    Currency = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "币种。标准货币缩写。"),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "金额,可以不用实体字段，明细合计显示也行."),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocFeeRequisitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DD_OtherNumberRules_OrgId_Code",
                table: "DD_OtherNumberRules",
                columns: new[] { "OrgId", "Code" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocFeeRequisitionItems");

            migrationBuilder.DropTable(
                name: "DocFeeRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_DD_OtherNumberRules_OrgId_Code",
                table: "DD_OtherNumberRules");

            migrationBuilder.DropColumn(
                name: "M_MeasureMent",
                table: "PlEaDocs");

            migrationBuilder.AlterColumn<string>(
                name: "MblNo",
                table: "PlJobs",
                type: "nvarchar(max)",
                nullable: true,
                comment: "主单号",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "主单号");
        }
    }
}
