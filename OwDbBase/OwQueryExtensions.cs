using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace OW.Data
{
    public static class OwQueryExtensions
    {
        /// <summary>
        /// 按升序对序列的元素进行排序。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="fieldName">字段名。支持xxx.xxx语法，如:name.displayname</param>
        /// <param name="isDesc">是否降序排序：true降序排序，false升序排序（省略或默认）。</param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> query, string fieldName, bool isDesc = false)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentNullException(nameof(fieldName));
            var names = fieldName.Split('.');

            ParameterExpression p = Expression.Parameter(typeof(T));
            //Expression key = Expression.Property(p, fieldName);
            var exprBody = OwExpression.PropertyOrField(p, fieldName, true);
            //var propInfo = GetPropertyInfo(typeof(T), fieldName, true);
            //var expr = GetOrderExpression(typeof(T), propInfo);
            if (isDesc)
            {
                var method = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
                var genericMethod = method.MakeGenericMethod(typeof(T), exprBody.Type);
                return (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { query, Expression.Lambda(exprBody, p) });
            }
            else
            {
                var method = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
                var genericMethod = method.MakeGenericMethod(typeof(T), exprBody.Type);
                return (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { query, Expression.Lambda(exprBody, p) });
            }
        }

        /// <summary>
        /// 获取属性反射对象。
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="name"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyInfo(Type objType, string name, bool ignoreCase = false)
        {
            var properties = objType.GetProperties();
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var matchedProperty = properties.FirstOrDefault(p => p.Name.Equals(name, comparison)) ?? throw new ArgumentException("对象不包含指定属性名");
            return matchedProperty;
        }

        /// <summary>
        /// 获取生成表达式
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="pi"></param>
        /// <returns></returns>
        public static LambdaExpression GetOrderExpression(Type objType, PropertyInfo pi)
        {
            var paramExpr = Expression.Parameter(objType);
            var propAccess = Expression.PropertyOrField(paramExpr, pi.Name);
            var expr = Expression.Lambda(propAccess, paramExpr);
            return expr;
        }
    }

    public class EfHelper
    {
        public static Expression PropertyOrField(Expression property, string value, bool ignoreCase = false)
        {
            return Expression.Property(property, value);
        }

        /// <summary>
        /// 获取字符串包含的表达式。
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Expression StringContains(Expression property, Expression value)
        {
            var method = Expression.Call(property, typeof(string).GetMethod("Contains", new Type[] { typeof(string) }), value);
            return method;
        }

        /// <summary>
        /// 获取介于2者之间的表达式。
        /// </summary>
        /// <param name="property"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static Expression Between(Expression property, Expression min, Expression max)
        {
            var l = Expression.GreaterThanOrEqual(property, min);
            var r = Expression.LessThanOrEqual(property, max);
            return Expression.AndAlso(l, r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <param name="conditional"></param>
        /// <returns>返回的可查询接口，null表示出错。</returns>
        public static IQueryable<T> GenerateWhereAnd<T>(IQueryable<T> queryable, IDictionary<string, string> conditional) where T : class
        {
            IQueryable<T> result = queryable;
            var type = typeof(T);
            var para = Expression.Parameter(type);
            foreach (var item in conditional)
            {
                if (type.GetProperty(item.Key, BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) is null) continue;
                var left = Expression.Property(para, item.Key);
                var values = item.Value.Split(',');
                Expression body;
                if (values.Length == 1)
                {
                    var right = Constant(values[0], left.Type);
                    if (typeof(string) == left.Type) //对字符串则使用模糊查找
                    {
                        body = StringContains(left, Constant(values[0], left.Type));

                    }
                    else
                        body = Expression.Equal(left, Constant(values[0], left.Type));
                }
                else if (values.Length == 2)
                {
                    body = Between(left, Constant(values[0], left.Type), Constant(values[1], left.Type));
                }
                else
                {
                    OwHelper.SetLastErrorAndMessage(404, $"不正确的参数格式——{item.Value}。");
                    return null;
                }
                var func = Expression.Lambda<Func<T, bool>>(body, para);
                result = result.Where(func);
            }
            return result;
        }

        public static Expression Constant(string value, Type type)
        {
            Expression result;
            var innerType = type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))  //可空类型
                innerType = type.GetGenericArguments()[0];
            switch (Type.GetTypeCode(innerType))
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                    result = Expression.Constant(null, type);
                    break;
                case TypeCode.Object:
                    if (innerType == typeof(Guid))
                    {
                        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                            result = Expression.Constant(null, type);
                        else if (!Guid.TryParse(value, out Guid guidValue))
                        {
                            OwHelper.SetLastErrorAndMessage(404, $"不能转换为类型{type}, {value}");
                            return null;
                        }
                        else
                            result = Expression.Constant(guidValue, type);
                    }
                    else
                    {
                        OwHelper.SetLastErrorAndMessage(404, $"不能转换为类型{type}, {value}");
                        return null;
                    }
                    break;
                case TypeCode.Boolean:
                    if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                        result = Expression.Constant(null, type);
                    else if (!bool.TryParse(value, out bool boolValue))
                    {
                        OwHelper.SetLastErrorAndMessage(404, $"不能转换为类型{type}, {value}");
                        return null;
                    }
                    else
                        result = Expression.Constant(boolValue, type);
                    break;
                case TypeCode.Char:
                    if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                        result = Expression.Constant(null, type);
                    else if (!char.TryParse(value, out char charValue))
                    {
                        OwHelper.SetLastErrorAndMessage(404, $"不能转换为类型{type}, {value}");
                        return null;
                    }
                    else result = Expression.Constant(charValue, type);
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                        result = Expression.Constant(null, type);
                    else if (!decimal.TryParse(value, out decimal decimalValue))
                    {
                        OwHelper.SetLastErrorAndMessage(404, $"不能转换为类型{type}, {value}");
                        return null;
                    }
                    else result = Expression.Convert(Expression.Constant(decimalValue), type);
                    break;
                case TypeCode.DateTime:
                    if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                        result = Expression.Constant(null, type);
                    else if (!DateTime.TryParse(value, out DateTime dateTimeValue))
                    {
                        OwHelper.SetLastErrorAndMessage(404, $"不能转换为类型{type}, {value}");
                        return null;
                    }
                    else result = Expression.Constant(dateTimeValue, type);
                    break;
                case TypeCode.String:
                    if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                        result = Expression.Constant(null, type);
                    else result = Expression.Constant(value, type);
                    break;
                default:
                    OwHelper.SetLastErrorAndMessage(404, $"不能转换为类型{type}, {value}");
                    return null;
            }
            return result;
        }
    }
}
