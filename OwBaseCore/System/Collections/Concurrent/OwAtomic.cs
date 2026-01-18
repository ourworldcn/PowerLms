/*
 * 项目：[OwBaseCore] | 模块：[Collections/ConcurrentAtomic]
 * 功能：[.NET 分类型原子注册表，保证并发创建的原子性]
 * 技术要点：[纯静态 Shared 模式、ConcurrentDictionary 线程安全、与 SingletonLocker 协作]
 * 作者：zc | 创建：2025-10 | 修改：2025-10-09 [重构 OwAtomic 为实例类，简化设计]
 */
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
namespace System.Collections.Concurrent
{
    /// <summary>
    /// 提供基于键的对象原子注册与管理的 .NET 分类型基础设施。
    /// </summary>
    /// <typeparam name="T">被管理对象的类型，通常为引用类型。</typeparam>
    /// <remarks>
    /// <para>原子表语义：</para>
    /// <list type="bullet">
    /// <item>每个键唯一映射一个实例，并发 <see cref="GetOrCreate"/> 保证只创建一次（原子性）</item>
    /// <item>移除后，未来的 <see cref="GetOrCreate"/> 会创建新实例</item>
    /// <item>调用方负责已获取实例的并发使用控制；管理器仅保证创建的原子性</item>
    /// </list>
    /// <para>设计理念：</para>
    /// <para>类似 <see cref="System.Buffers.ArrayPool{T}"/>，这是按类型分组的全局基础设施，
    /// 提供类型安全的键值原子映射服务，特别适合与 <see cref="System.Threading.SingletonLocker"/> 配合使用。</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // 基础使用
    /// var token = OwAtomic&lt;CancellationTokenSource&gt;.Shared
    ///     .GetOrCreate("cache-key", _ => new CancellationTokenSource());
    /// 
    /// // 与 SingletonLocker 配合锁定
    /// var lockObj = OwAtomic&lt;object&gt;.Shared
    ///     .GetOrCreate("business-lock", _ => new object());
    /// if (SingletonLocker.TryEnter(lockObj, TimeSpan.FromSeconds(5)))
    /// {
    ///     try { /* 临界区代码 */ }
    ///     finally { SingletonLocker.Exit(lockObj); }
    /// }
    /// 
    /// // 缓存清理
    /// OwAtomic&lt;CancellationTokenSource&gt;.Shared.TryRemoveAndDispose("cache-key");
    /// </code>
    /// </example>
    public class OwAtomic<T> where T : class
    {
        #region 静态共享实例
        /// <summary>
        /// 获取全局共享的原子注册表实例。
        /// </summary>
        /// <value>
        /// 返回类型化的原子注册表，保证应用程序内同一类型 T 的全局唯一性。
        /// </value>
        /// <remarks>
        /// <para>此属性提供按类型分组的全局协调服务，适用于需要跨组件协调的场景。</para>
        /// <para>特别适合与 <see cref="System.Threading.SingletonLocker"/> 配合使用时的锁定场景。</para>
        /// <para>注意：作为基础设施组件，资源会在应用程序结束时自动清理。</para>
        /// </remarks>
        public static OwAtomic<T> Shared { get; } = new OwAtomic<T>();
        #endregion 静态共享实例
        #region 核心存储和构造
        /// <summary>
        /// 存储键值映射的并发字典。
        /// </summary>
        private readonly ConcurrentDictionary<object, T> _map = new();
        /// <summary>
        /// 初始化 OwAtomic 的新实例。
        /// </summary>
        public OwAtomic()
        {
        }
        #endregion 核心存储和构造
        #region 核心操作方法
        /// <summary>
        /// 获取或按需创建与 key 关联的对象（原子操作）。
        /// </summary>
        /// <param name="key">用于唯一标识对象的键，不能为空。</param>
        /// <param name="factory">当 key 不存在时创建对象的工厂，不能为空。</param>
        /// <returns>返回与 key 关联的对象实例。</returns>
        /// <exception cref="ArgumentNullException">当 key 或 factory 为 null 时抛出。</exception>
        /// <remarks>
        /// <para>此方法保证对于相同的 key，无论多少线程同时调用，都只会创建一个实例。</para>
        /// <para>如果 key 已存在对应实例，则直接返回现有实例，不会调用工厂方法。</para>
        /// </remarks>
        public T GetOrCreate(object key, Func<object, T> factory)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(factory);
            return _map.GetOrAdd(key, factory);
        }
        /// <summary>
        /// 尝试获取与 key 关联的已创建对象。
        /// </summary>
        /// <param name="key">用于唯一标识对象的键，不能为空。</param>
        /// <param name="value">若找到则返回对应对象，否则为 null。</param>
        /// <returns>如果找到对象则返回 true，否则返回 false。</returns>
        /// <exception cref="ArgumentNullException">当 key 为 null 时抛出。</exception>
        /// <remarks>
        /// 此方法仅查找已存在的对象，不会触发创建操作。
        /// </remarks>
        public bool TryGet(object key, out T value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _map.TryGetValue(key, out value);
        }
        /// <summary>
        /// 尝试从注册表中移除 key 并释放对象（若实现了释放接口）。
        /// </summary>
        /// <param name="key">要移除的键，不能为空。</param>
        /// <returns>如果成功移除返回 true，否则（键不存在）返回 false。</returns>
        /// <exception cref="ArgumentNullException">当 key 为 null 时抛出。</exception>
        /// <remarks>
        /// <para>如果对象实现了 <see cref="IDisposable"/> 或 <see cref="IAsyncDisposable"/> 接口，
        /// 此方法会自动调用相应的释放方法。</para>
        /// <para>对于 <see cref="IAsyncDisposable"/> 对象，会在后台线程异步释放。</para>
        /// </remarks>
        public bool TryRemoveAndDispose(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (!_map.TryRemove(key, out var value)) return false;
            TryDispose(value);
            return true;
        }
        /// <summary>
        /// 尝试从注册表中移除 key，但不释放对象。
        /// </summary>
        /// <param name="key">要移除的键，不能为空。</param>
        /// <returns>如果成功移除返回 true，否则（键不存在）返回 false。</returns>
        /// <exception cref="ArgumentNullException">当 key 为 null 时抛出。</exception>
        /// <remarks>
        /// 移除后的对象不会被自动释放，调用方需要自行管理对象生命周期。
        /// </remarks>
        public bool TryRemove(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _map.TryRemove(key, out _);
        }
        /// <summary>
        /// 清空注册表并释放所有已创建对象。
        /// </summary>
        /// <remarks>
        /// <para>此方法主要用于测试或特殊清理场景，生产环境中应谨慎使用。</para>
        /// <para>对于全局 Shared 实例，此操作会影响所有使用者。</para>
        /// </remarks>
        public void Clear()
        {
            foreach (var kv in _map)
            {
                try
                {
                    TryDispose(kv.Value);
                }
                catch { } // 忽略释放异常，避免影响清理流程
            }
            _map.Clear();
        }
        /// <summary>
        /// 获取当前注册表中的对象数量。
        /// </summary>
        /// <value>返回已创建对象的数量。</value>
        /// <remarks>
        /// 此属性主要用于监控和诊断目的。
        /// </remarks>
        public int Count => _map.Count;
        #endregion 核心操作方法
        #region 私有辅助方法
        /// <summary>
        /// 尝试释放对象资源。
        /// </summary>
        /// <param name="obj">要释放的对象。</param>
        private static void TryDispose(object obj)
        {
            if (obj is null) return;
            // 同步释放优先
            if (obj is IDisposable d)
            {
                try { d.Dispose(); } catch { } // 忽略释放异常
                return;
            }
            // 异步释放在后台处理
            if (obj is IAsyncDisposable ad)
            {
                _ = Task.Run(async () =>
                {
                    try { await ad.DisposeAsync().ConfigureAwait(false); }
                    catch { } // 忽略异步释放异常
                });
            }
        }
        #endregion 私有辅助方法
    }
}