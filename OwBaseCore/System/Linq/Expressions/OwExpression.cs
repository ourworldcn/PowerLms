using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq.Expressions
{
    public class OwExpression
    {
        /// <summary>
        /// 获取属性/字段值的表达式。 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="propertyName">支持 name.displayname语法。只查找公有的实例成员（包含继承成员）。</param>
        /// <param name="ignoreCase">是否忽略大小写。true忽略，false(默认值)不忽略。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">找不到属性。</exception>
        public static Expression PropertyOrField(Expression expression, string propertyName, bool ignoreCase = false)
        {
            var ary = propertyName.Split('.');
            Expression exprTmp = expression;
            var binding = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.GetField;
            if (ignoreCase) binding |= BindingFlags.IgnoreCase;
            foreach (var item in ary)
            {
                var pis = exprTmp.Type.GetMember(item, binding);
                if (pis.Length == 0) throw new ArgumentException($"找不到属性/字段{item}", nameof(propertyName));
                exprTmp = Expression.PropertyOrField(exprTmp, pis[0].Name);
            }
            return exprTmp;
        }
    }
}
