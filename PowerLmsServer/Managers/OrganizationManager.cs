﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
    /// 商户信息。
    /// </summary>
    public class MerchantInfo
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public MerchantInfo()
        {

        }

        /// <summary>
        /// 商户对象。
        /// </summary>
        public PlMerchant Merchant { get; set; }

        /// <summary>
        /// 所有机构信息。
        /// </summary>
        public ConcurrentDictionary<Guid, PlOrganization> Orgs { get; set; } = new ConcurrentDictionary<Guid, PlOrganization>();
    }

    /// <summary>
    /// 组织机构管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OrganizationManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationManager(IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, MerchantManager merchantManager)
        {
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
            _MerchantManager = merchantManager;
        }

        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        readonly MerchantManager _MerchantManager;

        #region 机构缓存及相关

        /// <summary>
        /// 加载指定商户下所有机构对象。
        /// </summary>
        /// <param name="merchId"></param>
        /// <param name="dbContext">使用的数据库上下文，null则自动从池中取一个新上下文,调用者负责处置对象。</param>
        /// <returns></returns>
        public ConcurrentDictionary<Guid, PlOrganization> LoadOrgsFromMerchId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            dbContext ??= _DbContextFactory.CreateDbContext();
            IDictionary<Guid, PlOrganization> tmp;
            lock (dbContext)
            {
                var orgs = dbContext.PlOrganizations.Where(c => c.MerchantId == merchId).Include(c => c.Parent).Include(c => c.Children).AsEnumerable();
                tmp = orgs.SelectMany(c => OwHelper.GetAllSubItemsOfTree(new PlOrganization[] { c }, d => d.Children))
                    .ToDictionary(c => c.Id, c => c);
            }
            var result = new ConcurrentDictionary<Guid, PlOrganization>(tmp);
            return result;
        }

        /// <summary>
        /// 按指定商户Id获取或加载所有下属机构信息。
        /// </summary>
        /// <param name="merchantId"></param>
        /// <returns>没有找到指定商户则返回空字典。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> GetOrLoadOrgsFromMerchId(Guid merchantId)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId(merchantId, ".Orgs"), c =>
            {
                var merch = _MerchantManager.GetOrLoadMerchantFromId(OwCacheHelper.GetIdFromCacheKey(c.Key as string, ".Orgs").Value);
                PowerLmsUserDbContext db = merch.DbContext;
                c.AddExpirationToken(new CancellationChangeToken(merch.ExpirationTokenSource.Token));
                return LoadOrgsFromMerchId(OwCacheHelper.GetIdFromCacheKey(c.Key as string, ".Orgs").Value, ref db);
            });
            return result;
        }

        #endregion 机构缓存及相关

        /// <summary>
        /// 获取当前的登录机构及所有子机构。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>如果没有指定所属当前机构则返回空字典。</returns>
        public ConcurrentDictionary<Guid, PlOrganization> GetCurrentOrgsFromUser(Account user)
        {
            if (user.OrgId is null) return new ConcurrentDictionary<Guid, PlOrganization>();
            if (_MerchantManager.GetOrLoadMerchantFromUser(user) is not PlMerchant merchant) return new ConcurrentDictionary<Guid, PlOrganization>();
            var orgs = GetOrLoadOrgsFromMerchId(merchant.Id);   //所有机构

            var currentOrg = orgs[user.OrgId.Value];
            var result = OwHelper.GetAllSubItemsOfTree(currentOrg, c => c.Children).ToDictionary(c => c.Id);
            return new ConcurrentDictionary<Guid, PlOrganization>(result);
        }
    }

    /// <summary>
    /// 扩展方法类。
    /// </summary>
    public static class OrganizationManagerExtensions
    {
    }
}
