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
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class ExchangeRateManager : IDisposable
    {
        #region 私有字段
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private readonly IMemoryCache _Cache;
        private readonly ILogger<ExchangeRateManager> _Logger;
        private readonly IHostApplicationLifetime _ApplicationLifetime;
        private readonly SqlDependencyManager _SqlDependencyManager;
        private const string CacheKey = "ExchangeRateCache";
        private CancellationTokenSource _SqlDependencyTokenSource;
        #endregion 私有字段

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化数据库上下文工厂、缓存和日志记录器。
        /// </summary>
        /// <param name="dbContextFactory">数据库上下文工厂。</param>
        /// <param name="cache">内存缓存。</param>
        /// <param name="logger">日志记录器。</param>
        /// <param name="applicationLifetime">应用程序生命周期。</param>
        /// <param name="sqlDependencyManager">SQL 依赖管理器。</param>
        public ExchangeRateManager(IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, IMemoryCache cache, ILogger<ExchangeRateManager> logger, IHostApplicationLifetime applicationLifetime, SqlDependencyManager sqlDependencyManager)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            _SqlDependencyManager = sqlDependencyManager ?? throw new ArgumentNullException(nameof(sqlDependencyManager));

            RegisterSqlDependency();

            // 注册应用程序停止事件
            _ApplicationLifetime.ApplicationStopped.Register(OnApplicationStopped);
        }
        #endregion 构造函数

        #region 公共方法
        /// <summary>
        /// 获取汇率的 ILookup。
        /// </summary>
        /// <returns>汇率的 ILookup。</returns>
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

        #region 私有方法
        /// <summary>
        /// 注册 SqlDependency 监听。
        /// </summary>
        private void RegisterSqlDependency()
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            var query = dbContext.Set<PlExchangeRate>().AsNoTracking().ToQueryString();
            _SqlDependencyTokenSource = _SqlDependencyManager.RegisterSqlDependency(query, dbContext.Database.GetDbConnection().ConnectionString);
            _SqlDependencyTokenSource.Token.Register(OnDependencyChange);
        }

        /// <summary>
        /// SqlDependency 变化事件处理。
        /// </summary>
        private void OnDependencyChange()
        {
            InvalidateCache();
            _Logger.LogDebug("PlExchangeRate 表发生变化，已使缓存失效。");
        }

        /// <summary>
        /// 应用程序停止时调用的方法。
        /// </summary>
        private void OnApplicationStopped()
        {
            StopDatabaseListening();
        }

        /// <summary>
        /// 停止数据库侦听。
        /// </summary>
        private void StopDatabaseListening()
        {
            _SqlDependencyTokenSource?.Cancel();
            _Logger.LogDebug("已停止 PlExchangeRate 表的数据库侦听。");
        }
        #endregion 私有方法

        #region 释放资源
        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            StopDatabaseListening();
        }
        #endregion 释放资源
    }
}

