using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.VisualBasic;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 实体常用操作的封装类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class EntityManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="mapper"></param>
        public EntityManager(PowerLmsUserDbContext dbContext, IMapper mapper)
        {
            _DbContext = dbContext;
            _Mapper = mapper;
        }

        readonly PowerLmsUserDbContext _DbContext;
        readonly IMapper _Mapper;

        /// <summary>
        /// 修改可软删除的对象集合。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newValues"></param>
        /// <returns>true成功修改，调用着需要最终保存数据，false出现错误，调用应放弃保存。</returns>
        public bool ModifyWithMarkDelete<T>(IEnumerable<T> newValues) where T : class, IEntityWithSingleKey<Guid>, IMarkDelete
        {
            var entities = new List<T>();
            if (!Modify(newValues, entities)) return false;
            entities.ForEach(c => _DbContext.Entry(c).Property(c => c.IsDelete).IsModified = false);
            return true;
        }

        /// <summary>
        /// 修改的对象集合。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newValues"></param>
        /// <param name="result">在返回true时，这里记录更改的实体类的集合，省略或为null则不记录。返回false时，这里有随机内容。</param>
        /// <returns>true成功修改，调用着需要最终保存数据，false出现错误，调用应放弃保存。</returns>
        public bool Modify<T>(IEnumerable<T> newValues, ICollection<T> result = null) where T : class, IEntityWithSingleKey<Guid>
        {
            var dbSet = _DbContext.Set<T>();
            var ids = newValues.Select(c => c.Id).ToArray();
            var accColl = dbSet.Where(c => ids.Contains(c.Id)).ToArray(); //单次一起加载
            if (ids.Length > accColl.Length)
            {
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, $"至少有一个Id不存在。");
                return false;
            }
            foreach (var item in newValues)
            {
                var tmp = dbSet.Find(item.Id);
                Debug.Assert(tmp is not null);
                var entity = _DbContext.Entry(tmp);
                entity.CurrentValues.SetValues(item);
                try
                {
                    _Mapper.Map(item, tmp, typeof(T), typeof(T));
                }
                catch (AutoMapperMappingException)  //忽略不能映射的情况
                {
                }
                result?.Add(tmp);
                if (tmp is ICreatorInfo ci) //若实现创建信息接口
                {
                    entity.Property(nameof(ci.CreateBy)).IsModified = false;
                    entity.Property(nameof(ci.CreateDateTime)).IsModified = false;
                }
                try
                {
                    var orgidProp = entity?.Property("OrgId");
                    if (orgidProp != null) orgidProp.IsModified = false;
                }
                catch (Exception)
                {
                }
            }
            return true;
        }

        /// <summary>
        /// 恢复已经被软删除的实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Restore<T>(Guid id) where T : GuidKeyObjectBase, IMarkDelete
        {
            var dbSet = _DbContext.Set<T>();
            var entity = dbSet.Find(id);
            if (entity is null)
            {
                OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, $"找不到指定Id——{id}");
                return false;
            }
            if (!entity.IsDelete)
                OwHelper.SetLastErrorAndMessage(0, "指定Id并没有被标记为删除");
            entity.IsDelete = false;
            return true;
        }

        /// <summary>
        /// 获取一个集合的分页结果。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="startIndex"></param>
        /// <param name="count">-1则获取所有。</param>
        /// <returns></returns>
        public PagingReturnBase<T> GetAll<T>(IQueryable<T> values, int startIndex, int count = -1)
        {
            var result = new PagingReturnBase<T>();
            var coll = values.Skip(startIndex);
            if (count > -1) coll = coll.Take(count);
            result.Result.AddRange(coll);
            result.Total = count == -1 ? result.Result.Count : values.Count();
            return result;
        }

        /// <summary>
        /// 移除一个实体。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public EntityEntry<T> Remove<T>(T item) where T : class
        {
            if (item is IMarkDelete markDelete)
            {
                markDelete.IsDelete = true;
                return _DbContext.Entry(item);
            }
            else
                return _DbContext.Remove(item);
        }

        /// <summary>
        /// 比对新旧集合 进行增加，更改删除操作。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="older"></param>
        /// <param name="newer"></param>
        public void Set<T>(IEnumerable<T> older, IEnumerable<T> newer) where T : GuidKeyObjectBase
        {
            var dbSet = _DbContext.Set<T>();    //数据集

            //ISet<T> o = older as ISet<T>; o ??= older.ToHashSet();
            //ISet<T> n = newer as ISet<T>; n ??= newer.ToHashSet();

            var olderIds = older.Select(c => c.Id).ToHashSet();
            var newerIds = newer.Select(c => c.Id).ToHashSet();
            //删除
            var removes = older.ExceptBy(newerIds, c => c.Id).ToArray();
            //增加
            var adds = newer.ExceptBy(olderIds, c => c.Id).ToArray();
            //更新
            var updates = older.Join(newer, c => c.Id, c => c.Id, (c, d) => (c, d)).ToArray();

            dbSet.RemoveRange(removes);
            dbSet.AddRange(adds);
            foreach (var (o, n) in updates)
            {
                var entry = _DbContext.Entry(o);
                entry.CurrentValues.SetValues(n);
                //entry.State = EntityState.Modified;
            }
        }

        /// <summary>
        /// 复制对象，并能指定忽略属性和强行设置的新值。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="newVals">要设置的新值，不设置新值可以为null或空字典。字典可以被指定为键不区分大小写。</param>
        /// <param name="ignorePropertyNames">要忽略的属性名集合，不忽略可以为null或空集合。该集合可能本身就不区分大小写。</param>
        /// <returns></returns>
        public bool Copy<T>(T src, T dest, IDictionary<string, string> newVals, IEnumerable<string> ignorePropertyNames)
        {
            // 获取类型的所有属性，只需获取一次
            var properties = typeof(T).GetProperties();

            // 映射时直接使用原始的 ignorePropertyNames 进行映射
            _Mapper.Map(src, dest, c =>
            {
                if (ignorePropertyNames is not null)
                {
                    // 对于每个属性，检查是否在忽略列表中
                    // 这里使用属性的实际名称(区分大小写)，但通过 ignorePropertyNames.Contains 来判断
                    // ignorePropertyNames.Contains 会使用其自身的比较逻辑
                    foreach (var prop in properties)
                    {
                        if (ignorePropertyNames.Contains(prop.Name))
                        {
                            c.Items.Add($"-{prop.Name}", true);
                        }
                    }
                }
            });

            // 处理 newVals 中的特殊值
            if (newVals != null && newVals.Count > 0)
            {
                // 遍历所有可写属性
                foreach (var prop in properties.Where(p => p.CanWrite))
                {
                    // 尝试从 newVals 中获取值 - 这里会使用 newVals 的键比较逻辑
                    if (newVals.TryGetValue(prop.Name, out var strValue) &&
                        OwConvert.TryChangeType(strValue, prop.PropertyType, out var typedValue))
                    {
                        prop.SetValue(dest, typedValue);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 复制对象，强制不区分大小写处理属性名称，并能指定忽略属性和强行设置的新值。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="src">源对象</param>
        /// <param name="dest">目标对象</param>
        /// <param name="newVals">要设置的新值，不设置新值可以为null或空字典</param>
        /// <param name="ignorePropertyNames">要忽略的属性名集合，不忽略可以为null或空集合</param>
        /// <returns>是否成功复制</returns>
        public bool CopyIgnoreCase<T>(T src, T dest, IDictionary<string, string> newVals, IEnumerable<string> ignorePropertyNames)
        {
            // 创建不区分大小写的字典和集合
            var caseInsensitiveNewVals = newVals != null
                ? new Dictionary<string, string>(newVals, StringComparer.OrdinalIgnoreCase)
                : null;

            var caseInsensitiveIgnoreProps = ignorePropertyNames != null
                ? new HashSet<string>(ignorePropertyNames, StringComparer.OrdinalIgnoreCase)
                : null;

            // 复用现有的 Copy 方法实现
            return Copy(src, dest, caseInsensitiveNewVals, caseInsensitiveIgnoreProps);
        }

    }

    /// <summary>
    /// 分页/排序要求的基类。
    /// </summary>
    public class PagingParamsBase
    {
        /// <summary>
        /// 起始位置，从0开始。
        /// </summary>
        [Required, Range(0, int.MaxValue)]
        public int StartIndex { get; set; }

        /// <summary>
        /// 最大返回数量。
        /// 默认值-1，不限定返回数量。
        /// </summary>
        [Range(-1, int.MaxValue)]
        public int Count { get; set; } = -1;

        /// <summary>
        /// 排序的字段名。
        /// </summary>
        [Required]
        public string OrderFieldName { get; set; }

        /// <summary>
        /// 是否降序排序：true降序排序，false升序排序（省略或默认）。
        /// </summary>
        public bool IsDesc { get; set; }
    }

    /// <summary>
    /// 返回分页数据的封装类的基类
    /// </summary>
    /// <typeparam name="T">集合元素的类型。</typeparam>
    public class PagingReturnBase<T> : ReturnBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PagingReturnBase()
        {

        }

        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<T> Result { get; set; } = new List<T>();

    }

    /// <summary>
    /// 返回对象的基类。
    /// </summary>
    public class ReturnBase
    {
        /// <summary>
        /// 
        /// </summary>
        public ReturnBase()
        {

        }

        /// <summary>
        /// 是否有错误。不设置则使用<see cref="ErrorCode"/>来判定。
        /// </summary>
        /// <value>0没有错误，其它数值含义由应用定义。</value>
        public bool HasError { get; set; }

        /// <summary>
        /// 错误码，参见 ErrorCodes。
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 调试信息，如果发生错误，这里给出简要说明。
        /// </summary>
        public string DebugMessage { get; set; }

    }


}
