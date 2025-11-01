/*
 * OwDbBase - Ow系列数据访问基础库
 * 数据操作工具类 - EF Core批量操作基础组件
 * 
 * 功能说明：
 * - 提供高性能的批量数据库操作功能
 * - 支持按主键忽略重复数据的纯插入策略
 * - 基于EFCore.BulkExtensions实现
 * 
 * 技术特点：
 * - 框架自动检测主键实现智能重复数据处理
 * - 支持泛型和非泛型的灵活调用方式
 * - 严格的基础库设计，错误直接抛出
 * 
 * 作者：Ow系列基础库开发团队
 * 创建时间：2024年
 * 最后修改：2025-02-05 - 移除NPOI相关功能到OwExtensions
 */

using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OW.Data
{
    /// <summary>
    /// 数据操作工具类 - EF Core批量操作基础组件
    /// 提供高性能的批量数据库操作功能
    /// </summary>
    /// <remarks>
    /// 基础设施组件设计原则：
    /// - 数据源参数优先，目标参数其次，配置参数最后
    /// - 严重错误直接抛出异常，不做任何回退处理
    /// - 框架自动检测主键实现智能重复数据处理
    /// 
    /// 注意：Excel相关功能已移至OwExtensions.OwNpoiDataUnit
    /// </remarks>
    public static class OwDataUnit
    {
        #region 核心批量插入方法

        /// <summary>
        /// 批量插入实体集合到数据库
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="entities">数据源：要插入的实体集合</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="ignoreExisting">配置：是否忽略重复数据（按主键判断）</param>
        /// <returns>实际插入的新记录数（不包括被忽略或更新的重复数据）</returns>
        /// <exception cref="ArgumentNullException">当<paramref name="entities"/>或<paramref name="dbContext"/>为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当数据库操作失败时抛出</exception>
        /// <remarks>
        /// ignoreExisting参数说明：
        /// - true（默认）：遇到主键重复时跳过该条数据，不执行任何操作（仅插入新数据）
        /// - false：遇到主键重复时覆盖更新该条数据（插入或更新）
        /// </remarks>
        public static int BulkInsert<TEntity>(IEnumerable<TEntity> entities, DbContext dbContext, bool ignoreExisting = true)
           where TEntity : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            var entityList = entities.ToList();
            if (entityList.Count == 0) return 0;
            if (ignoreExisting)
            {
                entityList = FilterExistingEntities(entityList, dbContext);
                if (entityList.Count == 0) return 0;
            }
            var bulkConfig = CreateBulkConfig();
            if (ignoreExisting)
                dbContext.BulkInsert(entityList, bulkConfig);
            else
                dbContext.BulkInsertOrUpdate(entityList, bulkConfig);
            return entityList.Count;
        }

        /// <summary>
        /// 批量插入实体集合到数据库（自定义去重字段）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">去重字段类型</typeparam>
        /// <param name="entities">数据源：要插入的实体集合</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="uniqueKeySelector">去重字段选择器，如 e => e.Code 或 e => new { e.Name, e.Type }</param>
        /// <param name="ignoreExisting">配置：是否忽略重复数据</param>
        /// <returns>实际插入的新记录数</returns>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <remarks>
        /// 使用示例：
        /// <code>
        /// // 按Code字段去重
        /// BulkInsert(items, dbContext, e => e.Code);
        /// 
        /// // 按多字段组合去重
        /// BulkInsert(items, dbContext, e => new { e.Name, e.Type });
        /// </code>
        /// </remarks>
        public static int BulkInsert<TEntity, TKey>(IEnumerable<TEntity> entities, DbContext dbContext,
            Func<TEntity, TKey> uniqueKeySelector, bool ignoreExisting = true) where TEntity : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (uniqueKeySelector == null) throw new ArgumentNullException(nameof(uniqueKeySelector));
            var entityList = entities.ToList();
            if (entityList.Count == 0) return 0;
            if (ignoreExisting)
            {
                entityList = FilterExistingEntitiesByCustomKey(entityList, dbContext, uniqueKeySelector);
                if (entityList.Count == 0) return 0;
            }
            var bulkConfig = CreateBulkConfig();
            if (ignoreExisting)
                dbContext.BulkInsert(entityList, bulkConfig);
            else
                dbContext.BulkInsertOrUpdate(entityList, bulkConfig);
            return entityList.Count;
        }

        /// <summary>
        /// 非泛型版本 - 批量插入实体集合
        /// </summary>
        /// <param name="entities">数据源：要处理的实体集合</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="entityType">目标：实体类型</param>
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功处理的记录数</returns>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当反射调用失败时抛出</exception>
        public static int BulkInsert(System.Collections.IEnumerable entities, DbContext dbContext, Type entityType, bool ignoreExisting = true)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            var targetMethod = GetGenericBulkInsertMethod();
            var genericMethod = targetMethod.MakeGenericMethod(entityType);
            return (int)genericMethod.Invoke(null, new object[] { entities, dbContext, ignoreExisting });
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 过滤已存在的实体，只保留新数据
        /// </summary>
        private static List<TEntity> FilterExistingEntities<TEntity>(List<TEntity> entityList, DbContext dbContext)
      where TEntity : class
        {
            var primaryKeyProperties = GetPrimaryKeyProperties(typeof(TEntity), dbContext);
            if (!primaryKeyProperties.Any()) return entityList;
            var existingKeys = CollectEntityKeys(entityList, primaryKeyProperties);
            var existingKeysInDb = QueryExistingKeys<TEntity>(dbContext, primaryKeyProperties, existingKeys);
            return entityList.Where(entity =>
           {
               var compositeKey = BuildCompositeKey(entity, primaryKeyProperties);
               return !existingKeysInDb.Contains(compositeKey);
           }).ToList();
        }

        /// <summary>
        /// 按自定义字段过滤已存在的实体
        /// </summary>
        private static List<TEntity> FilterExistingEntitiesByCustomKey<TEntity, TKey>(
            List<TEntity> entityList, DbContext dbContext, Func<TEntity, TKey> uniqueKeySelector)
            where TEntity : class
        {
            var dbSet = dbContext.Set<TEntity>();
            var candidateKeys = new HashSet<TKey>(entityList.Select(uniqueKeySelector));
            var allDbEntities = dbSet.AsNoTracking().ToList();
            var existingKeys = new HashSet<TKey>(allDbEntities.Select(uniqueKeySelector).Where(k => candidateKeys.Contains(k)));
            return entityList.Where(e => !existingKeys.Contains(uniqueKeySelector(e))).ToList();
        }

        /// <summary>
        /// 收集实体集合的主键值
        /// </summary>
        private static HashSet<object> CollectEntityKeys<TEntity>(List<TEntity> entityList, PropertyInfo[] primaryKeyProperties)
        {
            var keys = new HashSet<object>();
            foreach (var entity in entityList)
            {
                var compositeKey = BuildCompositeKey(entity, primaryKeyProperties);
                keys.Add(compositeKey);
            }
            return keys;
        }

        /// <summary>
        /// 构造复合主键字符串
        /// </summary>
        private static object BuildCompositeKey<TEntity>(TEntity entity, PropertyInfo[] primaryKeyProperties)
        {
            var keyValues = primaryKeyProperties.Select(p => p.GetValue(entity)).ToArray();
            return keyValues.Length == 1 ? keyValues[0] : string.Join("|", keyValues);
        }

        /// <summary>
        /// 查询数据库中已存在的主键值
        /// </summary>
        private static HashSet<object> QueryExistingKeys<TEntity>(DbContext dbContext, PropertyInfo[] primaryKeyProperties, HashSet<object> candidateKeys)
     where TEntity : class
        {
            var dbSet = dbContext.Set<TEntity>();
            var existingKeys = new HashSet<object>();
            if (primaryKeyProperties.Length == 1)
            {
                var pkProperty = primaryKeyProperties[0];
                var allDbKeys = dbSet.AsNoTracking()
                   .Select(e => EF.Property<object>(e, pkProperty.Name))
               .ToList();
                foreach (var key in allDbKeys)
                {
                    if (candidateKeys.Contains(key))
                        existingKeys.Add(key);
                }
            }
            else
            {
                var allEntities = dbSet.AsNoTracking().ToList();
                foreach (var entity in allEntities)
                {
                    var keyValues = primaryKeyProperties.Select(p => p.GetValue(entity)).ToArray();
                    var compositeKey = string.Join("|", keyValues);
                    if (candidateKeys.Contains(compositeKey))
                        existingKeys.Add(compositeKey);
                }
            }
            return existingKeys;
        }

        /// <summary>
        /// 创建批量操作配置
        /// </summary>
        private static BulkConfig CreateBulkConfig()
        {
            return new BulkConfig
            {
                BatchSize = 1000,
                BulkCopyTimeout = 300,
                SetOutputIdentity = false,
                PreserveInsertOrder = false,
                UseTempDB = false,
            };
        }

        /// <summary>
        /// 获取实体类型的主键属性
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>主键属性数组</returns>
        private static PropertyInfo[] GetPrimaryKeyProperties(Type entityType, DbContext dbContext)
        {
            var entityTypeMetadata = dbContext.Model.FindEntityType(entityType);
            if (entityTypeMetadata == null)
                throw new InvalidOperationException($"找不到实体类型 {entityType.Name} 的EF Core元数据，请确保该实体已配置在DbContext中");
            var primaryKey = entityTypeMetadata.FindPrimaryKey();
            if (primaryKey == null)
                throw new InvalidOperationException($"实体类型 {entityType.Name} 未配置主键");
            return primaryKey.Properties.Select(p => entityType.GetProperty(p.Name)).Where(p => p != null).ToArray();
        }

        /// <summary>
        /// 获取泛型BulkInsert方法的反射信息
        /// </summary>
        /// <returns>方法信息</returns>
        /// <exception cref="InvalidOperationException">未找到匹配方法时抛出</exception>
        private static MethodInfo GetGenericBulkInsertMethod()
        {
            var methods = typeof(OwDataUnit).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var targetMethod = methods.FirstOrDefault(m =>
              m.Name == nameof(BulkInsert) &&
            m.IsGenericMethodDefinition &&
                   m.GetParameters().Length == 3 &&
          m.GetParameters()[0].ParameterType.IsGenericType &&
          m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                   m.GetParameters()[1].ParameterType == typeof(DbContext) &&
            m.GetParameters()[2].ParameterType == typeof(bool));
            return targetMethod ?? throw new InvalidOperationException($"未找到匹配的泛型BulkInsert方法");
        }

        #endregion
    }
}