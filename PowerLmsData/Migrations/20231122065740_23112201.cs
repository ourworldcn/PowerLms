using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23112201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountPlOrganizations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "用户Id"),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "直属组织机构Id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountPlOrganizations", x => new { x.UserId, x.OrgId });
                },
                comment: "账号所属组织机构多对多表");

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    LoginName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "登录名"),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "用户的显示名"),
                    PwdHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true, comment: "密码的Hash值"),
                    CurrentLanguageTag = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: true, comment: "使用的首选语言标准缩写。如:zh-CN"),
                    NodeNum = table.Column<int>(type: "int", nullable: true),
                    CreateUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建该对象的世界时间"),
                    Timeout = table.Column<TimeSpan>(type: "time", nullable: false),
                    LastModifyDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Token = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "最近使用的Token"),
                    State = table.Column<byte>(type: "tinyint", nullable: false, comment: "用户状态。0是正常使用用户，1是锁定用户。"),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "当前使用的组织机构Id。在登陆后要首先设置"),
                    WorkingStatusCode = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "工作状态编码"),
                    IncumbencyCode = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "在职状态编码"),
                    GenderCode = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "性别编码"),
                    QualificationsCode = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "学历编码"),
                    EMail = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "eMail地址"),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "移动电话号码")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataDicCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "数据字典的代码。"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名称"),
                    DataDicType = table.Column<int>(type: "int", nullable: false, comment: "数据字典的类型。1=简单字典，其它值随后逐步定义。"),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id。通常这里为null则有不同解释，如通用的模板或超管使用的数据字典。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDicCatalogs", x => x.Id);
                },
                comment: "专门针对数据字典的目录。");

            migrationBuilder.CreateTable(
                name: "Merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Name = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "正式名称，拥有相对稳定性"),
                    Name_ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "正式简称，对正式的组织机构通常简称也是规定的"),
                    Name_DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名，有时它是昵称或简称(系统内)的意思"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "描述"),
                    ShortcutCode = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。"),
                    Address_Tel = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "电话"),
                    Address_Fax = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "传真"),
                    Address_FullAddress = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "详细地址"),
                    StatusCode = table.Column<int>(type: "int", nullable: false, comment: "状态码。0=正常，1=停用。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchants", x => x.Id);
                },
                comment: "商户");

            migrationBuilder.CreateTable(
                name: "Multilinguals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "主键。"),
                    LanguageTag = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false, comment: "主键，也是语言的标准缩写名。"),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "键值字符串。如:未登录.登录.标题。"),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "内容。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Multilinguals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name_Name = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "正式名称，拥有相对稳定性"),
                    Name_ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "正式简称，对正式的组织机构通常简称也是规定的"),
                    Name_DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示名，有时它是昵称或简称(系统内)的意思"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "组织机构描述"),
                    ShortcutCode = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。"),
                    Otc = table.Column<int>(type: "int", nullable: false, comment: "机构类型，2公司，4下属机构"),
                    Address_Tel = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "电话"),
                    Address_Fax = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "传真"),
                    Address_FullAddress = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "详细地址"),
                    ContractName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "联系人名字"),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属组织机构Id。没有父的组织机构是顶层节点即\"商户\"。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlOrganizations_PlOrganizations_ParentId",
                        column: x => x.ParentId,
                        principalTable: "PlOrganizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SimpleDataDics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomsCode = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "海关码。项目类型决定有大量与海关的EDI行为，交换使用的码。"),
                    DataDicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属数据字典的的Id"),
                    CreateAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建人账号Id"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "创建时间"),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。"),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示的名称"),
                    ShortName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "缩写名"),
                    ShortcutName = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入名")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleDataDics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示的名称"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "说明"),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "父资源的Id。可能分类用")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemResources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LoginName",
                table: "Accounts",
                column: "LoginName");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Token",
                table: "Accounts",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_DataDicCatalogs_Code",
                table: "DataDicCatalogs",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Multilinguals_LanguageTag_Key",
                table: "Multilinguals",
                columns: new[] { "LanguageTag", "Key" },
                unique: true,
                filter: "[Key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlOrganizations_ParentId",
                table: "PlOrganizations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SimpleDataDics_DataDicId",
                table: "SimpleDataDics",
                column: "DataDicId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemResources_Name",
                table: "SystemResources",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountPlOrganizations");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "DataDicCatalogs");

            migrationBuilder.DropTable(
                name: "Merchants");

            migrationBuilder.DropTable(
                name: "Multilinguals");

            migrationBuilder.DropTable(
                name: "PlOrganizations");

            migrationBuilder.DropTable(
                name: "SimpleDataDics");

            migrationBuilder.DropTable(
                name: "SystemResources");
        }
    }
}
