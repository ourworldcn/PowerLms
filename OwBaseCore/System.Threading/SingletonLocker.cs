using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// 依据对象值相等<see cref="IEquatable{T}"/>锁定第一个实例。
    /// 虽然可以使用该类锁定值对象，且逻辑上也正确，但每次都装箱(后续装箱对象将被抛弃，可以GC)导致性能低下。
    /// 注意: 该类用于基于对象值相等性而非引用相等性的锁定场景。
    /// </summary>
    /// <remarks>
    /// 此类维护一个并发字典作为对象的"拘留池"，确保值相等的对象在池中只存在一个实例。
    /// 当多个线程需要使用"值相等"但"引用不同"的对象作为锁时，此类确保只有一个实例被实际锁定。
    /// </remarks>
    public static class SingletonLocker
    {
        /// <summary>
        /// 存储对象映射关系的并发字典，键和值都是同一对象，用于保证值相等的对象只有一个实例被锁定。
        /// </summary>
        private static readonly ConcurrentDictionary<object, object> _Data;

        /// <summary>
        /// 初始化 <see cref="SingletonLocker"/> 类的静态实例。
        /// </summary>
        static SingletonLocker()
        {
            _Data = new ConcurrentDictionary<object, object>();
        }

        /// <summary>
        /// 如果 key 在暂存池中，则返回对它的引用；否则返回 null。
        /// </summary>
        /// <param name="str">测试值相等的对象。参数名应改为obj以避免混淆</param>
        /// <returns>如果 key 值相等的实例在暂存池中，则返回池中对象的引用；否则返回 null。</returns>
        /// <remarks>
        /// 此方法使用对象的 <see cref="Object.Equals(object)"/> 方法和 <see cref="Object.GetHashCode"/> 方法来确定值相等性。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object IsInterned(object str) => _Data.TryGetValue(str, out var tmp) ? tmp : null;

        /// <summary>
        /// 检索对指定对象的引用。如不存在则添加到池中。
        /// </summary>
        /// <param name="obj">要检索或添加的对象</param>
        /// <returns>如果暂存了 obj 值相等的实例在暂存池中，则返回池中的引用；否则返回对值为 key 的对象的新引用，并加入池中。</returns>
        /// <exception cref="ArgumentNullException">obj 为 null。</exception>
        /// <remarks>
        /// 此方法确保值相等的对象在池中只有一个实例，类似于字符串的内部化机制。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Intern(object obj) => _Data.GetOrAdd(obj, obj);

        /// <summary>
        /// 确定当前线程是否保留指定对象上的锁。
        /// </summary>
        /// <param name="obj">要检查的对象</param>
        /// <returns>如果当前线程持有指定对象的锁，则为true；否则为false</returns>
        /// <remarks>
        /// 此方法首先检查对象是否在拘留池中，然后调用 <see cref="Monitor.IsEntered(object)"/> 方法。
        /// </remarks>
        /// <seealso cref="Monitor.IsEntered(object)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntered(object obj)
        {
            obj = IsInterned(obj);
            return obj is not null && Monitor.IsEntered(obj);
        }

        /// <summary>
        /// 锁定对象在当前应用程序域内的唯一实例。
        /// </summary>
        /// <param name="obj">试图锁定的对象，返回时可能变为池中原有对象，或无变化，锁是加在该对象上的</param>
        /// <param name="timeout">等待锁的超时时间</param>
        /// <returns>如果获得了锁，则为true；否则为false</returns>
        /// <remarks>
        /// 此方法首先将对象"内部化"到池中，然后尝试在超时时间内获取锁。
        /// 获取锁后，它会验证对象是否仍然是池中的那个实例（防止并发清理导致不一致）。
        /// 如果不一致，会释放锁并重试，直到获取正确的实例或超时。
        /// 注意: 该方法存在潜在的死循环风险，如果高并发下对象频繁被清理再添加。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool TryEnter(ref object obj, TimeSpan timeout)
        {
            obj = Intern(obj);
            var start = OwHelper.WorldNow;
            if (!Monitor.TryEnter(obj, timeout))
                return false;
            while (!ReferenceEquals(obj, IsInterned(obj)))  //若因并发导致对象被清理
            {
                Monitor.Exit(obj);
                var tmp = OwHelper.ComputeTimeout(start, timeout);
                if (tmp == TimeSpan.Zero)   //若超时
                    return false;
                obj = Intern(obj);
                if (!Monitor.TryEnter(obj, tmp))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 尝试锁定对象，内部调用 <see cref="TryEnter(ref object, TimeSpan)"/>。
        /// </summary>
        /// <param name="obj">要锁定的对象</param>
        /// <param name="timeout">等待锁的超时时间</param>
        /// <returns>如果获得了锁，则为true；否则为false</returns>
        /// <remarks>
        /// 此方法是 <see cref="TryEnter(ref object, TimeSpan)"/> 的便捷包装，允许在不传递 ref 参数的情况下使用。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnter(object obj, TimeSpan timeout) => TryEnter(ref obj, timeout);

        /// <summary>
        /// 在对象在当前应用程序域内的唯一实例上进行解锁。
        /// </summary>
        /// <param name="obj">要解锁的对象</param>
        /// <exception cref="ArgumentNullException">obj 参数为 null。</exception>
        /// <exception cref="SynchronizationLockException">当前线程没有拥有指定对象的锁。</exception>
        /// <remarks>
        /// 此方法首先获取对象在池中的唯一实例，然后释放该实例上的锁。
        /// 注意: 此处没有检查uniStr是否为null，如果IsInterned返回null将抛出异常。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exit(object obj)
        {
            var uniStr = IsInterned(obj);
            Monitor.Exit(uniStr);
        }

        /// <summary>
        /// 清理字符串拘留池中没有锁定的对象。
        /// </summary>
        /// <remarks>
        /// 此方法会尝试对每个对象加锁，如果成功则将其从池中移除，提高内存使用效率。
        /// 该方法使用 <see cref="MethodImplOptions.Synchronized"/> 特性确保一次只有一个线程可以执行。
        /// </remarks>
        [MethodImpl(MethodImplOptions.Synchronized)]    //该方法一次性只能在一个线程上执行。 静态方法在类型上锁定，而实例方法在实例上锁定。 只有一个线程可在任意实例函数中执行，且只有一个线程可在任意类的静态函数中执行。
        public static void TrimExcess()
        {
            object tmp;
            foreach (var item in _Data.Keys)
            {
                tmp = IsInterned(item);
                if (tmp is null || !Monitor.TryEnter(tmp, TimeSpan.Zero))
                    continue;
                try
                {
                    _Data.TryRemove(tmp, out _);
                }
                finally
                {
                    Monitor.Exit(tmp);
                }
            }
        }
    }
}