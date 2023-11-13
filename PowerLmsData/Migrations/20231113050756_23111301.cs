using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23111301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Token",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "最近使用的Token",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LoginName",
                table: "Accounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "登录名",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Accounts",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateUtc",
                table: "Accounts",
                type: "datetime2",
                nullable: false,
                comment: "创建该对象的世界时间",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Accounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "用户的显示名");

            migrationBuilder.AddColumn<Guid>(
                name: "GenderCode",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "性别编码");

            migrationBuilder.AddColumn<Guid>(
                name: "IncumbencyCode",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "在职状态编码");

            migrationBuilder.AddColumn<Guid>(
                name: "OrgId",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id");

            migrationBuilder.AddColumn<Guid>(
                name: "QualificationsCode",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "学历编码");

            migrationBuilder.AddColumn<byte>(
                name: "State",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "用户状态。0是正常使用用户，1是锁定用户。");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkingStatusCode",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                comment: "工作状态编码");

            migrationBuilder.CreateTable(
                name: "DataDicCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示的名称")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDicCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "组织机构名称"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "组织机构描述"),
                    ShortcutName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "机构编码"),
                    Otc = table.Column<int>(type: "int", nullable: false, comment: "机构类型，1商户，2公司，4下属机构"),
                    Address_Tel = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "电话"),
                    Address_Fax = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: true, comment: "传真"),
                    Address_FullAddress = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "详细地址"),
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
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "编码，对本系统有一定意义的编码"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "显示的名称"),
                    ShortcutName = table.Column<string>(type: "char(8)", maxLength: 8, nullable: true, comment: "快捷输入名")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleDataDics", x => x.Id);
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
                name: "IX_PlOrganizations_ParentId",
                table: "PlOrganizations",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataDicCatalogs");

            migrationBuilder.DropTable(
                name: "PlOrganizations");

            migrationBuilder.DropTable(
                name: "SimpleDataDics");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_LoginName",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Token",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "GenderCode",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IncumbencyCode",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "QualificationsCode",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "WorkingStatusCode",
                table: "Accounts");

            migrationBuilder.AlterColumn<Guid>(
                name: "Token",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "最近使用的Token");

            migrationBuilder.AlterColumn<string>(
                name: "LoginName",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "登录名");

            migrationBuilder.AlterColumn<string>(
                name: "LanguageTag",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateUtc",
                table: "Accounts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComment: "创建该对象的世界时间");
        }
    }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
