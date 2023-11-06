using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// 
    /// </summary>
    public class OwMemoryCacheOptions : IOptions<OwMemoryCacheOptions>
    {
        /// <summary>
        /// 设置或获取锁定键的回调。应支持递归与<see cref="UnlockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.TryEnter(object, TimeSpan)"/>。
        /// </summary>
        public Func<object, TimeSpan, bool> LockCallback { get; set; } = SingletonLocker.TryEnter;

        /// <summary>
        /// 设置或获取释放键的回调。应支持递归与<see cref="LockCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.Exit(object)"/>。
        /// </summary>
        public Action<object> UnlockCallback { get; set; } = SingletonLocker.Exit;

        /// <summary>
        /// 确定当前线程是否保留指定键上的锁。
        /// 默认值是<see cref="SingletonLocker.IsEntered(object)"/>
        /// </summary>
        public Func<object, bool> IsEnteredCallback { get; set; } = SingletonLocker.IsEntered;

        /// <summary>
        /// 默认的锁定超时时间。
        /// </summary>
        /// <value>默认值:3秒。</value>
        public TimeSpan DefaultLockTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items.
        /// </summary>
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

        double _compactionPercentage = 0.05;
        /// <summary>
        /// Gets or sets the amount to compact the cache by when the maximum size is exceeded.
        /// </summary>
        public double CompactionPercentage
        {
            get => _compactionPercentage;
            set
            {
                if (value is < 0 or > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be between 0 and 1 inclusive.");
                }

                _compactionPercentage = value;
            }
        }

        public OwMemoryCacheOptions Value => this;

    }

    /// <summary>
    /// 内存缓存的类。
    /// 针对每个项操作都会对其键值加锁，对高并发而言，不应有多个线程试图访问同一个键下的项。这样可以避免锁的碰撞。对基本单线程操作而言，此类性能较低。
    /// 此类公共成员（除嵌套类）可以多线程并发调用。3446377c-dad1-46d9-8717-bb4f420c1cc8
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwMemoryCache : IMemoryCache
    {

        /// <summary>
        /// 缓存项的配置信息类。
        /// </summary>
        public class OwMemoryCacheEntry : ICacheEntry
        {
            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="cache">指定所属缓存对象，在调用<see cref="Dispose"/>时可以加入该对象。</param>
            protected internal OwMemoryCacheEntry(object key, OwMemoryCache cache)
            {
                Key = key;
                Cache = cache;
            }

            /// <summary>
            /// 所属的缓存对象。
            /// </summary>
            public OwMemoryCache Cache { get; set; }

            #region ICacheEntry接口相关

            object _Key;

            public object Key { get => _Key; set => _Key = value; }

            public virtual object Value { get; set; }

            /// <summary>
            /// 未实装，不起作用。
            /// </summary>
            public DateTimeOffset? AbsoluteExpiration { get; set; }

            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

            public TimeSpan? SlidingExpiration { get; set; }

            /// <summary>
            /// 未实装不起作用。
            /// </summary>
            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            internal List<PostEvictionCallbackRegistration> _PostEvictionCallbacksLazyer;
            /// <summary>
            /// 所有的函数调用完毕才会解锁键对象。
            /// </summary>
            [NotNull]
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks
            {
                get => LazyInitializer.EnsureInitialized(ref _PostEvictionCallbacksLazyer);
            }

            /// <summary>
            /// 未实装，不起作用。
            /// </summary>
            public CacheItemPriority Priority { get; set; }

            /// <summary>
            /// 未实装，不起作用。
            /// </summary>
            public long? Size { get; set; }

            #region IDisposable接口相关

            bool _IsDisposed;
            /// <summary>
            /// 对象是否已经被处置，此类型特殊，被处置意味着已经加入到缓存配置表中，而非真的被处置。
            /// </summary>
            protected bool IsDisposed => _IsDisposed;

            /// <summary>
            /// 使此配置项加入或替换缓存对象。内部会试图锁定键。
            /// 在完成时自动调用<see cref="AddItemCore(ICacheEntry)"/>(在锁内)。
            /// </summary>
            /// <exception cref="TimeoutException">试图锁定键超时。</exception>
            public virtual void Dispose()
            {
                if (!_IsDisposed)
                {
                    using var dw = DisposeHelper.Create(Cache.TryEnter, Cache.Exit, Key, Cache.Options.DefaultLockTimeout);
                    if (dw.IsEmpty)
                        throw new TimeoutException();
                    var factEntity = Cache.AddOrUpdateEntryCore(this);
                    //factEntity.LastUseUtc = OwHelper.WorldClock;OwHelper
                    _IsDisposed = true;
                }
                //Cache.AddItemCore(this);
            }
            #endregion IDisposable接口相关

            #endregion ICacheEntry接口相关

            //internal Lazy<List<BeforeEvictionCallbackRegistration>> _BeforeEvictionCallbacksLazyer = new Lazy<List<BeforeEvictionCallbackRegistration>>(true);
            /// <summary>
            /// 获取或设置从缓存中即将逐出缓存项时将触发的回叫。
            /// 所有的函数调用完毕才会解锁键对象。
            /// 支持并发初始化，但返回集合本身不能支持并发。
            /// </summary>
            //public IList<BeforeEvictionCallbackRegistration> BeforeEvictionCallbacks => _BeforeEvictionCallbacksLazyer.Value;

            /// <summary>
            /// 最后一次使用的Utc时间。
            /// </summary>
            public DateTime LastUseUtc { get; internal set; } = OwHelper.WorldNow;

            /// <summary>
            /// 获取此配置项是否超期。
            /// </summary>
            /// <param name="utcNow"></param>
            /// <returns></returns>
            public virtual bool IsExpired(DateTime utcNow)
            {
                if (SlidingExpiration.HasValue && utcNow - LastUseUtc >= SlidingExpiration)
                    return true;
                if (AbsoluteExpiration.HasValue && utcNow >= AbsoluteExpiration)
                    return true;
                return false;
            }

        }

        /// <summary>
        /// 记录所有缓存项。
        /// 键是缓存项的键，值缓存配置项数据。
        /// </summary>
        ConcurrentDictionary<object, OwMemoryCacheEntry> _Items = new();

        /// <summary>
        /// 获取缓存内的所有内容。更改其中内容的结果未知。
        /// </summary>
        public IReadOnlyDictionary<object, OwMemoryCacheEntry> Items => _Items;

        OwMemoryCacheOptions _Options;
        /// <summary>
        /// 配置信息。
        /// </summary>
        /// <value>默认值是<see cref="OwMemoryCacheOptions"/>的默认对象。</value>
        public OwMemoryCacheOptions Options { get => _Options ??= new OwMemoryCacheOptions(); init => _Options = value; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="options"></param>
        public OwMemoryCache(IOptions<OwMemoryCacheOptions> options)
        {
            _Options = options.Value;
        }

        #region IMemoryCache接口及相关

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ICacheEntry CreateEntry(object key)
        {
            using var dw = DisposeHelper.Create(_Options.LockCallback, _Options.UnlockCallback, key, _Options.DefaultLockTimeout);
            if (dw.IsEmpty)
                return null;
            return CreateEntryCore(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public void Remove(object key)
        {
            using var dw = DisposeHelper.Create(_Options.LockCallback, _Options.UnlockCallback, key, _Options.DefaultLockTimeout);
            if (dw.IsEmpty)
                throw new TimeoutException();
            if (!_Items.TryGetValue(key, out var entry))
                throw new KeyNotFoundException();
            RemoveCore(entry, EvictionReason.Removed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public bool TryGetValue(object key, out object value)
        {
            using var dw = DisposeHelper.Create(_Options.LockCallback, _Options.UnlockCallback, key, _Options.DefaultLockTimeout);
            if (dw.IsEmpty)
                _ = OwHelper.GetLastError() switch
                {
                    0 => 0,
                    258 => throw new TimeoutException(),
                    _ => throw new InvalidOperationException(),
                };
            if (TryGetValueCore(key, out var entry))
            {
                entry.LastUseUtc = OwHelper.WorldNow;
                value = entry.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要则调用者需负责加/解锁。不会自动重置最后使用时间。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="entry">如果返回true则此处返回配置项。</param>
        /// <returns>true则返回指定键的配置项。false表示没有找到指定的键。</returns>
        protected virtual bool TryGetValueCore(object key, out OwMemoryCacheEntry entry)
        {
            entry = GetEntry(key);
            return entry is not null;
        }

        /// <summary>
        /// 调用此函数创建内部使用的配置项对象。
        /// 默认实现劲通过<see cref="OwMemoryCacheEntry(object, OwMemoryCache)"/>创建一个对象并返回。
        /// 非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual ICacheEntry CreateEntryCore(object key) => new OwMemoryCacheEntry(key, this);

        /// <summary>
        /// 以指定原因移除缓存项。
        /// 此函数会移除配置项后调用所有<see cref="OwMemoryCacheEntry.PostEvictionCallbacks"/>回调。但回在回调完成后才对键值解锁键。
        /// 回调的异常均被忽略。
        /// 派生类可以重载此函数。
        /// 非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="reason"></param>
        /// <returns>true=成功移除，false=没有找到指定键。</returns>
        protected virtual bool RemoveCore(OwMemoryCacheEntry entry, EvictionReason reason)
        {
            var result = _Items.TryRemove(entry.Key, out entry);
            try
            {
                if (result && entry._PostEvictionCallbacksLazyer is not null)
                    entry.PostEvictionCallbacks.SafeForEach(c => c.EvictionCallback?.Invoke(entry.Key, entry.Value, reason, c.State));
            }
            catch (Exception)
            {
            }
            return result;
        }

        #region IDisposable接口相关

        /// <summary>
        /// 如果对象已经被处置则抛出<see cref="ObjectDisposedException"/>异常。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
            if (_IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// 通过检测<see cref="OwHelper.GetLastError"/>返回值是否为258(WAIT_TIMEOUT)决定是否抛出异常<seealso cref="TimeoutException"/>。
        /// </summary>
        /// <param name="msg"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfTimeout(string msg)
        {
            if (OwHelper.GetLastError() == 258)
                throw new TimeoutException(msg);
        }

        /// <summary>
        /// 根据<see cref="OwHelper.GetLastError"/>返回值判断是否抛出锁定键超时的异常。
        /// </summary>
        /// <param name="key"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfLockKeyTimeout(object key)
        {
            if (OwHelper.GetLastError() == 258)
                throw new TimeoutException($"锁定键时超时，键:{key}");
        }


        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[DoesNotReturn]
        //static void Throw() => throw new ObjectDisposedException(typeof(LeafMemoryCache).FullName);

        private bool _IsDisposed;

        protected bool IsDisposed { get => _IsDisposed; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    //释放托管状态(托管对象)
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Options = null;
                _Items = null;
                _IsDisposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~LeafMemoryCache()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口相关

        #endregion IMemoryCache接口及相关

        /// <summary>
        /// 某一项加入缓存时被调用。
        /// 派生类可以重载此函数。非公有函数不会自动对键加锁，若需要调用者需要负责加/解锁。
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        protected virtual OwMemoryCacheEntry AddOrUpdateEntryCore(OwMemoryCacheEntry entry)
        {
            return _Items.AddOrUpdate(entry.Key, entry, (c1, c2) => entry);
        }

        /// <summary>
        /// 获取设置项数据，需要首先锁定键，解锁键将导致配置生效。
        /// </summary>
        /// <param name="key"></param>
        /// <returns>配置数据，如果不存在指定键，则返回null。</returns>
        /// <exception cref="InvalidOperationException">未锁定键，尽在调试状态下会检测。</exception>
        public OwMemoryCacheEntry GetEntry(object key)
        {
#if DEBUG
            if (!Options.IsEnteredCallback(key))
                throw new InvalidOperationException("要首先锁定键才能获取设置项。");
#endif
            return _Items.GetValueOrDefault(key);
        }

        #region 压缩及相关

        /// <summary>
        /// 压缩缓存数据。
        /// </summary>
        /// <param name="percentage">回收比例。</param>
        public void Compact()
        {
            ThrowIfDisposed();
            var last = Volatile.Read(ref _CompactTick);
            var now = OwHelper.WorldNow;
            if (now - new DateTime(last) < _Options.ExpirationScanFrequency)   //若最近已经压缩过
                return;
            if (Interlocked.CompareExchange(ref _CompactTick, now.Ticks, last) != last) //若已经被并发更改
                return;
            Compact(Math.Max((long)(_Items.Count * _Options.CompactionPercentage), 1));
        }

        /// <summary>
        /// 最后一次压缩的时间的刻度。
        /// </summary>
        long _CompactTick = OwHelper.WorldNow.Ticks;

        /// <summary>
        /// 压缩缓存。
        /// </summary>
        /// <param name="removalSizeTarget">最多驱逐多少项。</param>
        private void Compact(long removalSizeTarget)
        {
            //避免冲入,在 Options.ExpirationScanFrequency 是0的时候可能重入，但这种设置不正确。
            using var dwReenter = DisposeHelper.Create(Monitor.TryEnter, Monitor.Exit, _Items, TimeSpan.Zero);
            if (dwReenter.IsEmpty)
                return;
            long count = 0;
            var now = OwHelper.WorldNow;

            foreach (var key in _Items.Keys)
            {
                using var dw = DisposeHelper.Create(_Options.LockCallback, _Options.UnlockCallback, key, TimeSpan.Zero);
                if (dw.IsEmpty || !_Items.TryGetValue(key, out var entry))  //若未能锁定
                    continue;
                EvictionReason reason;
                if (IsExpired(entry, now))    //若超期
                    reason = EvictionReason.Expired;
                else //若无需驱逐
                    continue;
                try
                {
                    RemoveCore(entry, reason);
                }
                catch (Exception)
                {
                }
                if (++count >= removalSizeTarget)   //若已经完成任务
                    break;
            }
        }

        /// <summary>
        /// 是否超期。
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsExpired(OwMemoryCacheEntry entry, DateTime now)
        {
            if (now - entry.LastUseUtc >= entry.SlidingExpiration.GetValueOrDefault(TimeSpan.MaxValue))    //若相对超期
                return true;
            else if (now >= entry.AbsoluteExpiration.GetValueOrDefault(DateTime.MaxValue))   //若绝对超期
                return true;
            else
                return false;
        }

        #endregion 压缩及相关

        #region 锁定及相关

        /// <summary>
        /// 锁定指定键对象，以备进行操作。
        /// </summary>
        /// <param name="key">要锁定的键。</param>
        /// <param name="timeout">允许的最大的超时时间。</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool TryEnter(object key, TimeSpan timeout) => Options.LockCallback(key, timeout);

        /// <summary>
        /// 释放锁定的键。
        /// </summary>
        /// <param name="key">要释放的键。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Exit(object key) => Options.UnlockCallback(key);

        #endregion 锁定及相关
    }

}