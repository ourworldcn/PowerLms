using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        void Saving(Dictionary<object, object> states);
    }

    /// <summary>
    /// EF Core 触发器类。
    /// </summary>
    public class OwEfTriggers : IDisposable
    {
        #region 私有字段
        private readonly IHostApplicationLifetime _HostApplicationLifetime;
        private bool _Disposed = false;
        #endregion 私有字段

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwEfTriggers(IHostApplicationLifetime hostApplicationLifetime)
        {
            _HostApplicationLifetime = hostApplicationLifetime;
            _HostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        }

        /// <summary>
        /// 应用程序停止时的处理逻辑。
        /// </summary>
        private void OnStopping()
        {
            // 在应用程序停止时执行的逻辑
        }

        /// <summary>
        /// 执行保存更改的事件处理逻辑。
        /// </summary>
        public void ExecuteSavingChanges(DbContext dbContext, IServiceProvider serviceProvider)
        {
            dbContext.ChangeTracker.DetectChanges();

            // 获取被更改的实体列表
            var entities = new HashSet<EntityEntry>(
                dbContext.ChangeTracker.Entries().Where(c => c.State == EntityState.Added || c.State == EntityState.Modified || c.State == EntityState.Deleted),
                new EntityEntryEqualityComparer()
            );

            var lastEntities = entities;
            var states = new Dictionary<object, object>();
            var types = new HashSet<Type>();

            while (entities.Count > 0)
            {
                // 按实体类型分组
                var lookup = entities.ToLookup(c => c.Metadata.ClrType);
                lookup.ForEach(c => types.Add(c.Key));

                foreach (var grp in lookup)
                {
                    var svcType = typeof(IDbContextSaving<>).MakeGenericType(grp.Key);
                    var svcs = serviceProvider.GetServices(svcType);

                    foreach (var svc in svcs)
                    {
                        svc.GetType().InvokeMember(
                            "Saving",
                            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            svc,
                            new object[] { grp.AsEnumerable(), states }
                        );
                    }
                }

                dbContext.ChangeTracker.DetectChanges();

                entities = new HashSet<EntityEntry>(
                    dbContext.ChangeTracker.Entries().Where(c => c.State == EntityState.Added || c.State == EntityState.Modified || c.State == EntityState.Deleted),
                    new EntityEntryEqualityComparer()
                );

                entities.ExceptWith(lastEntities);
            }

            // 触发 After Saving 事件
            foreach (var type in types)
            {
                var svcType = typeof(IAfterDbContextSaving<>).MakeGenericType(type);
                var svcs = serviceProvider.GetServices(svcType);

                foreach (var svc in svcs)
                {
                    svc.GetType().InvokeMember(
                        "Saving",
                        BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        svc,
                        new object[] { states }
                    );
                }
            }
        }

        #region IDisposable实现
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                }
                // 释放非托管资源
                _Disposed = true;
            }
        }

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
