/*
* 项目：OwBaseCore | 模块：System.Collections.Generic
* 功能：基于内存池的列表类，提供完整 List<T> 兼容功能
* 技术要点：组合 OwCollection<T> 提供内存管理，实现 IList<T> 接口，版本检查防止枚举期间修改
* 作者：zc | 创建：2025-01 | 修改：2025-01-21 添加版本检查和结构体枚举器
*/
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace OW.Collections.Generic
{
    /// <summary>
    /// 基于内存池的列表类，提供完整的 List&lt;T&gt; 兼容功能。
    /// 使用 OwCollection&lt;T&gt; 提供内存管理，实现 IList&lt;T&gt; 接口及扩展方法。
    /// 默认情况下使用 ArrayMemoryPool&lt;T&gt; 包装 ArrayPool&lt;T&gt;.Shared，提供最佳性能。
    /// </summary>
    /// <remarks>
    /// <para><b>高性能通道与使用契约：</b></para>
    /// <para>本类型刻意暴露"底层直达"的高性能通道，提供零拷贝/低分配/缓存友好的性能：</para>
    /// <list type="bullet">
    /// <item><description><see cref="AsSpan()"/> / <see cref="AsSpan(int,int)"/> - 直接访问底层连续内存</description></item>
    /// <item><description><see cref="GetByRef(int)"/> - 返回元素引用，避免值拷贝</description></item>
    /// <item><description><see cref="InsertRange(int,int,bool)"/> - 预留空间并返回 Span，调用者直接写入</description></item>
    /// <item><description><see cref="RemoveRange(int,int)"/> - 批量移除，避免逐个删除的开销</description></item>
    /// </list>
    /// <para><b>⚠️ 使用这些高性能方法时必须遵守以下契约：</b></para>
    /// <list type="number">
    /// <item><description><b>Span 视图生命周期：</b>在任何 Span 视图存活期间，不得进行结构性修改（Add/Insert/Remove/Clear/Sort/Reverse 等）。
    /// 结构性修改可能导致内存重新分配，使 Span 引用失效，继续使用将产生未定义行为。</description></item>
    /// <item><description><b>引用短期使用：</b><c>GetByRef(int)</c> 返回的引用只可短期使用；
    /// 一旦发生结构性修改（包括扩容），该引用即失效。不得长期持有或跨方法调用保存。</description></item>
    /// <item><description><b>枚举期间修改检测：</b>与 <c>List&lt;T&gt;</c> 一致，枚举期间的结构性修改会抛出 <see cref="InvalidOperationException"/>。
    /// 这是通过版本号机制实现的快速失败保护。</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class OwList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IDisposable
    {
        private readonly OwCollection<T> _collection;
        private int _version;
        #region [构造函数]
        /// <summary>
        /// 初始化 OwList 类的新实例，使用默认的 ArrayMemoryPool 进行内存管理
        /// </summary>
        public OwList()
        {
            _collection = new OwCollection<T>();
        }
        /// <summary>
        /// 初始化 OwList 类的新实例，使用指定的 MemoryPool。
        /// 如果 pool 为 null，则创建新的 ArrayMemoryPool&lt;T&gt; 实例并在 Dispose 时释放。
        /// </summary>
        /// <param name="capacity">新列表最初可以存储的元素数</param>
        /// <param name="pool">用于内存管理的 MemoryPool，如果为 null 则创建默认的 ArrayMemoryPool</param>
        public OwList(int capacity, MemoryPool<T> pool = null)
        {
            _collection = new OwCollection<T>(capacity, pool);
        }
        /// <summary>
        /// 初始化 OwList 类的新实例，使用指定集合的元素和可选的内存池
        /// </summary>
        /// <param name="collection">要复制到新列表中的集合</param>
        /// <param name="pool">用于内存管理的 MemoryPool，如果为 null 则创建默认的 ArrayMemoryPool</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public OwList(IEnumerable<T> collection, MemoryPool<T> pool = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            _collection = new OwCollection<T>(0, pool);
            AddRange(collection);
        }
        #endregion [构造函数]
        #region [属性]
        /// <summary>
        /// 获取列表当前的容量（可存储的最大元素数）
        /// </summary>
        public int Capacity => _collection.Capacity;
        /// <summary>
        /// 获取列表中当前包含的元素数
        /// </summary>
        public int Count => _collection.Count;
        #endregion [属性]
        #region [高性能核心方法]
        /// <summary>
        /// 返回包含列表中所有元素的 Span，提供高性能的直接内存访问
        /// </summary>
        /// <returns>包含列表元素的 Span，如果列表为空则返回空 Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => _collection.AsSpan();
        /// <summary>
        /// 返回包含列表中指定范围元素的 Span
        /// </summary>
        /// <param name="start">范围的起始索引</param>
        /// <param name="length">范围中的元素数</param>
        /// <returns>包含指定范围元素的 Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int length) => _collection.AsSpan(start, length);
        /// <summary>
        /// 获取指定索引处元素的引用，实现零拷贝访问
        /// </summary>
        /// <param name="index">要获取引用的元素的从零开始的索引</param>
        /// <returns>指定索引处元素的引用</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetByRef(int index) => ref _collection.GetByRef(index);
        /// <summary>
        /// 在指定索引处插入元素（通过引用传递，避免大结构体复制）
        /// </summary>
        /// <param name="index">从零开始的索引，应在该位置插入 item</param>
        /// <param name="item">要插入的元素的引用</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertByRef(int index, in T item)
        {
            _collection.InsertByRef(index, in item);
            _version++;
        }
        /// <summary>
        /// 在指定索引处插入指定数量的空间，并返回预留区域的 Span
        /// </summary>
        /// <param name="index">插入位置的从零开始的索引</param>
        /// <param name="count">要插入的元素数量</param>
        /// <param name="clear">是否清零预留区域</param>
        /// <returns>预留区域的 Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> InsertRange(int index, int count, bool clear = false)
        {
            var result = _collection.InsertRange(index, count, clear);
            if (count > 0) _version++;
            return result;
        }
        /// <summary>
        /// 从列表中移除指定范围的元素
        /// </summary>
        /// <param name="index">要移除的元素范围的起始索引</param>
        /// <param name="count">要移除的元素数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveRange(int index, int count)
        {
            _collection.RemoveRange(index, count);
            if (count > 0) _version++;
        }
        #endregion [高性能核心方法]
        #region [内存管理]
        /// <summary>
        /// 将容量设置为列表中的实际元素数（如果该数小于当前容量的 90%）
        /// </summary>
        /// <returns>如果执行了收缩则返回 true，否则返回 false</returns>
        public bool TrimExcess() => _collection.TrimExcess();
        /// <summary>
        /// 固定列表的内存，防止 GC 移动，返回可用于非托管代码的句柄
        /// </summary>
        /// <returns>内存句柄</returns>
        public MemoryHandle Pin() => _collection.Pin();
        #endregion [内存管理]
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
        /// <summary>
        /// 在指定索引处插入集合中的元素
        /// </summary>
        /// <param name="index">插入位置</param>
        /// <param name="collection">要插入的元素集合</param>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            if (ReferenceEquals(collection, this))
                throw new NotSupportedException("InsertRange 不支持对自身执行插入操作。");
            if (OwCollection<T>.TryGetSpanFromCollection(collection, out var sourceSpan))
            {
                if (sourceSpan.Length == 0) return;
                sourceSpan.CopyTo(InsertRange(index, sourceSpan.Length, false));
                return;
            }
            if (collection.TryGetNonEnumeratedCount(out int count))
            {
                if (count == 0) return;
                var span = InsertRange(index, count, false);
                int write = 0;
                foreach (var item in collection) span[write++] = item;
                return;
            }
            var ary = collection.ToArray();
            if (ary.Length > 0)
                ary.CopyTo(InsertRange(index, ary.Length, false));
        }
        /// <summary>
        /// 在指定位置插入多个相同的元素
        /// </summary>
        /// <param name="index">插入位置</param>
        /// <param name="count">要插入的元素数量</param>
        /// <param name="item">要插入的元素</param>
        public void InsertRange(int index, int count, in T item)
        {
            if (count <= 0) return;
            InsertRange(index, count, false).Fill(item);
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
                _version++;
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
                _version++;
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
                _version++;
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
                int mid = lo + (hi - lo >> 1);
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
        #region [接口成员实现]
        #region [IList<T> Members]
        public T this[int index]
        {
            get => GetByRef(index);
            set => GetByRef(index) = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (comparer.Equals(span[i], item))
                    return i;
            }
            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            InsertByRef(index, in item);
        }
        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }
        #endregion [IList<T> Members]
        #region [ICollection<T> Members]
        public bool IsReadOnly => false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            InsertByRef(Count, in item);
        }
        public void Clear()
        {
            if (Count > 0)
            {
                RemoveRange(0, Count);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item) => IndexOf(item) >= 0;
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("目标数组空间不足");
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }
        #endregion [ICollection<T> Members]
        #region [IEnumerable<T> Members]
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion [IEnumerable<T> Members]
        #region [IDisposable Members]
        /// <summary>
        /// 释放由列表使用的所有资源
        /// </summary>
        public void Dispose()
        {
            _collection?.Dispose();
        }
        #endregion [IDisposable Members]
        #endregion [接口成员实现]
        #region [枚举器]
        /// <summary>
        /// OwList&lt;T&gt; 的枚举器（结构体，零分配，带版本检查）
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly OwList<T> _list;
            private readonly int _version;
            private int _index;
            private T _current;
            internal Enumerator(OwList<T> list)
            {
                _list = list;
                _version = list._version;
                _index = 0;
                _current = default;
            }
            public T Current => _current;
            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list.Count + 1)
                        throw new InvalidOperationException("枚举器位置无效");
                    return Current;
                }
            }
            public bool MoveNext()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("集合在枚举期间被修改");
                if (_index < _list.Count)
                {
                    _current = _list.GetByRef(_index);
                    _index++;
                    return true;
                }
                _index = _list.Count + 1;
                _current = default;
                return false;
            }
            public void Reset()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("集合在枚举期间被修改");
                _index = 0;
                _current = default;
            }
            public void Dispose()
            {
            }
        }
        #endregion [枚举器]
    }
}
