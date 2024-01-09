using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        /// <param name="fieldName">字段名。</param>
        /// <param name="isDesc">是否降序排序：true降序排序，false升序排序（省略或默认）。</param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> query, string fieldName, bool isDesc = false)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentNullException(nameof(fieldName));
            var names = fieldName.Split('.');

            ParameterExpression p = Expression.Parameter(typeof(T));
            Expression key = Expression.Property(p, fieldName);
            var propInfo = GetPropertyInfo(typeof(T), fieldName, true);
            var expr = GetOrderExpression(typeof(T), propInfo);
            if (isDesc)
            {
                var method = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
                var genericMethod = method.MakeGenericMethod(typeof(T), propInfo.PropertyType);
                return (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { query, expr });
            }
            else
            {
                var method = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
                var genericMethod = method.MakeGenericMethod(typeof(T), propInfo.PropertyType);
                return (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { query, expr });
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
            var tmp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var matchedProperty = properties.FirstOrDefault(p => p.Name.Equals(name, tmp));
            if (matchedProperty == null)
                throw new ArgumentException("对象不包含指定属性名");

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
}
