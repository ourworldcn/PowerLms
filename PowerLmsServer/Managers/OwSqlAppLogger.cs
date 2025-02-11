using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 应用日志服务类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class OwSqlAppLogger : IDisposable
    {
        private readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        private PowerLmsUserDbContext _DbContext;
        private ConcurrentDictionary<Guid, OwAppLogStore> _LoggerStores;
        private readonly BatchDbWriter _BatchDbWriter;

        /// <summary>
        /// 所有源。
        /// </summary>
        public ConcurrentDictionary<Guid, OwAppLogStore> LoggerStores => LazyInitializer.EnsureInitialized(ref _LoggerStores, () =>
        {
            lock (_DbContext)
            {
                return new ConcurrentDictionary<Guid, OwAppLogStore>(_DbContext.OwAppLogStores.AsEnumerable().ToDictionary(c => c.Id));
            }
        });

        /// <summary>
        /// 构造函数，初始化应用日志服务。
        /// </summary>
        /// <param name="dbContextFactory">数据库上下文工厂。</param>
        /// <param name="batchDbWriter">批量数据库写入器。</param>
        public OwSqlAppLogger(IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, BatchDbWriter batchDbWriter)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _DbContext = _DbContextFactory.CreateDbContext();
            _BatchDbWriter = batchDbWriter ?? throw new ArgumentNullException(nameof(batchDbWriter));
            Initializer();
        }

        /// <summary>
        /// 初始化方法。
        /// </summary>
        private void Initializer()
        {
            // 初始化操作
        }

        /// <summary>
        /// 定义事件源。
        /// </summary>
        /// <param name="typeId">事件类型 ID。</param>
        /// <param name="formatString">格式字符串。</param>
        public void Define(Guid typeId, string formatString)
        {
            if (string.IsNullOrEmpty(formatString))
                throw new ArgumentException("Format string cannot be null or empty.", nameof(formatString));

            LoggerStores.AddOrUpdate(typeId, new OwAppLogStore { Id = typeId, FormatString = formatString },
                (id, ov) => new OwAppLogStore { Id = typeId, FormatString = formatString });
        }

        /// <summary>
        /// 写入 OwAppLogItemStore 实体。
        /// </summary>
        /// <param name="logItem">要写入的 OwAppLogItemStore 实体。</param>
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
    }
}
