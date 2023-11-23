

using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    /// <summary>
    /// 依据对象值相等<see cref="IEquatable{T}"/>锁定第一个实例。
    /// 虽然可以使用该类锁定值对象，且逻辑上也正确，但每次都装箱(后续装箱对象将被抛弃，可以GC)导致性能低下。
    /// </summary>
    public static class SingletonLocker
    {
        private static readonly ConcurrentDictionary<object, object> _Data;

        static SingletonLocker()
        {
            if (_Data is null)
                Interlocked.CompareExchange(ref _Data, new ConcurrentDictionary<object, object>(), null);
        }

        /// <summary>
        /// 如果 key 在暂存池中，则返回对它的引用；否则返回 null。
        /// </summary>
        /// <param name="str">测试值相等的字符串。</param>
        /// <returns>如果 key 值相等的实例在暂存池中，则返回池中对象的引用；否则返回 null。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object IsInterned(object str) => _Data.TryGetValue(str, out var tmp) ? tmp : null;

        /// <summary>
        /// 检索对指定 对象 的引用。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>如果暂存了 obj 值相等的实例在暂存池中，则返回池中的引用；否则返回对值为 key 的字符串的新引用，并加入池中。</returns>
        /// <exception cref="ArgumentNullException">obj 为 null。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Intern(object obj) => _Data.GetOrAdd(obj, obj);

        /// <summary>
        /// 确定当前线程是否保留指定对象上的锁。
        /// 参见<seealso cref="Monitor.IsEntered(object)"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>检测</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntered(object obj)
        {
            obj = IsInterned(obj);
            return obj is not null && Monitor.IsEntered(obj);
        }


        /// <summary>
        /// 锁定字符串在当前应用程序域内的唯一实例。
        /// </summary>
        /// <param name="obj">试图锁定的字符串的值，返回时可能变为池中原有对象，或无变化，锁是加在该对象上的</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
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
        /// <seealso cref="TryEnter(ref string, TimeSpan)"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryEnter(object obj, TimeSpan timeout) => TryEnter(ref obj, timeout);

        /// <summary>
        /// 在对象在当前应用程序域内的唯一实例上进行解锁。
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="ArgumentNullException">obj 参数为 null。或指定对象没有锁定。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exit(object obj)
        {
            var uniStr = IsInterned(obj);
            Monitor.Exit(uniStr);
        }

        /// <summary>
        /// 清理字符串拘留池中没有锁定的对象。
        /// </summary>
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