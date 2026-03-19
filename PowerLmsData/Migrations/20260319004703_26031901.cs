using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _26031901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreateBy",
                table: "EsMbls",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "EsMbls",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "创建时间");

            migrationBuilder.AddColumn<Guid>(
                name: "CreateBy",
                table: "EsHbls",
                type: "uniqueidentifier",
                nullable: true,
                comment: "创建者Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDateTime",
                table: "EsHbls",
                type: "datetime2(3)",
                precision: 3,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                comment: "创建时间");

            migrationBuilder.AlterColumn<string>(
                name: "VoyageNumber",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "运输工具编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "运输工具编码");

            migrationBuilder.AlterColumn<string>(
                name: "TypistNo",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "录入员IC卡号，导入暂存时必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true,
                oldComment: "录入员IC卡号，导入暂存时必填");

            migrationBuilder.AlterColumn<string>(
                name: "TrafName",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "运输工具代码及名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "运输工具代码及名称");

            migrationBuilder.AlterColumn<string>(
                name: "TradeName",
                table: "CustomsDeclarations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "境内收发货人名称，私有通道导入时必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true,
                oldComment: "境内收发货人名称，私有通道导入时必填");

            migrationBuilder.AlterColumn<string>(
                name: "TradeCode",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "境内收发货人编号，私有通道导入时必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "境内收发货人编号，私有通道导入时必填");

            migrationBuilder.AlterColumn<string>(
                name: "TradeCoScc",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "收发货人统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "收发货人统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "TgdNo",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "关联单据号，空值预留字段",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "关联单据号，空值预留字段");

            migrationBuilder.AlterColumn<string>(
                name: "Tax_TaxNo",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "税单_税号",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "税单_税号");

            migrationBuilder.AlterColumn<string>(
                name: "Tax_OverdueNo",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "税单_滞纳金号",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "税单_滞纳金号");

            migrationBuilder.AlterColumn<string>(
                name: "Tax_AddedtaxNo",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "税单_增值税号",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "税单_增值税号");

            migrationBuilder.AlterColumn<string>(
                name: "SeqNo",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "数据中心统一编号，首次导入传空值由系统生成",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "数据中心统一编号，首次导入传空值由系统生成");

            migrationBuilder.AlterColumn<string>(
                name: "RelatedCustomsNO",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "关联报关单号",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "关联报关单号");

            migrationBuilder.AlterColumn<string>(
                name: "PartenerID",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "申报人标识（申报人姓名）",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "申报人标识（申报人姓名）");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerName",
                table: "CustomsDeclarations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "消费使用/生产销售单位名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true,
                oldComment: "消费使用/生产销售单位名称");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerCodeScc",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "消费使用/生产销售单位统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "消费使用/生产销售单位统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerCode",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "消费使用/生产销售单位代码",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "消费使用/生产销售单位代码");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorEname",
                table: "CustomsDeclarations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "境外发货人名称（外文）",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "境外发货人名称（外文）");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorCode",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "境外发货人代码",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "境外发货人代码");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorCname",
                table: "CustomsDeclarations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                comment: "境外收发货人中文名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComment: "境外收发货人中文名称");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorAddr",
                table: "CustomsDeclarations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "境外收发货人地址",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "境外收发货人地址");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsigneeEname",
                table: "CustomsDeclarations",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                comment: "境外收货人名称（外文）",
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400,
                oldNullable: true,
                oldComment: "境外收货人名称（外文）");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsigneeCode",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "境外收货人编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "境外收货人编码");

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNo",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "许可证编号",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "许可证编号");

            migrationBuilder.AlterColumn<string>(
                name: "GoodsPlace",
                table: "CustomsDeclarations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "存放地点",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "存放地点");

            migrationBuilder.AlterColumn<string>(
                name: "EntryId",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "海关编号",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "海关编号");

            migrationBuilder.AlterColumn<string>(
                name: "DomesticConsigneeEname",
                table: "CustomsDeclarations",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                comment: "境内收发货人名称（外文）",
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400,
                oldNullable: true,
                oldComment: "境内收发货人名称（外文）");

            migrationBuilder.AlterColumn<string>(
                name: "DeclareName",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "申报人员姓名",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "申报人员姓名");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationNo",
                table: "CustomsDeclarations",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                comment: "关联号码",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "关联号码");

            migrationBuilder.AlterColumn<string>(
                name: "CopName",
                table: "CustomsDeclarations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "录入单位名称，必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true,
                oldComment: "录入单位名称，必填");

            migrationBuilder.AlterColumn<string>(
                name: "CopCodeScc",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "录入单位统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "录入单位统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "ContriNO",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "保证金_缴款书号",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldComment: "保证金_缴款书号");

            migrationBuilder.AlterColumn<string>(
                name: "BLNo",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "B/L号，提货单号",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "B/L号，提货单号");

            migrationBuilder.AlterColumn<string>(
                name: "AssRecordNO",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "关联备案号",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldComment: "关联备案号");

            migrationBuilder.AlterColumn<string>(
                name: "AgentName",
                table: "CustomsDeclarations",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                comment: "申报单位名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(70)",
                oldMaxLength: 70,
                oldNullable: true,
                oldComment: "申报单位名称");

            migrationBuilder.AlterColumn<string>(
                name: "AgentCodeScc",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "申报单位统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "申报单位统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "AgentCode",
                table: "CustomsDeclarations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "申报单位代码",
                oldClrType: typeof(string),
                oldType: "nvarchar(18)",
                oldMaxLength: 18,
                oldNullable: true,
                oldComment: "申报单位代码");

            migrationBuilder.AddColumn<string>(
                name: "TaskNumber2",
                table: "CustomsDeclarations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "两步申报任务编号");

            migrationBuilder.CreateTable(
                name: "PlReportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true, comment: "额外日期时间；含义由调用方约定，可用于存储附加时间信息。"),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true, comment: "扩展定点数；含义由调用方约定，可用于存储附加数值信息，如金额等。"),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "副ID；含义由调用方约定，可指向任意关联对象Id。"),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "JSON对象字符串（前端解析定义）"),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "额外Guid；含义由调用方约定，可用于关联扩展对象或存储附加标识。"),
                    ExtraString = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, comment: "额外字符串；含义由调用方约定，可用于存储附加文本信息。"),
                    ExtraString2 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "扩展字符串二；含义由调用方约定，可用于存储附加分类或标识信息。")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlReportTemplates", x => x.Id);
                },
                comment: "报表模板");

            migrationBuilder.CreateIndex(
                name: "IX_PlReportTemplates_ExtraGuid",
                table: "PlReportTemplates",
                column: "ExtraGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PlReportTemplates_ExtraString",
                table: "PlReportTemplates",
                column: "ExtraString");

            migrationBuilder.CreateIndex(
                name: "IX_PlReportTemplates_ExtraString2",
                table: "PlReportTemplates",
                column: "ExtraString2");

            migrationBuilder.CreateIndex(
                name: "IX_PlReportTemplates_ParentId",
                table: "PlReportTemplates",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlReportTemplates");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "EsMbls");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "EsMbls");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "EsHbls");

            migrationBuilder.DropColumn(
                name: "CreateDateTime",
                table: "EsHbls");

            migrationBuilder.DropColumn(
                name: "TaskNumber2",
                table: "CustomsDeclarations");

            migrationBuilder.AlterColumn<string>(
                name: "VoyageNumber",
                table: "CustomsDeclarations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "运输工具编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "运输工具编码");

            migrationBuilder.AlterColumn<string>(
                name: "TypistNo",
                table: "CustomsDeclarations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                comment: "录入员IC卡号，导入暂存时必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "录入员IC卡号，导入暂存时必填");

            migrationBuilder.AlterColumn<string>(
                name: "TrafName",
                table: "CustomsDeclarations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "运输工具代码及名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "运输工具代码及名称");

            migrationBuilder.AlterColumn<string>(
                name: "TradeName",
                table: "CustomsDeclarations",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true,
                comment: "境内收发货人名称，私有通道导入时必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "境内收发货人名称，私有通道导入时必填");

            migrationBuilder.AlterColumn<string>(
                name: "TradeCode",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "境内收发货人编号，私有通道导入时必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "境内收发货人编号，私有通道导入时必填");

            migrationBuilder.AlterColumn<string>(
                name: "TradeCoScc",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "收发货人统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "收发货人统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "TgdNo",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "关联单据号，空值预留字段",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "关联单据号，空值预留字段");

            migrationBuilder.AlterColumn<string>(
                name: "Tax_TaxNo",
                table: "CustomsDeclarations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "税单_税号",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "税单_税号");

            migrationBuilder.AlterColumn<string>(
                name: "Tax_OverdueNo",
                table: "CustomsDeclarations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "税单_滞纳金号",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "税单_滞纳金号");

            migrationBuilder.AlterColumn<string>(
                name: "Tax_AddedtaxNo",
                table: "CustomsDeclarations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "税单_增值税号",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "税单_增值税号");

            migrationBuilder.AlterColumn<string>(
                name: "SeqNo",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "数据中心统一编号，首次导入传空值由系统生成",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "数据中心统一编号，首次导入传空值由系统生成");

            migrationBuilder.AlterColumn<string>(
                name: "RelatedCustomsNO",
                table: "CustomsDeclarations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "关联报关单号",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "关联报关单号");

            migrationBuilder.AlterColumn<string>(
                name: "PartenerID",
                table: "CustomsDeclarations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "申报人标识（申报人姓名）",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "申报人标识（申报人姓名）");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerName",
                table: "CustomsDeclarations",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true,
                comment: "消费使用/生产销售单位名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "消费使用/生产销售单位名称");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerCodeScc",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "消费使用/生产销售单位统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "消费使用/生产销售单位统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerCode",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "消费使用/生产销售单位代码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "消费使用/生产销售单位代码");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorEname",
                table: "CustomsDeclarations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "境外发货人名称（外文）",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "境外发货人名称（外文）");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorCode",
                table: "CustomsDeclarations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "境外发货人代码",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "境外发货人代码");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorCname",
                table: "CustomsDeclarations",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                comment: "境外收发货人中文名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "境外收发货人中文名称");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsignorAddr",
                table: "CustomsDeclarations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "境外收发货人地址",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "境外收发货人地址");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsigneeEname",
                table: "CustomsDeclarations",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true,
                comment: "境外收货人名称（外文）",
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true,
                oldComment: "境外收货人名称（外文）");

            migrationBuilder.AlterColumn<string>(
                name: "OverseasConsigneeCode",
                table: "CustomsDeclarations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "境外收货人编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "境外收货人编码");

            migrationBuilder.AlterColumn<string>(
                name: "LicenseNo",
                table: "CustomsDeclarations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "许可证编号",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "许可证编号");

            migrationBuilder.AlterColumn<string>(
                name: "GoodsPlace",
                table: "CustomsDeclarations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "存放地点",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "存放地点");

            migrationBuilder.AlterColumn<string>(
                name: "EntryId",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "海关编号",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "海关编号");

            migrationBuilder.AlterColumn<string>(
                name: "DomesticConsigneeEname",
                table: "CustomsDeclarations",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true,
                comment: "境内收发货人名称（外文）",
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true,
                oldComment: "境内收发货人名称（外文）");

            migrationBuilder.AlterColumn<string>(
                name: "DeclareName",
                table: "CustomsDeclarations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "申报人员姓名",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "申报人员姓名");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationNo",
                table: "CustomsDeclarations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "关联号码",
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true,
                oldComment: "关联号码");

            migrationBuilder.AlterColumn<string>(
                name: "CopName",
                table: "CustomsDeclarations",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true,
                comment: "录入单位名称，必填",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "录入单位名称，必填");

            migrationBuilder.AlterColumn<string>(
                name: "CopCodeScc",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "录入单位统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "录入单位统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "ContriNO",
                table: "CustomsDeclarations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "保证金_缴款书号",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "保证金_缴款书号");

            migrationBuilder.AlterColumn<string>(
                name: "BLNo",
                table: "CustomsDeclarations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "B/L号，提货单号",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "B/L号，提货单号");

            migrationBuilder.AlterColumn<string>(
                name: "AssRecordNO",
                table: "CustomsDeclarations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "关联备案号",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "关联备案号");

            migrationBuilder.AlterColumn<string>(
                name: "AgentName",
                table: "CustomsDeclarations",
                type: "nvarchar(70)",
                maxLength: 70,
                nullable: true,
                comment: "申报单位名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldComment: "申报单位名称");

            migrationBuilder.AlterColumn<string>(
                name: "AgentCodeScc",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "申报单位统一编码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "申报单位统一编码");

            migrationBuilder.AlterColumn<string>(
                name: "AgentCode",
                table: "CustomsDeclarations",
                type: "nvarchar(18)",
                maxLength: 18,
                nullable: true,
                comment: "申报单位代码",
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "申报单位代码");
        }
    }
}
