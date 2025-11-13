/*
 * OwExtensions - Ow系列基础库的第三方框架扩展包
 * NPOI + EF Core 数据集成工具类
 * 
 * 功能说明：
 * - 提供Excel到数据库的高性能批量导入
 * - 基于OwNpoiUnit的Excel数据处理
 * - 集成EF Core批量操作
 * 
 * 技术特点：
 * - 使用PooledList减少内存分配
 * - EFCore.BulkExtensions高性能批量操作
 * - 智能属性映射和类型转换
 * 
 * 作者：Ow系列基础库开发团队
 * 创建时间：2025-02-05
 * 最后修改：2025-02-05 - 从OwDataUnit拆分NPOI相关功能
 */

using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OwExtensions.NPOI
{
    /// <summary>
    /// NPOI + EF Core 数据集成工具类，提供Excel到数据库的高性能批量导入功能
    /// </summary>
    public static class OwNpoiDbUnit
    {
        #region Excel批量插入方法

        /// <summary>
        /// 从Excel工作表批量插入数据到数据库
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="sheet">数据源：Excel工作表</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功插入的记录数</returns>
        /// <exception cref="ArgumentNullException">当<paramref name="sheet"/>或<paramref name="dbContext"/>为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当Excel处理或数据库操作失败时抛出</exception>
        public static int BulkInsertFromExcel<TEntity>(ISheet sheet, DbContext dbContext, bool ignoreExisting = true)
            where TEntity : class, new()
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            using var allRows = OwNpoiUnit.GetStringList(sheet, out var columnHeaders);
            if (columnHeaders.Count == 0 || allRows.Count == 0) return 0;
            var entities = ConvertToEntities<TEntity>(allRows, columnHeaders);
            if (entities.Count == 0) return 0;
            return BulkInsertEntities(entities, dbContext, ignoreExisting);
        }

        /// <summary>
        /// 非泛型版本 - 从Excel工作表批量插入数据
        /// </summary>
        /// <param name="sheet">数据源：Excel工作表</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="entityType">目标：实体类型</param>
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功插入的记录数</returns>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当反射调用失败时抛出</exception>
        public static int BulkInsertFromExcel(ISheet sheet, DbContext dbContext, Type entityType, bool ignoreExisting = true)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            var targetMethod = GetGenericBulkInsertMethod();
            var genericMethod = targetMethod.MakeGenericMethod(entityType);
            return (int)genericMethod.Invoke(null, new object[] { sheet, dbContext, ignoreExisting });
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 批量插入实体集合（内部使用）
        /// </summary>
        private static int BulkInsertEntities<TEntity>(List<TEntity> entities, DbContext dbContext, bool ignoreExisting)
            where TEntity : class
        {
            if (entities == null || entities.Count == 0) return 0;
            // 简化实现：直接使用 EF Core 的 AddRange + SaveChanges
            // 移除 EFCore.BulkExtensions 依赖，避免包冲突
            if (ignoreExisting)
            {
                // 忽略已存在记录：先查询主键，只添加不存在的
                var existingKeys = GetExistingPrimaryKeys(dbContext, entities);
                var entitiesToAdd = entities.Where(e => !existingKeys.Contains(GetEntityKey(dbContext, e))).ToList();
                dbContext.AddRange(entitiesToAdd);
                dbContext.SaveChanges();
                return entitiesToAdd.Count;
            }
            else
            {
                // 直接添加所有记录
                dbContext.AddRange(entities);
                dbContext.SaveChanges();
                return entities.Count;
            }
        }

        /// <summary>
        /// 获取实体的主键值
        /// </summary>
        private static object GetEntityKey<TEntity>(DbContext dbContext, TEntity entity) where TEntity : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
            var key = entityType?.FindPrimaryKey();
            if (key == null || key.Properties.Count != 1)
                throw new NotSupportedException("仅支持单主键实体");
            var property = key.Properties[0];
            return property.PropertyInfo.GetValue(entity);
        }

        /// <summary>
        /// 获取已存在的主键集合
        /// </summary>
        private static HashSet<object> GetExistingPrimaryKeys<TEntity>(DbContext dbContext, List<TEntity> entities) where TEntity : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity));
            var key = entityType?.FindPrimaryKey();
            if (key == null || key.Properties.Count != 1)
                throw new NotSupportedException("仅支持单主键实体");
            var property = key.Properties[0];
            var keys = entities.Select(e => property.PropertyInfo.GetValue(e)).Where(k => k != null).ToList();
            // 查询数据库中已存在的主键
            var dbSet = dbContext.Set<TEntity>();
            var existingEntities = dbSet.Where(e => keys.Contains(property.PropertyInfo.GetValue(e))).ToList();
            return existingEntities.Select(e => property.PropertyInfo.GetValue(e)).ToHashSet();
        }

        /// <summary>
        /// 获取泛型BulkInsertFromExcel方法的反射信息
        /// </summary>
        private static MethodInfo GetGenericBulkInsertMethod()
        {
            var methods = typeof(OwNpoiDbUnit).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var targetMethod = methods.FirstOrDefault(m =>
     m.Name == nameof(BulkInsertFromExcel) &&
     m.IsGenericMethodDefinition &&
          m.GetParameters().Length == 3 &&
     m.GetParameters()[0].ParameterType == typeof(ISheet) &&
       m.GetParameters()[1].ParameterType == typeof(DbContext) &&
      m.GetParameters()[2].ParameterType == typeof(bool));
            return targetMethod ?? throw new InvalidOperationException($"未找到匹配的泛型BulkInsertFromExcel方法");
        }

        /// <summary>
        /// 将字符串数组转换为实体列表
        /// </summary>
        private static List<TEntity> ConvertToEntities<TEntity>(PooledList<PooledList<string>> allRows, PooledList<string> columnHeaders)
          where TEntity : class, new()
        {
            var entities = new List<TEntity>();
            var propertyMapping = CreatePropertyMapping<TEntity>(columnHeaders);
            if (propertyMapping.Count == 0)
                throw new InvalidOperationException($"实体类型{typeof(TEntity).Name}没有找到匹配的属性列");
            for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
            {
                using var currentRow = allRows[rowIndex];
                var entity = CreateEntity<TEntity>(currentRow, propertyMapping);
                if (entity != null) entities.Add(entity);
            }
            return entities;
        }

        /// <summary>
        /// 从列头创建属性映射字典
        /// </summary>
        private static Dictionary<int, PropertyInfo> CreatePropertyMapping<TEntity>(PooledList<string> columnHeaders)
        {
            var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
     .Where(p => p.CanWrite)
             .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            var mapping = new Dictionary<int, PropertyInfo>();
            for (int i = 0; i < columnHeaders.Count; i++)
            {
                var columnName = columnHeaders[i]?.Trim();
                if (!string.IsNullOrEmpty(columnName) && properties.TryGetValue(columnName, out var property))
                {
                    mapping[i] = property;
                }
            }
            return mapping;
        }

        /// <summary>
        /// 从字符串行数据创建实体对象
        /// </summary>
        private static TEntity CreateEntity<TEntity>(PooledList<string> rowData, Dictionary<int, PropertyInfo> propertyMapping)
       where TEntity : class, new()
        {
            var entity = new TEntity();
            bool hasValidData = false;
            foreach (var (columnIndex, property) in propertyMapping)
            {
                if (columnIndex >= rowData.Count) continue;
                var cellValue = rowData[columnIndex];
                if (string.IsNullOrWhiteSpace(cellValue)) continue;
                var convertedValue = ConvertStringValue(cellValue, property.PropertyType);
                if (convertedValue is not null)
                {
                    property.SetValue(entity, convertedValue);
                    hasValidData = true;
                }
            }
            return hasValidData ? entity : null;
        }

        /// <summary>
        /// 字符串值类型转换
        /// </summary>
        private static object ConvertStringValue(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return actualType.Name switch
            {
                nameof(String) => value.Trim(),
                nameof(Guid) => Guid.TryParse(value, out var guid) ? guid : null,
                nameof(DateTime) => DateTime.TryParse(value, out var dateTime) ? dateTime : null,
                nameof(Boolean) => ParseBooleanValue(value),
                _ when actualType.IsEnum => Enum.TryParse(actualType, value, true, out var enumValue) ? enumValue : null,
                _ => Convert.ChangeType(value, actualType)
            };
        }

        /// <summary>
        /// 解析布尔值，支持中文和多种格式
        /// </summary>
        private static bool ParseBooleanValue(string value)
        {
            if (bool.TryParse(value, out var boolValue)) return boolValue;
            var lowerValue = value.ToLower().Trim();
            return lowerValue is "是" or "true" or "1" or "yes";
        }

        #endregion
    }
}
