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

namespace OW.Data
{
    /// <summary>
    /// 在保存之前对不同类型引发事件。
    /// </summary>
    public interface IDbContextSaving<T> where T : class
    {
        void Saving(IEnumerable<EntityEntry> entity, Dictionary<object, object> states);
    }

    /// <summary>
    /// 在保存之前且所有分类型的事件引发后，引发该事件。
    /// </summary>
    public interface IAfterDbContextSaving<T>
    {
        void Saving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states);
    }

    /// <summary>
    /// EF Core 触发器类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)] // 这个服务必须是范围的
    public class OwEfTriggers : IDisposable
    {
        #region 私有字段
        private readonly IHostApplicationLifetime _HostApplicationLifetime;
        private readonly ILogger<OwEfTriggers> _Logger;
        private bool _Disposed = false;
        #endregion 私有字段

        #region 构造函数
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwEfTriggers(IHostApplicationLifetime hostApplicationLifetime, ILogger<OwEfTriggers> logger)
        {
            _HostApplicationLifetime = hostApplicationLifetime;
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _HostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _Logger.LogDebug("OwEfTriggers initialized.");
        }
        #endregion 构造函数

        #region OnStopping 方法
        /// <summary>
        /// 应用程序停止时的处理逻辑。
        /// </summary>
        private void OnStopping()
        {
            // 在应用程序停止时执行的逻辑
            _Logger.LogDebug("Application is stopping.");
        }
        #endregion OnStopping 方法

        #region ExecuteSavingChanges 方法
        /// <summary>
        /// 执行保存更改的事件处理逻辑。
        /// </summary>
        public void ExecuteSavingChanges(DbContext dbContext, IServiceProvider serviceProvider)
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            _Logger.LogDebug("Starting ExecuteSavingChanges.");

            dbContext.ChangeTracker.DetectChanges();

            // 获取被更改的实体列表
            var entities = GetChangedEntities(dbContext);
            var lastEntities = entities;
            var states = new Dictionary<object, object>();
            var types = new HashSet<Type>();

            while (entities.Count > 0)
            {
                // 按实体类型分组
                var lookup = entities.ToLookup(c => c.Metadata.ClrType);
                foreach (var grp in lookup)
                {
                    types.Add(grp.Key);
                    InvokeSavingMethods(serviceProvider, grp.Key, grp.AsEnumerable(), states);
                }

                dbContext.ChangeTracker.DetectChanges();
                entities = GetChangedEntities(dbContext);
                entities.ExceptWith(lastEntities);
            }

            // 触发 After Saving 事件
            foreach (var type in types)
            {
                InvokeAfterSavingMethods(dbContext, serviceProvider, type, states);
            }

            _Logger.LogDebug("Finished ExecuteSavingChanges.");
        }
        #endregion ExecuteSavingChanges 方法

        #region 帮助方法
        private HashSet<EntityEntry> GetChangedEntities(DbContext dbContext)
        {
            return new HashSet<EntityEntry>(
                dbContext.ChangeTracker.Entries().Where(c => c.State == EntityState.Added || c.State == EntityState.Modified || c.State == EntityState.Deleted),
                new EntityEntryEqualityComparer()
            );
        }

        private void InvokeSavingMethods(IServiceProvider serviceProvider, Type entityType, IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            var svcType = typeof(IDbContextSaving<>).MakeGenericType(entityType);
            var svcs = serviceProvider.GetServices(svcType);

            foreach (var svc in svcs)
            {
                try
                {
                    svc.GetType().InvokeMember(
                        "Saving",
                        BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        svc,
                        new object[] { entities, states }
                    );
                }
                catch (Exception ex)
                {
                    _Logger.LogError(ex, "Error invoking Saving method for entity type {EntityType}.", entityType);
                }
            }
        }

        private void InvokeAfterSavingMethods(DbContext dbContext, IServiceProvider serviceProvider, Type entityType, Dictionary<object, object> states)
        {
            var svcType = typeof(IAfterDbContextSaving<>).MakeGenericType(entityType);
            var svcs = serviceProvider.GetServices(svcType);

            foreach (var svc in svcs)
            {
                try
                {
                    svc.GetType().InvokeMember(
                        "Saving",
                        BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        svc,
                        new object[] { dbContext, serviceProvider, states }
                    );
                }
                catch (Exception ex)
                {
                    _Logger.LogError(ex, "Error invoking AfterSaving method for entity type {EntityType}.", entityType);
                }
            }
        }
        #endregion 帮助方法

        #region IDisposable实现
        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">指示是否释放托管资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    _Logger.LogDebug("Releasing managed resources.");
                }

                // 释放非托管资源

                _Disposed = true;
                _Logger.LogDebug("OwEfTriggers disposed.");
            }
        }

        /// <summary>
        /// 实现 IDisposable 接口的方法。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable实现

        #region EntityEntryEqualityComparer类
        /// <summary>
        /// 用于比较 EntityEntry 的私有类。
        /// </summary>
        private class EntityEntryEqualityComparer : IEqualityComparer<EntityEntry>
        {
            public bool Equals(EntityEntry x, EntityEntry y)
            {
                return x.Entity.Equals(y.Entity);
            }

            public int GetHashCode(EntityEntry obj)
            {
                return obj.Entity.GetHashCode();
            }
        }
        #endregion EntityEntryEqualityComparer类
    }

    /// <summary>
    /// OwEfTriggers 扩展方法类。
    /// </summary>
    public static class OwEfTriggersExtensions
    {
        /// <summary>
        /// 将 OwEfTriggers 添加到 AOC 容器中。
        /// </summary>
        public static IServiceCollection AddOwEfTriggers(this IServiceCollection services)
        {
            services.AddSingleton<OwEfTriggers>();
            return services;
        }
    }
}
