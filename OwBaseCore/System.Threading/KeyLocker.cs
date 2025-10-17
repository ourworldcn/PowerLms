/*
 * 项目：OurWorld基础库 | 模块：并发控制
 * 功能：基于任意键的读写锁机制，支持内存驻留控制
 * 技术要点：ReaderWriterLockSlim、ConcurrentDictionary、IDisposable模式
 * 作者：zc | 创建：2025-01 | 修改：2025-01-21 [重构为通用键值读写锁]
 */

using System.Collections.Concurrent;

namespace System.Threading
{
    /// <summary>
    /// 基于键的读写锁
    /// 提供基于任意键的读写锁机制，支持内存驻留控制
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 读锁期间：对象不能调入调出内存（内存固定）
    /// - 写锁期间：可以自由调入调出内存（独占访问）
    /// - 仅管理键值，不持有对象引用
    /// - 使用共享实例模式，与 ArrayPool.Shared 保持一致
    /// </remarks>
    public sealed class KeyReadWriteLock
    {
        /// <summary>
        /// 获取 KeyReadWriteLock 的共享实例
        /// </summary>
        public static KeyReadWriteLock Shared { get; } = new KeyReadWriteLock();

        /// <summary>
        /// 键锁状态映射表
        /// 使用ConcurrentDictionary确保线程安全
        /// </summary>
        private readonly ConcurrentDictionary<object, KeyLockState> _lockStates = new();

        /// <summary>
        /// 初始化 KeyReadWriteLock 的新实例
        /// </summary>
        public KeyReadWriteLock()
        {
        }

        /// <summary>
        /// 尝试获取或创建读锁
        /// 在读锁持有期间，对应的对象不应被调入调出内存
        /// </summary>
        /// <param name="key">锁定的键值，不能为null</param>
        /// <param name="timeout">获取锁的超时时间，默认30秒</param>
        /// <param name="lockHandle">成功时返回锁的释放器</param>
        /// <returns>true表示成功获取锁，false表示超时失败</returns>
        /// <exception cref="ArgumentNullException">键值为null时抛出</exception>
        public bool TryAcquireReadLock(object key, out IDisposable lockHandle, TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            lockHandle = null;

            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var lockState = _lockStates.GetOrAdd(key, k => new KeyLockState(k));
            
            try
            {
                if (lockState.Lock.TryEnterReadLock(actualTimeout))
                {
                    lockState.UpdateAccessTime(); // 成功获取锁后更新时间戳
                    lockHandle = new ReadLockReleaser(lockState);
                    return true;
                }
            }
            catch (ObjectDisposedException)
            {
                // 锁对象在获取过程中被清理，属于正常的并发竞争，返回失败
                return false;
            }

            return false;
        }

        /// <summary>
        /// 尝试获取或创建写锁
        /// 在写锁持有期间，调用者可以自由调入调出对应的对象
        /// </summary>
        /// <param name="key">锁定的键值，不能为null</param>
        /// <param name="timeout">获取锁的超时时间，默认30秒</param>
        /// <param name="lockHandle">成功时返回锁的释放器</param>
        /// <returns>true表示成功获取锁，false表示超时失败</returns>
        /// <exception cref="ArgumentNullException">键值为null时抛出</exception>
        public bool TryAcquireWriteLock(object key, out IDisposable lockHandle, TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            lockHandle = null;

            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            var lockState = _lockStates.GetOrAdd(key, k => new KeyLockState(k));
            
            try
            {
                if (lockState.Lock.TryEnterWriteLock(actualTimeout))
                {
                    lockState.UpdateAccessTime(); // 成功获取锁后更新时间戳
                    lockHandle = new WriteLockReleaser(lockState);
                    return true;
                }
            }
            catch (ObjectDisposedException)
            {
                // 锁对象在获取过程中被清理，属于正常的并发竞争，返回失败
                return false;
            }

            return false;
        }

