using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25022301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CarrieCode",
                table: "PlJobs",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "承运人，船公司或航空公司或，二字码。已废弃，使用Guid关联客户表。",
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldUnicode: false,
                oldMaxLength: 4,
                oldNullable: true,
                oldComment: "承运人，船公司或航空公司或，二字码");

            migrationBuilder.AddColumn<Guid>(
                name: "CarrieId",
                table: "PlJobs",
                type: "uniqueidentifier",
                nullable: true,
                comment: "承运人Id。关联客户表。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PreclearDate",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: false,
                comment: "预计结算日期，客户资料中信用日期自动计算出。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "预计结算日期，客户资料中信用日期自动计算出");

            migrationBuilder.AlterColumn<bool>(
                name: "IO",
                table: "DocFees",
                type: "bit",
                nullable: false,
                comment: "收入或支出，true为收入，false为支出。",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "收入或指出，true收入，false为支出。");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "DocFees",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "本位币汇率，默认从汇率表调取，Amount乘以该属性得到本位币金额。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "本位币汇率,默认从汇率表调取,机构本位币");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: false,
                comment: "创建时间，系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "新建时间,系统默认，不能更改。");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContainerTypeId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "单位，简单字典ContainerType，按票、按重量等",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "单位,简单字典ContainerType,按票、按重量等");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuditOperatorId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "审核人Id，为空则未审核。",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "审核人Id，为空则未审核");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: true,
                comment: "审核日期，为空则未审核。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "审核日期，为空则未审核");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "金额，两位小数，可以为负数。",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "金额,两位小数、可以为负数");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarrieId",
                table: "PlJobs");

            migrationBuilder.AlterColumn<string>(
                name: "CarrieCode",
                table: "PlJobs",
                type: "varchar(4)",
                unicode: false,
                maxLength: 4,
                nullable: true,
                comment: "承运人，船公司或航空公司或，二字码",
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldUnicode: false,
                oldMaxLength: 4,
                oldNullable: true,
                oldComment: "承运人，船公司或航空公司或，二字码。已废弃，使用Guid关联客户表。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PreclearDate",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: false,
                comment: "预计结算日期，客户资料中信用日期自动计算出",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "预计结算日期，客户资料中信用日期自动计算出。");

            migrationBuilder.AlterColumn<bool>(
                name: "IO",
                table: "DocFees",
                type: "bit",
                nullable: false,
                comment: "收入或指出，true收入，false为支出。",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "收入或支出，true为收入，false为支出。");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "DocFees",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                comment: "本位币汇率,默认从汇率表调取,机构本位币",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldComment: "本位币汇率，默认从汇率表调取，Amount乘以该属性得到本位币金额。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreateDateTime",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: false,
                comment: "新建时间,系统默认，不能更改。",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldComment: "创建时间，系统默认，不能更改。");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContainerTypeId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "单位,简单字典ContainerType,按票、按重量等",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "单位，简单字典ContainerType，按票、按重量等");

            migrationBuilder.AlterColumn<Guid>(
                name: "AuditOperatorId",
                table: "DocFees",
                type: "uniqueidentifier",
                nullable: true,
                comment: "审核人Id，为空则未审核",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                oldComment: "审核人Id，为空则未审核。");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AuditDateTime",
                table: "DocFees",
                type: "datetime2(2)",
                nullable: true,
                comment: "审核日期，为空则未审核",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(2)",
                oldNullable: true,
                oldComment: "审核日期，为空则未审核。");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DocFees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                comment: "金额,两位小数、可以为负数",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComment: "金额，两位小数，可以为负数。");
        }
    }
}
