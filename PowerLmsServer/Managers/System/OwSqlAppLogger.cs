/*
 * OwSqlAppLogger.cs
 * 版权所有 (c) 2023 PowerLms. 保留所有权利。
 * 此文件包含应用日志服务实现，负责日志的定义、记录及管理。
 * 
 * 应用日志系统需求：
 * - 日志查看功能：提供系统日志管理界面，支持多维度日志查询和筛选
 * - 日志级别管理：支持不同级别的日志记录和展示
 * - 操作追踪：记录操作IP、操作类型、操作人员等关键信息
 * - 时间管理：精確記錄操作時間，支持時間範圍查詢
 * 
 * 日志字段设计：
 * - 日志级别(Log Level)：用于区分日志的重要程度和类型
 * - 日志内容(Log Content)：记录具体的操作内容和结果信息
 * - 操作IP(Operation IP)：记录操作来源IP地址，用于安全审计
 * - 操作类型(Operation Type)：分类记录不同类型的操作（如登录、修改、删除等）
 * - 操作人ID(Operator ID)：记录执行操作的用户标识
 * - 操作人名(Operator Name)：记录操作人员的显示名称
 * - 操作时间(Operation Time)：精确记录操作发生的时间点
 * 
 * 系统特性：
 * - 搜索功能：支持按关键字搜索日志内容
 * - 筛选功能：支持按日志级别、操作类型、时间范围等条件筛选
 * - 分页展示：支持大量日志数据的分页查看
 * - 实时记录：自动记录用户操作，如"用户: admin[北京公司]登陆成功!"
 * 
 * 技术实现：
 * - 基于Entity Framework Core的数据持久化
 * - 批量写入机制提高性能
 * - 内存缓存优化查询效率
 * - HTTP上下文集成获取操作信息
 * 
 * 作者: OW
 * 创建日期: 2024-10-10
 * 修改日期: 2025-3-6
 * 修改记录: 2025-01-27 整合应用日志系统需求文档
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
    /// 
    /// 核心功能：
    /// - 日志记录：自动记录用户操作，包括登录、业务操作、系统事件等
    /// - 日志查询：提供多维度的日志查询和筛选功能
    /// - 日志管理：支持日志的分类、分级和批量处理
    /// - 性能优化：使用批量写入和内存缓存提高系统性能
    /// 
    /// 日志字段说明：
    /// - 操作IP：自动获取客户端IP地址，用于安全审计和访问控制
    /// - 操作类型：系统自动分类或用户指定，如"登录"、"修改"、"删除"等
    /// - 操作人员：集成用户身份信息，记录操作人ID和显示名称
    /// - 操作时间：精确到毫秒的时间戳，支持时间范围查询
    /// - 日志内容：详细的操作描述，如"用户: admin[北京公司]登陆成功!"
    /// 
    /// 使用场景：
    /// - 安全审计：追踪用户操作行为，发现异常访问
    /// - 问题诊断：记录系统错误和异常信息，便于故障排查
    /// - 合规要求：满足企业级应用的日志记录和审计要求
    /// - 运营分析：通过日志数据分析用户行为和系统使用情况
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, AutoCreateFirst = true)]
    public class OwSqlAppLogger : IDisposable
    {
        #region 私有字段
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private readonly OwBatchDbWriter<PowerLmsUserDbContext> _BatchDbWriter;
        private readonly IHttpContextAccessor _HttpContextAccessor;
        private readonly IServiceProvider _ServiceProvider;
        private readonly IMemoryCache _MemoryCache;
        private const string LoggerStoresCacheKey = "LoggerStoresCache";
        private readonly IMapper _Mapper;
        #endregion

        #region 公共属性
        /// <summary>
        /// 所有日志源。
        /// 日志源管理说明：
        /// - 缓存机制：使用内存缓存提高查询性能，缓存过期时间1小时
        /// - 并发安全：使用ConcurrentDictionary确保线程安全
        /// - 动态加载：支持运行时动态加载和更新日志源配置
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
        /// 
        /// 依赖注入说明：
        /// - BatchDbWriter：批量数据库写入器，提高日志写入性能
        /// - HttpContextAccessor：HTTP上下文访问器，获取请求相关信息（IP、用户等）
        /// - ServiceProvider：服务提供者，用于获取其他依赖服务
        /// - MemoryCache：内存缓存，优化日志源查询性能
        /// - Mapper：对象映射器，简化数据转换操作
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
            _DbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PowerLmsUserDbContext>>();
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
            using var dbContext = _DbContextFactory.CreateDbContext();
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
            var companyName = merchant?.Name_DisplayName ?? "Unknown";

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
            GC.SuppressFinalize(this);
        }
        #endregion
    }

}
