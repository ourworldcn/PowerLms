using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// 一个类似 List&lt;T&gt; 的泛型集合类，其内部存储数组通过 ArrayPool&lt;T&gt;.Shared 进行池化分配与回收，
    /// 以减少频繁分配大数组带来的 GC 压力和内存碎片，适用于高性能场景下大量临时数据的收集与处理。
    /// </summary>
    /// <typeparam name="T">集合中元素的类型</typeparam>
    public sealed class PooledList<T> : PooledListBase<T>
    {
        #region 构造函数

        /// <summary>
        /// 初始化 PooledList 类的新实例。
        /// </summary>
        public PooledList() : base() { }

        /// <summary>
        /// 初始化 PooledList 类的新实例，具有指定的初始容量。
        /// </summary>
        /// <param name="capacity">初始容量</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity 小于 0</exception>
        public PooledList(int capacity) : base(capacity) { }

        /// <summary>
        /// 初始化 PooledList 类的新实例，包含从指定集合复制的元素。
        /// </summary>
        /// <param name="collection">一个集合，其元素被复制到新列表中</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public PooledList(IEnumerable<T> collection) : base(collection) { }

        #endregion

        #region List<T> 兼容API - 查找方法

        /// <summary>
        /// 搜索与指定谓词匹配的元素，并返回整个列表中第一个匹配元素的从零开始的索引。
        /// </summary>
        public int FindIndex(Predicate<T> match) => FindIndex(0, Count, match);

        /// <summary>
        /// 从指定的索引开始搜索与指定谓词匹配的元素，并返回整个列表中第一个匹配元素的从零开始的索引。
        /// </summary>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            if (startIndex < 0 || startIndex > Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            return FindIndex(startIndex, Count - startIndex, match);
        }

        /// <summary>
        /// 从指定的索引开始搜索指定数量的元素中与指定谓词匹配的元素，并返回整个列表中第一个匹配元素的从零开始的索引。
        /// </summary>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            if (startIndex < 0 || startIndex > Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || startIndex + count > Count) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return -1;
            T[] buffer = Buffer;
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(buffer[i])) return i;
            }
            return -1;
        }

        /// <summary>
        /// 搜索与指定谓词定义的条件相匹配的元素，并返回列表中的第一个匹配元素。
        /// </summary>
        public T Find(Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            T[] buffer = Buffer;
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (match(buffer[i])) return buffer[i];
            }
            return default;
        }

        /// <summary>
        /// 检索与指定谓词定义的条件相匹配的所有元素。
        /// </summary>
        public PooledList<T> FindAll(Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            PooledList<T> list = new PooledList<T>();
            for (int i = 0; i < Count; i++)
            {
                if (match(this[i])) list.Add(this[i]);
            }
            return list;
        }

        /// <summary>
        /// 搜索与指定谓词匹配的元素，并返回整个列表中最后一个匹配元素的从零开始的索引。
        /// </summary>
        public int FindLastIndex(Predicate<T> match) => FindLastIndex(Count - 1, Count, match);

        /// <summary>
        /// 从指定的索引开始向前搜索与指定谓词匹配的元素，并返回整个列表中最后一个匹配元素的从零开始的索引。
        /// </summary>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            if (Count == 0) return -1;
            if (startIndex >= Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        /// <summary>
        /// 从指定索引开始向后搜索指定数量的元素中与指定谓词匹配的元素，并返回整个列表中最后一个匹配元素的从零开始的索引。
        /// </summary>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            if (Count == 0)
            {
                if (startIndex != -1) throw new ArgumentOutOfRangeException(nameof(startIndex));
                return -1;
            }
            if (startIndex < 0 || startIndex >= Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || startIndex - count + 1 < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return -1;
            T[] buffer = Buffer;
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (match(buffer[i])) return i;
            }
            return -1;
        }

        /// <summary>
        /// 搜索与指定谓词定义的条件相匹配的元素，并返回列表中的最后一个匹配元素。
        /// </summary>
        public T FindLast(Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            T[] buffer = Buffer;
            for (int i = Count - 1; i >= 0; i--)
            {
                if (match(buffer[i])) return buffer[i];
            }
            return default;
        }

        /// <summary>
        /// 确定列表中的每个元素是否与指定谓词定义的条件匹配。
        /// </summary>
        public bool TrueForAll(Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            for (int i = 0; i < Count; i++)
            {
                if (!match(this[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// 确定列表是否包含与指定谓词定义的条件相匹配的元素。
        /// </summary>
        public bool Exists(Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            for (int i = 0; i < Count; i++)
            {
                if (match(this[i])) return true;
            }
            return false;
        }

        #endregion

        #region List<T> 兼容API - 操作方法

        /// <summary>
        /// 对列表中的每个元素执行指定操作。
        /// </summary>
        public void ForEach(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            for (int i = 0; i < Count; i++) action(this[i]);
        }

        /// <summary>
        /// 从指定索引开始，在列表中搜索对象，并返回第一个匹配项的从零开始的索引。
        /// </summary>
        public int IndexOf(T item, int index)
        {
            if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (index == Count) return -1;
            return Array.IndexOf(Buffer, item, index, Count - index);
        }

        /// <summary>
        /// 在列表内，从指定的索引开始并包含指定计数的元素，搜索对象，并返回第一个匹配项的从零开始的索引。
        /// </summary>
        public int IndexOf(T item, int index, int count)
        {
            if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > Count) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return -1;
            return Array.IndexOf(Buffer, item, index, count);
        }

        /// <summary>
        /// 从列表中移除与指定谓词定义的条件相匹配的所有元素。
        /// </summary>
        public int RemoveAll(Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            int freeIndex = 0;
            while (freeIndex < Count && !match(this[freeIndex])) freeIndex++;
            if (freeIndex >= Count) return 0;
            int current = freeIndex + 1;
            while (current < Count)
            {
                while (current < Count && match(this[current])) current++;
                if (current < Count) this[freeIndex++] = this[current++];
            }
            int removed = Count - freeIndex;
            for (int i = 0; i < removed; i++)
                RemoveAt(Count - 1);
            return removed;
        }

        /// <summary>
        /// 从指定索引向后搜索对象，并返回最后一个匹配项的索引。
        /// </summary>
        public int LastIndexOf(T item)
        {
            if (Count == 0) return -1;
            return Array.LastIndexOf(Buffer, item, Count - 1, Count);
        }

        /// <summary>
        /// 从指定索引向后搜索对象。
        /// </summary>
        public int LastIndexOf(T item, int index)
        {
            if (index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (Count == 0) return -1;
            return Array.LastIndexOf(Buffer, item, index, index + 1);
        }

        /// <summary>
        /// 在指定范围内从指定索引向后搜索对象。
        /// </summary>
        public int LastIndexOf(T item, int index, int count)
        {
            if (Count != 0 && index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (Count != 0 && count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (Count == 0) return -1;
            if (index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (count > index + 1) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return -1;
            return Array.LastIndexOf(Buffer, item, index, count);
        }

        #endregion

        #region List<T> 兼容API - 排序和反转

        /// <summary>
        /// 反转整个列表中元素的顺序。
        /// </summary>
        public void Reverse() => Reverse(0, Count);

        /// <summary>
        /// 反转指定范围内元素的顺序。
        /// </summary>
        public void Reverse(int index, int count)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (Count - index < count) throw new ArgumentException("Invalid index/count");
            if (count > 1)
                Array.Reverse(Buffer, index, count);
        }

        /// <summary>
        /// 对整个列表进行排序。
        /// </summary>
        public void Sort() => Sort(0, Count, null);

        /// <summary>
        /// 使用指定的比较器对整个列表进行排序。
        /// </summary>
        public void Sort(IComparer<T> comparer) => Sort(0, Count, comparer);

        /// <summary>
        /// 对指定范围内的元素进行排序。
        /// </summary>
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (Count - index < count) throw new ArgumentException("Invalid index/count");
            if (count > 1)
                Array.Sort(Buffer, index, count, comparer);
        }

        #endregion

        #region List<T> 兼容API - 转换和复制

        /// <summary>
        /// 将列表转换为数组。
        /// </summary>
        public T[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<T>();
            var array = new T[Count];
            new ReadOnlySpan<T>(Buffer, 0, Count).CopyTo(array);
            return array;
        }

        /// <summary>
        /// 获取列表的只读包装。
        /// </summary>
        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        /// <summary>
        /// 将元素转换为另一种类型并返回新列表。
        /// </summary>
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            ArgumentNullException.ThrowIfNull(converter);
            var list = new List<TOutput>(Count);
            for (int i = 0; i < Count; i++)
            {
                list.Add(converter(Buffer[i]));
            }
            return list;
        }

        #endregion
    }


    /// <summary>
    /// <see cref="PooledList{T}"/> 的扩展方法集合
    /// </summary>
    public static class PooledListExtensions
    {
        /// <summary>
        /// 从 <see cref="IEnumerable{T}"/> 创建 <see cref="PooledList{T}"/>，可选预分配容量以避免动态扩容
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="capacityHint">容量提示，仅在无法获取集合数量时使用</param>
        /// <returns>包含源集合所有元素的 <see cref="PooledList{T}"/> 实例</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 null</exception>
        /// <remarks>
        /// 容量分配策略：
        /// <list type="bullet">
        /// <item><description>如果源集合能获取数量（ICollection、数组、某些LINQ查询）：使用实际数量（由 AddRange 自动处理）</description></item>
        /// <item><description>如果无法获取数量且提供了 <paramref name="capacityHint"/>：使用提示容量避免多次扩容</description></item>
        /// <item><description>否则：使用默认容量（首次分配8个元素），根据实际需要动态扩容</description></item>
        /// </list>
        /// </remarks>
        public static PooledList<T> ToPooledList<T>(this IEnumerable<T> source, int capacityHint = 0)
        {
            var result = source.TryGetNonEnumeratedCount(out var count)
               ? new PooledList<T>(count)              // 能获取数量：用实际数量
               : new PooledList<T>(capacityHint);      // 不能获取数量：用提示（0 时等价于默认）
            result.AddRange(source);
            return result;
        }
    }

}
