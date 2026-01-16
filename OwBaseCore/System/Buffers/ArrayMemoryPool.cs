using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// 基于 ArrayPool 的 MemoryPool 实现，提供高性能的内存租借和归还机制
    /// </summary>
    /// <typeparam name="T">池中元素的类型</typeparam>
    public sealed class ArrayMemoryPool<T> : MemoryPool<T>
    {
        private readonly ArrayPool<T> _arrayPool;
        private bool _disposed;

        /// <summary>
        /// 初始化 ArrayMemoryPool 类的新实例，使用共享的 ArrayPool
        /// </summary>
        public ArrayMemoryPool() : this(ArrayPool<T>.Shared)
        {
        }

        /// <summary>
        /// 初始化 ArrayMemoryPool 类的新实例，使用指定的 ArrayPool
        /// </summary>
        /// <param name="arrayPool">用于内存管理的 ArrayPool</param>
        /// <exception cref="ArgumentNullException">arrayPool 为 null</exception>
        public ArrayMemoryPool(ArrayPool<T> arrayPool)
        {
            _arrayPool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
        }

        /// <summary>
        /// 获取此池可返回的缓冲区的最大长度
        /// </summary>
        public override int MaxBufferSize => int.MaxValue;

        /// <summary>
        /// 从池中租借至少指定大小的内存块
        /// </summary>
        /// <param name="minBufferSize">所需的最小缓冲区大小，-1 表示使用默认大小（4096），0 表示空数组</param>
        /// <returns>一个 IMemoryOwner，表示租借的内存</returns>
        /// <exception cref="ObjectDisposedException">池已被释放</exception>
        /// <exception cref="ArgumentOutOfRangeException">minBufferSize 小于 -1</exception>
        public override IMemoryOwner<T> Rent(int minBufferSize = -1)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ArrayMemoryPool<T>));
            if (minBufferSize < -1)
                throw new ArgumentOutOfRangeException(nameof(minBufferSize));
            int actualSize = minBufferSize == -1 ? 4096 : minBufferSize;
            return new ArrayMemoryOwner(_arrayPool, actualSize);
        }

        /// <summary>
        /// 释放池使用的所有资源
        /// </summary>
        /// <param name="disposing">如果为 true，则释放托管资源和非托管资源；如果为 false，则仅释放非托管资源</param>
        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }

        /// <summary>
        /// 基于 ArrayPool 的 IMemoryOwner 实现
        /// </summary>
        private sealed class ArrayMemoryOwner : IMemoryOwner<T>
        {
            private T[] _array;
            private readonly ArrayPool<T> _pool;

            public ArrayMemoryOwner(ArrayPool<T> pool, int minBufferSize)
            {
                _pool = pool;
                if (minBufferSize == 0)
                {
                    _array = Array.Empty<T>();
                }
                else
                {
                    _array = pool.Rent(minBufferSize);
                }
            }

            public Memory<T> Memory
            {
                get
                {
                    var array = _array;
                    if (array == null)
                        throw new ObjectDisposedException(nameof(ArrayMemoryOwner));
                    return array.AsMemory();
                }
            }

            public void Dispose()
            {
                var array = _array;
                if (array != null)
                {
                    _array = null;
                    if (array.Length > 0)
                    {
                        _pool.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                    }
                }
            }
        }
    }
}
