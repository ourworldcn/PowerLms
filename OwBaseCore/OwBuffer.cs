using System;
using System.Buffers;
using System.Runtime.CompilerServices;
namespace OW;
/// <summary>
/// 提供内存重新分配和数据迁移的静态工具方法。
/// </summary>
public static class OwBuffer
{
    /// <summary>
    /// 重新分配内存并迁移指定范围的有效数据。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="pool">用于分配新内存的内存池</param>
    /// <param name="oldOwner">包含原始数据的内存所有者</param>
    /// <param name="validStart">原始数据中有效区域的起始索引</param>
    /// <param name="validLength">有效区域的长度</param>
    /// <param name="newCapacity">请求的新内存容量（实际分配的容量可能更大）</param>
    /// <param name="clearValidRegion">是否清空旧内存中的有效区域（默认 false）</param>
    /// <returns>包含迁移后数据的新内存所有者</returns>
    /// <exception cref="ArgumentNullException">pool 或 oldOwner 为 null</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// validStart、validLength 或 newCapacity 为负数
    /// </exception>
    /// <exception cref="ArgumentException">有效区域超出原始内存范围</exception>
    /// <remarks>
    /// <para>
    /// 此方法将 oldOwner 中 [validStart, validStart + validLength) 范围的数据
    /// 复制到新内存的起始位置 [0, toCopy)，其中 toCopy 为 validLength 和实际新容量的较小值。
    /// </para>
    /// <para>
    /// 如果 clearValidRegion 为 true 且 T 是引用类型或包含引用，
    /// 则会清空旧内存中的有效区域以避免内存泄漏。调用者仍需负责释放 oldOwner。
    /// </para>
    /// <para>
    /// 注意：内存池返回的实际容量可能大于 newCapacity，这是正常行为。
    /// </para>
    /// </remarks>
    public static IMemoryOwner<T> Resize<T>(
        MemoryPool<T> pool,
        IMemoryOwner<T> oldOwner,
        int validStart,
        int validLength,
        int newCapacity,
        bool clearValidRegion = false)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(oldOwner);
        if (validStart < 0)
            throw new ArgumentOutOfRangeException(nameof(validStart), validStart, "起始索引不能为负数");
        if (validLength < 0)
            throw new ArgumentOutOfRangeException(nameof(validLength), validLength, "长度不能为负数");
        if (newCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(newCapacity), newCapacity, "容量不能为负数");
        var oldMemory = oldOwner.Memory;
        if (validStart + validLength > oldMemory.Length)
            throw new ArgumentException("有效区间超出旧内存范围", nameof(validStart));
        var newOwner = pool.Rent(newCapacity);
        var newMemory = newOwner.Memory;
        if (validLength > 0)
        {
            int toCopy = Math.Min(validLength, newMemory.Length);
            oldMemory.Span.Slice(validStart, toCopy).CopyTo(newMemory.Span);
            if (clearValidRegion && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                oldMemory.Span.Slice(validStart, validLength).Clear();
        }
        return newOwner;
    }
}
