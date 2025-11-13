using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using OW.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace OwExtensions.NPOI
{
    /// <summary>
    /// NPOI 扩展方法类
    /// </summary>
    public static class OwNpoiExtensions
    {
        /// <summary>
        /// 从Excel Sheet读取数据并转换为实体集合
        /// 自动映射列标题到实体属性，跳过无法映射的列和无数据的行
        /// </summary>
        /// <typeparam name="T">实体类型，必须有无参构造函数</typeparam>
        /// <param name="sheet">Excel工作表，第一行为列标题</param>
        /// <param name="dest">目标集合，读取的实体将被添加到此集合</param>
        /// <param name="excludedProperties">要排除的属性名称集合（不区分大小写），默认为null表示导入所有可写属性</param>
        /// <returns>实际读取并添加到集合的记录数</returns>
        /// <remarks>
        /// 使用示例：
        /// <code>
        /// var entities = new List&lt;MyEntity&gt;();
        /// // 导入所有属性
        /// sheet.ReadEntities(entities);
        /// // 排除Id和OrgId属性
        /// sheet.ReadEntities(entities, new[] { "Id", "OrgId" });
        /// </code>
        /// </remarks>
        public static int ReadEntities<T>(this ISheet sheet, ICollection<T> dest, IEnumerable<string>? excludedProperties = null)
              where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(dest);
            if (sheet.LastRowNum < 1) return 0;
            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return 0;
            // 创建排除属性名的HashSet（不区分大小写）
            // 性能优化：如果传入的已经是使用正确比较器的HashSet，直接复用
            var excludedSet = excludedProperties switch
            {
                null => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                HashSet<string> { Comparer: StringComparer comparer } hs when comparer == StringComparer.OrdinalIgnoreCase => hs,
                _ => new HashSet<string>(excludedProperties, StringComparer.OrdinalIgnoreCase)
            };
            // 获取可写属性并排除指定属性
            var properties = typeof(T).GetProperties()
               .Where(p => p.CanWrite && !excludedSet.Contains(p.Name))
               .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            // 预先计算列映射
            var columnMappings = new Dictionary<int, PropertyInfo>();
            for (int i = 0; i <= headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.StringCellValue))
                {
                    var columnName = cell.StringCellValue.Trim();
                    if (properties.TryGetValue(columnName, out var prop))
                    {
                        columnMappings[i] = prop;
                    }
                }
            }
            if (columnMappings.Count == 0) return 0;
            // 读取数据行
            int addedCount = 0;
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;
                var entity = new T();
                bool hasData = false;
                // 填充属性
                foreach (var (colIndex, prop) in columnMappings)
                {
                    var cell = row.GetCell(colIndex);
                    if (cell != null)
                    {
                        var value = cell.GetValue(prop.PropertyType);
                        if (value != null)
                        {
                            prop.SetValue(entity, value);
                            hasData = true;
                        }
                    }
                }
                // 只添加有数据的行
                if (hasData)
                {
                    dest.Add(entity);
                    addedCount++;
                }
            }
            return addedCount;
        }

        /// <summary>
        /// 从Excel Sheet导入数据到DbContext。自动映射列到属性，支持字典加速重复判断。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TKey">用于判断重复的键类型</typeparam>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="keySelector">从实体提取键的委托</param>
        /// <param name="loadAll">预加载数据的查询表达式，为null时使用DbContext已有缓存</param>
        /// <param name="excludedProperties">要排除的属性名称集合（不区分大小写），默认为null表示导入所有可写属性</param>
        /// <param name="entityInitializer">实体初始化器，用于设置实体的额外属性（如Id、DataDicId等）</param>
        /// <returns>导入的记录数</returns>
        /// <remarks>
        /// 使用示例：
        /// <code>
        /// // 排除Id和DataDicId，由entityInitializer设置
        /// sheet.ImportToDbContext(
        ///     dbContext, 
        ///     e => e.Code,
        ///     loadAll: x => x.DataDicId == catalogId,
        ///     excludedProperties: new[] { "Id", "DataDicId" },
        ///     entityInitializer: e => { e.Id = Guid.NewGuid(); e.DataDicId = catalogId; }
        /// );
        /// </code>
        /// </remarks>
        public static int ImportToDbContext<T, TKey>(this ISheet sheet, DbContext dbContext, Func<T, TKey> keySelector,
            Expression<Func<T, bool>>? loadAll = null, IEnumerable<string>? excludedProperties = null, Action<T>? entityInitializer = null)
            where T : class, new() where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(keySelector);
            if (sheet.LastRowNum < 1) return 0;
            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return 0;
            // 创建排除属性名的HashSet（不区分大小写）
            // 性能优化：如果传入的已经是使用正确比较器的HashSet，直接复用
            var excludedSet = excludedProperties switch
            {
                null => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                HashSet<string> { Comparer: StringComparer comparer } hs when comparer == StringComparer.OrdinalIgnoreCase => hs,
                _ => new HashSet<string>(excludedProperties, StringComparer.OrdinalIgnoreCase)
            };
            // 获取属性映射（排除指定属性）
            var properties = typeof(T).GetProperties()
               .Where(p => p.CanWrite && !excludedSet.Contains(p.Name))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            // 预先计算列映射
            var columnMappings = new Dictionary<int, PropertyInfo>();
            for (int i = 0; i <= headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null && properties.TryGetValue(cell.StringCellValue?.Trim() ?? "", out var prop))
                    columnMappings[i] = prop;
            }
            // 读取数据行
            var entities = new List<T>();
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;
                var entity = new T();
                bool hasData = false;
                // 填充属性
                foreach (var (colIndex, prop) in columnMappings)
                {
                    var cell = row.GetCell(colIndex);
                    if (cell != null)
                    {
                        var value = cell.GetValue(prop.PropertyType);
                        if (value != null)
                        {
                            prop.SetValue(entity, value);
                            hasData = true;
                        }
                    }
                }
                if (!hasData) continue;
                // 执行实体初始化器（设置Id、DataDicId等）
                entityInitializer?.Invoke(entity);
                entities.Add(entity);
            }
            // 使用 DbContext.AddOrUpdate 扩展方法批量处理
            if (entities.Any())
            {
                dbContext.AddOrUpdate(source: entities, loadAll: loadAll, keySelector: keySelector);
                return entities.Count;
            }
            return 0;
        }

        /// <summary>
        /// 获取Excel单元格值并转换为目标类型
        /// </summary>
        /// <param name="cell">Excel单元格</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值，转换失败返回null</returns>
        /// <remarks>
        /// 转换策略：
        /// 1. 优先尝试字符串转换（覆盖90%+场景）
        /// 2. 数字类型使用 OwConvert.TryConvertNumeric（高性能，溢出返回null）
        /// 3. 布尔类型直接处理（极少场景）
        /// 4. 使用 OwConvert.TryChangeType 统一转换逻辑
        /// </remarks>
        public static object? GetValue(this ICell cell, Type targetType)
        {
            if (cell == null) return null;
            var cellType = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;
            // 获取实际类型（处理 Nullable）
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            switch (cellType)
            {
                case CellType.String:
                    // 字符串类型：使用 OwConvert.TryChangeType 高性能转换
                    return ConvertFromString(cell.StringCellValue, targetType);
                case CellType.Numeric:
                    // 数字类型：使用 OwConvert.TryConvertNumeric 高性能转换
                    var numericValue = cell.NumericCellValue;
                    // 日期类型特殊处理
                    if (actualType == typeof(DateTime))
                    {
                        try
                        {
                            return cell.DateCellValue;
                        }
                        catch
                        {
                            // 如果不是有效日期，回退到字符串转换
                            return ConvertFromString(numericValue.ToString(), targetType);
                        }
                    }
                    // ✅ 使用 OwConvert.TryConvertNumeric（高性能，溢出安全）
                    if (OwConvert.TryConvertNumeric(numericValue, targetType, out var convertedValue))
                    {
                        return convertedValue;
                    }
                    // 转换失败（溢出或类型不匹配），尝试字符串中转
                    return ConvertFromString(numericValue.ToString(), targetType);
                case CellType.Boolean:
                    // 布尔类型：直接处理
                    var boolValue = cell.BooleanCellValue;
                    if (actualType == typeof(bool)) return boolValue;
                    if (actualType == typeof(string)) return boolValue.ToString();
                    if (actualType == typeof(int)) return boolValue ? 1 : 0;
                    // 其他情况：通过字符串中转
                    return ConvertFromString(boolValue.ToString(), targetType);
                case CellType.Blank:
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 从字符串转换为目标类型，使用 OwConvert.TryChangeType 高性能转换
        /// </summary>
        private static object? ConvertFromString(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            // 特殊处理：布尔类型支持中文（TryChangeType 不支持）
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (actualType == typeof(bool))
            {
                var lowerValue = value.ToLowerInvariant();
                if (lowerValue is "是" or "true" or "1" or "yes" or "y") return true;
                if (lowerValue is "否" or "false" or "0" or "no" or "n") return false;
                // 回退到标准解析
                return OwConvert.TryChangeType(value, targetType, out var boolResult) ? boolResult : null;
            }
            // 使用 OwConvert.TryChangeType 处理所有其他类型
            return OwConvert.TryChangeType(value, targetType, out var result) ? result : null;
        }
    }
}

#nullable restore
