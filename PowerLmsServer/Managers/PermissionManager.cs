using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.HSSF.Record;
using NPOI.SS.Formula.PTG;
using Org.BouncyCastle.Asn1.X509.Qualified;
using OW.DDD;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        public PermissionManager(IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, RoleManager roleManager)
        {
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            _RoleManager = roleManager;
        }

        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        readonly RoleManager _RoleManager;

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
        /// 获取或加载所有权限对象。
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, PlPermission> GetOrLoadPermission()
        {
            var result = _Cache.GetOrCreate(PermissionsCacheKey, entry =>
            {
                var db = _DbContextFactory.CreateDbContext();
                var r = LoadPermission(ref db);
                var cts = new CancellationTokenSource();
                var item = new OwCacheItem<ConcurrentDictionary<string, PlPermission>>()
                {
                    Data = r,
                    CancellationTokenSource = cts,
                };
                entry.AddExpirationToken(new CancellationChangeToken(cts.Token));
                using var t = db;
                return item;
            });

            return result.Data;
        }

        /// <summary>
        /// 获取用户当前有效的权限。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, PlPermission> LoadCurrentPermissionsByUser(Account user, ref PowerLmsUserDbContext db)
        {
            var roles = _RoleManager.GetOrLoadCurrentRolesCacheItemByUser(user);

            db ??= _DbContextFactory.CreateDbContext();
            var ids = db.PlRolePermissions.Where(c => roles.Data.Keys.Contains(c.RoleId)).Select(c => c.PermissionId).Distinct().AsEnumerable().ToHashSet();

            var allPerm = GetOrLoadPermission();
            var coll = allPerm.Where(c => ids.Contains(c.Key));
            return new ConcurrentDictionary<string, PlPermission>(coll);
        }

        /// <summary>
        /// 获取或加载用户当前机构下的所有授权。
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, PlPermission> GetOrLoadCurrentPermissionsByUser(Account user)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(user.Id, ".CurrentPermissions"), c =>
            {
                var db = user.DbContext;
                var r = LoadCurrentPermissionsByUser(user, ref db); Debug.Assert(ReferenceEquals(user.DbContext, db));
                using var t = db;

                var item = new OwCacheItem<ConcurrentDictionary<string, PlPermission>>()
                {
                    Data = r,
                };
                item.SetCancellations(new CancellationTokenSource(), user.ExpirationTokenSource);
                return item;
            });
            return result.Data;
        }

        /// <summary>
        /// 指示权限已经发生变化。
        /// </summary>
        /// <param name="userId">用户Id。</param>
        /// <returns>true 设置了变化信号，false未加载相关数据。</returns>
        public bool SetChange(Guid userId)
        {
            if (_Cache.Get(OwCacheHelper.GetCacheKeyFromId(userId, ".CurrentPermissions")) is not OwCacheItem<ConcurrentDictionary<string, PlPermission>> ci)
                return false;
            ci.CancellationTokenSource.Cancel();
            return true;
        }
    }
}
