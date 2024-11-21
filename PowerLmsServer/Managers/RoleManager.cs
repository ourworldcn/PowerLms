using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
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
    /// 角色管理器
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class RoleManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public RoleManager(MerchantManager merchantManager, OrganizationManager organizationManager, IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory)
        {
            _MerchantManager = merchantManager;
            _OrganizationManager = organizationManager;
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
        }

        MerchantManager _MerchantManager;
        OrganizationManager _OrganizationManager;
        IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="merchId"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlRole> LoadRolesFromMerchantId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            var dic = _OrganizationManager.GetOrLoadOrgsFromMerchId(merchId);
            var orgIds = dic.Keys;
            dbContext ??= _DbContextFactory.CreateDbContext();
            lock (dbContext)
            {
                var tmp = dbContext.PlRoles.Where(c => orgIds.Contains(c.OrgId.Value)).AsEnumerable().ToDictionary(c => c.Id);
                return new ConcurrentDictionary<Guid, PlRole>(tmp);
            }
        }

        /// <summary>
        /// 获取商户下所有角色的字典。
        /// </summary>
        /// <param name="merchId"></param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadRolesFromMerchantId(Guid merchId)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(merchId, ".Roles"), entry =>
            {
                var merch = _MerchantManager.GetOrLoadMerchantFromId(merchId);
                entry.AddExpirationToken(new CancellationChangeToken(merch.ExpirationTokenSource.Token));
                var db = merch.DbContext;
                return LoadRolesFromMerchantId(merchId, ref db);
            });
            return result;
        }

    }
}
