/*
* 项目：OwBaseCore | 模块：集合
* 功能：高性能位数组，使用平台原生整数类型优化存储和访问
* 技术要点：使用 nuint 根据平台自适应宽度（64位/32位/WASM），基于 OwCollection 池化内存管理
* 作者：zc | 创建：2024-01 | 修改：2025-01-22 优化构造函数批量操作
*/
#if !NET5_0_OR_GREATER
#error OwBitArray 需要 .NET 5.0 或更高版本才能使用 nuint 类型。请升级到 .NET 5+ / .NET 6+ / .NET 7+
#endif
using OW.Collections.Generic;
using System;
using System.Buffers;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
namespace OW.Collections
{
    /// <summary>
    /// 高性能位数组，使用平台原生整数类型 nuint 进行位操作。
    /// 自动适配平台：64位平台使用64位宽度，32位平台和WASM使用32位宽度。
    /// 底层使用 OwCollection 进行池化内存管理，减少 GC 压力。
    /// 支持平台：.NET 5+、.NET 6+、MAUI、Blazor WebAssembly
    /// </summary>
    public class OwBitArray : ICollection, IEnumerable, ICloneable, IDisposable
    {
        static readonly int BitsPerElement = IntPtr.Size * 8;
        private OwCollection<nuint> _array;
        private int _length;
        #region [构造函数]
        /// <summary>
        /// 初始化 OwBitArray 类的新实例，所有位初始化为 false
        /// </summary>
        /// <param name="length">位数组中的位数</param>
        public OwBitArray(int length) : this(length, false)
        {
        }
        /// <summary>
        /// 初始化 OwBitArray 类的新实例，所有位初始化为指定值
        /// </summary>
        /// <param name="length">位数组中的位数</param>
        /// <param name="defaultValue">分配给每个位的初始值</param>
        public OwBitArray(int length, bool defaultValue)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            _length = length;
            int arrayLength = GetArrayLength(length);
            _array = new OwCollection<nuint>(arrayLength);
            nuint fillValue = defaultValue ? nuint.MaxValue : 0;
            var span = _array.InsertRange(0, arrayLength);
            span.Fill(fillValue);
        }
        /// <summary>
        /// 初始化 OwBitArray 类的新实例，从字节数组复制值
        /// </summary>
        /// <param name="bytes">包含要复制到位数组的值的字节数组</param>
        public OwBitArray(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            _length = bytes.Length * 8;
            int arrayLength = GetArrayLength(_length);
            _array = new OwCollection<nuint>(arrayLength);
            // 🔧 优化：使用 InsertRange 批量预留空间，然后直接写入 Span
            var span = _array.InsertRange(0, arrayLength, false);
            int bytesPerElement = IntPtr.Size;
            for (int i = 0; i < arrayLength; i++)
            {
                nuint value = 0;
                int byteIndex = i * bytesPerElement;
                int bytesToCopy = Math.Min(bytesPerElement, bytes.Length - byteIndex);
                for (int j = 0; j < bytesToCopy; j++)
                {
                    value |= (nuint)bytes[byteIndex + j] << j * 8;
                }
                span[i] = value; // 直接写入 Span，避免逐个 InsertByRef
            }
        }
        #endregion [构造函数]
        #region [属性]
        /// <summary>
        /// 获取位数组中的位数
        /// </summary>
        public int Length => _length;
        /// <summary>
        /// 获取位数组当前的容量（可存储的位数）
        /// </summary>
        public int Capacity => _array.Count * BitsPerElement;
        /// <summary>
        /// 获取位数组中值为 true 的位数（动态扫描，O(n) 复杂度，排除视野外垃圾位）
        /// </summary>
        public int TrueCount
        {
            get
            {
                if (_length == 0)
                    return 0;
                int count = 0;
                var span = _array.AsSpan();
                int fullElements = _length / BitsPerElement;
                for (int i = 0; i < fullElements; i++)
                {
                    count += BitOperations.PopCount(span[i]);
                }
                int remainingBits = _length % BitsPerElement;
                if (remainingBits > 0)
                {
                    nuint mask = nuint.MaxValue >> BitsPerElement - remainingBits;
                    count += BitOperations.PopCount(span[fullElements] & mask);
                }
                return count;
            }
        }
        /// <summary>
        /// 获取或设置位数组中指定位置的位值
        /// </summary>
        /// <param name="index">要访问的位的从零开始的索引</param>
        /// <returns>index 处位的值</returns>
        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                int arrayIndex = index / BitsPerElement;
                int bitIndex = index % BitsPerElement;
                return (_array.GetByRef(arrayIndex) & (nuint)1 << bitIndex) != 0;
            }
            set
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                int arrayIndex = index / BitsPerElement;
                int bitIndex = index % BitsPerElement;
                ref var element = ref _array.GetByRef(arrayIndex);
                if (value)
                {
                    element |= (nuint)1 << bitIndex;
                }
                else
                {
                    element &= ~((nuint)1 << bitIndex);
                }
            }
        }
        #endregion [属性]
        #region [公有方法]
        /// <summary>
        /// 将所有位设置为指定值
        /// </summary>
        public void SetAll(bool value)
        {
            if (_length == 0)
                return;
            var span = _array.AsSpan();
            nuint fillValue = value ? nuint.MaxValue : 0;
            span.Fill(fillValue);
        }
        /// <summary>
        /// 从末尾添加指定数量的位，新位初始化为指定值
        /// </summary>
        /// <param name="count">要添加的位数</param>
        /// <param name="value">新位的初始值</param>
        public void AddBits(int count, bool value)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;
            int oldLength = _length;
            int newLength = oldLength + count;
            int oldArrayLength = _array.Count;
            int newArrayLength = GetArrayLength(newLength);
            int elementsToAdd = newArrayLength - oldArrayLength;
            if (elementsToAdd > 0)
            {
                nuint fillValue = value ? nuint.MaxValue : 0;
                var span = _array.InsertRange(_array.Count, elementsToAdd, false);
                span.Fill(fillValue);
            }
            if (oldLength > 0)
            {
                int startBitIndex = oldLength % BitsPerElement;
                if (startBitIndex != 0)
                {
                    int bitsInFirstElement = Math.Min(BitsPerElement - startBitIndex, count);
                    nuint mask = nuint.MaxValue >> BitsPerElement - bitsInFirstElement << startBitIndex;
                    ref var element = ref _array.GetByRef(oldArrayLength);
                    if (value)
                    {
                        element |= mask;
                    }
                    else
                    {
                        element &= ~mask;
                    }
                }
            }
            _length = newLength;
        }
        /// <summary>
        /// 从位数组中移除指定范围的位
        /// </summary>
        /// <param name="index">要移除的位范围的起始索引</param>
        /// <param name="count">要移除的位数</param>
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index + count > _length)
                throw new ArgumentOutOfRangeException(nameof(count), $"索引 ({index}) 和数量 ({count}) 超出位数组范围 ({_length})");
            if (count == 0)
                return;
            if (index + count == _length)
            {
                int newLength = _length - count;
                int newArrayLength = GetArrayLength(newLength);
                int currentArrayLength = _array.Count;
                if (newArrayLength < currentArrayLength)
                {
                    _array.RemoveRange(newArrayLength, currentArrayLength - newArrayLength);
                }
                _length = newLength;
            }
            else
            {
                throw new NotSupportedException("当前仅支持从末尾移除位，index + count 必须等于 Length");
            }
        }
        /// <summary>
        /// 设置位数组的长度，如果新长度大于当前长度则扩展，新位初始化为指定值
        /// </summary>
        /// <param name="newLength">新的位数</param>
        /// <param name="defaultValue">新位的初始值</param>
        public void SetLength(int newLength, bool defaultValue)
        {
            if (newLength < 0)
                throw new ArgumentOutOfRangeException(nameof(newLength));
            if (newLength > _length)
            {
                AddBits(newLength - _length, defaultValue);
            }
            else if (newLength < _length)
            {
                RemoveRange(newLength, _length - newLength);
            }
        }
        /// <summary>
        /// 对当前位数组和指定位数组执行按位 AND 运算
        /// </summary>
        public OwBitArray And(OwBitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (_length != value._length)
                throw new ArgumentException("位数组长度必须相同");
            var thisSpan = _array.AsSpan();
            var valueSpan = value._array.AsSpan();
            int count = _array.Count;
            for (int i = 0; i < count; i++)
            {
                thisSpan[i] &= valueSpan[i];
            }
            return this;
        }
        /// <summary>
        /// 对当前位数组和指定位数组执行按位 OR 运算
        /// </summary>
        public OwBitArray Or(OwBitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (_length != value._length)
                throw new ArgumentException("位数组长度必须相同");
            var thisSpan = _array.AsSpan();
            var valueSpan = value._array.AsSpan();
            int count = _array.Count;
            for (int i = 0; i < count; i++)
            {
                thisSpan[i] |= valueSpan[i];
            }
            return this;
        }
        /// <summary>
        /// 对当前位数组和指定位数组执行按位 XOR 运算
        /// </summary>
        public OwBitArray Xor(OwBitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (_length != value._length)
                throw new ArgumentException("位数组长度必须相同");
            var thisSpan = _array.AsSpan();
            var valueSpan = value._array.AsSpan();
            int count = _array.Count;
            for (int i = 0; i < count; i++)
            {
                thisSpan[i] ^= valueSpan[i];
            }
            return this;
        }
        /// <summary>
        /// 对当前位数组执行按位 NOT 运算
        /// </summary>
        public OwBitArray Not()
        {
            var span = _array.AsSpan();
            for (int i = 0; i < _array.Count; i++)
            {
                span[i] = ~span[i];
            }
            return this;
        }
        /// <summary>
        /// 将位数组复制到数组，从指定索引开始
        /// </summary>
        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (array is bool[] boolArray)
            {
                if (array.Length - index < _length)
                    throw new ArgumentException("目标数组空间不足");
                for (int i = 0; i < _length; i++)
                {
                    boolArray[index + i] = this[i];
                }
            }
            else
            {
                throw new ArgumentException("数组类型必须是 bool[]");
            }
        }
        /// <summary>
        /// 在指定范围内搜索指定值的第一个匹配项，并返回其从零开始的索引（使用硬件加速优化）
        /// </summary>
        /// <param name="value">要搜索的值</param>
        /// <param name="startIndex">从零开始的搜索起始索引</param>
        /// <param name="count">要搜索的元素数</param>
        /// <returns>如果找到 value，则为该值第一次出现的从零开始的索引；否则为 -1</returns>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 或 count 超出有效范围</exception>
        public int IndexOf(bool value, int startIndex, int count)
        {
            if (startIndex < 0 || startIndex > _length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count < 0 || startIndex + count > _length)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return -1;
            int endIndex = startIndex + count;
            var span = _array.AsSpan();
            int startArrayIndex = startIndex / BitsPerElement;
            int startBitIndex = startIndex % BitsPerElement;
            int endArrayIndex = (endIndex - 1) / BitsPerElement;
            int endBitIndex = (endIndex - 1) % BitsPerElement;
            if (startArrayIndex == endArrayIndex)
            {
                nuint element = span[startArrayIndex];
                int validBits = endBitIndex - startBitIndex + 1;
                nuint mask = nuint.MaxValue >> BitsPerElement - validBits << startBitIndex;
                nuint maskedElement = element & mask;
                nuint searchPattern = value ? maskedElement : ~element & mask;
                if (searchPattern != 0)
                {
                    int bitPosition = BitOperations.TrailingZeroCount(searchPattern);
                    return startArrayIndex * BitsPerElement + bitPosition;
                }
                return -1;
            }
            if (startBitIndex != 0)
            {
                nuint element = span[startArrayIndex];
                nuint mask = nuint.MaxValue << startBitIndex;
                nuint maskedElement = element & mask;
                nuint searchPattern = value ? maskedElement : ~element & mask;
                if (searchPattern != 0)
                {
                    int bitPosition = BitOperations.TrailingZeroCount(searchPattern);
                    return startArrayIndex * BitsPerElement + bitPosition;
                }
                startArrayIndex++;
            }
            for (int arrayIndex = startArrayIndex; arrayIndex < endArrayIndex; arrayIndex++)
            {
                nuint element = span[arrayIndex];
                nuint searchPattern = value ? element : ~element;
                if (searchPattern != 0)
                {
                    int bitPosition = BitOperations.TrailingZeroCount(searchPattern);
                    return arrayIndex * BitsPerElement + bitPosition;
                }
            }
            nuint lastElement = span[endArrayIndex];
            nuint lastMask = nuint.MaxValue >> BitsPerElement - endBitIndex - 1;
            nuint lastMaskedElement = lastElement & lastMask;
            nuint lastSearchPattern = value ? lastMaskedElement : ~lastElement & lastMask;
            if (lastSearchPattern != 0)
            {
                int bitPosition = BitOperations.TrailingZeroCount(lastSearchPattern);
                return endArrayIndex * BitsPerElement + bitPosition;
            }
            return -1;
        }
        #endregion [公有方法]
        #region [ICollection Members]
        public int Count => _length;
        public bool IsSynchronized => false;
        public object SyncRoot => this;
        #endregion [ICollection Members]
        #region [IEnumerable Members]
        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < _length; i++)
            {
                yield return this[i];
            }
        }
        #endregion [IEnumerable Members]
        #region [IDisposable Members]
        public void Dispose()
        {
            _array?.Dispose();
            _array = null;
            _length = 0;
        }
        #endregion [IDisposable Members]
        #region [ICloneable Members]
        /// <summary>
        /// 创建当前 OwBitArray 的浅表副本
        /// </summary>
        /// <returns>当前位数组的副本</returns>
        public object Clone()
        {
            var clone = new OwBitArray(_length, false);
            var sourceSpan = _array.AsSpan();
            var destSpan = clone._array.AsSpan();
            sourceSpan.CopyTo(destSpan);
            return clone;
        }
        #endregion [ICloneable Members]
        #region [辅助方法]
        /// <summary>
        /// 计算存储指定位数所需的元素数量
        /// </summary>
        /// <param name="bitLength">位数</param>
        /// <returns>所需的元素数量</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetArrayLength(int bitLength)
        {
            return (bitLength + BitsPerElement - 1) / BitsPerElement;
        }
        #endregion [辅助方法]
    }
}
