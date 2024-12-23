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
        /// <param name="merchId"></param>
        /// <param name="dbContext">使用的数据库上下文，null则自动从池中取一个新上下文,调用者负责处置对象。</param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlOrganization> LoadOrgsByMerchantId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            dbContext ??= _DbContextFactory.CreateDbContext();
            IDictionary<Guid, PlOrganization> tmp;
            ConcurrentDictionary<Guid, PlOrganization> result;
            lock (dbContext)
            {
                var orgs = dbContext.PlOrganizations.Where(c => c.MerchantId == merchId).Include(c => c.Parent).Include(c => c.Children).AsEnumerable();
                tmp = orgs.SelectMany(c => OwHelper.GetAllSubItemsOfTree(new PlOrganization[] { c }, d => d.Children))
                    .ToDictionary(c => c.Id, c => c);
                result = new ConcurrentDictionary<Guid, PlOrganization>(tmp);
                OrgsLoaded(result);
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
            var merch = _MerchantManager.GetOrLoadCacheItemById(merchId);
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
        /// <returns>指定商户的所有机构的缓存项，如果没找到则返回null。</returns>
        public OwCacheItem<ConcurrentDictionary<Guid, PlOrganization>> GetOrgsCacheItemByMerchantId(Guid merchantId)
        {
            var result = _Cache.Get<OwCacheItem<ConcurrentDictionary<Guid, PlOrganization>>>(OwCacheHelper.GetCacheKeyFromId(merchantId, ".Orgs"));
            return result;
        }

        /// <summary>
        /// 按指定商户Id获取或加载所有下属机构信息。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns>没有找到指定商户则返回空字典。</returns>
        public OwCacheItem<ConcurrentDictionary<Guid, PlOrganization>> GetOrLoadOrgsCacheItemByMerchantId(Guid merchantId)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(merchantId, ".Orgs"), c =>
            {
                var merch = _MerchantManager.GetOrLoadCacheItemById(OwCacheHelper.GetIdFromCacheKey(c.Key as string, ".Orgs").Value);
                var db = merch.Data.DbContext;
                var r = new OwCacheItem<ConcurrentDictionary<Guid, PlOrganization>>
                {
                    Data = LoadOrgsByMerchantId(OwCacheHelper.GetIdFromCacheKey(c.Key as string, ".Orgs").Value, ref db),
                };
                r.SetCancellations(new CancellationTokenSource(), merch.Data.ExpirationTokenSource);
                c.AddExpirationToken(r.ChangeToken);
                return r;
            });
            return result;
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
            List<Guid> ids = new List<Guid>();  //需要排除的下属子公司及其机构
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
        public OwCacheItem<ConcurrentDictionary<Guid, PlOrganization>> GetOrLoadCurrentOrgsCacheItemByUser(Account user)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(user.Id, ".CurrentOrgs"), c =>
            {
                var r = new OwCacheItem<ConcurrentDictionary<Guid, PlOrganization>>
                {
                    Data = LoadCurrentOrgsByUser(user),
                };
                var merch = _MerchantManager.GetOrLoadCacheItemByUser(user);
                var orgs = GetOrLoadOrgsCacheItemByMerchantId(merch.Data.Id);
                var userCi = _AccountManager.GetOrLoadById(user.Id);
                if (userCi is null) return null;
                r.SetCancellations(new CancellationTokenSource(), userCi.ChangeToken, orgs.ChangeToken);
                return r;
            });
            return result;
        }

        /// <summary>
        /// 获取用户当前登录到的公司。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>返回登录的公司，该对象不可更改。没有登录到机构时会返回null。</returns>
        public PlOrganization GetCurrentCompanyByUser(Account user)
        {
            if (user.OrgId is null) return null;
            if (_MerchantManager.GetOrLoadCacheItemByUser(user) is not OwCacheItem<PlMerchant> merch) return null;
            if (GetOrLoadOrgsCacheItemByMerchantId(merch.Data.Id) is not OwCacheItem<ConcurrentDictionary<Guid, PlOrganization>> orgs) return null;
            if (!orgs.Data.TryGetValue(user.OrgId.Value, out var org)) return null;
            PlOrganization tmp;
            for (tmp = org; tmp is not null && tmp.Otc != 2; tmp = tmp.Parent) ;
            return tmp;
        }
    }
}
