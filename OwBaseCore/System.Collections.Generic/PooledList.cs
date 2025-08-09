/*
 * 项目：OwBaseCore基础设施组件
 * 模块：System.Collections.Generic - 池化列表
 * 文件说明：
 * - 功能1：提供基于ArrayPool<T>的高性能池化列表实现
 * - 功能2：减少频繁分配大数组带来的GC压力和内存碎片
 * - 功能3：与List<T>保持API兼容，便于迁移和替换
 * 
 * PooledList<T> 设计与实现说明：
 * 
 * 1. 设计目标：
 * - 实现一个类似List<T>的泛型集合类PooledList<T>，其内部存储数组全部通过ArrayPool<T>.Shared进行池化分配与回收
 * - 减少频繁分配大数组带来的GC压力和内存碎片，适用于高性能场景下大量临时数据的收集与处理
 * - 应用场景主要针对短生命周期的临时数据集合，避免对长期存在的对象使用该类
 * 
 * 2. 主要功能需求：
 * - 通用泛型集合：支持Add、Insert、Remove、Clear、索引器、Count、Capacity等常用操作
 * - 自动扩容与裁剪：数据超出当前容量时自动扩容，不再使用时可主动裁剪
 * - 池化内存管理：所有内部数组通过ArrayPool<T>.Shared.Rent/Return管理
 * - Dispose支持：实现IDisposable，归还内部数组资源
 * - 数据安全：归还前清理未用槽位，防止数据泄漏
 * - 接口兼容：尽量与List<T> API保持一致，公有成员要一致，方便迁移和替换
 * - 支持枚举：实现IEnumerable<T>，支持foreach
 * - 基类：从Collection<T>派生
 * 
 * 3. 设计要点与实现细节：
 * 
 * 3.1 字段与基本结构：
 * - _items：当前池租用的存储数组
 * - _count：当前元素数量
 * - _disposed：防止重复释放
 * - 默认最小容量设为8或由用户指定
 * 
 * 3.2 构造与初始化：
 * - 支持默认与自定义初始容量
 * - 初始化时通过ArrayPool<T>.Shared.Rent(capacity)获取数组
 * 
 * 3.3 添加/插入/扩容：
 * - 添加元素时，如果容量不足，自动扩容（翻倍增长策略）
 * - 扩容时，租用新数组，将原有数据拷贝到新数组，归还旧数组
 * 
 * 3.4 删除/裁剪/归还：
 * - 支持Remove、RemoveAt、Clear等操作
 * - Clear/Dispose/TrimExcess时将不用的数组归还池
 * - 归还前，将未用元素设为default(T)，防止数据泄漏
 * 
 * 3.5 Dispose模式：
 * - 实现IDisposable，Dispose时归还数组并置空引用，防止再次访问
 * 
 * 3.6 枚举器：
 * - 实现IEnumerable<T>以支持foreach
 * 
 * 4. 典型用法：
 * using var list = new PooledList<byte>(1024);
 * for (int i = 0; i < 10000; i++) list.Add((byte)i);
 * DoSomething(list);
 * // 退出using块或调用Dispose后，底层数组自动归还池
 * 
 * 5. 注意事项与局限：
 * - 归还数组后不得再访问
 * - 与List<T>不同，Dispose后实例不可再用
 * - 不可跨线程访问（如需线程安全请自行加锁）
 * - 适用于短生命周期、大批量数据的场景，不建议长期持有
 * 
 * 6. 性能优势：
 * - 减少GC压力：通过ArrayPool复用数组，减少垃圾回收频率
 * - 避免内存碎片：池化管理避免频繁的大对象堆分配
 * - 提高缓存效率：复用的数组可能仍在CPU缓存中
 * - 降低分配延迟：避免大数组分配时的延迟
 * 
 * 技术要点：
 * - 基于.NET ArrayPool<T>的池化内存管理
 * - 实现完整的List<T>兼容API，包括查找、排序、移除等高级功能
 * - 支持谓词操作，如FindIndex、FindAll、RemoveAll等
 * - 内存安全处理，防止数据泄漏和重复释放
 * 
 * 作者：zc
 * 创建：2024年
 * 修改：2025-01-27 整合PooledList{T}设计需求文档
 */

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
    /// 
    /// 设计特点：
    /// - 池化内存管理：所有内部数组通过ArrayPool&lt;T&gt;管理，自动租用和归还
    /// - API兼容性：与List&lt;T&gt;保持高度兼容，支持所有常用操作和高级功能
    /// - 自动扩容：容量不足时自动扩容，采用翻倍增长策略优化性能
    /// - 内存安全：归还前自动清理未用槽位，防止敏感数据泄漏
    /// - 资源管理：实现IDisposable模式，确保资源及时释放
    /// 
    /// 性能优势：
    /// - 减少GC压力：复用ArrayPool中的数组，降低垃圾回收频率
    /// - 避免LOH分配：大数组复用避免进入大对象堆，减少内存碎片
    /// - 提高缓存命中：复用的数组可能仍在CPU缓存中，提高访问效率
    /// - 降低分配延迟：避免大数组分配时的性能开销
    /// 
    /// 使用场景：
    /// - 高频临时数据收集：如日志处理、数据转换、批量计算等
    /// - 大量数据的临时存储：如文件读取、网络数据接收等
    /// - 性能敏感的算法：需要频繁创建和销毁列表的算法
    /// - 内存受限环境：需要减少内存分配和GC压力的场景
    /// 
    /// 限制和注意事项：
    /// - 短生命周期设计：不适合长期持有，使用完毕必须Dispose
    /// - 非线程安全：需要外部同步机制保证线程安全
    /// - 一次性使用：Dispose后不能再次使用，与普通List&lt;T&gt;不同
    /// - 容量管理：建议根据预期大小设置合适的初始容量
    /// </summary>
    /// <remarks>
    /// 注意：此类设计用于短生命周期场景，不适合长期持有。使用完毕后必须调用 Dispose 方法归还资源。
    /// 典型用法是在 using 语句中使用此类的实例。
    /// 
    /// 最佳实践：
    /// - 使用using语句确保及时释放：using var list = new PooledList&lt;T&gt;();
    /// - 设置合适的初始容量以减少扩容次数
    /// - 避免在长生命周期对象中保持引用
    /// - 在高并发场景下注意线程安全
    /// </remarks>
    /// <typeparam name="T">集合中元素的类型</typeparam>
    public sealed class PooledList<T> : PooledListBase<T>, IDisposable
    {
        /// <summary>
        /// 默认的初始容量
        /// 
        /// 容量选择说明：
        /// - 设为8是基于List&lt;T&gt;的默认容量设计
        /// - 平衡内存使用和扩容频率
        /// - 适合大多数临时数据收集场景
        /// </summary>
        private const int DefaultCapacity = 8;

        #region 构造函数
        /// <summary>
        /// 初始化 PooledList 类的新实例，具有指定的初始容量
        /// </summary>
        /// <param name="capacity">初始容量，默认为 8</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity 小于 0</exception>
        public PooledList(int capacity = DefaultCapacity) : base(Math.Max(capacity, DefaultCapacity))
        {
        }

        /// <summary>
        /// 初始化 PooledList 类的新实例，包含从指定集合复制的元素
        /// </summary>
        /// <param name="collection">一个集合，其元素被复制到新列表中</param>
        /// <exception cref="ArgumentNullException">collection 为 null</exception>
        public PooledList(IEnumerable<T> collection) : base(collection is ICollection<T> c ? c.Count : DefaultCapacity)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            AddRange(collection);
        }
        #endregion

        #region 增强的List<T>兼容方法
        /// <summary>
        /// 搜索与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中第一个匹配元素的从零开始的索引。
        /// 查找功能说明：
        /// - 提供基于谓词的灵活查找机制
        /// - 支持复杂的查找条件，不仅限于相等比较
        /// - 性能优化：从头开始线性搜索，适合小到中等规模数据
        /// </summary>
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

        /// <summary>
        /// 从指定的索引开始搜索指定数量的元素中与指定谓词匹配的元素，并返回整个 PooledList&lt;T&gt; 中第一个匹配元素的从零开始的索引
        /// </summary>
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

        /// <summary>
        /// 检索与指定谓词定义的条件相匹配的所有元素
        /// 
        /// 批量查找说明：
        /// - 返回新的PooledList&lt;T&gt;实例，调用者负责释放
        /// - 如果没有匹配元素，返回空的PooledList&lt;T&gt;
        /// - 适合需要对匹配元素进行进一步处理的场景
        /// </summary>
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

        /// <summary>
        /// 从 PooledList&lt;T&gt; 中移除与指定谓词定义的条件相匹配的所有元素
        /// 
        /// 批量移除说明：
        /// - 使用高效的就地算法，避免多次数组拷贝
        /// - 保持剩余元素的相对顺序不变
        /// - 返回实际移除的元素数量
        /// </summary>
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
