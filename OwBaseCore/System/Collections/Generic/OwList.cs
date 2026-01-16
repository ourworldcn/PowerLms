/*
 * 项目：OwBaseCore | 模块：System.Collections.Generic
 * 功能：基于内存池的列表类，提供完整 List<T> 兼容功能
 * 技术要点：继承自 OwListBase<T>，扩展排序、查找、转换等便利方法
 * 作者：zc | 创建：2025-01 | 修改：2025-01-20 重构为派生类
 */
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// 基于内存池的列表类，提供完整的 List&lt;T&gt; 兼容功能。
    /// 继承自 OwListBase&lt;T&gt;，提供高性能核心操作 + List&lt;T&gt; 扩展方法。
    /// 默认情况下使用 ArrayMemoryPool&lt;T&gt; 包装 ArrayPool&lt;T&gt;.Shared，提供最佳性能。
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class OwList<T> : OwListBase<T>, IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IDisposable
    {
        #region [构造函数]
        /// <summary>
        /// 初始化 OwList 类的新实例，使用默认的 ArrayMemoryPool 进行内存管理
        /// </summary>
        public OwList() : base()
        {
        }

        /// <summary>
        /// 初始化 OwList 类的新实例，使用指定的 MemoryPool。
        /// 如果 pool 为 null，则创建新的 ArrayMemoryPool&lt;T&gt; 实例并在 Dispose 时释放。
        /// </summary>
        /// <param name="capacity">新列表最初可以存储的元素数</param>
        /// <param name="pool">用于内存管理的 MemoryPool，如果为 null 则创建默认的 ArrayMemoryPool</param>
        public OwList(int capacity, MemoryPool<T> pool = null) : base(capacity, pool)
        {
        }

        /// <summary>
        /// 初始化 OwList 类的新实例，使用指定集合的元素和可选的内存池
        /// </summary>
        /// <param name="collection">要复制到新列表中的集合</param>
        /// <param name="pool">用于内存管理的 MemoryPool，如果为 null 则创建默认的 ArrayMemoryPool</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public OwList(IEnumerable<T> collection, MemoryPool<T> pool = null) : base(0, pool)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            AddRange(collection);
        }
        #endregion [构造函数]

        #region [批量操作]
        /// <summary>
        /// 将集合的元素添加到列表的末尾
        /// </summary>
        /// <param name="collection">要添加的元素集合</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(Count, collection);
        }
        #endregion [批量操作]

        #region [查找方法 - LastIndexOf系列]
        /// <summary>
        /// 搜索指定的对象，并返回整个列表中最后一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="item">要在列表中定位的对象</param>
        /// <returns>如果在列表中找到 item 的最后一个匹配项，则为该项的从零开始的索引；否则为 -1</returns>
        public int LastIndexOf(T item)
        {
            return LastIndexOf(item, Count - 1, Count);
        }
        /// <summary>
        /// 搜索指定的对象，并返回列表中从第一个元素到指定索引的元素范围内最后一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="item">要在列表中定位的对象</param>
        /// <param name="index">向后搜索的从零开始的起始索引</param>
        /// <returns>如果在列表中找到 item 的最后一个匹配项，则为该项的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 超出列表的有效索引范围</exception>
        public int LastIndexOf(T item, int index)
        {
            if (Count == 0)
                return -1;
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return LastIndexOf(item, index, index + 1);
        }
        /// <summary>
        /// 搜索指定的对象，并返回列表中包含指定数量的元素并在指定索引处结束的元素范围内最后一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="item">要在列表中定位的对象</param>
        /// <param name="index">向后搜索的从零开始的起始索引</param>
        /// <param name="count">要搜索的部分中的元素数</param>
        /// <returns>如果在列表中找到 item 的最后一个匹配项，则为该项的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出列表的有效范围</exception>
        public int LastIndexOf(T item, int index, int count)
        {
            if (Count == 0)
                return -1;
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index - count + 1 < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            var span = AsSpan();
            int startIndex = index - count + 1;
            for (int i = index; i >= startIndex; i--)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], item))
                    return i;
            }
            return -1;
        }
        #endregion [查找方法 - LastIndexOf系列]

        #region [批量移除]
        /// <summary>
        /// 移除与指定的谓词所定义的条件相匹配的所有元素
        /// </summary>
        /// <param name="match">定义要移除的元素应满足的条件的谓词</param>
        /// <returns>从列表中移除的元素数</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            int freeIndex = 0;
            var span = AsSpan();
            while (freeIndex < Count && !match(span[freeIndex]))
                freeIndex++;
            if (freeIndex >= Count)
                return 0;
            int current = freeIndex + 1;
            while (current < Count)
            {
                while (current < Count && match(span[current]))
                    current++;
                if (current < Count)
                    span[freeIndex++] = span[current++];
            }
            int result = Count - freeIndex;
            RemoveRange(freeIndex, result);
            return result;
        }
        #endregion [批量移除]

        #region [转换方法]
        /// <summary>
        /// 将列表的元素复制到新数组中
        /// </summary>
        /// <returns>包含列表元素副本的数组</returns>
        public T[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<T>();
            var array = new T[Count];
            AsSpan().CopyTo(array);
            return array;
        }
        #endregion [转换方法]

        #region [查找方法 - Find系列]
        /// <summary>
        /// 搜索与指定谓词所定义的条件相匹配的元素，并返回整个列表中的第一个匹配元素
        /// </summary>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与指定谓词定义的条件匹配的第一个元素，则为该元素；否则为类型 T 的默认值</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public T Find(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (match(span[i]))
                    return span[i];
            }
            return default;
        }
        /// <summary>
        /// 检索与指定谓词定义的条件匹配的所有元素
        /// </summary>
        /// <param name="match">定义要搜索的元素应满足的条件</param>
        /// <returns>如果找到，则为一个包含与指定谓词定义的条件匹配的所有元素的列表；否则为空列表</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public OwList<T> FindAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            var result = new OwList<T>();
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (match(span[i]))
                    result.Add(span[i]);
            }
            return result;
        }
        /// <summary>
        /// 搜索与指定谓词所定义的条件相匹配的元素，并返回整个列表中第一个匹配元素的从零开始的索引
        /// </summary>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与 match 定义的条件匹配的第一个元素，则为该元素的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, Count, match);
        }
        /// <summary>
        /// 搜索与指定谓词所定义的条件相匹配的元素，并返回从指定索引到最后一个元素的元素范围内第一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="startIndex">从零开始的搜索的起始索引</param>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与 match 定义的条件匹配的第一个元素，则为该元素的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 超出列表的有效索引范围</exception>
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, Count - startIndex, match);
        }
        /// <summary>
        /// 搜索与指定谓词所定义的条件相匹配的元素，并返回从指定索引开始并包含指定元素数的元素范围内第一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="startIndex">从零开始的搜索的起始索引</param>
        /// <param name="count">要搜索的部分中的元素数</param>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与 match 定义的条件匹配的第一个元素，则为该元素的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 或 count 超出列表的有效范围</exception>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            if (startIndex < 0 || startIndex > Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || startIndex + count > Count)
                throw new ArgumentOutOfRangeException(nameof(count));
            var span = AsSpan(startIndex, count);
            for (int i = 0; i < span.Length; i++)
            {
                if (match(span[i]))
                    return startIndex + i;
            }
            return -1;
        }
        /// <summary>
        /// 搜索与指定谓词所定义的条件相匹配的元素，并返回整个列表中的最后一个匹配元素
        /// </summary>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与指定谓词定义的条件匹配的最后一个元素，则为该元素；否则为类型 T 的默认值</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public T FindLast(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            var span = AsSpan();
            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (match(span[i]))
                    return span[i];
            }
            return default;
        }
        /// <summary>
        /// 搜索与指定谓词所定义的条件相匹配的元素，并返回整个列表中最后一个匹配元素的从零开始的索引
        /// </summary>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与 match 定义的条件匹配的最后一个元素，则为该元素的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(Count - 1, Count, match);
        }
        /// <summary>
        /// 搜索与由指定谓词定义的条件相匹配的元素，并返回从第一个元素到指定索引的元素范围内最后一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="startIndex">向后搜索的从零开始的起始索引</param>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与 match 定义的条件匹配的最后一个元素，则为该元素的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 超出列表的有效索引范围</exception>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            if (Count == 0)
                return -1;
            return FindLastIndex(startIndex, startIndex + 1, match);
        }
        /// <summary>
        /// 搜索与指定谓词所定义的条件相匹配的元素，并返回包含指定元素数并在指定索引处结束的元素范围内最后一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="startIndex">向后搜索的从零开始的起始索引</param>
        /// <param name="count">要搜索的部分中的元素数</param>
        /// <param name="match">定义要搜索的元素的条件</param>
        /// <returns>如果找到与 match 定义的条件匹配的最后一个元素，则为该元素的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 或 count 超出列表的有效范围</exception>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            if (Count == 0)
                return -1;
            if (startIndex < 0 || startIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            var span = AsSpan();
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (match(span[i]))
                    return i;
            }
            return -1;
        }
        /// <summary>
        /// 确定列表是否包含与指定谓词所定义的条件相匹配的元素
        /// </summary>
        /// <param name="match">定义要搜索的元素应满足的条件</param>
        /// <returns>如果列表包含一个或多个与指定谓词定义的条件相匹配的元素，则为 true；否则为 false</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public bool Exists(Predicate<T> match)
        {
            return FindIndex(match) != -1;
        }
        /// <summary>
        /// 确定列表中的每个元素是否都与指定谓词所定义的条件相匹配
        /// </summary>
        /// <param name="match">定义要对元素进行检查的条件</param>
        /// <returns>如果列表中的每个元素都与指定谓词定义的条件匹配，则为 true；否则为 false。如果列表没有元素，则返回值为 true</returns>
        /// <exception cref="ArgumentNullException">match 为 null</exception>
        public bool TrueForAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (!match(span[i]))
                    return false;
            }
            return true;
        }
        #endregion [查找方法 - Find系列]

        #region [反转方法]
        /// <summary>
        /// 将整个列表中元素的顺序反转
        /// </summary>
        public void Reverse()
        {
            Reverse(0, Count);
        }
        /// <summary>
        /// 将指定范围中元素的顺序反转
        /// </summary>
        /// <param name="index">要反转的范围的从零开始的起始索引</param>
        /// <param name="count">要反转的范围内的元素数</param>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 小于 0</exception>
        /// <exception cref="ArgumentException">index 和 count 不表示列表中元素的有效范围</exception>
        public void Reverse(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index + count > Count)
                throw new ArgumentException("索引和数量超出列表范围");
            if (count > 1)
            {
                var span = AsSpan(index, count);
                span.Reverse();
            }
        }
        #endregion [反转方法]

        #region [其他便利方法]
        /// <summary>
        /// 对列表中的每个元素执行指定操作
        /// </summary>
        /// <param name="action">要对列表的每个元素执行的委托</param>
        /// <exception cref="ArgumentNullException">action 为 null</exception>
        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                action(span[i]);
            }
        }
        /// <summary>
        /// 创建当前列表中元素范围的浅表副本
        /// </summary>
        /// <param name="index">范围开始处的从零开始的索引</param>
        /// <param name="count">范围中的元素数</param>
        /// <returns>当前列表中元素范围的浅表副本</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 小于 0</exception>
        /// <exception cref="ArgumentException">index 和 count 不表示列表中元素的有效范围</exception>
        public OwList<T> GetRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index + count > Count)
                throw new ArgumentException("索引和数量超出列表范围");
            var result = new OwList<T>(count);
            if (count > 0)
            {
                var span = AsSpan(index, count);
                for (int i = 0; i < span.Length; i++)
                {
                    result.Add(span[i]);
                }
            }
            return result;
        }
        /// <summary>
        /// 将列表的元素复制到新数组中
        /// </summary>
        /// <param name="array">作为列表中的元素的目标位置的一维数组</param>
        /// <exception cref="ArgumentNullException">array 为 null</exception>
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }
        /// <summary>
        /// 从目标数组的指定索引处开始，将列表中某个范围的元素复制到兼容的一维数组
        /// </summary>
        /// <param name="index">源列表中复制开始位置的从零开始的索引</param>
        /// <param name="array">作为从列表复制的元素的目标位置的一维数组</param>
        /// <param name="arrayIndex">目标数组中从零开始的索引，从此处开始复制</param>
        /// <param name="count">要复制的元素数</param>
        /// <exception cref="ArgumentNullException">array 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">index、arrayIndex 或 count 小于 0</exception>
        /// <exception cref="ArgumentException">index 和 count 不表示列表中元素的有效范围，或者 arrayIndex 和 count 不表示 array 中元素的有效范围</exception>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index + count > Count)
                throw new ArgumentException("索引和数量超出列表范围");
            if (arrayIndex + count > array.Length)
                throw new ArgumentException("数组空间不足");
            AsSpan(index, count).CopyTo(array.AsSpan(arrayIndex));
        }
        #endregion [其他便利方法]

        #region [排序方法]
        /// <summary>
        /// 使用默认比较器对整个列表中的元素进行排序
        /// </summary>
        public void Sort()
        {
            Sort(0, Count, null);
        }
        /// <summary>
        /// 使用指定的比较器对整个列表中的元素进行排序
        /// </summary>
        /// <param name="comparer">比较元素时要使用的 IComparer&lt;T&gt; 实现，或者为 null 以使用默认比较器</param>
        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }
        /// <summary>
        /// 使用指定的比较委托对整个列表中的元素进行排序
        /// </summary>
        /// <param name="comparison">比较元素时要使用的比较委托</param>
        /// <exception cref="ArgumentNullException">comparison 为 null</exception>
        public void Sort(Comparison<T> comparison)
        {
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            if (Count > 1)
            {
                var span = AsSpan();
                var comparer = Comparer<T>.Create(comparison);
                span.Sort(comparer);
            }
        }
        /// <summary>
        /// 使用指定的比较器对列表中某个范围内的元素进行排序
        /// </summary>
        /// <param name="index">要排序的范围的从零开始的起始索引</param>
        /// <param name="count">要排序的范围的长度</param>
        /// <param name="comparer">比较元素时要使用的 IComparer&lt;T&gt; 实现，或者为 null 以使用默认比较器</param>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 小于 0</exception>
        /// <exception cref="ArgumentException">index 和 count 不表示列表中元素的有效范围</exception>
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index + count > Count)
                throw new ArgumentException("索引和数量超出列表范围");
            if (count > 1)
            {
                var span = AsSpan(index, count);
                span.Sort(comparer);
            }
        }
        #endregion [排序方法]

        #region [二分查找]
        /// <summary>
        /// 使用默认比较器在整个已排序的列表中搜索元素，并返回该元素从零开始的索引
        /// </summary>
        /// <param name="item">要定位的对象</param>
        /// <returns>如果找到 item，则为已排序列表中 item 的从零开始的索引；否则为负数，该负数是大于 item 的第一个元素的索引的按位求补</returns>
        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, null);
        }
        /// <summary>
        /// 使用指定的比较器在整个已排序的列表中搜索元素，并返回该元素从零开始的索引
        /// </summary>
        /// <param name="item">要定位的对象</param>
        /// <param name="comparer">比较元素时要使用的 IComparer&lt;T&gt; 实现，或者为 null 以使用默认比较器</param>
        /// <returns>如果找到 item，则为已排序列表中 item 的从零开始的索引；否则为负数，该负数是大于 item 的第一个元素的索引的按位求补</returns>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }
        /// <summary>
        /// 使用指定的比较器在已排序列表的某个元素范围中搜索元素，并返回该元素从零开始的索引
        /// </summary>
        /// <param name="index">要搜索的范围的从零开始的起始索引</param>
        /// <param name="count">要搜索的范围的长度</param>
        /// <param name="item">要定位的对象</param>
        /// <param name="comparer">比较元素时要使用的 IComparer&lt;T&gt; 实现，或者为 null 以使用默认比较器</param>
        /// <returns>如果找到 item，则为已排序列表中 item 的从零开始的索引；否则为负数，该负数是大于 item 的第一个元素的索引的按位求补</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 小于 0</exception>
        /// <exception cref="ArgumentException">index 和 count 不表示列表中元素的有效范围</exception>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index + count > Count)
                throw new ArgumentException("索引和数量超出列表范围");
            comparer ??= Comparer<T>.Default;
            int lo = index;
            int hi = index + count - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int c = comparer.Compare(this[mid], item);
                if (c == 0)
                    return mid;
                if (c < 0)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }
            return ~lo;
        }
        #endregion [二分查找]

        #region [转换方法 - ConvertAll]
        /// <summary>
        /// 将当前列表中的元素转换为另一种类型，并返回包含转换后的元素的列表
        /// </summary>
        /// <typeparam name="TOutput">目标数组元素的类型</typeparam>
        /// <param name="converter">用于将每个元素从一种类型转换为另一种类型的委托</param>
        /// <returns>目标类型的列表，包含从当前列表转换而来的元素</returns>
        /// <exception cref="ArgumentNullException">converter 为 null</exception>
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            var result = new List<TOutput>(Count);
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                result.Add(converter(span[i]));
            }
            return result;
        }
        #endregion [转换方法 - ConvertAll]

        #region [只读包装]
        /// <summary>
        /// 返回当前集合的只读 ReadOnlyCollection&lt;T&gt; 包装
        /// </summary>
        /// <returns>一个作为当前列表的只读包装的 ReadOnlyCollection&lt;T&gt;</returns>
        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }
        #endregion [只读包装]
    }
}

