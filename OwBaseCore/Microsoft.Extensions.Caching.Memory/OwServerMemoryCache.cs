/*
 * 利用开源的MemoryCache代码，稍微修改，实现了服务器专用的缓存类。
 */
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Memory
{
    public class OwServerCacheOptions : IOptions<OwServerCacheOptions>
    {
        /// <summary>
        /// 设置或获取锁定键的回调。应支持递归与<see cref="ExitCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.TryEnter(object, TimeSpan)"/>。
        /// </summary>
        public Func<object, TimeSpan, bool> TryEnterCallback { get; set; } = SingletonLocker.TryEnter;

        /// <summary>
        /// 设置或获取释放键的回调。应支持递归与<see cref="TryEnterCallback"/>配对使用。
        /// 默认值是<see cref="SingletonLocker.Exit(object)"/>。
        /// </summary>
        public Action<object> ExitCallback { get; set; } = SingletonLocker.Exit;

        /// <summary>
        /// 确定当前线程是否保留指定键上的锁。
        /// 默认值是<see cref="SingletonLocker.IsEntered(object)"/>
        /// </summary>
        public Func<object, bool> IsEnteredCallback { get; set; } = SingletonLocker.IsEntered;

        /// <summary>
        /// 默认的锁定超时时间。
        /// </summary>
        /// <value>默认值:3秒。</value>
        public TimeSpan EnterKeyTimeout { get; set; } = TimeSpan.FromSeconds(3);

        private long? _sizeLimit;
        private double _compactionPercentage = 0.05;

        public ISystemClock Clock { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items.
        /// </summary>
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the maximum size of the cache.
        /// </summary>
        public long? SizeLimit
        {
            get => _sizeLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
                }

                _sizeLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets the amount to compact the cache by when the maximum size is exceeded.
        /// </summary>
        public double CompactionPercentage
        {
            get => _compactionPercentage;
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be between 0 and 1 inclusive.");
                }

                _compactionPercentage = value;
            }
        }

        OwServerCacheOptions IOptions<OwServerCacheOptions>.Value
        {
            get { return this; }
        }
    }

    internal static class CacheEntryHelper
    {
        private static readonly AsyncLocal<OwServerCacheEntry> _current = new AsyncLocal<OwServerCacheEntry>();

        internal static OwServerCacheEntry Current
        {
            get => _current.Value;
            private set => _current.Value = value;
        }

        internal static OwServerCacheEntry EnterScope(OwServerCacheEntry current)
        {
            OwServerCacheEntry previous = Current;
            Current = current;
            return previous;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="previous"></param>
        internal static void ExitScope(OwServerCacheEntry current, OwServerCacheEntry previous)
        {
            Debug.Assert(Current == current, "Entries disposed in invalid order");
            Current = previous;
        }
    }

    internal sealed partial class OwServerCacheEntry : ICacheEntry
    {
        // this type exists just to reduce average OwServerCacheEntry size
        // which typically is not using expiration tokens or callbacks
        private sealed class CacheEntryTokens
        {
            private List<IChangeToken> _expirationTokens;
            private List<IDisposable> _expirationTokenRegistrations;
            private List<PostEvictionCallbackRegistration> _postEvictionCallbacks; // this is not really related to tokens, but was moved here to shrink typical OwServerCacheEntry size

            internal List<IChangeToken> ExpirationTokens => _expirationTokens ??= new List<IChangeToken>();
            internal List<PostEvictionCallbackRegistration> PostEvictionCallbacks => _postEvictionCallbacks ??= new List<PostEvictionCallbackRegistration>();

            internal void AttachTokens(OwServerCacheEntry cacheEntry)
            {
                if (_expirationTokens != null)
                {
                    lock (this)
                    {
                        for (int i = 0; i < _expirationTokens.Count; i++)
                        {
                            IChangeToken expirationToken = _expirationTokens[i];
                            if (expirationToken.ActiveChangeCallbacks)
                            {
                                _expirationTokenRegistrations ??= new List<IDisposable>(1);
                                IDisposable registration = expirationToken.RegisterChangeCallback(ExpirationCallback, cacheEntry);
                                _expirationTokenRegistrations.Add(registration);
                            }
                        }
                    }
                }
            }

            internal bool CheckForExpiredTokens(OwServerCacheEntry cacheEntry)
            {
                if (_expirationTokens != null)
                {
                    for (int i = 0; i < _expirationTokens.Count; i++)
                    {
                        IChangeToken expiredToken = _expirationTokens[i];
                        if (expiredToken.HasChanged)
                        {
                            cacheEntry.SetExpired(EvictionReason.TokenExpired);
                            return true;
                        }
                    }
                }
                return false;
            }

            internal bool CanPropagateTokens() => _expirationTokens != null;

            internal void PropagateTokens(OwServerCacheEntry parentEntry)
            {
                if (_expirationTokens != null)
                {
                    lock (this)
                    {
                        lock (parentEntry.GetOrCreateTokens())
                        {
                            foreach (IChangeToken expirationToken in _expirationTokens)
                            {
                                parentEntry.AddExpirationToken(expirationToken);
                            }
                        }
                    }
                }
            }

            internal void DetachTokens()
            {
                // _expirationTokenRegistrations is not checked for null, because AttachTokens might initialize it under lock
                // instead we are checking for _expirationTokens, because if they are not null, then _expirationTokenRegistrations might also be not null
                if (_expirationTokens != null)
                {
                    lock (this)
                    {
                        List<IDisposable> registrations = _expirationTokenRegistrations;
                        if (registrations != null)
                        {
                            _expirationTokenRegistrations = null;
                            for (int i = 0; i < registrations.Count; i++)
                            {
                                IDisposable registration = registrations[i];
                                registration.Dispose();
                            }
                        }
                    }
                }
            }

            internal void InvokeEvictionCallbacks(OwServerCacheEntry cacheEntry)
            {
                if (_postEvictionCallbacks != null)
                {
                    Task.Factory.StartNew(state => InvokeCallbacks((OwServerCacheEntry)state), cacheEntry,
                        CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
            }

            private static void InvokeCallbacks(OwServerCacheEntry entry)
            {
                List<PostEvictionCallbackRegistration> callbackRegistrations = Interlocked.Exchange(ref entry._tokens._postEvictionCallbacks, null);

                if (callbackRegistrations == null)
                {
                    return;
                }

                for (int i = 0; i < callbackRegistrations.Count; i++)
                {
                    PostEvictionCallbackRegistration registration = callbackRegistrations[i];

                    try
                    {
                        registration.EvictionCallback?.Invoke(entry.Key, entry.Value, entry.EvictionReason, registration.State);
                    }
                    catch (Exception e)
                    {
                        // This will be invoked on a background thread, don't let it throw.
                        entry._cache._logger.LogError(e, "EvictionCallback invoked failed");
                    }
                }
            }
        }

        // this type exists just to reduce OwServerCacheEntry size by replacing many enum & boolean fields with one of a size of Int32
        private struct CacheEntryState
        {
            private byte _flags;
            private byte _evictionReason;
            private byte _priority;

            internal CacheEntryState(CacheItemPriority priority) : this() => _priority = (byte)priority;

            internal bool IsDisposed
            {
                get => ((Flags)_flags & Flags.IsDisposed) != 0;
                set => SetFlag(Flags.IsDisposed, value);
            }

            internal bool IsExpired
            {
                get => ((Flags)_flags & Flags.IsExpired) != 0;
                set => SetFlag(Flags.IsExpired, value);
            }

            internal bool IsValueSet
            {
                get => ((Flags)_flags & Flags.IsValueSet) != 0;
                set => SetFlag(Flags.IsValueSet, value);
            }

            internal EvictionReason EvictionReason
            {
                get => (EvictionReason)_evictionReason;
                set => _evictionReason = (byte)value;
            }

            internal CacheItemPriority Priority
            {
                get => (CacheItemPriority)_priority;
                set => _priority = (byte)value;
            }

            private void SetFlag(Flags option, bool value) => _flags = (byte)(value ? (_flags | (byte)option) : (_flags & ~(byte)option));

            [Flags]
            private enum Flags : byte
            {
                Default = 0,
                IsValueSet = 1 << 0,
                IsExpired = 1 << 1,
                IsDisposed = 1 << 2,
            }
        }

        // this type exists just to reduce average OwServerCacheEntry size
        // which typically is not using expiration tokens or callbacks
        private static readonly Action<object> ExpirationCallback = ExpirationTokensExpired;

        private readonly OwServerMemoryCache _cache;

        private CacheEntryTokens _tokens; // might be null if user is not using the tokens or callbacks
        private TimeSpan? _absoluteExpirationRelativeToNow;
        private TimeSpan? _slidingExpiration;
        private long? _size;
        private OwServerCacheEntry _previous; // this field is not null only before the entry is added to the cache
        private object _value;
        private CacheEntryState _state;

        internal OwServerCacheEntry(object key, OwServerMemoryCache memoryCache)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _previous = CacheEntryHelper.EnterScope(this);
            _state = new CacheEntryState(CacheItemPriority.Normal);
        }

        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow
        {
            get => _absoluteExpirationRelativeToNow;
            set
            {
                // this method does not set AbsoluteExpiration as it would require calling Clock.UtcNow twice:
                // once here and once in OwServerMemoryCache.SetEntry

                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(AbsoluteExpirationRelativeToNow),
                        value,
                        "The relative expiration value must be positive.");
                }

                _absoluteExpirationRelativeToNow = value;
            }
        }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get => _slidingExpiration;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(SlidingExpiration),
                        value,
                        "The sliding expiration value must be positive.");
                }

                _slidingExpiration = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="IChangeToken"/> instances which cause the cache entry to expire.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens => GetOrCreateTokens().ExpirationTokens;

        /// <summary>
        /// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
        /// </summary>
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => GetOrCreateTokens().PostEvictionCallbacks;

        /// <summary>
        /// Gets or sets the priority for keeping the cache entry in the cache during a
        /// memory pressure triggered cleanup. The default is <see cref="CacheItemPriority.Normal"/>.
        /// </summary>
        public CacheItemPriority Priority { get => _state.Priority; set => _state.Priority = value; }

        /// <summary>
        /// Gets or sets the size of the cache entry value.
        /// </summary>
        public long? Size
        {
            get => _size;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
                }

                _size = value;
            }
        }

        public object Key { get; private set; }

        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                _state.IsValueSet = true;
            }
        }

        internal DateTimeOffset LastAccessed { get; set; }

        internal EvictionReason EvictionReason { get => _state.EvictionReason; private set => _state.EvictionReason = value; }

        public void Dispose()
        {
            if (!_state.IsDisposed)
            {
                using var dw = DisposeHelper.Create(_cache.TryEnterKey, _cache.ExitKey, Key, _cache.Options.EnterKeyTimeout);
                if (dw.IsEmpty) //若无法锁定
                    throw new TimeoutException();   //TODO 当前这种设计模式无法避免在Dispose中引发异常

                _state.IsDisposed = true;

                CacheEntryHelper.ExitScope(this, _previous);

                // Don't commit or propagate options if the OwServerCacheEntry Value was never set.
                // We assume an exception occurred causing the caller to not set the Value successfully,
                // so don't use this entry.
                if (_state.IsValueSet)
                {
                    _cache.SetEntry(this);

                    if (_previous != null && CanPropagateOptions())
                    {
                        PropagateOptions(_previous);
                    }
                }

                _previous = null; // we don't want to root unnecessary objects
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        internal bool CheckExpired(in DateTimeOffset now)
            => _state.IsExpired
                || CheckForExpiredTime(now)
                || (_tokens != null && _tokens.CheckForExpiredTokens(this));

        internal void SetExpired(EvictionReason reason)
        {
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            _state.IsExpired = true;
            _tokens?.DetachTokens();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        private bool CheckForExpiredTime(in DateTimeOffset now)
        {
            if (!AbsoluteExpiration.HasValue && !_slidingExpiration.HasValue)
            {
                return false;
            }

            return FullCheck(now);

            bool FullCheck(in DateTimeOffset offset)
            {
                if (AbsoluteExpiration.HasValue && AbsoluteExpiration.Value <= offset)
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }

                if (_slidingExpiration.HasValue
                    && (offset - LastAccessed) >= _slidingExpiration)
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }

                return false;
            }
        }

        internal void AttachTokens() => _tokens?.AttachTokens(this);

        private static void ExpirationTokensExpired(object obj)
        {
            // start a new thread to avoid issues with callbacks called from RegisterChangeCallback
            Task.Factory.StartNew(state =>
            {
                var entry = (OwServerCacheEntry)state;
                entry.SetExpired(EvictionReason.TokenExpired);
                entry._cache.EntryExpired(entry);
            }, obj, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        internal void InvokeEvictionCallbacks() => _tokens?.InvokeEvictionCallbacks(this);

        // this simple check very often allows us to avoid expensive call to PropagateOptions(CacheEntryHelper.Current)
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        internal bool CanPropagateOptions() => (_tokens != null && _tokens.CanPropagateTokens()) || AbsoluteExpiration.HasValue;

        internal void PropagateOptions(OwServerCacheEntry parent)
        {
            if (parent == null)
            {
                return;
            }

            // Copy expiration tokens and AbsoluteExpiration to the cache entries hierarchy.
            // We do this regardless of it gets cached because the tokens are associated with the value we'll return.
            _tokens?.PropagateTokens(parent);

            if (AbsoluteExpiration.HasValue)
            {
                if (!parent.AbsoluteExpiration.HasValue || AbsoluteExpiration < parent.AbsoluteExpiration)
                {
                    parent.AbsoluteExpiration = AbsoluteExpiration;
                }
            }
        }

        private CacheEntryTokens GetOrCreateTokens()
        {
            if (_tokens != null)
            {
                return _tokens;
            }

            CacheEntryTokens result = new CacheEntryTokens();
            return Interlocked.CompareExchange(ref _tokens, result, null) ?? result;
        }
    }

    /// <summary>
    /// An implementation of <see cref="IMemoryCache"/> using a dictionary to
    /// store its entries.
    /// 针对每个项操作都会对其键值加锁，对高并发而言，不应有多个线程试图访问同一个键下的项。这样可以避免锁的碰撞。对基本单线程操作而言，此类性能较低。
    /// 此类公共成员（除嵌套类）可以多线程并发调用。
    /// </summary>
    public class OwServerMemoryCache : IMemoryCache
    {
        internal readonly ILogger _logger;

        private readonly OwServerCacheOptions _options;
        /// <summary>
        /// 获取配置信息，注意不可以更改，否则行为未知。
        /// </summary>
        internal OwServerCacheOptions Options => _options;

        private readonly ConcurrentDictionary<object, OwServerCacheEntry> _entries;

        private long _cacheSize;
        private DateTimeOffset _lastExpirationScan;

        #region 构造函数及相关

        /// <summary>
        /// Creates a new <see cref="OwServerMemoryCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        public OwServerMemoryCache(IOptions<OwServerCacheOptions> optionsAccessor)
            : this(optionsAccessor, NullLoggerFactory.Instance) { }

        /// <summary>
        /// Creates a new <see cref="OwServerMemoryCache"/> instance.
        /// </summary>
        /// <param name="optionsAccessor">The options of the cache.</param>
        /// <param name="loggerFactory">The factory used to create loggers.</param>
        public OwServerMemoryCache(IOptions<OwServerCacheOptions> optionsAccessor, ILoggerFactory loggerFactory)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = optionsAccessor.Value;
            _logger = loggerFactory.CreateLogger<OwServerMemoryCache>();

            _entries = new ConcurrentDictionary<object, OwServerCacheEntry>();

            _options.Clock ??= new OwSystemClock();

            _lastExpirationScan = _options.Clock.UtcNow;
        }

        #endregion 构造函数及相关

        #region 锁定及相关

        /// <summary>
        /// 锁定键。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool TryEnterKey(object key, TimeSpan timeout) => _options.TryEnterCallback(key, timeout);

        /// <summary>
        /// 锁定键。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout">省略或null表示使用默认超时:<see cref="OwServerCacheOptions.EnterKeyTimeout"/>。</param>
        /// <returns></returns>
        public bool TryEnterKey(object key, TimeSpan? timeout = null) => _options.TryEnterCallback(key, timeout ?? _options.EnterKeyTimeout);

        public void ExitKey(object key) => _options.ExitCallback(key);

        public bool IsEnteredKey(object key) => _options.IsEnteredCallback(key);

        #endregion 锁定及相关

        /// <summary>
        /// Cleans up the background collection events.
        /// </summary>
        ~OwServerMemoryCache() => Dispose(false);

        /// <summary>
        /// Gets the count of the current entries for diagnostic purposes.
        /// </summary>
        public int Count => _entries.Count;

        // internal for testing
        internal long Size { get => Interlocked.Read(ref _cacheSize); }

        private ICollection<KeyValuePair<object, OwServerCacheEntry>> EntriesCollection => _entries;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ICacheEntry CreateEntry(object key)
        {
            CheckDisposed();
            using var dw = DisposeHelper.Create(_options.TryEnterCallback, _options.ExitCallback, key, _options.EnterKeyTimeout);
            if (dw.IsEmpty) //若无法锁定
                throw new TimeoutException();

            ValidateCacheKey(key);

            return new OwServerCacheEntry(key, this);
        }

        internal void SetEntry(OwServerCacheEntry entry)
        {
            if (_Disposed)
            {
                // No-op instead of throwing since this is called during OwServerCacheEntry.Dispose
                return;
            }

            if (_options.SizeLimit.HasValue && !entry.Size.HasValue)
            {
                throw new InvalidOperationException(/*SR.Format(SR.CacheEntryHasEmptySize, nameof(entry.Size), nameof(_options.SizeLimit))*/);
            }

            DateTimeOffset utcNow = _options.Clock.UtcNow;

            DateTimeOffset? absoluteExpiration = null;
            if (entry.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow + entry.AbsoluteExpirationRelativeToNow;
            }
            else if (entry.AbsoluteExpiration.HasValue)
            {
                absoluteExpiration = entry.AbsoluteExpiration;
            }

            // Applying the option's absolute expiration only if it's not already smaller.
            // This can be the case if a dependent cache entry has a smaller value, and
            // it was set by cascading it to its parent.
            if (absoluteExpiration.HasValue)
            {
                if (!entry.AbsoluteExpiration.HasValue || absoluteExpiration.Value < entry.AbsoluteExpiration.Value)
                {
                    entry.AbsoluteExpiration = absoluteExpiration;
                }
            }

            // Initialize the last access timestamp at the time the entry is added
            entry.LastAccessed = utcNow;

            if (_entries.TryGetValue(entry.Key, out OwServerCacheEntry priorEntry))
            {
                priorEntry.SetExpired(EvictionReason.Replaced);
            }

            bool exceedsCapacity = UpdateCacheSizeExceedsCapacity(entry);

            if (!entry.CheckExpired(utcNow) && !exceedsCapacity)
            {
                bool entryAdded;
                if (priorEntry == null)
                {
                    // Try to add the new entry if no previous entries exist.
                    entryAdded = _entries.TryAdd(entry.Key, entry);
                }
                else
                {
                    // Try to update with the new entry if a previous entries exist.
                    entryAdded = _entries.TryUpdate(entry.Key, entry, priorEntry);

                    if (entryAdded)
                    {
                        if (_options.SizeLimit.HasValue)
                        {
                            // The prior entry was removed, decrease the by the prior entry's size
                            Interlocked.Add(ref _cacheSize, -priorEntry.Size.Value);
                        }
                    }
                    else
                    {
                        // The update will fail if the previous entry was removed after retrival.
                        // Adding the new entry will succeed only if no entry has been added since.
                        // This guarantees removing an old entry does not prevent adding a new entry.
                        entryAdded = _entries.TryAdd(entry.Key, entry);
                    }
                }

                if (entryAdded)
                {
                    entry.AttachTokens();
                }
                else
                {
                    if (_options.SizeLimit.HasValue)
                    {
                        // Entry could not be added, reset cache size
                        Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                    }
                    entry.SetExpired(EvictionReason.Replaced);
                    entry.InvokeEvictionCallbacks();
                }

                priorEntry?.InvokeEvictionCallbacks();
            }
            else
            {
                if (exceedsCapacity)
                {
                    // The entry was not added due to overcapacity
                    entry.SetExpired(EvictionReason.Capacity);

                    TriggerOvercapacityCompaction();
                }
                else
                {
                    if (_options.SizeLimit.HasValue)
                    {
                        // Entry could not be added due to being expired, reset cache size
                        Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                    }
                }

                entry.InvokeEvictionCallbacks();
                if (priorEntry != null)
                {
                    RemoveEntry(priorEntry);
                }
            }

            StartScanForExpiredItemsIfNeeded(utcNow);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetValue(object key, out object result)
        {
            ValidateCacheKey(key);
            CheckDisposed();

            using var dw = DisposeHelper.Create(_options.TryEnterCallback, _options.ExitCallback, key, _options.EnterKeyTimeout);
            if (dw.IsEmpty) //若无法锁定
                throw new TimeoutException();

            DateTimeOffset utcNow = _options.Clock.UtcNow;

            if (_entries.TryGetValue(key, out OwServerCacheEntry entry))
            {
                // Check if expired due to expiration tokens, timers, etc. and if so, remove it.
                // Allow a stale Replaced value to be returned due to concurrent calls to SetExpired during SetEntry.
                if (!entry.CheckExpired(utcNow) || entry.EvictionReason == EvictionReason.Replaced)
                {
                    entry.LastAccessed = utcNow;
                    result = entry.Value;

                    if (entry.CanPropagateOptions())
                    {
                        // When this entry is retrieved in the scope of creating another entry,
                        // that entry needs a copy of these expiration tokens.
                        entry.PropagateOptions(CacheEntryHelper.Current);
                    }

                    StartScanForExpiredItemsIfNeeded(utcNow);

                    return true;
                }
                else
                {
                    // TODO: For efficiency queue this up for batch removal
                    RemoveEntry(entry);
                }
            }

            StartScanForExpiredItemsIfNeeded(utcNow);

            result = null;
            return false;
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public void Remove(object key)
        {
            ValidateCacheKey(key);

            CheckDisposed();

            using var dw = DisposeHelper.Create(_options.TryEnterCallback, _options.ExitCallback, key, _options.EnterKeyTimeout);
            if (dw.IsEmpty) //若无法锁定
                throw new TimeoutException();

            if (_entries.TryRemove(key, out OwServerCacheEntry entry))
            {
                if (_options.SizeLimit.HasValue)
                {
                    Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                }

                entry.SetExpired(EvictionReason.Removed);
                entry.InvokeEvictionCallbacks();
            }

            StartScanForExpiredItemsIfNeeded(_options.Clock.UtcNow);
        }

        private void RemoveEntry(OwServerCacheEntry entry)
        {
            if (EntriesCollection.Remove(new KeyValuePair<object, OwServerCacheEntry>(entry.Key, entry)))
            {
                if (_options.SizeLimit.HasValue)
                {
                    Interlocked.Add(ref _cacheSize, -entry.Size.Value);
                }
                entry.InvokeEvictionCallbacks();
            }
        }

        internal void EntryExpired(OwServerCacheEntry entry)
        {
            // TODO: For efficiency consider processing these expirations in batches.
            RemoveEntry(entry);
            StartScanForExpiredItemsIfNeeded(_options.Clock.UtcNow);
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartScanForExpiredItemsIfNeeded(DateTimeOffset utcNow)
        {
            if (_options.ExpirationScanFrequency < utcNow - _lastExpirationScan)
            {
                ScheduleTask(utcNow);
            }

            void ScheduleTask(DateTimeOffset utcNow)
            {
                _lastExpirationScan = utcNow;
                Task.Factory.StartNew(state => ScanForExpiredItems((OwServerMemoryCache)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private /*static*/ void ScanForExpiredItems(OwServerMemoryCache cache)
        {
            DateTimeOffset now = cache._lastExpirationScan = cache._options.Clock.UtcNow;

            foreach (KeyValuePair<object, OwServerCacheEntry> item in cache._entries)
            {
                OwServerCacheEntry entry = item.Value;
                using var dw = DisposeHelper.Create(_options.TryEnterCallback, _options.ExitCallback, entry.Key, TimeSpan.Zero);
                if (dw.IsEmpty) //若无法锁定
                    continue;
                if (entry.CheckExpired(now))
                {
                    cache.RemoveEntry(entry);
                }
            }
        }

        private bool UpdateCacheSizeExceedsCapacity(OwServerCacheEntry entry)
        {
            if (!_options.SizeLimit.HasValue)
            {
                return false;
            }

            long newSize;   // = 0L;
            for (int i = 0; i < 100; i++)
            {
                long sizeRead = Interlocked.Read(ref _cacheSize);
                newSize = sizeRead + entry.Size.Value;

                if (newSize < 0 || newSize > _options.SizeLimit)
                {
                    // Overflow occurred, return true without updating the cache size
                    return true;
                }

                if (sizeRead == Interlocked.CompareExchange(ref _cacheSize, newSize, sizeRead))
                {
                    return false;
                }
            }

            return true;
        }

        private void TriggerOvercapacityCompaction()
        {
            _logger.LogDebug("Overcapacity compaction triggered");

            // Spawn background thread for compaction
            ThreadPool.QueueUserWorkItem(s => OvercapacityCompaction((OwServerMemoryCache)s), this);
        }

        [SuppressMessage("Usage", "CA2254:模板应为静态表达式", Justification = "<挂起>")]
        private static void OvercapacityCompaction(OwServerMemoryCache cache)
        {
            long currentSize = Interlocked.Read(ref cache._cacheSize);

            cache._logger.LogDebug($"Overcapacity compaction executing. Current size {currentSize}");

            double? lowWatermark = cache._options.SizeLimit * (1 - cache._options.CompactionPercentage);
            if (currentSize > lowWatermark)
            {
                cache.Compact(currentSize - (long)lowWatermark, entry => entry.Size.Value);
            }

            cache._logger.LogDebug($"Overcapacity compaction executed. New size {Interlocked.Read(ref cache._cacheSize)}");
        }

        /// Remove at least the given percentage (0.10 for 10%) of the total entries (or estimated memory?), according to the following policy:
        /// 1. Remove all expired items.
        /// 2. Bucket by CacheItemPriority.
        /// 3. Least recently used objects.
        /// ?. Items with the soonest absolute expiration.
        /// ?. Items with the soonest sliding expiration.
        /// ?. Larger objects - estimated by object graph size, inaccurate.
        public void Compact(double percentage)
        {
            int removalCountTarget = (int)(_entries.Count * percentage);
            Compact(removalCountTarget, _ => 1);
        }

        private void Compact(long removalSizeTarget, Func<OwServerCacheEntry, long> computeEntrySize)
        {
            var entriesToRemove = new List<OwServerCacheEntry>();
            // cache LastAccessed outside of the OwServerCacheEntry so it is stable during compaction
            var lowPriEntries = new List<CompactPriorityEntry>();
            var normalPriEntries = new List<CompactPriorityEntry>();
            var highPriEntries = new List<CompactPriorityEntry>();
            long removedSize = 0;

            // Sort items by expired & priority status
            DateTimeOffset now = _options.Clock.UtcNow;
            using var dw = DisposeHelper.Create(c =>
            {
                c.ForEach(entry => _options.ExitCallback(entry.Key));
            }, entriesToRemove);
            foreach (KeyValuePair<object, OwServerCacheEntry> item in _entries)
            {
                if (!_options.TryEnterCallback(item.Key, TimeSpan.Zero)) //若无法锁定
                    continue;
                OwServerCacheEntry entry = item.Value;
                if (entry.CheckExpired(now))
                {
                    entriesToRemove.Add(entry);
                    removedSize += computeEntrySize(entry);
                }
                else
                {
                    _options.ExitCallback(item.Key);
                    switch (entry.Priority)
                    {
                        case CacheItemPriority.Low:
                            lowPriEntries.Add(new CompactPriorityEntry(entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.Normal:
                            normalPriEntries.Add(new CompactPriorityEntry(entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.High:
                            highPriEntries.Add(new CompactPriorityEntry(entry, entry.LastAccessed));
                            break;
                        case CacheItemPriority.NeverRemove:
                            break;
                        default:
                            throw new NotSupportedException("Not implemented: " + entry.Priority);
                    }
                }
            }

            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, lowPriEntries);
            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, normalPriEntries);
            ExpirePriorityBucket(ref removedSize, removalSizeTarget, computeEntrySize, entriesToRemove, highPriEntries);

            foreach (OwServerCacheEntry entry in entriesToRemove)
            {
                RemoveEntry(entry);
            }
            // Policy:
            // 1. Least recently used objects.
            // ?. Items with the soonest absolute expiration.
            // ?. Items with the soonest sliding expiration.
            // ?. Larger objects - estimated by object graph size, inaccurate.
            /*static*/
            void ExpirePriorityBucket(ref long removedSize, long removalSizeTarget, Func<OwServerCacheEntry, long> computeEntrySize,
                List<OwServerCacheEntry> entriesToRemove, List<CompactPriorityEntry> priorityEntries)
            {
                // Do we meet our quota by just removing expired entries?
                if (removalSizeTarget <= removedSize)
                {
                    // No-op, we've met quota
                    return;
                }

                // Expire enough entries to reach our goal
                // TODO: Refine policy

                // LRU
                priorityEntries.Sort(static (e1, e2) => e1.LastAccessed.CompareTo(e2.LastAccessed));
                foreach (CompactPriorityEntry priorityEntry in priorityEntries)
                {
                    OwServerCacheEntry entry = priorityEntry.Entry;
                    if (!_options.TryEnterCallback(entry.Key, TimeSpan.Zero)) //若无法锁定
                        continue;
                    entry.SetExpired(EvictionReason.Capacity);
                    entriesToRemove.Add(entry);
                    removedSize += computeEntrySize(entry);

                    if (removalSizeTarget <= removedSize)
                    {
                        break;
                    }
                }
            }
        }

        // use a struct instead of a ValueTuple to avoid adding a new dependency
        // on System.ValueTuple on .NET Framework in a servicing release
        private readonly struct CompactPriorityEntry
        {
            public readonly OwServerCacheEntry Entry;
            public readonly DateTimeOffset LastAccessed;

            public CompactPriorityEntry(OwServerCacheEntry entry, DateTimeOffset lastAccessed)
            {
                Entry = entry;
                LastAccessed = lastAccessed;
            }
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
                _Disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (_Disposed)
            {
                Throw();
            }

            static void Throw() => throw new ObjectDisposedException(typeof(OwServerMemoryCache).FullName);
        }

        #endregion IDisposable接口及相关

        private static void ValidateCacheKey(object key)
        {
            if (key == null)
            {
                Throw();
            }

            static void Throw() => throw new ArgumentNullException(nameof(key));
        }
    }

    /// <summary>
    /// 封装扩展方法。
    /// </summary>
    public static class OwServerMemoryCacheExtensions
    {
    }
}