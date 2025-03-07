using Microsoft.EntityFrameworkCore;
using OW.DDD;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OW.Data
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// 插入或更新一个实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="obj"></param>
        public static void AddOrUpdate<T>(this DbContext dbContext, T obj) where T : class
        {
            var keyPi = typeof(T).GetProperties().OfType<PropertyInfo>().First(c => c.GetCustomAttribute<KeyAttribute>() is not null);
            var key = keyPi.GetValue(obj);
            var set = dbContext.Set<T>();
            try
            {
                var existingEntity = dbContext.Set<T>().Find(key);
                if (existingEntity == null)
                {
                    set.Add(obj);
                }
                else
                {
                    dbContext.Entry(existingEntity).CurrentValues.SetValues(obj);
                }

            }
            catch (Exception err)
            {
                Debug.WriteLine(err.Message);
                throw;
            }
        }

        /// <summary>
        /// 插入或更新一组实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        public static void AddOrUpdate<T>(this DbContext context, IEnumerable<T> entities) where T : class
        {
            var set = context.Set<T>();
            var keyPi = typeof(T).GetProperties().OfType<PropertyInfo>().First(c => c.GetCustomAttribute<KeyAttribute>() is not null);
            foreach (var entity in entities)
            {
                var id = keyPi.GetValue(entity);
                var existingEntity = set.Find(id);
                if (existingEntity is null)
                {
                    try
                    {
                        set.Add(entity);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                else
                {
                    context.Entry(existingEntity).CurrentValues.SetValues(entity);
                }
            }
        }

        /// <summary>
        /// 扩展方法，用于清空指定的数据库表。
        /// </summary>
        /// <param name="context">数据库上下文实例。</param>
        /// <param name="tableName">要清空的表名。</param>
        public static void TruncateTable(this DbContext context, string tableName)
        {
            // 确保表名有效，防止SQL注入
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("无效的表名。", nameof(tableName));
            }

            // 查询信息架构视图，确保表名存在
            var tableExists = context.Database
                .ExecuteSqlRaw($"SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') THEN 1 ELSE 0 END");

            if (tableExists == 0)
            {
                throw new ArgumentException("无效的表名。", nameof(tableName));
            }
            // 执行 TRUNCATE TABLE SQL 命令
            context.Database.ExecuteSqlRaw($"TRUNCATE TABLE {tableName}");
        }

    }
}
