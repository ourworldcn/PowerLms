using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 商户和组织缓存项。
    /// </summary>
    /// <typeparam name="TDbContext">数据库上下文类型。</typeparam>
    public class OrgCacheItem<TDbContext> where TDbContext : OwDbContext
    {
        /// <summary>
        /// 商户对象。
        /// </summary>
        public PlMerchant Merchant { get; internal set; }

        internal ConcurrentDictionary<Guid, PlOrganization> _Orgs;

        /// <summary>
        /// 获取的组织机构列表。键是机构的Id，值是机构对象。
        /// </summary>
        public IReadOnlyDictionary<Guid, PlOrganization> Orgs { get => _Orgs ??= new ConcurrentDictionary<Guid, PlOrganization>(); }
    }

    /// <summary>
    /// 合并商户和组织机构管理逻辑的服务。
    /// </summary>
    /// <remarks> 替代原有的OrganizationManager 和 MerchantManager。这是一个范围服务。</remarks>
    public class OrgManager<TDbContext>
        where TDbContext : OwDbContext
    {
        #region 私有字段和构造函数
        private readonly IMemoryCache _MemoryCache;
        private readonly IDbContextFactory<TDbContext> _DbContextFactory;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="memoryCache">缓存服务。</param>
        /// <param name="dbContextFactory">数据库上下文工厂。</param>
        public OrgManager(IMemoryCache memoryCache, IDbContextFactory<TDbContext> dbContextFactory)
        {
            _MemoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }
        #endregion

        #region 主要查询方法
        /// <summary>
        /// 找到指定用户Id的商户Id。先从缓存中获取，如果没有就从数据库加载，找不到返回null。
        /// </summary>
        /// <remarks>在缓存中维护一个并发字典，从中找到需要的数据。</remarks>
        /// <param name="userId">用户ID。</param>
        /// <returns>用户所属的商户ID，未找到返回null。</returns>
        public Guid? GetMerchantIdByUserId(Guid userId)
        {
            var cacheKey = OwCacheExtensions.GetCacheKeyFromId(userId, "UserToMerchant.");

            // 先尝试从缓存获取
            if (_MemoryCache.TryGetValue(cacheKey, out Guid? cachedMerchantId))
            {
                return cachedMerchantId;
            }

            // 缓存中没有，从数据库查询
            using var dbContext = _DbContextFactory.CreateDbContext();
            var merchantId = ResolveMerchantIdByUserIdFromDatabaseCore(dbContext, userId);

            // 只有找到结果时才放入缓存，避免缓存null值
            if (merchantId.HasValue)
            {
                var entry = _MemoryCache.CreateEntry(cacheKey);
                ConfigureIdLookupCacheEntry(entry);
                entry.Value = merchantId;
                entry.Dispose(); // 提交到缓存
            }

            return merchantId;
        }

        /// <summary>
        /// 找到指定组织机构Id的商户Id。先从缓存中获取，如果没有就从数据库加载，找不到返回null。
        /// </summary>
        /// <remarks>缓存中维护一个并发字典，从中找到需要的数据。</remarks>
        /// <param name="orgId">机构/商户Id。若是商户Id则立即返回该Id。</param>
        /// <returns>机构所属的商户Id，未找到返回null。</returns>
        public Guid? GetMerchantIdByOrgId(Guid orgId)
        {
            var cacheKey = OwCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");

            // 先尝试从缓存获取
            if (_MemoryCache.TryGetValue(cacheKey, out Guid? cachedMerchantId))
            {
                return cachedMerchantId;
            }

            // 缓存中没有，从数据库查询
            using var dbContext = _DbContextFactory.CreateDbContext();
            var merchantId = ResolveMerchantIdFromDatabaseCore(dbContext, orgId);

            // 只有找到结果时才放入缓存，避免缓存null值
            if (merchantId.HasValue)
            {
                var entry = _MemoryCache.CreateEntry(cacheKey);
                ConfigureIdLookupCacheEntry(entry);
                entry.Value = merchantId;
                entry.Dispose(); // 提交到缓存
            }

            return merchantId;
        }

        /// <summary>
        /// 从缓存中获取或加载指定商户Id的商户和组织缓存项OrgCacheItem。
        /// </summary>
        /// <remarks>加载是要完整加载商户和所有相关的组织机构数据。避免未来使用其中对象属性时再次读取。</remarks>
        /// <param name="merchId">商户ID。</param>
        /// <returns>机构的缓存数据。</returns>
        public OrgCacheItem<TDbContext> GetOrLoadOrgCacheItem(Guid merchId)
        {
            var cacheKey = OwCacheExtensions.GetCacheKeyFromId(merchId, "OrgCacheItem.");

            return _MemoryCache.GetOrCreate(cacheKey, entry =>
            {
                var cacheItem = LoadOrgCacheItemFromDatabase(merchId);
                ConfigureOrgCacheEntry(entry, merchId);

                // 加载完成后，同时初始化 OrgToMerchant 缓存映射
                InitializeOrgToMerchantCache(cacheItem);

                return cacheItem;
            });
        }

        /// <summary>
        /// 获取指定机构Id所属公司的Id。包含自身。
        /// </summary>
        /// <param name="orgId">机构/公司Id，若本身就是公司则立即返回该值。</param>
        /// <returns>公司Id，未找到返回null。</returns>
        public Guid? GetCompanyIdByOrgId(Guid orgId)
        {
            var merchantId = GetMerchantIdByOrgId(orgId);
            if (!merchantId.HasValue)
                return null; // 未找到所属商户时返回null

            var cacheItem = GetOrLoadOrgCacheItem(merchantId.Value);

            // 如果传入的就是商户ID，查找第一个公司类型的组织
            if (cacheItem.Merchant.Id == orgId)
            {
                var firstCompany = cacheItem.Orgs.Values.FirstOrDefault(o => o.Otc == 2);
                return firstCompany?.Id; // 未找到公司时返回null
            }

            // 查找指定机构
            if (!cacheItem.Orgs.TryGetValue(orgId, out var targetOrg))
                return null; // 机构未找到时返回null

            // 如果本身就是公司，直接返回
            if (targetOrg.Otc == 2)
                return targetOrg.Id;

            // 向上查找到公司级别
            var current = targetOrg;
            while (current != null && current.Otc != 2)
            {
                if (current.ParentId.HasValue && cacheItem.Orgs.TryGetValue(current.ParentId.Value, out var parent))
                    current = parent;
                else
                    break;
            }

            return current?.Id; // 未找到公司时返回null
        }

        /// <summary>
        /// 指定机构Id获取与其同属一个公司的所有机构Id列表。
        /// </summary>
        /// <param name="orgId">指定的机构Id.</param>
        /// <returns>返回公司及下属机构的所有Id列表。</returns>
        public IReadOnlyList<Guid> GetOrgIdsByCompanyId(Guid orgId)
        {
            var companyId = GetCompanyIdByOrgId(orgId);
            if (!companyId.HasValue)
                return Array.Empty<Guid>(); // 未找到公司时返回空列表

            var merchantId = GetMerchantIdByOrgId(companyId.Value);
            if (!merchantId.HasValue)
                return Array.Empty<Guid>(); // 未找到商户时返回空列表

            var cacheItem = GetOrLoadOrgCacheItem(merchantId.Value);

            if (!cacheItem.Orgs.TryGetValue(companyId.Value, out var company))
                return Array.Empty<Guid>(); // 公司未找到时返回空列表

            // 优化：在树遍历时直接排除子公司，自然不会获取子公司的下属机构
            var result = new List<Guid> { companyId.Value };
            var subOrgs = OwHelper.GetAllSubItemsOfTree(new[] { company }, c => c.Children.Where(child => child.Otc != 2));

            // 添加所有非公司类型的下属机构
            result.AddRange(subOrgs.Where(o => o.Id != companyId.Value).Select(o => o.Id));

            return result.AsReadOnly();
        }

        /// <summary>
        /// 获取用户当前登录的公司对象。
        /// </summary>
        /// <param name="user">用户对象。</param>
        /// <returns>公司对象，未找到返回null。</returns>
        public PlOrganization GetCurrentCompanyByUser(Account user)
        {
            if (user.OrgId == null) return null;
            var companyId = GetCompanyIdByOrgId(user.OrgId.Value);
            if (!companyId.HasValue) return null;

            try
            {
                var merchantId = GetMerchantIdByOrgId(companyId.Value);
                if (!merchantId.HasValue) return null;

                var cacheItem = GetOrLoadOrgCacheItem(merchantId.Value);
                return cacheItem.Orgs.TryGetValue(companyId.Value, out var company) ? company : null;
            }
            catch
            {
                return null; // 缓存中未找到对应数据
            }
        }
        #endregion

        #region 缓存失效方法
        /// <summary>
        /// 使指定商户的所有相关缓存失效。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>成功失效的缓存数量。</returns>
        public int InvalidateOrgCaches(Guid merchantId)
        {
            var count = 0;
            var orgCacheKey = OwCacheExtensions.GetCacheKeyFromId(merchantId, "OrgCacheItem.");
            // 使用新API: 获取取消令牌源并取消
            var cts = _MemoryCache.GetCancellationTokenSource(orgCacheKey);
            if (cts != null && !cts.IsCancellationRequested)
            {
                try
                {
                    cts.Cancel();
                    count++;
                }
                catch { /* 忽略可能的异常 */ }
            }
            return count;
        }

        /// <summary>
        /// 使指定用户的商户查找缓存失效。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>是否成功失效。</returns>
        public bool InvalidateUserMerchantCache(Guid userId)
        {
            var cacheKey = OwCacheExtensions.GetCacheKeyFromId(userId, "UserToMerchant.");
            // 使用新API: 获取取消令牌源并取消
            var cts = _MemoryCache.GetCancellationTokenSource(cacheKey);
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
        /// 使指定组织的商户查找缓存失效。
        /// </summary>
        /// <param name="orgId">组织ID。</param>
        /// <returns>失效的缓存数量。</returns>
        public int InvalidateOrgMerchantCache(Guid orgId)
        {
            var count = 0;
            // 1. 首先失效单个映射缓存
            var orgCacheKey = OwCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");
            var cts = _MemoryCache.GetCancellationTokenSource(orgCacheKey);
            if (cts != null && !cts.IsCancellationRequested)
            {
                try
                {
                    cts.Cancel();
                    count++;
                }
                catch { /* 忽略可能的异常 */ }
            }
            // 2. 🔥 关键修复：如果组织结构发生变化，必须失效整个商户的主缓存
            // 因为主缓存中的组织列表可能已经过期
            using var dbContext = _DbContextFactory.CreateDbContext();
            var merchantId = ResolveMerchantIdFromDatabaseCore(dbContext, orgId);
            if (merchantId.HasValue)
            {
                // 失效整个商户的缓存，确保组织列表是最新的
                var mainCacheCount = InvalidateOrgCaches(merchantId.Value);
                count += mainCacheCount;
                // 记录日志以便调试
                System.Diagnostics.Debug.WriteLine($"组织 {orgId} 缓存失效导致商户 {merchantId} 的 {mainCacheCount} 个缓存项失效");
            }
            else
            {
                // 如果无法确定商户ID（比如组织已被删除），记录警告但不阻断流程
                System.Diagnostics.Debug.WriteLine($"无法确定组织 {orgId} 的商户ID进行缓存失效");
            }
            return count;
        }

        /// <summary>
        /// 批量使指定组织的商户查找缓存失效。适用于批量操作场景。
        /// </summary>
        /// <param name="orgIds">组织ID集合。</param>
        /// <returns>失效的缓存数量。</returns>
        public int InvalidateOrgMerchantCaches(IEnumerable<Guid> orgIds)
        {
            if (orgIds == null) return 0;
            var count = 0;
            var processedMerchants = new HashSet<Guid>(); // 避免重复处理同一商户
            using var dbContext = _DbContextFactory.CreateDbContext();
            foreach (var orgId in orgIds)
            {
                // 失效单个映射缓存
                var orgCacheKey = OwCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");
                var cts = _MemoryCache.GetCancellationTokenSource(orgCacheKey);
                if (cts != null && !cts.IsCancellationRequested)
                {
                    try
                    {
                        cts.Cancel();
                        count++;
                    }
                    catch { /* 忽略可能的异常 */ }
                }
                // 尝试获取商户ID并失效主缓存
                var merchantId = ResolveMerchantIdFromDatabaseCore(dbContext, orgId);
                if (merchantId.HasValue && processedMerchants.Add(merchantId.Value)) // 如果是第一次处理这个商户
                {
                    var mainCacheCount = InvalidateOrgCaches(merchantId.Value);
                    count += mainCacheCount;
                }
            }
            return count;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 配置组织缓存条目。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        /// <param name="merchantId">商户ID。</param>
        private void ConfigureOrgCacheEntry(ICacheEntry entry, Guid merchantId)
        {
            // 设置缓存过期策略
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(12));
            entry.SetPriority(CacheItemPriority.Normal);
            // 启用优先级驱逐回调
            entry.EnablePriorityEvictionCallback(_MemoryCache);
        }

        /// <summary>
        /// 配置ID查找缓存条目。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        private void ConfigureIdLookupCacheEntry(ICacheEntry entry)
        {
            // ID查找缓存过期时间较短，因为数据相对简单
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(6));
            entry.SetPriority(CacheItemPriority.Low);

            // 启用优先级驱逐回调
            entry.EnablePriorityEvictionCallback(_MemoryCache);
        }

        /// <summary>
        /// 配置ID查找缓存选项。(已废弃,不再使用)
        /// </summary>
        /// <param name="options">缓存选项。</param>
        [Obsolete("不再使用此方法,请使用 ConfigureIdLookupCacheEntry(ICacheEntry) 代替")]
        private void ConfigureIdLookupCacheEntry(MemoryCacheEntryOptions options)
        {
            // 此方法已废弃,保留仅为兼容性
        }
        #endregion

        #region 新增或更改的代码

        /// <summary>
        /// 初始化 OrgToMerchant 缓存映射，避免后续再次查询数据库。
        /// </summary>
        /// <param name="cacheItem">已加载的组织缓存项。</param>
        private void InitializeOrgToMerchantCache(OrgCacheItem<TDbContext> cacheItem)
        {
            if (cacheItem?.Merchant == null) return;

            var merchantId = cacheItem.Merchant.Id;

            // 为商户ID本身建立映射缓存
            var merchantCacheKey = OwCacheExtensions.GetCacheKeyFromId(merchantId, "OrgToMerchant.");
            var merchantEntry = _MemoryCache.CreateEntry(merchantCacheKey);
            ConfigureIdLookupCacheEntry(merchantEntry);
            merchantEntry.Value = (Guid?)merchantId;
            merchantEntry.Dispose();

            // 为所有组织机构建立到商户的映射缓存
            if (cacheItem.Orgs != null)
            {
                // 获取主缓存的取消令牌源，用于建立依赖关系
                var mainCacheKey = OwCacheExtensions.GetCacheKeyFromId(merchantId, "OrgCacheItem.");
                var mainTokenSource = _MemoryCache.GetCancellationTokenSource(mainCacheKey);

                foreach (var orgId in cacheItem.Orgs.Keys)
                {
                    var orgCacheKey = OwCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");

                    var orgEntry = _MemoryCache.CreateEntry(orgCacheKey);
                    ConfigureIdLookupCacheEntry(orgEntry);

                    // 添加对主缓存的依赖，当主缓存失效时这些映射也失效
                    if (mainTokenSource != null)
                    {
                        try
                        {
                            // 真正可能抛异常的地方：访问已释放的 CTS 的 Token 属性
                            orgEntry.AddExpirationToken(new CancellationChangeToken(mainTokenSource.Token));
                        }
                        catch (ObjectDisposedException)
                        {
                            // 取消令牌源在添加依赖时已被释放，跳过依赖关系建立
                            // 缓存项仍会正常工作，只是失去级联过期功能
                        }
                    }

                    orgEntry.Value = (Guid?)merchantId;
                    orgEntry.Dispose();
                }
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 从数据库解析用户的商户ID。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>商户ID。</returns>
        private Guid ResolveMerchantIdByUserIdFromDatabase(Guid userId)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();

            // 使用泛型方法访问AccountPlOrganizations实体
            var userOrg = dbContext.Set<AccountPlOrganization>()
                  .AsNoTracking()
          .Where(c => c.UserId == userId)
              .FirstOrDefault();

            if (userOrg == null)
                throw new InvalidOperationException($"用户 {userId} 未找到所属组织机构");

            var merchantId = ResolveMerchantIdFromDatabaseCore(dbContext, userOrg.OrgId);
            if (!merchantId.HasValue)
                throw new InvalidOperationException($"用户 {userId} 所属组织机构 {userOrg.OrgId} 未找到关联的商户");

            return merchantId.Value;
        }

        /// <summary>
        /// 从数据库解析用户的商户ID的核心逻辑。
        /// </summary>
        /// <param name="dbContext">数据库上下文。</param>
        /// <param name="userId">用户ID。</param>
        /// <returns>商户ID，未找到返回null。</returns>
        private static Guid? ResolveMerchantIdByUserIdFromDatabaseCore(TDbContext dbContext, Guid userId)
        {
            // 使用泛型方法访问AccountPlOrganizations实体
            var userOrg = dbContext.Set<AccountPlOrganization>()
               .AsNoTracking()
                .Where(c => c.UserId == userId)
                 .FirstOrDefault();

            if (userOrg == null)
                return null; // 用户未找到所属组织机构

            return ResolveMerchantIdFromDatabaseCore(dbContext, userOrg.OrgId);
        }

        /// <summary>
        /// 从数据库解析商户ID的核心逻辑。
        /// </summary>
        /// <param name="dbContext">数据库上下文。</param>
        /// <param name="orgId">机构或商户ID。</param>
        /// <returns>商户ID，未找到返回null。</returns>
        private static Guid? ResolveMerchantIdFromDatabaseCore(TDbContext dbContext, Guid orgId)
        {
            // 首先尝试作为商户ID查找，使用Set<T>()方法
            var merchant = dbContext.Set<PlMerchant>().Find(orgId);
            if (merchant is not null)
                return merchant.Id;

            // 然后尝试作为组织机构ID查找，递归向上查找商户
            var org = dbContext.Set<PlOrganization>().Find(orgId);
            while (org != null)
            {
                if (org.ParentId is null) // 到达顶层组织
                {
                    if (org.MerchantId.HasValue)
                        return org.MerchantId.Value;
                    // 顶层组织未关联商户，这是数据不一致的情况，返回null而不抛异常
                    return null;
                }
                org = dbContext.Set<PlOrganization>().Find(org.ParentId);
            }

            // 未找到对应的商户或组织机构，返回null
            return null;
        }

        /// <summary>
        /// 从数据库加载完整的商户和组织缓存项。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>完整的缓存项。</returns>
        private OrgCacheItem<TDbContext> LoadOrgCacheItemFromDatabase(Guid merchantId)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();

            // ✅ 移除 AsNoTracking,让EF Core跟踪实体并自动加载导航属性
            var merchant = dbContext.Set<PlMerchant>()
                .FirstOrDefault(c => c.Id == merchantId);
            if (merchant is null)
                throw new InvalidOperationException($"商户 {merchantId} 未找到");

            // ✅ 查询根组织机构(ParentId为null)
            var rootOrgs = dbContext.Set<PlOrganization>()
                .Where(c => c.MerchantId == merchantId && c.ParentId == null)
                .ToList(); // ✅ 立即执行查询

            // ✅ EF Core会自动加载Children导航属性(延迟加载或显式加载)
            // DbContext在using结束时自动释放,Children已经加载完毕
            var orgsDict = new ConcurrentDictionary<Guid, PlOrganization>();
            if (rootOrgs.Any())
            {
                // ✅ GetAllSubItemsOfTree遍历时,Children已经通过EF Core加载
                var allOrgsFlat = rootOrgs.SelectMany(c => OwHelper.GetAllSubItemsOfTree(new[] { c }, d => d.Children));
                foreach (var org in allOrgsFlat)
                    orgsDict.TryAdd(org.Id, org);
            }

            // ✅ 返回纯数据对象，DbContext 在 using 结束时自动释放
            // 即使实体被跟踪,DbContext释放后也不影响已加载的数据使用
            return new OrgCacheItem<TDbContext>
            {
                Merchant = merchant,
                _Orgs = orgsDict
            };
        }

        /// <summary>
        /// 确保导航属性被正确加载。(已废弃，不再需要)
        /// </summary>
        /// <param name="orgs">组织机构集合。</param>
        [Obsolete("不再需要此方法，导航属性已在加载时通过 Include 预加载")]
        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void EnsureNavigationPropertiesLoaded(IEnumerable<PlOrganization> orgs)
        {
            // 此方法已废弃，保留仅为兼容性
        }
        #endregion
    }

    /// <summary>
    /// 注册 <see cref="OrgManager{TDbContext}"/> 服务到服务集合中。
    /// </summary>
    public static class OrgManagerExtensions
    {
        /// <summary>
        /// 注册 OrgManager 服务。
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文类型。</typeparam>
        /// <param name="services">服务集合。</param>
        /// <returns>服务集合。</returns>
        public static IServiceCollection AddOrgManager<TDbContext>(this IServiceCollection services)
      where TDbContext : OwDbContext
        {
            return services.AddScoped<OrgManager<TDbContext>>();
        }
    }
}
