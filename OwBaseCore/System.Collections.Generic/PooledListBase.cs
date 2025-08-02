using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Generic
{
    /// <summary>
    /// 基于 ArrayPool 池化数组的列表基类，实现 IList&lt;T&gt; 接口
    /// </summary>
    /// <typeparam name="T">列表中的元素类型</typeparam>
    public class PooledListBase<T> : IList<T>, IDisposable
    {
        private T[] _Buffer;          // 存储元素的池化数组

        /// <summary>
        /// 内部数据的数组，它从0开始有<see cref="Count"/>个有效数据。数组的长度是容量<see cref="Capacity"/>。
        /// </summary>
        protected T[] Buffer => _Buffer;

        private int _Count;           // 列表中当前元素数量
        private bool _isDisposed;    // 对象是否已被释放
        private const int DefaultCapacity = 8;  // 默认初始容量

        /// <summary>
        /// 使用默认容量初始化 PooledListBase 实例
        /// </summary>
        public PooledListBase()
        {
            _Buffer = ArrayPool<T>.Shared.Rent(DefaultCapacity);
            _Count = 0;
        }

        /// <summary>
        /// 使用指定的初始容量初始化 PooledListBase 实例
        /// </summary>
        /// <param name="capacity">初始容量</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity 小于 0 时抛出</exception>
        public PooledListBase(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "容量不能为负数");

            _Buffer = capacity == 0 ? Array.Empty<T>() : ArrayPool<T>.Shared.Rent(capacity);
            _Count = 0;
        }

        /// <summary>
        /// 从指定的集合初始化 PooledListBase 实例
        /// </summary>
        /// <param name="collection">用于初始化列表的集合</param>
        /// <exception cref="ArgumentNullException">collection 为 null 时抛出</exception>
        public PooledListBase(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            // 先初始化缓冲区，然后添加元素
            _Buffer = ArrayPool<T>.Shared.Rent(DefaultCapacity);
            _Count = 0;
            AddRange(collection);
        }

        #region IList<T> 和 ICollection<T> 接口实现

        /// <summary>
        /// 列表中的元素数量
        /// </summary>
        public int Count => _Count;

        /// <summary>
        /// 列表是否为只读（始终返回 false）
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 获取或设置指定索引处的元素
        /// </summary>
        /// <param name="index">要获取或设置的元素的索引</param>
        /// <returns>指定索引处的元素</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于等于 Count</exception>
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _Buffer[index];
            }
            set
            {
                if ((uint)index >= (uint)_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _Buffer[index] = value;
            }
        }

        /// <summary>
        /// 将元素添加到列表末尾
        /// </summary>
        /// <param name="item">要添加的元素</param>
        public void Add(T item)
        {
            if (_Count == _Buffer.Length)
            {
                EnsureCapacity(_Count + 1);
            }

            _Buffer[_Count++] = item;
        }

        /// <summary>
        /// 在指定索引处插入元素
        /// </summary>
        /// <param name="index">要插入元素的位置</param>
        /// <param name="item">要插入的元素</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于 Count</exception>
        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_Count == _Buffer.Length)
            {
                EnsureCapacity(_Count + 1);
            }

            if (index < _Count)
            {
                Array.Copy(_Buffer, index, _Buffer, index + 1, _Count - index);
            }

            _Buffer[index] = item;
            _Count++;
        }

        /// <summary>
        /// 从列表中移除指定元素的第一个匹配项
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>如果成功移除元素，则为 true；否则为 false</returns>
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

        /// <summary>
        /// 移除指定索引处的元素
        /// </summary>
        /// <param name="index">要移除的元素的索引</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于等于 Count</exception>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _Count--;

            if (index < _Count)
            {
                Array.Copy(_Buffer, index + 1, _Buffer, index, _Count - index);
            }

            // 防止保留引用
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _Buffer[_Count] = default;
            }
        }

        /// <summary>
        /// 从列表中移除所有元素
        /// </summary>
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_Buffer, 0, _Count);
            }

            _Count = 0;
        }

        /// <summary>
        /// 返回元素在列表中首次出现的索引
        /// </summary>
        /// <param name="item">要在列表中查找的元素</param>
        /// <returns>如果找到元素，则为该元素的索引；否则为 -1</returns>
        public int IndexOf(T item)
        {
            return Array.IndexOf(_Buffer, item, 0, _Count);
        }

        /// <summary>
        /// 确定列表是否包含特定元素
        /// </summary>
        /// <param name="item">要在列表中查找的元素</param>
        /// <returns>如果在列表中找到该元素，则为 true；否则为 false</returns>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>
        /// 将列表的元素复制到数组中
        /// </summary>
        /// <param name="array">作为复制目标的一维数组</param>
        /// <param name="arrayIndex">array 中从零开始的索引，从此处开始复制</param>
        /// <exception cref="ArgumentNullException">array 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex 小于 0</exception>
        /// <exception cref="ArgumentException">源中的元素数目超出从 arrayIndex 到目标数组末尾的可用空间</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length - _Count)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            Array.Copy(_Buffer, 0, array, arrayIndex, _Count);
        }

        /// <summary>
        /// 返回循环访问集合的枚举器
        /// </summary>
        /// <returns>可用于循环访问集合的枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _Count; i++)
            {
                yield return _Buffer[i];
            }
        }

        /// <summary>
        /// 返回循环访问集合的枚举器
        /// </summary>
        /// <returns>可用于循环访问集合的枚举器</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// 列表的当前容量，只读属性
        /// </summary>
        public int Capacity => _Buffer.Length;

        /// <summary>
        /// 调整列表的容量大小
        /// </summary>
        /// <remarks>如果 newSize 大于 Length 旧数组，则会分配一个新数组，并将所有元素从旧数组复制到新数组。
        /// 如果 newSize 小于 Length 旧数组，则会分配一个新数组，并将元素从旧数组复制到新数组，直到填充新数组;旧数组中的其余元素将被忽略。
        /// 如果 newSize 等于 Length 旧数组，则此方法不执行任何作用。</remarks>
        /// <param name="buffer">要调整大小的数组的引用</param>
        /// <param name="count">数组中有效元素的数量的引用</param>
        /// <param name="newSize">新的容量大小</param>
        public static void Resize<TElement>(ref TElement[] buffer, ref int count, int newSize)
        {
            // 如果大小相同，无需操作
            if (newSize == buffer.Length)
                return;

            // 处理新大小为0的情况
            if (newSize == 0)
            {
                ReturnPooledArray(ref buffer);
                count = 0;  // 重置元素计数，因为没有存储空间
                return;
            }

            // 分配新数组
            var newItems = ArrayPool<TElement>.Shared.Rent(newSize);

            // 检查是否由于内存对齐得到了相同大小的数组
            if (newItems.Length == buffer.Length)
            {
                ArrayPool<TElement>.Shared.Return(newItems);
                return;
            }

            // 只在有数据时复制
            if (count > 0)
                Array.Copy(buffer, 0, newItems, 0, Math.Min(newSize, count));

            // 归还旧数组，设置新数组
            ReturnPooledArray(ref buffer);
            buffer = newItems;
            count = Math.Min(newSize, count);
        }

        /// <summary>
        /// 将数组归还到数组池
        /// </summary>
        /// <param name="array">要归还的数组的引用，归还后会被设置为空数组</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledArray<TElement>(ref TElement[] array)
        {
            // 使用 Exchange 原子地替换数组并获取旧值
            var toReturn = Interlocked.Exchange(ref array, Array.Empty<TElement>());

            // 如果没有数组需要返回，直接退出
            if (toReturn == null || toReturn.Length == 0)
                return;

            try
            {
                // 直接使用 clearArray: true 让 ArrayPool 内部处理清理工作
                ArrayPool<TElement>.Shared.Return(toReturn, clearArray: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"在归还数组到 ArrayPool 时出错: {ex.Message}");
            }
        }

        #region IDisposable 接口实现

        /// <summary>
        /// 释放由 PooledListBase 使用的所有资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放由 PooledListBase 使用的非托管资源，并指示是否也应释放托管资源
        /// </summary>
        /// <param name="disposing">如果应该释放托管资源，则为 true；否则为 false</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    ReturnPooledArray(ref _Buffer);
                }

                // 释放非托管资源
                _isDisposed = true;
            }
        }

        #endregion

        /// <summary>
        /// 将现有集合中的元素添加到列表的末尾。
        /// </summary>
        /// <param name="collection">要添加的集合,可以是空的集合。</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_Count + count);
                    c.CopyTo(_Buffer, _Count);
                    _Count += count;
                }
            }
            else
            {
                foreach (var item in collection)
                {
                    Add(item);
                }
            }
        }

        /// <summary>
        /// 对列表执行操作以减少未使用的内存
        /// </summary>
        public void TrimExcess()
        {
            if (_Count < _Buffer.Length * 0.9)
            {
                Resize(ref _Buffer, ref _Count, _Count);
            }
        }

        /// <summary>
        /// 确保列表具有足够的容量来容纳指定数量的元素
        /// </summary>
        /// <param name="min">所需的最小容量</param>
        public void EnsureCapacity(int min)
        {
            // 确保_Buffer已经初始化
            if (_Buffer is null)
            {
                _Buffer = ArrayPool<T>.Shared.Rent(Math.Max(min, DefaultCapacity));
                return;
            }

            if (_Buffer.Length < min)
            {
                int newCapacity = _Buffer.Length == 0 ? DefaultCapacity : _Buffer.Length * 2;

                if (newCapacity < min)
                    newCapacity = min;

                Resize(ref _Buffer, ref _Count, newCapacity);
            }
        }

    }

    /// <summary>
    /// 为 IEnumerable&lt;T&gt; 提供扩展方法
    /// </summary>
    public static class PooledListBaseExtensions
    {
        /// <summary>
        /// 从 IEnumerable&lt;T&gt; 创建一个 PooledListBase&lt;T&gt;
        /// </summary>
        /// <typeparam name="TSource">源集合元素的类型</typeparam>
        /// <param name="source">要转换的 IEnumerable&lt;T&gt; 集合</param>
        /// <returns>包含输入序列中的元素的 PooledListBase&lt;T&gt;</returns>
        /// <exception cref="ArgumentNullException">source 为 null</exception>
        public static PooledListBase<TSource> ToPooledListBase<TSource>(this IEnumerable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);
            // 对于未知大小的集合，使用默认容量
            var list = new PooledListBase<TSource>(source);
            return list;
        }

        /// <summary>
        /// 从 IEnumerable&lt;T&gt; 创建一个具有指定初始容量的 PooledListBase&lt;T&gt;
        /// </summary>
        /// <typeparam name="TSource">源集合元素的类型</typeparam>
        /// <param name="source">要转换的 IEnumerable&lt;T&gt; 集合</param>
        /// <param name="capacity">初始容量</param>
        /// <returns>包含输入序列中的元素的 PooledListBase&lt;T&gt;</returns>
        /// <exception cref="ArgumentNullException">source 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">capacity 小于 0</exception>
        public static PooledListBase<TSource> ToPooledListBase<TSource>(this IEnumerable<TSource> source, int capacity)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            var list = new PooledListBase<TSource>(capacity);
            list.AddRange(source);
            return list;
        }
    }
}
