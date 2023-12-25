using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
                _DbContext.Entry(tmp).CurrentValues.SetValues(item);
                try
                {
                    _Mapper.Map(item, tmp, typeof(T), typeof(T));
                }
                catch (AutoMapperMappingException)  //忽略不能映射的情况
                {
                }
                result?.Add(tmp);
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
