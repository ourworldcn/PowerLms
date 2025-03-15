using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.Util.HSSFColor;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 角色管理器
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class RoleManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public RoleManager(MerchantManager merchantManager, OrganizationManager organizationManager, IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, AccountManager accountManager)
        {
            _MerchantManager = merchantManager;
            _OrganizationManager = organizationManager;
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            _AccountManager = accountManager;
        }

        readonly MerchantManager _MerchantManager;
        readonly OrganizationManager _OrganizationManager;
        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        readonly AccountManager _AccountManager;

        /// <summary>
        /// 从数据库调入指定商户下的所有角色。
        /// </summary>
        /// <param name="merchId"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlRole> LoadByMerchantId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            var orgs = _OrganizationManager.GetOrLoadByMerchantId(merchId);
            var orgIds = orgs.Keys;
            dbContext ??= _DbContextFactory.CreateDbContext();
            lock (dbContext)
            {
                var tmp = dbContext.PlRoles.Where(c => orgIds.Contains(c.OrgId.Value)).AsEnumerable().ToDictionary(c => c.Id);
                return new ConcurrentDictionary<Guid, PlRole>(tmp);
            }
        }

        /// <summary>
        /// 获取指定商户下所有角色。
        /// </summary>
        /// <param name="merchId"></param>
        /// <returns>指定商户下所有角色，如果没找到则返回null。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetRolesByMerchantId(Guid merchId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchId, ".Roles");
            return _Cache.TryGetValue(cacheKey, out ConcurrentDictionary<Guid, PlRole> roles) ? roles : null;
        }

        /// <summary>
        /// 获取或加载商户下所有角色。
        /// </summary>
        /// <param name="merchId">商户Id。</param>
        /// <returns>商户下所有角色。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadRolesByMerchantId(Guid merchId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchId, ".Roles");

            return _Cache.GetOrCreate(cacheKey, entry =>
            {
                // 获取商户和组织信息
                var merchant = _MerchantManager.GetOrLoadById(merchId);
                if (merchant == null) return new ConcurrentDictionary<Guid, PlRole>();

                var orgs = _OrganizationManager.GetOrLoadByMerchantId(merchId);

                // 加载角色数据
                PowerLmsUserDbContext db = merchant.DbContext;
                var rolesData = LoadByMerchantId(merchId, ref db);

                // 配置缓存条目
                ConfigureRolesCacheEntry(entry, rolesData);

                return rolesData;
            });
        }

        /// <summary>
        /// 使指定商户ID的角色缓存失效。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateRoleCache(Guid merchantId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchantId, ".Roles");
            return _Cache.CancelSource(cacheKey);
        }

        /// <summary>
        /// 配置角色缓存条目属性
        /// </summary>
        /// <param name="entry">缓存条目</param>
        /// <param name="rolesData">角色数据</param>
        private void ConfigureRolesCacheEntry(ICacheEntry entry, ConcurrentDictionary<Guid, PlRole> rolesData)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));

            // 使用OwMemoryCacheExtensions注册取消令牌
            entry.RegisterCancellationToken(_Cache);
        }

        /// <summary>
        /// 返回指定用户当前登录公司的所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>用户当前登录公司的所有角色，如果没找到则返回null。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetCurrentRoles(Account user)
        {
            return GetCurrentRoles(user.Id);
        }

        /// <summary>
        /// 返回指定用户当前登录公司的所有角色。
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>用户当前登录公司的所有角色，如果没找到则返回null。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetCurrentRoles(Guid userId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(userId, ".CurrentRoles");
            return _Cache.TryGetValue(cacheKey, out ConcurrentDictionary<Guid, PlRole> roles) ? roles : null;
        }

        /// <summary>
        /// 使指定用户ID的角色缓存失效。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateUserRolesCache(Guid userId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(userId, ".CurrentRoles");
            return _Cache.CancelSource(cacheKey);
        }

        /// <summary>
        /// 按指定用户当前的登录机构加载其所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        public ConcurrentDictionary<Guid, PlRole> LoadCurrentRolesByUser(Account user, ref PowerLmsUserDbContext db)
        {
            var orgs = _OrganizationManager.GetOrLoadCurrentOrgsByUser(user);   // 用户登录的有效机构集合
            var merchant = _MerchantManager.GetOrLoadByUser(user);
            if (merchant == null) return new ConcurrentDictionary<Guid, PlRole>();

            var allRoles = GetOrLoadRolesByMerchantId(merchant.Id);   // 商户下所有角色

            db ??= _DbContextFactory.CreateDbContext();
            HashSet<Guid> hsRoleIds;
            lock (db)
                hsRoleIds = db.PlAccountRoles.Where(c => c.UserId == user.Id).Select(c => c.RoleId).ToHashSet(); // 所有直属角色Id集合

            var coll = allRoles.Where(c => c.Value.OrgId.HasValue && hsRoleIds.Contains(c.Key)   // 直属角色
                && orgs.ContainsKey(c.Value.OrgId.Value));    // 属于有效机构

            return new ConcurrentDictionary<Guid, PlRole>(coll);
        }

        /// <summary>
        /// 获取用户当前登录的所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>所有当前有效角色的字典。未登录到公司则返回空字典。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadCurrentRolesByUser(Account user)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(user.Id, ".CurrentRoles");

            return _Cache.GetOrCreate(cacheKey, entry =>
            {
                // 加载用户角色数据
                PowerLmsUserDbContext db = user.DbContext;
                var rolesData = LoadCurrentRolesByUser(user, ref db);

                // 配置缓存条目
                ConfigureCurrentRolesCacheEntry(entry);

                return rolesData;
            });
        }

        /// <summary>
        /// 配置当前用户角色缓存条目属性
        /// </summary>
        /// <param name="entry">缓存条目</param>
        private void ConfigureCurrentRolesCacheEntry(ICacheEntry entry)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

            // 使用OwMemoryCacheExtensions注册取消令牌
            entry.RegisterCancellationToken(_Cache);
        }
    }

}
