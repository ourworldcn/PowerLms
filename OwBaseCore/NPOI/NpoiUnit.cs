using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NPOI
{
    /// <summary>
    /// 提供NPOI相关的扩展方法和工具类。
    /// </summary>
    static public class NpoiUnit
    {
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
            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.StringCellValue, // 公式单元格尝试获取字符串值
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
            
            // 使用项目统一的类型转换逻辑
            if (OwConvert.TryChangeType(cellStringValue, targetType, out var result))
            {
                return result;
            }
            
            // 如果通用转换失败，抛出简洁异常（基础库不提供详细错误信息）
            var detailedError = isNullableType 
                ? $"无法将单元格值'{cellStringValue}'转换为可空类型'{targetType.Name}'"
                : $"无法将单元格值'{cellStringValue}'转换为类型'{targetType.Name}'";
            
            throw new InvalidCastException(detailedError);
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
    }
}
