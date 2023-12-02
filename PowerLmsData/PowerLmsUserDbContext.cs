using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PowerLms.Data;
using PowerLmsData.基础数据;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.EfData
{
    /// <summary>
    /// 
    /// </summary>
    public static class MigrateDbInitializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public static void Initialize(PowerLmsUserDbContext context)
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PowerLmsUserDbContext : DbContext
    {
        /// <summary>
        /// 
        /// </summary>
        public PowerLmsUserDbContext()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public PowerLmsUserDbContext(DbContextOptions options) : base(options)
        {
            //Database.SetInitializer<Context>(new MigrateDatabaseToLatestVersion<Context, ReportingDbMigrationsConfiguration>());
        }

        #region 方法

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=.;Database=PowerLmsUserDevelopment;Integrated Security=True;Trusted_Connection=True;MultipleActiveResultSets=true;Pooling=True");
                Trace.WriteLine("OnConfiguring被调用");
            }
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountPlOrganization>().HasKey(nameof(AccountPlOrganization.UserId), nameof(AccountPlOrganization.OrgId));
            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// 删除一组指定Id的对象。立即生效。
        /// </summary>
        /// <param name="deleteIds"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public int Delete(List<Guid> deleteIds, string tableName)
        {
            var ids = string.Join(',', deleteIds.Select(c => c.ToString()));
            var sql = $"Delete From {tableName} Where [Id] In ({ids})";
            return Database.ExecuteSqlRaw(sql);
        }

        #endregion 方法

        #region 系统资源相关
        /// <summary>
        /// 系统资源表。包含数据字典目录。
        /// </summary>
        public DbSet<SystemResource> DD_SystemResources { get; set; }

        /// <summary>
        /// 数据字典目录。
        /// </summary>
        public DbSet<DataDicCatalog> DD_DataDicCatalogs { get; set; }

        /// <summary>
        /// 简单数据字典表。
        /// </summary>
        public DbSet<SimpleDataDic> DD_SimpleDataDics { get; set; }

        /// <summary>
        /// 业务大类字典表。
        /// </summary>
        public DbSet<BusinessTypeDataDic> DD_BusinessTypeDataDics { get; set; }

        /// <summary>
        /// 港口数据字典表。
        /// </summary>
        public DbSet<PlPort> DD_PlPorts { get; set; }

        /// <summary>
        /// 航线表。
        /// </summary>
        public DbSet<PlCargoRoute> DD_PlCargoRoutes { get; set; }

        /// <summary>
        /// 汇率表。
        /// </summary>
        public DbSet<PlExchangeRate> DD_PlExchangeRates { get; set; }

        /// <summary>
        /// 单位换算表。
        /// </summary>
        public DbSet<UnitConversion> DD_UnitConversions { get; set; }

        /// <summary>
        /// 费用种类表。
        /// </summary>
        public DbSet<FeesType> DD_FeesTypes { get; set; }

        /// <summary>
        /// 业务编码规则表。
        /// </summary>
        public DbSet<JobNumberRule> DD_JobNumberRules { get; set; }

        /// <summary>
        /// 国家表。
        /// </summary>
        public DbSet<PlCountry> DD_PlCountrys { get; set; }

        /// <summary>
        /// 币种表。
        /// </summary>
        public DbSet<PlCurrency> DD_PlCurrencys { get; set; }
        #endregion 系统资源相关

        #region 多语言相关

        /// <summary>
        /// 多语言资源表。
        /// </summary>
        public DbSet<Multilingual> Multilinguals { get; set; }

        ///// <summary>
        ///// 语言字典。已并入简单字典。
        ///// </summary>
        //public DbSet<LanguageDataDic> LanguageDataDics { get; set; }
        #endregion 多语言相关

        #region 账号相关
        /// <summary>
        /// 账号表。
        /// </summary>
        public DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// 用户所属组织机构表。
        /// </summary>
        public DbSet<AccountPlOrganization> AccountPlOrganizations { get; set; }

        #endregion 账号相关

        #region 组织机构相关

        /// <summary>
        /// 商户。
        /// </summary>
        public DbSet<PlMerchant> Merchants { get; set; }

        /// <summary>
        /// 组织机构表。
        /// </summary>
        public DbSet<PlOrganization> PlOrganizations { get; set; }
        #endregion 组织机构相关
    }

    /// <summary>
    /// 数据库上下文的扩展方法。
    /// </summary>
    public static class OwDbContextExtensions
    {
    }
}
