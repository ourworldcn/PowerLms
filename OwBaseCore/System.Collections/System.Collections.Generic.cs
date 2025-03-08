
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    public static class OwEnumerableExtensions
    {
        /// <summary>
        /// 对 集合 的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">修改该集合本身将引发异常。可以修改其元素的内容。</param>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException">action 为 null。</exception>
        /// <exception cref="InvalidOperationException">已修改集合中的某个元素。</exception>
        public static void ForEach<T>(this IEnumerable<T> obj, Action<T> action)
        {
            foreach (var item in obj)
                action(item);
        }

        /// <summary>
        /// 对 集合 的每个元素执行指定操作。
        /// 每个操作引发的异常会归并到一起，最终用<see cref="AggregateException"/>引发。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="action">注意不要修改集合<paramref name="obj"/>。</param>
        /// <exception cref="AggregateException">其中是每个操作引发的异常。</exception>
        [DebuggerHidden]
        public static void SafeForEach<T>(this IEnumerable<T> obj, Action<T> action)
        {
            List<Exception> list = null;
            foreach (var item in obj)
                try
                {
                    action(item);
                }
                catch (Exception err)
                {
                    list ??= new List<Exception>();
                    list.Add(err);
                }
            if (list?.Count > 0)
                throw new AggregateException(list);
        }

        /// <summary>
        /// 返回指定枚举的实体状态，若能转换为<see cref="ICollection{T}"/>则返回该接口，否则生成其数组并返回。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ICollection<T> TryToCollection<T>(this IEnumerable<T> source) => source is ICollection<T> coll ? coll : source.ToArray();


        /// <summary>
        /// 尝试在不强制枚举的情况下确定序列中的元素数,若不能则用 Count() 数量。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetNonEnumeratedCountOrCount<T>(this IEnumerable<T> source) => source.TryGetNonEnumeratedCount(out var count) ? count : source.Count();

        /// <summary>
        /// 尝试从 IEnumerable 获取数组，如果可以直接获取元素数量，则使用数组池获取数组并返回。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源集合。</param>
        /// <returns>包含数组和有效元素数量的 PooledArray 结构。</returns>
        public static PooledArray<T> TryToPooledArray<T>(this IEnumerable<T> source)
        {
            if (source.TryGetNonEnumeratedCount(out int count))
            {
                var pool = ArrayPool<T>.Shared;
                var array = pool.Rent(count);
                var s = array[..^1];
                if (source is ICollection<T> collection)
                    collection.CopyTo(array, 0);
                else
                {
                    int index = 0;
                    foreach (var item in source)
                    {
                        array[index++] = item;
                    }
                }

                return new PooledArray<T>(array, count, pool);
            }
            else
            {
                var array = source.ToArray();
                return new PooledArray<T>(array, array.Length, null);
            }
        }

        /// <summary>
        /// 用于在必要时将池中获取的数据返回到池中的结构。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        public ref struct PooledArray<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            private readonly ArrayPool<T> _pool;

            public PooledArray(T[] array, int count, ArrayPool<T> pool)
            {
                _array = array;
                _count = count;
                _pool = pool;
            }

            /// <summary>
            /// 获取数组。
            /// </summary>
            public readonly T[] Array => _array;

            /// <summary>
            /// 获取有效元素数量。
            /// </summary>
            public readonly int Count => _count;

            /// <summary>
            /// 释放资源，将数组返回到池中（如果适用）。
            /// </summary>
            public readonly void Dispose()
            {
                _pool?.Return(_array, clearArray: true);
            }
        }
    }
}