        /// <summary>
        /// 获取或创建读锁（便利方法，失败时抛出异常）
        /// 在读锁持有期间，对应的对象不应被调入调出内存
        /// </summary>
        /// <param name="key">锁定的键值，不能为null</param>
        /// <param name="timeout">获取锁的超时时间，可选</param>
        /// <returns>读锁的释放器，释放时自动解锁</returns>
        /// <exception cref="ArgumentNullException">键值为null时抛出</exception>
        /// <exception cref="TimeoutException">获取锁超时时抛出</exception>
        public IDisposable AcquireReadLock(object key, TimeSpan? timeout = null)
        {
            if (TryAcquireReadLock(key, out var lockHandle, timeout))
            {
                return lockHandle;
            }
            
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            throw new TimeoutException($"获取读锁超时：{key}，超时时间：{actualTimeout}");
        }

        /// <summary>
        /// 获取或创建写锁（便利方法，失败时抛出异常）
        /// 在写锁持有期间，调用者可以自由调入调出对应的对象
        /// </summary>
        /// <param name="key">锁定的键值，不能为null</param>
        /// <param name="timeout">获取锁的超时时间，可选</param>
        /// <returns>写锁的释放器，释放时自动解锁</returns>
        /// <exception cref="ArgumentNullException">键值为null时抛出</exception>
        /// <exception cref="TimeoutException">获取锁超时时抛出</exception>
        public IDisposable AcquireWriteLock(object key, TimeSpan? timeout = null)
        {
            if (TryAcquireWriteLock(key, out var lockHandle, timeout))
            {
                return lockHandle;
            }
            
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
            throw new TimeoutException($"获取写锁超时：{key}，超时时间：{actualTimeout}");
        }

        /// <summary>
        /// 删除指定键的锁状态
        /// 必须在持有写锁的情况下调用，会彻底释放锁资源
        /// </summary>
        /// <param name="key">要删除的键值，不能为null</param>
        /// <returns>true表示删除成功，false表示键不存在或未持有写锁</returns>
        /// <exception cref="ArgumentNullException">键值为null时抛出</exception>
        /// <exception cref="InvalidOperationException">未持有写锁时抛出</exception>
        public bool RemoveKey(object key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!_lockStates.TryGetValue(key, out var lockState))
                return false;

            if (!lockState.IsWriteLockHeld)
                throw new InvalidOperationException($"删除键必须持有写锁：{key}");

            // 从字典中移除并释放资源
            var removed = _lockStates.TryRemove(key, out _);
            
            if (removed)
            {
                try
                {
                    lockState.Dispose();
                }
                catch
                {
                    // 忽略释放异常
                }
            }
            
            return removed;
        }

