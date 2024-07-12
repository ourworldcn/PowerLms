
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
    }
}