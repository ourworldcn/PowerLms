/*
 * 文件名：DotNetDBFManager.cs
 * 作者：OW
 * 创建日期：2025年2月21日
 * 修改日期：2023年2月21日
 * 描述：该文件包含 DotNetDBFManager 类的实现，用于操作 DBF 文件。
 */

using System;
using System.Data;
using System.IO;
using DotNetDBF;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OW.Data
{
    /// <summary>
    /// DotNetDBFManager 类，用于操作 DBF 文件。
    /// </summary>
    public class DotNetDBFManager
    {
        private readonly ILogger<DotNetDBFManager> _logger;

        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public DotNetDBFManager(ILogger<DotNetDBFManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 从 DBF 文件读取数据到 DataTable。
        /// </summary>
        /// <param name="filePath">DBF 文件路径。</param>
        /// <returns>包含 DBF 数据的 DataTable。</returns>
        public DataTable ReadDBFToDataTable(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogDebug("文件路径为空或仅包含空白字符。");
                throw new ArgumentNullException(nameof(filePath));
            }

            var dataTable = new DataTable();

            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new DBFReader(stream))
                {
                    reader.CharEncoding = System.Text.Encoding.UTF8;
                    var fields = reader.Fields;

                    // 创建 DataTable 列
                    foreach (var field in fields)
                    {
                        dataTable.Columns.Add(field.Name, GetFieldTypeFromDBF(field.DataType));
                    }

                    // 读取数据
                    object[] record;
                    while ((record = reader.NextRecord()) != null)
                    {
                        var dataRow = dataTable.NewRow();
                        for (int i = 0; i < fields.Length; i++)
                        {
                            dataRow[fields[i].Name] = record[i];
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }
                _logger.LogDebug("成功从文件读取数据到 DataTable。");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "读取 DBF 文件时发生错误。");
                throw;
            }

            return dataTable;
        }

        /// <summary>
        /// 将 DataTable 写入 DBF 文件。
        /// </summary>
        /// <param name="filePath">DBF 文件路径。</param>
        /// <param name="dataTable">要写入的 DataTable。</param>
        public void WriteDataTableToDBF(string filePath, DataTable dataTable)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogDebug("文件路径为空或仅包含空白字符。");
                throw new ArgumentNullException(nameof(filePath));
            }
            if (dataTable == null || dataTable.Columns.Count == 0)
            {
                _logger.LogDebug("DataTable 为空或不包含任何列。");
                throw new ArgumentNullException(nameof(dataTable));
            }

            try
            {
                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new DBFWriter(stream))
                {
                    writer.CharEncoding = System.Text.Encoding.UTF8;

                    // 获取字段信息
                    var fields = new DBFField[dataTable.Columns.Count];
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        var column = dataTable.Columns[i];
                        var fieldType = GetDBFFieldType(column.DataType);
                        fields[i] = new DBFField(column.ColumnName, fieldType);
                    }

                    writer.Fields = fields;

                    // 写入数据
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var recordData = new object[fields.Length];
                        for (int i = 0; i < fields.Length; i++)
                        {
                            recordData[i] = row[i];
                        }
                        writer.WriteRecord(recordData);
                    }
                }
                _logger.LogDebug("成功将 DataTable 写入文件。");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "写入 DBF 文件时发生错误。");
                throw;
            }
        }

        /// <summary>
        /// 获取 DBF 字段类型。
        /// </summary>
        /// <param name="type">字段类型。</param>
        /// <returns>DBF 字段类型。</returns>
        private NativeDbType GetDBFFieldType(Type type)
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
        private Type GetFieldTypeFromDBF(NativeDbType dbfType)
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


        /// <summary>
        /// 将实体集合写入流，该流可以保存为 .dbf 文件。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="stream">要写入的流</param>
        /// <param name="entities">实体集合</param>
        /// <param name="fieldMappings">字段映射字典，键为DBF字段名，值为实体属性名</param>
        /// <param name="customFieldTypes">可选的自定义字段类型字典，键为DBF字段名，值为DBF字段类型</param>
        /// <param name="encoding">字符编码，默认为UTF-8</param>
        public void WriteEntitiesToStream<T>(Stream stream, IEnumerable<T> entities,
            Dictionary<string, string> fieldMappings,
            Dictionary<string, NativeDbType> customFieldTypes = null,
            System.Text.Encoding encoding = null) where T : class
        {
            if (stream == null)
            {
                _logger.LogDebug("流对象为空。");
                throw new ArgumentNullException(nameof(stream));
            }
            if (entities == null)
            {
                _logger.LogDebug("实体集合为空。");
                throw new ArgumentNullException(nameof(entities));
            }
            if (fieldMappings == null || fieldMappings.Count == 0)
            {
                _logger.LogDebug("字段映射为空或不包含任何映射关系。");
                throw new ArgumentNullException(nameof(fieldMappings));
            }

            try
            {
                using (var writer = new DBFWriter(stream))
                {
                    writer.CharEncoding = encoding ?? System.Text.Encoding.UTF8;

                    var entityType = typeof(T);
                    var properties = entityType.GetProperties();
                    var propertyMap = properties.ToDictionary(p => p.Name, p => p);

                    // 创建字段定义
                    var fields = new List<DBFField>();
                    foreach (var mapping in fieldMappings)
                    {
                        string dbfFieldName = mapping.Key;
                        string propertyName = mapping.Value;

                        if (!propertyMap.TryGetValue(propertyName, out var property))
                        {
                            _logger.LogWarning($"属性 {propertyName} 在类型 {entityType.Name} 中不存在，将被跳过。");
                            continue;
                        }

                        NativeDbType fieldType;
                        if (customFieldTypes != null && customFieldTypes.TryGetValue(dbfFieldName, out var customType))
                        {
                            fieldType = customType;
                        }
                        else
                        {
                            try
                            {
                                fieldType = GetDBFFieldType(property.PropertyType);
                            }
                            catch (ArgumentException)
                            {
                                _logger.LogWarning($"属性 {propertyName} 的类型 {property.PropertyType.Name} 不支持，将被跳过。");
                                continue;
                            }
                        }

                        // 根据属性类型设置适当的字段长度
                        int length = 0;
                        int decimalCount = 0;

                        switch (fieldType)
                        {
                            case NativeDbType.Char:
                                length = 254; // 最大字符长度
                                break;
                            case NativeDbType.Numeric:
                                length = 18;
                                decimalCount = 0;
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
                        }

                        fields.Add(new DBFField(dbfFieldName, fieldType, length, decimalCount));
                    }

                    writer.Fields = fields.ToArray();

                    // 写入记录
                    foreach (var entity in entities)
                    {
                        var record = new object[fields.Count];
                        int fieldIndex = 0;

                        foreach (var mapping in fieldMappings)
                        {
                            if (fieldIndex >= fields.Count) break;

                            string propertyName = mapping.Value;
                            if (!propertyMap.TryGetValue(propertyName, out var property))
                            {
                                record[fieldIndex++] = null;
                                continue;
                            }

                            var value = property.GetValue(entity);
                            // 对可空类型进行处理
                            if (value != null && property.PropertyType.IsGenericType &&
                                property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType));
                            }

                            record[fieldIndex++] = value;
                        }

                        writer.WriteRecord(record);
                    }
                }
                _logger.LogDebug("成功将实体集合写入流。");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "写入 DBF 流时发生错误。");
                throw;
            }
        }

        /// <summary>
        /// 将实体集合写入 DBF 文件。
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="filePath">DBF 文件路径</param>
        /// <param name="entities">实体集合</param>
        /// <param name="fieldMappings">字段映射字典，键为DBF字段名，值为实体属性名</param>
        /// <param name="customFieldTypes">可选的自定义字段类型字典，键为DBF字段名，值为DBF字段类型</param>
        /// <param name="encoding">字符编码，默认为UTF-8</param>
        public void WriteEntitiesToDBF<T>(string filePath, IEnumerable<T> entities,
            Dictionary<string, string> fieldMappings,
            Dictionary<string, NativeDbType> customFieldTypes = null,
            System.Text.Encoding encoding = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogDebug("文件路径为空或仅包含空白字符。");
                throw new ArgumentNullException(nameof(filePath));
            }

            try
            {
                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                {
                    WriteEntitiesToStream(stream, entities, fieldMappings, customFieldTypes, encoding);
                }
                _logger.LogDebug($"成功将实体集合写入文件 {filePath}。");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"写入 DBF 文件 {filePath} 时发生错误。");
                throw;
            }
        }

    }

    public class DotNetDBFManagerTest
    {
        public static void Test()
        {
            var logger = NullLogger<DotNetDBFManager>.Instance;
            var dbfManager = new DotNetDBFManager(logger);
            var filePath = "c:\\test.dbf";

            // 创建样例 DataTable
            var dataTable = new DataTable();
            dataTable.Columns.Add("Field1", typeof(string));
            dataTable.Columns.Add("Field2", typeof(int));
            dataTable.Columns.Add("Field3", typeof(DateTime));

            var row = dataTable.NewRow();
            row["Field1"] = "Test";
            row["Field2"] = 123;
            row["Field3"] = DateTime.Now;
            dataTable.Rows.Add(row);

            // 使用 WriteDataTableToDBF 写入文件
            dbfManager.WriteDataTableToDBF(filePath, dataTable);

            // 使用 ReadDBFToDataTable 读回数据
            var result = dbfManager.ReadDBFToDataTable(filePath);

            // 输出结果
            Console.WriteLine("读取的 DataTable:");
            foreach (DataColumn column in result.Columns)
            {
                Console.Write(column.ColumnName + "\t");
            }
            Console.WriteLine();

            foreach (DataRow dataRow in result.Rows)
            {
                foreach (var item in dataRow.ItemArray)
                {
                    Console.Write(item + "\t");
                }
                Console.WriteLine();
            }

            // 删除测试文件
            File.Delete(filePath);
        }
    }
}



