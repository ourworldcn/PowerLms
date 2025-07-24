using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25072401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "SettlementMethod",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "ExpenseDate",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuditOperatorId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "审核操作者Id。为空则表示未审核。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "审核操作者Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "审核时间。为空则表示未审核。审核通过后填写审核时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplyDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "申请时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "申请时间");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicantId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "申请人Id，员工账号Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "申请人Id，员工账号Id");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "OaExpenseRequisitions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "金额，两位小数");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "OaExpenseRequisitions",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "币种代码");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "OaExpenseRequisitions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                comment: "汇率，四位小数");

            migrationBuilder.AddColumn<string>(
                name: "ExpenseCategory",
                table: "OaExpenseRequisitions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "费用种类，选择日常费用种类，申请的费用种类申请人填写，不关联科目代码");

            migrationBuilder.AddColumn<byte>(
                name: "IncomeExpenseType",
                table: "OaExpenseRequisitions",
                type: "tinyint",
                nullable: true,
                comment: "收支类型，收款/付款");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "申请单Id，所属申请单Id，关联到OaExpenseRequisition的Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "申请单Id，所属申请单Id，关联到OaExpenseRequisition的Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "DailyFeesTypeId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "日常费用种类Id，关联到DailyFeesType的Id，财务选择正确的费用种类",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "日常费用种类Id，关联到DailyFeesType的Id");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "OaExpenseRequisitionItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "金额，此明细项的金额，两位小数");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "部门Id，选择系统中的组织架构部门，关联到PlOrganization的Id");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "员工Id，费用可能核算到不同员工名下，关联到Account的Id");

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "OaExpenseRequisitionItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "序号，用于明细表的排序显示");

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementAccountId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "结算账号Id，关联到BankInfo的Id，统一的账号选择");

            migrationBuilder.AddColumn<DateTime>(
                name: "SettlementDateTime",
                table: "OaExpenseRequisitionItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "结算时间，财务人员处理时的结算时间，可控制凭证期间");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "OaExpenseRequisitionItems",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                comment: "摘要，财务填写的费用摘要说明");

            migrationBuilder.AddColumn<string>(
                name: "VoucherNumber",
                table: "OaExpenseRequisitionItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "凭证号，后台自动生成，格式：期间-凭证字-序号");

            migrationBuilder.AddColumn<string>(
                name: "VoucherCharacter",
                table: "BankInfos",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true,
                comment: "凭证字。专用于OA日常费用申请单的凭证号生成，如：银、现、转、记等。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "ExpenseCategory",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "IncomeExpenseType",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "SettlementAccountId",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "SettlementDateTime",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "VoucherNumber",
                table: "OaExpenseRequisitionItems");

            migrationBuilder.DropColumn(
                name: "VoucherCharacter",
                table: "BankInfos");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuditOperatorId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "审核操作者Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "审核操作者Id。为空则表示未审核。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "审核时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核时间。为空则表示未审核。审核通过后填写审核时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApplyDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "申请时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "申请时间");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicantId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "申请人Id，员工账号Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "申请人Id，员工账号Id");

            migrationBuilder.AddColumn<Guid>(
                name: "BankAccountId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "银行账户Id，当结算方式是银行时选择本公司信息中的银行账户id，审批流程成功完成后通过单独结算接口处理");

            migrationBuilder.AddColumn<byte>(
                name: "SettlementMethod",
                table: "OaExpenseRequisitions",
                type: "tinyint",
                nullable: true,
                comment: "结算方式，现金或银行转账，审批流程成功完成后通过单独结算接口处理");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "申请单Id，所属申请单Id，关联到OaExpenseRequisition的Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "申请单Id，所属申请单Id，关联到OaExpenseRequisition的Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "DailyFeesTypeId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "日常费用种类Id，关联到DailyFeesType的Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "日常费用种类Id，关联到DailyFeesType的Id，财务选择正确的费用种类");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "OaExpenseRequisitionItems",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "币种，标准货币缩写");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "OaExpenseRequisitionItems",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                comment: "汇率");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpenseDate",
                table: "OaExpenseRequisitionItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "费用发生时间");
        }
    }
}
