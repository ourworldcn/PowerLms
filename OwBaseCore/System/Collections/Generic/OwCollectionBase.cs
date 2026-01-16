/*
 * 项目：OwBaseCore | 模块：System.Collections.Generic
 * 功能：基于内存池的集合基类，提供内存管理和高性能核心操作
 * 技术要点：
 *   - 内存池管理（MemoryPool<T>）
 *   - 高性能 Span 访问
 *   - 引用传递避免复制
 *   - 资源释放（IDisposable）
 * 作者：zc | 创建：2025-01 | 修改：2025-01-21 从 OwListBase 重构
 */
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// 基于内存池的集合基类，提供内存管理和高性能核心操作。
    /// 默认情况下使用 ArrayMemoryPool&lt;T&gt; 包装 ArrayPool&lt;T&gt;.Shared，提供最佳性能。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    public abstract class OwCollectionBase<T> : IDisposable
    {
        private readonly MemoryPool<T> _pool;
        private readonly bool _disposePool;
        private IMemoryOwner<T> _memoryOwner;
        protected Memory<T> _memory;
        protected int _count;

        #region [构造函数]

        public OwCollectionBase() : this(0, null)
        {

        }

        /// <summary>
        /// 初始化 OwCollectionBase 类的新实例，使用指定的 MemoryPool。
        /// 如果 pool 为 null或省略，则创建新的 ArrayMemoryPool&lt;T&gt; 实例并在 Dispose 时释放。
        /// </summary>
        /// <param name="capacity">集合初始可以存储的元素数</param>
        /// <param name="pool">用于内存管理的 MemoryPool，如果为 null 则创建默认的 ArrayMemoryPool</param>
        protected OwCollectionBase(int capacity, MemoryPool<T> pool = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (pool == null)
            {
                _pool = new ArrayMemoryPool<T>();
                _disposePool = true;
            }
            else
            {
                _pool = pool;
                _disposePool = false;
            }
            if (capacity > 0)
            {
                _memoryOwner = _pool.Rent(capacity);
                _memory = _memoryOwner.Memory;
            }
            else
            {
                _memory = Memory<T>.Empty;
            }
            _count = 0;
        }
        #endregion [构造函数]

        #region [属性]
        /// <summary>
        /// 获取集合当前的容量
        /// </summary>
        public int Capacity => _memory.Length;

        /// <summary>
        /// 获取集合中包含的元素数
        /// </summary>
        public int Count => _count;
        #endregion [属性]

        #region [高性能核心方法]
        /// <summary>
        /// 返回包含集合中所有元素的 Span，提供高性能的直接内存访问
        /// </summary>
        /// <returns>包含集合元素的 Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return _memory.Span.Slice(0, _count);
        }

        /// <summary>
        /// 返回包含集合中指定范围元素的 Span
        /// </summary>
        /// <param name="start">范围的起始索引</param>
        /// <param name="length">范围中的元素数</param>
        /// <returns>包含指定范围元素的 Span</returns>
        /// <exception cref="ArgumentOutOfRangeException">start 或 length 超出有效范围</exception>
        public Span<T> AsSpan(int start, int length)
        {
            if (start < 0 || start > _count)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (length < 0 || start + length > _count)
                throw new ArgumentOutOfRangeException(nameof(length));
            return _memory.Span.Slice(start, length);
        }

        /// <summary>
        /// 获取指定索引处元素的引用，实现零拷贝访问
        /// </summary>
        /// <param name="index">要获取引用的元素的从零开始的索引</param>
        /// <returns>指定索引处元素的引用</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于等于 Count</exception>
        public ref T GetByRef(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref _memory.Span[index];
        }
        #endregion [高性能核心方法]

        #region [容量管理 - 供派生类使用]
        /// <summary>
        /// 确保集合容量至少为指定的最小值。如果当前容量不足，将自动扩容。
        /// </summary>
        /// <param name="min">所需的最小容量（必须为非负数）</param>
        /// <remarks>
        /// <para><b>扩容策略：</b>容量为 0 时设为 4，否则翻倍；不足 min 时使用 min</para>
        /// <para><b>性能：</b>容量足够时 O(1)，扩容时 O(n)</para>
        /// <para><b>线程安全性：</b>非线程安全</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">min 为负数时抛出</exception>
        /// <exception cref="OutOfMemoryException">系统内存不足时抛出</exception>
        protected void EnsureCapacity(int min)
        {
            if (Capacity < min)
            {
                int newCapacity = Capacity == 0 ? 4 : Capacity * 2;
                if (newCapacity < min)
                    newCapacity = min;
                if (newCapacity < 1)
                    newCapacity = 1;
                SetCapacity(newCapacity);
            }
        }

        /// <summary>
        /// 将容量设置为集合中的实际元素数（如果该数小于当前容量的 90%）
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(Capacity * 0.9);
            if (_count < threshold)
            {
                SetCapacity(_count);
            }
        }

        /// <summary>
        /// 设置集合的容量。从内存池租借新内存块，复制现有元素，释放旧内存块。
        /// </summary>
        /// <param name="newCapacity">新的容量（必须为非负数）</param>
        /// <remarks>
        /// <para><b>行为：</b>实际容量可能大于 newCapacity（取决于内存池），但保证至少为 newCapacity</para>
        /// <para><b>副作用：</b>之前获取的 Span 引用将失效</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">newCapacity 小于 0 时抛出</exception>
        /// <exception cref="OutOfMemoryException">系统内存不足时抛出</exception>
        protected void SetCapacity(int newCapacity)
        {
            if (newCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), "容量不能为负数");
            var newOwner = _pool.Rent(newCapacity);
            var newMemory = newOwner.Memory;
            if (_count > 0)
            {
                _memory.Span.Slice(0, _count).CopyTo(newMemory.Span);
            }
            _memoryOwner?.Dispose();
            _memoryOwner = newOwner;
            _memory = newMemory;
        }
        #endregion [容量管理 - 供派生类使用]

        #region [IDisposable Members]
        /// <summary>
        /// 释放由集合使用的所有资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放由集合使用的非托管资源，并可根据需要释放托管资源
        /// </summary>
        /// <param name="disposing">如果为 true，则释放托管资源和非托管资源；如果为 false，则仅释放非托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _memoryOwner?.Dispose();
                _memoryOwner = null;
                if (_disposePool)
                {
                    _pool?.Dispose();
                }
                _memory = default;
                _count = 0;
            }
        }
        #endregion [IDisposable Members]

        #region [静态辅助方法]
        /// <summary>
        /// 尝试从集合中获取连续内存的 Span 表示。
        /// </summary>
        /// <param name="collection">要检查的集合</param>
        /// <param name="span">如果集合是连续内存，则为其 Span 表示；否则为 default</param>
        /// <returns>如果集合是连续内存（T[]、List&lt;T&gt;、OwCollectionBase&lt;T&gt;），返回 true；否则为 false</returns>
        public static bool TryGetSpanFromCollection(IEnumerable<T> collection, out ReadOnlySpan<T> span)
        {
            switch (collection)
            {
                case T[] array:
                    span = array.AsSpan();
                    return true;
                case List<T> list:
                    span = CollectionsMarshal.AsSpan(list);
                    return true;
                case OwCollectionBase<T> owCollection:
                    span = owCollection.AsSpan();
                    return true;
                default:
                    span = default;
                    return false;
            }
        }
        #endregion [静态辅助方法]
    }
}
