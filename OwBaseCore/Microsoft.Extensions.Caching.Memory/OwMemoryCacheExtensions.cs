using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

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
        private static ConcurrentDictionary<object, CancellationTokenSource> GetCancellationTokenMap(this IMemoryCache cache)
        {
            return cache.GetOrCreate(CancellationTokenMapKey, entry =>
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
            if (entry == null) throw new ArgumentNullException(nameof(entry)); // 检查entry是否为空
            if (cache == null) throw new ArgumentNullException(nameof(cache)); // 检查cache是否为空

            var cts = tokenSource ?? new CancellationTokenSource(); // 获取或创建取消令牌源
            var tokenMap = cache.GetCancellationTokenMap(); // 获取取消令牌字典
            tokenMap[entry.Key] = cts; // 存储取消令牌源
            entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token)); // 注册更改的通知

            // 创建一个包含所需状态的对象，避免闭包直接捕获参数
            var state = new TokenCallbackState
            {
                Cache = cache,
                AutoDispose = autoDispose
            };

            // 注册逐出回调，当缓存项被逐出时清理取消令牌资源并取消令牌
            entry.RegisterPostEvictionCallback((key, _, reason, callbackState) =>
            {
                if (callbackState is TokenCallbackState state && state.Cache is IMemoryCache ch)
                {
                    var map = ch.GetCancellationTokenMap();
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
            if (options == null) throw new ArgumentNullException(nameof(options)); // 检查options是否为空
            if (cache == null) throw new ArgumentNullException(nameof(cache)); // 检查cache是否为空
            if (key == null) throw new ArgumentNullException(nameof(key)); // 检查key是否为空

            var cts = tokenSource ?? new CancellationTokenSource(); // 获取或创建取消令牌源
            var tokenMap = cache.GetCancellationTokenMap(); // 获取取消令牌字典
            tokenMap[key] = cts; // 存储取消令牌源
            options.ExpirationTokens.Add(new CancellationChangeToken(cts.Token)); // 注册更改的通知

            // 创建一个包含所需状态的对象，避免闭包直接捕获参数
            var state = new TokenCallbackState
            {
                Cache = cache,
                AutoDispose = autoDispose
            };

            // 注册逐出回调，当缓存项被逐出时清理取消令牌资源并取消令牌
            options.RegisterPostEvictionCallback((evictedKey, _, reason, callbackState) =>
            {
                if (callbackState is TokenCallbackState callbackStateObj && callbackStateObj.Cache is IMemoryCache ch)
                {
                    var map = ch.GetCancellationTokenMap();
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

        /// <summary> 用于存储回调状态的辅助类。 </summary>
        private class TokenCallbackState
        {
            public IMemoryCache Cache { get; set; }
            public bool AutoDispose { get; set; }
        }

        /// <summary>
        /// 根据键获取关联的取消令牌源。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>与键关联的取消令牌源，如果没有找到则返回null。</returns>
        /// <exception cref="ArgumentNullException">当cache或key参数为null时抛出。</exception>
        public static CancellationTokenSource GetCancellationTokenSource(this IMemoryCache cache, object key)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (key == null) throw new ArgumentNullException(nameof(key));

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
            if (cache == null) throw new ArgumentNullException(nameof(cache)); // 检查cache是否为空
            if (key == null) throw new ArgumentNullException(nameof(key)); // 检查key是否为空

            // 只调用一次GetCancellationTokenSource，将结果保存到局部变量
            var source = cache.GetCancellationTokenSource(key);

            // 检查源是否不为null且未被取消
            if (source != null && !source.IsCancellationRequested)
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
            if (key == null) return null;

            // 如果有前缀，检查键是否以前缀开头
            if (prefix != null)
            {
                if (!key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    OwHelper.SetLastErrorAndMessage(400, "格式错误：前缀不匹配");
                    return null;
                }

                // 移除前缀
                key = key.Substring(prefix.Length);
            }

            // 确保剩余部分至少等于GUID的长度
            if (key.Length < IdKeyLength) return null;

            // 提取GUID部分（取前IdKeyLength个字符）
            var guidPart = key.Substring(0, IdKeyLength);

            // 尝试解析GUID
            if (!Guid.TryParse(guidPart, out var id))
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
        public static Guid? GetIdFromCacheKey<T>(string key, string prefix = null)
        {
            // 如果未提供前缀，使用类型名称作为前缀
            prefix = prefix ?? $"{typeof(T).Name}.";
            return GetIdFromCacheKey(key, prefix);
        }

        /// <summary>
        /// 根据前缀和GUID构建缓存键。
        /// </summary>
        /// <param name="id">GUID标识符</param>
        /// <param name="prefix">前缀</param>
        /// <returns>格式化的缓存键</returns>
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
        public static string GetCacheKeyFromId<T>(Guid id, string prefix = null)
        {
            // 如果未提供前缀，使用类型名称作为前缀
            prefix = prefix ?? $"{typeof(T).Name}.";
            return GetCacheKeyFromId(id, prefix);
        }
    }

    /// <summary>
    /// IMemoryCache 优先级回调扩展，提供按优先级顺序执行的缓存失效回调功能。
    /// </summary>
    public static class OwPriorityCallbackExtensions
    {
        /// <summary>
        /// 优先级回调映射字典的缓存键名，用于在缓存中存储优先级回调映射表。
        /// </summary>
        private const string PriorityCallbackMapKey = $"{OwMemoryCacheExtensions.GuidString}.PriorityCallbackMap";

        /// <summary>
        /// 获取优先级回调字典，使用 IMemoryCache 内置的线程安全方法。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <returns>存储缓存项优先级回调的并发字典。</returns>
        private static ConcurrentDictionary<object, PriorityQueue<PostEvictionCallbackRegistration, int>> GetPriorityCallbackMap(this IMemoryCache cache) =>
            cache.GetOrCreate(PriorityCallbackMapKey, entry =>
            {
                // 创建永不过期的缓存项
                entry.SetPriority(CacheItemPriority.NeverRemove);
                
                // 创建并返回新的并发字典
                return new ConcurrentDictionary<object, PriorityQueue<PostEvictionCallbackRegistration, int>>();
            });

        /// <summary>
        /// 注册一个带优先级的缓存失效回调。优先级值越小，回调执行的顺序越靠前。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        /// <param name="cache">缓存实例。</param>
        /// <param name="callback">当缓存项被移除时要调用的回调方法。</param>
        /// <param name="state">传递给回调的状态对象。</param>
        /// <param name="priority">回调的优先级，值越小优先级越高。默认为0。</param>
        /// <returns>传入的缓存条目（用于链式调用）。</returns>
        public static ICacheEntry RegisterPriorityPostEvictionCallback(
            this ICacheEntry entry,
            IMemoryCache cache,
            PostEvictionDelegate callback,
            object state = null,
            int priority = 0)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));
            if (cache is null)
                throw new ArgumentNullException(nameof(cache));
            if (callback is null)
                throw new ArgumentNullException(nameof(callback));

            // 获取优先级回调映射表
            var callbackMap = cache.GetPriorityCallbackMap();

            // 创建回调注册对象
            var callbackRegistration = new PostEvictionCallbackRegistration
            {
                EvictionCallback = callback,
                State = state
            };

            // 为当前缓存键获取优先级队列，如果不存在则创建新队列
            var priorityQueue = callbackMap.GetOrAdd(entry.Key, _ =>
                new PriorityQueue<PostEvictionCallbackRegistration, int>());

            // 添加新的回调项到优先级队列中
            lock (priorityQueue)
            {
                priorityQueue.Enqueue(callbackRegistration, priority);

                // 如果这是第一个回调，注册执行器
                if (priorityQueue.Count == 1)
                {
                    entry.RegisterPostEvictionCallback((key, value, reason, _) =>
                        ExecuteCallbacks(key, value, reason, callbackMap));
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
        /// <param name="priority">回调的优先级，值越小优先级越高。默认为0。</param>
        /// <returns>传入的缓存选项（用于链式调用）。</returns>
        public static MemoryCacheEntryOptions RegisterPriorityPostEvictionCallback(
            this MemoryCacheEntryOptions options,
            IMemoryCache cache,
            object key,
            PostEvictionDelegate callback,
            object state = null,
            int priority = 0)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (cache is null)
                throw new ArgumentNullException(nameof(cache));
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (callback is null)
                throw new ArgumentNullException(nameof(callback));

            // 获取优先级回调映射表
            var callbackMap = cache.GetPriorityCallbackMap();

            // 创建回调注册对象
            var callbackRegistration = new PostEvictionCallbackRegistration
            {
                EvictionCallback = callback,
                State = state
            };

            // 为当前缓存键获取优先级队列，如果不存在则创建新队列
            var priorityQueue = callbackMap.GetOrAdd(key, _ =>
                new PriorityQueue<PostEvictionCallbackRegistration, int>());

            // 添加新的回调项到优先级队列中
            lock (priorityQueue)
            {
                priorityQueue.Enqueue(callbackRegistration, priority);

                // 如果这是第一个回调，注册执行器
                if (priorityQueue.Count == 1)
                {
                    options.RegisterPostEvictionCallback((evictedKey, value, reason, _) =>
                        ExecuteCallbacks(evictedKey, value, reason, callbackMap));
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
            if (callbackMap.TryRemove(key, out var callbacks))
            {
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
                        System.Diagnostics.Debug.WriteLine($"优先级回调执行异常: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 获取与缓存键关联的所有优先级回调数量。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>优先级回调数量，如果没有找到则返回0。</returns>
        public static int GetPriorityCallbackCount(this IMemoryCache cache, object key)
        {
            if (cache is null)
                throw new ArgumentNullException(nameof(cache));
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            return cache.GetPriorityCallbackMap().TryGetValue(key, out var callbacks)
                ? callbacks.Count
                : 0;
        }

        /// <summary>
        /// 清除与缓存键关联的所有优先级回调。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>如果找到并成功清除则返回true，否则返回false。</returns>
        public static bool ClearPriorityCallbacks(this IMemoryCache cache, object key)
        {
            if (cache is null)
                throw new ArgumentNullException(nameof(cache));
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            return cache.GetPriorityCallbackMap().TryRemove(key, out _);
        }
    }
}
