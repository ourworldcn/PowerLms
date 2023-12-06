using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _23120601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShortcutCode",
                table: "PlOrganizations",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入码。服务器不使用。",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutCode",
                table: "Merchants",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入码。服务器不使用。",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。");

            migrationBuilder.AddColumn<bool>(
                name: "IsDelete",
                table: "Merchants",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。");

            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                comment: "用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "用户状态。0是正常使用用户，1是锁定用户。");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDelete",
                table: "Merchants");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutCode",
                table: "PlOrganizations",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入码。服务器不使用。");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutCode",
                table: "Merchants",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入码。服务器不使用。8个ASCII字符不足的尾部填充空格（写入时可不填充，但读回后会自动加入）。",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入码。服务器不使用。");

            migrationBuilder.AlterColumn<byte>(
                name: "State",
                table: "Accounts",
                type: "tinyint",
                nullable: false,
                comment: "用户状态。0是正常使用用户，1是锁定用户。",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "用户状态掩码。D0=1是锁定用户，D1=1用户应尽快更改密码。");
        }
    }
}
