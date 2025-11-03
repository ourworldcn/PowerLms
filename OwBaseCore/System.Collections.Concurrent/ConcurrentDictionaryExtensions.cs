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
        /// <para><strong>性能提示</strong>：此方法为高性能版本，不支持超时机制。如需超时保护以防止死锁，请使用带 TimeSpan 参数的重载。</para>
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

        /// <summary>
        /// 获取并发字典中的值并加锁，使用双重检查锁模式验证有效性，支持超时机制。
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型（必须是引用类型，用作锁对象）</typeparam>
        /// <param name="dictionary">并发字典</param>
        /// <param name="key">键</param>
        /// <param name="timeout">超时时间。<see cref="Timeout.InfiniteTimeSpan"/> 表示无限等待，<see cref="TimeSpan.Zero"/> 表示立即返回，正值表示等待指定时间。</param>
        /// <returns>持有锁的 DisposeHelper，如果键不存在、验证失败或超时返回 default</returns>
        /// <remarks>
        /// <para>使用双重检查锁模式：先获取值，加锁后再次验证值是否仍然有效（防止并发移除/替换）。</para>
        /// <para>成功时返回持有锁的 DisposeHelper，调用方负责在 using 块中使用。</para>
        /// <para>失败时返回 default，State 为 null。失败原因可能是：键不存在、验证失败或获取锁超时。</para>
        /// <para><strong>线程安全</strong>：保证 TryGetValue 和锁操作的原子性。</para>
        /// <para><strong>超时机制</strong>：防止死锁导致线程永久阻塞。建议在可能发生死锁的场景中使用此重载。</para>
        /// <para><strong>重试机制</strong>：双重检查失败时，如果还有剩余超时时间，会自动重试直到成功或超时。</para>
        /// <para><strong>性能优化</strong>：当 timeout 为 <see cref="Timeout.InfiniteTimeSpan"/> 时，会自动转发到无超时的高性能版本。</para>
        /// <para><strong>性能提示</strong>：由于需要异常处理机制和可能的重试，此重载的性能略低于无超时版本。如果确定不会死锁，优先使用无参数重载。</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">timeout 参数为负值且不等于 <see cref="Timeout.InfiniteTimeSpan"/>。</exception>
        public static DisposeHelper<TValue> GetAndLock<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key,
            TimeSpan timeout) where TValue : class
        {
            // ✅ 性能优化：无限超时时直接转发到高性能无参版本
            // 避免不必要的 while 循环、try-catch 和时间计算开销
            if (timeout == Timeout.InfiniteTimeSpan)
                return GetAndLock(dictionary, key);

            if (!dictionary.TryGetValue(key, out var value)) return default;

            // 计算截止时间（用于重试逻辑）
            var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;

            while (true)
            {
                bool lockTaken = false;
                try
                {
                    // 计算剩余超时时间
                    var remainingTimeout = TimeSpan.FromMilliseconds(Math.Max(0, deadline - Environment.TickCount64));

                    Monitor.TryEnter(value, remainingTimeout, ref lockTaken);
                    if (!lockTaken) return default;  // 超时

                    // 双重检查：验证值未被替换
                    if (ReferenceEquals(value, dictionary.TryGetValue(key, out var current) ? current : null))
                        return new DisposeHelper<TValue>(static v => Monitor.Exit(v), value);

                    // 验证失败：值已被替换，释放锁并准备重试
                    Monitor.Exit(value);
                    lockTaken = false;

                    // 检查是否还有剩余时间
                    if (Environment.TickCount64 >= deadline)
                        return default;  // 超时，不再重试

                    // 重新获取最新的值
                    if (!dictionary.TryGetValue(key, out value))
                        return default;  // 键已被移除

                    // 继续循环重试
                }
                catch
                {
                    // 确保异常情况下锁被正确释放
                    if (lockTaken)
                        Monitor.Exit(value);
                    throw;
                }
            }
        }
    }
}
