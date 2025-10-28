/*
 * 项目：OwBaseCore | 模块：IMemoryCache 高级特性扩展
 * 功能：提供缓存Key的引用计数和优先级驱逐回调功能
 * 技术要点：引用计数、优先级队列、Interlocked原子操作、延迟构造、DisposeHelper零分配、双重检查锁
 * 作者：zc | 创建：2025-01-19
 */
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// IMemoryCache 高级特性扩展，提供引用计数和优先级驱逐回调功能。
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
            public bool IsCallbackRegistered;
            private static readonly Func<PriorityQueue<PostEvictionCallbackRegistration, int>> s_queueFactory =
                static () => new PriorityQueue<PostEvictionCallbackRegistration, int>();
            public void AddRef() => Interlocked.Increment(ref _refCount);
            public void Release() => Interlocked.Decrement(ref _refCount);
            public int RefCount => Volatile.Read(ref _refCount);
            public PriorityQueue<PostEvictionCallbackRegistration, int> Queue =>
                LazyInitializer.EnsureInitialized(ref _callbackQueue, s_queueFactory);
            public bool HasQueue => Volatile.Read(ref _callbackQueue) != null;
        }

        private static readonly Func<object, CacheEntryState> s_stateFactory = static _ => new CacheEntryState();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ConcurrentDictionary<object, CacheEntryState> GetCacheEntryStateMap(this IMemoryCache cache) =>
            cache.GetOrCreate(CacheEntryStateMapKey, static e =>
            {
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
            entryState.Release();
            if (!entryState.HasQueue) return;
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
            if (entryState.RefCount <= 0)
            {
                cache.GetCacheEntryStateMap().TryRemove(key, out _);
            }
            if (exceptions is not null)
                throw new AggregateException("优先级驱逐回调执行时发生异常", exceptions);
        }
    }
}
