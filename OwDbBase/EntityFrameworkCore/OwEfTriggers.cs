/*
文件名称: OwEfTriggers.cs
作者: OW
创建日期: 2025年2月6日
修改日期: 2025年2月8日
描述: 这个文件包含 EF Core 触发器类 OwEfTriggers 以及相关的接口和扩展方法。
*/
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace OW.EntityFrameworkCore
{
    #region 接口 IDbContextSaving
    /// <summary>
    /// 在保存之前对不同类型引发事件。
    /// </summary>
    public interface IDbContextSaving<T> where T : class
    {
        /// <summary>
        /// 在保存之前引发事件。
        /// </summary>
        /// <param name="entity">实体条目集合。此集合一定不为空。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        void Saving(IEnumerable<EntityEntry> entity, IServiceProvider serviceProvider, Dictionary<object, object> states);
        /// <summary>
        /// 优先级。值越小，优先级越高。
        /// </summary>
        public int Priority => 10000;
    }
    #endregion 接口 IDbContextSaving
    #region 接口 IAfterDbContextSaving
    /// <summary>
    /// 在保存之前且所有分类型的事件引发后，引发该事件。
    /// </summary>
    public interface IAfterDbContextSaving<T>
    {
        /// <summary>
        /// 在保存之前且所有分类型的事件引发后，引发该事件。
        /// </summary>
        /// <param name="dbContext">数据即将被保存的 DbContext 实例。</param>
        /// <param name="serviceProvider">数据上下文所属的服务提供者。</param>
        /// <param name="states">状态字典。</param>
        void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states);
    }
    #endregion 接口 IAfterDbContextSaving
}
