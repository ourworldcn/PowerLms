/*
 * 项目：PowerLms物流管理系统 | 模块：业务逻辑层
 * 功能：查询辅助类，提供动态查询条件构建功能
 * 技术要点：薄封装EfHelper.GenerateWhereAnd，为项目特定需求预留扩展点
 * 作者：zc | 创建：2025-01 | 修改：2025-01-31 创建
 */
using OW.Data;
using System.Collections.Generic;
using System.Linq;
namespace PowerLmsServer.Helpers
{
    /// <summary>
    /// 查询辅助类。提供动态查询条件构建等功能。
    /// </summary>
    public static class QueryHelper
    {
        /// <summary>
        /// 依据条件字典中的条件生成查询表达式，并使用"与"逻辑组合所有条件。
        /// 这是对EfHelper.GenerateWhereAnd的薄封装，为未来项目特定需求预留扩展点。
        /// 条件字典格式示例：
        /// 1. 单值条件："PropertyName"="Value" - 对字符串使用包含(Contains)匹配，对其他类型使用等于(==)匹配
        /// 2. 范围条件："PropertyName"="MinValue,MaxValue" - 使用大于等于和小于等于匹配
        /// 3. 单边范围："PropertyName"=",MaxValue" 或 "PropertyName"="MinValue," - 只应用一侧的限制
        /// 4. 空值条件："PropertyName"="null" - 匹配属性值为 null 的记录
        /// 日期范围特殊处理：检测到日期类型的范围条件时，上限日期自动加一天，实现包含当天的查询效果。
        /// </summary>
        /// <typeparam name="T">查询的实体类型</typeparam>
        /// <param name="queryable">原始查询对象</param>
        /// <param name="conditional">条件字典，其中键为属性名，值为过滤条件。
        /// 注意，字符串的键必须是ORM映射的属性名，且不区分大小写，如存在非ORM属性名可能出错。</param>
        /// <returns>
        /// 应用了所有条件的查询对象。如果条件处理过程中出现错误，则返回 null，并通过 OwHelper.SetLastErrorAndMessage 设置错误信息。
        /// 如果 conditional 为空或不包含任何有效的属性名，则返回原始查询对象。
        /// </returns>
        public static IQueryable<T> GenerateWhereAnd<T>(IQueryable<T> queryable, IDictionary<string, string> conditional) where T : class
        {
            if (conditional == null || !conditional.Any())
                return EfHelper.GenerateWhereAnd(queryable, conditional);
            var processedConditional = new Dictionary<string, string>(conditional);
            var type = typeof(T);
            foreach (var item in conditional.ToArray())
            {
                if (string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Value))
                    continue;
                var property = type.GetProperty(item.Key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
                if (property == null)
                    continue;
                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (propertyType == typeof(DateTime))
                {
                    var values = item.Value.Split(',');
                    if (values.Length == 2 && !string.IsNullOrEmpty(values[1]))
                    {
                        if (DateTime.TryParse(values[1], out var maxDate))
                        {
                            var adjustedMaxDate = maxDate.AddDays(1);
                            processedConditional[item.Key] = string.IsNullOrEmpty(values[0])
                                ? $",{adjustedMaxDate:yyyy-MM-dd}"
                                : $"{values[0]},{adjustedMaxDate:yyyy-MM-dd}";
                        }
                    }
                }
            }
            return EfHelper.GenerateWhereAnd(queryable, processedConditional);
        }
    }
}
