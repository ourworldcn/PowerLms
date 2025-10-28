using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// ConcurrentDictionary 的并发同步扩展。
    /// </summary>
    public static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// 获取并发字典中的值并加锁，使用双重检查锁模式验证有效性。
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型（必须是引用类型，用作锁对象）</typeparam>
        /// <param name="dictionary">并发字典</param>
        /// <param name="key">键</param>
        /// <returns>持有锁的 DisposeHelper，如果键不存在或验证失败返回 default</returns>
        /// <remarks>
        /// <para>使用双重检查锁模式：先获取值，加锁后再次验证值是否仍然有效（防止并发移除/替换）。</para>
        /// <para>成功时返回持有锁的 DisposeHelper，调用方负责在 using 块中使用。</para>
        /// <para>失败时返回 default，State 为 null。</para>
        /// <para><strong>线程安全</strong>：保证 TryGetValue 和锁操作的原子性。</para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposeHelper<TValue> GetAndLock<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) where TValue : class
        {
            if (!dictionary.TryGetValue(key, out var value)) return default;
            Monitor.Enter(value);
            // 无需 try-catch 的原因：
            // 1. TryGetValue 和 ReferenceEquals 不会抛异常
            // 2. Monitor.Exit 的调用时机正确（成功路径转移锁所有权，失败路径手动释放）
            // 3. 即使发生极端异常（如 OutOfMemoryException），进程即将崩溃，锁清理已无意义
            // 4. 移除 try-catch 可减少异常处理开销并允许 JIT 内联优化
            // 性能优化：优先检查快路径（值未改变），仅在失败时才再次查找
            if (ReferenceEquals(value, dictionary.TryGetValue(key, out var current) ? current : null))
                return new DisposeHelper<TValue>(static v => Monitor.Exit(v), value);
            Monitor.Exit(value);
            return default;
        }
    }
}
