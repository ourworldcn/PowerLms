using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Linq;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 商户功能管理服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class MerchantManager
    {
        #region 私有字段
        private readonly IMemoryCache _MemoryCache;
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private readonly PowerLmsUserDbContext _DbContext;
        #endregion 私有字段

        #region 构造函数
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="memoryCache">内存缓存。</param>
        /// <param name="dbContextFactory">数据库上下文工厂。</param>
        /// <param name="dbContext">数据库上下文。</param>
        public MerchantManager(IMemoryCache memoryCache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, PowerLmsUserDbContext dbContext)
        {
            _MemoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        #endregion 构造函数

        #region 缓存商户信息及相关
        /// <summary>
        /// 根据指定Id加载商户对象。
        /// </summary>
        /// <param name="merchId">商户ID。</param>
        /// <param name="dbContext">传递null，则用池自动获取一个数据库上下文。</param>
        /// <returns>没有找到则返回null。</returns>
        public PlMerchant LoadById(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            dbContext ??= _DbContextFactory.CreateDbContext();
            PlMerchant result;
            lock (dbContext)
                result = dbContext.Merchants.FirstOrDefault(c => c.Id == merchId);
            Loaded(result, dbContext);
            return result;
        }

        /// <summary>
        /// 设置商户对象的数据库上下文。
        /// </summary>
        /// <param name="merch">商户对象。</param>
        /// <param name="dbContext">数据库上下文。</param>
        private void Loaded(PlMerchant merch, PowerLmsUserDbContext dbContext)
        {
            if (merch != null)  // 添加空检查以避免NullReferenceException
                merch.DbContext = dbContext;
        }

        /// <summary>
        /// 获取指定id的商户对象。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>商户对象，若没有找到，则返回null。</returns>
        public PlMerchant GetById(Guid merchantId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<PlMerchant>(merchantId);
            return _MemoryCache.TryGetValue(cacheKey, out PlMerchant merchant) ? merchant : null;
        }

        /// <summary>
        /// 使指定ID的商户缓存失效。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateCache(Guid merchantId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<PlMerchant>(merchantId);
            return _MemoryCache.CancelSource(cacheKey);
        }

        /// <summary>
        /// 获取商户缓存的取消令牌源。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>取消令牌源，如果不存在则返回null</returns>
        public CancellationTokenSource GetCacheTokenSource(Guid merchantId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<PlMerchant>(merchantId);
            return _MemoryCache.GetCancellationTokenSource(cacheKey);
        }

        /// <summary>
        /// 加载或获取缓存的商户对象。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>商户对象。</returns>
        public PlMerchant GetOrLoadById(Guid merchantId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<PlMerchant>(merchantId);

            return _MemoryCache.GetOrCreate(cacheKey, entry =>
            {
                PowerLmsUserDbContext db = null;
                var merchant = LoadById(merchantId, ref db);

                // 配置缓存条目
                ConfigureMerchantCacheEntry(entry, merchant);

                return merchant;
            });
        }

        /// <summary>
        /// 配置商户缓存条目
        /// </summary>
        /// <param name="entry">缓存条目</param>
        /// <param name="merchant">商户对象</param>
        private void ConfigureMerchantCacheEntry(ICacheEntry entry, PlMerchant merchant)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(30));

            // 设置绝对过期时间
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(24));

            // 设置优先级
            entry.SetPriority(CacheItemPriority.Normal);

            // 使用OwMemoryCacheExtensions注册取消令牌
            entry.RegisterCancellationToken(_MemoryCache);
        }

        /// <summary>
        /// 按 OrgId 获取或加载 PlMerchant。
        /// </summary>
        /// <param name="orgId">组织ID。</param>
        /// <returns>商户对象。</returns>
        public PlMerchant GetOrLoadByOrgId(Guid orgId)
        {
            if (TryGetIdByOrgOrMerchantId(orgId, out var merchantId) && merchantId.HasValue)
            {
                return GetOrLoadById(merchantId.Value);
            }
            return null;
        }
        #endregion 缓存商户信息及相关

        #region 用户相关
        /// <summary>
        /// 从指定的用户对象获取其商户信息。
        /// </summary>
        /// <param name="user">用户对象。</param>
        /// <returns>对于不属于商户的账号返回null。</returns>
        public PlMerchant GetOrLoadByUser(Account user)
        {
            var merchId = user.MerchantId;
            if (merchId is null)    //若未缓存商户Id
            {
                if (!GetIdByUserId(user.Id, out merchId)) return null;
                user.MerchantId = merchId;
            }
            return merchId.HasValue ? GetOrLoadById(merchId.Value) : null;
        }

        /// <summary>
        /// 获取指定账户所属的商户Id。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <param name="MerchantId">商户ID。</param>
        /// <returns>true指定账户所属商户Id。如果不属于任何商户则返回null。false 没有找到指定的用户Id。</returns>
        public bool GetIdByUserId(Guid userId, out Guid? MerchantId)
        {
            var userOrg = _DbContext.AccountPlOrganizations.AsNoTracking().FirstOrDefault(c => c.UserId == userId);    //随机找到一个所属的组织机构
            if (userOrg == null)
            {
                MerchantId = null;
                return false;
            }
            return TryGetIdByOrgOrMerchantId(userOrg.OrgId, out MerchantId);
        }

        /// <summary>
        /// 从数据库中取指定组织机构Id或商户Id所属的商户Id。
        /// </summary>
        /// <param name="id">机构Id或商户Id。</param>
        /// <param name="merchantId">商户Id。</param>
        /// <param name="dbContext">可选参数，指定使用的数据库上下文。</param>
        /// <returns>true则找到了商户Id。false没有找到。</returns>
        public bool TryGetIdByOrgOrMerchantId(Guid id, out Guid? merchantId, PowerLmsUserDbContext dbContext = null)
        {
            var localContext = dbContext == null;
            dbContext ??= _DbContextFactory.CreateDbContext(); // 如果没有传递dbContext，则创建一个临时数据库上下文

            // 初始化商户Id为null
            merchantId = null;

            try
            {
                // 尝试直接使用id查找商户对象
                var merchant = dbContext.Merchants.Find(id);
                if (merchant != null)
                {
                    merchantId = merchant.Id;
                    return true;
                }

                // 尝试使用id查找机构对象并递归查找其父对象
                var org = dbContext.PlOrganizations.Find(id);
                while (org != null)
                {
                    if (org.ParentId is null)
                    {
                        merchantId = org.MerchantId;
                        return merchantId.HasValue;
                    }
                    org = dbContext.PlOrganizations.Find(org.ParentId);
                }

                return false;
            }
            finally
            {
                if (localContext)
                {
                    dbContext.Dispose();
                }
            }
        }

        /// <summary>
        /// 根据角色Id获取商户Id。
        /// </summary>
        /// <param name="roleId">角色Id。</param>
        /// <param name="merchantId">商户Id。</param>
        /// <returns>true则找到了商户Id。false没有找到。</returns>
        public bool GetIdByRoleId(Guid roleId, out Guid? merchantId)
        {
            if (_DbContext.PlRoles.Find(roleId) is not PlRole role || !role.OrgId.HasValue)
            {
                merchantId = null;
                return false;
            }
            return TryGetIdByOrgOrMerchantId(role.OrgId.Value, out merchantId);
        }
        #endregion 用户相关

        /// <summary>
        /// 获取指定商户的缓存项。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>缓存项，如果不存在则返回null。</returns>
        public PlMerchant GetCacheItemById(Guid merchantId)
        {
            var cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<PlMerchant>(merchantId);
            return _MemoryCache.TryGetValue<PlMerchant>(cacheKey, out var result) ? result : null;
        }
    }
}
