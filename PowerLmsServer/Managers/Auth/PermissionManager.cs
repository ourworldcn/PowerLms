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
using System.Runtime.CompilerServices;
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

        #region 所有权限对象及相关

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
            // 原方法保持为空，符合原有实现
        }

        /// <summary>
        /// 获取所有权限。
        /// </summary>
        /// <returns>所有权限的集合，如果没有找到则返回null</returns>
        public ConcurrentDictionary<string, PlPermission> GetPermissions()
        {
            return _Cache.TryGetValue(PermissionsCacheKey, out ConcurrentDictionary<string, PlPermission> permissions) ? permissions : null;
        }

        /// <summary>
        /// 使全局权限缓存失效。
        /// </summary>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidatePermissionCache()
        {
            // ✅ 使用新API: 获取取消令牌源并取消
            var cts = _Cache.GetCancellationTokenSource(PermissionsCacheKey);
            if (cts != null && !cts.IsCancellationRequested)
            {
                try
                {
                    cts.Cancel();
                    return true;
                }
                catch { /* 忽略可能的异常 */ }
            }
            return false;
        }

        /// <summary>
        /// 获取或加载所有权限对象。
        /// </summary>
        /// <returns>所有权限的集合</returns>
        public ConcurrentDictionary<string, PlPermission> GetOrLoadPermissions()
        {
            return _Cache.GetOrCreate(PermissionsCacheKey, entry =>
{
    // ? 启用优先级驱逐回调
    entry.EnablePriorityEvictionCallback(_Cache);

    var db = _DbContextFactory.CreateDbContext();
    try
    {
        var permissions = LoadPermission(ref db);

        // 配置缓存条目
        ConfigurePermissionsCacheEntry(entry);

        return permissions;
    }
    finally
    {
        db?.Dispose();
    }
});
        }

        /// <summary>
        /// 配置权限缓存条目
        /// </summary>
        /// <param name="entry">缓存条目</param>
        private void ConfigurePermissionsCacheEntry(ICacheEntry entry)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));

            // ✅ 启用优先级驱逐回调
            entry.EnablePriorityEvictionCallback(_Cache);

            // ✅ 获取取消令牌源并注册到过期令牌列表
            var cts = _Cache.GetCancellationTokenSource(entry.Key);
            entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token));
        }

        #endregion 所有权限对象及相关

        /// <summary>
        /// 获取指定用户的当前权限。
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>指定用户的当前权限，没有则返回null</returns>
        public ConcurrentDictionary<string, PlPermission> GetCurrentPermissions(Guid userId)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(userId, ".CurrentPermissions");
            return _Cache.TryGetValue(cacheKey, out ConcurrentDictionary<string, PlPermission> permissions) ? permissions : null;
        }

        /// <summary>
        /// 使指定用户ID的权限缓存失效。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateUserPermissionsCache(Guid userId)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(userId, ".CurrentPermissions");
            // ✅ 使用新API: 获取取消令牌源并取消
            var cts = _Cache.GetCancellationTokenSource(cacheKey);
            if (cts != null && !cts.IsCancellationRequested)
            {
                try
                {
                    cts.Cancel();
                    return true;
                }
                catch { /* 忽略可能的异常 */ }
            }
            return false;
        }

        /// <summary>
        /// 获取用户当前有效的权限。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, PlPermission> LoadCurrentPermissionsByUser(Account user, ref PowerLmsUserDbContext db)
        {
            var roles = _RoleManager.GetOrLoadCurrentRolesByUser(user);    // 用户所属的所有角色

            db ??= _DbContextFactory.CreateDbContext();
            HashSet<string> ids;
            lock (db)
                ids = db.PlRolePermissions.Where(c => roles.Keys.Contains(c.RoleId))
                    .Select(c => c.PermissionId)
                    .Distinct()
                    .AsEnumerable()
                    .ToHashSet();

            var allPerm = GetOrLoadPermissions();
            var coll = allPerm.Where(c => ids.Contains(c.Key));
            return new ConcurrentDictionary<string, PlPermission>(coll);
        }

        /// <summary>
        /// 获取或加载用户当前机构下的所有授权。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>用户当前的所有权限</returns>
        public ConcurrentDictionary<string, PlPermission> GetOrLoadUserCurrentPermissions(Account user)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(user.Id, ".CurrentPermissions");

            return _Cache.GetOrCreate(cacheKey, entry =>
   {
       // ? 启用优先级驱逐回调
       entry.EnablePriorityEvictionCallback(_Cache);

       var db = user.DbContext;
       var permissions = LoadCurrentPermissionsByUser(user, ref db);

       // 配置缓存条目
       ConfigureUserPermissionsCacheEntry(entry, user.Id);

       return permissions;
   });
        }

        /// <summary>
        /// 配置用户当前权限缓存条目
        /// </summary>
        /// <param name="entry">缓存条目</param>
        /// <param name="userId">用户ID</param>
        private void ConfigureUserPermissionsCacheEntry(ICacheEntry entry, Guid userId)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

            // ✅ 启用优先级驱逐回调
            entry.EnablePriorityEvictionCallback(_Cache);

            // ✅ 获取取消令牌源并注册到过期令牌列表
            var cts = _Cache.GetCancellationTokenSource(entry.Key);
            entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token));

            // 添加权限和角色更改的依赖关系
            // 获取全局权限缓存的取消令牌源
            var permissionsTokenSource = _Cache.GetCancellationTokenSource(PermissionsCacheKey);
            if (permissionsTokenSource != null)
            {
                entry.AddExpirationToken(new CancellationChangeToken(permissionsTokenSource.Token));
            }

            // 获取用户角色缓存的取消令牌源
            string rolesCacheKey = OwCacheExtensions.GetCacheKeyFromId(userId, ".CurrentRoles");
            var rolesTokenSource = _Cache.GetCancellationTokenSource(rolesCacheKey);
            if (rolesTokenSource != null)
            {
                entry.AddExpirationToken(new CancellationChangeToken(rolesTokenSource.Token));
            }
        }
    }
}

