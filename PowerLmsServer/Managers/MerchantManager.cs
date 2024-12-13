using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Functions;
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
    /// 商户功能管理服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class MerchantManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public MerchantManager(IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, PowerLmsUserDbContext dbContext)
        {
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            _DbContext = dbContext;
        }

        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        readonly PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 根据指定Id加载商户对象。
        /// </summary>
        /// <param name="merchId"></param>
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
        /// 
        /// </summary>
        /// <param name="merch"></param>
        /// <param name="dbContext"></param>
        private void Loaded(PlMerchant merch, PowerLmsUserDbContext dbContext)
        {
            merch.DbContext = dbContext;
            merch.ExpirationTokenSource = new CancellationTokenSource();

        }

        /// <summary>
        /// 获取指定id的商户对象缓存。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns>商户对象，若没有找到，则返回null。</returns>
        public OwCacheItem<PlMerchant> GetCacheItemById(Guid merchantId)
        {
            var result = _Cache.Get<OwCacheItem<PlMerchant>>(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(merchantId));
            return result;
        }

        /// <summary>
        /// 加载或获取缓存的商户对象。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns></returns>
        public OwCacheItem<PlMerchant> GetOrLoadCacheItemById(Guid merchantId)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(merchantId), c =>
            {
                PowerLmsUserDbContext db = null;
                var r = LoadById(OwCacheHelper.GetIdFromCacheKey<PlMerchant>(c.Key as string).Value, ref db);
                var item = new OwCacheItem<PlMerchant>()
                {
                    Data = r,
                    CancellationTokenSource = new CancellationTokenSource(),
                    Tag = db,
                };
                item.ChangeToken = new CancellationChangeToken(item.CancellationTokenSource.Token);
                c.AddExpirationToken(item.ChangeToken);
                return item;
            });
            return result;
        }

        /// <summary>
        /// 从指定的用户对象获取其商户信息。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>对于不属于商户的账号返回null。</returns>
        public OwCacheItem<PlMerchant> GetOrLoadCacheItemByUser(Account user)
        {
            var merchId = user.MerchantId;
            if (merchId is null)    //若未缓存商户Id
            {
                if (!GetIdByUserId(user.Id, out merchId)) return null;
                user.MerchantId = merchId;
            }
            return GetOrLoadCacheItemById(merchId.Value);
        }

        /// <summary>
        /// 获取指定账户所属的商户Id。
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="MerchantId"></param>
        /// <returns>true指定账户所属商户Id,如果不属于任何商户则返回null。false 没有找到指定的用户Id。</returns>
        public bool GetIdByUserId(Guid userId, out Guid? MerchantId)
        {
            var userOrg = _DbContext.AccountPlOrganizations.AsNoTracking().FirstOrDefault(c => c.UserId == userId);    //随机找到一个所属的组织机构
            if (userOrg == null)
            {
                MerchantId = null;
                return false;
            }
            return GetIdByOrgId(userOrg.OrgId, out MerchantId);
        }

        /// <summary>
        /// 从数据库中取指定组织机构Id所属的商户Id。
        /// </summary>
        /// <param name="orgId">机构Id。</param>
        /// <param name="merchantId"></param>
        /// <returns>true则找到了商户Id，false没有找到。</returns>
        public bool GetIdByOrgId(Guid orgId, out Guid? merchantId)
        {
            var org = _DbContext.PlOrganizations.Find(orgId);   //找到组织机构对象
            if (org == null)
            {
                if (_DbContext.Merchants.Find(orgId) is PlMerchant merch)
                {
                    merchantId = merch.Id;
                    return true;
                }
                merchantId = null;
                return false;
            }
            for (; org is not null; org = org.Parent)
            {
                if (org.ParentId is null)
                {
                    merchantId = org.MerchantId;
                    return true;
                }
            }
            merchantId = null;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="merchantId"></param>
        /// <returns></returns>
        public bool GetIdByRoleId(Guid roleId, out Guid? merchantId)
        {
            if (_DbContext.PlRoles.Find(roleId) is not PlRole role || !role.OrgId.HasValue)
            {
                merchantId = null;
                return false;
            }
            return GetIdByOrgId(role.OrgId.Value, out merchantId);
        }
    }
}
