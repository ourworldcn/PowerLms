using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Linq.Expressions;
using System.Reflection;

namespace OW.Data
{
    /// <summary>
    /// 实体框架帮助类。
    /// </summary>
    public static class EfHelper
    {
        #region 动态编译及相关
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
        /// 获取介于范围之间的表达式。如果min或max为null，则只应用另一个限制。
        /// </summary>
        /// <param name="property">要比较的属性表达式</param>
        /// <param name="min">最小值表达式，可以为null表示不限制下限</param>
        /// <param name="max">最大值表达式，可以为null表示不限制上限</param>
        /// <returns>组合的表达式，如果min和max都为null则返回一个始终为true的表达式</returns>
        public static Expression Between(Expression property, Expression min, Expression max)
        {
            // 如果两个限制都没有，返回始终为true的表达式
            if (min == null && max == null)
            {
                return Expression.Constant(true);
            }

            // 如果只有最小值限制
            if (max == null)
            {
                return Expression.GreaterThanOrEqual(property, min);
            }

            // 如果只有最大值限制
            if (min == null)
            {
                return Expression.LessThanOrEqual(property, max);
            }

            // 同时有最小值和最大值限制
            var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, min);
            var lessThanOrEqual = Expression.LessThanOrEqual(property, max);
            return Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
        }

        /// <summary>
        /// 获取多个条件或关系的表达式。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <param name="conditional"></param>
        /// <returns></returns>
        public static IQueryable<T> GenerateWhereOr<T>(IQueryable<T> queryable, IDictionary<string, string> conditional) where T : class
        {
            if (conditional == null || !conditional.Any())
                return queryable;

            // 添加 null 检查以确保 queryable 不为 null
            if (queryable == null)
                return null;

            var type = typeof(T);
            var para = Expression.Parameter(type);
            Expression body = null;

            foreach (var item in conditional)
            {
                // 检查条件项是否为 null 或空
                if (item.Key == null || item.Value == null)
                    continue;

                if (type.GetProperty(item.Key, BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) is null) continue;
                var left = Expression.Property(para, item.Key);
                var values = item.Value.Split(',');
                Expression condition;

                if (values.Length == 1)
                {
                    var right = Constant(values[0], left.Type);
                    if (typeof(string) == left.Type) //对字符串则使用模糊查找
                    {
                        condition = StringContains(left, Constant(values[0], left.Type));
                    }
                    else
                    {
                        condition = Expression.Equal(left, Constant(values[0], left.Type));
                    }
                }
                else if (values.Length == 2)
                {
                    condition = Between(left, Constant(values[0], left.Type), Constant(values[1], left.Type));
                }
                else
                {
                    OwHelper.SetLastErrorAndMessage(404, $"不正确的参数格式——{item.Value}。");
                    return null;
                }

                body = body == null ? condition : Expression.OrElse(body, condition);
            }

            // 如果没有有效的条件，返回原始查询
            if (body == null)
                return queryable;

            var func = Expression.Lambda<Func<T, bool>>(body, para);
            return queryable.Where(func);
        }

