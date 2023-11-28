using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
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
        public EntityManager(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 修改可软删除的对象集合。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newValues"></param>
        /// <returns>true成功修改，调用着需要最终保存数据，false出现错误，调用应放弃保存。</returns>
        public bool Modify<T>(IEnumerable<T> newValues) where T : GuidKeyObjectBase, IMarkDelete
        {
            if (!ModifyEntities(newValues)) return false;
            var dbSet = _DbContext.Set<T>();
            foreach (var item in newValues)
            {
                var tmp = dbSet.Find(item.Id);
                if (tmp is null)
                {
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, $"找不到指定Id——{item.Id}");
                    return false;
                }
                _DbContext.Entry(tmp).Property(c => c.IsDelete).IsModified = false;
            }
            return true;
        }

        /// <summary>
        /// 修改的对象集合。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newValues"></param>
        /// <returns>true成功修改，调用着需要最终保存数据，false出现错误，调用应放弃保存。</returns>
        public bool ModifyEntities<T>(IEnumerable<T> newValues) where T : GuidKeyObjectBase
        {
            var dbSet = _DbContext.Set<T>();
            var ids = newValues.Select(c => c.Id).ToArray();
            var accColl = dbSet.Where(c => ids.Contains(c.Id)); //单次一起加载
            foreach (var item in newValues)
            {
                var tmp = dbSet.Find(item.Id);
                if (tmp is null)
                {
                    OwHelper.SetLastErrorAndMessage((int)HttpStatusCode.BadRequest, $"找不到指定Id——{item.Id}");
                    return false;
                }
                _DbContext.Entry(tmp).CurrentValues.SetValues(item);
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
    }
}
