/*
 * 文件名：DotNetDbfUtil.cs
 * 作者：OW
 * 创建日期：2025年5月8日
 * 修改日期：2025年5月8日
 * 描述：该文件包含 DotNetDBF 工具类的实现，用于操作 DBF 文件。
 */

using DotNetDBF;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OW.Data
{
    /// <summary>
    /// DotNetDBF 工具类，提供 DBF 文件操作的辅助方法。
    /// </summary>
    public static class DotNetDbfUtil
    {
        #region 类型转换
        /// <summary>
        /// 获取 DBF 字段类型。
        /// </summary>
        /// <param name="type">字段类型。</param>
        /// <returns>DBF 字段类型。</returns>
        public static NativeDbType GetDBFFieldType(Type type)
        {
            // 处理可空类型
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            return type switch
            {
                // 字符串类型
                Type t when t == typeof(string) => NativeDbType.Char,

                // 整数类型
                Type t when t == typeof(int) || t == typeof(uint) => NativeDbType.Long,
                Type t when t == typeof(long) || t == typeof(ulong) => NativeDbType.Numeric,
                Type t when t == typeof(short) || t == typeof(ushort) => NativeDbType.Numeric,
                Type t when t == typeof(byte) || t == typeof(sbyte) => NativeDbType.Numeric,

                // 浮点类型
                Type t when t == typeof(float) => NativeDbType.Float,
                Type t when t == typeof(double) => NativeDbType.Double,
                Type t when t == typeof(decimal) => NativeDbType.Numeric,

                // 日期时间类型
                Type t when t == typeof(DateTime) => NativeDbType.Date,
                Type t when t == typeof(DateTimeOffset) => NativeDbType.Date,

                // 布尔类型
                Type t when t == typeof(bool) => NativeDbType.Logical,

                // 二进制数据类型
                Type t when t == typeof(byte[]) => NativeDbType.Binary,
#if NET472_OR_GREATER || NETFRAMEWORK
                Type t when t == typeof(System.Data.Linq.Binary) => NativeDbType.Binary,
#endif
                // 其他类型默认为字符类型
                _ => NativeDbType.Char
            };
        }

        /// <summary>
        /// 根据 DBF 字段类型获取 DataTable 列的类型。
        /// </summary>
        /// <param name="dbfType">DBF 字段类型。</param>
        /// <returns>DataTable 列的类型。</returns>
        public static Type GetFieldTypeFromDBF(NativeDbType dbfType)
        {
            return dbfType switch
            {
                NativeDbType.Char => typeof(string),        // 'C' 字符型
                NativeDbType.Numeric => typeof(decimal),    // 'N' 数值型
                NativeDbType.Float => typeof(float),        // 'F' 浮点型
                NativeDbType.Date => typeof(DateTime),      // 'D' 日期型
                NativeDbType.Logical => typeof(bool),       // 'L' 逻辑型
                NativeDbType.Memo => typeof(string),        // 'M' 备注型
                NativeDbType.Binary => typeof(byte[]),      // 'B' 二进制
                NativeDbType.Long => typeof(int),           // 'I' 长整型
                NativeDbType.Double => typeof(double),      // 'O' 双精度浮点型
                NativeDbType.Autoincrement => typeof(int),  // '+' 自增型
                NativeDbType.Timestamp => typeof(DateTime), // '@' 时间戳
                NativeDbType.Ole => typeof(byte[]),         // 'G' OLE对象
                _ => typeof(string)                         // 默认为字符串
            };
        }
        #endregion

        #region 字段映射
        /// <summary>
        /// 为给定的实体类型创建自动字段映射
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="fieldMappings">现有的字段映射，没有指定的属性将自动增加映射；为null时创建新的映射</param>
        /// <returns>字段映射字典，键为DBF字段名(最多10字符)，值为实体属性名</returns>
        public static Dictionary<string, string> CreateAutoFieldMappings<T>(Dictionary<string, string> fieldMappings = null) where T : class
        {
            var entityType = typeof(T);
            
            try
            {
                // 获取所有可读的公共属性
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0) // 排除索引器属性
                    .ToArray();

                if (properties.Length == 0)
                {
                    OwHelper.SetLastErrorAndMessage(400, $"类型 {entityType.Name} 没有可用的属性用于DBF映射");
                    return fieldMappings ?? new Dictionary<string, string>();
                }

                // 初始化映射字典
                fieldMappings ??= new Dictionary<string, string>();

                // 创建反向映射，用于检查哪些属性已有映射
                var reverseMappings = fieldMappings.ToDictionary(kv => kv.Value, kv => kv.Key);

                // 为未映射的属性创建映射
                foreach (var prop in properties)
                {
                    // 跳过已有映射的属性
                    if (reverseMappings.ContainsKey(prop.Name))
                        continue;

                    // 生成DBF字段名（最多10个字符）
                    var dbfFieldName = GenerateUniqueDbfFieldName(prop.Name, fieldMappings);
                    if (!string.IsNullOrEmpty(dbfFieldName))
                    {
                        fieldMappings[dbfFieldName] = prop.Name;
                    }
                }

                return fieldMappings;
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(500, $"创建字段映射时发生错误: {ex.Message}");
                return fieldMappings ?? new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 生成唯一的DBF字段名
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="existingMappings">现有映射</param>
        /// <returns>唯一的DBF字段名，如果无法生成则返回null</returns>
        private static string GenerateUniqueDbfFieldName(string propertyName, Dictionary<string, string> existingMappings)
        {
            // 截断到10个字符
            var baseName = propertyName.Length > 10 ? propertyName[..10] : propertyName;
            
            // 如果没有冲突，直接返回
            if (!existingMappings.ContainsKey(baseName))
                return baseName;

            // 处理冲突，添加数字后缀
            for (int suffix = 1; suffix < 100; suffix++)
            {
                var suffixStr = suffix.ToString();
                var maxBaseLength = 10 - suffixStr.Length - 1; // 减去后缀长度和下划线
                if (maxBaseLength <= 0) break;

                var truncatedBase = baseName.Length > maxBaseLength ? baseName[..maxBaseLength] : baseName;
                var newFieldName = $"{truncatedBase}_{suffixStr}";
                
                if (!existingMappings.ContainsKey(newFieldName))
                    return newFieldName;
            }

            // 无法生成唯一名称
            OwHelper.SetLastErrorAndMessage(400, $"无法为属性 {propertyName} 生成唯一的DBF字段名");
            return null;
        }
        #endregion

        #region 数据写入
        /// <summary>
        /// 将实体集合写入到流中，属性将被自动转换为 .dbf 文件。
        /// 使用更安全的DBF写入器处理，避免NullReferenceException
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        /// <param name="stream">要写入的流。函数返回后，流的位置不可预知，流会保持打开状态。</param>
        /// <param name="fieldMappings">字段映射字典，键为DBF字段名，值为实体属性名。为null则自动生成</param>
        /// <param name="customFieldTypes">可选，自定义字段类型字典，键为DBF字段名，值为DBF字段类型</param>
        /// <param name="encoding">字符编码，默认为GB2312</param>
        public static void WriteToStream<T>(IEnumerable<T> entities, Stream stream,
            Dictionary<string, string> fieldMappings = null,
            Dictionary<string, NativeDbType> customFieldTypes = null,
            System.Text.Encoding? encoding = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(entities);

            // 转换为列表以便多次枚举和计数
            var entityList = entities as List<T> ?? entities.ToList();
            if (!entityList.Any())
            {
                OwHelper.SetLastErrorAndMessage(400, "实体集合为空，无法生成DBF文件");
                throw new InvalidOperationException("实体集合为空，无法生成DBF文件");
            }

            // 如果未提供映射，则创建自动映射
            fieldMappings ??= CreateAutoFieldMappings<T>();
            
            if (fieldMappings == null || !fieldMappings.Any())
            {
                throw new InvalidOperationException("无法创建有效的字段映射");
            }

            var recordsWritten = 0;
            
            try
            {
                using var wapper = new WrapperStream(stream, true); // 使用包装器流以确保正确释放资源
                using var writer = new DBFWriter(wapper);   // 使用 using 语句确保 DBFWriter 正确释放
                writer.CharEncoding = encoding ?? System.Text.Encoding.GetEncoding("GB2312"); // 默认编码

                var entityType = typeof(T);
                var properties = entityType.GetProperties();
                var propertyMap = properties.ToDictionary(p => p.Name, p => p); // 属性名查找字典

                // 定义字段定义
                var fields = new List<DBFField>();
                var validMappings = new List<KeyValuePair<string, string>>(); // 存储有效的映射
                
                foreach (var mapping in fieldMappings)
                {
                    string dbfFieldName = mapping.Key;
                    string propertyName = mapping.Value;

                    // 确保字段名符合DBF规范
                    if (dbfFieldName.Length > 10)
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"字段名 {dbfFieldName} 超过10个字符限制，已截断");
                        dbfFieldName = dbfFieldName[..10]; // 使用范围运算符简化截断操作
                    }

                    if (!propertyMap.TryGetValue(propertyName, out var property))
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"属性 {propertyName} 在类型 {entityType.Name} 中不存在，跳过处理");
                        continue;
                    }

                    NativeDbType fieldType;
                    if (customFieldTypes != null && customFieldTypes.TryGetValue(dbfFieldName, out var customType))
                    {
                        fieldType = customType; // 使用自定义类型
                    }
                    else
                    {
                        try
                        {
                            fieldType = GetDBFFieldType(property.PropertyType); // 根据属性类型确定DBF类型
                        }
                        catch (ArgumentException)
                        {
                            OwHelper.SetLastErrorAndMessage(400, $"属性 {propertyName} 的类型 {property.PropertyType.Name} 不支持，跳过处理");
                            continue;
                        }
                    }

                    // 根据字段类型创建DBFField - 修复：检查是否支持长度设置
                    try
                    {
                        DBFField field = fieldType switch
                        {
                            NativeDbType.Date => new DBFField(dbfFieldName, fieldType), // 日期类型不需要设置长度
                            NativeDbType.Logical => new DBFField(dbfFieldName, fieldType), // 逻辑类型不需要设置长度
                            NativeDbType.Char => new DBFField(dbfFieldName, fieldType, 254, 0), // 字符类型设置长度
                            NativeDbType.Numeric => new DBFField(dbfFieldName, fieldType, 18, 4), // 数字类型设置长度和小数位
                            NativeDbType.Float => new DBFField(dbfFieldName, fieldType, 18, 6), // 浮点类型设置长度和小数位
                            NativeDbType.Long => new DBFField(dbfFieldName, fieldType, 10, 0), // 长整型设置长度
                            NativeDbType.Double => new DBFField(dbfFieldName, fieldType, 18, 8), // 双精度浮点型设置长度和小数位
                            _ => new DBFField(dbfFieldName, fieldType) // 其他类型尝试默认构造
                        };

                        fields.Add(field);
                        validMappings.Add(new KeyValuePair<string, string>(dbfFieldName, propertyName));
                    }
                    catch (Exception ex)
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"创建DBF字段 {dbfFieldName} (类型: {fieldType}) 时发生错误: {ex.Message}，跳过处理");
                        continue;
                    }
                }

                // 确保至少有一个字段
                if (fields.Count == 0)
                {
                    throw new InvalidOperationException("没有有效的字段定义，无法创建DBF文件");
                }

                // 设置字段结构 - 这是关键步骤，必须在写入记录之前完成
                writer.Fields = fields.ToArray();

                // 写入记录
                foreach (var entity in entityList)
                {
                    if (entity is null)
                    {
                        OwHelper.SetLastErrorAndMessage(400, "实体集合中包含null值，跳过处理");
                        continue;
                    }

                    var record = new object?[fields.Count];
                    
                    for (int fieldIndex = 0; fieldIndex < validMappings.Count; fieldIndex++)
                    {
                        var mapping = validMappings[fieldIndex];
                        string propertyName = mapping.Value;
                        
                        if (!propertyMap.TryGetValue(propertyName, out var property))
                        {
                            record[fieldIndex] = null; // 属性不存在时使用null
                            continue;
                        }

                        var value = property.GetValue(entity); // 获取属性值

                        // 处理可空类型
                        if (value is not null && property.PropertyType.IsGenericType &&
                            property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            try
                            {
                                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType)!);
                            }
                            catch
                            {
                                OwHelper.SetLastErrorAndMessage(400, $"转换属性 {propertyName} 的值时发生错误");
                                value = null;
                            }
                        }

                        // 处理特殊值的清理
                        value = value switch
                        {
                            string strValue when strValue.Length > 254 => strValue[..254], // 截断过长字符串
                            DateTime dateValue when dateValue == DateTime.MinValue || dateValue.Year < 1900 => null, // DBF不支持过早的日期
                            _ => value
                        };

                        record[fieldIndex] = value; // 存储值到记录
                    }

                    writer.WriteRecord(record); // 写入记录到DBF
                    recordsWritten++; // 记录已写入的记录数
                }

                // 确保至少写入了一条记录
                if (recordsWritten == 0)
                {
                    OwHelper.SetLastErrorAndMessage(400, "没有有效的记录被写入DBF文件");
                    throw new InvalidOperationException("没有有效的记录被写入DBF文件");
                }

                // DBFWriter 会在 using 语句结束时自动调用 Dispose 方法完成写入
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(500, $"写入 DBF 流时发生错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 将实体集合写入DBF文件
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="fieldMappings">字段映射字典，键为DBF字段名，值为实体属性名；为null则自动创建</param>
        /// <param name="customFieldTypes">可选的自定义字段类型字典，键为DBF字段名，值为DBF字段类型</param>
        /// <param name="encoding">字符编码，默认为GB2312</param>
        public static void WriteToFile<T>(IEnumerable<T> entities, string filePath,
            Dictionary<string, string> fieldMappings = null,
            Dictionary<string, NativeDbType> customFieldTypes = null,
            System.Text.Encoding encoding = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath)); // 检查文件路径

            try
            {
                using var stream = File.Open(filePath, FileMode.Create, FileAccess.Write);
                WriteToStream(entities, stream, fieldMappings, customFieldTypes, encoding); // 调用流写入方法
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(500, $"写入 DBF 文件 {filePath} 时发生错误: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region 数据读取
        /// <summary>
        /// 将DataTable转换为实体集合
        /// </summary>
        /// <typeparam name="T">目标实体类型</typeparam>
        /// <param name="dataTable">源数据表</param>
        /// <param name="fieldMappings">字段映射，键为DBF字段名，值为实体属性名</param>
        /// <returns>实体集合</returns>
        public static IEnumerable<T> ConvertDataTableToEntities<T>(DataTable dataTable,
            Dictionary<string, string> fieldMappings) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(dataTable);
            
            if (fieldMappings == null || fieldMappings.Count == 0)
            {
                OwHelper.SetLastErrorAndMessage(400, "字段映射不能为空");
                throw new ArgumentException("字段映射不能为空", nameof(fieldMappings));
            }

            var result = new List<T>();
            var entityType = typeof(T);
            var propertyMap = entityType.GetProperties().ToDictionary(p => p.Name, p => p);

            // 反向映射，从DBF字段名映射到属性名（不区分大小写）
            var reverseMapping = fieldMappings.ToDictionary(
                kv => kv.Key.ToUpperInvariant(), 
                kv => kv.Value, 
                StringComparer.OrdinalIgnoreCase);

            // 创建列名到索引的映射（不区分大小写）
            var columnIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                columnIndexMap[dataTable.Columns[i].ColumnName] = i;
            }

            // 处理每一行数据
            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    var entity = new T();
                    bool hasValidData = false;

                    foreach (var kvp in reverseMapping)
                    {
                        var fieldName = kvp.Key;
                        var propertyName = kvp.Value;

                        // 检查属性是否存在
                        if (!propertyMap.TryGetValue(propertyName, out var property))
                        {
                            OwHelper.SetLastErrorAndMessage(400, $"属性 {propertyName} 在类型 {entityType.Name} 中不存在");
                            continue;
                        }

                        // 检查列是否存在
                        if (!columnIndexMap.TryGetValue(fieldName, out int columnIndex))
                        {
                            OwHelper.SetLastErrorAndMessage(400, $"字段 {fieldName} 在数据表中不存在");
                            continue;
                        }

                        try
                        {
                            var cellValue = row[columnIndex];
                            if (cellValue == null || cellValue == DBNull.Value)
                                continue;

                            // 转换并设置属性值
                            var convertedValue = ConvertValue(cellValue, property.PropertyType);
                            if (convertedValue != null)
                            {
                                property.SetValue(entity, convertedValue);
                                hasValidData = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            OwHelper.SetLastErrorAndMessage(400, $"转换字段 {fieldName} 到属性 {propertyName} 时发生错误: {ex.Message}");
                        }
                    }

                    // 只添加有有效数据的实体
                    if (hasValidData)
                        result.Add(entity);
                }
                catch (Exception ex)
                {
                    OwHelper.SetLastErrorAndMessage(500, $"处理数据行时发生错误: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 转换值到目标类型
        /// </summary>
        /// <param name="value">原始值</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值，失败返回null</returns>
        private static object ConvertValue(object value, Type targetType)
        {
            try
            {
                // 处理可空类型
                var isNullable = targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>);
                var actualType = isNullable ? Nullable.GetUnderlyingType(targetType) : targetType;

                // 根据目标类型进行特殊处理
                return actualType.Name switch
                {
                    nameof(String) => value.ToString().Trim(),
                    nameof(DateTime) => DateTime.TryParse(value.ToString(), out var dt) ? dt : null,
                    nameof(Boolean) => ConvertToBoolean(value.ToString()),
                    nameof(Guid) => Guid.TryParse(value.ToString(), out var guid) ? guid : null,
                    _ => Convert.ChangeType(value, actualType) // 常规类型转换
                };
            }
            catch
            {
                return null; // 转换失败返回null
            }
        }

        /// <summary>
        /// 转换字符串到布尔值
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <returns>布尔值</returns>
        private static bool ConvertToBoolean(string value)
        {
            var upperValue = value.ToUpperInvariant().Trim();
            return upperValue is "Y" or "T" or "YES" or "TRUE" or "1";
        }
        #endregion
    }
}