        /// <summary>
        /// 依据条件字典中的条件生成查询表达式，并使用"与"逻辑组合所有条件。
        /// </summary>
        /// <typeparam name="T">查询的实体类型</typeparam>
        /// <param name="queryable">原始查询对象</param>
        /// <param name="conditional">条件字典，其中键为属性名，值为过滤条件。
        /// 注意，字符串的键必须是ORM映射的属性名，且不区分大小写，如存在非ORM属性名可能出错。</param>
        /// 格式示例：
        /// 1. 单值条件："PropertyName"="Value" - 对字符串使用包含(Contains)匹配，对其他类型使用等于(==)匹配
        /// 2. 范围条件："PropertyName"="MinValue,MaxValue" - 使用大于等于和小于等于匹配
        /// 3. 单边范围："PropertyName"=",MaxValue" 或 "PropertyName"="MinValue," - 只应用一侧的限制
        /// 4. 空值条件："PropertyName"="null" - 匹配属性值为 null 的记录
        /// </param>
        /// <returns>
        /// 应用了所有条件的查询对象。如果条件处理过程中出现错误，则返回 null，并通过 OwHelper.SetLastErrorAndMessage 设置错误信息。
        /// 如果 conditional 为空或不包含任何有效的属性名，则返回原始查询对象。
        /// </returns>
        /// <remarks>
        /// 方法特性：
        /// 1. 属性名匹配不区分大小写
        /// 2. 支持自动类型转换，将字符串条件值转换为属性类型
        /// 3. 支持处理可空类型和枚举类型
        /// 4. 字符串属性使用 Contains 进行模糊查询
        /// 5. 对于范围查询，如果某一边界为空字符串，则表示无该边界限制
        /// 
        /// 注意事项：
        /// - 如果条件字典中的属性名在实体类中不存在，该条件会被忽略
        /// - 如果无法将条件值转换为属性类型，将返回 null 并设置错误信息
        /// - 对于枚举类型，可以使用名称或对应的整数值
        /// - 生成的表达式会作为 Where 子句应用到原始查询中
        /// </remarks>
        /// <example>
        /// 使用示例：
        /// <code>
        /// var query = dbContext.Users.AsQueryable();
        /// var conditions = new Dictionary<string, string>
        /// {
        ///     { "Age", "18,30" },        // 年龄在18到30之间
        ///     { "Name", "John" },        // 名字包含"John"
        ///     { "Status", "Active" },    // 状态为"Active"枚举值
        ///     { "CreatedDate", "2023-01-01," }  // 创建日期大于等于2023-01-01
        /// };
        /// var result = EfHelper.GenerateWhereAnd(query, conditions);
        /// </code>
        /// </example>
        public static IQueryable<T> GenerateWhereAnd<T>(IQueryable<T> queryable, IDictionary<string, string> conditional) where T : class
        {
            // 如果条件集合为空或null，直接返回原始查询
            if (conditional == null || !conditional.Any())
                return queryable;

            // 添加 null 检查以确保 queryable 不为 null
            if (queryable == null)
                return null;

            IQueryable<T> result = queryable;
            var type = typeof(T);
            var para = Expression.Parameter(type); // 创建参数表达式，表示查询对象中的实体

            foreach (var item in conditional)
            {
                // 检查条件项是否为 null 或空
                if (item.Key == null || item.Value == null)
                    continue;

                // 忽略实体类中不存在的属性
                if (type.GetProperty(item.Key, BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) is null)
                    continue;

                // 获取属性表达式
                var left = Expression.Property(para, item.Key);
                // 分割属性值，处理可能的范围条件
                var values = item.Value.Split(',');
                Expression body;

                if (values.Length == 1)
                {
                    // 单值查询处理
                    Expression right;

                    // 检查属性是否为枚举类型，枚举类型需要特殊处理
                    if (IsEnumType(left.Type))
                    {
                        right = ParseEnumConstant(values[0], left.Type);
                    }
                    else
                    {
                        // 非枚举类型通过Constant方法转换
                        right = Constant(values[0], left.Type);
                    }

                    // 转换失败时返回null
                    if (right == null)
                        return null;

                    // 字符串类型使用Contains方法进行模糊查询
                    if (typeof(string) == left.Type)
                    {
                        body = StringContains(left, right);
                    }
                    else
                        // 其他类型使用等于比较
                        body = Expression.Equal(left, right);
                }
                else if (values.Length == 2)
                {
                    // 处理范围查询

                    // 处理最小值，可能为空字符串表示无下限
                    Expression minExpr = null;
                    if (!string.IsNullOrEmpty(values[0]))
                    {
                        // 枚举类型特殊处理
                        if (IsEnumType(left.Type))
                        {
                            minExpr = ParseEnumConstant(values[0], left.Type);
                        }
                        else
                        {
                            minExpr = Constant(values[0], left.Type);
                        }

                        // 转换失败时返回null
                        if (minExpr == null)
                            return null;
                    }

                    // 处理最大值，可能为空字符串表示无上限
                    Expression maxExpr = null;
                    if (!string.IsNullOrEmpty(values[1]))
                    {
                        // 枚举类型特殊处理
                        if (IsEnumType(left.Type))
                        {
                            maxExpr = ParseEnumConstant(values[1], left.Type);
                        }
                        else
                        {
                            maxExpr = Constant(values[1], left.Type);
                        }

                        // 转换失败时返回null
                        if (maxExpr == null)
                            return null;
                    }

                    // 使用Between方法创建范围表达式
                    body = Between(left, minExpr, maxExpr);
                }
                else
                {
                    // 如果值分段数不是1或2，则格式错误
                    OwHelper.SetLastErrorAndMessage(404, $"不正确的参数格式——{item.Value}。");
                    return null;
                }

                // 将表达式转换为Lambda表达式，并应用到查询中
                var func = Expression.Lambda<Func<T, bool>>(body, para);
                result = result.Where(func);
            }
            return result;
        }

        /// <summary>
        /// 检查类型是否为枚举或可空枚举类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>如果是枚举或可空枚举则返回true，否则返回false</returns>
        private static bool IsEnumType(Type type)
        {
            // 检查是否为直接枚举类型
            if (type.IsEnum)
                return true;

            // 检查是否为可空枚举类型
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return type.GetGenericArguments()[0].IsEnum;
            }

