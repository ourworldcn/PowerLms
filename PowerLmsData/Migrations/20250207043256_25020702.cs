using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerLmsData.Migrations
{
    public partial class _25020702 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            try
            {
                migrationBuilder.Sql("DROP VIEW IF EXISTS OwAppLogVO");
                migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'OwAppLogVO'))
                BEGIN
                    EXEC('CREATE VIEW OwAppLogVO
                    AS
                    SELECT 
                        ali.Id AS Id, 
                        ali.ParentId AS TypeId, 
                        als.FormatString AS FormatString, 
                        ali.ParamstersJson AS ParamstersJson, 
                        ali.ExtraBytes AS ExtraBytes, 
                        ali.CreateUtc AS CreateUtc, 
                        ali.MerchantId AS MerchantId
                    FROM 
                        OwAppLogItemStores ali
                    LEFT JOIN 
                        OwAppLogStores als ON ali.ParentId = als.Id;')
                END");
            }
            catch (Exception)
            {
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
