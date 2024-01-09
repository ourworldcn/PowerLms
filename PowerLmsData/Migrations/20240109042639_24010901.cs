using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

namespace PowerLmsData.Migrations
{
    public partial class _24010901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "PlFileInfos",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                comment: "文件类型Id。文件的相对路径和全名。",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "文件类型Id。关联字典FileType。");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "PlFileInfos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "上传时的文件名",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "上传时的文件名");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "PlFileInfos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "文件的显示名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "文件的显示名称");

            migrationBuilder.CreateIndex(
                name: "IX_PlFileInfos_ParentId",
                table: "PlFileInfos",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlCustomerTidans_CustomerId",
                table: "PlCustomerTidans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlCustomerTaxInfos_CustomerId",
                table: "PlCustomerTaxInfos",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlCustomerLoadingAddrs_CustomerId",
                table: "PlCustomerLoadingAddrs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlCustomerContacts_CustomerId",
                table: "PlCustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlCustomerBusinessHeaders_CustomerId",
                table: "PlCustomerBusinessHeaders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBlacklists_CustomerId_Datetime",
                table: "CustomerBlacklists",
                columns: new[] { "CustomerId", "Datetime" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlFileInfos_ParentId",
                table: "PlFileInfos");

            migrationBuilder.DropIndex(
                name: "IX_PlCustomerTidans_CustomerId",
                table: "PlCustomerTidans");

            migrationBuilder.DropIndex(
                name: "IX_PlCustomerTaxInfos_CustomerId",
                table: "PlCustomerTaxInfos");

            migrationBuilder.DropIndex(
                name: "IX_PlCustomerLoadingAddrs_CustomerId",
                table: "PlCustomerLoadingAddrs");

            migrationBuilder.DropIndex(
                name: "IX_PlCustomerContacts_CustomerId",
                table: "PlCustomerContacts");

            migrationBuilder.DropIndex(
                name: "IX_PlCustomerBusinessHeaders_CustomerId",
                table: "PlCustomerBusinessHeaders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerBlacklists_CustomerId_Datetime",
                table: "CustomerBlacklists");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "PlFileInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "文件类型Id。关联字典FileType。",
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true,
                oldComment: "文件类型Id。文件的相对路径和全名。");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "PlFileInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "上传时的文件名",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "上传时的文件名");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "PlFileInfos",
                type: "nvarchar(max)",
                nullable: true,
                comment: "文件的显示名称",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true,
                oldComment: "文件的显示名称");
        }
    }
}