            return false;
        }

        /// <summary>
        /// 解析字符串为枚举常量表达式
        /// </summary>
        /// <param name="value">要解析的字符串值，可以是数字或枚举名称</param>
        /// <param name="enumType">目标枚举类型</param>
        /// <returns>表示枚举值的常量表达式，解析失败则返回null</returns>
        private static Expression ParseEnumConstant(string value, Type enumType)
        {
            // 处理可空类型
            Type underlyingType = enumType;
            bool isNullable = false;

            if (enumType.IsGenericType && enumType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                underlyingType = enumType.GetGenericArguments()[0];
                isNullable = true;
            }

            // 处理null值
            if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            {
                if (isNullable)
                    return Expression.Constant(null, enumType);
                else
                {
                    OwHelper.SetLastErrorAndMessage(404, $"非可空枚举类型{underlyingType}不能设置为null");
                    return null;
                }
            }

            try
            {
                // 先尝试按名称解析
                if (Enum.TryParse(underlyingType, value, true, out object enumValue))
                {
                    return Expression.Constant(enumValue, isNullable ? enumType : underlyingType);
                }

                // 如果按名称解析失败，尝试按数值解析
                if (int.TryParse(value, out int intValue))
                {
                    if (Enum.IsDefined(underlyingType, intValue))
                    {
                        enumValue = Enum.ToObject(underlyingType, intValue);
                        return Expression.Constant(enumValue, isNullable ? enumType : underlyingType);
                    }
                }

                // 解析失败
                OwHelper.SetLastErrorAndMessage(404, $"无法将值'{value}'解析为枚举类型{underlyingType}");
                return null;
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(404, $"解析枚举值出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 分析字符串获得一个指定类型的常量表达式。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Expression Constant(string value, Type type)
        {
            // null 值处理
            if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            {
                return Expression.Constant(null, type);
            }

            // 使用统一的类型转换逻辑
            if (OwConvert.TryChangeType(value, type, out var convertedValue))
            {
                return Expression.Constant(convertedValue, type);
            }

            // 转换失败，设置简洁错误信息（基础库不提供详细错误诊断）
            var detailedError = $"动态查询条件转换失败：无法将值 '{value}' 转换为类型 '{type.Name}'";

            OwHelper.SetLastErrorAndMessage(404, detailedError);
            return null;
        }

        #endregion 动态编译及相关

        /// <summary>
        /// 设置一个子表的完全集合，不在参数内的都删除，对已有Id的实体更新，对新Id执行添加操作。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="context">使用的数据库上下位，不会自动保存，调用者需要自己保存数据。</param>
        public static bool SetChildren<T>(IEnumerable<T> src, Guid parentId, DbContext context, ICollection<T> result = null) where T : GuidKeyObjectBase, IOwSubtables
        {
            var ids = new HashSet<Guid>(src.Select(c => c.Id)); //提取所有源实体的Id到HashSet，自动去重
            if (!src.TryGetNonEnumeratedCount(out var idCount)) idCount = src.Count(); //尝试获取集合元素数量，若无法直接获取则执行完整枚举
            if (idCount != ids.Count) //检查是否存在重复Id
            {
                OwHelper.SetLastErrorAndMessage(400, $"{nameof(src)}中有重复键值。");
                return false;
            }
            if (src.Any(c => c.ParentId != parentId)) //验证所有源实体的ParentId是否匹配
            {
                OwHelper.SetLastErrorAndMessage(400, $"{nameof(src)}中父实体对象Id非法——应为{parentId}。");
                return false;
            }
            var exists = context.Set<T>().Where(c => c.ParentId == parentId).ToArray(); //从数据库加载该父实体下的所有现存子实体
            var removes = exists.Where(c => !ids.Contains(c.Id)); //筛选出需要删除的实体（数据库有但源集合没有）
            context.RemoveRange(removes); //标记删除
            var existsIds = exists.Select(c => c.Id); //提取已存在实体的Id集合
            var adds = src.Where(c => !existsIds.Contains(c.Id)); //筛选出需要新增的实体（源集合有但数据库没有）
            context.AddRange(adds); //标记新增
            if (result is not null) //如果提供了结果集合
                adds.ForEach(c => result.Add(c)); //将新增的实体添加到结果集合
            var modifies = src.Where(c => existsIds.Contains(c.Id)); //筛选出需要更新的实体（源集合和数据库都有）
            var set = context.Set<T>(); //获取实体集
            foreach (var item in modifies)
            {
                var entity = set.Find(item.Id); //从上下文中查找现存实体（注意：这里会重复查询，因exists已包含该实体）
                context.Entry(entity).CurrentValues.SetValues(item); //用源实体的值更新数据库实体的所有属性
                result?.Add(item); //将更新的源实体添加到结果集合（注意：添加的是item而非entity）
            }
            return true; //操作成功
        }

    }
}