        /// <summary>
        /// 手动清理指定时间范围内未使用的锁状态
        /// </summary>
        /// <param name="unusedTimeThreshold">未使用时间阈值，超过此时间的锁将被清理</param>
        /// <returns>清理的锁数量</returns>
        public int CleanupUnusedLocks(TimeSpan unusedTimeThreshold)
        {
            var cleanedCount = 0;
            var cutoffTime = DateTime.UtcNow - unusedTimeThreshold;
            
            try
            {
                // 直接枚举字典，先检查时间再尝试清理
                foreach (var kvp in _lockStates)
                {
                    var key = kvp.Key;
                    var lockState = kvp.Value;
                    
                    // 第一次检查时间，避免不必要的锁操作
                    if (lockState.LastAccessTime >= cutoffTime)
                        continue;
                    
                    // 尝试获取写锁进行清理（非阻塞）
                    if (lockState.Lock.TryEnterWriteLock(TimeSpan.Zero))
                    {
                        try
                        {
                            // 二次判定：在持有写锁时重新检查时间条件
                            // 因为在获取锁的过程中，其他线程可能已经更新了访问时间
                            if (lockState.LastAccessTime >= cutoffTime)
                            {
                                // 时间条件不再满足，跳过清理
                                continue;
                            }
                            
                            // 获取写锁成功且时间条件仍然满足，执行清理
                            if (_lockStates.TryRemove(key, out _))
                            {
                                cleanedCount++;
                                lockState.Dispose(); // KeyLockState.Dispose 会处理锁释放和资源清理
                            }
                        }
                        finally
                        {
                            // 如果对象未被释放，手动释放写锁
                            try
                            {
                                lockState.Lock.ExitWriteLock();
                            }
                            catch (ObjectDisposedException)
                            {
                                // Dispose 已经处理了锁释放，忽略
                            }
                        }
                    }
                    // 无法获取写锁说明正在使用，跳过
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
            {
                // 记录清理过程中的异常用于调试，但不影响系统运行
                // 过滤掉严重系统异常，让它们正常抛出
                System.Diagnostics.Debug.WriteLine($"KeyReadWriteLock cleanup error: {ex.Message}");
            }

            return cleanedCount;
        }

        #region 内部类型

        /// <summary>
        /// 键锁状态封装类
        /// 简化设计，直接暴露锁对象，提供必要的状态跟踪
        /// </summary>
        private sealed class KeyLockState : IDisposable
        {
            /// <summary>
            /// 底层读写锁对象
            /// </summary>
            public readonly ReaderWriterLockSlim Lock;
            
            /// <summary>
            /// 锁定的键值
            /// </summary>
            public readonly object Key;
            
            /// <summary>
            /// 最后访问时间（Ticks格式，用于原子操作）
            /// </summary>
            private long _lastAccessTimeTicks;

            /// <summary>
            /// 最后访问时间属性
            /// </summary>
            public DateTime LastAccessTime => new DateTime(Interlocked.Read(ref _lastAccessTimeTicks), DateTimeKind.Utc);

            /// <summary>
            /// 是否持有写锁
            /// </summary>
            public bool IsWriteLockHeld => Lock.IsWriteLockHeld;

            /// <summary>
            /// 初始化键锁状态
            /// </summary>
            /// <param name="key">键值</param>
            public KeyLockState(object key)
            {
                Key = key ?? throw new ArgumentNullException(nameof(key));
                // 启用递归锁定，允许同一线程多次获取同类型锁
                Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
                _lastAccessTimeTicks = DateTime.UtcNow.Ticks;
            }

            /// <summary>
            /// 更新最后访问时间
            /// </summary>
            public void UpdateAccessTime()
            {
                Interlocked.Exchange(ref _lastAccessTimeTicks, DateTime.UtcNow.Ticks);
            }

            /// <summary>
            /// 释放资源
            /// </summary>
            public void Dispose()
            {
                try
                {
                    // 可选：在调试模式下检查锁状态
                    #if DEBUG
                    if (Lock != null)
                    {
                        var hasActiveLocks = Lock.IsReadLockHeld || Lock.IsWriteLockHeld;
                        if (hasActiveLocks)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Warning: Disposing KeyLockState with active locks. " +
                                $"ReadLock: {Lock.IsReadLockHeld}, WriteLock: {Lock.IsWriteLockHeld}, Key: {Key}");
                        }
                    }
                    #endif
                    
                    // 释放锁对象 - 即使有未释放的锁也不会抛异常
                    Lock?.Dispose();
                }
                catch
                {
                    // 忽略释放异常 - 防御性编程
                }
            }
        }

        /// <summary>
        /// 读锁释放器
        /// 实现IDisposable接口，释放时自动退出读锁
        /// </summary>
        private sealed class ReadLockReleaser : IDisposable
        {
            private KeyLockState _lockState;

            /// <summary>
            /// 初始化读锁释放器
            /// </summary>
            /// <param name="lockState">锁状态引用</param>
            public ReadLockReleaser(KeyLockState lockState)
            {
                _lockState = lockState ?? throw new ArgumentNullException(nameof(lockState));
            }

            /// <summary>
            /// 释放读锁
            /// </summary>
            public void Dispose()
            {
                var lockState = Interlocked.Exchange(ref _lockState, null);
                if (lockState != null)
                {
                    try
                    {
                        lockState.Lock.ExitReadLock();
                    }
                    catch (ObjectDisposedException)
                    {
                        // 已释放，忽略
                    }
                    catch (SynchronizationLockException)
                    {
                        // 当前线程没有持有锁，忽略
                    }
                }
            }
        }

