/*
 * OwSqlAppLogger.cs
 * 版权所有 (c) 2023 PowerLms. 保留所有权利。
 * 此文件包含应用日志服务实现，负责日志的定义、记录及管理。
 * 作者: OW
 * 创建日期: 2024-10-10
 * 修改日期: 2025-3-6
 */

using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Data;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 应用日志服务类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, AutoCreateFirst = true)]
    public class OwSqlAppLogger : IDisposable
    {
        #region 私有字段
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private PowerLmsUserDbContext _DbContext;
        private readonly OwBatchDbWriter<PowerLmsUserDbContext> _BatchDbWriter;
        private readonly IHttpContextAccessor _HttpContextAccessor;
        private readonly IServiceProvider _ServiceProvider;
        private readonly IMemoryCache _MemoryCache;
        private const string LoggerStoresCacheKey = "LoggerStoresCache";
        IMapper _Mapper;
        #endregion

        #region 公共属性
        /// <summary>
        /// 所有日志源。
        /// </summary>
        public ConcurrentDictionary<Guid, OwAppLogStore> LoggerStores => _MemoryCache.GetOrCreate(LoggerStoresCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // 设置缓存过期时间
            return LoadLoggerStores();
        });
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化应用日志服务。
        /// </summary>
        /// <param name="batchDbWriter">批量数据库写入器。</param>
        /// <param name="httpContextAccessor">HTTP 上下文访问器。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="memoryCache">内存缓存。</param>
        /// <param name="mapper"></param>
        public OwSqlAppLogger(OwBatchDbWriter<PowerLmsUserDbContext> batchDbWriter, IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider, IMemoryCache memoryCache, IMapper mapper)
        {
            _BatchDbWriter = batchDbWriter ?? throw new ArgumentNullException(nameof(batchDbWriter));
            _HttpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _MemoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            Initializer();
            _Mapper = mapper;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化方法。
        /// </summary>
        private void Initializer()
        {
            // 初始化操作
        }

        /// <summary>
        /// 加载所有 OwAppLogStore 对象并存储到缓存中。
        /// </summary>
        /// <returns>包含所有 OwAppLogStore 对象的字典。</returns>
        private ConcurrentDictionary<Guid, OwAppLogStore> LoadLoggerStores()
        {
            using var dbContext = _ServiceProvider.GetRequiredService<IDbContextFactory<PowerLmsUserDbContext>>().CreateDbContext();
            var loggerStores = dbContext.OwAppLogStores.AsEnumerable().ToDictionary(c => c.Id);
            return new ConcurrentDictionary<Guid, OwAppLogStore>(loggerStores);
        }
        #endregion

        #region 公共方法

        /// <summary>
        /// 定义事件源。
        /// </summary>
        /// <param name="typeId">事件类型 ID。</param>
        /// <param name="formatString">格式字符串。</param>
        /// <param name="logLevel"></param>
        public void Define(Guid typeId, string formatString, LogLevel logLevel)
        {
            if (string.IsNullOrEmpty(formatString))
                throw new ArgumentException("格式字符串不能为空。", nameof(formatString));

            var appLogStore = new OwAppLogStore
            {
                Id = typeId,
                FormatString = formatString,
                LogLevel = logLevel,
            };
            LoggerStores.AddOrUpdate(typeId, appLogStore, (id, ov) => appLogStore);
        }

        /// <summary>
        /// 记录通用信息日志条目。
        /// </summary>
        /// <param name="operationType">操作类型。</param>
        public void LogGeneralInfo(string operationType)
        {
            var context = _HttpContextAccessor.HttpContext;
            var request = context?.Request;

            var clientType = request?.Headers["User-Agent"].ToString() ?? "Unknown";
            var operationIp = context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            // 动态获取服务
            var owContext = _ServiceProvider.GetService<OwContext>();
            var loginName = owContext?.User?.LoginName ?? "Unknown";
            var displayName = owContext?.User?.DisplayName ?? "Unknown";

            var orgManager = _ServiceProvider.GetService<OrgManager<PowerLmsUserDbContext>>();
            var userId = owContext?.User?.Id;
            var userMerchantId = userId.HasValue ? orgManager?.GetMerchantIdByUserId(userId.Value) : null;
            var merchant = userMerchantId.HasValue ? orgManager?.GetOrLoadOrgCacheItem(userMerchantId.Value)?.Merchant : null;
            var companyName = merchant?.Name.DisplayName ?? "Unknown";

            // 通用日志记录
            var generalInfo = new GeneralInfoLogEntry
            {
                OperationType = operationType,
                LoginName = loginName,
                CompanyName = companyName,
                DisplayName = displayName,
                OperationIp = operationIp,
                ClientType = clientType
            };

            var merchantId = merchant?.Id;

            // 将 generalInfo 写入日志
            WriteLogItem(new OwAppLogItemStore
            {
                Id = Guid.NewGuid(),
                ParentId = GeneralInfoLogEntry.TypeId,
                CreateUtc = DateTime.UtcNow,
                MerchantId = merchantId, // 根据需要设置商户ID
                ParamstersJson = JsonSerializer.Serialize(generalInfo),
                ExtraBytes = null // 根据需要设置额外的二进制信息
            });
        }

        /// <summary>
        /// 写入日志项。
        /// </summary>
        /// <param name="logItem">要写入的日志项。</param>
        public void WriteLogItem(OwAppLogItemStore logItem)
        {
            if (logItem == null)
                throw new ArgumentNullException(nameof(logItem));

            var dbOperation = new DbOperation
            {
                OperationType = DbOperationType.Insert,
                Entity = logItem
            };

            _BatchDbWriter.AddItem(dbOperation);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            _DbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }

}
