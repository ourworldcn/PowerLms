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
            var entityList = entities.ToList(); // 转换为List便于多次迭代
            if (entityList.Count == 0) return 0; // 空集合直接返回
            if (ignoreExisting) // 分支A：忽略重复数据，仅插入新数据
            {
                var primaryKeyProperties = GetPrimaryKeyProperties(typeof(TEntity), dbContext); // 获取主键属性
                if (primaryKeyProperties.Any()) // 有主键则检测重复
                {
                    var dbSet = dbContext.Set<TEntity>();
                    var existingKeys = new HashSet<object>(); // 收集待查询的主键值
                    foreach (var entity in entityList)
                    {
                        var keyValues = primaryKeyProperties.Select(p => p.GetValue(entity)).ToArray();
                        var compositeKey = keyValues.Length == 1 ? keyValues[0] : string.Join("|", keyValues); // 单键直接用，复合键用"|"连接
                        existingKeys.Add(compositeKey);
                    }
                    var existingEntities = dbSet.AsNoTracking().Where(e => existingKeys.Contains(primaryKeyProperties.Length == 1 ? primaryKeyProperties[0].GetValue(e) : string.Join("|", primaryKeyProperties.Select(p => p.GetValue(e))))).ToList(); // 查询数据库中已存在的实体（只读查询）
                    var existingKeysInDb = new HashSet<object>(); // 收集数据库中已存在的主键值
                    foreach (var entity in existingEntities)
                    {
                        var keyValues = primaryKeyProperties.Select(p => p.GetValue(entity)).ToArray();
                        var compositeKey = keyValues.Length == 1 ? keyValues[0] : string.Join("|", keyValues);
                        existingKeysInDb.Add(compositeKey);
                    }
                    entityList = entityList.Where(entity => { var keyValues = primaryKeyProperties.Select(p => p.GetValue(entity)).ToArray(); var compositeKey = keyValues.Length == 1 ? keyValues[0] : string.Join("|", keyValues); return !existingKeysInDb.Contains(compositeKey); }).ToList(); // 过滤掉已存在的实体，只保留新数据
                }
                if (entityList.Count == 0) return 0; // 全部重复，无需插入
                var bulkConfig = new BulkConfig // 批量插入配置
                {
                    BatchSize = 1000, // 每批1000条
                    BulkCopyTimeout = 300, // 超时5分钟
                    SetOutputIdentity = false, // 不回写自增主键
                    PreserveInsertOrder = false, // 不保持插入顺序（提升性能）
                    UseTempDB = false, // 不使用临时数据库（减少IO）
                };
                dbContext.BulkInsert(entityList, bulkConfig); // 执行批量插入（仅新数据）
                return entityList.Count; // 返回实际插入数量
            }
            else // 分支B：覆盖更新重复数据，插入或更新
            {
                var bulkConfig = new BulkConfig
                {
                    BatchSize = 1000,
                    BulkCopyTimeout = 300,
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    UseTempDB = false,
                };
                dbContext.BulkInsertOrUpdate(entityList, bulkConfig); // 执行批量插入或更新
                return entityList.Count; // 返回处理总数（包含插入和更新）
            }
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