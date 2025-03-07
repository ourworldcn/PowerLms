using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25030702 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 检查视图是否存在，然后再删除，避免初次创建时出错
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE name = 'OwAppLogView' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    DROP VIEW dbo.OwAppLogView;
                END
            ");

            // 创建视图，增加计算字段
            migrationBuilder.Sql(@"
                CREATE VIEW dbo.OwAppLogView AS
                SELECT 
                    i.Id,
                    i.ParentId AS TypeId,
                    s.LogLevel,
                    s.FormatString,
                    i.ParamstersJson,
                    i.ExtraBytes,
                    i.CreateUtc,
                    i.MerchantId,
                    -- 添加JSON计算字段
                    JSON_VALUE(i.ParamstersJson, '$.LoginName') AS LoginName,
                    JSON_VALUE(i.ParamstersJson, '$.CompanyName') AS CompanyName,
                    JSON_VALUE(i.ParamstersJson, '$.DisplayName') AS DisplayName,
                    JSON_VALUE(i.ParamstersJson, '$.OperationIp') AS OperationIp,
                    JSON_VALUE(i.ParamstersJson, '$.OperationType') AS OperationType,
                    JSON_VALUE(i.ParamstersJson, '$.ClientType') AS ClientType
                FROM 
                    dbo.OwAppLogItemStores i
                    LEFT JOIN dbo.OwAppLogStores s ON i.ParentId = s.Id
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 检查视图是否存在，然后再删除
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE name = 'OwAppLogView' AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    DROP VIEW dbo.OwAppLogView;
                END
            ");
        }
    }
}
