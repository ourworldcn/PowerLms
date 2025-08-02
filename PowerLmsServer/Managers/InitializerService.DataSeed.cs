using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PowerLmsServer.EfData;
using System.Reflection;
using Microsoft.Extensions.Logging;
using OW.Data;
using NPOI; // 添加NPOI引用以使用NpoiUnit.GetStringList
using System.Collections.Concurrent; // 添加并发字典支持

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 初始化服务的数据种子部分 - 使用NpoiUnit.GetStringList优化版本
    /// </summary>
    public partial class InitializerService
    {
        /// <summary>
        /// 从Excel文件初始化数据库数据 - 使用NpoiUnit.GetStringList大幅简化代码
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <returns>是否成功初始化</returns>
        public bool InitializeDataFromExcel(PowerLmsUserDbContext db)
        {
            try
            {
                _Logger.LogInformation("开始从Excel文件初始化数据库数据（GetStringList优化版本）");

                // 获取Excel文件路径
                var excelFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PowerLmsData", "系统资源", "预初始化数据.xlsx");
                
                if (!File.Exists(excelFilePath))
                {
                    _Logger.LogWarning("Excel初始化文件不存在: {FilePath}", excelFilePath);
                    return false;
                }

                using var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read);
                var workbook = new XSSFWorkbook(fileStream);
                
                var processedSheets = 0;
                var totalInserted = 0;
                var dbType = typeof(PowerLmsUserDbContext);

                // 使用PooledList收集处理结果
                using var processedSheetNames = new PooledList<string>(workbook.NumberOfSheets);
                using var errorMessages = new PooledList<string>(workbook.NumberOfSheets);

                // 遍历所有工作表
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    var sheetName = sheet.SheetName;

                    try
                    {
                        // 验证DbSet是否存在
                        var dbSetProperty = dbType.GetProperty(sheetName);
                        if (dbSetProperty == null)
                        {
                            _Logger.LogWarning("跳过工作表：{SheetName}，未找到对应的数据库表", sheetName);
                            continue;
                        }

                        var dbSetPropertyType = dbSetProperty.PropertyType;
                        if (!dbSetPropertyType.IsGenericType || dbSetPropertyType.GetGenericTypeDefinition() != typeof(DbSet<>))
                        {
                            _Logger.LogWarning("跳过工作表：{SheetName}，对应属性不是DbSet类型", sheetName);
                            continue;
                        }

                        var entityType = dbSetPropertyType.GetGenericArguments()[0];
                        var dbSetValue = dbSetProperty.GetValue(db);

                        if (dbSetValue == null)
                        {
                            _Logger.LogWarning("跳过工作表：{SheetName}，DbSet实例为null", sheetName);
                            continue;
                        }

                        // 🚀 关键优化：使用GetStringList一次性获取所有数据
                        var insertedCount = ProcessSheetWithGetStringList(sheet, db, dbSetValue, entityType);
                        totalInserted += insertedCount;
                        processedSheets++;
                        processedSheetNames.Add($"{sheetName}({insertedCount}条记录)");

                        _Logger.LogInformation("成功处理工作表：{SheetName}，实体类型：{EntityType}，插入记录：{InsertedCount}", 
                            sheetName, entityType.Name, insertedCount);
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"工作表[{sheetName}]: {ex.Message}");
                        _Logger.LogError(ex, "处理工作表失败：{SheetName}", sheetName);
                    }
                }

                // 记录处理结果
                if (processedSheetNames.Count > 0)
                {
                    _Logger.LogInformation("成功处理的工作表: {ProcessedSheets}", string.Join(", ", processedSheetNames));
                }
                
                if (errorMessages.Count > 0)
                {
                    _Logger.LogWarning("处理错误汇总: {ErrorMessages}", string.Join("; ", errorMessages));
                }

                _Logger.LogInformation("数据初始化完成，处理工作表：{ProcessedSheets}，总插入记录：{TotalInserted}", 
                    processedSheets, totalInserted);

                return true;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "从Excel文件初始化数据时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 使用GetStringList处理单个工作表 - 大幅简化的版本
        /// </summary>
        /// <param name="sheet">工作表</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="dbSet">对应的DbSet</param>
        /// <param name="entityType">实体类型</param>
        /// <returns>插入的记录数</returns>
        private int ProcessSheetWithGetStringList(ISheet sheet, PowerLmsUserDbContext db, object dbSet, Type entityType)
        {
            try
            {
                // 🚀 使用NpoiUnit.GetStringList一次性获取所有数据
                using var allRows = NpoiUnit.GetStringList(sheet, out var columnHeaders);
                
                if (columnHeaders.Count == 0)
                {
                    _Logger.LogDebug("工作表 {SheetName} 没有列头", sheet.SheetName);
                    return 0;
                }

                if (allRows.Count == 0)
                {
                    _Logger.LogDebug("工作表 {SheetName} 没有数据行", sheet.SheetName);
                    return 0;
                }

                // 获取实体属性映射
                var propertyMap = CreatePropertyMapping(entityType, columnHeaders);
                if (propertyMap.Count == 0)
                {
                    _Logger.LogWarning("工作表 {SheetName} 没有找到匹配的属性", sheet.SheetName);
                    return 0;
                }

                // 获取主键属性
                var primaryKeyProperty = GetPrimaryKeyProperty(entityType);

                // 使用PooledList存储要插入的实体
                using var entitiesToInsert = new PooledList<object>(allRows.Count);
                using var validationErrors = new PooledList<string>(allRows.Count / 10);

                // 🎯 核心优化：批量处理所有行数据
                for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
                {
                    using var currentRow = allRows[rowIndex]; // 当前行数据
                    
                    try
                    {
                        var entity = CreateEntityFromStringRow(entityType, currentRow, columnHeaders, propertyMap);
                        if (entity != null)
                        {
                            // 检查是否已存在
                            if (primaryKeyProperty == null || !IsEntityExists(db, dbSet, entity, primaryKeyProperty))
                            {
                                entitiesToInsert.Add(entity);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        validationErrors.Add($"第{rowIndex + 2}行: {ex.Message}"); // +2因为Excel从1开始，且跳过了表头
                    }
                }

                // 记录验证错误
                if (validationErrors.Count > 0)
                {
                    var errorSample = string.Join("; ", validationErrors.Take(3));
                    _Logger.LogWarning("工作表 {SheetName} 有 {ErrorCount} 个数据错误，示例: {ErrorSample}", 
                        sheet.SheetName, validationErrors.Count, errorSample);
                }

                // 批量插入
                if (entitiesToInsert.Count > 0)
                {
                    return BulkInsertEntities(db, dbSet, entitiesToInsert, entityType, sheet.SheetName);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "使用GetStringList处理工作表 {SheetName} 时发生错误", sheet.SheetName);
                return 0;
            }
        }

        /// <summary>
        /// 创建属性映射 - 将列名与实体属性关联
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <param name="columnHeaders">列头列表</param>
        /// <returns>列索引到属性的映射</returns>
        private Dictionary<int, PropertyInfo> CreatePropertyMapping(Type entityType, PooledList<string> columnHeaders)
        {
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
        /// 从字符串行数据创建实体对象 - 大幅简化的版本
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <param name="rowData">行数据</param>
        /// <param name="columnHeaders">列头</param>
        /// <param name="propertyMap">属性映射</param>
        /// <returns>创建的实体对象</returns>
        private object CreateEntityFromStringRow(Type entityType, PooledList<string> rowData, 
            PooledList<string> columnHeaders, Dictionary<int, PropertyInfo> propertyMap)
        {
            var entity = Activator.CreateInstance(entityType);
            if (entity == null) return null;

            bool hasData = false;

            for (int colIndex = 0; colIndex < Math.Min(rowData.Count, columnHeaders.Count); colIndex++)
            {
                if (!propertyMap.TryGetValue(colIndex, out var property)) continue;

                var cellValue = colIndex < rowData.Count ? rowData[colIndex] : string.Empty;
                if (string.IsNullOrWhiteSpace(cellValue)) continue;

                try
                {
                    var convertedValue = ConvertStringToPropertyType(cellValue, property.PropertyType);
                    if (convertedValue != null)
                    {
                        property.SetValue(entity, convertedValue);
                        hasData = true;
                    }
                }
                catch (Exception ex)
                {
                    _Logger.LogDebug("转换属性 {PropertyName} 值 '{CellValue}' 失败: {Error}", 
                        property.Name, cellValue, ex.Message);
                }
            }

            return hasData ? entity : null;
        }

        /// <summary>
        /// 将字符串转换为指定的属性类型 - 简化版本
        /// </summary>
        /// <param name="stringValue">字符串值</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值</returns>
        private object ConvertStringToPropertyType(string stringValue, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(stringValue)) return null;

            try
            {
                // 处理可空类型
                var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // 字符串类型直接返回
                if (actualType == typeof(string)) return stringValue.Trim();

                // Guid类型
                if (actualType == typeof(Guid))
                {
                    return Guid.TryParse(stringValue, out var guid) ? guid : null;
                }

                // DateTime类型
                if (actualType == typeof(DateTime))
                {
                    return DateTime.TryParse(stringValue, out var dateTime) ? dateTime : null;
                }

                // 布尔类型
                if (actualType == typeof(bool))
                {
                    if (bool.TryParse(stringValue, out var boolValue)) return boolValue;
                    var lowerValue = stringValue.ToLower().Trim();
                    return lowerValue == "是" || lowerValue == "true" || lowerValue == "1" || lowerValue == "yes";
                }

                // 数值类型
                if (actualType.IsNumericType())
                {
                    return Convert.ChangeType(stringValue, actualType);
                }

                // 🔧 替换OwConvert.TryChangeType为内置Convert.ChangeType
                return Convert.ChangeType(stringValue, actualType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 批量插入实体 - 优化版本，使用DataSeedHelper
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="dbSet">DbSet</param>
        /// <param name="entities">要插入的实体列表</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="sheetName">工作表名称</param>
        /// <returns>插入的记录数</returns>
        private int BulkInsertEntities(PowerLmsUserDbContext db, object dbSet, PooledList<object> entities, Type entityType, string sheetName)
        {
            try
            {
                // 分批处理大量数据
                const int batchSize = 1000;
                int totalInserted = 0;

                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    var batchCount = Math.Min(batchSize, entities.Count - i);
                    var batch = entities.Skip(i).Take(batchCount).ToList();

                    // 🚀 使用DataSeedHelper的非泛型优化方法，避免反射开销
                    var insertedInBatch = DataSeedHelper.TryBulkInsertOptimizedNonGeneric(
                        db, batch, entityType, true, _Logger, $"工作表{sheetName}批量插入");

                    totalInserted += insertedInBatch;
                }

                _Logger.LogInformation("工作表 {SheetName} 批量插入完成，总计插入：{Count} 条记录", sheetName, totalInserted);
                return totalInserted;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量插入实体失败");
                return 0;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 获取实体的主键属性
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <returns>主键属性</returns>
        private PropertyInfo GetPrimaryKeyProperty(Type entityType)
        {
            var keyProperty = entityType.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() != null);

            if (keyProperty == null)
            {
                keyProperty = entityType.GetProperty("Id");
            }

            return keyProperty;
        }

        /// <summary>
        /// 检查实体是否已存在于数据库中
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="dbSet">DbSet</param>
        /// <param name="entity">实体对象</param>
        /// <param name="primaryKeyProperty">主键属性</param>
        /// <returns>是否存在</returns>
        private bool IsEntityExists(PowerLmsUserDbContext db, object dbSet, object entity, PropertyInfo primaryKeyProperty)
        {
            try
            {
                var primaryKeyValue = primaryKeyProperty.GetValue(entity);
                if (primaryKeyValue == null) return false;

                var findMethod = dbSet.GetType().GetMethod("Find");
                if (findMethod == null) return false;

                var existingEntity = findMethod.Invoke(dbSet, new[] { primaryKeyValue });
                return existingEntity != null;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// 类型扩展方法
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// 判断类型是否为数值类型
        /// </summary>
        public static bool IsNumericType(this Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                   type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(ushort) || type == typeof(sbyte) || type == typeof(decimal) ||
                   type == typeof(double) || type == typeof(float);
        }
    }
}