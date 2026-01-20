using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Generic
{
    /// <summary>
    /// 基于 ArrayPool 池化数组的列表基类。
    /// 提供高性能的列表实现，通过 ArrayPool&lt;T&gt; 复用数组减少 GC 压力。
    /// </summary>
    /// <typeparam name="T">列表元素的类型</typeparam>
    /// <remarks>
    /// 核心优化特性：
    /// <list type="bullet">
    /// <item><description>结构体枚举器 - 避免枚举时的装箱开销</description></item>
    /// <item><description>SIMD 批量拷贝 - 大数据集使用硬件加速</description></item>
    /// <item><description>内联热路径 - Add/索引访问等高频操作内联优化</description></item>
    /// <item><description>ArrayPool 预测 - 智能预测池大小，减少重新分配</description></item>
    /// <item><description>值类型优化 - 归还数组时跳过不必要的清理</description></item>
    /// <item><description>并发修改检测 - 枚举期间检测集合修改，抛出异常</description></item>
    /// </list>
    /// 
    /// 性能优势:
    /// <list type="number">
    /// <item><description>减少 GC 压力 - 复用 ArrayPool 中的数组</description></item>
    /// <item><description>避免 LOH 分配 - 大数组复用避免进入大对象堆</description></item>
    /// <item><description>提高缓存命中 - 复用的数组可能仍在 CPU 缓存中</description></item>
    /// </list>
    /// 
    /// 线程安全：
    /// <para>
    /// 此类型<b>不是线程安全的</b>。如果需要从多个线程访问，必须提供外部同步。
    /// </para>
    /// <para>
    /// <b>Fail-Fast 机制</b>：版本号使用 <c>volatile</c> 修饰，确保并发修改能被枚举线程<b>尽快检测到</b>。
    /// 当检测到集合在枚举期间被修改时，会立即抛出 <see cref="InvalidOperationException"/>。
    /// 注意：这不能防止数据竞争或保证线程安全，仅用于快速暴露并发使用问题。
    /// </para>
    /// <para>
    /// 多线程使用时，必须使用 <c>lock</c> 或其他同步机制保护所有操作，
    /// 或改用线程安全集合如 <see cref="System.Collections.Concurrent.ConcurrentBag{T}"/>。
    /// </para>
    /// <para>
    /// <b>资源管理</b>：本类实现了 <see cref="IDisposable"/> 接口和终结器模式。
    /// 建议使用 <c>using</c> 语句或手动调用 <see cref="Dispose"/> 方法及时释放资源。
    /// 如果忘记调用 Dispose，终结器会在 GC 回收时自动归还数组，但这会增加 GC 压力和延迟。
    /// 最佳实践：始终使用 <c>using var list = new PooledList&lt;T&gt;();</c>
    /// </para>
    /// </remarks>
    public class PooledListBase<T> : IList<T>, IDisposable
    {
        private T[] _Buffer;
        private int _Count;
        private bool _isDisposed;
        private volatile int _version;
        private const int DefaultCapacity = 8;

        #region 属性

        /// <summary>
        /// 获取内部缓冲区的引用。
        /// </summary>
        protected T[] Buffer => _Buffer;

        /// <summary>
        /// 获取或设置列表中实际包含的元素数。
        /// </summary>
        public int Count => _Count;

        /// <summary>
        /// 获取一个值，该值指示列表是否为只读。
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 获取列表的容量。
        /// </summary>
        public int Capacity => _Buffer.Length;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化列表的新实例，使用默认容量。
        /// </summary>
        public PooledListBase()
        {
            _Buffer = ArrayPool<T>.Shared.Rent(DefaultCapacity);
            _Count = 0;
        }

        /// <summary>
        /// 初始化列表的新实例，具有指定的初始容量。
        /// </summary>
        /// <param name="capacity">新列表最初可以存储的元素数，小于8则视同为8.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于 0</exception>
        public PooledListBase(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "容量不能为负数");
            _Buffer = capacity < 8 ? ArrayPool<T>.Shared.Rent(8) : ArrayPool<T>.Shared.Rent(capacity);
            _Count = 0;
        }

        /// <summary>
        /// 初始化列表的新实例，包含从指定集合复制的元素。
        /// </summary>
        /// <param name="collection">一个集合，其元素被复制到新列表中</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 null</exception>
        public PooledListBase(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            int capacity = collection is ICollection<T> c ? c.Count : DefaultCapacity;
            _Buffer = capacity < 8 ? ArrayPool<T>.Shared.Rent(8) : ArrayPool<T>.Shared.Rent(capacity);
            _Count = 0;
            AddRange(collection);
        }

        /// <summary>
        /// 终结器：在 GC 回收时归还数组到 ArrayPool（备用机制）
        /// </summary>
        /// <remarks>
        /// <para><b>重要提示</b>：终结器仅在 Dispose 未被调用时作为安全网运行。</para>
        /// <para>
        /// <b>性能影响</b>：
        /// <list type="bullet">
        /// <item><description>对象会被提升到 GC 的终结队列，延迟回收</description></item>
        /// <item><description>增加 GC 压力，可能触发额外的 Gen2 回收</description></item>
        /// <item><description>终结器在单独的线程执行，可能造成延迟</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>最佳实践</b>：始终使用 <c>using</c> 语句或手动调用 <see cref="Dispose"/>，
        /// 避免依赖终结器。终结器仅作为最后的安全机制。
        /// </para>
        /// <para>
        /// <b>应用程序域卸载</b>：当应用程序域卸载时，CLR 不保证终结器会执行。
        /// 对于长期运行的应用程序，这通常不是问题，但在 AppDomain 卸载场景下，
        /// 数组可能不会被归还。这是可接受的，因为 ArrayPool 本身也会被回收。
        /// ArrayPool.Return 即使在 AppDomain 卸载过程中调用也是安全的，
        /// 因为其内部有异常保护，最坏情况是数组无法归还而被 GC 回收。
        /// </para>
        /// </remarks>
        ~PooledListBase()
        {
            // ✅ 即使在 AppDomain 卸载时，Dispose(false) 也是安全的
            Dispose(false);
        }

        #endregion

        #region IList<T> 接口实现

        /// <summary>
        /// 获取或设置指定索引处的元素。
        /// </summary>
        /// <param name="index">要获取或设置的元素的从零开始的索引</param>
        /// <returns>指定索引处的元素</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于 0 或大于等于 <see cref="Count"/></exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _Buffer[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _Buffer[index] = value;
            }
        }

        /// <summary>
        /// 确定列表中特定项的索引。
        /// </summary>
        /// <param name="item">要在列表中查找的对象</param>
        /// <returns>如果在列表中找到 <paramref name="item"/>，则为该项的从零开始的索引；否则为 -1</returns>
        public int IndexOf(T item) => Array.IndexOf(_Buffer, item, 0, _Count);

        /// <summary>
        /// 确定列表是否包含特定值。
        /// </summary>
        /// <param name="item">要在列表中查找的对象</param>
        /// <returns>如果在列表中找到 <paramref name="item"/>，则为 true；否则为 false</returns>
        public bool Contains(T item) => IndexOf(item) >= 0;

        /// <summary>
        /// 将对象添加到列表的结尾处。
        /// </summary>
        /// <param name="item">要添加到列表末尾的对象</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            int count = _Count;
            T[] buffer = _Buffer;
            if ((uint)count < (uint)buffer.Length)
            {
                buffer[count] = item;
                _Count = count + 1;
                _version++;
            }
            else
            {
                AddWithResize(item);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item)
        {
            EnsureCapacity(_Count + 1);
            _Buffer[_Count++] = item;
            _version++;
        }

        /// <summary>
        /// 将元素插入列表的指定索引处。
        /// </summary>
        /// <param name="index">应插入 <paramref name="item"/> 的从零开始的索引</param>
        /// <param name="item">要插入的对象</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于 0 或大于 <see cref="Count"/></exception>
        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            EnsureCapacity(_Count + 1);
            if (index < _Count)
                _Buffer.AsSpan(index, _Count - index).CopyTo(_Buffer.AsSpan(index + 1));
            _Buffer[index] = item;
            _Count++;
            _version++;
        }

        /// <summary>
        /// 从列表中移除特定对象的第一个匹配项。
        /// </summary>
        /// <param name="item">要从列表中移除的对象</param>
        /// <returns>如果成功移除 <paramref name="item"/>，则为 true；否则为 false。如果在列表中没有找到 <paramref name="item"/>，此方法也会返回 false</returns>
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
        /// 移除列表的指定索引处的元素。
        /// </summary>
        /// <param name="index">要移除的元素的从零开始的索引</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于 0 或大于等于 <see cref="Count"/></exception>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _Count--;
            if (index < _Count)
                _Buffer.AsSpan(index + 1, _Count - index).CopyTo(_Buffer.AsSpan(index));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _Buffer[_Count] = default;
            _version++;
        }

        /// <summary>
        /// 从列表中移除所有元素。
        /// </summary>
        /// <remarks>
        /// <para><see cref="Count"/> 被设置为 0，并且释放对其他对象的引用。</para>
        /// <para>对于引用类型或包含引用的值类型，会自动清理内部数组以避免内存泄漏。</para>
        /// </remarks>
        public void Clear()
        {
            int count = _Count;
            _Count = 0;
            _version++;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && count > 0)
                Array.Clear(_Buffer, 0, count);
        }

        /// <summary>
        /// 从特定的索引开始，将列表的元素复制到数组中。
        /// </summary>
        /// <param name="array">作为从列表复制的元素的目标位置的一维数组</param>
        /// <param name="arrayIndex"><paramref name="array"/> 中从零开始的索引，从此处开始复制</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> 小于 0 或大于 <paramref name="array"/> 的长度减去 <see cref="Count"/></exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            if (arrayIndex < 0 || arrayIndex > array.Length - _Count)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            _Buffer.AsSpan(0, _Count).CopyTo(array.AsSpan(arrayIndex));
        }

        /// <summary>
        /// 返回循环访问列表的枚举器（结构体版本）。
        /// </summary>
        /// <returns>用于循环访问列表的 <see cref="Enumerator"/></returns>
        /// <remarks>
        /// 此方法返回结构体枚举器，避免了堆分配，提供更好的性能。
        /// </remarks>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// 返回循环访问集合的枚举器。
        /// </summary>
        /// <returns>可用于循环访问集合的 <see cref="IEnumerator{T}"/></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 返回循环访问集合的枚举器。
        /// </summary>
        /// <returns>可用于循环访问集合的 <see cref="IEnumerator"/></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region 性能优化方法

        /// <summary>
        /// 将指定集合的元素添加到列表的末尾。
        /// </summary>
        /// <param name="collection">要添加其元素的集合</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 null</exception>
        /// <remarks>
        /// 性能优化：
        /// <list type="bullet">
        /// <item><description>优先使用 FastCopyTo 批量复制（已针对 ICollection 优化）</description></item>
        /// <item><description>对无法获取数量的枚举器，逐个添加并利用 Add 的内联优化</description></item>
        /// <item><description>空集合提前返回，避免修改版本号和不必要的操作</description></item>
        /// </list>
        /// </remarks>
        public void AddRange(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            if (collection.TryGetNonEnumeratedCount(out int count))
            {
                if (count > 0)  // ✅ 仅当有元素时才执行
                {
                    EnsureCapacity(_Count + count);
                    collection.FastCopyTo(_Buffer, _Count);
                    _Count += count;
                    _version++;
                }
            }
            else
            {
                foreach (var item in collection)
                    Add(item);
            }
        }

        /// <summary>
        /// 从列表中移除指定范围的元素。
        /// </summary>
        /// <param name="index">要移除的元素范围的起始索引</param>
        /// <param name="count">要移除的元素数</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> 小于 0 或大于等于 <see cref="Count"/>
        /// 或 <paramref name="count"/> 小于 0
        /// 或 <paramref name="index"/> + <paramref name="count"/> 大于 <see cref="Count"/>
        /// </exception>
        public void RemoveRange(int index, int count)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count > 0)  // ✅ 仅当有元素需要移除时才执行
            {
                int remainingCount = Count - (index + count);
                if (remainingCount > 0)
                {
                    _Buffer.AsSpan(index + count, remainingCount).CopyTo(_Buffer.AsSpan(index));
                }
                _Count -= count;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(_Buffer, _Count, count);
                }
                _version++;
            }
        }

        /// <summary>
        /// 确保容量至少为指定的最小值。
        /// </summary>
        /// <param name="min">所需的最小容量</param>
        /// <remarks>
        /// 仅当当前容量不足时才会触发扩容操作。
        /// 扩容策略由 <see cref="GrowCapacity"/> 方法实现。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EnsureCapacity(int min)
        {
            if (_Buffer.Length < min)
                GrowCapacity(min);
        }

        /// <summary>
        /// 扩展数组容量。采用 ArrayPool 的智能分配策略。
        /// ArrayPool 会自动将容量向上调整到最近的2的幂（如16, 32, 64, 128...），
        /// 从而实现高效的倍增策略和内存池复用。
        /// </summary>
        /// <param name="min">所需的最小容量</param>
        /// <remarks>
        /// ArrayPool 内部优化：
        /// <list type="bullet">
        /// <item><description>自动向上取到2的幂（如101→128, 600→1024）</description></item>
        /// <item><description>维护多个大小桶，提高复用率</description></item>
        /// <item><description>避免频繁分配，减少内存碎片</description></item>
        /// <item><description>大数组自动进入合适的池桶，避免LOH碎片</description></item>
        /// </list>
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowCapacity(int min)
        {
            int newCapacity = Math.Max(DefaultCapacity, min);
            var newItems = ArrayPool<T>.Shared.Rent(newCapacity);
            if (_Count > 0)
            {
                _Buffer.AsSpan(0, _Count).CopyTo(newItems.AsSpan());
            }
            ReturnPooledArray(ref _Buffer);
            _Buffer = newItems;
        }

        /// <summary>
        /// 归还数组到 ArrayPool。使用 Interlocked 确保线程安全。
        /// </summary>
        /// <typeparam name="TElement">数组元素类型</typeparam>
        /// <param name="array">要归还的数组引用，归还后会被替换为空数组</param>
        /// <remarks>
        /// 安全特性：
        /// <list type="bullet">
        /// <item><description>使用 Interlocked.Exchange 保证原子操作</description></item>
        /// <item><description>引用类型数组会被清空，避免内存泄漏</description></item>
        /// <item><description>捕获异常，防止归还失败影响主逻辑</description></item>
        /// </list>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledArray<TElement>(ref TElement[] array)
        {
            var toReturn = Interlocked.Exchange(ref array, Array.Empty<TElement>());
            if (toReturn == null || toReturn.Length == 0)
                return;
            try
            {
                bool clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<TElement>();
                ArrayPool<TElement>.Shared.Return(toReturn, clearArray);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"归还数组到 ArrayPool 时出错: {ex.Message}");
            }
        }

        #endregion

        #region 容量管理

        /// <summary>
        /// 将容量设置为列表中的实际元素数目（如果该数目小于当前容量）。
        /// </summary>
        /// <remarks>
        /// <para>此方法可用于最小化集合的内存开销。</para>
        /// <para>行为说明：</para>
        /// <list type="bullet">
        /// <item><description>空列表：归还当前缓冲区，恢复到默认容量</description></item>
        /// <item><description>如果 ArrayPool 返回的数组不比当前小，则不进行收缩</description></item>
        /// <item><description>成功收缩后，归还旧缓冲区到 ArrayPool</description></item>
        /// </list>
        /// <para>适用场景：批量操作后释放多余内存。</para>
        /// <para>注意：此操作会使当前正在进行的枚举失效。</para>
        /// </remarks>
        public void TrimExcess()
        {
            // ✅ 使用模式匹配简化逻辑
            switch ((_Count, _Buffer.Length))
            {
                case (0, > DefaultCapacity):
                    // 空列表且缓冲区大于默认容量：收缩到默认容量
                    ReturnPooledArray(ref _Buffer);
                    _Buffer = ArrayPool<T>.Shared.Rent(DefaultCapacity);
                    _version++;
                    break;

                case ( > 0, var currentCapacity):
                    // 非空列表：尝试收缩到实际元素数量
                    var newBuffer = ArrayPool<T>.Shared.Rent(_Count);
                    if (newBuffer.Length < currentCapacity)
                    {
                        _Buffer.AsSpan(0, _Count).CopyTo(newBuffer);
                        ReturnPooledArray(ref _Buffer);
                        _Buffer = newBuffer;
                        _version++;
                    }
                    else
                    {
                        ArrayPool<T>.Shared.Return(newBuffer, false);
                    }
                    break;
            }
        }
        #endregion

        #region 结构体枚举器

        /// <summary>
        /// 枚举列表的元素（结构体实现，避免装箱开销）。
        /// </summary>
        /// <remarks>
        /// 性能优化：
        /// <list type="bullet">
        /// <item><description>结构体实现，foreach 时不会装箱分配</description></item>
        /// <item><description>直接访问内部缓冲区，无额外开销</description></item>
        /// <item><description>版本检测，枚举期间集合修改会抛出异常</description></item>
        /// </list>
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly PooledListBase<T> _list;
            private readonly int _version;
            private int _index;
            private T _current;

            /// <summary>
            /// 初始化枚举器实例。
            /// </summary>
            /// <param name="list">要枚举的列表</param>
            internal Enumerator(PooledListBase<T> list)
            {
                _list = list;
                _version = list._version;
                _index = 0;
                _current = default;
            }

            /// <summary>
            /// 获取枚举器当前位置的元素。
            /// </summary>
            public T Current => _current;

            /// <summary>
            /// 获取枚举器当前位置的元素（非泛型版本）。
            /// </summary>
            object IEnumerator.Current => Current;

            /// <summary>
            /// 将枚举器推进到集合的下一个元素。
            /// </summary>
            /// <returns>如果枚举器成功推进到下一个元素，则为 true；如果枚举器传递到集合的末尾，则为 false</returns>
            /// <exception cref="InvalidOperationException">集合在枚举期间被修改</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("集合在枚举期间被修改");
                if ((uint)_index < (uint)_list._Count)
                {
                    _current = _list._Buffer[_index];
                    _index++;
                    return true;
                }
                _index = _list._Count + 1;
                _current = default;
                return false;
            }

            /// <summary>
            /// 将枚举器设置为其初始位置，该位置位于集合中第一个元素之前。
            /// </summary>
            /// <exception cref="InvalidOperationException">集合在枚举期间被修改</exception>
            public void Reset()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException("集合在枚举期间被修改");
                _index = 0;
                _current = default;
            }

            /// <summary>
            /// 释放枚举器使用的资源（结构体无需释放资源）。
            /// </summary>
            public void Dispose() { }
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放由列表占用的资源。
        /// </summary>
        /// <remarks>
        /// <para>调用此方法后，列表将不再可用。</para>
        /// <para>此方法将内部缓冲区归还到 <see cref="ArrayPool{T}"/>，并重置计数为 0。</para>
        /// <para>建议在不再需要列表时调用此方法，以便及时释放内存资源。</para>
        /// <para>可以安全地多次调用此方法（幂等性）。</para>
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  // ✅ 阻止终结器运行，避免双重清理
        }

        /// <summary>
        /// 释放资源的核心实现（支持 Dispose 模式）
        /// </summary>
        /// <param name="disposing">
        /// true 表示从 <see cref="Dispose()"/> 方法调用（用户显式释放）；
        /// false 表示从终结器调用（GC 自动回收）
        /// </param>
        /// <remarks>
        /// <para><b>Dispose 模式最佳实践</b>：</para>
        /// <list type="bullet">
        /// <item><description>disposing = true：可以安全访问托管对象（如 _Buffer）</description></item>
        /// <item><description>disposing = false：不应访问其他托管对象（可能已被 GC 回收）</description></item>
        /// <item><description>无论哪种情况，都应释放非托管资源（ArrayPool 的数组可安全归还）</description></item>
        /// </list>
        /// <para>
        /// 在本实现中，<c>_Buffer</c> 是托管数组引用，但 ArrayPool 的归还操作是线程安全的，
        /// 即使在终结器线程中调用也是安全的。
        /// </para>
        /// <para>
        /// <b>AppDomain 卸载安全性</b>：ArrayPool.Shared.Return() 内部有异常保护，
        /// 即使在 AppDomain 卸载过程中被调用，也不会抛出异常或导致崩溃。
        /// 最坏情况是数组无法归还到池中，而是被 GC 正常回收。
        /// </para>
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            try
            {
                // ✅ 安全检查：确保 _Buffer 不为空且未被卸载
                // 在 AppDomain 卸载时，这些检查可能避免访问已释放的内存
                var buffer = _Buffer;
                if (buffer != null && buffer.Length > 0)
                {
                    // ✅ ArrayPool.Return 是异常安全的：
                    // - 内部有 try-catch 保护
                    // - 即使在 AppDomain 卸载时调用也安全
                    // - 最坏情况：数组无法归还，被 GC 回收
                    ReturnPooledArray(ref _Buffer);
                }
                _Count = 0;
            }
            catch (Exception ex)
            {
                // ✅ 双重保护：即使 ReturnPooledArray 抛出异常（极端情况）
                // 也不会导致终结器线程崩溃
                // 注意：终结器中抛出未处理异常会导致进程终止（.NET Framework）
                // 或仅终止终结器线程（.NET Core/5+），但仍应避免
                Debug.WriteLine($"PooledListBase.Dispose 时出错 (disposing={disposing}): {ex.Message}");
            }
            finally
            {
                _isDisposed = true;
            }
        }
        #endregion
    }
}
