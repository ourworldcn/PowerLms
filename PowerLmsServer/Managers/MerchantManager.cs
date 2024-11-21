using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
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
        public MerchantManager(PowerLmsUserDbContext dbContext, IMemoryCache cache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory)
        {
            _DbContext = dbContext;
            _Cache = cache;
            _DbContextFactory = dbContextFactory;
        }

        readonly PowerLmsUserDbContext _DbContext;
        readonly IMemoryCache _Cache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;

        /// <summary>
        /// 根据指定Id加载商户对象。
        /// </summary>
        /// <param name="merchId"></param>
        /// <param name="dbContext">传递null，则用池自动获取一个数据库上下文。</param>
        /// <returns>没有找到则返回null。</returns>
        public PlMerchant LoadMerchantFromId(Guid merchId, ref PowerLmsUserDbContext dbContext)
        {
            dbContext ??= _DbContextFactory.CreateDbContext();
            PlMerchant result;
            lock (_DbContext)
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
        /// <returns></returns>
        public PlMerchant GetOrLoadMerchantFromId(Guid id)
        {
            var result = _Cache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId<PlMerchant>(id), c =>
            {
                PowerLmsUserDbContext db = null;
                return LoadMerchantFromId(OwCacheHelper.GetIdFromCacheKey<PlMerchant>(c.Key as string).Value, ref db);
            });
            return result;
        }


    }
}
