/*
 * 项目：OwBaseCore | 模块：System.Collections.Generic
 * 功能：基于内存池的列表基类，实现 IList<T> 接口
 * 技术要点：
 *   - 继承自 OwCollectionBase<T>
 *   - 实现 IList<T> 完整接口
 *   - 引用传递避免复制
 * 作者：zc | 创建：2025-01 | 修改：2025-01-21 重构为派生类
 */
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// 基于内存池的列表基类，提供完整的 IList&lt;T&gt; 实现。
    /// 继承自 OwCollectionBase&lt;T&gt;，专注于列表操作。
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    public class OwListBase<T> : OwCollectionBase<T>, IList<T>
    {

        #region [构造函数]
        /// <summary>
        /// 初始化 OwListBase 类的新实例，使用默认的 ArrayMemoryPool 进行内存管理
        /// </summary>
        public OwListBase() : base(0, null)
        {
        }

        /// <summary>
        /// 初始化 OwListBase 类的新实例，使用默认的 ArrayMemoryPool 和指定的初始容量
        /// </summary>
        /// <param name="capacity">新列表最初可以存储的元素数</param>
        public OwListBase(int capacity) : base(capacity, null)
        {
        }

        /// <summary>
        /// 初始化 OwListBase 类的新实例，使用指定的 MemoryPool。
        /// 如果 pool 为 null，则创建新的 ArrayMemoryPool&lt;T&gt; 实例并在 Dispose 时释放。
        /// </summary>
        /// <param name="capacity">新列表最初可以存储的元素数</param>
        /// <param name="pool">用于内存管理的 MemoryPool，如果为 null 则创建默认的 ArrayMemoryPool</param>
        public OwListBase(int capacity, MemoryPool<T> pool) : base(capacity, pool)
        {
        }
        #endregion [构造函数]

        #region [列表特定方法]
        /// <summary>
        /// 在列表中的指定索引处插入项（通过引用传递，避免大结构体复制）
        /// </summary>
        /// <param name="index">从零开始的索引，应在该位置插入 item</param>
        /// <param name="item">要插入的对象的引用</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于 Count</exception>
        public void InsertByRef(int index, in T item)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            EnsureCapacity(_count + 1);
            var span = _memory.Span;
            if (index < _count)
            {
                span.Slice(index, _count - index).CopyTo(span.Slice(index + 1));
            }
            span[index] = item;
            _count++;
        }
        /// <summary>
        /// 在指定索引处插入指定数量的相同元素（通过引用传递，避免复制）
        /// </summary>
        /// <param name="index">从零开始的索引，应在该位置插入新元素</param>
        /// <param name="count">要插入的元素数量</param>
        /// <param name="item">要插入的元素的引用</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于 Count，或 count 小于 0</exception>
        public void InsertRange(int index, int count, in T item)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;
            EnsureCapacity(_count + count);
            var span = _memory.Span;
            if (index < _count)
            {
                span.Slice(index, _count - index).CopyTo(span.Slice(index + count));
            }
            span.Slice(index, count).Fill(item);
            _count += count;
        }

        /// <summary>
        /// 在指定索引处插入集合中的元素。此方法是 OwListBase 的底层核心操作，
        /// 负责扩容、内存搬移、批量写入以及更新元素计数。派生类不得重写或自行实现。
        /// </summary>
        /// <param name="index">插入位置，必须满足 0 ≤ index ≤ Count。index == Count 表示追加。</param>
        /// <param name="collection">要插入的元素集合，不允许为 null。</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">index 越界</exception>
        /// <exception cref="NotSupportedException">禁止对自身执行 InsertRange</exception>
        /// <exception cref="OutOfMemoryException">扩容失败</exception>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (ReferenceEquals(collection, this))  //仅检测是否对自身执行插入操作，不同实例空间不会重叠
                throw new NotSupportedException("InsertRange 不支持对自身执行插入操作。");
            int count;
            Span<T> span;
            if (TryGetSpanFromCollection(collection, out var sourceSpan))
            {
                count = sourceSpan.Length;
                if (count == 0) return;
                EnsureCapacity(_count + count);
                span = _memory.Span;
                if (index < _count)
                    span.Slice(index, _count - index).CopyTo(span.Slice(index + count));
                sourceSpan.CopyTo(span.Slice(index));
                _count += count;
                return;
            }
            if (collection.TryGetNonEnumeratedCount(out count))
            {
                if (count == 0) return;
                EnsureCapacity(_count + count);
                span = _memory.Span;
                if (index < _count)
                    span.Slice(index, _count - index).CopyTo(span.Slice(index + count));
                int write = index;
                foreach (var item in collection)
                    span[write++] = item;
                _count += count;
                return;
            }
            var ary = collection.ToArray(); //强制枚举以获取准确数量
            count = ary.Length;
            if (count == 0) return;
            EnsureCapacity(_count + count);
            span = _memory.Span;
            if (index < _count)
                span.Slice(index, _count - index).CopyTo(span.Slice(index + count));
            ary.CopyTo(span.Slice(index));
            _count += count;
            return;
        }

        /// <summary>
        /// 从列表中移除指定范围的元素
        /// </summary>
        /// <param name="index">要移除的元素范围的起始索引</param>
        /// <param name="count">要移除的元素数量</param>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出有效范围</exception>
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "移除数量不能为负数");
            if (index + count > _count)
                throw new ArgumentOutOfRangeException(nameof(count), $"索引 ({index}) 和数量 ({count}) 超出列表范围 ({_count})");
            if (count == 0)
                return;
            var span = _memory.Span;
            if (index + count < _count)
            {
                span.Slice(index + count, _count - index - count).CopyTo(span.Slice(index));
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                span.Slice(_count - count, count).Clear();
            }
            _count -= count;
        }

        /// <summary>
        /// 搜索指定的对象，并返回列表中从指定的索引到最后一个元素的元素范围内第一个匹配项的从零开始的索引
        /// </summary>
        /// <param name="item">要在列表中定位的对象</param>
        /// <param name="index">从零开始的搜索的起始索引</param>
        /// <param name="count">要搜索的部分中的元素数</param>
        /// <returns>如果在列表中找到 item 的第一个匹配项，则为该项的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出列表的有效范围</exception>
        public int IndexOf(T item, int index, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > _count)
                throw new ArgumentOutOfRangeException(nameof(count));
            var comparer = EqualityComparer<T>.Default;
            var span = AsSpan(index, count);
            for (int i = 0; i < span.Length; i++)
            {
                if (comparer.Equals(span[i], item))
                    return index + i;
            }
            return -1;
        }
        #endregion [列表特定方法]

        #region [IList<T> Members]
        /// <summary>
        /// 获取或设置指定索引处的元素
        /// </summary>
        /// <param name="index">要获取或设置的元素的从零开始的索引</param>
        /// <returns>指定索引处的元素</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于等于 Count</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _memory.Span[index];
            }
            set
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _memory.Span[index] = value;
            }
        }

        /// <summary>
        /// 确定列表中特定项的索引
        /// </summary>
        /// <param name="item">要在列表中定位的对象</param>
        /// <returns>如果在列表中找到 item，则为该项的索引；否则为 -1</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
        {
            return IndexOf(item, 0, _count);
        }

        /// <summary>
        /// 在列表中的指定索引处插入项
        /// </summary>
        /// <param name="index">从零开始的索引，应在该位置插入 item</param>
        /// <param name="item">要插入的对象</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于 Count</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            InsertByRef(index, in item);
        }
        /// <summary>
        /// 移除指定索引处的元素
        /// </summary>
        /// <param name="index">要移除的元素的从零开始的索引</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于等于 Count</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _count--;
            var span = _memory.Span;
            if (index < _count)
            {
                span.Slice(index + 1, _count - index).CopyTo(span.Slice(index));
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                span[_count] = default;
            }
        }
        #endregion [IList<T> Members]

        #region [ICollection<T> Members]
        /// <summary>
        /// 获取一个值，该值指示列表是否为只读
        /// </summary>
        public bool IsReadOnly => false;
        /// <summary>
        /// 将对象添加到列表的结尾处
        /// </summary>
        /// <param name="item">要添加到列表的对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            InsertByRef(_count, in item);
        }
        /// <summary>
        /// 从列表中移除所有元素
        /// </summary>
        public void Clear()
        {
            if (_count > 0)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    _memory.Span.Slice(0, _count).Clear();
                }
                _count = 0;
            }
        }
        /// <summary>
        /// 确定列表是否包含特定值
        /// </summary>
        /// <param name="item">要在列表中定位的对象</param>
        /// <returns>如果在列表中找到 item，则为 true；否则为 false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }
        /// <summary>
        /// 从特定的数组索引开始，将列表的元素复制到一个数组中
        /// </summary>
        /// <param name="array">作为从列表复制的元素的目标的一维数组</param>
        /// <param name="arrayIndex">array 中从零开始的索引，从此处开始复制</param>
        /// <exception cref="ArgumentNullException">array 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex 小于 0</exception>
        /// <exception cref="ArgumentException">array 空间不足</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < _count)
                throw new ArgumentException("目标数组空间不足");
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }
        /// <summary>
        /// 从列表中移除特定对象的第一个匹配项
        /// </summary>
        /// <param name="item">要从列表中移除的对象</param>
        /// <returns>如果成功移除 item，则为 true；否则为 false</returns>
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
        /// <summary>
        /// 返回一个循环访问集合的枚举器
        /// </summary>
        /// <returns>用于循环访问集合的枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _memory.Span[i];
            }
        }
        #endregion [IEnumerable<T> Members]

        #region [IEnumerable Members]
        /// <summary>
        /// 返回一个循环访问集合的枚举器
        /// </summary>
        /// <returns>可用于循环访问集合的 IEnumerator 对象</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion [IEnumerable Members]
    }
}
