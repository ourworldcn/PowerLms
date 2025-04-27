using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private T[] _Items;          // 存储元素的池化数组

        /// <summary>
        /// 内部数据的数组，它从0开始有<see cref="Count"/>个有效数据。数组的长度是容量<see cref="Capacity"/>。
        /// </summary>
        protected T[] Items=>_Items;

        private int _Count;           // 列表中当前元素数量
        private bool _isDisposed;    // 对象是否已被释放
        private const int DefaultCapacity = 8;  // 默认初始容量

        /// <summary>
        /// 使用默认容量初始化 PooledListBase 实例
        /// </summary>
        public PooledListBase()
        {
            _Items = ArrayPool<T>.Shared.Rent(DefaultCapacity);
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

            _Items = capacity == 0 ? Array.Empty<T>() : ArrayPool<T>.Shared.Rent(capacity);
            _Count = 0;
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

                return _Items[index];
            }
            set
            {
                if ((uint)index >= (uint)_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _Items[index] = value;
            }
        }

        /// <summary>
        /// 将元素添加到列表末尾
        /// </summary>
        /// <param name="item">要添加的元素</param>
        public void Add(T item)
        {
            if (_Count == _Items.Length)
            {
                EnsureCapacity(_Count + 1);
            }

            _Items[_Count++] = item;
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

            if (_Count == _Items.Length)
            {
                EnsureCapacity(_Count + 1);
            }

            if (index < _Count)
            {
                Array.Copy(_Items, index, _Items, index + 1, _Count - index);
            }

            _Items[index] = item;
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
                Array.Copy(_Items, index + 1, _Items, index, _Count - index);
            }

            // 防止保留引用
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _Items[_Count] = default;
            }
        }

        /// <summary>
        /// 从列表中移除所有元素
        /// </summary>
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_Items, 0, _Count);
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
            return Array.IndexOf(_Items, item, 0, _Count);
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

            Array.Copy(_Items, 0, array, arrayIndex, _Count);
        }

        /// <summary>
        /// 返回循环访问集合的枚举器
        /// </summary>
        /// <returns>可用于循环访问集合的枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _Count; i++)
            {
                yield return _Items[i];
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
        public int Capacity => _Items.Length;

        /// <summary>
        /// 调整列表的容量大小
        /// </summary>
        /// <param name="newCapacity">新的容量大小</param>
        /// <exception cref="ArgumentOutOfRangeException">newCapacity 小于当前元素数量</exception>
        public void Resize(int newCapacity)
        {
            if (newCapacity < _Count)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), "新容量小于当前元素数量");

            if (newCapacity != _Items.Length)
            {
                if (newCapacity > 0)
                {
                    T[] newItems = ArrayPool<T>.Shared.Rent(newCapacity);
                    if (newItems.Length == _Items.Length) //直接返回原数组，避免不必要的内存分配
                    {
                        ArrayPool<T>.Shared.Return(newItems);
                        return;
                    }
                    if (_Count > 0)
                    {
                        Array.Copy(_Items, newItems, _Count);
                    }

                    ReturnArray();
                    _Items = newItems;
                }
                else
                {
                    ReturnArray();
                    _Items = Array.Empty<T>();
                }
            }
        }

        /// <summary>
        /// 将内部数组归还到数组池
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnArray()
        {
            // 使用 Interlocked.Exchange 原子地替换 _items 并获取旧值
            T[] toReturn = Interlocked.Exchange(ref _Items, Array.Empty<T>());

            // 如果没有数组需要返回，直接退出
            if (toReturn == null || toReturn.Length == 0)
                return;

            try
            {
                // 直接使用 clearArray: true 让 ArrayPool 内部处理清理工作
                ArrayPool<T>.Shared.Return(toReturn, clearArray: true);
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
                    ReturnArray();
                }

                // 释放非托管资源
                _isDisposed = true;
            }
        }

        #endregion

        /// <summary>
        /// 将现有集合中的元素添加到列表的末尾
        /// </summary>
        /// <param name="collection">要添加的集合</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_Count + count);
                    c.CopyTo(_Items, _Count);
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
            if (_Count < _Items.Length * 0.9)
            {
                Resize(_Count);
            }
        }

        /// <summary>
        /// 确保列表具有足够的容量来容纳指定数量的元素
        /// </summary>
        /// <param name="min">所需的最小容量</param>
        public void EnsureCapacity(int min)
        {
            if (_Items.Length < min)
            {
                int newCapacity = _Items.Length == 0 ? DefaultCapacity : _Items.Length * 2;

                if (newCapacity < min)
                    newCapacity = min;

                Resize(newCapacity);
            }
        }

    }
}
