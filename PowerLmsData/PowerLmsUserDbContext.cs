using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        /// 删除一组指定Id的对象。立即生效。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="deleteIds"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public int Delete<T>(List<Guid> deleteIds, DbContext db)
        {
            var ids = string.Join(',', deleteIds.Select(c => c.ToString()));
            var sql = $"Delete From Where [Id] In ({ids})";
            return db.Database.ExecuteSqlRaw(sql);
        }

        #endregion 方法

        #region 系统资源相关
        /// <summary>
        /// 系统资源表。
        /// </summary>
        public DbSet<SystemResource> SystemResources { get; set; }

        #endregion 系统资源相关

        #region 多语言相关

        /// <summary>
        /// 多语言资源表。
        /// </summary>
        public DbSet<Multilingual> Multilinguals { get; set; }

        /// <summary>
        /// 语言字典。
        /// </summary>
        public DbSet<LanguageDataDic> LanguageDataDics { get; set; }
        #endregion 多语言相关

        #region 账号相关
        /// <summary>
        /// 账号表。
        /// </summary>
        public DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// 工作状态表。
        /// </summary>
        public DbSet<WorkingStatusDataDic> WorkingStatusDataDics { get;set; }

        /// <summary>
        /// 在职状态表。
        /// </summary>
        public DbSet<IncumbencyDataDic> IncumbencyDataDics { get; set; }

        /// <summary>
        /// 性别表。
        /// </summary>
        public DbSet<GenderDataDic> GenderDataDics { get; set; }

        /// <summary>
        /// 学历表。
        /// </summary>
        public DbSet<QualificationsDataDic> QualificationsDataDics { get; set; }

        #endregion 账号相关

        #region 组织机构相关
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
        /// <summary>
        /// 插入或更新一个实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// 
        public static void InsertOrUpdate<T>(this DbContext context, T entity) where T : class
        {
            var keyPi = typeof(T).GetProperties().OfType<PropertyInfo>().First(c => c.GetCustomAttribute<KeyAttribute>() is not null);
            var set = context.Set<T>();
            var existingBlog = context.Set<T>().Find(keyPi.GetValue(entity));
            if (existingBlog == null)
            {
                set.Add(entity);
            }
            else
            {
                context.Entry(entity).CurrentValues.SetValues(entity);
            }
        }

        /// <summary>
        /// 插入或更新一组实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        public static void InsertOrUpdate<T>(this DbContext context, IEnumerable<T> entities) where T : class
        {
            var set = context.Set<T>();
            set.Load();
            var keyPi = typeof(T).GetProperties().OfType<PropertyInfo>().First(c => c.GetCustomAttribute<KeyAttribute>() is not null);
            foreach (var entity in entities)
            {
                var existingBlog = set.Find(keyPi.GetValue(entity));
                if (existingBlog is null)
                {
                    set.Add(entity);
                }
                else
                {
                    context.Entry(entity).CurrentValues.SetValues(entity);
                }
            }
        }

        /// <summary>
        /// 截断表。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tableName"></param>
        public static void TruncateTable(this DbContext context, string tableName)
        {
            var sql = $"Truncate Table {tableName}";
            context.Database.ExecuteSqlRaw(sql, tableName);
        }
    }
}
