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

        /// <summary>
        /// 获取或设置数据库上下文。仅查询和加载数据时使用。
        /// </summary>
        public TDbContext DbContext { get; internal set; }
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
        /// 找到指定用户Id的商户Id。
        /// </summary>
        /// <remarks>在缓存中维护一个并发字典，从中找到需要的数据。</remarks>
        /// <param name="userId">用户ID。</param>
        /// <returns>用户所属的商户ID。</returns>
        public Guid GetMerchantIdByUserId(Guid userId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(userId, "UserToMerchant.");
            
            return _MemoryCache.GetOrCreate(cacheKey, entry =>
            {
                ConfigureIdLookupCacheEntry(entry);
                return ResolveMerchantIdByUserIdFromDatabase(userId);
            });
        }

        /// <summary>
        /// 找到指定组织机构Id的商户Id。
        /// </summary>
        /// <remarks>缓存中维护一个并发字典，从中找到需要的数据。</remarks>
        /// <param name="orgId">机构/商户Id。若是商户Id则立即返回该Id。</param>
        /// <returns>机构所属的商户Id。</returns>
        public Guid GetMerchantIdByOrgId(Guid orgId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");
            
            return _MemoryCache.GetOrCreate(cacheKey, entry =>
            {
                ConfigureIdLookupCacheEntry(entry);
                return ResolveMerchantIdFromDatabase(orgId);
            });
        }

        /// <summary>
        /// 从缓存中获取或加载指定商户Id的商户和组织缓存项OrgCacheItem。
        /// </summary>
        /// <remarks>加载是要完整加载商户和所有相关的组织机构数据。避免未来使用其中对象属性时再次读取。</remarks>
        /// <param name="merchId">商户ID。</param>
        /// <returns>机构的缓存数据。</returns>
        public OrgCacheItem<TDbContext> GetOrLoadOrgCacheItem(Guid merchId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchId, "OrgCacheItem.");
            
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
        /// 获取指定机构Id所属公司的Id列表。包含自身。
        /// </summary>
        /// <param name="orgId">机构/公司Id，若本身就是公司则立即返回该值。</param>
        /// <returns>公司Id。</returns>
        public Guid GetCompanyIdByOrgId(Guid orgId)
        {
            var merchantId = GetMerchantIdByOrgId(orgId);
            var cacheItem = GetOrLoadOrgCacheItem(merchantId);
            
            // 如果传入的就是商户ID，查找第一个公司类型的组织
            if (cacheItem.Merchant.Id == orgId)
            {
                var firstCompany = cacheItem.Orgs.Values.FirstOrDefault(o => o.Otc == 2);
                return firstCompany?.Id ?? throw new InvalidOperationException($"商户 {merchantId} 下未找到公司类型的组织机构");
            }

            // 查找指定机构
            if (!cacheItem.Orgs.TryGetValue(orgId, out var targetOrg))
                throw new InvalidOperationException($"组织机构 {orgId} 未找到");

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

            return current?.Id ?? throw new InvalidOperationException($"组织机构 {orgId} 未找到所属公司");
        }

        /// <summary>
        /// 指定机构Id获取与其同属一个公司的所有机构Id列表。
        /// </summary>
        /// <param name="orgId">指定的机构Id.</param>
        /// <returns>返回公司及下属机构的所有Id列表。</returns>
        public IReadOnlyList<Guid> GetOrgIdsByCompanyId(Guid orgId)
        {
            var companyId = GetCompanyIdByOrgId(orgId);
            var merchantId = GetMerchantIdByOrgId(companyId);
            var cacheItem = GetOrLoadOrgCacheItem(merchantId);

            if (!cacheItem.Orgs.TryGetValue(companyId, out var company))
                throw new InvalidOperationException($"公司 {companyId} 未找到");

            // 获取公司及其所有下属机构（但排除其他子公司）
            var result = new List<Guid> { companyId };
            var subOrgs = OwHelper.GetAllSubItemsOfTree(new[] { company }, c => c.Children);
            
            foreach (var subOrg in subOrgs.Where(o => o.Id != companyId))
            {
                // 排除其他子公司及其下属机构
                if (subOrg.Otc == 2) continue; // 跳过子公司本身
                
                // 检查是否是其他子公司的下属机构
                var isUnderOtherCompany = false;
                var current = subOrg;
                while (current.ParentId.HasValue && cacheItem.Orgs.TryGetValue(current.ParentId.Value, out var parent))
                {
                    if (parent.Otc == 2 && parent.Id != companyId)
                    {
                        isUnderOtherCompany = true;
                        break;
                    }
                    current = parent;
                }
                
                if (!isUnderOtherCompany)
                    result.Add(subOrg.Id);
            }

            return result.AsReadOnly();
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
            
            // 获取缓存项以便清理相关的ID映射缓存
            var orgCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchantId, "OrgCacheItem.");
            if (_MemoryCache.TryGetValue(orgCacheKey, out OrgCacheItem<TDbContext> cacheItem))
            {
                // 清理所有相关的 OrgToMerchant 缓存
                var orgIds = new List<Guid> { merchantId }; // 包含商户ID本身
                if (cacheItem.Orgs != null)
                    orgIds.AddRange(cacheItem.Orgs.Keys);
                
                foreach (var orgId in orgIds)
                {
                    var idCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");
                    if (_MemoryCache.CancelSource(idCacheKey))
                        count++;
                }
            }
            
            // 使主缓存失效
            if (_MemoryCache.CancelSource(orgCacheKey))
                count++;
            
            return count;
        }

        /// <summary>
        /// 使指定用户的商户查找缓存失效。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>是否成功失效。</returns>
        public bool InvalidateUserMerchantCache(Guid userId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(userId, "UserToMerchant.");
            return _MemoryCache.CancelSource(cacheKey);
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
            var orgCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");
            if (_MemoryCache.CancelSource(orgCacheKey))
                count++;
            
            // 2. 🔥 关键修复：如果组织结构发生变化，必须失效整个商户的主缓存
            // 因为主缓存中的组织列表可能已经过期
            try
            {
                // 尝试从数据库获取该组织所属的商户ID
                var merchantId = ResolveMerchantIdFromDatabase(orgId);
                
                // 失效整个商户的缓存，确保组织列表是最新的
                var mainCacheCount = InvalidateOrgCaches(merchantId);
                count += mainCacheCount;
                
                // 记录日志以便调试
                // 注意：在生产环境中可以移除或改为Debug级别
                System.Diagnostics.Debug.WriteLine($"组织 {orgId} 缓存失效导致商户 {merchantId} 的 {mainCacheCount} 个缓存项失效");
            }
            catch (Exception ex)
            {
                // 如果无法确定商户ID（比如组织已被删除），记录警告但不阻断流程
                System.Diagnostics.Debug.WriteLine($"无法确定组织 {orgId} 的商户ID进行缓存失效: {ex.Message}");
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
            
            foreach (var orgId in orgIds)
            {
                // 失效单个映射缓存
                var orgCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");
                if (_MemoryCache.CancelSource(orgCacheKey))
                    count++;
                
                // 尝试获取商户ID并失效主缓存
                try
                {
                    var merchantId = ResolveMerchantIdFromDatabase(orgId);
                    if (processedMerchants.Add(merchantId)) // 如果是第一次处理这个商户
                    {
                        var mainCacheCount = InvalidateOrgCaches(merchantId);
                        count += mainCacheCount;
                    }
                }
                catch
                {
                    // 忽略单个组织的失败，继续处理其他组织
                }
            }
            
            return count;
        }
        #endregion

        #region 兼容性适配方法
        /// <summary>
        /// 获取指定商户下所有组织机构ID列表。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>组织机构ID集合。</returns>
        public IEnumerable<Guid> GetOrgIdsByMerchantId(Guid merchantId)
        {
            var cacheItem = GetOrLoadOrgCacheItem(merchantId);
            return cacheItem.Orgs.Keys;
        }

        /// <summary>
        /// 获取指定商户下所有组织机构的字典。适配原有接口。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>组织机构字典。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> GetOrLoadByMerchantId(Guid merchantId)
        {
            var cacheItem = GetOrLoadOrgCacheItem(merchantId);
            return new ConcurrentDictionary<Guid, PlOrganization>(cacheItem.Orgs);
        }

        /// <summary>
        /// 通过用户获取其所属商户对象。算法：获取user.OrgId所属商户。
        /// </summary>
        /// <param name="user">用户对象。</param>
        /// <returns>商户对象，未找到返回null。</returns>
        public PlMerchant GetMerchantByUser(Account user)
        {
            try
            {
                var merchantId = GetMerchantIdByUserId(user.Id);
                var cacheItem = GetOrLoadOrgCacheItem(merchantId);
                return cacheItem.Merchant;
            }
            catch
            {
                return null; // 用户未关联有效商户时返回null
            }
        }

        /// <summary>
        /// 安全版本的GetMerchantIdByOrgId，未找到时返回null而非抛出异常。
        /// </summary>
        /// <param name="orgId">组织或商户ID。</param>
        /// <returns>商户ID，未找到返回null。</returns>
        public Guid? GetMerchantIdByOrgIdOrNull(Guid orgId)
        {
            try
            {
                return GetMerchantIdByOrgId(orgId);
            }
            catch
            {
                return null; // 未找到时返回null
            }
        }

        /// <summary>
        /// TryGet模式的商户ID查找方法，兼容原有接口。
        /// </summary>
        /// <param name="orgId">组织或商户ID。</param>
        /// <param name="merchantId">输出的商户ID。</param>
        /// <returns>是否找到。</returns>
        public bool TryGetMerchantIdByOrgId(Guid orgId, out Guid? merchantId)
        {
            merchantId = GetMerchantIdByOrgIdOrNull(orgId);
            return merchantId.HasValue;
        }

        /// <summary>
        /// 获取用户当前登录公司的ID。
        /// </summary>
        /// <param name="user">用户对象。</param>
        /// <returns>公司ID，未找到返回null。</returns>
        public Guid? GetCurrentCompanyIdByUser(Account user)
        {
            if (user.OrgId == null) return null;
            try
            {
                return GetCompanyIdByOrgId(user.OrgId.Value);
            }
            catch
            {
                return null; // 用户未登录到有效机构
            }
        }

        /// <summary>
        /// 获取用户当前登录的公司对象。适配原有接口。
        /// </summary>
        /// <param name="user">用户对象。</param>
        /// <returns>公司对象，未找到返回null。</returns>
        public PlOrganization GetCurrentCompanyByUser(Account user)
        {
            var companyId = GetCurrentCompanyIdByUser(user);
            if (!companyId.HasValue) return null;

            try
            {
                var merchantId = GetMerchantIdByOrgId(companyId.Value);
                var cacheItem = GetOrLoadOrgCacheItem(merchantId);
                return cacheItem.Orgs[companyId.Value];
            }
            catch
            {
                return null; // 缓存中未找到对应数据
            }
        }

        /// <summary>
        /// 兼容原有的缓存失效方法名。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>是否成功失效。</returns>
        public bool InvalidateOrgCache(Guid merchantId)
        {
            return InvalidateOrgCaches(merchantId) > 0;
        }

        /// <summary>
        /// 兼容原有的商户缓存失效方法名。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>是否成功失效。</returns>
        public bool InvalidateCache(Guid merchantId)
        {
            return InvalidateOrgCaches(merchantId) > 0;
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

            return ResolveMerchantIdFromDatabaseCore(dbContext, userOrg.OrgId);
        }

        /// <summary>
        /// 从数据库解析商户ID。
        /// </summary>
        /// <param name="orgId">机构或商户ID。</param>
        /// <returns>商户ID。</returns>
        private Guid ResolveMerchantIdFromDatabase(Guid orgId)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            return ResolveMerchantIdFromDatabaseCore(dbContext, orgId);
        }

        /// <summary>
        /// 从数据库解析商户ID的核心逻辑。
        /// </summary>
        /// <param name="dbContext">数据库上下文。</param>
        /// <param name="orgId">机构或商户ID。</param>
        /// <returns>商户ID。</returns>
        private static Guid ResolveMerchantIdFromDatabaseCore(TDbContext dbContext, Guid orgId)
        {
            // 首先尝试作为商户ID查找，使用Set<T>()方法
            var merchant = dbContext.Set<PlMerchant>().Find(orgId);
            if (merchant != null)
                return merchant.Id;

            // 然后尝试作为组织机构ID查找，递归向上查找商户
            var org = dbContext.Set<PlOrganization>().Find(orgId);
            while (org != null)
            {
                if (org.ParentId is null) // 到达顶层组织
                {
                    if (org.MerchantId.HasValue)
                        return org.MerchantId.Value;
                    throw new InvalidOperationException($"顶层组织机构 {org.Id} 未关联商户");
                }
                org = dbContext.Set<PlOrganization>().Find(org.ParentId);
            }

            throw new InvalidOperationException($"未找到ID {orgId} 对应的商户或组织机构");
        }

        /// <summary>
        /// 从数据库加载完整的商户和组织缓存项。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>完整的缓存项。</returns>
        private OrgCacheItem<TDbContext> LoadOrgCacheItemFromDatabase(Guid merchantId)
        {
            var dbContext = _DbContextFactory.CreateDbContext();
            
            try
            {
                // 加载商户信息，使用Set<T>()方法
                var merchant = dbContext.Set<PlMerchant>().FirstOrDefault(c => c.Id == merchantId);
                if (merchant == null)
                    throw new InvalidOperationException($"商户 {merchantId} 未找到");

                // 加载该商户下的所有组织机构，使用Include预加载导航属性提高性能
                var rootOrgs = dbContext.Set<PlOrganization>()
                    .Where(c => c.MerchantId == merchantId && c.ParentId == null)
                    .Include(c => c.Parent)
                    .Include(c => c.Children)
                    .AsEnumerable();

                var orgsDict = new ConcurrentDictionary<Guid, PlOrganization>();
                if (rootOrgs.Any())
                {
                    // 获取所有子孙组织机构
                    var allOrgs = rootOrgs.SelectMany(c => OwHelper.GetAllSubItemsOfTree(new[] { c }, d => d.Children));
                    foreach (var org in allOrgs)
                        orgsDict.TryAdd(org.Id, org);

                    // 确保导航属性被正确加载
                    EnsureNavigationPropertiesLoaded(orgsDict.Values);
                }

                return new OrgCacheItem<TDbContext>
                {
                    Merchant = merchant,
                    _Orgs = orgsDict,
                    DbContext = dbContext
                };
            }
            catch
            {
                dbContext?.Dispose(); // 出现异常时释放资源
                throw;
            }
        }

        /// <summary>
        /// 初始化 OrgToMerchant 缓存映射，避免后续再次查询数据库。
        /// </summary>
        /// <param name="cacheItem">已加载的组织缓存项。</param>
        private void InitializeOrgToMerchantCache(OrgCacheItem<TDbContext> cacheItem)
        {
            if (cacheItem?.Merchant == null) return;

            var merchantId = cacheItem.Merchant.Id;
            
            // 为商户ID本身建立映射缓存
            var merchantCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchantId, "OrgToMerchant.");
            var merchantOptions = new MemoryCacheEntryOptions();
            ConfigureIdLookupCacheEntry(merchantOptions);
            _MemoryCache.Set(merchantCacheKey, merchantId, merchantOptions);

            // 为所有组织机构建立到商户的映射缓存
            if (cacheItem.Orgs != null)
            {
                // 获取主缓存的取消令牌源，用于建立依赖关系
                var mainCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(merchantId, "OrgCacheItem.");
                var mainTokenSource = _MemoryCache.GetCancellationTokenSource(mainCacheKey);
                
                foreach (var orgId in cacheItem.Orgs.Keys)
                {
                    var orgCacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId(orgId, "OrgToMerchant.");
                    
                    var orgOptions = new MemoryCacheEntryOptions();
                    ConfigureIdLookupCacheEntry(orgOptions);
                    
                    // 添加对主缓存的依赖，当主缓存失效时这些映射也失效
                    if (mainTokenSource != null)
                    {
                        orgOptions.AddExpirationToken(new CancellationChangeToken(mainTokenSource.Token));
                    }
                    
                    _MemoryCache.Set(orgCacheKey, merchantId, orgOptions);
                }
            }
        }

        /// <summary>
        /// 确保导航属性被正确加载。
        /// </summary>
        /// <param name="orgs">组织机构集合。</param>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void EnsureNavigationPropertiesLoaded(IEnumerable<PlOrganization> orgs)
        {
            foreach (var org in orgs)
            {
                var _ = org.Parent; // 触发Parent属性的延迟加载
            }
        }

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

            // 注册取消令牌以支持主动失效
            entry.RegisterCancellationToken(_MemoryCache);

            // 注册后置逐出回调，用于清理资源
            entry.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (value is OrgCacheItem<TDbContext> cacheItem && cacheItem.DbContext != null)
                {
                    try
                    {
                        cacheItem.DbContext.Dispose(); // 释放数据库上下文
                    }
                    catch { /* 忽略释放时的异常 */ }
                }
            });
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

            // 注册取消令牌以支持主动失效
            entry.RegisterCancellationToken(_MemoryCache);
        }

        /// <summary>
        /// 配置ID查找缓存选项。
        /// </summary>
        /// <param name="options">缓存选项。</param>
        private void ConfigureIdLookupCacheEntry(MemoryCacheEntryOptions options)
        {
            // ID查找缓存过期时间较短，因为数据相对简单
            options.SetSlidingExpiration(TimeSpan.FromMinutes(15));
            options.SetAbsoluteExpiration(TimeSpan.FromHours(6));
            options.SetPriority(CacheItemPriority.Low);

            // 注册取消令牌以支持主动失效
            options.RegisterCancellationToken(_MemoryCache, Guid.NewGuid());
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
