using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System.Collections.Concurrent;

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
        public RoleManager(OrgManager<PowerLmsUserDbContext> orgManager, IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, 
            AccountManager accountManager)
        {
            _OrgManager = orgManager;
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            _AccountManager = accountManager;
        }

        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        readonly AccountManager _AccountManager;

        /// <summary>
        /// 从数据库调入指定商户下的所有角色。
        /// </summary>
        /// <param name="merchId">商户Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>指定商户下所有角色的字典集合</returns>
        private ConcurrentDictionary<Guid, PlRole> LoadByMerchantId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            var orgIds = _OrgManager.GetOrLoadOrgCacheItem(merchId).Orgs.Keys.ToHashSet();
            // 添加商户ID本身到查询范围
            orgIds.Add(merchId);
            
            dbContext ??= _DbContextFactory.CreateDbContext();
            
            lock (dbContext)
            {
                // ✅ 使用 AsNoTracking 确保返回只读对象
                // ✅ 使用 ToList() 立即执行查询
                var roles = dbContext.PlRoles
                    .AsNoTracking()
                    .Where(c => c.OrgId.HasValue && orgIds.Contains(c.OrgId.Value))
                    .ToList()
                    .ToDictionary(c => c.Id);
                
                return new ConcurrentDictionary<Guid, PlRole>(roles);
            }
        }

        /// <summary>
        /// 获取指定商户下所有角色。
        /// </summary>
        /// <param name="merchId"></param>
        /// <returns>指定商户下所有角色，如果没找到则返回null。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetRolesByMerchantId(Guid merchId)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(merchId, ".Roles");
            return _Cache.TryGetValue(cacheKey, out ConcurrentDictionary<Guid, PlRole> roles) ? roles : null;
        }

        /// <summary>
        /// 获取或加载商户下所有角色。
        /// </summary>
        /// <param name="merchId">商户Id。</param>
        /// <returns>商户下所有角色。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadRolesByMerchantId(Guid merchId)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(merchId, ".Roles");

            return _Cache.GetOrCreate(cacheKey, entry =>
            {
                // ✅ 启用优先级驱逐回调
                entry.EnablePriorityEvictionCallback(_Cache);
                
                // 获取商户和组织信息(只用于验证商户存在)
                var cacheItem = _OrgManager.GetOrLoadOrgCacheItem(merchId);
                if (cacheItem.Merchant == null) return new ConcurrentDictionary<Guid, PlRole>();

                // ✅ 修复: 创建独立的 DbContext
                PowerLmsUserDbContext db = null;
                var rolesData = LoadByMerchantId(merchId, ref db);
                
                // ✅ 确保释放 DbContext
                db?.Dispose();

                // 配置缓存条目
                ConfigureRolesCacheEntry(entry, rolesData);

                return rolesData;
            });
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

            // 启用优先级驱逐回调
            entry.EnablePriorityEvictionCallback(_Cache);

            // 获取取消令牌源并注册到过期令牌列表
            var cts = _Cache.GetCancellationTokenSource(entry.Key);
            entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token));
        }

        /// <summary>
        /// 按指定用户当前的登录机构加载其所有角色。
        /// </summary>
        /// <param name="user">用户对象</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>用户当前有效的角色集合</returns>
        public ConcurrentDictionary<Guid, PlRole> LoadCurrentRolesByUser(Account user, ref PowerLmsUserDbContext db)
        {
            // 获取用户当前登录的公司及其下属机构ID
            if (user.OrgId == null) return new ConcurrentDictionary<Guid, PlRole>();
            var companyId = _OrgManager.GetCompanyIdByOrgId(user.OrgId.Value);
            if (!companyId.HasValue) return new ConcurrentDictionary<Guid, PlRole>();
            var orgIds = _OrgManager.GetOrgIdsByCompanyId(companyId.Value).ToHashSet();
            // 获取用户所属商户ID
            var merchantId = _OrgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue) return new ConcurrentDictionary<Guid, PlRole>();
            orgIds.Add(merchantId.Value); // 添加商户ID本身到查询范围
            db ??= _DbContextFactory.CreateDbContext();
            HashSet<Guid> hsRoleIds;
            lock (db)
                hsRoleIds = db.PlAccountRoles
                    .AsNoTracking()
                    .Where(c => c.UserId == user.Id)
                    .Select(c => c.RoleId)
                    .ToHashSet(); // 所有直属角色Id集合
            if (hsRoleIds.Count == 0) return new ConcurrentDictionary<Guid, PlRole>(); // 如果用户没有角色，直接返回空字典
            // ✅ 使用 AsNoTracking 确保返回只读对象
            // ✅ 使用 ToList() 立即执行查询
            var userRoles = db.PlRoles
                .AsNoTracking()
                .Where(c => hsRoleIds.Contains(c.Id) && 
                           c.OrgId.HasValue && orgIds.Contains(c.OrgId.Value))
                .ToList()
                .ToDictionary(c => c.Id);
            return new ConcurrentDictionary<Guid, PlRole>(userRoles);
        }

        /// <summary>
        /// 配置当前用户角色缓存条目属性
        /// </summary>
        /// <param name="entry">缓存条目</param>
        private void ConfigureCurrentRolesCacheEntry(ICacheEntry entry)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

            // 启用优先级驱逐回调
            entry.EnablePriorityEvictionCallback(_Cache);

            // 获取取消令牌源并注册到过期令牌列表
            var cts = _Cache.GetCancellationTokenSource(entry.Key);
            entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token));
        }

        /// <summary>
        /// 使指定商户ID的角色缓存失效。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateRoleCache(Guid merchantId)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(merchantId, ".Roles");
            // 使用新API: 获取取消令牌源并取消
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
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(userId, ".CurrentRoles");
            return _Cache.TryGetValue(cacheKey, out ConcurrentDictionary<Guid, PlRole> roles) ? roles : null;
        }

        /// <summary>
        /// 使指定用户ID的角色缓存失效。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateUserRolesCache(Guid userId)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(userId, ".CurrentRoles");
            // 使用新API: 获取取消令牌源并取消
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
        /// 获取用户当前登录的所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>所有当前有效角色的字典。未登录到公司则返回空字典。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadCurrentRolesByUser(Account user)
        {
            string cacheKey = OwCacheExtensions.GetCacheKeyFromId(user.Id, ".CurrentRoles");

            return _Cache.GetOrCreate(cacheKey, entry =>
            {
                // ✅ 启用优先级驱逐回调
                entry.EnablePriorityEvictionCallback(_Cache);
                
                // ✅ 修复: 创建独立的 DbContext
                PowerLmsUserDbContext db = null;
                var rolesData = LoadCurrentRolesByUser(user, ref db);
                
                // ✅ 确保释放 DbContext
                db?.Dispose();

                // 配置缓存条目
                ConfigureCurrentRolesCacheEntry(entry);

                return rolesData;
            });
        }
    }
}
