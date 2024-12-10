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
        public PlMerchant LoadMerchantById(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            dbContext ??= _DbContextFactory.CreateDbContext();
            PlMerchant result;
            lock (dbContext)
                result = dbContext.Merchants.FirstOrDefault(c => c.Id == merchId);
            MerchantLoaded(result, dbContext);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="merch"></param>
        /// <param name="dbContext"></param>
        private void MerchantLoaded(PlMerchant merch, PowerLmsUserDbContext dbContext)
        {
            merch.DbContext = dbContext;
            merch.ExpirationTokenSource = new CancellationTokenSource();

        }

        /// <summary>
        /// 加载或获取缓存的商户对象。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns></returns>
        public OwCacheItem<PlMerchant> GetOrLoadMerchantCacheItemById(Guid merchantId)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(merchantId), c =>
            {
                PowerLmsUserDbContext db = null;
                var r = LoadMerchantById(OwCacheHelper.GetIdFromCacheKey<PlMerchant>(c.Key as string).Value, ref db);
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
        public OwCacheItem<PlMerchant> GetOrLoadMerchantCacheItemByUser(Account user)
        {
            var merchId = user.MerchantId;
            if (merchId is null)    //若未缓存商户Id
            {
                if (!GetMerchantIdByUserId(user.Id, out merchId)) return null;
                user.MerchantId = merchId;
            }
            return GetOrLoadMerchantCacheItemById(merchId.Value);
        }

        /// <summary>
        /// 获取指定账户所属的商户Id。
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="MerchantId"></param>
        /// <returns>true指定账户所属商户Id,如果不属于任何商户则返回null。false 没有找到指定的用户Id。</returns>
        public bool GetMerchantIdByUserId(Guid userId, out Guid? MerchantId)
        {
            var userOrg = _DbContext.AccountPlOrganizations.AsNoTracking().FirstOrDefault(c => c.UserId == userId);    //随机找到一个所属的组织机构
            if (userOrg == null)
            {
                MerchantId = null;
                return false;
            };
            return GetMerchantIdByOrgId(userOrg.OrgId, out MerchantId);
        }

        /// <summary>
        /// 从数据库中取指定组织机构Id所属的商户Id。
        /// </summary>
        /// <param name="orgId">机构Id。</param>
        /// <param name="MerchantId"></param>
        /// <returns>true则找到了商户Id，false没有找到。</returns>
        public bool GetMerchantIdByOrgId(Guid orgId, out Guid? MerchantId)
        {
            var org = _DbContext.PlOrganizations.Find(orgId);   //找到组织机构对象
            if (org == null)
            {
                MerchantId = null;
                return false;
            }
            for (; org is not null; org = org.Parent)
            {
                if (org.ParentId is null)
                {
                    MerchantId = org.MerchantId;
                    return true;
                }
            }
            MerchantId = null;
            return false;
        }
    }
}
