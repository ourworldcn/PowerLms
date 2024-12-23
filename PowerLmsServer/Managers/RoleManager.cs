using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.Util.HSSFColor;

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
        public RoleManager(MerchantManager merchantManager, OrganizationManager organizationManager, IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, AccountManager accountManager)
        {
            _MerchantManager = merchantManager;
            _OrganizationManager = organizationManager;
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            _AccountManager = accountManager;
        }

        readonly MerchantManager _MerchantManager;
        readonly OrganizationManager _OrganizationManager;
        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        readonly AccountManager _AccountManager;

        /// <summary>
        /// 从数据库调入指定商户下的所有角色。
        /// </summary>
        /// <param name="merchId"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlRole> LoadRolesByMerchantId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            var dic = _OrganizationManager.GetOrLoadOrgsCacheItemByMerchantId(merchId);
            var orgIds = dic.Data.Keys;
            dbContext ??= _DbContextFactory.CreateDbContext();
            lock (dbContext)
            {
                var tmp = dbContext.PlRoles.Where(c => orgIds.Contains(c.OrgId.Value)).AsEnumerable().ToDictionary(c => c.Id);
                return new ConcurrentDictionary<Guid, PlRole>(tmp);
            }
        }

        /// <summary>
        /// 获取指定商户下所有角色的缓存项。
        /// </summary>
        /// <param name="merchId"></param>
        /// <returns>指定商户下所有角色的缓存项，如果没找到则返回null。</returns>
        public OwCacheItem<ConcurrentDictionary<Guid, PlRole>> GetRolesCacheItemByMerchantId(Guid merchId)
        {
            var result = _Cache.Get<OwCacheItem<ConcurrentDictionary<Guid, PlRole>>>(OwCacheHelper.GetCacheKeyFromId(merchId, ".Roles"));
            return result;
        }

        /// <summary>
        /// 获取或加载商户下所有角色的缓存项。
        /// </summary>
        /// <param name="merchId">商户Id。</param>
        /// <returns></returns>
        public OwCacheItem<ConcurrentDictionary<Guid, PlRole>> GetOrLoadRolesCacheItemByMerchantId(Guid merchId)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(merchId, ".Roles"), entry =>
            {
                var merchCi = _MerchantManager.GetOrLoadCacheItemById(merchId);
                var orgCi = _OrganizationManager.GetOrLoadOrgsCacheItemByMerchantId(merchCi.Data.Id);
                var db = merchCi.Data.DbContext;
                var r = new OwCacheItem<ConcurrentDictionary<Guid, PlRole>>
                {
                    Data = LoadRolesByMerchantId(merchId, ref db),
                };
                r.SetCancellations(new CancellationTokenSource(), merchCi.ChangeToken, orgCi.ChangeToken);
                entry.AddExpirationToken(r.ChangeToken);
                return r;
            });
            return result;
        }

        /// <summary>
        /// 返回指定用户当前登录公司的所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>用户当前登录公司的所有角色缓存项，如果没找到则返回null。</returns>
        public OwCacheItem<ConcurrentDictionary<Guid, PlRole>> GetCurrentRolesCacheItem(Account user)
        {
            return GetCurrentRolesCacheItem(user.Id);
        }

        /// <summary>
        /// 返回指定用户当前登录公司的所有组织机构。
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>用户当前登录公司的所有组织机构缓存项，如果没找到则返回null。</returns>
        public OwCacheItem<ConcurrentDictionary<Guid, PlRole>> GetCurrentRolesCacheItem(Guid userId)
        {
            var result = _Cache.Get<OwCacheItem<ConcurrentDictionary<Guid, PlRole>>>(OwCacheHelper.GetCacheKeyFromId(userId, ".CurrentRoles"));
            return result;
        }

        /// <summary>
        /// 按指定用户当前的登录机构加载其所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        public ConcurrentDictionary<Guid, PlRole> LoadCurrentRolesByUser(Account user, ref PowerLmsUserDbContext db)
        {
            var orgs = _OrganizationManager.GetOrLoadCurrentOrgsCacheItemByUser(user);   //用户登录的有效机构集合
            var merchant = _MerchantManager.GetOrLoadCacheItemByUser(user);
            var allRoles = GetOrLoadRolesCacheItemByMerchantId(merchant.Data.Id);   //商户下所有角色

            db ??= _DbContextFactory.CreateDbContext();
            HashSet<Guid> hsRoleIds;
            lock (db)
                hsRoleIds = db.PlAccountRoles.Where(c => c.UserId == user.Id).Select(c => c.RoleId).ToHashSet(); //所有直属角色Id集合
            var coll = allRoles.Data.Where(c => c.Value.OrgId.HasValue && hsRoleIds.Contains(c.Key)   //直属角色
                && orgs.Data.ContainsKey(c.Value.OrgId.Value));    //属于有效机构

            return new ConcurrentDictionary<Guid, PlRole>(coll);
        }

        /// <summary>
        /// 获取用户当前登录的所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>所有当前有效角色的字典。未登录到公司则返回空字典。</returns>
        public OwCacheItem<ConcurrentDictionary<Guid, PlRole>> GetOrLoadCurrentRolesCacheItemByUser(Account user)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(user.Id, ".CurrentRoles"), c =>
            {
                var db = user.DbContext;
                var r = new OwCacheItem<ConcurrentDictionary<Guid, PlRole>>
                {
                    Data = LoadCurrentRolesByUser(user, ref db),
                };
                var merch = _MerchantManager.GetOrLoadCacheItemByUser(user);
                var roles = GetRolesCacheItemByMerchantId(merch.Data.Id);
                var userCi = _AccountManager.GetOrLoadById(user.Id);
                if (userCi is null) return null;
                r.SetCancellations(new CancellationTokenSource(), userCi.ChangeToken, roles.ChangeToken);
                c.AddExpirationToken(r.ChangeToken);
                return r;
            });
            return result;
        }

    }

}
