using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25071201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "PlOrganizations",
                comment: "机构实体，包括下属机构，公司。");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "PlRoles",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id或商户Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属组织机构Id。");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutCode",
                table: "PlOrganizations",
                type: "varchar(8)",
                unicode: false,
                maxLength: 8,
                nullable: true,
                comment: "快捷输入码。服务器不使用。",
                oldClrType: typeof(string),
                oldType: "char(8)",
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入码。服务器不使用。");

            migrationBuilder.AlterColumn<int>(
                name: "Otc",
                table: "PlOrganizations",
                type: "int",
                nullable: false,
                comment: "机构类型，2公司；4机构，此时祖先机构中必有公司类型的机构。",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "机构类型，2公司，4下属机构");

            migrationBuilder.AddColumn<bool>(
                name: "IsDomestic",
                table: "PlCustomers",
                type: "bit",
                nullable: true,
                comment: "国内外字段");

            migrationBuilder.AlterColumn<byte>(
                name: "StatusValue",
                table: "OwTaskStores",
                type: "tinyint",
                nullable: false,
                comment: "任务当前执行状态",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "任务当前执行状态值");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartUtc",
                table: "OwTaskStores",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "任务开始执行时间，UTC格式",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "任务开始执行时间，UTC格式，精确到毫秒");

            migrationBuilder.AlterColumn<string>(
                name: "ResultJson",
                table: "OwTaskStores",
                type: "nvarchar(max)",
                nullable: true,
                comment: "任务执行结果，JSON格式",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "任务执行结果，JSON格式的字符串");

            migrationBuilder.AlterColumn<string>(
                name: "ParametersJson",
                table: "OwTaskStores",
                type: "nvarchar(max)",
                nullable: true,
                comment: "任务参数，JSON格式",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "任务参数，JSON格式的字符串");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedUtc",
                table: "OwTaskStores",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "任务创建时间，UTC格式",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "任务创建时间，UTC格式，精确到毫秒");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedUtc",
                table: "OwTaskStores",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "任务完成时间，UTC格式",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "任务完成时间，UTC格式，精确到毫秒");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDomestic",
                table: "PlCustomers");

            migrationBuilder.AlterTable(
                name: "PlOrganizations",
                oldComment: "机构实体，包括下属机构，公司。");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgId",
                table: "PlRoles",
                type: "uniqueidentifier",
                nullable: true,
                comment: "所属组织机构Id。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "所属组织机构Id或商户Id。");

            migrationBuilder.AlterColumn<string>(
                name: "ShortcutCode",
                table: "PlOrganizations",
                type: "char(8)",
                maxLength: 8,
                nullable: true,
                comment: "快捷输入码。服务器不使用。",
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldUnicode: false,
                oldMaxLength: 8,
                oldNullable: true,
                oldComment: "快捷输入码。服务器不使用。");

            migrationBuilder.AlterColumn<int>(
                name: "Otc",
                table: "PlOrganizations",
                type: "int",
                nullable: false,
                comment: "机构类型，2公司，4下属机构",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "机构类型，2公司；4机构，此时祖先机构中必有公司类型的机构。");

            migrationBuilder.AlterColumn<byte>(
                name: "StatusValue",
                table: "OwTaskStores",
                type: "tinyint",
                nullable: false,
                comment: "任务当前执行状态值",
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "任务当前执行状态");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartUtc",
                table: "OwTaskStores",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "任务开始执行时间，UTC格式，精确到毫秒",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "任务开始执行时间，UTC格式");

            migrationBuilder.AlterColumn<string>(
                name: "ResultJson",
                table: "OwTaskStores",
                type: "nvarchar(max)",
                nullable: true,
                comment: "任务执行结果，JSON格式的字符串",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "任务执行结果，JSON格式");

            migrationBuilder.AlterColumn<string>(
                name: "ParametersJson",
                table: "OwTaskStores",
                type: "nvarchar(max)",
                nullable: true,
                comment: "任务参数，JSON格式的字符串",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "任务参数，JSON格式");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedUtc",
                table: "OwTaskStores",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                comment: "任务创建时间，UTC格式，精确到毫秒",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldComment: "任务创建时间，UTC格式");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedUtc",
                table: "OwTaskStores",
                type: "datetime2(3)",
                precision: 3,
                nullable: true,
                comment: "任务完成时间，UTC格式，精确到毫秒",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldPrecision: 3,
                oldNullable: true,
                oldComment: "任务完成时间，UTC格式");
        }
    }
}
