using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW.Server;

namespace OwDbBase
{
    /// <summary>
    /// 
    /// </summary>
    public class DataObjectManagerOptions : IOptions<DataObjectManagerOptions>
    {
        #region 构造函数及相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DataObjectManagerOptions()
        {
        }

        #endregion 构造函数及相关

        #region IOptions接口

        public DataObjectManagerOptions Value => this;

        #endregion IOptions接口

        /// <summary>
        /// 扫描间隔。暂时不起作用。
        /// </summary>
        /// <value>默认1分钟。</value>
        public TimeSpan Frequency { get; set; } = TimeSpan.FromMinutes(1.0);

        /// <summary>
        /// 默认的锁定超时时间。
        /// </summary>
        /// <value>默认值:3秒。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class DataObjectEntry
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="cache">指定所属缓存对象，在调用<see cref="Dispose"/>时可以加入该对象。</param>
        protected DataObjectEntry(object key, OwMemoryCache cache)
        {
            Key = key;
            Cache = cache;
        }

        /// <summary>
        /// 键。
        /// </summary>
        public object Key { get; private set; }

        /// <summary>
        /// 所属的缓存对象。
        /// </summary>
        public OwMemoryCache Cache { get; set; }

        public ICacheEntry CacheEntry { get; set; }

        public OwSchedulerEntry SchedulerEntry { get; set; }

        /// <summary>
        /// 保存使用的上下文。
        /// </summary>
        public DbContext Context { get; set; }
    }

    /// <summary>
    /// EF实体类缓存管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class DataObjectManager : IDisposable
    {
        #region 构造函数及相关

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DataObjectManager()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="cache"></param>
        /// <param name="ss"></param>
        /// <param name="service"></param>
        public DataObjectManager(IOptions<DataObjectManagerOptions> options, ILogger<DataObjectManager> logger, OwServerMemoryCache cache,
            IEnumerable<IHostedService> ss, IServiceProvider service)
        {
            Options = options.Value;
            _Logger = logger;
            _Cache = cache;
            _Service = service;
            _Scheduler = ss.First(c => c is OwScheduler) as OwScheduler;
        }

        #endregion 构造函数及相关

        private DataObjectManagerOptions _Options;
        /// <summary>
        /// 配置数据。
        /// </summary>
        public DataObjectManagerOptions Options { get => _Options; internal set => _Options = value; }

        private ILogger<DataObjectManager> _Logger;

        OwScheduler _Scheduler;
        /// <summary>
        /// 任务计划对象。
        /// </summary>
        public OwScheduler Scheduler { get => _Scheduler; }

        OwServerMemoryCache _Cache;
        /// <summary>
        /// 缓存对象。
        /// </summary>
        public OwServerMemoryCache Cache { get => _Cache; }

        /// <summary>
        /// 使用的根服务容器。
        /// </summary>
        IServiceProvider _Service;

        /// <summary>
        /// 
        /// </summary>
        public IServiceProvider Service => _Service;

        public bool TryEnterKey(object key, TimeSpan timeout) => _Cache.TryEnterKey(key, timeout);
        public void ExitKey(object key) => _Cache.ExitKey(key);

        public void TryAdd(object key, object value)
        {

        }

        public void TryRemove(object key) { }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="key"></param>
        /// <param name="predicate"></param>
        /// <param name="result"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool GetOrLoad<TContext, TSource>(object key, Func<TSource, bool> predicate, out TSource result, out TContext context)
            where TContext : DbContext where TSource : class
        {
            using var dwKey = DisposeHelper.Create(TryEnterKey, ExitKey, key, Options.DefaultLockTimeout);
            if (dwKey.IsEmpty)
            {
                result = null; context = default;
                return false;
            }

            if (_Cache.TryGetValue(key, out var obj))   //若找到缓存项
            {
                result = (TSource)obj; context = default;
                return true;
            }
            //加载
            var db = _Service.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext();
            var value = db.Set<TSource>().FirstOrDefault(predicate);
            if (value is null)   //若没有找到
            {
                result = default; context = default;
                return false;
            }
            result = value;
            //设置缓存
            _Cache.Set(key, value);
            //设置任务项
            _Scheduler.TryAdd(key, new OwSchedulerEntry()
            {
                Period = Timeout.InfiniteTimeSpan,
                State = db,
                TaskCallback = (cKey, cState) =>
                {
                    var db = (DbContext)cState;
                    db.SaveChanges();
                    return true;
                }
            });
            context = db;
            return true;
        }

        #region IDisposable接口及相关

        private bool _Disposed;

        protected bool Disposed => _Disposed;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Cache = null;
                _Scheduler = null;
                _Logger = null;
                _Service = null;
                _Disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_Disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        #endregion IDisposable接口及相关

    }
}
