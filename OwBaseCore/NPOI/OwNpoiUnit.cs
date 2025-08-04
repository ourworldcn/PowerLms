/*
 * PowerLms - 货运物流业务管理系统
 * NPOI扩展工具类 - 高性能Excel数据处理核心组件
 * 
 * 功能说明：
 * - 提供零装箱的Excel到JSON转换
 * - 支持高性能的PooledList数据处理
 * - 集成WrapperStream解决流管理问题
 * - 实体转换和字符串数组处理
 * 
 * 技术特点：
 * - 使用PooledList减少内存分配
 * - 零装箱技术确保高性能
 * - WrapperStream确保流资源安全
 * - 统一的单元格值处理逻辑
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年12月 - 重构优化，消除代码重复
 */

using NPOI.SS.UserModel;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NPOI
{
    /// <summary>
    /// 提供NPOI相关的扩展方法和工具类。
    /// 高性能Excel数据处理的基础库组件，支持字符串数组处理和实体转换。
    /// </summary>
    static public class OwNpoiUnit
    {
        #region Excel数据读取方法

        /// <summary>
        /// 返回指定Excel表中的字符串列表。
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="columnHead">第一行数据头</param>
        /// <returns>返回包含所有数据行的PooledList，每一行也是一个PooledList&lt;string&gt;</returns>
        /// <exception cref="ArgumentNullException">当 sheet 为 null 时抛出</exception>
        public static PooledList<PooledList<string>> GetStringList(ISheet sheet, out PooledList<string> columnHead)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            var result = new PooledList<PooledList<string>>();
            columnHead = new PooledList<string>();
            if (sheet.PhysicalNumberOfRows == 0) return result; // 空工作表返回空PooledList
            var headerRow = sheet.GetRow(sheet.FirstRowNum);
            if (headerRow == null) return result; // 如果第一行为空，返回空结果
            var maxColumnIndex = headerRow.LastCellNum; // 获取最大列索引
            for (int colIndex = 0; colIndex < maxColumnIndex; colIndex++) // 提取表头数据
            {
                var cell = headerRow.GetCell(colIndex);
                var cellValue = GetCellStringValue(cell) ?? string.Empty; // 空单元格用空字符串
                columnHead.Add(cellValue);
            }
            for (int rowIndex = sheet.FirstRowNum + 1; rowIndex < sheet.PhysicalNumberOfRows; rowIndex++) // 处理数据行（从第二行开始）
            {
                var dataRow = sheet.GetRow(rowIndex);
                if (dataRow == null) // 空行也要处理，创建一个空字符串填充的行
                {
                    var emptyRowData = new PooledList<string>();
                    for (int colIndex = 0; colIndex < maxColumnIndex; colIndex++)
                    {
                        emptyRowData.Add(string.Empty);
                    }
                    result.Add(emptyRowData);
                    continue;
                }
                var rowData = new PooledList<string>(); // 创建当前行的数据列表
                for (int colIndex = 0; colIndex < maxColumnIndex; colIndex++) // 按照表头的列数来处理，确保每行的列数一致
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
        /// <typeparam name="T">目标实体类型</typeparam>
        /// <param name="sheet">第一行是属性名，转换为目标属性的可写形式，<typeparamref name="T"/> 中没有的可写属性名，则忽略该列。</param>
        /// <returns>实体集合</returns>
        /// <exception cref="ArgumentNullException">当 sheet 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">当工作表格式不正确时抛出</exception>
        public static IEnumerable<T> GetSheet<T>(ISheet sheet) where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(sheet);
            if (sheet.PhysicalNumberOfRows == 0) return Enumerable.Empty<T>(); // 空表返回空集合
            var result = new List<T>(); // 存储结果的集合
            var type = typeof(T); // 获取目标类型
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance) // 获取所有公共实例属性
                .Where(p => p.CanWrite) // 只要可写属性
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase); // 创建属性字典，忽略大小写
            var headerRow = sheet.GetRow(sheet.FirstRowNum); // 获取第一行作为表头
            if (headerRow == null) throw new InvalidOperationException("工作表第一行为空，无法获取列名");
            var columnMappings = new List<(int columnIndex, PropertyInfo property)>(); // 列索引到属性的映射
            for (int columnIndex = 0; columnIndex < headerRow.PhysicalNumberOfCells; columnIndex++) // 遍历表头，建立列索引到属性的映射关系
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
            for (int rowIndex = sheet.FirstRowNum + 1; rowIndex < sheet.PhysicalNumberOfRows; rowIndex++) // 从第二行开始处理数据行
            {
                var dataRow = sheet.GetRow(rowIndex);
                if (dataRow == null) continue; // 跳过空行
                try
                {
                    var instance = new T(); // 创建目标类型实例
                    bool hasData = false; // 标记是否有有效数据
                    foreach (var (columnIndex, property) in columnMappings) // 遍历所有列映射，设置属性值
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
        /// 获取指定Excel工作表的列名列表
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="startIndex">开始索引，默认值0表示从第一行开始读取</param>
        /// <returns>返回列名的PooledList，序号即为列号，使用完毕后需要释放</returns>
        /// <exception cref="ArgumentNullException">当sheet为null时抛出</exception>
        /// <remarks>
        /// 简化版本的列名获取方法：
        /// - 返回PooledList&lt;string&gt;，序号即为列号
        /// - 自动查找非空的列标题行作为有效的列名行
        /// - 空列名自动生成为"Column{序号}"格式
        /// - 注意：返回的PooledList需要调用方释放资源
        /// </remarks>
        public static PooledList<string> GetColumnNames(ISheet sheet, int startIndex = 0)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            var result = new PooledList<string>();
            if (sheet.PhysicalNumberOfRows == 0) return result; // 空工作表返回空PooledList
            var currentRowIndex = startIndex;
            var maxRowIndex = sheet.LastRowNum;
            while (currentRowIndex <= maxRowIndex) // 查找有效的列标题行
            {
                var currentRow = sheet.GetRow(currentRowIndex);
                if (currentRow == null)
                {
                    currentRowIndex++;
                    continue;
                }
                result.Clear(); // 清空之前的结果
                var emptyColumnCount = 0;
                var totalColumns = currentRow.LastCellNum;
                for (int columnIndex = 0; columnIndex < totalColumns; columnIndex++) // 提取当前行的所有列名
                {
                    var cell = currentRow.GetCell(columnIndex);
                    var cellValue = GetCellStringValue(cell);
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        emptyColumnCount++;
                        result.Add($"Column{columnIndex}"); // 空列名使用默认名称
                    }
                    else
                    {
                        result.Add(cellValue.Trim());
                    }
                }
                if (emptyColumnCount == 0 || (totalColumns > 0 && emptyColumnCount < totalColumns)) // 如果当前行没有空列（或空列数量可接受），则认为找到了有效的列标题行
                {
                    return result; // 返回找到的列名
                }
                currentRowIndex++;
            }
            return result; // 如果没有找到有效的列标题行，返回空PooledList
        }

        #endregion

        #region Excel数据写入方法

        /// <summary>
        /// 将数据集合写入Excel工作表
        /// </summary>
        /// <typeparam name="T">数据集合的元素类型</typeparam>
        /// <param name="collection">可以是空集合，此时仅写入表头</param>
        /// <param name="columnNames">要写入的列名数组，对应实体的属性名</param>
        /// <param name="sheet">目标Excel工作表</param>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <exception cref="ArgumentException">当columnNames为空数组时抛出</exception>
        public static void WriteToExcel<T>(IEnumerable<T> collection, string[] columnNames, ISheet sheet)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(columnNames);
            ArgumentNullException.ThrowIfNull(sheet);
            if (columnNames.Length == 0) throw new ArgumentException("列名数组不能为空", nameof(columnNames));
            if (!collection.TryGetNonEnumeratedCount(out var rowCount))
                rowCount = collection.Count(); // 行数
            int columnCount = columnNames.Length; // 列数
            var typeProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance) // 创建属性映射，验证属性是否存在
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
            var headerRow = sheet.CreateRow(0); // 设置列头（第一行）
            for (int i = 0; i < columnCount; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(mapping[i].columnName);
            }
            int rowIndex = 1; // 写入数据行（从第二行开始）
            foreach (var item in collection)
            {
                var dataRow = sheet.CreateRow(rowIndex);
                for (int j = 0; j < columnCount; j++)
                {
                    var cell = dataRow.CreateCell(j);
                    var propertyValue = mapping[j].property.GetValue(item);
                    SetCellValue(cell, propertyValue); // 使用辅助方法设置单元格值，支持不同数据类型
                }
                rowIndex++;
            }
        }

        #endregion

        #region JSON转换方法

        /// <summary>
        /// 将Excel工作表转换为Json并写入流
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="startIndex">开始的<paramref name="sheet"/>表的行号，从0开始，此行要包含表头。</param>
        /// <param name="stream">将Json字符串写入流，从流的当前位置写入。返回时不会关闭流。</param>
        /// <exception cref="ArgumentNullException">当sheet或stream为null时抛出</exception>
        /// <exception cref="ArgumentException">当stream不可写时抛出</exception>
        /// <exception cref="InvalidOperationException">当单元格内容错误时抛出</exception>
        /// <remarks>
        /// 使用WrapperStream的高性能JSON写入：
        /// - 直接将Excel数据转换为JSON并写入流
        /// - 使用零装箱技术，性能优异
        /// - 复用GetColumnNames方法避免重复逻辑
        /// </remarks>
        public static void WriteJsonToStream(ISheet sheet, int startIndex, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanWrite) throw new ArgumentException("流必须可写", nameof(stream));
            if (startIndex < 0) throw new ArgumentException("起始索引不能为负数", nameof(startIndex));
            if (sheet.PhysicalNumberOfRows <= startIndex || sheet.GetRow(startIndex) == null)
            {
                var emptyJson = Encoding.UTF8.GetBytes("[]");
                stream.Write(emptyJson);
                return;
            }
            var headerRow = sheet.GetRow(startIndex);
            using var columnNames = GetColumnNames(sheet, startIndex); // 复用GetColumnNames方法，注意释放资源
            using var wrapper = new WrapperStream(stream, leaveOpen: true); // 包装流，保护原始流
            using var writer = new Utf8JsonWriter(wrapper, new JsonWriterOptions { Indented = false });
            writer.WriteStartArray();
            for (int rowIndex = startIndex + 1; rowIndex < sheet.PhysicalNumberOfRows; rowIndex++)
            {
                var dataRow = sheet.GetRow(rowIndex);
                if (dataRow == null) continue;
                writer.WriteStartObject();
                for (int colIndex = 0; colIndex < headerRow.LastCellNum; colIndex++)
                {
                    var columnName = colIndex < columnNames.Count ? columnNames[colIndex] : $"Column{colIndex}"; // 安全获取列名
                    WriteCellToJson(writer, columnName, dataRow.GetCell(colIndex), rowIndex, colIndex); // 使用本地函数，传入位置信息
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.Flush(); // 确保数据写入流

            #region 本地辅助函数

            void WriteCellToJson(Utf8JsonWriter writer, string propertyName, ICell cell, int rowIndex, int colIndex)
            {
                if (cell == null || cell.CellType == CellType.Blank) return; // 空单元格不写入JSON属性
                var cellType = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;
                switch (cellType)
                {
                    case CellType.String:
                        var stringValue = cell.StringCellValue;
                        if (!string.IsNullOrWhiteSpace(stringValue)) // 只有非空字符串才写入
                        {
                            writer.WritePropertyName(propertyName);
                            writer.WriteStringValue(stringValue);
                        }
                        break;
                    case CellType.Numeric:
                        writer.WritePropertyName(propertyName);
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            var dateValue = cell.DateCellValue;
                            writer.WriteStringValue(dateValue.ToString("yyyy-MM-ddTHH:mm:ss.fffK")); // 使用K格式符，可排序的ISO 8601格式
                        }
                        else
                        {
                            writer.WriteStringValue(cell.ToString());
                        }
                        break;
                    case CellType.Boolean:
                        writer.WritePropertyName(propertyName);
                        writer.WriteBooleanValue(cell.BooleanCellValue);
                        break;
                    case CellType.Formula:
                        stringValue = cell.ToString();
                        if (!string.IsNullOrWhiteSpace(stringValue)) // 只有非空字符串才写入
                        {
                            writer.WritePropertyName(propertyName);
                            writer.WriteStringValue(stringValue);
                        }
                        break;
                    case CellType.Blank:
                        return; // 空白单元格不写入JSON属性
                    case CellType.Error:
                        throw new InvalidOperationException($"遇到错误单元格：工作表'{sheet.SheetName}'第{rowIndex + 1}行第{colIndex + 1}列，错误代码：{cell.ErrorCellValue}"); // 错误单元格抛出异常
                    case CellType.Unknown:
                        throw new InvalidOperationException($"遇到未知类型单元格：工作表'{sheet.SheetName}'第{rowIndex + 1}行第{colIndex + 1}列，可能文件已损坏"); // 未知类型抛出异常
                    default:
                        throw new InvalidOperationException($"遇到不支持的单元格类型：工作表'{sheet.SheetName}'第{rowIndex + 1}行第{colIndex + 1}列，类型：{cellType}"); // 其他未处理类型抛出异常
                }
            }

            #endregion
        }

        #endregion

        #region 私有辅助方法

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
        /// <returns>字符串值，空值返回null而不是空字符串</returns>
        /// <exception cref="InvalidOperationException">当遇到错误或未知类型单元格时抛出</exception>
        private static string GetCellStringValue(ICell cell)
        {
            return cell?.CellType switch
            {
                CellType.String => string.IsNullOrWhiteSpace(cell.StringCellValue) ? null : cell.StringCellValue,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue.ToString("yyyy-MM-ddTHH:mm:ss.fffK") // 使用可排序的ISO 8601格式
                    : cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => GetFormulaCellStringValue(cell), // 使用专门的公式处理方法
                CellType.Blank => null, // 空白返回null
                CellType.Error => throw new InvalidOperationException($"遇到错误单元格：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，错误代码：{cell.ErrorCellValue}"), // 错误单元格抛出异常
                CellType.Unknown => throw new InvalidOperationException($"遇到未知类型单元格：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，可能文件已损坏"), // 未知类型抛出异常
                null => null,
                _ => throw new InvalidOperationException($"遇到不支持的单元格类型：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，类型：{cell.CellType}") // 其他未处理类型抛出异常
            };
        }

        /// <summary>
        /// 获取公式单元格的字符串值
        /// </summary>
        /// <param name="cell">公式单元格</param>
        /// <returns>字符串值</returns>
        /// <exception cref="InvalidOperationException">当遇到错误或未知类型单元格时抛出</exception>
        private static string GetFormulaCellStringValue(ICell cell)
        {
            try
            {
                return cell.CachedFormulaResultType switch
                {
                    CellType.String => string.IsNullOrWhiteSpace(cell.StringCellValue) ? null : cell.StringCellValue,
                    CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                        ? cell.DateCellValue.ToString("yyyy-MM-ddTHH:mm:ss.fffK") // 使用可排序格式
                        : cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    CellType.Blank => null,
                    CellType.Error => throw new InvalidOperationException($"公式单元格计算错误：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，错误代码：{cell.ErrorCellValue}"), // 公式计算错误
                    CellType.Unknown => throw new InvalidOperationException($"公式单元格结果为未知类型：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，可能文件已损坏"), // 未知类型抛出异常
                    _ => throw new InvalidOperationException($"公式单元格结果类型不支持：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，类型：{cell.CachedFormulaResultType}") // 其他未处理类型
                };
            }
            catch (Exception ex) when (!(ex is InvalidOperationException)) // 只捕获非我们自己抛出的异常
            {
                throw new InvalidOperationException($"公式单元格计算异常：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，错误：{ex.Message}", ex); // 包装其他异常
            }
        }

        /// <summary>
        /// 将单元格值转换为指定类型
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值</returns>
        /// <exception cref="InvalidOperationException">当遇到错误或未知类型单元格时抛出</exception>
        private static object ConvertCellValue(ICell cell, Type targetType)
        {
            if (cell == null || cell.CellType == CellType.Blank) return null;
            if (cell.CellType == CellType.Error) // 优先检查错误类型
            {
                throw new InvalidOperationException($"遇到错误单元格：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，错误代码：{cell.ErrorCellValue}");
            }
            if (cell.CellType == CellType.Unknown) // 检查未知类型
            {
                throw new InvalidOperationException($"遇到未知类型单元格：第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列，可能文件已损坏");
            }
            var isNullableType = IsNullableType(targetType); // 判断是否为可空类型
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType; // 处理可空类型
            if (cell.CellType == CellType.Numeric && IsNumericType(underlyingType)) // 优先处理 Excel 原生类型，避免不必要的字符串转换
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
                    throw new InvalidCastException($"第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列数值单元格转换失败：{ex.Message}", ex);
                }
            }
            if (cell.CellType == CellType.Boolean && underlyingType == typeof(bool)) // 处理布尔类型
            {
                return cell.BooleanCellValue;
            }
            var cellStringValue = GetCellStringValue(cell); // 对于其他情况，获取字符串值并使用通用转换逻辑，这里可能会抛出异常
            if (string.IsNullOrWhiteSpace(cellStringValue)) // 处理空字符串或空白字符串的情况
            {
                if (isNullableType || targetType == typeof(string)) return null; // 可空类型或字符串类型返回 null
                throw new InvalidCastException($"第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列不可空类型'{targetType.Name}'不能接受空值"); // 非可空值类型抛出异常
            }
            try // 使用内置转换方法进行类型转换
            {
                return Convert.ChangeType(cellStringValue, targetType);
            }
            catch (Exception ex)
            {
                var detailedError = isNullableType // 如果通用转换失败，抛出简洁异常
                    ? $"第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列无法将单元格值'{cellStringValue}'转换为可空类型'{targetType.Name}'"
                    : $"第{cell.RowIndex + 1}行第{cell.ColumnIndex + 1}列无法将单元格值'{cellStringValue}'转换为类型'{targetType.Name}'";
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
                case string strValue: cell.SetCellValue(strValue); break;
                case int intValue: cell.SetCellValue(intValue); break;
                case long longValue: cell.SetCellValue(longValue); break;
                case double doubleValue: cell.SetCellValue(doubleValue); break;
                case decimal decimalValue: cell.SetCellValue((double)decimalValue); break;
                case float floatValue: cell.SetCellValue(floatValue); break;
                case DateTime dateTimeValue: cell.SetCellValue(dateTimeValue); break;
                case bool boolValue: cell.SetCellValue(boolValue); break;
                case byte byteValue: cell.SetCellValue(byteValue); break;
                case short shortValue: cell.SetCellValue(shortValue); break;
                default: cell.SetCellValue(value.ToString()); break; // 对于其他类型，使用ToString()方法
            }
        }

        #endregion
    }
}
