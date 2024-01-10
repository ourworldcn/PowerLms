using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Atp;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 组织机构管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, AutoCreateFirst = true)]
    public class OrganizationManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationManager(PowerLmsUserDbContext dbContext, IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory)
        {
            _DbContext = dbContext;
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            Initialize();
        }

        private void Initialize()
        {
            var dic1 = Id2Merchants;
            var dic2 = Id2Orgs;
        }

        PowerLmsUserDbContext _DbContext;
        IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;

        /// <summary>
        /// 获取指定账户所属的商户Id。
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="MerchantId"></param>
        /// <returns>true指定账户所属商户Id,如果不属于任何商户则返回null。false 没有找到指定的用户Id。</returns>
        public bool GetMerchantId(Guid accountId, out Guid? MerchantId)
        {
            var userOrg = _DbContext.AccountPlOrganizations.AsNoTracking().FirstOrDefault(c => c.UserId == accountId);    //随机找到一个所属的组织机构
            if (userOrg == null)
            {
                MerchantId = null;
                return false;
            };
            var org = _DbContext.PlOrganizations.FirstOrDefault(c => c.Id == userOrg.OrgId);
            if (org == null)    //若不是组织机构
            {
                var merch = _DbContext.Merchants.Find(userOrg.OrgId);
                if (merch is null)  //若也不是商户Id
                {
                    MerchantId = null;
                    return false;
                }
                MerchantId = merch.Id;
                return true;
            }
            return GetMerchantIdFromOrgId(org.Id, out MerchantId);
        }

        /// <summary>
        /// 取指定组织机构Id所属的商户Id。
        /// </summary>
        /// <param name="orgId">机构Id。</param>
        /// <param name="MerchantId"></param>
        /// <returns>true则找到了商户Id，false没有找到。</returns>
        public bool GetMerchantIdFromOrgId(Guid orgId, out Guid? MerchantId)
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

        /// <summary>
        /// 获取指定根Id(商户或机构)下所有组织机构。不包含指定id的实体。
        /// </summary>
        /// <param name="rootId"></param>
        /// <returns></returns>
        public List<PlOrganization> GetAllOrgInRoot(Guid rootId)
        {
            var result = new List<PlOrganization>();
            if (_DbContext.Merchants.Find(rootId) is PlMerchant rootMerchant)  //若是商户Id
            {
                foreach (var org in _DbContext.PlOrganizations.Where(c => c.MerchantId == rootId))
                {
                    result.AddRange(GetChidren(org));
                    result.Add(org);
                }
            }
            else //若非商户Id
            {
                var root = _DbContext.PlOrganizations.Find(rootId);
                if (root is null) return result;
                result.AddRange(GetChidren(root));
            }
            return result;
        }

        /// <summary>
        /// 获取指定机构的所有子机构，不包含自身。
        /// </summary>
        /// <param name="org"></param>
        /// <returns></returns>
        public IEnumerable<PlOrganization> GetChidren(PlOrganization org)
        {
            foreach (var child in org.Children)
            {
                foreach (var item in GetChidren(child))
                    yield return item;
                yield return child;
            }
        }

        const string MerchantCacheKey = "MerchantCacheKey.256a2f95-bd83-480b-97ae-d3c978ffbe0b";
        const string OrgCacheKey = "OrgCacheKey.fa85fb74-d809-403a-902f-8da913cf8f4a";
        const string DbCacheKey = "DbCacheKey.fde207c2-99bc-4401-aab6-f5f0465ee368";

        /// <summary>
        /// 获取一个读写用的数据库上下文。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        PowerLmsUserDbContext GetDb()
        {
            var result = _Cache.GetOrCreate(DbCacheKey, c =>
            {
                return _DbContextFactory.CreateDbContext();
            });
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<Guid, PlMerchant> Id2Merchants
        {
            get
            {
                return _Cache.GetOrCreate(MerchantCacheKey, c =>
                {
                    var result = new ConcurrentDictionary<Guid, PlMerchant>(GetDb().Merchants.ToDictionary(c => c.Id, c => c));
                    return result;
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ConcurrentDictionary<Guid, PlOrganization> Id2Orgs
        {
            get
            {
                return _Cache.GetOrCreate(OrgCacheKey, c =>
                {
                    var result = new ConcurrentDictionary<Guid, PlOrganization>(GetDb().PlOrganizations.ToDictionary(c => c.Id, c => c));
                    return result;
                });
            }
        }
    }

    /// <summary>
    /// 扩展方法类。
    /// </summary>
    public static class OrganizationManagerExtensions
    {
        /// <summary>
        /// 获取用户商户内所有机构。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        static public List<PlOrganization> GetAllOrg(this OrganizationManager mng, Account user)
        {
            var result = new List<PlOrganization>();
            if (mng.GetMerchantId(user.Id, out var merId))
            {
                if (merId.HasValue)
                    result = mng.GetAllOrgInRoot(merId.Value);
            }
            return result;
        }
    }
}
