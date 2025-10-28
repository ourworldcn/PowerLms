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
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功处理的记录数</returns>
        /// <exception cref="ArgumentNullException">当<paramref name="entities"/>或<paramref name="dbContext"/>为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当数据库操作失败时抛出</exception>
      public static int BulkInsert<TEntity>(IEnumerable<TEntity> entities, DbContext dbContext, bool ignoreExisting = true)
         where TEntity : class
        {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            var entityList = entities.ToList();
        if (entityList.Count == 0) return 0;
            var bulkConfig = CreateBulkConfig(ignoreExisting, typeof(TEntity), dbContext);
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
        /// 创建批量操作配置对象
        /// </summary>
      /// <param name="ignoreExisting">是否忽略重复数据</param>
      /// <param name="entityType">实体类型</param>
        /// <param name="dbContext">数据库上下文，用于获取实体映射元数据</param>
        /// <returns>配置对象</returns>
   private static BulkConfig CreateBulkConfig(bool ignoreExisting, Type entityType, DbContext dbContext)
        {
         var config = new BulkConfig
      {
       BatchSize = 1000,
           BulkCopyTimeout = 300,
SetOutputIdentity = false,
   PreserveInsertOrder = false,
     UseTempDB = false,
    };
       if (ignoreExisting)
      {
        config.PropertiesToExcludeOnUpdate = GetDatabaseMappedPropertiesFromMetadata(entityType, dbContext);
 }
            return config;
        }

        /// <summary>
 /// 从EF Core元数据获取实际映射到数据库的属性名列表
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>映射到数据库的属性名列表</returns>
        private static List<string> GetDatabaseMappedPropertiesFromMetadata(Type entityType, DbContext dbContext)
        {
            var entityTypeMetadata = dbContext.Model.FindEntityType(entityType);
 if (entityTypeMetadata == null)
    throw new InvalidOperationException($"找不到实体类型 {entityType.Name} 的EF Core元数据，请确保该实体已配置在DbContext中");
            return entityTypeMetadata.GetProperties()
   .Where(p => !p.IsShadowProperty())
     .Select(p => p.Name)
 .ToList();
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