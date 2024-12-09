using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
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

        #region 商户下所有角色

        #endregion 商户下所有角色
        /// <summary>
        /// 从数据库调入指定商户下的所有角色。
        /// </summary>
        /// <param name="merchId"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlRole> LoadRolesByMerchantId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            var dic = _OrganizationManager.GetOrLoadOrgsByMerchantId(merchId);
            var orgIds = dic.Keys;
            dbContext ??= _DbContextFactory.CreateDbContext();
            lock (dbContext)
            {
                var tmp = dbContext.PlRoles.Where(c => orgIds.Contains(c.OrgId.Value)).AsEnumerable().ToDictionary(c => c.Id);
                return new ConcurrentDictionary<Guid, PlRole>(tmp);
            }
        }

        /// <summary>
        /// 获取或加载商户下所有角色的字典。
        /// </summary>
        /// <param name="merchId"></param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadRolesByMerchantId(Guid merchId)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(merchId, ".Roles"), entry =>
            {
                var merch = _MerchantManager.GetOrLoadMerchantById(merchId);
                var db = merch.DbContext;
                var r = new OwCacheItem<ConcurrentDictionary<Guid, PlRole>>
                {
                    Data = LoadRolesByMerchantId(merchId, ref db),
                };
                r.SetCancellations(new CancellationTokenSource(), merch.ExpirationTokenSource);
                entry.AddExpirationToken(r.ChangeToken);
                return r;
            });
            return result.Data;
        }

        /// <summary>
        /// 使指定商户下所有角色缓存失效。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns></returns>
        public bool SetRolesChange(Guid merchantId)
        {
            if (_Cache.Get(OwCacheHelper.GetCacheKeyFromId(merchantId, ".Roles")) is OwCacheItem<ConcurrentDictionary<Guid, PlRole>> item)
            {
                item.CancellationTokenSource.Cancel();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 按指定用户当前的登录机构加载其所有角色。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        public ConcurrentDictionary<Guid, PlRole> LoadCurrentRolesByUser(Account user, ref PowerLmsUserDbContext db)
        {
            //var key = OwCacheHelper.GetCacheKeyFromId(user.Id, ".CurrentOrgs");

            var merchant = _MerchantManager.GetOrLoadMerchantByUser(user);
            var orgs = _OrganizationManager.GetOrLoadCurrentOrgsByUser(user);   //用户所处所有机构集合
            var allRoles = GetOrLoadRolesByMerchantId(merchant.Id).Values;   //商户下所有角色
            var coll = allRoles.Where(c => c.OrgId.HasValue && orgs.ContainsKey(c.OrgId.Value));
            return new ConcurrentDictionary<Guid, PlRole>(coll.ToDictionary(c => c.Id));
        }

        /// <summary>
        /// 获取用户当前角色。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>所有当前有效角色的字典。</returns>
        public ConcurrentDictionary<Guid, PlRole> GetOrLoadCurrentRolesByUser(Account user)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(user.Id, ".CurrentRoles"), c =>
            {
                var db = user.DbContext;
                var r = LoadCurrentRolesByUser(user, ref db);
                c.AddExpirationToken(new CancellationChangeToken(user.ExpirationTokenSource.Token));
                return r;
            });
            return result;
        }

    }

}
