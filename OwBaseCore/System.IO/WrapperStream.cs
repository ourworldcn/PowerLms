/*
 * 文件名：WrapperStream.cs
 * 作者：OW
 * 创建日期：2025年1月17日
 * 修改日期：2025年1月17日
 * 描述：流包装器类，提供对底层流的包装，支持控制在释放时是否保持底层流打开
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// 流包装器类，提供对底层流的包装功能
    /// 主要用于控制在释放包装器时是否同时释放底层流
    /// </summary>
    public class WrapperStream : Stream
    {
        #region 私有字段
        private readonly Stream _baseStream; // 被包装的底层流
        private readonly bool _leaveOpen; // 释放时是否保持底层流打开
        private bool _disposed; // 是否已释放
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化流包装器
        /// </summary>
        /// <param name="baseStream">要包装的底层流，不能为null</param>
        /// <param name="leaveOpen">释放包装器时是否保持底层流打开，true=不处置基础流，默认false</param>
        /// <exception cref="ArgumentNullException">当baseStream为null时抛出</exception>
        public WrapperStream(Stream baseStream, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            _baseStream = baseStream;
            _leaveOpen = leaveOpen;
        }
        #endregion

        #region Stream 重写属性
        /// <summary>获取底层流是否支持读取</summary>
        public override bool CanRead => _baseStream.CanRead;

        /// <summary>获取底层流是否支持定位</summary>
        public override bool CanSeek => _baseStream.CanSeek;

        /// <summary>获取底层流是否支持写入</summary>
        public override bool CanWrite => _baseStream.CanWrite;

        /// <summary>获取底层流的长度</summary>
        public override long Length => _baseStream.Length;

        /// <summary>获取或设置底层流的当前位置</summary>
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
        #endregion

        #region Stream 重写方法
        /// <summary>刷新底层流的缓冲区</summary>
        public override void Flush() => _baseStream.Flush();

        /// <summary>从底层流读取数据</summary>
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);

        /// <summary>在底层流中定位到指定位置</summary>
        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        /// <summary>设置底层流的长度</summary>
        public override void SetLength(long value) => _baseStream.SetLength(value);

        /// <summary>向底层流写入数据</summary>
        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);

        #endregion

        #region 资源释放
        /// <summary>
        /// 释放流资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (!_leaveOpen) // 只有在不保留底层流时才释放
                {
                    _baseStream?.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing); // 调用基类的Dispose方法
        }

        #endregion
    }
}