using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;

namespace System.Collections.Generic
{

    /// <summary>
    /// 线程安全的哈希集合，实现了 ISet&lt;T&gt; 接口的全部功能
    /// </summary>
    /// <typeparam name="T">集合中元素的类型</typeparam>
    public class ConcurrentHashSet<T> : ISet<T>, IReadOnlyCollection<T>
    {
        private readonly ConcurrentDictionary<T, byte> _internalDictionary = new ConcurrentDictionary<T, byte>();
        private static readonly byte DummyValue = 0; // 用作字典值的占位符

        /// <summary>
        /// 初始化 ConcurrentHashSet&lt;T&gt; 类的新实例
        /// </summary>
        public ConcurrentHashSet() { }

        /// <summary>
        /// 初始化 ConcurrentHashSet&lt;T&gt; 类的新实例，该实例包含从指定集合复制的元素
        /// </summary>
        /// <param name="collection">从中复制元素的集合</param>
        public ConcurrentHashSet(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
            {
                _internalDictionary.TryAdd(item, DummyValue);
            }
        }

        /// <summary>
        /// 初始化 ConcurrentHashSet&lt;T&gt; 类的新实例，该实例使用指定的相等比较器
        /// </summary>
        /// <param name="comparer">比较元素时要使用的 IEqualityComparer&lt;T&gt; 实现</param>
        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            _internalDictionary = new ConcurrentDictionary<T, byte>(comparer);
        }

