using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.OpenXmlFormats.Shared;
using NPOI.SS.Formula.Atp;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 组织机构管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OrganizationManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationManager(IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, MerchantManager merchantManager, AccountManager accountManager)
        {
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            _MerchantManager = merchantManager;
            _AccountManager = accountManager;
        }

        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        readonly MerchantManager _MerchantManager;
        readonly AccountManager _AccountManager;

        #region 机构缓存及相关
        /// <summary>
        /// 加载指定商户下所有机构对象。
        /// </summary>
        /// <param name="merchId">商户ID</param>
        /// <param name="dbContext">使用的数据库上下文，null则自动从池中取一个新上下文,调用者负责处置对象。</param>
        /// <returns>指定商户下的所有组织机构的并发字典</returns>
        public ConcurrentDictionary<Guid, PlOrganization> LoadOrgsByMerchantId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            dbContext ??= _DbContextFactory.CreateDbContext();
            IDictionary<Guid, PlOrganization> tmp;
            ConcurrentDictionary<Guid, PlOrganization> result;

            lock (dbContext)
            {
                // 1. 首先查询该商户直接关联的顶层组织机构（总公司，ParentId为null且MerchantId为指定商户ID）
                var rootOrgs = dbContext.PlOrganizations
                    .Where(c => c.MerchantId == merchId && c.ParentId == null)
                    .Include(c => c.Parent)
                    .Include(c => c.Children)
                    .AsEnumerable();

                // 2. 如果没有找到任何顶层机构，返回空字典
                if (!rootOrgs.Any())
                {
                    return new ConcurrentDictionary<Guid, PlOrganization>();
                }

                // 3. 对每个顶层组织机构，获取其所有子孙节点
                tmp = rootOrgs.SelectMany(c => OwHelper.GetAllSubItemsOfTree(new[] { c }, d => d.Children))
                    .ToDictionary(c => c.Id);

                // 4. 创建并发字典
                result = new ConcurrentDictionary<Guid, PlOrganization>(tmp);

                // 5. 加载完成后的处理
                if (result.Count > 0)
                {
                    OrgsLoaded(result);
                }
            }

            return result;
        }

        /// <summary>
        /// 加载后调用。
        /// </summary>
        /// <param name="orgs"></param>
        public void OrgsLoaded(ConcurrentDictionary<Guid, PlOrganization> orgs)
        {
            var merchId = orgs.Values.First(c => c.MerchantId is not null).MerchantId.Value;
            var merch = _MerchantManager.GetOrLoadById(merchId);
            EnParent(orgs.Values);
        }

        /// <summary>
        /// 确保parent属性被读入。
        /// </summary>
        /// <param name="orgs"></param>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void EnParent(IEnumerable<PlOrganization> orgs) => orgs.ForEach(c =>
        {
            var tmp = c.Parent;
        });

        /// <summary>
        /// 获取指定商户的所有机构。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns>指定商户的所有机构，如果没找到则返回null。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> GetOrgsByMerchantId(Guid merchantId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchantId, ".Orgs");
            return _Cache.TryGetValue(cacheKey, out ConcurrentDictionary<Guid, PlOrganization> orgs) ? orgs : null;
        }

        /// <summary>
        /// 按指定商户Id获取或加载所有下属机构信息。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns>没有找到指定商户则返回空字典。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> GetOrLoadByMerchantId(Guid merchantId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchantId, ".Orgs");

            return _Cache.GetOrCreate(cacheKey, entry =>
            {
                // 获取商户信息
                var merch = _MerchantManager.GetOrLoadById(merchantId);
                if (merch == null) return new ConcurrentDictionary<Guid, PlOrganization>();

                // 使用商户的数据库上下文加载组织机构
                var db = merch.DbContext;
                var orgsData = LoadOrgsByMerchantId(merchantId, ref db);

                // 配置缓存条目
                ConfigureOrgsCacheEntry(entry, orgsData, merch);

                return orgsData;
            });
        }

        /// <summary>
        /// 使指定商户ID的组织机构缓存失效。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateOrgCache(Guid merchantId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchantId, ".Orgs");
            return _Cache.CancelSource(cacheKey);
        }

        /// <summary>
        /// 配置组织机构缓存条目属性
        /// </summary>
        /// <param name="entry">缓存条目</param>
        /// <param name="orgsData">组织机构数据</param>
        /// <param name="merch">关联的商户信息</param>
        private void ConfigureOrgsCacheEntry(ICacheEntry entry, ConcurrentDictionary<Guid, PlOrganization> orgsData,
            PlMerchant merch)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));

            // 使用OwMemoryCacheExtensions注册取消令牌
            entry.RegisterCancellationToken(_Cache);

            // 如果商户对象已关联取消令牌源，则添加依赖关系
            var merchantCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<PlMerchant>(merch.Id);
            var merchantTokenSource = _Cache.GetCancellationTokenSource(merchantCacheKey);

            if (merchantTokenSource != null)
            {
                entry.AddExpirationToken(new CancellationChangeToken(merchantTokenSource.Token));
            }
        }

        /// <summary>
        /// 按指定组织机构Id获取或加载所有同一商户下的机构信息。
        /// </summary>
        /// <param name="orgId">组织机构ID。</param>
        /// <returns>同一商户下的所有机构。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> GetOrLoadByOrgId(Guid orgId)
        {
            // 获取商户ID
            if (!_MerchantManager.TryGetIdByOrgOrMerchantId(orgId, out var merchantId))
            {
                throw new Exception($"Merchant for OrganizationId {orgId} not found.");
            }

            // 获取或加载同一个商户下的所有机构
            return GetOrLoadByMerchantId(merchantId.Value);
        }
        #endregion 机构缓存及相关

        /// <summary>
        /// 获取当前的登录公司及子机构但排除下属公司。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>如果没有指定所属当前机构则返回空字典。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> LoadCurrentOrgsByUser(Account user)
        {
            if (GetCurrentCompanyByUser(user) is not PlOrganization root) return new ConcurrentDictionary<Guid, PlOrganization>();
            var result = new ConcurrentDictionary<Guid, PlOrganization>(OwHelper.GetAllSubItemsOfTree(root, c => c.Children).ToDictionary(c => c.Id));
            var ids = new List<Guid>();  //需要排除的下属子公司及其机构
            foreach (var child in root.Children)
            {
                if (child.Otc == 2 && child != root) //若应当排除
                {
                    ids.AddRange(OwHelper.GetAllSubItemsOfTree(child, c => c.Children).Select(c => c.Id));
                }
            }
            ids.ForEach(c => result.TryRemove(c, out _));
            return result;
        }

        /// <summary>
        /// 获取当前的登录公司及子机构但排除下属公司。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>如果没有指定所属当前机构则返回空字典。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> GetOrLoadCurrentOrgsByUser(Account user)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(user.Id, ".CurrentOrgs");

            return _Cache.GetOrCreate(cacheKey, entry =>
            {
                // 加载当前用户的组织机构
                var currentOrgsData = LoadCurrentOrgsByUser(user);

                // 获取用户所属商户信息
                var merchant = _MerchantManager.GetOrLoadByUser(user);
                if (merchant != null)
                {
                    // 配置缓存条目
                    ConfigureCurrentOrgsCacheEntry(entry, user.Id);
                }

                return currentOrgsData;
            });
        }

        /// <summary>
        /// 使指定用户ID的当前组织机构缓存失效。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateCurrentOrgCache(Guid userId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(userId, ".CurrentOrgs");
            return _Cache.CancelSource(cacheKey);
        }

        /// <summary>
        /// 配置当前用户组织机构缓存条目
        /// </summary>
        /// <param name="entry">缓存条目</param>
        /// <param name="userId">用户ID</param>
        private void ConfigureCurrentOrgsCacheEntry(ICacheEntry entry, Guid userId)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

            // 使用OwMemoryCacheExtensions注册取消令牌
            entry.RegisterCancellationToken(_Cache);

            // 添加对用户账号缓存的依赖
            var accountCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<Account>(userId);
            var accountTokenSource = _Cache.GetCancellationTokenSource(accountCacheKey);

            if (accountTokenSource != null)
            {
                entry.AddExpirationToken(new CancellationChangeToken(accountTokenSource.Token));
            }
        }

        /// <summary>
        /// 获取用户当前登录到的公司。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>返回登录的公司，该对象不可更改。没有登录到机构时会返回null。</returns>
        public PlOrganization GetCurrentCompanyByUser(Account user)
        {
            if (user.OrgId is null) return null;

            var merchItem = _MerchantManager.GetOrLoadByUser(user);
            if (merchItem == null) return null;

            var orgs = GetOrLoadByMerchantId(merchItem.Id);
            if (orgs == null || !orgs.TryGetValue(user.OrgId.Value, out var org)) return null;

            PlOrganization tmp;
            for (tmp = org; tmp is not null && tmp.Otc != 2; tmp = tmp.Parent) ;
            return tmp;
        }
    }
}
