/*
* 项目：OwBaseCore | 模块：System.Collections.Generic
* 功能：基于内存池的集合基类，提供内存管理和高性能核心操作
* 技术要点：
*   - 内存池管理（MemoryPool<T>）
*   - 高性能 Span 访问
*   - 引用传递避免复制
*   - 资源释放（IDisposable）
* 作者：zc | 创建：2025-01 | 修改：2025-01-22 优化 RemoveRange，使用 FindSurvivorBlock 简化
*/
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OW.Buffers;
namespace OW.Collections.Generic
{
    /// <summary>
    /// 高性能谓词接口。
    /// <para>实现者按以下方式实现可获得最佳性能：</para>
    /// <list type="bullet">
    /// <item><description>如果可能，请用 <c>struct</c>（推荐 <c>readonly struct</c>）实现。</description></item>
    /// <item><description>如果使用类，请尽量将实现类声明为 <c>sealed</c>。</description></item>
    /// <item><description><c>Match</c> 方法尽量保持小且简单。尽量标记内联 <see cref="MethodImplOptions.AggressiveInlining"/> </description></item>
    /// <item><description>不要把结构或类先转换为 <c>IRefPredicate&lt;T&gt;</c> 再传入方法 <see cref="OwCollection{T}.RemoveRange{TPredicate}(int, int, in TPredicate)"/> 。</description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public interface IRefPredicate<T>
    {
        /// <summary>
        /// 判断元素是否“匹配”（返回 <c>true</c> 表示匹配，例如可用于“删除该元素”）。
        /// </summary>
        /// <param name="item">按只读引用传入的元素。</param>
        /// <returns>匹配返回 <c>true</c>，否则 <c>false</c>。</returns>
        bool Match(in T item);
    }
    /// <summary>
    /// 基于内存池的集合，提供内存管理和高性能核心操作。
    /// </summary>
    /// <remarks>使用静态 <see cref="OwBuffer.Resize{T}(MemoryPool{T}, IMemoryOwner{T}, int, int, int, bool)"/> 管理内存重新分配，自身专注于集合语义。</remarks>
    /// <typeparam name="T">集合元素类型</typeparam>
    public class OwCollection<T> : IDisposable
    {
        private readonly MemoryPool<T> _pool;
        private readonly bool _disposePool;
        private IMemoryOwner<T> _owner;
        private int _count;
        #region [构造函数]
        /// <summary>
        /// 初始化 OwCollection 类的新实例，使用默认容量和默认内存池
        /// </summary>
        public OwCollection() : this(0, null)
        {
        }
        /// <summary>
        /// 初始化 OwCollection 类的新实例，使用指定的容量和内存池
        /// </summary>
        /// <param name="capacity">集合初始可以存储的元素数</param>
        /// <param name="pool">用于内存管理的 MemoryPool，如果为 null 则创建默认的 ArrayMemoryPool</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity 小于 0</exception>
        public OwCollection(int capacity, MemoryPool<T> pool = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (pool == null)
            {
                _pool = new ArrayMemoryPool<T>(); // 使用默认内存池
                _disposePool = true; // 标记需要释放池
            }
            else
            {
                _pool = pool; // 使用外部提供的池
                _disposePool = false; // 不释放外部池
            }
            if (capacity > 0)
            {
                _owner = _pool.Rent(capacity); // 租借初始容量
            }
            else
            {
                _owner = null; // 延迟分配
            }
            _count = 0;
        }
        #endregion [构造函数]
        #region [属性]
        /// <summary>
        /// 获取集合当前的容量（可存储的最大元素数）
        /// </summary>
        public int Capacity => _owner?.Memory.Length ?? 0;
        /// <summary>
        /// 获取集合中当前包含的元素数
        /// </summary>
        public int Count => _count;
        #endregion [属性]
        #region [高性能核心方法]
        /// <summary>
        /// 返回包含集合中所有元素的 Span，提供高性能的直接内存访问
        /// </summary>
        /// <returns>包含集合元素的 Span，如果集合为空则返回空 Span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return _owner == null ? Span<T>.Empty : _owner.Memory.Span.Slice(0, _count);
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
            if (length == 0) return Span<T>.Empty;
            return _owner.Memory.Span.Slice(start, length);
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
            return ref _owner.Memory.Span[index];
        }
        /// <summary>
        /// 在指定索引处插入元素（通过引用传递，避免大结构体复制）
        /// </summary>
        /// <param name="index">从零开始的索引，应在该位置插入 item</param>
        /// <param name="item">要插入的元素的引用</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于 0 或大于 Count</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertByRef(int index, in T item)
        {
            var reserved = InsertRange(index, 1, false); // 预留1个位置
            reserved[0] = item; // 直接写入元素
        }
        #endregion [高性能核心方法]
        #region [内存管理]
        /// <summary>
        /// 确保集合容量至少为指定的最小值。如果当前容量不足，将自动扩容。
        /// </summary>
        /// <param name="min">所需的最小容量</param>
        /// <remarks>
        /// 扩容策略：容量为 0 时设为 1，否则翻倍；不足 min 时使用 min
        /// </remarks>
        private void EnsureCapacity(int min)
        {
            if (Capacity >= min) return; // 容量已足够
            int newCapacity = Capacity == 0 ? 1 : Capacity * 2; // 初始1或翻倍
            if (newCapacity < min) newCapacity = min; // 确保满足最小需求
            SetCapacity(newCapacity);
        }
        /// <summary>
        /// 设置集合的容量。使用 OwBuffer.Resize 管理内存重新分配。
        /// </summary>
        /// <param name="newCapacity">新的容量</param>
        /// <remarks>
        /// 副作用：之前获取的 Span 引用将失效
        /// </remarks>
        private void SetCapacity(int newCapacity)
        {
            if (_owner == null)
            {
                _owner = _pool.Rent(newCapacity); // 首次分配
                return;
            }
            var newOwner = OwBuffer.Resize(_pool, _owner, 0, _count, newCapacity, clearValidRegion: true); // 调整容量并复制有效数据
            _owner.Dispose(); // 释放旧内存
            _owner = newOwner;
            if (_count > newOwner.Memory.Length) // 防御性检查
                _count = newOwner.Memory.Length;
        }
        /// <summary>
        /// 将容量设置为集合中的实际元素数（如果该数小于当前容量的 90%）
        /// </summary>
        /// <returns>如果执行了收缩则返回 true，否则返回 false</returns>
        public bool TrimExcess()
        {
            int threshold = (int)(Capacity * 0.9); // 90%阈值
            if (_count < threshold)
            {
                SetCapacity(_count); // 收缩到实际大小
                return true;
            }
            return false;
        }
        /// <summary>
        /// 固定集合的内存，防止 GC 移动，返回可用于非托管代码的句柄
        /// </summary>
        /// <returns>内存句柄，如果集合为空则返回默认句柄</returns>
        public MemoryHandle Pin()
        {
            return _owner == null ? default : _owner.Memory.Slice(0, _count).Pin();
        }
        #endregion [内存管理]
        #region [空间操作]
        /// <summary>
        /// 在指定索引处插入指定数量的空间，并返回预留区域的 Span
        /// </summary>
        /// <param name="index">插入位置的从零开始的索引</param>
        /// <param name="count">要插入的元素数量</param>
        /// <param name="clear">是否清零预留区域</param>
        /// <returns>预留区域的 Span，调用者可直接写入数据</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出有效范围</exception>
        public Span<T> InsertRange(int index, int count, bool clear = false)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return Span<T>.Empty; // 空操作
            EnsureCapacity(_count + count); // 确保容量足够
            var span = _owner.Memory.Span;
            if (index < _count) // 需要移动现有元素
            {
                span.Slice(index, _count - index).CopyTo(span.Slice(index + count)); // 后移元素
            }
            var reserved = span.Slice(index, count); // 获取预留区域
            if (clear)
            {
                reserved.Clear(); // 可选清零
            }
            _count += count; // 更新计数
            return reserved;
        }
        /// <summary>
        /// 从集合中移除指定范围的元素
        /// </summary>
        /// <param name="index">要移除的元素范围的起始索引</param>
        /// <param name="count">要移除的元素数量</param>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出有效范围</exception>
        public void RemoveRange(int index, int count)
        {
            switch ((index, count, _count))
            {
                case var (i, _, _) when i < 0:
                    throw new ArgumentOutOfRangeException(nameof(index));
                case var (_, c, _) when c < 0:
                    throw new ArgumentOutOfRangeException(nameof(count), "移除数量不能为负数");
                case var (i, c, cnt) when i + c > cnt:
                    throw new ArgumentOutOfRangeException(nameof(count), $"索引 ({i}) 和数量 ({c}) 超出集合范围 ({cnt})");
                case (_, 0, _):
                    return; // 空操作
            }
            var span = _owner.Memory.Span;
            if (index + count < _count) // 需要前移后续元素
            {
                span.Slice(index + count, _count - index - count).CopyTo(span.Slice(index));
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) // 引用类型需要清理
            {
                span.Slice(_count - count, count).Clear(); // 清理尾部
            }
            _count -= count; // 更新计数
        }

        /// <summary>
        /// 查找指定区间内第一个连续的"幸存块"（不匹配谓词的元素块），用于简化 RemoveRange
        /// </summary>
        /// <typeparam name="TPredicate">谓词类型</typeparam>
        /// <param name="index">起始查找的索引</param>
        /// <param name="count">要检查的元素数量</param>
        /// <param name="predicate">谓词引用（匹配的元素将被删除）</param>
        /// <returns>幸存块的起始索引和长度 (start, length)，未找到返回 (-1, 0)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (int start, int length) FindSurvivorBlock<TPredicate>(int index, int count, in TPredicate predicate)
            where TPredicate : IRefPredicate<T>
        {
#if DEBUG
            // 1) 谓词仅在类类型时可能为 null
            if (typeof(TPredicate).IsClass && predicate == null)
                throw new ArgumentNullException(nameof(predicate), "谓词引用不能为空");

            // 2) index / count 不能为负
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "index 不能为负数");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "count 不能为负数");

            // 3) index 不得超过已有元素数；index+count 不得越界（避免加法溢出）
            if ((uint)index > (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"index 必须在 [0, {_count}] 之间");
            if (index > _count - count)
                throw new ArgumentOutOfRangeException(nameof(count), count,
                    $"index+count 超出范围（index={index}, count={count}, total={_count}）");
#endif
            var span = _owner.Memory.Span;
            int searchStart = index;
            int searchEnd = index + count;
            // 跳过匹配谓词的元素（要删除的），找到第一个幸存元素
            while (searchStart < searchEnd && predicate.Match(in span[searchStart]))
            {
                searchStart++;
            }
            if (searchStart >= searchEnd) return (-1, 0); // 未找到幸存元素
            // 记录幸存块的起点
            int blockStart = searchStart++; //当前元素已是幸存元素，继续扫描
            // 继续扫描，找到幸存块的终点
            while (searchStart < searchEnd && !predicate.Match(in span[searchStart]))
            {
                searchStart++;
            }
            return (blockStart, searchStart - blockStart); // 返回 (起始索引, 长度)
        }

        /// <summary>
        /// 从指定区间 [index, index+count) 中按谓词删除元素（结构体谓词版本）
        /// 使用幸存块压实算法，通过 FindSurvivorBlock 和 Span.CopyTo 批量复制连续保留块
        /// </summary>
        /// <typeparam name="TPredicate">谓词类型，注意不要用接口，那样会减低性能，应该直接传入类或结构。</typeparam>
        /// <param name="index">起始索引</param>
        /// <param name="count">要检查的元素数量</param>
        /// <param name="predicate">谓词（通过 in 引用传递）</param>
        /// <returns>删除的元素数量</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 超出有效范围</exception>
        public int RemoveRange<TPredicate>(int index, int count, in TPredicate predicate)
            where TPredicate : IRefPredicate<T>
        {
            if (typeof(TPredicate).IsClass) ArgumentNullException.ThrowIfNull(predicate);
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > _count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return 0; // 空操作
            var span = AsSpan();
            int regionEnd = index + count; // 处理区间的结束位置
            int writePos = index; // 写指针：下一个要写入的位置
            int searchPos = index; // 搜索指针：下一个要搜索的位置
            int removedCount = 0;
            // 使用 FindSurvivorBlock 批量查找并复制幸存块
            while (searchPos < regionEnd)
            {
                var (blockStart, blockLength) = FindSurvivorBlock(searchPos, regionEnd - searchPos, in predicate);
                if (blockStart == -1) // 未找到更多幸存块
                {
                    removedCount += regionEnd - searchPos; // 剩余全部是要删除的
                    break;
                }
                // 统计跳过的删除元素
                removedCount += blockStart - searchPos;
                // 复制幸存块（仅当需要移动时）
                if (writePos != blockStart)
                {
                    span.Slice(blockStart, blockLength).CopyTo(span.Slice(writePos));
                }
                writePos += blockLength;
                searchPos = blockStart + blockLength;
            }
            // 前移尾部元素（处理区间之后的部分）
            if (regionEnd < _count)
            {
                span.Slice(regionEnd, _count - regionEnd).CopyTo(span.Slice(writePos));
                writePos += _count - regionEnd;
            }
            // 清理尾部（引用类型需要清零）
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                span.Slice(writePos, _count - writePos).Clear();
            }
            _count = writePos; // 更新计数
            return removedCount;
        }
        #endregion [空间操作]
        #region [IDisposable Members]
        /// <summary>
        /// 释放由集合使用的所有资源
        /// </summary>
        public void Dispose()
        {
            if (_owner != null)
            {
                if (_count > 0 && RuntimeHelpers.IsReferenceOrContainsReferences<T>()) // 清理视野内的引用
                    _owner.Memory.Span.Slice(0, _count).Clear();
                _owner.Dispose(); // 归还内存到池
                _owner = null;
            }
            if (_disposePool) // 如果使用的是默认池
                _pool?.Dispose();
            _count = 0;
        }
        #endregion [IDisposable Members]
        #region [辅助方法]
        /// <summary>
        /// 尝试从集合中获取连续内存的 Span 表示。
        /// </summary>
        /// <param name="collection">要检查的集合</param>
        /// <param name="span">如果集合是连续内存，则为其 Span 表示；否则为 default</param>
        /// <returns>如果集合是连续内存（T[]、List&lt;T&gt;、OwCollection&lt;T&gt;），返回 true；否则为 false</returns>
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
                case OwCollection<T> owCollection:
                    span = owCollection.AsSpan();
                    return true;
                default:
                    span = default;
                    return false;
            }
        }
        #endregion [辅助方法]
    }
}
