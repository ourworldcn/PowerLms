﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
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
        /// 获取缓存的商户对象。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PlMerchant GetMerchantById(Guid id)
        {
            var result = _Cache.Get<PlMerchant>(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(id));
            return result;
        }

        /// <summary>
        /// 加载或获取缓存的商户对象。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PlMerchant GetOrLoadMerchantById(Guid id)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(id), c =>
            {
                PowerLmsUserDbContext db = null;
                return LoadMerchantById(OwCacheHelper.GetIdFromCacheKey<PlMerchant>(c.Key as string).Value, ref db);
            });
            return result;
        }

        /// <summary>
        /// 从指定的用户对象获取其商户信息。
        /// </summary>
        /// <param name="user"></param>
        /// <returns>对于不属于商户的账号返回null。</returns>
        public PlMerchant GetOrLoadMerchantByUser(Account user)
        {
            var merchId = user.MerchantId;
            if (merchId is null)
            {
                if (!GetMerchantId(user.Id, out merchId)) return null;
                user.MerchantId = merchId;
            }
            return GetOrLoadMerchantById(merchId.Value);
        }

        /// <summary>
        /// 指出商户已经变化。
        /// </summary>
        /// <param name="merchId"></param>
        /// <returns></returns>
        public bool SetChange(Guid merchId)
        {
            if (GetMerchantById(merchId) is PlMerchant merch)
            {
                merch.ExpirationTokenSource?.Cancel();
                return true;
            }
            else return false;
        }

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
            return GetMerchantIdByOrgId(org.Id, out MerchantId);
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
