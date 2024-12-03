using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        readonly MerchantManager _MerchantManager;
        readonly OrganizationManager _OrganizationManager;
        readonly IMemoryCache _Cache;
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

        /// <summary>
        /// 按指定用户当前的登录机构加载其所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        public ConcurrentDictionary<Guid, PlRole> LoadCurrentRolesFromUser(Account user, ref PowerLmsUserDbContext db)
        {
            //var key = OwCacheHelper.GetCacheKeyFromId(user.Id, ".CurrentOrgs");
            var merchant = _MerchantManager.GetOrLoadMerchantFromUser(user);
            var orgs = _OrganizationManager.GetCurrentOrgsFromUser(user);   //用户所处所有机构集合

            db ??= _DbContextFactory.CreateDbContext();

            var allRoles = GetOrLoadRolesFromMerchantId(merchant.Id).Values;   //机构下所有角色

            var roles = allRoles.Where(c => c.OrgId.HasValue && orgs.ContainsKey(c.OrgId.Value)).Select(c => c.Id).ToArray();    //可能的角色

            Guid[] roleIds;
            lock (db)
                roleIds = db.PlAccountRoles.Where(c => c.UserId == user.Id && roles.Contains(c.RoleId)).Select(c => c.RoleId).Distinct().ToArray();    //真实的角色Id集合

            var coll = GetOrLoadRolesFromMerchantId(merchant.Id).Where(c => roleIds.Contains(c.Key));
            return new ConcurrentDictionary<Guid, PlRole>(coll);
        }

        /// <summary>
        /// 获取用户当前角色。
        /// </summary>
        /// <param name="user"></param>
        /// 
        /// <returns>所有当前有效角色的字典。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadCurrentRolesFromUser(Account user)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(user.Id, ".CurrentRoles"), c =>
            {
                var db = user.DbContext;
                var r = LoadCurrentRolesFromUser(user, ref db);
                c.AddExpirationToken(new CancellationChangeToken(user.ExpirationTokenSource.Token));
                return r;
            });
            return result;
        }
    }
}
