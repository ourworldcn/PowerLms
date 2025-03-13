using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// IMemoryCache 依赖关系管理扩展，提供缓存项之间的依赖关系管理功能。
    /// </summary>
    public static class OwMemoryCacheExtensions
    {
        /// <summary>
        /// 依赖关系字典的缓存键名，用于在缓存中存储依赖关系映射表。
        /// </summary>
        private const string DependencyMapKey = "___CacheDependencyMap___";

        /// <summary>
        /// 取消令牌字典的缓存键名，用于在缓存中存储取消令牌映射表。
        /// </summary>
        private const string CancellationTokenMapKey = "___CacheCancellationTokenMap___";

        /// <summary>
        /// 获取依赖关系字典，使用 IMemoryCache 内置的线程安全方法。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <returns>存储缓存项依赖关系的并发字典。</returns>
        private static ConcurrentDictionary<object, ConcurrentHashSet<object>> GetDependencyMap(this IMemoryCache cache)
        {
            return cache.GetOrCreate(DependencyMapKey, entry =>
            {
                // 创建永不过期的缓存项
                entry.SetPriority(CacheItemPriority.NeverRemove);

                // 创建并返回新的并发字典，使用ConcurrentHashSet作为值
                return new ConcurrentDictionary<object, ConcurrentHashSet<object>>();
            });
        }

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
        /// 添加缓存项依赖关系，使一个缓存项依赖于一个或多个其他缓存项。
        /// </summary>
        /// <param name="cache">缓存接口。</param>
        /// <param name="key">缓存项键，将依赖于指定的其他缓存项。</param>
        /// <param name="dependsOnKeys">被依赖的缓存项键列表，当这些键对应的缓存项被移除时，key也会被移除。</param>
        /// <returns>传入的缓存接口（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当cache或key参数为null时抛出。</exception>
        public static IMemoryCache AddDependency(this IMemoryCache cache, object key, params object[] dependsOnKeys)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (dependsOnKeys == null || dependsOnKeys.Length == 0) return cache;

            var map = cache.GetDependencyMap();
            var validDependsOnKeys = dependsOnKeys.Where(k => k != null).ToArray();

            foreach (var dependsOnKey in validDependsOnKeys)
            {
                // 创建或获取被依赖项的依赖集合 - 使用ConcurrentHashSet不需要锁
                var set = map.GetOrAdd(dependsOnKey, _ => new ConcurrentHashSet<object>());
                // 添加依赖项 - ConcurrentHashSet是线程安全的
                set.Add(key);
            }

            return cache;
        }

        /// <summary>
        /// 为缓存条目添加逐出传播功能，当缓存项被逐出时自动逐出其所有依赖项。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        /// <param name="cache">缓存实例，用于获取依赖关系字典。</param>
        /// <returns>传入的缓存条目（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当entry或cache参数为null时抛出。</exception>
        public static ICacheEntry RegisterPostDependency(this ICacheEntry entry, IMemoryCache cache)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (cache == null) throw new ArgumentNullException(nameof(cache));

            // 注册逐出回调
            entry.RegisterPostEvictionCallback((key, _, _, state) =>
            {
                if (state is IMemoryCache ch)
                {
                    var dic = ch.GetDependencyMap();
                    if (dic.TryGetValue(key, out var set))
                    {
                        // 当缓存项被逐出时，传播逐出给依赖项
                        // 使用ConcurrentHashSet无需加锁
                        foreach (var dependentKey in set)
                        {
                            ch.Remove(dependentKey);
                        }

                        // 从依赖图中移除此键
                        dic.TryRemove(key, out _);
                    }
                }
            }, cache);

            return entry;
        }

        /// <summary>
        /// 为缓存条目关联一个取消令牌，当令牌被取消时自动逐出缓存条目。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        /// <param name="cache">缓存实例，用于获取取消令牌字典。</param>
        /// <param name="tokenSource">可选的取消令牌源。如果为null，将创建一个新的。</param>
        /// <returns>传入的缓存条目（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当entry或cache参数为null时抛出。</exception>
        public static ICacheEntry RegisterCancellationToken(this ICacheEntry entry, IMemoryCache cache, CancellationTokenSource tokenSource = null)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (cache == null) throw new ArgumentNullException(nameof(cache));

            // 获取或创建取消令牌源
            var cts = tokenSource ?? new CancellationTokenSource();
            
            // 获取取消令牌字典
            var tokenMap = cache.GetCancellationTokenMap();
            
            // 存储取消令牌源
            tokenMap[entry.Key] = cts;

            // 注册更改的通知
            entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token));
            
            // 注册逐出回调，当缓存项被逐出时清理取消令牌资源
            entry.RegisterPostEvictionCallback((key, _, _, state) =>
            {
                if (state is IMemoryCache ch)
                {
                    var map = ch.GetCancellationTokenMap();
                    // 从映射中移除取消令牌源
                    if (map.TryRemove(key, out var source) && source != tokenSource) // 不处置外部提供的令牌源
                    {
                        source.Dispose();
                    }
                }
            }, cache);

            return entry;
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
        /// 根据键获取关联的取消令牌。
        /// </summary>
        /// <param name="cache">缓存实例。</param>
        /// <param name="key">缓存项键。</param>
        /// <returns>与键关联的取消令牌，如果没有找到则返回默认取消令牌。</returns>
        /// <exception cref="ArgumentNullException">当cache或key参数为null时抛出。</exception>
        public static CancellationToken GetCancellationToken(this IMemoryCache cache, object key)
        {
            var source = cache.GetCancellationTokenSource(key);
            return source?.Token ?? CancellationToken.None;
        }

        /// <summary>
        /// 为缓存条目关联多个变化令牌，形成一个组合变化令牌。
        /// </summary>
        /// <param name="entry">缓存条目。</param>
        /// <param name="changeTokens">要添加的变化令牌集合。</param>
        /// <returns>传入的缓存条目（用于链式调用）。</returns>
        /// <exception cref="ArgumentNullException">当entry或changeTokens参数为null时抛出。</exception>
        public static ICacheEntry AddExpirationTokens(this ICacheEntry entry, params IChangeToken[] changeTokens)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (changeTokens == null) throw new ArgumentNullException(nameof(changeTokens));

            foreach (var token in changeTokens.Where(t => t != null))
            {
                entry.ExpirationTokens.Add(token);
            }

            return entry;
        }

        /// <summary>
        /// 创建一个包含多个取消令牌的组合变化令牌。
        /// </summary>
        /// <param name="primaryToken">主要的取消令牌源。</param>
        /// <param name="additionalTokens">附加的取消令牌源集合。</param>
        /// <returns>组合的变化令牌。</returns>
        /// <exception cref="ArgumentNullException">当primaryToken参数为null时抛出。</exception>
        public static IChangeToken CreateCompositeChangeToken(CancellationTokenSource primaryToken, params CancellationTokenSource[] additionalTokens)
        {
            if (primaryToken == null) throw new ArgumentNullException(nameof(primaryToken));

            if (additionalTokens == null || additionalTokens.Length == 0)
            {
                return new CancellationChangeToken(primaryToken.Token);
            }

            var tokens = new List<IChangeToken>
            {
                new CancellationChangeToken(primaryToken.Token)
            };

            tokens.AddRange(additionalTokens
                .Where(t => t != null)
                .Select(t => new CancellationChangeToken(t.Token)));

            return new CompositeChangeToken(tokens);
        }
    }
}
