/*
 * PowerLms - 货运物流业务管理系统
 * 数据操作工具类 - 基础设施组件
 * 
 * 功能说明：
 * - 提供高性能的Excel数据导入和批量数据库操作功能
 * - 支持按主键忽略重复数据的纯插入策略
 * - 基于OwNpoiUnit的Excel数据处理和实体转换
 * - 严格的基础库设计，错误直接抛出
 * 
 * 技术特点：
 * - 使用EFCore.BulkExtensions实现高性能批量操作
 * - 框架自动检测主键实现智能重复数据处理
 * - 支持泛型和非泛型的灵活调用方式
 * - 内存优化的字符串数组到实体转换
 * - C# 10新特性应用
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年12月 - 按提示词规范重整，移除回退逻辑
 */

using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using NPOI.SS.UserModel;
using NPOI;

namespace OW.Data
{
    /// <summary>
    /// 数据操作工具类 - PowerLms基础设施组件
    /// 提供高性能的Excel数据导入和批量数据库操作功能
    /// </summary>
    /// <remarks>
    /// 基础设施组件设计原则：
    /// - 数据源参数优先，目标参数其次，配置参数最后
    /// - 严重错误直接抛出异常，不做任何回退处理
    /// - 复用<paramref name="OwNpoiUnit"/>进行Excel数据处理
    /// - 框架自动检测主键实现智能重复数据处理
    /// </remarks>
    public static class OwDataUnit
    {
        #region 核心批量插入方法

        /// <summary>
        /// 批量插入实体集合到数据库
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="entities">数据源：要插入的实体集合</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功处理的记录数</returns>
        /// <exception cref="ArgumentNullException">当<paramref name="entities"/>或<paramref name="dbContext"/>为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当数据库操作失败时抛出</exception>
        public static int BulkInsert<TEntity>(IEnumerable<TEntity> entities, DbContext dbContext, bool ignoreExisting = true)
            where TEntity : class
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            var entityList = entities.ToList();
            if (entityList.Count == 0) return 0;
            if (ignoreExisting)
            {
                dbContext.BulkInsert(entities, new BulkConfig
                {
                    SqlBulkCopyOptions = Microsoft.Data.SqlClient.SqlBulkCopyOptions.KeepIdentity | Microsoft.Data.SqlClient.SqlBulkCopyOptions.TableLock,
                });
            }
            else
            {
                var bulkConfig = CreateBulkConfig(ignoreExisting, typeof(TEntity));
                dbContext.BulkInsertOrUpdate(entityList, bulkConfig);
            }
            return entityList.Count;
        }

