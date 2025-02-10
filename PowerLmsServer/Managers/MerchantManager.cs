using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Linq;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 商户功能管理服务。
    /// 作者: OW
    /// 修改日期: 2025年2月10日。
    /// 创建日期: 2025年2月10日。
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
            merch.DbContext = dbContext;
        }

        /// <summary>
        /// 获取指定id的商户对象缓存。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>商户对象，若没有找到，则返回null。</returns>
        public OwCacheItem<PlMerchant> GetCacheItemById(Guid merchantId)
        {
            return _MemoryCache.Get<OwCacheItem<PlMerchant>>(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(merchantId));
        }

        /// <summary>
        /// 加载或获取缓存的商户对象。
        /// </summary>
        /// <param name="merchantId">商户ID。</param>
        /// <returns>商户对象缓存项。</returns>
        public OwCacheItem<PlMerchant> GetOrLoadById(Guid merchantId)
        {
            return _MemoryCache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(merchantId), c =>
            {
                PowerLmsUserDbContext db = null;
                var r = LoadById(OwCacheHelper.GetIdFromCacheKey<PlMerchant>(c.Key as string).Value, ref db);
                var item = new OwCacheItem<PlMerchant>
                {
                    Data = r,
                };
                item.SetCancellations(new CancellationTokenSource());
                return item;
            });
        }

        /// <summary>
        /// 按 OrgId 获取或加载 PlMerchant 的缓存项。
        /// </summary>
        /// <param name="orgId">组织ID。</param>
        /// <returns>商户对象缓存项。</returns>
        public OwCacheItem<PlMerchant> GetOrLoadByOrgId(Guid orgId)
        {
            if (TryGetIdByOrgOrMerchantId(orgId, out var merchantId))
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
        public OwCacheItem<PlMerchant> GetOrLoadByUser(Account user)
        {
            var merchId = user.MerchantId;
            if (merchId is null)    //若未缓存商户Id
            {
                if (!GetIdByUserId(user.Id, out merchId)) return null;
                user.MerchantId = merchId;
            }
            return GetOrLoadById(merchId.Value);
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
                        return true;
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
    }
}
