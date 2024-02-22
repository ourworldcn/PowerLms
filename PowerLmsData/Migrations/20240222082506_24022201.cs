using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _24022201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingLanes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false, comment: "是否已标记为删除。false(默认)未标记为删除，true标记为删除。"),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "创建者的唯一标识。"),
                    CreateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "创建的时间。"),
                    UpdateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "最后更新者的唯一标识。"),
                    UpdateDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "最后更新的时间。"),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "所属机构Id。"),
                    StartCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "启运港编码"),
                    EndCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, comment: "目的港编码"),
                    Shipper = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "航空公司"),
                    VesslRate = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "航班周期"),
                    ArrivalTime = table.Column<TimeSpan>(type: "time", nullable: false, comment: "到达时长"),
                    Packing = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, comment: "包装规范"),
                    KgsM = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS M"),
                    KgsN = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS N"),
                    A45 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS45"),
                    A100 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS100"),
                    A300 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS300"),
                    A500 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS500"),
                    A1000 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS1000"),
                    A2000 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "KGS2000"),
                    StartDateTime = table.Column<DateTime>(type: "datetime2(2)", nullable: false, comment: "生效日期"),
                    EndDateTime = table.Column<DateTime>(type: "datetime2(2)", nullable: false, comment: "终止日期"),
                    Remark = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "备注"),
                    Contact = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "联系人。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingLanes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingLanes");
        }
    }
}