        /// <summary>
        /// 初始化 ConcurrentHashSet&lt;T&gt; 类的新实例，该实例包含从指定集合复制的元素并使用指定的相等比较器
        /// </summary>
        /// <param name="collection">从中复制元素的集合</param>
        /// <param name="comparer">比较元素时要使用的 IEqualityComparer&lt;T&gt; 实现</param>
        public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _internalDictionary = new ConcurrentDictionary<T, byte>(comparer);
            foreach (var item in collection)
            {
                _internalDictionary.TryAdd(item, DummyValue);
            }
        }

        /// <summary>
        /// 将元素添加到当前集中
        /// </summary>
        /// <param name="item">要添加到集中的元素</param>
        /// <returns>如果成功添加元素则为 true；如果元素已存在则为 false</returns>
        public bool Add(T item)
        {
            return _internalDictionary.TryAdd(item, DummyValue);
        }

        /// <summary>
        /// 从当前集中移除指定集合中的所有元素
        /// </summary>
        /// <param name="other">要从集中移除的元素的集合</param>
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            foreach (var item in other)
            {
                _internalDictionary.TryRemove(item, out _);
            }
        }

        /// <summary>
        /// 修改当前集，使当前集仅包含指定集合中也存在的元素
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // 先创建一个临时集合，避免在迭代过程中修改集合
            var otherSet = new HashSet<T>(other);

            // 移除当前集中不在 other 中的所有元素
            foreach (var item in _internalDictionary.Keys.ToArray())
            {
                if (!otherSet.Contains(item))
                {
                    _internalDictionary.TryRemove(item, out _);
                }
            }
        }

        /// <summary>
        /// 确定当前集是否为指定集合的真子集
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        /// <returns>如果当前集是 other 的真子集，则为 true；否则为 false</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var otherSet = new HashSet<T>(other);
            return Count < otherSet.Count && IsSubsetOf(otherSet);
        }

        /// <summary>
        /// 确定当前集是否为指定集合的真超集
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        /// <returns>如果当前集是 other 的真超集，则为 true；否则为 false</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // 计算其他集合中的元素数
            int otherCount = 0;
            bool allContained = true;
            foreach (var item in other)
            {
                otherCount++;
                if (!Contains(item))
                {
                    allContained = false;
                }
            }

            return Count > otherCount && allContained;
        }

        /// <summary>
        /// 确定当前集是否为指定集合的子集
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        /// <returns>如果当前集是 other 的子集，则为 true；否则为 false</returns>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var otherSet = new HashSet<T>(other);
            // 当前集中的每一项都必须在另一个集合中
            return _internalDictionary.Keys.All(otherSet.Contains);
        }

        /// <summary>
        /// 确定当前集是否为指定集合的超集
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        /// <returns>如果当前集是 other 的超集，则为 true；否则为 false</returns>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // 其他集合中的每一项都必须在当前集中
            return other.All(Contains);
        }

        /// <summary>
        /// 确定当前集是否与指定集合重叠
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        /// <returns>如果当前集与 other 至少共享一个元素，则为 true；否则为 false</returns>
        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return other.Any(Contains);
        }

        /// <summary>
        /// 确定当前集是否包含与指定集合相同的元素
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        /// <returns>如果当前集等于 other，则为 true；否则为 false</returns>
        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var otherSet = new HashSet<T>(other);
            if (Count != otherSet.Count)
                return false;

            return _internalDictionary.Keys.All(otherSet.Contains);
        }

        /// <summary>
        /// 修改当前集，使当前集仅包含当前集或指定集合中存在的元素，但不同时存在于两者中
        /// </summary>
        /// <param name="other">要与当前集进行比较的集合</param>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // 创建一个包含当前所有元素的临时集合
            var elementsToRemove = new HashSet<T>(_internalDictionary.Keys);

            foreach (var item in other)
            {
                // 如果元素已在集合中，标记为删除
                // 如果元素不在集合中，添加它
                if (!_internalDictionary.TryAdd(item, DummyValue))
                {
                    elementsToRemove.Add(item);
                }
                else
                {
                    elementsToRemove.Remove(item);
                }
            }

            // 实际删除需要删除的元素
            foreach (var item in elementsToRemove)
            {
                _internalDictionary.TryRemove(item, out _);
            }
        }

        /// <summary>
        /// 修改当前集，使其包含当前集和指定集合中的所有元素
        /// </summary>
        /// <param name="other">要与当前集合并的集合</param>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            foreach (var item in other)
            {
                _internalDictionary.TryAdd(item, DummyValue);
            }
        }

        /// <summary>
        /// 将元素添加到集合中
        /// </summary>
        /// <param name="item">要添加到集合中的元素</param>
        void ICollection<T>.Add(T item)
        {
            _internalDictionary.TryAdd(item, DummyValue);
        }

        /// <summary>
        /// 从集合中移除所有元素
        /// </summary>
        public void Clear()
        {
            _internalDictionary.Clear();
        }

        /// <summary>
        /// 确定集合是否包含特定元素
        /// </summary>
        /// <param name="item">要在集合中定位的元素</param>
        /// <returns>如果在集合中找到 item，则为 true；否则为 false</returns>
        public bool Contains(T item)
        {
            return _internalDictionary.ContainsKey(item);
        }

        /// <summary>
        /// 将集合元素复制到数组中，从指定索引开始
        /// </summary>
        /// <param name="array">作为从集合复制的元素的目标的一维数组</param>
        /// <param name="arrayIndex">array 中开始复制的从零开始的索引</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("目标数组不够大");

            _internalDictionary.Keys.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 获取集合中包含的元素数
        /// </summary>
        public int Count => _internalDictionary.Count;

        /// <summary>
        /// 获取一个值，该值指示集合是否为只读
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 从集合中移除特定元素
        /// </summary>
        /// <param name="item">要从集合中删除的元素</param>
        /// <returns>如果成功删除 item，则为 true；否则为 false</returns>
        public bool Remove(T item)
        {
            return _internalDictionary.TryRemove(item, out _);
        }

        /// <summary>
        /// 返回循环访问集合的枚举数
        /// </summary>
        /// <returns>用于循环访问集合的枚举数</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _internalDictionary.Keys.GetEnumerator();
        }

        /// <summary>
        /// 返回循环访问集合的枚举数
        /// </summary>
        /// <returns>用于循环访问集合的枚举数</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}