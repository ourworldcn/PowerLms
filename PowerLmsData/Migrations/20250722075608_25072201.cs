using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25072201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DD_DailyFeesTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectCode = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true, comment: "会计科目代码"),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id"),
                    ShortcutName = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true, comment: "快捷输入名"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。"),
                    Code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DD_DailyFeesTypes", x => x.Id);
                },
                comment: "日常费用种类字典");

            migrationBuilder.CreateTable(
                name: "OaExpenseRequisitionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "申请单Id，所属申请单Id，关联到OaExpenseRequisition的Id"),
                    DailyFeesTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "日常费用种类Id，关联到DailyFeesType的Id"),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "费用发生时间"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "发票号"),
                    Currency = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "币种，标准货币缩写"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, comment: "汇率"),
                    Remark = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OaExpenseRequisitionItems", x => x.Id);
                },
                comment: "OA费用申请单明细表");

            migrationBuilder.CreateTable(
                name: "OaExpenseRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id"),
                    ApplicantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "申请人Id，员工账号Id"),
                    ApplyDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "申请时间"),
                    IsLoan = table.Column<bool>(type: "bit", nullable: false, comment: "是否借款，true表示借款申请，false表示报销申请"),
                    IsImportFinancialSoftware = table.Column<bool>(type: "bit", nullable: false, comment: "是否导入财务软件，作为后期金蝶财务软件导入条件"),
                    RelatedCustomer = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "相关客户，字符串即可，不用从客户资料中选择"),
                    ReceivingBank = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "收款银行"),
                    ReceivingAccountName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "收款户名"),
                    ReceivingAccountNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "收款账户"),
                    DiscussedMatters = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "所谈事项，大文本"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注，大文本"),
                    SettlementMethod = table.Column<byte>(type: "tinyint", nullable: true, comment: "结算方式，现金或银行转账，审批流程成功完成后通过单独结算接口处理"),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "银行账户Id，当结算方式是银行时选择本公司信息中的银行账户id，审批流程成功完成后通过单独结算接口处理"),
                    AuditDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "审核时间"),
                    AuditOperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "审核操作者Id"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者Id，即登记人Id"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "创建的时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OaExpenseRequisitions", x => x.Id);
                },
                comment: "OA日常费用申请单主表");

            migrationBuilder.CreateIndex(
                name: "IX_OaExpenseRequisitionItems_ParentId",
                table: "OaExpenseRequisitionItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OaExpenseRequisitions_OrgId",
                table: "OaExpenseRequisitions",
                column: "OrgId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DD_DailyFeesTypes");

            migrationBuilder.DropTable(
                name: "OaExpenseRequisitionItems");

            migrationBuilder.DropTable(
                name: "OaExpenseRequisitions");
        }
    }
}
