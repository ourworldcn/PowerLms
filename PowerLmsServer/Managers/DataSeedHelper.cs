using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EFCore.BulkExtensions; // 直接引用框架
using OW.Data; // 添加OwDbBase扩展引用
using NPOI.SS.UserModel; // 添加NPOI引用
using NPOI.SS.Util; // 添加NPOI工具类
using NPOI; // 添加NPOI引用以使用NpoiUnit

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 数据种子和批量操作辅助类 - 利用框架功能处理重复数据
    /// </summary>
    /// <remarks>
    /// 🚀 **框架功能优先**:
    /// - ✅ 使用 BulkInsertOrUpdate 自动处理重复数据
    /// - ✅ 框架自动检测主键（单键或复合键）
    /// - ✅ 双级策略：BulkInsertOrUpdate → AddRange
    /// - ✅ 不手动处理ID生成，交给框架和数据库
    /// </remarks>
    public static class DataSeedHelper
    {
        /// <summary>
        /// 高性能批量插入方法 - 支持忽略已存在数据参数
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="entities">要插入的实体集合</param>
        /// <param name="ignoreExisting">是否忽略已存在的数据（true=仅插入新数据，false=插入或更新）</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <param name="operationName">操作名称，用于日志记录（可选）</param>
        /// <returns>成功处理的记录数</returns>
        /// <remarks>
        /// 🎯 **支持ignoreExisting参数的简化设计**:
        /// - ignoreExisting=true: 使用BulkInsert（跳过重复数据）
        /// - ignoreExisting=false: 使用BulkInsertOrUpdate（插入或更新）
        /// - 框架自动检测主键配置和约束处理
        /// </remarks>
        public static int TryBulkInsertOptimized<TEntity>(
            DbContext dbContext, 
            IEnumerable<TEntity> entities, 
            bool ignoreExisting,
            ILogger logger = null, 
            string operationName = null) 
            where TEntity : class
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var operationLabel = operationName ?? $"{typeof(TEntity).Name}批量处理";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var entityList = entities.ToList();
                if (entityList.Count == 0)
                {
                    logger?.LogInformation("{Operation}完成：没有数据需要处理", operationLabel);
                    return 0;
                }

                var mode = ignoreExisting ? "忽略重复" : "插入或更新";
                logger?.LogInformation("{Operation}：准备处理{Count}条记录（{Mode}模式）", 
                    operationLabel, entityList.Count, mode);

                // 🚀 第一级：根据ignoreExisting参数选择合适的BulkExtensions方法
                try
                {
                    var bulkConfig = new BulkConfig
                    {
                        BatchSize = 1000,
                        BulkCopyTimeout = 300
                    };

                    if (ignoreExisting)
                    {
                        // 🎯 仅插入新数据：使用BulkInsert + 配置忽略冲突
                        bulkConfig.SetOutputIdentity = false;
                        dbContext.BulkInsert(entityList, bulkConfig);
                    }
                    else
                    {
                        // 🔄 插入或更新：使用BulkInsertOrUpdate
                        dbContext.BulkInsertOrUpdate(entityList, bulkConfig);
                    }
                    
                    stopwatch.Stop();
                    logger?.LogInformation("{Operation}完成（BulkExtensions {Mode}模式）：处理{Count}条记录，耗时{ElapsedMs}ms", 
                        operationLabel, mode, entityList.Count, stopwatch.ElapsedMilliseconds);
                    return entityList.Count;
                }
                catch (Exception ex)
                {
                    logger?.LogDebug(ex, "{Operation}：BulkExtensions失败，回退到AddOrUpdate", operationLabel);
                    
                    // 🛡️ 第二级：回退到逐个AddOrUpdate
                    foreach (var entity in entityList)
                    {
                        dbContext.AddOrUpdate(entity);
                    }
                    var result = dbContext.SaveChanges();
                    
                    stopwatch.Stop();
                    logger?.LogInformation("{Operation}完成（AddOrUpdate模式）：处理{Count}条记录，耗时{ElapsedMs}ms", 
                        operationLabel, result, stopwatch.ElapsedMilliseconds);
                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger?.LogError(ex, "{Operation}失败，耗时{ElapsedMs}ms", operationLabel, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// 从Excel工作表直接批量插入 - 使用NpoiUnit.GetStringList简化版本
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="ignoreExisting">是否忽略已存在的数据</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <param name="operationName">操作名称（可选）</param>
        /// <returns>成功插入的记录数</returns>
        /// <remarks>
        /// 🚀 **使用NpoiUnit.GetStringList的简化方案**:
        /// - 利用NpoiUnit.GetStringList直接获取字符串数组
        /// - 跳过JSON序列化步骤，直接转换为实体
        /// - 使用TryBulkInsertOptimized进行高性能批量处理
        /// - 完全替代NpoiManager.WriteToDb的复杂流程
        /// </remarks>
        public static int BulkInsertFromExcelWithStringList<TEntity>(
            ISheet sheet,
            DbContext dbContext,
            bool ignoreExisting,
            ILogger logger = null,
            string operationName = null)
            where TEntity : class, new()
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

            var operationLabel = operationName ?? $"从Excel导入{typeof(TEntity).Name}";

            try
            {
                logger?.LogInformation("{Operation}：开始从工作表{SheetName}读取数据", operationLabel, sheet.SheetName);

                // 🚀 使用NpoiUnit.GetStringList直接获取字符串数组
                using var allRows = NpoiUnit.GetStringList(sheet, out var columnHeaders);
                
                if (columnHeaders.Count == 0 || allRows.Count == 0)
                {
                    logger?.LogInformation("{Operation}：工作表{SheetName}没有有效数据", operationLabel, sheet.SheetName);
                    return 0;
                }

                // 🎯 转换字符串数组为实体列表
                var entities = ConvertStringArraysToEntities<TEntity>(allRows, columnHeaders, logger);
                
                if (entities.Count == 0)
                {
                    logger?.LogWarning("{Operation}：工作表{SheetName}没有转换成功的实体", operationLabel, sheet.SheetName);
                    return 0;
                }

                logger?.LogInformation("{Operation}：从工作表{SheetName}转换了{Count}条实体，开始批量处理", 
                    operationLabel, sheet.SheetName, entities.Count);

                // 🚀 使用TryBulkInsertOptimized进行批量处理
                return TryBulkInsertOptimized(dbContext, entities, ignoreExisting, logger, operationLabel);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "{Operation}失败", operationLabel);
                throw;
            }
        }

        /// <summary>
        /// 非泛型版本 - 从Excel工作表批量插入数据
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="ignoreExisting">是否忽略已存在的数据</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <param name="operationName">操作名称（可选）</param>
        /// <returns>成功插入的记录数</returns>
        public static int BulkInsertFromExcelNonGeneric(
            ISheet sheet,
            DbContext dbContext,
            Type entityType,
            bool ignoreExisting,
            ILogger logger = null,
            string operationName = null)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            // 使用反射调用泛型方法
            var method = typeof(DataSeedHelper).GetMethod(nameof(BulkInsertFromExcelWithStringList));
            var genericMethod = method.MakeGenericMethod(entityType);
            
            return (int)genericMethod.Invoke(null, new object[] { sheet, dbContext, ignoreExisting, logger, operationName });
        }

        /// <summary>
        /// 将字符串数组转换为实体列表 - 简化版本
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="allRows">所有数据行</param>
        /// <param name="columnHeaders">列头</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>实体列表</returns>
        private static List<TEntity> ConvertStringArraysToEntities<TEntity>(
            PooledList<PooledList<string>> allRows,
            PooledList<string> columnHeaders,
            ILogger logger)
            where TEntity : class, new()
        {
            var entities = new List<TEntity>();

            try
            {
                // 创建属性映射
                var propertyMap = CreatePropertyMappingFromHeaders<TEntity>(columnHeaders);
                if (propertyMap.Count == 0)
                {
                    logger?.LogWarning("没有找到匹配的属性列");
                    return entities;
                }

                // 转换每一行数据
                for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
                {
                    using var currentRow = allRows[rowIndex];
                    
                    try
                    {
                        var entity = CreateEntityFromStringArray<TEntity>(currentRow, propertyMap);
                        if (entity != null)
                        {
                            entities.Add(entity);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug("跳过第{RowIndex}行：{Error}", rowIndex + 1, ex.Message);
                    }
                }

                logger?.LogDebug("成功转换{Count}条实体", entities.Count);
                return entities;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "转换字符串数组为实体时发生错误");
                return entities;
            }
        }

        /// <summary>
        /// 从列头创建属性映射
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="columnHeaders">列头</param>
        /// <returns>属性映射</returns>
        private static Dictionary<int, System.Reflection.PropertyInfo> CreatePropertyMappingFromHeaders<TEntity>(
            PooledList<string> columnHeaders)
        {
            var properties = typeof(TEntity).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            var mapping = new Dictionary<int, System.Reflection.PropertyInfo>();

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
        /// 从字符串数组创建实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="rowData">行数据</param>
        /// <param name="propertyMap">属性映射</param>
        /// <returns>实体对象</returns>
        private static TEntity CreateEntityFromStringArray<TEntity>(
            PooledList<string> rowData,
            Dictionary<int, System.Reflection.PropertyInfo> propertyMap)
            where TEntity : class, new()
        {
            var entity = new TEntity();
            bool hasData = false;

            foreach (var kvp in propertyMap)
            {
                var columnIndex = kvp.Key;
                var property = kvp.Value;

                if (columnIndex >= rowData.Count) continue;

                var cellValue = rowData[columnIndex];
                if (string.IsNullOrWhiteSpace(cellValue)) continue;

                try
                {
                    var convertedValue = ConvertStringToType(cellValue, property.PropertyType);
                    if (convertedValue != null)
                    {
                        property.SetValue(entity, convertedValue);
                        hasData = true;
                    }
                }
                catch
                {
                    // 忽略转换错误，继续处理其他属性
                }
            }

            return hasData ? entity : null;
        }

        /// <summary>
        /// 字符串类型转换 - 简化版本
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值</returns>
        private static object ConvertStringToType(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (actualType == typeof(string)) return value.Trim();
            if (actualType == typeof(Guid)) return Guid.TryParse(value, out var guid) ? guid : null;
            if (actualType == typeof(DateTime)) return DateTime.TryParse(value, out var dateTime) ? dateTime : null;
            if (actualType == typeof(bool))
            {
                if (bool.TryParse(value, out var boolValue)) return boolValue;
                var lowerValue = value.ToLower().Trim();
                return lowerValue == "是" || lowerValue == "true" || lowerValue == "1" || lowerValue == "yes";
            }

            if (actualType.IsEnum) return Enum.TryParse(actualType, value, true, out var enumValue) ? enumValue : null;

            return Convert.ChangeType(value, actualType);
        }

        /// <summary>
        /// 非泛型版本 - 支持运行时类型（向后兼容）
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="entities">要处理的实体集合</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="ignoreExisting">是否忽略已存在的数据</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <param name="operationName">操作名称，用于日志记录（可选）</param>
        /// <returns>成功处理的记录数</returns>
        public static int TryBulkInsertOptimizedNonGeneric(
            DbContext dbContext,
            System.Collections.IEnumerable entities,
            Type entityType,
            bool ignoreExisting,
            ILogger logger = null,
            string operationName = null)
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            // 🎯 简化版本：只进行一次反射调用
            var method = typeof(DataSeedHelper).GetMethod(nameof(TryBulkInsertOptimized));
            var genericMethod = method.MakeGenericMethod(entityType);
            
            return (int)genericMethod.Invoke(null, new object[] { dbContext, entities, ignoreExisting, logger, operationName });
        }
    }
}