using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OwDbBase;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 应用日志服务类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton, AutoCreateFirst = true)]
    public class OwSqlAppLogger : IDisposable
    {
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        PowerLmsUserDbContext _DbContext;
        ConcurrentDictionary<Guid, OwAppLoggerStore> _LoggerStores;
        BatchDbWriter _BatchDbWriter;

        /// <summary>
        /// 所有源。
        /// </summary>
        public ConcurrentDictionary<Guid, OwAppLoggerStore> LoggerStores => LazyInitializer.EnsureInitialized(ref _LoggerStores, () =>
        {
            lock (_DbContext)
                return new ConcurrentDictionary<Guid, OwAppLoggerStore>(_DbContext.OwAppLoggerStores.AsEnumerable().ToDictionary(c => c.Id));
        });

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwSqlAppLogger(IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, BatchDbWriter batchDbWriter)
        {
            _DbContextFactory = dbContextFactory;
            _DbContext = _DbContextFactory.CreateDbContext();
            _BatchDbWriter = batchDbWriter;
            Initializer();
        }

        private void Initializer()
        {
        }

        /// <summary>
        /// 定义事件源。
        /// </summary>
        public void Define(Guid typeId, string formatString)
        {
            LoggerStores.AddOrUpdate(typeId, new OwAppLoggerStore { Id = typeId, FormatString = formatString },
                (id, ov) =>
                {
                    return new OwAppLoggerStore { Id = typeId, FormatString = formatString };
                });
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            _DbContext?.Dispose();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class OwSqlAppLoggerFlat
    {
    }
}
