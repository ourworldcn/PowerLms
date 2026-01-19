using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26011901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "ContainerKindCounts",
                comment: "箱型箱量子表");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlRoles",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlRolePermissions",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlOrganizations",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OperatingDateTime",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "操作时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "操作时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Etd",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "开航日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "开航日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ETA",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "到港日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "到港日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDate",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "提送货日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "提送货日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CloseDate",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "关闭日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "关闭日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AccountDate",
                table: "PlJobs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "财务日期，前端自动计算设置，后端直接使用",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "财务日期，前端自动计算设置，后端直接使用");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpToDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "提货日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "提货日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "提货日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "提货日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ContainerFreeDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "免箱期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "免箱期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BillDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "实际换单日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "实际换单日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BargeStartDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "驳船开航日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "驳船开航日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivedDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "进口日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "进口日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AnticipateBillDateTime",
                table: "PlIsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "预计换单日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "预计换单日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "IoDateTime",
                table: "PlInvoicess",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "首付日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "首付日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FinanceDateTime",
                table: "PlInvoicess",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "财务日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "财务日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlInvoicess",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConfirmDateTime",
                table: "PlInvoicess",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "确认时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "确认时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "YujiXiaobaoDateTime",
                table: "PlIaDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "预计消保日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "预计消保日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "XiaobaoDateTime",
                table: "PlIaDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "实际消保日期，null表示未消保。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "实际消保日期，null表示未消保。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RukuDateTime",
                table: "PlIaDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "入库日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "入库日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PickUpDateTime",
                table: "PlIaDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "提货时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "提货时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlIaDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WarehousingDateTime",
                table: "PlEsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "放舱日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "放舱日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CutOffGoodsDateTime",
                table: "PlEsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "截货日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "截货日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CutOffDateTime",
                table: "PlEsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "截关日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "截关日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlEsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BookingsDateTime",
                table: "PlEsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "订舱日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "订舱日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BargeSailDateTime",
                table: "PlEsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "驳船开航日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "驳船开航日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BargeArrivalDateTime",
                table: "PlEsDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "驳船到港日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "驳船到港日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlEaDocs",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlCustomerTidans",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建时间。默认值为创建对象的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建时间。默认值为创建对象的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlCustomers",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlAccountRoles",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivalDateTime",
                table: "OwWfNodes",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "到达此节点的时间，如果是第一个节点则是创建并保存节点的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "到达此节点的时间，如果是第一个节点则是创建并保存节点的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocFeeTemplates",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DD_SimpleDataDics",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "创建时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "创建时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RepeatDate",
                table: "DD_OtherNumberRules",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "记录最后一次归零的日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "记录最后一次归零的日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Datetime",
                table: "CustomerBlacklists",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "执行时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "执行时间");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "ContainerKindCounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属业务单据Id",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属业务单据Id。");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "ContainerKindCounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "箱型",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "箱型。");

            migrationBuilder.AlterColumn<int>(
                name: "Count",
                table: "ContainerKindCounts",
                type: "int",
                nullable: false,
                comment: "数量",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "数量。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "ActualFinancialTransactions",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "收付款日期，实际发生收付款的业务日期，精确到毫秒",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "收付款日期，实际发生收付款的业务日期，精确到毫秒");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "ActualFinancialTransactions",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建时间，记录创建的时间，精确到毫秒",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建时间，记录创建的时间，精确到毫秒");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifyDateTimeUtc",
                table: "Accounts",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateUtc",
                table: "Accounts",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "创建该对象的世界时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建该对象的世界时间");

            migrationBuilder.CreateTable(
                name: "PlEaMawbInbounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属机构Id"),
                    MawbNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "主单号（标准格式，去空格）"),
                    MawbNoDisplay = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true, comment: "主单号（显示格式，保留空格）"),
                    SourceType = table.Column<int>(type: "int", nullable: false, comment: "来源类型：0航司登记/1过单代理"),
                    AirlineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "航空公司Id（客户资料）"),
                    TransferAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "过单代理Id（客户资料）"),
                    RegisterDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "登记日期"),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "备注"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者Id"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "创建时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlEaMawbInbounds", x => x.Id);
                },
                comment: "空运出口主单领入登记表");

            migrationBuilder.CreateTable(
                name: "PlEaMawbOutbounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属机构Id"),
                    MawbNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "主单号（标准格式，去空格）"),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "领单代理Id（客户资料，通常为二级代理）"),
                    RecipientName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "领用人姓名"),
                    IssueDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "领用日期"),
                    PlannedReturnDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "预计返回日期"),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "实际返回日期"),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "备注"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者Id"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, comment: "创建时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlEaMawbOutbounds", x => x.Id);
                },
                comment: "空运出口主单领出登记表");

            migrationBuilder.CreateIndex(
                name: "IX_PlEaMawbInbounds_MawbNo",
                table: "PlEaMawbInbounds",
                column: "MawbNo");

            migrationBuilder.CreateIndex(
                name: "IX_PlEaMawbInbounds_OrgId",
                table: "PlEaMawbInbounds",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_PlEaMawbOutbounds_MawbNo",
                table: "PlEaMawbOutbounds",
                column: "MawbNo");

            migrationBuilder.CreateIndex(
                name: "IX_PlEaMawbOutbounds_OrgId",
                table: "PlEaMawbOutbounds",
                column: "OrgId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlEaMawbInbounds");

            migrationBuilder.DropTable(
                name: "PlEaMawbOutbounds");

            migrationBuilder.AlterTable(
                name: "ContainerKindCounts",
                oldComment: "箱型箱量子表");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlRoles",
                type: "datetime2",
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlRolePermissions",
                type: "datetime2",
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlOrganizations",
                type: "datetime2",
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OperatingDateTime",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "操作时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "操作时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Etd",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "开航日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "开航日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ETA",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "到港日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "到港日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDate",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "提送货日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "提送货日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlJobs",
                type: "datetime2",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CloseDate",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "关闭日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "关闭日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AccountDate",
                table: "PlJobs",
                type: "datetime2",
                nullable: true,
                comment: "财务日期，前端自动计算设置，后端直接使用",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "财务日期，前端自动计算设置，后端直接使用");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpToDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: true,
                comment: "提货日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "提货日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: true,
                comment: "提货日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "提货日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ContainerFreeDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: true,
                comment: "免箱期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "免箱期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BillDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: true,
                comment: "实际换单日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "实际换单日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BargeStartDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: true,
                comment: "驳船开航日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "驳船开航日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivedDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: true,
                comment: "进口日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "进口日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AnticipateBillDateTime",
                table: "PlIsDocs",
                type: "datetime2",
                nullable: true,
                comment: "预计换单日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "预计换单日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "IoDateTime",
                table: "PlInvoicess",
                type: "datetime2",
                nullable: true,
                comment: "首付日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "首付日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FinanceDateTime",
                table: "PlInvoicess",
                type: "datetime2",
                nullable: true,
                comment: "财务日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "财务日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlInvoicess",
                type: "datetime2",
                nullable: false,
                comment: "创建的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConfirmDateTime",
                table: "PlInvoicess",
                type: "datetime2",
                nullable: true,
                comment: "确认时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "确认时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "YujiXiaobaoDateTime",
                table: "PlIaDocs",
                type: "datetime2",
                nullable: true,
                comment: "预计消保日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "预计消保日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "XiaobaoDateTime",
                table: "PlIaDocs",
                type: "datetime2",
                nullable: true,
                comment: "实际消保日期，null表示未消保。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "实际消保日期，null表示未消保。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RukuDateTime",
                table: "PlIaDocs",
                type: "datetime2",
                nullable: true,
                comment: "入库日期。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "入库日期。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PickUpDateTime",
                table: "PlIaDocs",
                type: "datetime2",
                nullable: true,
                comment: "提货时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "提货时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlIaDocs",
                type: "datetime2",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WarehousingDateTime",
                table: "PlEsDocs",
                type: "datetime2",
                nullable: false,
                comment: "放舱日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "放舱日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CutOffGoodsDateTime",
                table: "PlEsDocs",
                type: "datetime2",
                nullable: true,
                comment: "截货日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "截货日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CutOffDateTime",
                table: "PlEsDocs",
                type: "datetime2",
                nullable: true,
                comment: "截关日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "截关日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlEsDocs",
                type: "datetime2",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BookingsDateTime",
                table: "PlEsDocs",
                type: "datetime2",
                nullable: false,
                comment: "订舱日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "订舱日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BargeSailDateTime",
                table: "PlEsDocs",
                type: "datetime2",
                nullable: true,
                comment: "驳船开航日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "驳船开航日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "BargeArrivalDateTime",
                table: "PlEsDocs",
                type: "datetime2",
                nullable: true,
                comment: "驳船到港日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "驳船到港日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlEaDocs",
                type: "datetime2",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlCustomerTidans",
                type: "datetime2",
                nullable: false,
                comment: "创建时间。默认值为创建对象的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建时间。默认值为创建对象的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlCustomers",
                type: "datetime2",
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlAccountRoles",
                type: "datetime2",
                nullable: false,
                comment: "创建的时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivalDateTime",
                table: "OwWfNodes",
                type: "datetime2",
                nullable: false,
                comment: "到达此节点的时间，如果是第一个节点则是创建并保存节点的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "到达此节点的时间，如果是第一个节点则是创建并保存节点的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocFeeTemplates",
                type: "datetime2",
                nullable: false,
                comment: "创建的时间。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建的时间。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DD_SimpleDataDics",
                type: "datetime2",
                nullable: true,
                comment: "创建时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "创建时间");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RepeatDate",
                table: "DD_OtherNumberRules",
                type: "datetime2",
                nullable: false,
                comment: "记录最后一次归零的日期",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "记录最后一次归零的日期");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Datetime",
                table: "CustomerBlacklists",
                type: "datetime2",
                nullable: false,
                comment: "执行时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "执行时间");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentId",
                table: "ContainerKindCounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属业务单据Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属业务单据Id");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "ContainerKindCounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "箱型。",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "箱型");

            migrationBuilder.AlterColumn<int>(
                name: "Count",
                table: "ContainerKindCounts",
                type: "int",
                nullable: false,
                comment: "数量。",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "数量");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TransactionDate",
                table: "ActualFinancialTransactions",
                type: "datetime2",
                nullable: false,
                comment: "收付款日期，实际发生收付款的业务日期，精确到毫秒",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "收付款日期，实际发生收付款的业务日期，精确到毫秒");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "ActualFinancialTransactions",
                type: "datetime2",
                nullable: false,
                comment: "创建时间，记录创建的时间，精确到毫秒",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建时间，记录创建的时间，精确到毫秒");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifyDateTimeUtc",
                table: "Accounts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateUtc",
                table: "Accounts",
                type: "datetime2",
                nullable: false,
                comment: "创建该对象的世界时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "创建该对象的世界时间");
        }
    }
}