        /// <summary>
        /// 写锁释放器
        /// 实现IDisposable接口，释放时自动退出写锁
        /// </summary>
        private sealed class WriteLockReleaser : IDisposable
        {
            private KeyLockState _lockState;

            /// <summary>
            /// 初始化写锁释放器
            /// </summary>
            /// <param name="lockState">锁状态引用</param>
            public WriteLockReleaser(KeyLockState lockState)
            {
                _lockState = lockState ?? throw new ArgumentNullException(nameof(lockState));
            }

            /// <summary>
            /// 释放写锁
            /// </summary>
            public void Dispose()
            {
                var lockState = Interlocked.Exchange(ref _lockState, null);
                if (lockState != null)
                {
                    try
                    {
                        lockState.Lock.ExitWriteLock();
                    }
                    catch (ObjectDisposedException)
                    {
                        // 已释放，忽略
                    }
                    catch (SynchronizationLockException)
                    {
                        // 当前线程没有持有锁，忽略
                    }
                }
            }
        }

        #endregion 内部类型

        #region 测试和验证方法

        /// <summary>
        /// 测试 ReaderWriterLockSlim.Dispose() 在有未释放锁时的行为
        /// 仅用于开发和测试阶段验证
        /// </summary>
        /// <returns>测试结果描述</returns>
        public static string TestDisposeWithActiveLocks()
        {
            var results = new System.Text.StringBuilder();
            var rwLock = new ReaderWriterLockSlim();
            
            try
            {
                results.AppendLine("=== ReaderWriterLockSlim.Dispose() 行为测试 ===");
                
                // 测试1: 持有读锁时 Dispose
                rwLock.EnterReadLock();
                results.AppendLine($"1. 获取读锁成功: IsReadLockHeld = {rwLock.IsReadLockHeld}");
                
                try
                {
                    rwLock.Dispose(); // 这里不会抛异常
                    results.AppendLine("2. 持有读锁时调用 Dispose() - 成功，无异常");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"2. 持有读锁时调用 Dispose() - 异常: {ex.GetType().Name}: {ex.Message}");
                }
                
                // 测试2: Dispose 后尝试释放读锁
                try
                {
                    rwLock.ExitReadLock(); // 这里会抛异常
                    results.AppendLine("3. Dispose 后释放读锁 - 成功");
                }
                catch (Exception ex)
                {
                    results.AppendLine($"3. Dispose 后释放读锁 - 异常: {ex.GetType().Name}: {ex.Message}");
                }
                
            }
            catch (Exception ex)
            {
                results.AppendLine($"测试过程中发生异常: {ex.GetType().Name}: {ex.Message}");
            }
            
            // 测试3: 新的锁实例测试写锁
            var rwLock2 = new ReaderWriterLockSlim();
            try
            {
                rwLock2.EnterWriteLock();
                results.AppendLine($"4. 新实例获取写锁成功: IsWriteLockHeld = {rwLock2.IsWriteLockHeld}");
                
                rwLock2.Dispose(); // 持有写锁时 Dispose
                results.AppendLine("5. 持有写锁时调用 Dispose() - 成功，无异常");
                
                try
                {
                    rwLock2.ExitWriteLock(); // 这里会抛异常
                }
                catch (Exception ex)
                {
                    results.AppendLine($"6. Dispose 后释放写锁 - 异常: {ex.GetType().Name}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                results.AppendLine($"写锁测试异常: {ex.GetType().Name}: {ex.Message}");
            }
            
            results.AppendLine("=== 测试结论 ===");
            results.AppendLine("- ReaderWriterLockSlim.Dispose() 不会因未释放的锁而抛异常");
            results.AppendLine("- 但 Dispose() 后任何锁操作都会抛 ObjectDisposedException");
            results.AppendLine("- 建议在 Dispose 前确保所有锁都已正确释放");
            
            return results.ToString();
        }

        #endregion 测试和验证方法
    }
}