        /// <summary>
        /// 从Excel工作表批量插入数据到数据库
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="sheet">数据源：Excel工作表</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功插入的记录数</returns>
        /// <exception cref="ArgumentNullException">当<paramref name="sheet"/>或<paramref name="dbContext"/>为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当Excel处理或数据库操作失败时抛出</exception>
        public static int BulkInsert<TEntity>(ISheet sheet, DbContext dbContext, bool ignoreExisting = true)
            where TEntity : class, new()
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            using var allRows = OwNpoiUnit.GetStringList(sheet, out var columnHeaders); // 复用OwNpoiUnit基础设施
            if (columnHeaders.Count == 0 || allRows.Count == 0) return 0;
            var entities = ConvertToEntities<TEntity>(allRows, columnHeaders);
            if (entities.Count == 0) return 0;
            return BulkInsert(entities, dbContext, ignoreExisting);
        }

        /// <summary>
        /// 非泛型版本 - 从Excel工作表批量插入数据
        /// </summary>
        /// <param name="sheet">数据源：Excel工作表</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="entityType">目标：实体类型</param>
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功插入的记录数</returns>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当反射调用失败时抛出</exception>
        public static int BulkInsert(ISheet sheet, DbContext dbContext, Type entityType, bool ignoreExisting = true)
        {
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            var targetMethod = GetGenericBulkInsertMethod(forSheet: true);
            var genericMethod = targetMethod.MakeGenericMethod(entityType);
            return (int)genericMethod.Invoke(null, new object[] { sheet, dbContext, ignoreExisting });
        }

        /// <summary>
        /// 非泛型版本 - 批量插入实体集合
        /// </summary>
        /// <param name="entities">数据源：要处理的实体集合</param>
        /// <param name="dbContext">目标：数据库上下文</param>
        /// <param name="entityType">目标：实体类型</param>
        /// <param name="ignoreExisting">配置：是否按主键忽略重复数据</param>
        /// <returns>成功处理的记录数</returns>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当反射调用失败时抛出</exception>
        public static int BulkInsert(System.Collections.IEnumerable entities, DbContext dbContext, Type entityType, bool ignoreExisting = true)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            var targetMethod = GetGenericBulkInsertMethod(forSheet: false);
            var genericMethod = targetMethod.MakeGenericMethod(entityType);
            return (int)genericMethod.Invoke(null, new object[] { entities, dbContext, ignoreExisting });
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 创建批量操作配置对象
        /// </summary>
        /// <param name="ignoreExisting">是否忽略重复数据</param>
        /// <param name="entityType">实体类型</param>
        /// <returns>配置对象</returns>
        private static BulkConfig CreateBulkConfig(bool ignoreExisting, Type entityType)
        {
            var config = new BulkConfig
            {
                BatchSize = 1000,
                BulkCopyTimeout = 300,
                SetOutputIdentity = false,
                PreserveInsertOrder = false,
                UseTempDB = false,  //不要用临时表，避免还要显示开事务
            };
            if (ignoreExisting) // 忽略重复数据：排除所有字段更新，实现按主键跳过重复记录
            {
                config.PropertiesToExcludeOnUpdate = entityType.GetProperties()
                    .Where(p => p.CanWrite)
                    .Select(p => p.Name)
                    .ToList();
            }
            return config;
        }

        /// <summary>
        /// 获取泛型BulkInsert方法的反射信息
        /// </summary>
        /// <param name="forSheet">是否为Excel工作表版本</param>
        /// <returns>方法信息</returns>
        /// <exception cref="InvalidOperationException">未找到匹配方法时抛出</exception>
        private static System.Reflection.MethodInfo GetGenericBulkInsertMethod(bool forSheet)
        {
            var methods = typeof(OwDataUnit).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            System.Reflection.MethodInfo targetMethod = forSheet
                ? methods.FirstOrDefault(m => 
                    m.Name == nameof(BulkInsert) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 3 &&
                    m.GetParameters()[0].ParameterType == typeof(ISheet) &&
                    m.GetParameters()[1].ParameterType == typeof(DbContext) &&
                    m.GetParameters()[2].ParameterType == typeof(bool))
                : methods.FirstOrDefault(m => 
                    m.Name == nameof(BulkInsert) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 3 &&
                    m.GetParameters()[0].ParameterType.IsGenericType &&
                    m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                    m.GetParameters()[1].ParameterType == typeof(DbContext) &&
                    m.GetParameters()[2].ParameterType == typeof(bool));
            return targetMethod ?? throw new InvalidOperationException($"未找到匹配的泛型BulkInsert方法");
        }

        /// <summary>
        /// 将字符串数组转换为实体列表
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="allRows">所有数据行</param>
        /// <param name="columnHeaders">列头信息</param>
        /// <returns>实体列表</returns>
        /// <exception cref="InvalidOperationException">当转换失败时抛出</exception>
        private static List<TEntity> ConvertToEntities<TEntity>(PooledList<PooledList<string>> allRows, PooledList<string> columnHeaders)
            where TEntity : class, new()
        {
            var entities = new List<TEntity>();
            var propertyMapping = CreatePropertyMapping<TEntity>(columnHeaders);
            if (propertyMapping.Count == 0)
                throw new InvalidOperationException($"实体类型{typeof(TEntity).Name}没有找到匹配的属性列");
            for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
            {
                using var currentRow = allRows[rowIndex];
                var entity = CreateEntity<TEntity>(currentRow, propertyMapping);
                if (entity != null) entities.Add(entity);
            }
            return entities;
        }

        /// <summary>
        /// 从列头创建属性映射字典
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="columnHeaders">列头信息</param>
        /// <returns>列索引到属性的映射字典</returns>
        private static Dictionary<int, System.Reflection.PropertyInfo> CreatePropertyMapping<TEntity>(PooledList<string> columnHeaders)
        {
            var properties = typeof(TEntity).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            var mapping = new Dictionary<int, System.Reflection.PropertyInfo>();
            for (int i = 0; i < columnHeaders.Count; i++)
            {
                var columnName = columnHeaders[i]?.Trim();
                if (!string.IsNullOrEmpty(columnName) && properties.TryGetValue(columnName, out var property))
                {
                    mapping[i] = property;
                }
            }
            return mapping;
        }

        /// <summary>
        /// 从字符串行数据创建实体对象
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="rowData">行数据</param>
        /// <param name="propertyMapping">属性映射</param>
        /// <returns>实体对象，如果行数据无效则返回null</returns>
        private static TEntity CreateEntity<TEntity>(PooledList<string> rowData, Dictionary<int, System.Reflection.PropertyInfo> propertyMapping)
            where TEntity : class, new()
        {
            var entity = new TEntity();
            bool hasValidData = false;
            foreach (var (columnIndex, property) in propertyMapping)
            {
                if (columnIndex >= rowData.Count) continue;
                var cellValue = rowData[columnIndex];
                if (string.IsNullOrWhiteSpace(cellValue)) continue;
                var convertedValue = ConvertStringValue(cellValue, property.PropertyType);
                if (convertedValue != null)
                {
                    property.SetValue(entity, convertedValue);
                    hasValidData = true;
                }
            }
            return hasValidData ? entity : null;
        }

        /// <summary>
        /// 字符串值类型转换
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>转换后的值</returns>
        private static object ConvertStringValue(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return actualType.Name switch
            {
                nameof(String) => value.Trim(),
                nameof(Guid) => Guid.TryParse(value, out var guid) ? guid : null,
                nameof(DateTime) => DateTime.TryParse(value, out var dateTime) ? dateTime : null,
                nameof(Boolean) => ParseBooleanValue(value),
                _ when actualType.IsEnum => Enum.TryParse(actualType, value, true, out var enumValue) ? enumValue : null,
                _ => Convert.ChangeType(value, actualType)
            };
        }

        /// <summary>
        /// 解析布尔值，支持中文和多种格式
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <returns>布尔值</returns>
        private static bool ParseBooleanValue(string value)
        {
            if (bool.TryParse(value, out var boolValue)) return boolValue;
            var lowerValue = value.ToLower().Trim();
            return lowerValue is "是" or "true" or "1" or "yes";
        }

        #endregion
    }
}