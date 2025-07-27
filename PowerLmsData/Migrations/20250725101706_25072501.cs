using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25072501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplyDateTime",
                table: "OaExpenseRequisitions");

            migrationBuilder.AlterColumn<string>(
                name: "TacCountNo",
                table: "PlCustomers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "财务编码。B账（外账）输出金蝶时的对接。",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "财务编码");

            migrationBuilder.AddColumn<string>(
                name: "FinanceCodeAP",
                table: "PlCustomers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "财务编码(AP)。该客户在金蝶系统中的供应商编码，用于应付类业务凭证生成。");

            migrationBuilder.AddColumn<string>(
                name: "FinanceCodeAR",
                table: "PlCustomers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "财务编码(AR)。该客户在金蝶系统中的客户编码，用于应收类业务凭证生成。");

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "OaExpenseRequisitions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注。长文本",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "备注，大文本");

            migrationBuilder.AlterColumn<string>(
                name: "RelatedCustomer",
                table: "OaExpenseRequisitions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "相关客户。字符串填写，可以从客户列表中选择",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "相关客户，字符串即可，不用从客户资料中选择");

            migrationBuilder.AlterColumn<string>(
                name: "ReceivingAccountNumber",
                table: "OaExpenseRequisitions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "收款人账号",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "收款账户");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLoan",
                table: "OaExpenseRequisitions",
                type: "bit",
                nullable: false,
                comment: "是否借款。true表示借款申请，false表示报销申请",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否借款，true表示借款申请，false表示报销申请");

            migrationBuilder.AlterColumn<bool>(
                name: "IsImportFinancialSoftware",
                table: "OaExpenseRequisitions",
                type: "bit",
                nullable: false,
                comment: "是否导入财务软件。作为后期导入财务软件的导入条件",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否导入财务软件，作为后期金蝶财务软件导入条件");

            migrationBuilder.AlterColumn<byte>(
                name: "IncomeExpenseType",
                table: "OaExpenseRequisitions",
                type: "tinyint",
                nullable: true,
                comment: "收支类型（收款/付款）",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true,
                oldComment: "收支类型，收款/付款");

            migrationBuilder.AlterColumn<string>(
                name: "ExpenseCategory",
                table: "OaExpenseRequisitions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "费用类别。选择日常费用的类别，由后台费用类别管理配置维护，可以类别编码项",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "费用种类，选择日常费用种类，申请的费用种类申请人填写，不关联科目代码");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "OaExpenseRequisitions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "汇率（两位小数）",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "汇率，四位小数");

            migrationBuilder.AlterColumn<string>(
                name: "DiscussedMatters",
                table: "OaExpenseRequisitions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "洽谈事项。长文本",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "所谈事项，大文本");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建时间（即申请时间）",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreateBy",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者Id（即登记人Id）",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "创建者Id，即登记人Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuditOperatorId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "审核操作员Id。为空表示未审核",
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
                comment: "审核时间。为空表示未审核。一般通过后台填写审核时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "审核时间。为空则表示未审核。审核通过后填写审核时间。");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicantId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "申请人Id（员工账号Id）",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "申请人Id，员工账号Id");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "OaExpenseRequisitions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "金额（两位小数）",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "金额，两位小数");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "财务部门Id。关联简单字典finance-depart类型，用于金蝶核算部门",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "部门Id，选择系统中的组织架构部门，关联到PlOrganization的Id");

            migrationBuilder.AddColumn<string>(
                name: "FinanceCode",
                table: "Accounts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "财务编码。金蝶系统中该员工的唯一编码，用于员工核算相关凭证生成。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinanceCodeAP",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "FinanceCodeAR",
                table: "PlCustomers");

            migrationBuilder.DropColumn(
                name: "FinanceCode",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "TacCountNo",
                table: "PlCustomers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "财务编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "财务编码。B账（外账）输出金蝶时的对接。");

            migrationBuilder.AlterColumn<string>(
                name: "Remark",
                table: "OaExpenseRequisitions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "备注，大文本",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "备注。长文本");

            migrationBuilder.AlterColumn<string>(
                name: "RelatedCustomer",
                table: "OaExpenseRequisitions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "相关客户，字符串即可，不用从客户资料中选择",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "相关客户。字符串填写，可以从客户列表中选择");

            migrationBuilder.AlterColumn<string>(
                name: "ReceivingAccountNumber",
                table: "OaExpenseRequisitions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "收款账户",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "收款人账号");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLoan",
                table: "OaExpenseRequisitions",
                type: "bit",
                nullable: false,
                comment: "是否借款，true表示借款申请，false表示报销申请",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否借款。true表示借款申请，false表示报销申请");

            migrationBuilder.AlterColumn<bool>(
                name: "IsImportFinancialSoftware",
                table: "OaExpenseRequisitions",
                type: "bit",
                nullable: false,
                comment: "是否导入财务软件，作为后期金蝶财务软件导入条件",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "是否导入财务软件。作为后期导入财务软件的导入条件");

            migrationBuilder.AlterColumn<byte>(
                name: "IncomeExpenseType",
                table: "OaExpenseRequisitions",
                type: "tinyint",
                nullable: true,
                comment: "收支类型，收款/付款",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true,
                oldComment: "收支类型（收款/付款）");

            migrationBuilder.AlterColumn<string>(
                name: "ExpenseCategory",
                table: "OaExpenseRequisitions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "费用种类，选择日常费用种类，申请的费用种类申请人填写，不关联科目代码",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "费用类别。选择日常费用的类别，由后台费用类别管理配置维护，可以类别编码项");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "OaExpenseRequisitions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "汇率，四位小数",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "汇率（两位小数）");

            migrationBuilder.AlterColumn<string>(
                name: "DiscussedMatters",
                table: "OaExpenseRequisitions",
                type: "nvarchar(max)",
                nullable: true,
                comment: "所谈事项，大文本",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "洽谈事项。长文本");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建时间（即申请时间）");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreateBy",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者Id，即登记人Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "创建者Id（即登记人Id）");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuditOperatorId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "审核操作者Id。为空则表示未审核。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "审核操作员Id。为空表示未审核");

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
                oldComment: "审核时间。为空表示未审核。一般通过后台填写审核时间");

            migrationBuilder.AlterColumn<Guid>(
                name: "ApplicantId",
                table: "OaExpenseRequisitions",
                type: "uniqueidentifier",
                nullable: true,
                comment: "申请人Id，员工账号Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "申请人Id（员工账号Id）");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "OaExpenseRequisitions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "金额，两位小数",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "金额（两位小数）");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplyDateTime",
                table: "OaExpenseRequisitions",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "申请时间");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "OaExpenseRequisitionItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "部门Id，选择系统中的组织架构部门，关联到PlOrganization的Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "财务部门Id。关联简单字典finance-depart类型，用于金蝶核算部门");
        }
    }
}
