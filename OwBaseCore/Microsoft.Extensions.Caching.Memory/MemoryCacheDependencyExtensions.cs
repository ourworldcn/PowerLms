using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// IMemoryCache 依赖关系管理扩展，提供缓存项之间的依赖关系管理功能。
    /// </summary>
    public static class MemoryCacheDependencyExtensions
    {
        /// <summary>
        /// 依赖关系字典的缓存键名，用于在缓存中存储依赖关系映射表。
        /// </summary>
        private const string DependencyMapKey = "___CacheDependencyMap___";

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
    }
}
