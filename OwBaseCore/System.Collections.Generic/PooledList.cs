using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// 一个类似 List&lt;T&gt; 的泛型集合类，其内部存储数组通过 ArrayPool&lt;T&gt;.Shared 进行池化分配与回收，
    /// 以减少频繁分配大数组带来的 GC 压力和内存碎片，适用于高性能场景下大量临时数据的收集与处理。
    /// </summary>
    /// <remarks>
    /// 注意：此类设计用于短生命周期场景，不适合长期持有。使用完毕后必须调用 Dispose 方法归还资源。
    /// 典型用法是在 using 语句中使用此类的实例。
    /// </remarks>
    /// <typeparam name="T">集合中元素的类型</typeparam>
    public sealed class PooledList<T> : PooledListBase<T>, IDisposable
    {
        private const int DefaultCapacity = 8; // 默认的初始容量

        #region 构造函数
        /// <summary>初始化 PooledList&lt;T&gt; 类的新实例，具有指定的初始容量。</summary>
        /// <param name="capacity">初始容量，默认为 8</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity 小于 0</exception>
        public PooledList(int capacity = DefaultCapacity) : base(Math.Max(capacity, DefaultCapacity))
        {
        }

        /// <summary>初始化 PooledList&lt;T&gt; 类的新实例，该实例包含从指定集合复制的元素</summary>
        /// <param name="collection">一个集合，其元素被复制到新列表中</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public PooledList(IEnumerable<T> collection) : base(collection is ICollection<T> c ? c.Count : DefaultCapacity)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            AddRange(collection);
        }
        #endregion

        #region 增强的List<T>兼容方法
        /// <summary>搜索与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中第一个匹配元素的从零开始的索引</summary>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到第一个与指定谓词匹配的元素，则为该元素的从零开始的索引，否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public int FindIndex(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            return FindIndex(0, Count, match);
        }

        /// <summary>从指定的索引开始搜索与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中第一个匹配元素的从零开始的索引</summary>
        /// <param name="startIndex">从零开始的搜索起始索引</param>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到第一个与指定谓词匹配的元素，则为该元素的从零开始的索引，否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 超出范围</exception>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (startIndex < 0 || startIndex > Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            return FindIndex(startIndex, Count - startIndex, match);
        }

        /// <summary>从指定的索引开始搜索指定数量的元素中与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中第一个匹配元素的从零开始的索引</summary>
        /// <param name="startIndex">从零开始的搜索起始索引</param>
        /// <param name="count">要搜索的部分中的元素数</param>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到第一个与指定谓词匹配的元素，则为该元素的从零开始的索引，否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 或 count 超出范围</exception>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (startIndex < 0 || startIndex > Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || startIndex + count > Count) throw new ArgumentOutOfRangeException(nameof(count));

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(this[i])) return i;
            }
            return -1;
        }

        /// <summary>搜索与指定谓词定义的条件相匹配的元素，并返回 PooledList&lt;T&gt; 中的第一个匹配元素</summary>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到与指定谓词定义的条件相匹配的第一个元素，则为该元素；否则为类型 T 的默认值</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public T Find(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            for (int i = 0; i < Count; i++)
            {
                if (match(this[i])) return this[i];
            }
            return default;
        }

        /// <summary>检索与指定谓词定义的条件相匹配的所有元素</summary>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到与指定谓词定义的条件相匹配的元素，则为这些元素组成的 PooledList&lt;T&gt;；否则为空的 PooledList&lt;T&gt;</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public PooledList<T> FindAll(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            PooledList<T> list = new PooledList<T>();
            for (int i = 0; i < Count; i++)
            {
                if (match(this[i])) list.Add(this[i]);
            }
            return list;
        }

        /// <summary>搜索与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中最后一个匹配元素的从零开始的索引</summary>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到最后一个与指定谓词匹配的元素，则为该元素的从零开始的索引，否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public int FindLastIndex(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            return FindLastIndex(Count - 1, Count, match);
        }

        /// <summary>从指定的索引开始向前搜索与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中最后一个匹配元素的从零开始的索引</summary>
        /// <param name="startIndex">从零开始的向后搜索的起始索引</param>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到最后一个与指定谓词匹配的元素，则为该元素的从零开始的索引，否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 超出范围</exception>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (Count == 0) return -1;
            if (startIndex >= Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        /// <summary>从指定索引开始向后搜索指定数量的元素中与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中最后一个匹配元素的从零开始的索引</summary>
        /// <param name="startIndex">从零开始的向后搜索的起始索引</param>
        /// <param name="count">要搜索的部分中的元素数</param>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到最后一个与指定谓词匹配的元素，则为该元素的从零开始的索引，否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 或 count 超出范围</exception>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (Count == 0)
            {
                if (startIndex != -1) throw new ArgumentOutOfRangeException(nameof(startIndex));
                return -1;
            }
            if (startIndex < 0 || startIndex >= Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || startIndex - count + 1 < 0) throw new ArgumentOutOfRangeException(nameof(count));

            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (match(this[i])) return i;
            }
            return -1;
        }

        /// <summary>搜索与指定谓词定义的条件相匹配的元素，并返回 PooledList&lt;T&gt; 中的最后一个匹配元素</summary>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果找到与指定谓词定义的条件相匹配的最后一个元素，则为该元素；否则为类型 T 的默认值</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public T FindLast(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            for (int i = Count - 1; i >= 0; i--)
            {
                if (match(this[i])) return this[i];
            }
            return default;
        }

        /// <summary>确定 PooledList&lt;T&gt; 中的每个元素是否与指定谓词定义的条件匹配</summary>
        /// <param name="match">定义要搜索的元素的条件的谓词</param>
        /// <returns>如果 PooledList&lt;T&gt; 中的每个元素都与指定谓词定义的条件匹配，则为 true；否则为 false</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            for (int i = 0; i < Count; i++)
            {
                if (!match(this[i])) return false;
            }
            return true;
        }

        /// <summary>对 PooledList&lt;T&gt; 中的每个元素执行指定操作</summary>
        /// <param name="action">要对 PooledList&lt;T&gt; 的每个元素执行的委托</param>
        /// <exception cref="ArgumentNullException">action 为 null</exception>
        public void ForEach(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            for (int i = 0; i < Count; i++) action(this[i]);
        }

        /// <summary>从指定索引开始，在 PooledList&lt;T&gt; 中搜索对象，并返回第一个匹配项的从零开始的索引</summary>
        /// <param name="item">要在 PooledList&lt;T&gt; 中定位的对象，对于引用类型，该值可以为 null</param>
        /// <param name="index">从零开始的搜索起始索引</param>
        /// <returns>从 index 开始，如果在 PooledList&lt;T&gt; 中找到 item，则为该项的第一个匹配项的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 超出范围</exception>
        public int IndexOf(T item, int index)
        {
            if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
            return Array.IndexOf(Buffer, item, index, Count - index);
        }

        /// <summary>在 PooledList&lt;T&gt; 内，从指定的索引开始并包含指定计数的元素，搜索对象，并返回第一个匹配项的从零开始的索引</summary>
        /// <param name="item">要在 PooledList&lt;T&gt; 中定位的对象，对于引用类型，该值可以为 null</param>
        /// <param name="index">从零开始的搜索起始索引</param>
        /// <param name="count">要搜索的区域中的元素数</param>
        /// <returns>从 index 开始，在 count 个元素范围内，如果在 PooledList&lt;T&gt; 中找到 item，则为该项的第一个匹配项的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出范围</exception>
        public int IndexOf(T item, int index, int count)
        {
            if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > Count) throw new ArgumentOutOfRangeException(nameof(count));
            return Array.IndexOf(Buffer, item, index, count);
        }

        /// <summary>从 PooledList&lt;T&gt; 中移除与指定谓词定义的条件相匹配的所有元素</summary>
        /// <param name="match">定义要移除的元素的条件的谓词</param>
        /// <returns>从 PooledList&lt;T&gt; 中移除的元素数目</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            int freeIndex = 0;
            // 找到第一个要删除的元素
            while (freeIndex < Count && !match(this[freeIndex])) freeIndex++;
            if (freeIndex >= Count) return 0;

            int current = freeIndex + 1;
            while (current < Count)
            {
                while (current < Count && match(this[current])) current++;
                if (current < Count) this[freeIndex++] = this[current++];
            }

            int removed = Count - freeIndex;
            // 移除元素
            for (int i = 0; i < removed; i++)
                RemoveAt(Count - 1);

            return removed;
        }

        /// <summary>从 PooledList&lt;T&gt; 中移除指定范围的元素</summary>
        /// <param name="index">要移除的第一个元素的从零开始的索引</param>
        /// <param name="count">要移除的元素数</param>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出范围</exception>
        public void RemoveRange(int index, int count)
        {
            if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > Count) throw new ArgumentOutOfRangeException(nameof(count));

            // 从最后往前删除，这样不需要移动元素
            for (int i = index + count - 1; i >= index; i--)
                RemoveAt(i);
        }

        /// <summary>在指定的索引处将集合的元素插入 PooledList&lt;T&gt;</summary>
        /// <param name="index">应在此处插入新元素的从零开始的索引</param>
        /// <param name="collection">要插入的集合，集合本身不能为 null，但它可以包含为 null 的元素</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于 Count</exception>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));

            foreach (var item in collection)
                Insert(index++, item);
        }

        /// <summary>将指定数量的元素从源数组复制到 PooledList&lt;T&gt;</summary>
        /// <param name="source">要从中复制元素的源数组</param>
        /// <param name="sourceIndex">源数组中开始复制的索引</param>
        /// <param name="destinationIndex">PooledList&lt;T&gt; 中开始粘贴的索引</param>
        /// <param name="count">要复制的元素数</param>
        /// <exception cref="ArgumentNullException">source 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">sourceIndex、destinationIndex 或 count 超出范围</exception>
        /// <exception cref="ArgumentException">源数组中没有足够的元素可复制</exception>
        public void CopyFrom(T[] source, int sourceIndex, int destinationIndex, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sourceIndex < 0) throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            if (destinationIndex < 0 || destinationIndex > Count) throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            if (count < 0 || sourceIndex + count > source.Length || destinationIndex + count > Count) throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
                this[destinationIndex + i] = source[sourceIndex + i];
        }
        #endregion
    }
}
