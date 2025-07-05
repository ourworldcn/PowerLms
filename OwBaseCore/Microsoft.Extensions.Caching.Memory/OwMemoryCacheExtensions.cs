using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// IMemoryCache 依赖关系管理扩展，提供缓存项之间的依赖关系管理功能。
    /// </summary>
    [Guid(GuidString)]
    public static class OwMemoryCacheExtensions
    {
        internal const string GuidString = "E8A08708-8518-4AF9-86F8-98C35DA4A6A5";

        /// <summary>
        /// 取消令牌字典的缓存键名，用于在缓存中存储取消令牌映射表。
        /// </summary>
        private const string CancellationTokenMapKey = $"{GuidString}.CacheCancellationTokenMap";

        /// <summary>
        /// 获取取消令牌字典，使用 IMemoryCache 内置的线程安全方法。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <returns>存储缓存项取消令牌的并发字典。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ConcurrentDictionary<object, CancellationTokenSource> GetCancellationTokenMap(this IMemoryCache cache)
        {
            return cache.GetOrCreate(CancellationTokenMapKey, static entry =>
            {
                // 创建永不过期的缓存项
                entry.SetPriority(CacheItemPriority.NeverRemove);

                // 创建并返回新的并发字典，用于存储取消令牌
                return new ConcurrentDictionary<object, CancellationTokenSource>();
            });
        }

        /// <summary>
        /// 为缓存条目关联一个取消令牌，当令牌被取消时自动逐出缓存条目。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        /// <param name="cache">缓存实例，用于获取取消令牌字典。</param>
        /// <param name="tokenSource">可选的取消令牌源。如果为null，将创建一个新的。</param>
        /// <param name="autoDispose">是否自动处置令牌源。默认为true。</param>
        /// <returns>传入的缓存条目（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当entry或cache参数为null时抛出。</exception>
        public static ICacheEntry RegisterCancellationToken(this ICacheEntry entry, IMemoryCache cache,
            CancellationTokenSource tokenSource = null, bool autoDispose = true)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(cache);

            var cts = tokenSource ?? new CancellationTokenSource(); // 获取或创建取消令牌源
            var tokenMap = cache.GetCancellationTokenMap(); // 获取取消令牌字典
            tokenMap[entry.Key] = cts; // 存储取消令牌源
            entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token)); // 注册更改的通知

            // 创建一个包含所需状态的对象，避免闭包直接捕获参数
            var state = new TokenCallbackState(cache, autoDispose);

            // 注册逐出回调，当缓存项被逐出时清理取消令牌资源并取消令牌
            entry.RegisterPostEvictionCallback(static (key, _, reason, callbackState) =>
            {
                if (callbackState is TokenCallbackState state && state.Cache is not null)
                {
                    var map = state.Cache.GetCancellationTokenMap();
                    if (map.TryRemove(key, out var source)) // 从映射中移除取消令牌源
                    {
                        try
                        {
                            if (!source.IsCancellationRequested) // 确保令牌被取消，以便通知观察者
                            {
                                source.Cancel();
                            }

                            if (state.AutoDispose) // 如果设置了自动处置，则处置令牌源
                            {
                                source.Dispose();
                            }
                        }
                        catch { /* 忽略可能的异常 */ }
                    }
                }
            }, state);

            return entry;
        }

        /// <summary>
        /// 为缓存选项关联一个取消令牌，当令牌被取消时自动逐出相关缓存条目。
        /// </summary>
        /// <param name="options">缓存条目选项。</param>
        /// <param name="cache">缓存实例，用于获取取消令牌字典。</param>
        /// <param name="key">缓存项的键，用于在取消令牌字典中标识此条目。</param>
        /// <param name="tokenSource">可选的取消令牌源。如果为null，将创建一个新的。</param>
        /// <param name="autoDispose">是否自动处置令牌源。默认为true。</param>
        /// <returns>传入的缓存选项（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当options、cache或key参数为null时抛出。</exception>
        public static MemoryCacheEntryOptions RegisterCancellationToken(this MemoryCacheEntryOptions options,
            IMemoryCache cache, object key, CancellationTokenSource tokenSource = null, bool autoDispose = true)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);

            var cts = tokenSource ?? new CancellationTokenSource(); // 获取或创建取消令牌源
            var tokenMap = cache.GetCancellationTokenMap(); // 获取取消令牌字典
            tokenMap[key] = cts; // 存储取消令牌源
            options.ExpirationTokens.Add(new CancellationChangeToken(cts.Token)); // 注册更改的通知

            // 创建一个包含所需状态的对象，避免闭包直接捕获参数
            var state = new TokenCallbackState(cache, autoDispose);

            // 注册逐出回调，当缓存项被逐出时清理取消令牌资源并取消令牌
            options.RegisterPostEvictionCallback(static (evictedKey, _, reason, callbackState) =>
            {
                if (callbackState is TokenCallbackState callbackStateObj && callbackStateObj.Cache is not null)
                {
                    var map = callbackStateObj.Cache.GetCancellationTokenMap();
                    if (map.TryRemove(evictedKey, out var source)) // 从映射中移除取消令牌源
                    {
                        try
                        {
                            if (!source.IsCancellationRequested) // 确保令牌被取消，以便通知观察者
                            {
                                source.Cancel();
                            }

                            if (callbackStateObj.AutoDispose) // 如果设置了自动处置，则处置令牌源
                            {
                                source.Dispose();
                            }
                        }
                        catch { /* 忽略可能的异常 */ }
                    }
                }
            }, state);

            return options;
        }

        /// <summary>
        /// 用于存储回调状态的辅助类。
        /// </summary>
        private readonly struct TokenCallbackState
        {
            public TokenCallbackState(IMemoryCache cache, bool autoDispose)
            {
                Cache = cache;
                AutoDispose = autoDispose;
            }

            public IMemoryCache Cache { get; }
            public bool AutoDispose { get; }
        }

        /// <summary>
        /// 根据键获取关联的取消令牌源。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>与键关联的取消令牌源，如果没有找到则返回null。</returns>
        /// <exception cref="ArgumentNullException">当cache或key参数为null时抛出。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CancellationTokenSource GetCancellationTokenSource(this IMemoryCache cache, object key)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);

            var tokenMap = cache.GetCancellationTokenMap();
            tokenMap.TryGetValue(key, out var source);
            return source;
        }

        /// <summary>
        /// 使指定键关联的取消源失效（取消）。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>如果找到并成功取消则返回true，否则返回false。</returns>
        /// <exception cref="ArgumentNullException">当cache或key参数为null时抛出。</exception>
        public static bool CancelSource(this IMemoryCache cache, object key)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);

            // 只调用一次GetCancellationTokenSource，将结果保存到局部变量
            var source = cache.GetCancellationTokenSource(key);

            // 检查源是否不为null且未被取消
            if (source is not null && !source.IsCancellationRequested)
            {
                try
                {
                    source.Cancel();
                    return true;
                }
                catch { /* 忽略可能的异常 */ }
            }
            return false;
        }

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

        /// <summary>
        /// 批量取消多个缓存键关联的取消令牌源。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <param name="keys">要取消的缓存键集合</param>
        /// <param name="removeFromMap">是否从映射中移除已取消的令牌源，默认为true</param>
        /// <returns>成功取消的数量</returns>
        public static int CancelSources(this IMemoryCache cache, IEnumerable<object> keys, bool removeFromMap = true)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(keys);

            var count = 0;
            var tokenMap = cache.GetCancellationTokenMap();

            foreach (var key in keys)
            {
                if (tokenMap.TryGetValue(key, out var source) && 
                    source is not null && 
                    !source.IsCancellationRequested)
                {
                    try
                    {
                        source.Cancel();
                        count++;
                        
                        // 如果设置了移除标志，则从映射中移除
                        if (removeFromMap)
                        {
                            tokenMap.TryRemove(key, out _);
                        }
                    }
                    catch { /* 忽略异常 */ }
                }
            }

            return count;
        }

        /// <summary>
        /// 获取所有活跃的取消令牌源的数量。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <returns>活跃的取消令牌源数量</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetActiveCancellationTokenCount(this IMemoryCache cache)
        {
            ArgumentNullException.ThrowIfNull(cache);
            
            var tokenMap = cache.GetCancellationTokenMap();
            return tokenMap.Count;
        }

        /// <summary>
        /// 清理所有已取消或已处置的取消令牌源。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <returns>清理的令牌源数量</returns>
        public static int CleanupCancelledTokenSources(this IMemoryCache cache)
        {
            ArgumentNullException.ThrowIfNull(cache);

            var tokenMap = cache.GetCancellationTokenMap();
            var keysToRemove = new List<object>();

            // 找出所有已取消或已处置的令牌源
            foreach (var kvp in tokenMap)
            {
                var source = kvp.Value;
                var shouldRemove = false;

                if (source is null)
                {
                    shouldRemove = true;
                }
                else
                {
                    try
                    {
                        // 检查是否已处置（通过访问Token属性）
                        _ = source.Token;
                        // 检查是否已取消
                        if (source.IsCancellationRequested)
                        {
                            shouldRemove = true;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        shouldRemove = true;
                    }
                }

                if (shouldRemove)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            // 批量移除已找到的键，提高性能
            var count = 0;
            foreach (var key in keysToRemove)
            {
                if (tokenMap.TryRemove(key, out _))
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// IMemoryCache 优先级回调扩展，提供按优先级顺序执行的缓存失效回调功能。
    /// </summary>
    [Guid(GuidString)]
    public static class OwPriorityCallbackExtensions
    {
        /// <summary>
        /// GUID 标识符，用于唯一标识此扩展的缓存键。
        /// </summary>
        internal const string GuidString = "B690E855-E90E-4B4D-B454-937477A0DB70";

        /// <summary>
        /// 优先级回调映射字典的缓存键名，用于在缓存中存储优先级回调映射表。
        /// </summary>
        internal const string PriorityCallbackMapKey = $"{GuidString}.PriorityCallbackMap";

        /// <summary>
        /// 获取优先级回调字典，使用 IMemoryCache 内置的线程安全方法。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <returns>存储缓存项优先级回调的并发字典。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ConcurrentDictionary<object, PriorityQueue<PostEvictionCallbackRegistration, int>> GetPriorityCallbackMap(this IMemoryCache cache) =>
            cache.GetOrCreate(PriorityCallbackMapKey, static entry =>
            {
                entry.SetPriority(CacheItemPriority.NeverRemove);   // 设置缓存项优先级为永不过期
                return new ConcurrentDictionary<object, PriorityQueue<PostEvictionCallbackRegistration, int>>();    // 创建并返回新的并发字典，用于存储优先级回调
            });

        /// <summary>
        /// 注册一个带优先级的缓存失效回调。优先级值越小，回调执行的顺序越靠前。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        /// <param name="cache">缓存实例。</param>
        /// <param name="callback">当缓存项被移除时要调用的回调方法。</param>
        /// <param name="state">传递给回调的状态对象。</param>
        /// <param name="priority">回调的优先级，值越小优先级越高。默认为10。</param>
        /// <returns>传入的缓存条目（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
        public static ICacheEntry RegisterPriorityPostEvictionCallback(
            this ICacheEntry entry,
            IMemoryCache cache,
            PostEvictionDelegate callback,
            object state = null,
            int priority = 10)
        {
            // 参数检查 - 使用 .NET 6 的 ArgumentNullException.ThrowIfNull
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(callback);

            var callbackMap = cache.GetPriorityCallbackMap();   // 获取优先级回调映射表

            var callbackRegistration = new PostEvictionCallbackRegistration // 创建回调注册对象
            {
                EvictionCallback = callback,
                State = state
            };

            var priorityQueue = callbackMap.GetOrAdd(entry.Key, static _ =>
                new PriorityQueue<PostEvictionCallbackRegistration, int>());    // 为当前缓存键获取优先级队列，如果不存在则创建新队列

            lock (priorityQueue)    // 锁定优先级队列以确保线程安全
            {
                priorityQueue.Enqueue(callbackRegistration, priority);

                if (priorityQueue.Count == 1)   // 如果这是第一个回调，注册执行器
                {
                    entry.RegisterPostEvictionCallback(HandleEvictionCallback, cache);
                }
            }

            return entry;
        }

        /// <summary>
        /// 为缓存选项注册一个带优先级的缓存失效回调。优先级值越小，回调执行的顺序越靠前。
        /// </summary>
        /// <param name="options">缓存条目选项。</param>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项的键。</param>
        /// <param name="callback">当缓存项被移除时要调用的回调方法。</param>
        /// <param name="state">传递给回调的状态对象。</param>
        /// <param name="priority">回调的优先级，值越小优先级越高。默认为10。</param>
        /// <returns>传入的缓存选项（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
        public static MemoryCacheEntryOptions RegisterPriorityPostEvictionCallback(
            this MemoryCacheEntryOptions options,
            IMemoryCache cache,
            object key,
            PostEvictionDelegate callback,
            object state = null,
            int priority = 10)
        {
            // 参数检查
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(callback);

            // 获取优先级回调映射表
            var callbackMap = cache.GetPriorityCallbackMap();

            // 创建回调注册对象
            var callbackRegistration = new PostEvictionCallbackRegistration
            {
                EvictionCallback = callback,
                State = state
            };

            // 为当前缓存键获取优先级队列，如果不存在则创建新队列
            var priorityQueue = callbackMap.GetOrAdd(key, static _ =>
                new PriorityQueue<PostEvictionCallbackRegistration, int>());

            // 添加新的回调项到优先级队列中
            lock (priorityQueue)
            {
                priorityQueue.Enqueue(callbackRegistration, priority);

                // 如果这是第一个回调，注册执行器
                if (priorityQueue.Count == 1)
                {
                    options.RegisterPostEvictionCallback(HandleEvictionCallback, cache);
                }
            }

            return options;
        }

        // 私有辅助方法，用于执行回调队列
        private static void ExecuteCallbacks(
            object key,
            object value,
            EvictionReason reason,
            ConcurrentDictionary<object, PriorityQueue<PostEvictionCallbackRegistration, int>> callbackMap)
        {
            // 尝试获取并移除与此键关联的回调队列
            if (!callbackMap.TryRemove(key, out var callbacks)) 
                return;

            // 创建一个临时列表来存储所有回调，以便按优先级顺序执行
            var orderedCallbacks = new List<PostEvictionCallbackRegistration>();
            
            lock (callbacks)
            {
                // 将优先级队列中的所有元素按优先级出队到列表中
                while (callbacks.Count > 0)
                {
                    orderedCallbacks.Add(callbacks.Dequeue());
                }
            }

            // 依次执行所有回调
            foreach (var callbackItem in orderedCallbacks)
            {
                try
                {
                    callbackItem.EvictionCallback(key, value, reason, callbackItem.State);
                }
                catch (Exception ex)
                {
                    // 记录异常但不阻止其他回调执行
                    System.Diagnostics.Debug.WriteLine($"优先级回调执行异常: {ex}");
                }
            }
        }

        /// <summary>
        /// 回调包装器，用于处理缓存和回调映射。
        /// </summary>
        private static void HandleEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (state is IMemoryCache cache)
            {
                ExecuteCallbacks(key, value, reason, cache.GetPriorityCallbackMap());
            }
        }

        /// <summary>
        /// 获取与缓存键关联的所有优先级回调数量。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>优先级回调数量，如果没有找到则返回0。</returns>
        /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPriorityCallbackCount(this IMemoryCache cache, object key)
        {
            // 参数检查
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);

            var callbackMap = cache.GetPriorityCallbackMap();
            if (callbackMap.TryGetValue(key, out var callbacks))
            {
                lock (callbacks)
                {
                    return callbacks.Count;
                }
            }
            return 0;
        }

        /// <summary>
        /// 清除与缓存键关联的所有优先级回调。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>如果找到并成功清除则返回true，否则返回false。</returns>
        /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
        public static bool ClearPriorityCallbacks(this IMemoryCache cache, object key)
        {
            // 参数检查
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(key);

            return cache.GetPriorityCallbackMap().TryRemove(key, out _);
        }

        /// <summary>
        /// 获取所有有回调的缓存键列表。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <returns>包含回调的缓存键集合</returns>
        /// <exception cref="ArgumentNullException">当cache参数为null时抛出</exception>
        public static IEnumerable<object> GetKeysWithPriorityCallbacks(this IMemoryCache cache)
        {
            ArgumentNullException.ThrowIfNull(cache);
            
            var callbackMap = cache.GetPriorityCallbackMap();
            return callbackMap.Keys.ToList(); // 创建快照以避免并发修改异常
        }

        /// <summary>
        /// 批量清除多个缓存键的优先级回调。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <param name="keys">要清除回调的缓存键集合</param>
        /// <returns>成功清除的回调数量</returns>
        /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
        public static int ClearPriorityCallbacksBatch(this IMemoryCache cache, IEnumerable<object> keys)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(keys);

            var count = 0;
            var callbackMap = cache.GetPriorityCallbackMap();

            foreach (var key in keys)
            {
                if (callbackMap.TryRemove(key, out _))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 清理所有空的优先级回调队列。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <returns>清理的空队列数量</returns>
        /// <exception cref="ArgumentNullException">当cache参数为null时抛出</exception>
        public static int CleanupEmptyPriorityCallbackQueues(this IMemoryCache cache)
        {
            ArgumentNullException.ThrowIfNull(cache);

            var callbackMap = cache.GetPriorityCallbackMap();
            var keysToRemove = new List<object>();

            // 找出所有空的队列，使用快照避免并发修改
            var snapshot = callbackMap.ToArray();
            foreach (var kvp in snapshot)
            {
                var queue = kvp.Value;
                if (queue is not null)
                {
                    lock (queue)
                    {
                        if (queue.Count == 0)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                }
                else
                {
                    // 处理null队列的情况
                    keysToRemove.Add(kvp.Key);
                }
            }

            // 批量移除空队列
            var count = 0;
            foreach (var key in keysToRemove)
            {
                if (callbackMap.TryRemove(key, out _))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 获取优先级回调的统计信息。
        /// </summary>
        /// <param name="cache">缓存实例</param>
        /// <returns>包含统计信息的元组：(总队列数, 总回调数, 空队列数)</returns>
        /// <exception cref="ArgumentNullException">当cache参数为null时抛出</exception>
        public static (int TotalQueues, int TotalCallbacks, int EmptyQueues) GetPriorityCallbackStatistics(this IMemoryCache cache)
        {
            ArgumentNullException.ThrowIfNull(cache);

            var callbackMap = cache.GetPriorityCallbackMap();
            var totalQueues = callbackMap.Count;
            var totalCallbacks = 0;
            var emptyQueues = 0;

            // 使用快照避免并发修改，提高统计准确性
            var snapshot = callbackMap.Values.ToArray();
            foreach (var queue in snapshot)
            {
                if (queue is not null)
                {
                    lock (queue)
                    {
                        var count = queue.Count;
                        totalCallbacks += count;
                        if (count == 0)
                        {
                            emptyQueues++;
                        }
                    }
                }
                else
                {
                    emptyQueues++;
                }
            }

            return (totalQueues, totalCallbacks, emptyQueues);
        }
    }
}
