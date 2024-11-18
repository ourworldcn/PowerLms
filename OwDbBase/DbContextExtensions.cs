using Microsoft.EntityFrameworkCore;
using OW.DDD;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
