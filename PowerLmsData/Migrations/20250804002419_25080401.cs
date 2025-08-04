using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25080401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VoucherGroup",
                table: "SubjectConfigurations",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "凭证类别字",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true,
                oldComment: "凭证类别字");

            migrationBuilder.AlterColumn<string>(
                name: "SubjectNumber",
                table: "SubjectConfigurations",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                comment: "会计科目编码",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldComment: "会计科目编码");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "SubjectConfigurations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "显示名称。",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldComment: "显示名称。");

            migrationBuilder.AlterColumn<string>(
                name: "AccountingCategory",
                table: "SubjectConfigurations",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true,
                comment: "核算类别",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "核算类别");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreateBy",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者Id。统一记录创建人、登记人、申请人信息",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "创建者Id（即登记人Id）");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicantId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "申请人Id（员工账号Id）。已废弃字段，请使用CreateBy",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "申请人Id（员工账号Id）");

            migrationBuilder.AddColumn<string>(
                name: "BankFlowNumber",
                table: "OaExpenseRequisitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "银行流水号，用于确认的银行流水号");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "确认时间，会计执行确认操作的时间");

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmOperatorId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "确认操作人Id，执行确认操作的会计人员Id");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmRemark",
                table: "OaExpenseRequisitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "确认备注，确认相关的备注说明");

            migrationBuilder.AddColumn<DateTime>(
                name: "SettlementDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "结算时间，出纳执行结算操作的时间");

            migrationBuilder.AddColumn<string>(
                name: "SettlementMethod",
                table: "OaExpenseRequisitions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "结算方式，现金或银行转账等结算方式说明");

            migrationBuilder.AddColumn<Guid>(
                name: "SettlementOperatorId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "结算操作人Id，执行结算操作的出纳人员Id");

            migrationBuilder.AddColumn<string>(
                name: "SettlementRemark",
                table: "OaExpenseRequisitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "结算备注，结算相关的备注说明");

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "OaExpenseRequisitions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "申请单状态，采用二进制位值");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankFlowNumber",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "ConfirmDateTime",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "ConfirmOperatorId",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "ConfirmRemark",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "SettlementDateTime",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "SettlementMethod",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "SettlementOperatorId",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "SettlementRemark",
                table: "OaExpenseRequisitions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OaExpenseRequisitions");

            migrationBuilder.AlterColumn<string>(
                name: "VoucherGroup",
                table: "SubjectConfigurations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                comment: "凭证类别字",
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "凭证类别字");

            migrationBuilder.AlterColumn<string>(
                name: "SubjectNumber",
                table: "SubjectConfigurations",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                comment: "会计科目编码",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "会计科目编码");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "SubjectConfigurations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                comment: "显示名称。",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "显示名称。");

            migrationBuilder.AlterColumn<string>(
                name: "AccountingCategory",
                table: "SubjectConfigurations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "核算类别",
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "核算类别");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreateBy",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者Id（即登记人Id）",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "创建者Id。统一记录创建人、登记人、申请人信息");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicantId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "申请人Id（员工账号Id）",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "申请人Id（员工账号Id）。已废弃字段，请使用CreateBy");
        }
    }
}
