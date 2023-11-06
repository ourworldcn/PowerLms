using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        /// 更新或追加一组数据。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="datas">主键若已经存在则更新数据，主键若不存在则追加数据。</param>
        public void AddOrUpdate<T>(IEnumerable<T> datas) where T : class
        {

        }

        /// <summary>
        /// 删除一组指定Id的对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="deleteIds"></param>
        public void Delete<T>(List<Guid> deleteIds)
        {
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

        #endregion 账号相关
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
        /// <param name="getIdCallback"></param>
        public static void InsertOrUpdate<T>(this DbContext context, T entity, Func<T, object> getIdCallback) where T : class
        {
            var set = context.Set<T>();
            var existingBlog = context.Set<T>().Find(getIdCallback(entity));
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
        /// 插入或更新一个实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        public static void InsertOrUpdate<T>(this DbContext context, T entity) where T : class, IEntityWithSingleKey<Guid>
        {
            var set = context.Set<T>();
            var existingBlog = context.Set<T>().Find(entity.Id);
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
