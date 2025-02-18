/*
 * 文件名：ExchangeRateManager.cs
 * 作者：OW
 * 创建日期：2023年10月25日
 * 描述：该文件包含 ExchangeRateManager 服务的实现，用于缓存 PlExchangeRate 表，并在变化时使缓存失效。
 * 当前文件内容概述：
 * - ExchangeRateManager：用于管理 PlExchangeRate 表的缓存，并在表变化时使缓存失效。
 */

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Data;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 汇率管理器，用于缓存 PlExchangeRate 表。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped,ServiceType =typeof(IAfterDbContextSaving<PlExchangeRate>))]
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class ExchangeRateManager : IDisposable, IAfterDbContextSaving<PlExchangeRate>
    {
        #region 私有字段
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private readonly IMemoryCache _Cache;
        private readonly ILogger<ExchangeRateManager> _Logger;
        private const string CacheKey = "ExchangeRateCache";
        #endregion 私有字段

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化数据库上下文工厂、缓存和日志记录器。
        /// </summary>
        /// <param name="dbContextFactory">数据库上下文工厂。</param>
        /// <param name="cache">内存缓存。</param>
        /// <param name="logger">日志记录器。</param>
        public ExchangeRateManager(IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, IMemoryCache cache, ILogger<ExchangeRateManager> logger)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion 构造函数

        #region 公共方法
        /// <summary>
        /// 获取汇率的 ILookup。
        /// </summary>
        /// <returns>汇率的 ILookup。项已经按结束事件点降序排序。</returns>
        public ILookup<(string, string), PlExchangeRate> GetExchangeRates()
        {
            return _Cache.GetOrCreate(CacheKey, entry =>
            {
                using var dbContext = _DbContextFactory.CreateDbContext();
                var exchangeRates = dbContext.Set<PlExchangeRate>().AsNoTracking()
                    .OrderByDescending(rate => rate.EndData) // 确保排序稳定
                    .ToList();
                _Logger.LogDebug("缓存 PlExchangeRate 表，共 {Count} 条记录。", exchangeRates.Count);
                return exchangeRates.ToLookup(rate => (rate.SCurrency, rate.DCurrency));
            });
        }

        /// <summary>
        /// 使缓存失效。
        /// </summary>
        public void InvalidateCache()
        {
            _Cache.Remove(CacheKey);
            _Logger.LogDebug("已使 PlExchangeRate 缓存失效。");
        }
        #endregion 公共方法

        #region IAfterDbContextSaving 实现
        /// <summary>
        /// 在保存 PlExchangeRate 后，检测变化并使缓存失效。
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (dbContext.ChangeTracker.Entries<PlExchangeRate>().Any(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            {
                InvalidateCache();
                _Logger.LogDebug("PlExchangeRate 表发生变化，已使缓存失效。");
            }
        }
        #endregion IAfterDbContextSaving 实现

        #region 释放资源
        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            // 释放资源
        }
        #endregion 释放资源
    }
}

