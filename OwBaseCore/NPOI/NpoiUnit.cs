using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json; // 添加JSON序列化支持
using System.Threading.Tasks;

namespace NPOI
{
    /// <summary>
    /// 提供NPOI相关的扩展方法和工具类。
    /// </summary>
    static public class NpoiUnit
    {
        /// <summary>
        /// 返回指定Excel表中的字符串列表。
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="columnHead">第一行数据头</param>
        /// <returns>返回包含所有数据行的PooledList，每一行也是一个PooledList&lt;string&gt;</returns>
        /// <exception cref="ArgumentNullException">当 sheet 为 null 时抛出</exception>
        public static PooledList<PooledList<string>> GetStringList(ISheet sheet, out PooledList<string> columnHead)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet), "工作表不能为空");

            // 初始化返回值
            var result = new PooledList<PooledList<string>>();
            columnHead = new PooledList<string>();

            // 检查工作表是否有数据
            if (sheet.PhysicalNumberOfRows == 0)
            {
                return result; // 返回空的PooledList，columnHead也是空的
            }

            // 获取第一行作为表头
            var headerRow = sheet.GetRow(sheet.FirstRowNum);
            if (headerRow == null)
            {
                return result; // 如果第一行为空，返回空结果
            }

            // 提取表头数据
            var maxColumnIndex = headerRow.LastCellNum; // 获取最大列索引
            for (int colIndex = 0; colIndex < maxColumnIndex; colIndex++)
            {
                var cell = headerRow.GetCell(colIndex);
                var cellValue = GetCellStringValue(cell) ?? string.Empty; // 空单元格用空字符串
                columnHead.Add(cellValue);
            }

            // 处理数据行（从第二行开始）
            for (int rowIndex = sheet.FirstRowNum + 1; rowIndex < sheet.PhysicalNumberOfRows; rowIndex++)
            {
                var dataRow = sheet.GetRow(rowIndex);
                if (dataRow == null)
                {
                    // 空行也要处理，创建一个空字符串填充的行
                    var emptyRowData = new PooledList<string>();
                    for (int colIndex = 0; colIndex < maxColumnIndex; colIndex++)
                    {
                        emptyRowData.Add(string.Empty);
                    }
                    result.Add(emptyRowData);
                    continue;
                }

                // 创建当前行的数据列表
                var rowData = new PooledList<string>();
                
                // 按照表头的列数来处理，确保每行的列数一致
                for (int colIndex = 0; colIndex < maxColumnIndex; colIndex++)
                {
                    var cell = dataRow.GetCell(colIndex);
                    var cellValue = GetCellStringValue(cell) ?? string.Empty; // 空单元格用空字符串
                    rowData.Add(cellValue);
                }

                result.Add(rowData);
            }

            return result;
        }

        /// <summary>
        /// 提取并返回指定工作表中的数据。
        /// </summary>
        /// <remarks>抛出异常注明详细错误。</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="sheet">第一行是属性名，转换为目标属性的可写形式，<typeparamref name="T"/> 中没有的可写属性名，则忽略该列。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">当 sheet 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">当工作表格式不正确时抛出</exception>
        public static IEnumerable<T> GetSheet<T>(ISheet sheet) where T : class, new()
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet), "工作表不能为空");
            if (sheet.PhysicalNumberOfRows == 0) return Enumerable.Empty<T>(); // 空表返回空集合
            
            var result = new List<T>(); // 存储结果的集合
            var type = typeof(T); // 获取目标类型
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance) // 获取所有公共实例属性
                .Where(p => p.CanWrite) // 只要可写属性
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase); // 创建属性字典，忽略大小写
            
            var headerRow = sheet.GetRow(sheet.FirstRowNum); // 获取第一行作为表头
            if (headerRow == null) throw new InvalidOperationException("工作表第一行为空，无法获取列名");
            
            var columnMappings = new List<(int columnIndex, PropertyInfo property)>(); // 列索引到属性的映射
            
            // 遍历表头，建立列索引到属性的映射关系
            for (int columnIndex = 0; columnIndex < headerRow.PhysicalNumberOfCells; columnIndex++)
            {
                var cell = headerRow.GetCell(columnIndex);
                if (cell != null)
                {
                    var columnName = GetCellStringValue(cell)?.Trim(); // 获取列名并去除空白
                    if (!string.IsNullOrEmpty(columnName) && properties.TryGetValue(columnName, out var property))
                    {
                        columnMappings.Add((columnIndex, property)); // 如果找到匹配的属性，添加映射
                    }
                }
            }
            
            // 从第二行开始处理数据行
            for (int rowIndex = sheet.FirstRowNum + 1; rowIndex < sheet.PhysicalNumberOfRows; rowIndex++)
            {
                var dataRow = sheet.GetRow(rowIndex);
                if (dataRow == null) continue; // 跳过空行
                
                try
                {
                    var instance = new T(); // 创建目标类型实例
                    bool hasData = false; // 标记是否有有效数据
                    
                    // 遍历所有列映射，设置属性值
                    foreach (var (columnIndex, property) in columnMappings)
                    {
                        var cell = dataRow.GetCell(columnIndex);
                        if (cell != null && cell.CellType != CellType.Blank)
                        {
                            try
                            {
                                var value = ConvertCellValue(cell, property.PropertyType); // 转换单元格值
                                if (value != null || IsNullableType(property.PropertyType) || property.PropertyType == typeof(string))
                                {
                                    property.SetValue(instance, value); // 设置属性值
                                    hasData = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"第{rowIndex + 1}行第{columnIndex + 1}列数据转换失败：{ex.Message}", ex);
                            }
                        }
                        else if (IsNullableType(property.PropertyType) || property.PropertyType == typeof(string))
                        {
                            property.SetValue(instance, null); // 对于可空类型和字符串，显式设置为 null
                        }
                    }
                    
                    if (hasData) result.Add(instance); // 如果有有效数据才添加到结果集
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"处理第{rowIndex + 1}行数据时发生错误：{ex.Message}", ex);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 判断类型是否为可空值类型
        /// </summary>
        /// <param name="type">要判断的类型</param>
        /// <returns>true 表示是可空值类型</returns>
        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        
        /// <summary>
        /// 获取单元格的字符串值
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <returns>字符串值</returns>
        private static string GetCellStringValue(ICell cell)
        {
            return cell?.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue.ToString() : cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.StringCellValue, // 公式单元格尝试获取字符串值
                CellType.Blank => string.Empty,
                null => string.Empty,
                _ => string.Empty
            };
        }
        
        /// <summary>
        /// 将单元格值转换为指定类型
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值</returns>
        private static object ConvertCellValue(ICell cell, Type targetType)
        {
            if (cell == null || cell.CellType == CellType.Blank) return null;
            
            var isNullableType = IsNullableType(targetType); // 判断是否为可空类型
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType; // 处理可空类型
            
            // 优先处理 Excel 原生类型，避免不必要的字符串转换
            if (cell.CellType == CellType.Numeric && IsNumericType(underlyingType))
            {
                try
                {
                    return underlyingType switch
                    {
                        Type t when t == typeof(int) => (int)cell.NumericCellValue,
                        Type t when t == typeof(long) => (long)cell.NumericCellValue,
                        Type t when t == typeof(double) => cell.NumericCellValue,
                        Type t when t == typeof(decimal) => (decimal)cell.NumericCellValue,
                        Type t when t == typeof(float) => (float)cell.NumericCellValue,
                        Type t when t == typeof(byte) => (byte)cell.NumericCellValue,
                        Type t when t == typeof(short) => (short)cell.NumericCellValue,
                        Type t when t == typeof(DateTime) => DateUtil.IsCellDateFormatted(cell) ? cell.DateCellValue : throw new InvalidCastException("数值单元格不是日期格式"),
                        _ => throw new InvalidCastException($"不支持的数值类型：{underlyingType.Name}")
                    };
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"数值单元格转换失败：{ex.Message}", ex);
                }
            }
            
            // 处理布尔类型
            if (cell.CellType == CellType.Boolean && underlyingType == typeof(bool))
            {
                return cell.BooleanCellValue;
            }
            
            // 对于其他情况，获取字符串值并使用通用转换逻辑
            var cellStringValue = GetCellStringValue(cell);
            
            // 处理空字符串或空白字符串的情况
            if (string.IsNullOrWhiteSpace(cellStringValue))
            {
                if (isNullableType || targetType == typeof(string)) return null; // 可空类型或字符串类型返回 null
                throw new InvalidCastException($"不可空类型'{targetType.Name}'不能接受空值"); // 非可空值类型抛出异常
            }
            
            // 🔧 替换OwConvert.TryChangeType为内置Convert.ChangeType
            try
            {
                return Convert.ChangeType(cellStringValue, targetType);
            }
            catch (Exception ex)
            {
                // 如果通用转换失败，抛出简洁异常
                var detailedError = isNullableType 
                    ? $"无法将单元格值'{cellStringValue}'转换为可空类型'{targetType.Name}'"
                    : $"无法将单元格值'{cellStringValue}'转换为类型'{targetType.Name}'";
                
                throw new InvalidCastException(detailedError, ex);
            }
        }
        
        /// <summary>
        /// 判断类型是否为数值类型
        /// </summary>
        /// <param name="type">要判断的类型</param>
        /// <returns>true 表示是数值类型</returns>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(double) ||
                   type == typeof(decimal) || type == typeof(float) || type == typeof(byte) ||
                   type == typeof(short) || type == typeof(DateTime);
        }

        /// <summary>
        /// 测试GetStringList方法的示例用法
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <returns>测试结果信息</returns>
        public static string TestGetStringList(ISheet sheet)
        {
            try
            {
                // 调用GetStringList方法
                var dataRows = GetStringList(sheet, out var columnHeaders);
                
                var result = new StringBuilder();
                result.AppendLine($"工作表: {sheet.SheetName}");
                result.AppendLine($"列数: {columnHeaders.Count}");
                result.AppendLine($"数据行数: {dataRows.Count}");
                
                // 显示列标题
                result.AppendLine("列标题:");
                for (int i = 0; i < columnHeaders.Count; i++)
                {
                    result.AppendLine($"  [{i}] {columnHeaders[i]}");
                }
                
                // 显示前几行数据（最多5行）
                result.AppendLine("数据预览 (前5行):");
                var maxRowsToShow = Math.Min(5, dataRows.Count);
                for (int rowIndex = 0; rowIndex < maxRowsToShow; rowIndex++)
                {
                    var row = dataRows[rowIndex];
                    result.AppendLine($"  第{rowIndex + 1}行:");
                    for (int colIndex = 0; colIndex < row.Count; colIndex++)
                    {
                        result.AppendLine($"    [{colIndex}] {row[colIndex]}");
                    }
                }
                
                // 清理资源（PooledList实现了IDisposable）
                columnHeaders.Dispose();
                foreach (var row in dataRows)
                {
                    row.Dispose();
                }
                dataRows.Dispose();
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"测试失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 将数据集合写入Excel工作表
        /// </summary>
        /// <typeparam name="T">数据集合的元素类型</typeparam>
        /// <param name="collection">可以是空集合，此时仅写入表头</param>
        /// <param name="columnNames">要写入的列名数组，对应实体的属性名</param>
        /// <param name="sheet">目标Excel工作表</param>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <exception cref="ArgumentException">当columnNames为空数组时抛出</exception>
        /// <remarks>
        /// 🚀 **从NpoiManager迁移到NpoiUnit的静态方法**:
        /// - 第一行为列标题，从第二行开始写入数据
        /// - 通过反射获取指定属性的值并写入单元格
        /// - 支持空集合输入，此时只写入表头
        /// - 自动处理不同数据类型的单元格值设置
        /// </remarks>
        public static void WriteToExcel<T>(IEnumerable<T> collection, string[] columnNames, ISheet sheet)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (columnNames == null) throw new ArgumentNullException(nameof(columnNames));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (columnNames.Length == 0) throw new ArgumentException("列名数组不能为空", nameof(columnNames));

            if (!collection.TryGetNonEnumeratedCount(out var rowCount))
                rowCount = collection.Count(); // 行数
            int columnCount = columnNames.Length; // 列数

            // 创建属性映射，验证属性是否存在
            var typeProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            var mapping = new List<(string columnName, PropertyInfo property)>();
            foreach (var columnName in columnNames)
            {
                if (typeProperties.TryGetValue(columnName, out var property))
                {
                    mapping.Add((columnName, property));
                }
                else
                {
                    throw new ArgumentException($"类型 {typeof(T).Name} 中未找到属性 '{columnName}'", nameof(columnNames));
                }
            }

            // 设置列头（第一行）
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < columnCount; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(mapping[i].columnName);
            }

            // 写入数据行（从第二行开始）
            int rowIndex = 1;
            foreach (var item in collection)
            {
                var dataRow = sheet.CreateRow(rowIndex);
                for (int j = 0; j < columnCount; j++)
                {
                    var cell = dataRow.CreateCell(j);
                    var propertyValue = mapping[j].property.GetValue(item);

                    // 使用辅助方法设置单元格值，支持不同数据类型
                    SetCellValue(cell, propertyValue);
                }
                rowIndex++;
            }
        }

        /// <summary>
        /// 设置单元格的值，根据数据类型自动选择合适的方法
        /// </summary>
        /// <param name="cell">目标单元格</param>
        /// <param name="value">要设置的值</param>
        private static void SetCellValue(ICell cell, object value)
        {
            if (value == null)
            {
                cell.SetCellValue(string.Empty);
                return;
            }

            switch (value)
            {
                case string strValue:
                    cell.SetCellValue(strValue);
                    break;
                case int intValue:
                    cell.SetCellValue(intValue);
                    break;
                case long longValue:
                    cell.SetCellValue(longValue);
                    break;
                case double doubleValue:
                    cell.SetCellValue(doubleValue);
                    break;
                case decimal decimalValue:
                    cell.SetCellValue((double)decimalValue);
                    break;
                case float floatValue:
                    cell.SetCellValue(floatValue);
                    break;
                case DateTime dateTimeValue:
                    cell.SetCellValue(dateTimeValue);
                    break;
                case bool boolValue:
                    cell.SetCellValue(boolValue);
                    break;
                case byte byteValue:
                    cell.SetCellValue(byteValue);
                    break;
                case short shortValue:
                    cell.SetCellValue(shortValue);
                    break;
                default:
                    // 对于其他类型，使用ToString()方法
                    cell.SetCellValue(value.ToString());
                    break;
            }
        }

        /// <summary>
        /// 获取指定Excel工作表的列名 - 从NpoiManager迁移过来
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="startIndex">开始索引（可选）</param>
        /// <returns>返回列名和索引的元组列表</returns>
        /// <exception cref="ArgumentNullException">当sheet为null时抛出</exception>
        /// <remarks>
        /// 🚀 **从NpoiManager迁移到NpoiUnit的方法**:
        /// - 自动查找非空的列标题行作为有效的列名行
        /// - 支持跳过空行，直到找到有效的列标题
        /// - 返回列名和对应的列索引
        /// - 使用更现代的算法和错误处理
        /// </remarks>
        public static List<(string ColumnName, int ColumnIndex)> GetColumnNames(ISheet sheet, int? startIndex = null)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet), "工作表不能为空");
            
            var result = new List<(string, int)>();
            
            if (sheet.PhysicalNumberOfRows == 0)
                return result; // 空工作表返回空列表
            
            var currentRowIndex = startIndex ?? sheet.FirstRowNum;
            var maxRowIndex = sheet.LastRowNum;
            
            // 查找有效的列标题行
            while (currentRowIndex <= maxRowIndex)
            {
                var currentRow = sheet.GetRow(currentRowIndex);
                if (currentRow == null)
                {
                    currentRowIndex++;
                    continue;
                }
                
                result.Clear();
                var emptyColumnCount = 0;
                var totalColumns = currentRow.LastCellNum;
                
                // 提取当前行的所有列名
                for (int columnIndex = 0; columnIndex < totalColumns; columnIndex++)
                {
                    var cell = currentRow.GetCell(columnIndex);
                    var cellValue = GetCellStringValue(cell);
                    
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        emptyColumnCount++;
                        result.Add((string.Empty, columnIndex));
                    }
                    else
                    {
                        result.Add((cellValue.Trim(), columnIndex));
                    }
                }
                
                // 如果当前行没有空列（或空列数量可接受），则认为找到了有效的列标题行
                if (emptyColumnCount == 0 || (totalColumns > 0 && emptyColumnCount < totalColumns))
                {
                    // 过滤掉空列名，只返回有效的列名
                    return result.Where(x => !string.IsNullOrWhiteSpace(x.Item1)).ToList();
                }
                
                currentRowIndex++;
            }
            
            return result; // 如果没有找到有效的列标题行，返回空列表
        }

        /// <summary>
        /// 将Excel工作表转换为Json字符串 - 从NpoiManager迁移过来
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="startIndex">开始行索引，默认值0表示从第一行开始读取</param>
        /// <returns>JSON字符串</returns>
        /// <exception cref="ArgumentNullException">当sheet为null时抛出</exception>
        /// <remarks>
        /// 🚀 **从NpoiManager迁移到NpoiUnit的方法**:
        /// - 使用 GetColumnNames 方法自动获取列标题
        /// - 支持多种数据类型的单元格值处理
        /// - 返回标准的JSON格式字符串
        /// - 建议：优先使用 GetSheet&lt;T&gt; 或 GetStringList 方法
        /// </remarks>
        public static string GetJson(ISheet sheet, int startIndex = 0)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet), "工作表不能为空");
            
            var cols = GetColumnNames(sheet, startIndex);
            var list = new List<Dictionary<string, object>>();
            
            for (int rowIndex = startIndex + 1; rowIndex < sheet.PhysicalNumberOfRows; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue; // 跳过空行
                
                var dic = new Dictionary<string, object>();
                for (int columnIndex = 0; columnIndex < cols.Count; columnIndex++)
                {
                    var cell = row.GetCell(columnIndex);
                    var columnName = cols[columnIndex].ColumnName;
                    
                    dic[columnName] = cell?.CellType switch
                    {
                        CellType.String => cell.StringCellValue,
                        CellType.Numeric => DateUtil.IsCellDateFormatted(cell) ? (object)cell.DateCellValue : cell.NumericCellValue,
                        CellType.Boolean => cell.BooleanCellValue,
                        CellType.Formula => GetCellStringValue(cell), // 处理公式单元格
                        CellType.Blank => null,
                        null => null,
                        _ => GetCellStringValue(cell)
                    };
                }
                list.Add(dic);
            }
            
            return System.Text.Json.JsonSerializer.Serialize(list);
        }
    }
}
