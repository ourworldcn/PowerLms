/*
 * 项目：OwBaseCore | 模块：IMemoryCache 高级特性扩展
 * 功能：提供缓存Key的引用计数、优先级驱逐回调和依赖管理功能
 * 技术要点：引用计数、优先级队列、取消令牌、Interlocked原子操作、延迟构造、DisposeHelper零分配、双重检查锁
 * 作者：zc | 创建：2025-01-19 | 修改：2025-01-31 增加取消令牌支持
 */
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// IMemoryCache 高级特性扩展，提供引用计数、优先级驱逐回调和依赖管理功能。
    /// </summary>
    [Guid(GuidString)]
    public static class OwCacheExtensions
    {
        public const string GuidString = "A3B7C1D5-8E2F-4A9B-B6C3-7D4E9F1A2B5C";
        private const string CacheEntryStateMapKey = $"CacheEntryStateMap.{GuidString}";

        private sealed class CacheEntryState
        {
            private int _refCount;
            private PriorityQueue<PostEvictionCallbackRegistration, int> _callbackQueue;
            private CancellationTokenSource _cancellationTokenSource;
            public bool IsCallbackRegistered;

            private static readonly Func<PriorityQueue<PostEvictionCallbackRegistration, int>> s_queueFactory =
                   static () => new PriorityQueue<PostEvictionCallbackRegistration, int>();
            private static readonly Func<CancellationTokenSource> s_ctsFactory =
           static () => new CancellationTokenSource();

            public void AddRef() => Interlocked.Increment(ref _refCount);
            public void Release() => Interlocked.Decrement(ref _refCount);
            public int RefCount => Volatile.Read(ref _refCount);

            public PriorityQueue<PostEvictionCallbackRegistration, int> Queue =>
                LazyInitializer.EnsureInitialized(ref _callbackQueue, s_queueFactory);
            public bool HasQueue => Volatile.Read(ref _callbackQueue) != null;

            /// <summary>获取或创建取消令牌源</summary>
            public CancellationTokenSource CancellationTokenSource =>
                LazyInitializer.EnsureInitialized(ref _cancellationTokenSource, s_ctsFactory);

            /// <summary>检查是否已创建取消令牌源</summary>
            public bool HasCancellationTokenSource => Volatile.Read(ref _cancellationTokenSource) != null;
        }

        private static readonly Func<object, CacheEntryState> s_stateFactory = static _ => new CacheEntryState();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ConcurrentDictionary<object, CacheEntryState> GetCacheEntryStateMap(this IMemoryCache cache) =>
            cache.GetOrCreate(CacheEntryStateMapKey, static e =>
            {
                // ✅ 状态映射字典本身也需要启用优先级驱逐回调
                // 注意：这里不能调用 EnablePriorityEvictionCallback，因为会导致循环依赖
                // StateMap 本身设置为 NeverRemove，不需要额外的驱逐回调
                e.SetPriority(CacheItemPriority.NeverRemove);
                return new ConcurrentDictionary<object, CacheEntryState>();
            });

        /// <summary>获取或创建状态并加锁，保证返回有效状态</summary>
        private static DisposeHelper<CacheEntryState> GetOrCreateAndLockState(IMemoryCache cache, object key)
        {
            var stateMap = cache.GetCacheEntryStateMap();
            while (true)
            {
                stateMap.GetOrAdd(key, s_stateFactory);
                var lockedState = stateMap.GetAndLock(key);
                if (lockedState.State != null)
                    return lockedState;
            }
        }

        #region 引用计数管理

        /// <summary>
        /// 为指定键增加引用计数（+1），返回 DisposeHelper，Dispose 时自动减少计数（-1）。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <param name="key">缓存键</param>
        /// <returns>DisposeHelper，Dispose 时自动减少引用计数</returns>
        /// <remarks>
        /// <para><strong>语义</strong>：引用计数>0表示缓存项仍在内存中活跃，即使缓存中已无法获取。</para>
        /// <para><strong>创建者</strong>：调用但不释放返回值，维持长期引用。</para>
        /// <para><strong>使用者</strong>：使用 using 语句自动释放，临时引用。</para>
        /// <para><strong>跨线程安全</strong>：支持不同线程增减引用，使用Interlocked原子操作保证线程安全。</para>
        /// </remarks>
        public static DisposeHelper<object> AddKeyRef(this IMemoryCache cache, object key)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);
            using (var lockedState = GetOrCreateAndLockState(cache, key))
            {
                lockedState.State.AddRef();
                return new DisposeHelper<object>(static s => ((CacheEntryState)s).Release(), lockedState.State);
            }
        }

        /// <summary>
        /// 手动释放指定键的一个引用（引用计数-1）。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <param name="key">缓存键</param>
        /// <remarks>通常应该使用 using(cache.AddKeyRef(key)) 自动管理，此方法用于特殊场景。</remarks>
        public static void ReleaseKeyRef(this IMemoryCache cache, object key)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);
            if (cache.GetCacheEntryStateMap().TryGetValue(key, out var state))
                state.Release();
        }

        /// <summary>
        /// 获取指定键的当前引用计数。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <param name="key">缓存键</param>
        /// <returns>当前引用计数，键不存在返回0</returns>
        public static int GetKeyRefCount(this IMemoryCache cache, object key)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);
            return cache.GetCacheEntryStateMap().TryGetValue(key, out var state) ? state.RefCount : 0;
        }

        #endregion

        #region 优先级驱逐回调

        /// <summary>
        /// 为缓存条目启用优先级驱逐回调（自动持有引用计数）。
        /// </summary>
        /// <param name="entry">缓存条目</param>
        /// <param name="cache">缓存实例</param>
        /// <param name="registration">驱逐回调注册信息（可选）</param>
        /// <param name="priority">优先级（值越小优先级越高，默认10）</param>
        /// <returns>传入的缓存条目（支持链式调用）</returns>
        /// <remarks>
        /// <para>此方法会自动增加该键的引用计数（+1），标记缓存项在内存中活跃。</para>
        /// <para>只有当缓存项被驱逐且引用计数归零时，驱逐回调才会真正执行清理操作。</para>
        /// <para>使用内部锁保护回调队列的并发安全。</para>
        /// </remarks>
        public static ICacheEntry EnablePriorityEvictionCallback(this ICacheEntry entry, IMemoryCache cache, PostEvictionCallbackRegistration registration = null, int priority = 10)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(cache);
            using (var lockedState = GetOrCreateAndLockState(cache, entry.Key))
            {
                var state = lockedState.State;
                if (!state.IsCallbackRegistered)
                {
                    state.AddRef();
                    entry.RegisterPostEvictionCallback(ExecuteCallbacks, cache);
                    state.IsCallbackRegistered = true;
                }
                if (registration is not null)
                    state.Queue.Enqueue(registration, priority);
            }
            return entry;
        }

        /// <summary>为指定键注册优先级驱逐回调。</summary>
        /// <param name="cache">缓存实例</param>
        /// <param name="key">缓存键</param>
        /// <param name="registration">驱逐回调注册信息</param>
        /// <param name="priority">优先级（值越小优先级越高，默认10）</param>
        /// <exception cref="InvalidOperationException">当指定的缓存键未启用优先级驱逐回调机制时抛出</exception>
        public static void RegisterPriorityEvictionCallback(this IMemoryCache cache, object key, PostEvictionCallbackRegistration registration, int priority = 10)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(registration);
            using (var lockedState = cache.GetCacheEntryStateMap().GetAndLock(key))
            {
                if (lockedState.State == null)
                    throw new InvalidOperationException($"缓存键 '{key}' 未启用优先级驱逐回调机制。请先调用 EnablePriorityEvictionCallback 启用。");
                lockedState.State.Queue.Enqueue(registration, priority);
            }
        }

        private static void ExecuteCallbacks(object key, object value, EvictionReason reason, object state)
        {
            if (state is not IMemoryCache cache) return;
            using var lockedState = cache.GetCacheEntryStateMap().GetAndLock(key);
            if (lockedState.State == null) return;
            var entryState = lockedState.State;

            // 释放引用计数
            entryState.Release();

            // 如果没有队列，清理资源后返回
            if (!entryState.HasQueue)
            {
                if (entryState.RefCount <= 0)
                {
                    // 清理取消令牌源（如果存在）
                    if (entryState.HasCancellationTokenSource)
                    {
                        try
                        {
                            entryState.CancellationTokenSource?.Dispose();
                        }
                        catch { }
                    }
                    cache.GetCacheEntryStateMap().TryRemove(key, out _);
                }
                return;
            }

            // ✅ 执行优先级队列中的所有回调（包括取消令牌回调）
            List<Exception> exceptions = null;
            while (entryState.Queue.Count > 0)
            {
                var cb = entryState.Queue.Dequeue();
                try
                {
                    cb.EvictionCallback(key, value, reason, cb.State);
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            // 清理资源
            if (entryState.RefCount <= 0)
            {
                // 清理取消令牌源（如果存在）
                if (entryState.HasCancellationTokenSource)
                {
                    try
                    {
                        entryState.CancellationTokenSource?.Dispose();
                    }
                    catch { }
                }
                cache.GetCacheEntryStateMap().TryRemove(key, out _);
            }

            if (exceptions is not null)
                throw new AggregateException("优先级驱逐回调执行时发生异常", exceptions);
        }

        #endregion

        #region 取消令牌管理

        /// <summary>
        /// 获取或创建指定缓存键的取消令牌源。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <param name="key">缓存键</param>
        /// <returns>与键关联的取消令牌源</returns>
        /// <remarks>
        /// <para>此方法总是返回有效的取消令牌源，无论是否启用了优先级驱逐。</para>
        /// <para>如果已启用优先级驱逐回调（通过 EnablePriorityEvictionCallback），则会自动注册取消回调，确保缓存项被驱逐时令牌被取消。</para>
        /// <para>如果未启用优先级驱逐回调，令牌仍然可用，但不会在驱逐时自动取消。</para>
        /// <para>取消回调的优先级为 1024，确保在大多数用户回调之后执行。</para>
        /// <para>返回的令牌源由缓存基础设施管理，调用者不应手动 Dispose。</para>
        /// </remarks>
        public static CancellationTokenSource GetCancellationTokenSource(this IMemoryCache cache, object key)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);
            using var lockedState = GetOrCreateAndLockState(cache, key);
            var state = lockedState.State;

            // ✅ 如果已启用优先级回调且尚未注册取消令牌回调，则注册
            if (state.IsCallbackRegistered && !state.HasCancellationTokenSource)
            {
                // ✅ 使用标准 API 注册取消令牌回调
                // 优先级 1024：让大多数用户回调（优先级 0-1023）先执行
                // 这样用户回调可以依赖令牌尚未取消的状态进行清理
                try
                {
                    cache.RegisterPriorityEvictionCallback(key, new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = static (key, value, reason, state) =>
                        {
                            // ✅ 在优先级回调中取消令牌
                            if (state is CancellationTokenSource cts && !cts.IsCancellationRequested)
                            {
                                try
                                {
                                    cts.Cancel(); // 同步取消令牌
                                }
                                catch { }
                            }
                        },
                        State = state.CancellationTokenSource // 传递令牌源作为状态
                    }, priority: 1024); // 优先级 1024，在大多数用户回调之后执行
                }
                catch (InvalidOperationException)
                {
                    // ⚠️ 理论上不应该发生（因为已检查 IsCallbackRegistered）
                    // 但如果发生了，静默处理，不影响令牌的返回
                }
            }

            // ✅ 总是返回令牌源，无论是否启用了优先级回调
            // 如果未启用，令牌仍然可用，只是不会自动取消
            return state.CancellationTokenSource;
        }

        #endregion

        #region ID-Key 转换工具

        /// <summary>
        /// Id在缓存键值中的长度，标准GUID字符串表示为36个字符。
        /// </summary>
        public const int IdKeyLength = 36;

        /// <summary>
        /// 从缓存键中提取GUID标识符。
        /// </summary>
        /// <param name="key">缓存键字符串</param>
        /// <param name="prefix">预期的前缀，为null则不会验证前缀是否合法</param>
        /// <returns>提取的GUID，如果格式不合法或前缀不匹配则返回null</returns>
        public static Guid? GetIdFromCacheKey(string key, string prefix = null)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            // 如果有前缀，检查键是否以前缀开头
            if (prefix is not null)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    OwHelper.SetLastErrorAndMessage(400, $"格式错误：前缀不匹配，期望'{prefix}'");
                    return null;
                }

                // 移除前缀，使用Span提高性能
                key = key[prefix.Length..];
            }

            // 确保剩余部分至少等于GUID的长度
            if (key.Length < IdKeyLength)
            {
                OwHelper.SetLastErrorAndMessage(400, $"格式错误：GUID部分长度不足，需要{IdKeyLength}个字符");
                return null;
            }

            // 提取GUID部分（取前IdKeyLength个字符）
            var guidSpan = key.AsSpan(0, IdKeyLength);

            // 尝试解析GUID，使用Span版本提高性能
            if (!Guid.TryParse(guidSpan, out var id))
            {
                OwHelper.SetLastErrorAndMessage(400, "格式错误：无效的GUID格式");
                return null;
            }

            return id;
        }

        /// <summary>
        /// 根据类型和缓存键提取GUID标识符。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="key">缓存键字符串</param>
        /// <param name="prefix">预期的前缀，默认为类型名称加点</param>
        /// <returns>提取的GUID，如果格式不合法或前缀不匹配则返回null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid? GetIdFromCacheKey<T>(string key, string prefix = null)
        {
            // 如果未提供前缀，使用类型名称作为前缀
            prefix ??= $"{typeof(T).Name}.";
            return GetIdFromCacheKey(key, prefix);
        }

        /// <summary>
        /// 根据前缀和GUID构建缓存键。
        /// </summary>
        /// <param name="id">GUID标识符</param>
        /// <param name="prefix">前缀</param>
        /// <returns>格式化的缓存键</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetCacheKeyFromId(Guid id, string prefix = null)
        {
            // 使用标准格式（含连字符）生成GUID字符串
            return $"{prefix ?? string.Empty}{id:D}";
        }

        /// <summary>
        /// 根据类型、前缀和GUID构建缓存键。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="id">GUID标识符</param>
        /// <param name="prefix">前缀，默认为类型名称加点</param>
        /// <returns>格式化的缓存键</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetCacheKeyFromId<T>(Guid id, string prefix = null)
        {
            // 如果未提供前缀，使用类型名称作为前缀
            prefix ??= $"{typeof(T).Name}.";
            return GetCacheKeyFromId(id, prefix);
        }

        #endregion
    }
}
