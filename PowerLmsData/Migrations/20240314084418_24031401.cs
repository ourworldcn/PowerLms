using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _24031401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreateBy",
                table: "PlFileInfos",
                type: "uniqueidentifier",
                nullable: true,
                comment: "操作员，可以更改相当于工作号的所有者");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "PlFileInfos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "新建时间,系统默认，不能更改。");

            migrationBuilder.CreateTable(
                name: "DocBills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "账单号。"),
                    DocNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "业务编号,默认为该业务的JobNo，可修改,不绑定业务表的id。"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建人，建立时系统默认，默认不可更改"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2(2)", nullable: false, comment: "新建时间,系统默认，不能更改。"),
                    PayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "付款人。选Id"),
                    InscribeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "金额"),
                    CurrTypeId = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "币种Id"),
                    ClearAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, comment: "已核销金额"),
                    CheckDate = table.Column<DateTime>(type: "datetime2(2)", nullable: true, comment: "审核日期，为空则未审核"),
                    ChechManId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "审核人Id，为空则未审核"),
                    IsEnable = table.Column<bool>(type: "bit", nullable: false, comment: "是否有效"),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "备注"),
                    IODate = table.Column<DateTime>(type: "datetime2(2)", nullable: true, comment: "审核日期，为空则未审核"),
                    Vessel = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "船名或航班号"),
                    Voyage = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "航次"),
                    MblNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "主单号"),
                    HblNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "分单号"),
                    LoadingCode = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "起始港编码"),
                    DischargeCode = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "中转港编码"),
                    DestinationCode = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true, comment: "目的港编码"),
                    Etd = table.Column<DateTime>(type: "datetime2(2)", nullable: false, comment: "开航日期。"),
                    Eta = table.Column<DateTime>(type: "datetime2(2)", nullable: false, comment: "到港日期。"),
                    SoNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "So编号。"),
                    BookingNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "订舱编号。"),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "货物名称。"),
                    PkgsCount = table.Column<int>(type: "int", nullable: false, comment: "件数。"),
                    Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "结算计费重量，3位小数"),
                    MeasureMent = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, comment: "体积，3位小数"),
                    ContainerNum = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "箱量"),
                    Consignor = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "发货人"),
                    Consignee = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "收货人"),
                    Carrier = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "承运人"),
                    LinkMan = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "客户联系人"),
                    LinkTel = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true, comment: "联系人电话"),
                    LinkFax = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true, comment: "联系人传真"),
                    ContractNo = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "合同号")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocBills", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocBills");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "PlFileInfos");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "PlFileInfos");
        }
    }
}
