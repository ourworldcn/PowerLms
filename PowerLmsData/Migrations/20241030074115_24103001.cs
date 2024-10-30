using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24103001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocFeeTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "申请单Id"),
                    IO = table.Column<bool>(type: "bit", nullable: false, comment: "收付，false支出，true收入。"),
                    CoKind = table.Column<byte>(type: "tinyint", nullable: false, comment: "1=货主（业务中的客户）、2=收货人、4=发货人、8=承运人、16=代理人、32=固定客户"),
                    CoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "固定结算单位,当前一项类型选固定客户时必填。"),
                    FeePayTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "结算方式,简单字典FeePayType。"),
                    ContainerTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "单位,简单字典ContainerType。"),
                    FeesTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: " 费用种类,费用种类字典Id。"),
                    Currency = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "币种。标准货币缩写。"),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "单价"),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "基价,默认为0."),
                    FreeDayCount = table.Column<short>(type: "smallint", nullable: false, comment: "免费天数。"),
                    PriceOfLessTen = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "10天以下单价"),
                    PriceOfGreateOrEqTen = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "10天(含)以上单价。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocFeeTemplateItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocFeeTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者的唯一标识。"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建的时间。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocFeeTemplates", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocFeeTemplateItems");

            migrationBuilder.DropTable(
                name: "DocFeeTemplates");
        }
    }
}
