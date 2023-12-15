using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _23121501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CurrentNumber",
                table: "DD_JobNumberRules",
                type: "int",
                nullable: false,
                comment: "当前未用的最小编号",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "当前编号");

            migrationBuilder.CreateTable(
                name: "CustomerBlacklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属客户Id"),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false, comment: "类型，1=加入超额，2=加入超期，3=移除超额，4=移除超期"),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, comment: "来源，1=系统，0=人工。"),
                    OpertorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "操作员Id"),
                    Datetime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "执行时间"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBlacklists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlCustomerBusinessHeaders",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "客户Id"),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "用户Id"),
                    OrderTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "负责的业务Id。连接业务种类字典。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlCustomerBusinessHeaders", x => new { x.CustomerId, x.AccountId, x.OrderTypeId });
                });

            migrationBuilder.CreateTable(
                name: "PlCustomerContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "姓名。"),
                    SexId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "性别Id。"),
                    Title = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "职务/行政级别。"),
                    Contact_Tel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "电话"),
                    Contact_Fax = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "传真"),
                    Contact_EMail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "电子邮件"),
                    Mobile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "移动电话。"),
                    Bank = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "开户行。"),
                    Account = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "银行账号。"),
                    Keyword = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "搜索用的关键字。逗号分隔多个关键字。"),
                    Remark = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "备注。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlCustomerContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlCustomerLoadingAddrs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属客户Id"),
                    Contact = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "所属客户Id"),
                    Tel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "联系电话"),
                    Addr = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "详细地址")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlCustomerLoadingAddrs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "海关编码。"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "客户编码"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "正式名"),
                    CrideCode = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "纳税人识别号"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "简称"),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "编号"),
                    Contact_Tel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "电话"),
                    Contact_Fax = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "传真"),
                    Contact_EMail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "电子邮件"),
                    Address_CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "国家编码Id"),
                    Address_Province = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "省"),
                    Address_City = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "地市"),
                    Address_Address = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "详细地址"),
                    Address_ZipCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true, comment: "邮政编码"),
                    InternetAddress = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true, comment: "网址"),
                    Keyword = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "搜索用的关键字。逗号分隔多个关键字。"),
                    BillingInfo_IsExesGather = table.Column<bool>(type: "bit", nullable: true, comment: "是否应收结算单位"),
                    BillingInfo_IsExesPayer = table.Column<bool>(type: "bit", nullable: true, comment: "是否应付结算单位"),
                    BillingInfo_Dayslimited = table.Column<int>(type: "int", nullable: true, comment: "信用期限天数"),
                    BillingInfo_CurrtypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "拖欠限额币种Id"),
                    BillingInfo_AmountLimited = table.Column<decimal>(type: "decimal(18,2)", nullable: true, comment: "拖欠金额"),
                    BillingInfo_AmountTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "付费方式Id"),
                    BillingInfo_IsCEBlack = table.Column<bool>(type: "bit", nullable: true, comment: "是否超额黑名单"),
                    BillingInfo_IsBlack = table.Column<bool>(type: "bit", nullable: true, comment: "是否超期黑名单"),
                    BillingInfo_IsNeedTrace = table.Column<bool>(type: "bit", nullable: true, comment: "是否特别注意"),
                    ShipperPropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "货主性质Id"),
                    CustomerLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "客户级别Id"),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, comment: "是否有效"),
                    IsShipper = table.Column<bool>(type: "bit", nullable: false, comment: "是否委托单位"),
                    IsBalance = table.Column<bool>(type: "bit", nullable: false, comment: "是否结算单位"),
                    IsConsignor = table.Column<bool>(type: "bit", nullable: false, comment: "是否发货人"),
                    IsConsignee = table.Column<bool>(type: "bit", nullable: false, comment: "是否收货人"),
                    IsNotify = table.Column<bool>(type: "bit", nullable: false, comment: "是否通知人"),
                    IsAirway = table.Column<bool>(type: "bit", nullable: false, comment: "是否航空公司"),
                    IsShipOwner = table.Column<bool>(type: "bit", nullable: false, comment: "是否船公司"),
                    IsBookingAgent = table.Column<bool>(type: "bit", nullable: false, comment: "是否订舱代理"),
                    IsDestAgent = table.Column<bool>(type: "bit", nullable: false, comment: "是否目的港代理"),
                    IsLocal = table.Column<bool>(type: "bit", nullable: false, comment: "是否卡车公司"),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false, comment: "是否报关行"),
                    IsInsure = table.Column<bool>(type: "bit", nullable: false, comment: "是否保险公司"),
                    IsProvide = table.Column<bool>(type: "bit", nullable: false, comment: "是否供货商"),
                    IsStock = table.Column<bool>(type: "bit", nullable: false, comment: "是否仓储公司"),
                    IsOthers = table.Column<bool>(type: "bit", nullable: false, comment: "是否其他")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlCustomerTaxInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属客户Id"),
                    Type = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Number = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "纳税人识别号"),
                    BankStdCoin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "人民币账号"),
                    BankNoRMB = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "开户行"),
                    BankUSD = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "美金账户"),
                    BankNoUSD = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "美金开户行"),
                    Addr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "地址"),
                    Tel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "电话"),
                    TaxRate = table.Column<int>(type: "int", nullable: false, comment: "税率"),
                    InvoiceSignAddr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "发票邮寄地址")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlCustomerTaxInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlCustomerTidans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "所属客户Id"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "提单内容"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建时间。默认值为创建对象的时间。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlCustomerTidans", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerBlacklists");

            migrationBuilder.DropTable(
                name: "PlCustomerBusinessHeaders");

            migrationBuilder.DropTable(
                name: "PlCustomerContacts");

            migrationBuilder.DropTable(
                name: "PlCustomerLoadingAddrs");

            migrationBuilder.DropTable(
                name: "PlCustomers");

            migrationBuilder.DropTable(
                name: "PlCustomerTaxInfos");

            migrationBuilder.DropTable(
                name: "PlCustomerTidans");

            migrationBuilder.AlterColumn<int>(
                name: "CurrentNumber",
                table: "DD_JobNumberRules",
                type: "int",
                nullable: false,
                comment: "当前编号",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "当前未用的最小编号");
        }
    }
}
