using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    /// 
    /// </summary>
    public class OwEfTriggers
    {
    }

    public class OwEfTriggersSaveChangesInterceptor : SaveChangesInterceptor
    {
        class EntityEntryEqualityComparer : EqualityComparer<EntityEntry>
        {
            public override bool Equals(EntityEntry b1, EntityEntry b2)
            {
                return b1.Entity.Equals(b2.Entity);
            }

            public override int GetHashCode(EntityEntry bx)
            {
                return bx.Entity.GetHashCode();
            }
        }

        IServiceProvider _ServiceProvider;

        public OwEfTriggersSaveChangesInterceptor()
        {
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            return InnerSavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            return new ValueTask<InterceptionResult<int>>(Task.Run(() => InnerSavingChanges(eventData, result)));
        }

        protected virtual InterceptionResult<int> InnerSavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var db = eventData.Context;
            db.ChangeTracker.DetectChanges();
            var entities = new HashSet<EntityEntry>(db.ChangeTracker.Entries().Where(c => c.State == EntityState.Added || c.State == EntityState.Modified ||
                c.State == EntityState.Deleted), new EntityEntryEqualityComparer());
            var lastEntities = entities;
            dynamic dyn = db;
            IServiceProvider con = dyn.ServiceProvider;
            HashSet<Type> types = new HashSet<Type>();

            var states = new Dictionary<object, object>();
            for (; entities.Count > 0;)
            {
                var lookup = entities.ToLookup(c => c.Metadata.ClrType);
                lookup.ForEach(c => types.Add(c.Key));
                foreach (var grp in lookup)
                {
                    var svcType = typeof(IDbContextSaving<>).MakeGenericType(grp.Key);
                    var svcs = con.GetServices(svcType);
                    foreach (var svc in svcs)
                    {
                        svc.GetType().InvokeMember("Saving", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            default, svc, new object[] { grp.AsEnumerable(), states });
                    }
                }
                lastEntities = entities;
                db.ChangeTracker.DetectChanges();
                entities = new HashSet<EntityEntry>(db.ChangeTracker.Entries().Where(c => c.State == EntityState.Added || c.State == EntityState.Modified ||
                    c.State == EntityState.Deleted), new EntityEntryEqualityComparer());
                entities.ExceptWith(lastEntities);
            }
            foreach (var type in types)
            {
                var svcType = typeof(IAfterDbContextSaving<>).MakeGenericType(type);
                var svcs = con.GetServices(svcType);
                foreach (var svc in svcs)
                {
                    var ss = nameof(IAfterDbContextSaving<OwEfTriggers>.Saving);
                    svc.GetType().InvokeMember("Saving", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        default, svc, new object[] { states });
                }
            }
            return result;
        }
    }

    public static class OwEfTriggersExtensions
    {
        public static DbContextOptionsBuilder UseOwEfTriggers(this DbContextOptionsBuilder builder)
        {
            return builder.AddInterceptors(new OwEfTriggersSaveChangesInterceptor());
        }
    }
}
