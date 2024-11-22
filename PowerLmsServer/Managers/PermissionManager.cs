using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Org.BouncyCastle.Asn1.X509.Qualified;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 许可对象的管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class PermissionManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PermissionManager(IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory)
        {
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
        }

        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;

        /// <summary>
        /// 缓存中的Key。
        /// </summary>
        public static readonly string PermissionsCacheKey = typeof(PlPermission).GUID.ToString() + ".Permissions";

        /// <summary>
        /// 加载数据库中许可对象。
        /// </summary>
        /// <param name="dbContext">返回时此上下文已经不再使用，加载对象均已完全加载。</param>
        /// <returns></returns>
        public ConcurrentDictionary<string, PlPermission> LoadPermission(ref PowerLmsUserDbContext dbContext)
        {
            dbContext ??= _DbContextFactory.CreateDbContext();
            IDictionary<string, PlPermission> dic;
            lock (dbContext)
            {
                dic = dbContext.PlPermissions.Include(c => c.Children).Include(c => c.Parent).AsEnumerable().ToDictionary(c => c.Name);
                PermissionLoaded(dic);
            }
            return new ConcurrentDictionary<string, PlPermission>(dic);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        public void PermissionLoaded(IDictionary<string, PlPermission> dic)
        {
            //foreach (var kvp in dic)
            //{
            //    var p = kvp.Value.Parent;
            //    var c = kvp.Value.Children;
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, PlPermission> GetOrLoadPermission()
        {
            var result = _Cache.GetOrCreate(PermissionsCacheKey, entry =>
            {
                var db = _DbContextFactory.CreateDbContext();
                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                entry.AddExpirationToken(new CancellationChangeToken(ct));
                var result = LoadPermission(ref db);
                using var t = db;
                return result;
            });
            return result;
        }
    }
}
