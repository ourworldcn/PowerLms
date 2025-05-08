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
            var entityType = typeof(T); // 获取实体类型
            var properties = entityType.GetProperties().Where(c => c.CanRead).ToPooledListBase(); // 获取所有可读属性

            // 如果未提供映射或为空，则创建新的
            if (fieldMappings == null)
                fieldMappings = new Dictionary<string, string>();

            // 创建反向映射，用于检查哪些属性已有映射
            var reverseMappings = fieldMappings.ToDictionary(kv => kv.Value, kv => kv.Key);

            // 为未映射的属性创建映射
            foreach (var prop in properties)
            {
                // 如果属性已经有映射，则跳过
                if (reverseMappings.ContainsKey(prop.Name))
                    continue;

                // 属性名作为DBF字段名，截断到10个字符（DBF字段名长度限制）
                string dbfFieldName = prop.Name.Length > 10 ? prop.Name.Substring(0, 10) : prop.Name;

                // 避免字段名重复
                if (!fieldMappings.ContainsKey(dbfFieldName))
                {
                    fieldMappings.Add(dbfFieldName, prop.Name);
                }
                else
                {
                    // 处理字段名冲突，添加数字后缀
                    int suffix = 1;
                    string newFieldName;
                    do
                    {
                        // 确保总长度不超过10个字符
                        string baseName = dbfFieldName.Length >= 8 ? dbfFieldName.Substring(0, 8) : dbfFieldName;
                        newFieldName = $"{baseName}_{suffix++}";
                    } while (fieldMappings.ContainsKey(newFieldName) && suffix < 100); // 避免无限循环

                    if (suffix < 100) // 确保找到了可用名称
                    {
                        fieldMappings.Add(newFieldName, prop.Name);
                        OwHelper.SetLastErrorAndMessage(400, $"字段名 {dbfFieldName} 冲突，已重命名为 {newFieldName}");
                    }
                    else
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"无法为属性 {prop.Name} 创建唯一字段名，已跳过");
                    }
                }
            }

            return fieldMappings; // 返回完整的映射字典
        }
        #endregion

        #region 数据写入
        /// <summary>
        /// 将实体集合写入流，该流可以保存为 .dbf 文件。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体集合</param>
        /// <param name="stream">要写入的流</param>
        /// <param name="fieldMappings">字段映射字典，键为DBF字段名，值为实体属性名；为null则自动创建</param>
        /// <param name="customFieldTypes">可选的自定义字段类型字典，键为DBF字段名，值为DBF字段类型</param>
        /// <param name="encoding">字符编码，默认为GB2312</param>
        public static void WriteToStream<T>(IEnumerable<T> entities, Stream stream,
            Dictionary<string, string> fieldMappings = null,
            Dictionary<string, NativeDbType> customFieldTypes = null,
            System.Text.Encoding encoding = null) where T : class
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream)); // 检查流对象
            if (entities == null) throw new ArgumentNullException(nameof(entities)); // 检查实体集合

            // 如果未提供映射，则创建自动映射
            if (fieldMappings == null || fieldMappings.Count == 0)
            {
                fieldMappings = CreateAutoFieldMappings<T>();
            }

            try
            {
                using var writer = new DBFWriter(stream);
                writer.CharEncoding = encoding ?? System.Text.Encoding.GetEncoding("GB2312"); // 默认使用GB2312编码

                var entityType = typeof(T);
                var properties = entityType.GetProperties();
                var propertyMap = properties.ToDictionary(p => p.Name, p => p); // 创建属性查找字典

                // 创建字段定义
                var fields = new List<DBFField>();
                foreach (var mapping in fieldMappings)
                {
                    string dbfFieldName = mapping.Key;
                    string propertyName = mapping.Value;

                    // 确保字段名符合DBF限制（最多10个字符）
                    if (dbfFieldName.Length > 10)
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"字段名 {dbfFieldName} 超过10个字符的限制，已截断");
                        dbfFieldName = dbfFieldName.Substring(0, 10);
                    }

                    if (!propertyMap.TryGetValue(propertyName, out var property))
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"属性 {propertyName} 在类型 {entityType.Name} 中不存在，将被跳过");
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
                            OwHelper.SetLastErrorAndMessage(400, $"属性 {propertyName} 的类型 {property.PropertyType.Name} 不支持，将被跳过");
                            continue;
                        }
                    }

                    // 根据属性类型设置适当的字段长度和精度
                    int length = 0;
                    int decimalCount = 0;

                    switch (fieldType)
                    {
                        case NativeDbType.Char:
                            length = 254; // 最大字符长度
                            break;
                        case NativeDbType.Numeric:
                            length = 18;
                            decimalCount = 4; // 适当精度
                            break;
                        case NativeDbType.Float:
                            length = 18;
                            decimalCount = 6;
                            break;
                        case NativeDbType.Date:
                            length = 8;
                            break;
                        case NativeDbType.Logical:
                            length = 1;
                            break;
                        case NativeDbType.Long:
                            length = 10;
                            break;
                        default:
                            length = 254; // 默认最大长度
                            break;
                    }

                    fields.Add(new DBFField(dbfFieldName, fieldType, length, decimalCount)); // 添加字段定义
                }

                writer.Fields = fields.ToArray(); // 设置DBF文件的字段结构

                // 写入记录
                foreach (var entity in entities)
                {
                    if (entity == null)
                    {
                        OwHelper.SetLastErrorAndMessage(400, "实体集合中包含null值，已跳过");
                        continue;
                    }

                    var record = new object[fields.Count];
                    int fieldIndex = 0;

                    foreach (var mapping in fieldMappings)
                    {
                        if (fieldIndex >= fields.Count) break; // 防止索引越界

                        string propertyName = mapping.Value;
                        if (!propertyMap.TryGetValue(propertyName, out var property))
                        {
                            record[fieldIndex++] = null; // 属性不存在，值为null
                            continue;
                        }

                        var value = property.GetValue(entity); // 获取属性值

                        // 对可空类型进行处理
                        if (value != null && property.PropertyType.IsGenericType &&
                            property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            try
                            {
                                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType)); // 转换可空类型
                            }
                            catch
                            {
                                OwHelper.SetLastErrorAndMessage(400, $"转换属性 {propertyName} 的值时发生错误");
                                value = null;
                            }
                        }

                        // 处理特殊类型
                        if (value != null)
                        {
                            if (value is string strValue && strValue.Length > 254)
                            {
                                value = strValue.Substring(0, 254); // 截断过长字符串
                                OwHelper.SetLastErrorAndMessage(400, $"字符串值被截断: {propertyName}");
                            }
                            else if (value is DateTime dateValue && (dateValue == DateTime.MinValue || dateValue.Year < 1900))
                            {
                                value = null; // DBF不支持过早的日期
                                OwHelper.SetLastErrorAndMessage(400, $"日期值不支持: {propertyName}");
                            }
                        }

                        record[fieldIndex++] = value; // 存储值到记录
                    }

                    writer.WriteRecord(record); // 写入记录到DBF
                }
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

        /// <summary>
        /// 将实体集合写入文件，适用于大数据量的写入操作。耗费内存。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        /// <param name="filePath"></param>
        /// <param name="fieldMappings"></param>
        /// <param name="customFieldTypes"></param>
        /// <param name="encoding"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void WriteLargeFile<T>(IEnumerable<T> entities, string filePath,
            Dictionary<string, string> fieldMappings = null,
            Dictionary<string, NativeDbType> customFieldTypes = null,
            System.Text.Encoding encoding = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath)); // 检查文件路径
            const int memoryLimit = 1024 * 1024 * 1024; //内存阀门获取1GB内存
            MemoryStream ms = new MemoryStream(memoryLimit);// 创建内存流
            try
            {
                using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8 * 1024 * 1024);
                WriteToStream(entities, ms, fieldMappings, customFieldTypes, encoding); // 调用流写入方法
                ms.Position = 0; // 重置内存流位置
                ms.CopyTo(stream); // 将内存流内容复制到文件流
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(500, $"写入 DBF 文件 {filePath} 时发生错误: {ex.Message}");
                throw;
            }
            finally
            {
                // 释放内存流，保证分配的内存被及时回收
                ms = null;
                GC.Collect(); // 强制垃圾回收
                GC.WaitForPendingFinalizers(); // 等待所有终结器完成
                GC.Collect(); // 再次强制垃圾回收,确保LOB对象被释放
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
            if (dataTable == null) throw new ArgumentNullException(nameof(dataTable)); // 参数验证
            if (fieldMappings == null || fieldMappings.Count == 0)
            {
                OwHelper.SetLastErrorAndMessage(400, "字段映射不能为空");
                throw new ArgumentException("字段映射不能为空", nameof(fieldMappings));
            }

            var result = new List<T>(); // 结果集合
            var entityType = typeof(T); // 实体类型
            var propertyMap = entityType.GetProperties().ToDictionary(p => p.Name, p => p); // 属性字典，便于快速查找

            // 反向映射，从DBF字段名映射到属性名
            var reverseMapping = new Dictionary<string, string>();
            foreach (var mapping in fieldMappings)
            {
                if (!reverseMapping.ContainsKey(mapping.Key.ToUpper()))
                    reverseMapping.Add(mapping.Key.ToUpper(), mapping.Value);
            }

            // 创建列名到索引的映射，以便更快地查找列
            var columnIndexMap = new Dictionary<string, int>();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                columnIndexMap[dataTable.Columns[i].ColumnName.ToUpper()] = i;
            }

            // 遍历每一行数据
            foreach (DataRow row in dataTable.Rows)
            {
                var entity = new T(); // 创建新实体
                bool hasValidData = false; // 标记是否有有效数据

                // 遍历所有列
                foreach (var fieldName in reverseMapping.Keys)
                {
                    string propertyName = reverseMapping[fieldName];
                    if (!propertyMap.TryGetValue(propertyName, out var property))
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"属性 {propertyName} 在类型 {entityType.Name} 中不存在");
                        continue;
                    }

                    // 查找列索引
                    if (!columnIndexMap.TryGetValue(fieldName, out int columnIndex))
                    {
                        OwHelper.SetLastErrorAndMessage(400, $"字段 {fieldName} 在数据表中不存在");
                        continue;
                    }

                    try
                    {
                        var value = row[columnIndex]; // 获取单元格值

                        if (value == null || value == DBNull.Value)
                            continue; // 跳过空值

                        // 类型转换
                        object convertedValue = null;

                        try
                        {
                            var propertyType = property.PropertyType;
                            bool isNullable = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);

                            // 处理可空类型
                            if (isNullable)
                                propertyType = Nullable.GetUnderlyingType(propertyType);

                            // 处理字符串特殊情况
                            if (propertyType == typeof(string))
                            {
                                convertedValue = value.ToString().Trim();
                            }
                            // 处理日期特殊情况
                            else if (propertyType == typeof(DateTime))
                            {
                                if (DateTime.TryParse(value.ToString(), out var dt))
                                    convertedValue = dt;
                            }
                            // 处理布尔特殊情况
                            else if (propertyType == typeof(bool))
                            {
                                string strValue = value.ToString().ToUpper().Trim();
                                convertedValue = strValue == "Y" || strValue == "T" || strValue == "YES" ||
                                               strValue == "TRUE" || strValue == "1";
                            }
                            // 处理Guid特殊情况
                            else if (propertyType == typeof(Guid))
                            {
                                if (Guid.TryParse(value.ToString(), out var guid))
                                    convertedValue = guid;
                            }
                            // 常规类型转换
                            else
                            {
                                convertedValue = Convert.ChangeType(value, propertyType);
                            }

                            if (convertedValue != null)
                            {
                                property.SetValue(entity, convertedValue); // 设置属性值
                                hasValidData = true; // 标记有有效数据
                            }
                        }
                        catch (Exception ex)
                        {
                            OwHelper.SetLastErrorAndMessage(400, $"转换属性 {propertyName} 的值 {value} 时发生错误: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        OwHelper.SetLastErrorAndMessage(500, $"处理字段 {fieldName} 时发生错误: {ex.Message}");
                    }
                }

                // 仅添加有效数据的实体
                if (hasValidData)
                    result.Add(entity);
            }

            return result;
        }
        #endregion
    }
}
