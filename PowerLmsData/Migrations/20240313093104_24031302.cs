using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24031302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocFees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "业务单的Id"),
                    AccountNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "账单号，账单表中的id"),
                    FeeTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "费用种类字典项Id"),
                    BalanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "结算单位，客户资料中为结算单位的客户id。"),
                    IO = table.Column<bool>(type: "bit", nullable: false, comment: "收入或指出，true支持，false为收入。"),
                    GainTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "结算方式，简单字典FeePayType"),
                    ContainerTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "单位,简单字典ContainerType,按票、按重量等"),
                    UnitCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "数量"),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "单价，4位小数。"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "金额,两位小数、可以为负数"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "本位币汇率,默认从汇率表调取,机构本位币"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建人，建立时系统默认，默认不可更改"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(2)", nullable: false, comment: "新建时间,系统默认，不能更改。"),
                    PreclearDate = table.Column<DateTime>(type: "datetime2(2)", nullable: false, comment: "预计结算日期，客户资料中信用日期自动计算出"),
                    CheckDate = table.Column<DateTime>(type: "datetime2(2)", nullable: true, comment: "审核日期，为空则未审核"),
                    ChechManId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "审核人Id，为空则未审核")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocFees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocFees_DocId",
                table: "DocFees",
                column: "DocId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocFees");
        }
    }
}
