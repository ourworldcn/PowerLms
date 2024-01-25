using Microsoft.Extensions.Options;
using OW;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    internal class OwUdpData
    {
        public OwUdpData(int length)
        {
            Debug.Assert(length > 9);
            Buffer = new byte[length];
        }

        public byte Flag { get => Buffer[0]; set => Buffer[0] = value; }

        public ulong Seq { get => BitConverter.ToUInt64(Buffer, 1); set => BitConverter.TryWriteBytes(Buffer.AsSpan(1, 8), value); }

        public void Set(byte flag, ulong seq, ReadOnlySpan<byte> buffer)
        {
            Buffer[0] = flag;
            buffer.CopyTo(Buffer.AsSpan(9, buffer.Length));
        }

        public IPEndPoint Romote { get; set; }

        public byte[] Buffer { get; set; }
    }

    internal class OwUdpServerDataEntry
    {
        /// <summary>
        /// 唯一不变标识。
        /// </summary>
        public string Id { get; set; }

        public ulong Seq;
    }

    public class OwUdpServerOptions : IOptions<OwUdpServerOptions>
    {
        public OwUdpServerOptions Value => this;
    }

    /// <summary>
    /// Udp服务端。与 OwUdpClient 配合使用。
    /// </summary>
    public class OwUdpServer : OwDisposableBase, IDisposable
    {
        public OwUdpServer(IPEndPoint iPEndPoint, IOptions<OwUdpServerOptions> options)
        {
            _IPEndPoint = iPEndPoint;
            _Options = options;
        }

        /// <summary>
        /// Internet上的标准MTU值为576字节，所以在进行Internet的UDP编程时，最好将UDP的数据长度控件在548字节(576-8-20)以内。
        /// </summary>
        const int Mtu = 548;
        const int Mts = Mtu - 8 - 1;
        IPEndPoint _IPEndPoint;
        UdpClient _Udp;

        IOptions<OwUdpServerOptions> _Options;
        /// <summary>
        /// 待发送数据。
        /// </summary>
        ConcurrentQueue<(string, byte[])> _Datas = new ConcurrentQueue<(string, byte[])>();

        ConcurrentDictionary<(string, short), IPEndPoint> _EndPoint = new ConcurrentDictionary<(string, short), IPEndPoint>();

        ConcurrentDictionary<string, OwUdpServerDataEntry> _Entries = new ConcurrentDictionary<string, OwUdpServerDataEntry>();
        ArrayPool<byte> _Pool = ArrayPool<byte>.Shared;

        void ResetError()
        {
            const uint IOC_IN = 0x80000000;
            const uint IOC_VENDOR = 0x18000000;
            const uint IOC_UDP_RESET = IOC_IN | IOC_VENDOR | 12;
            _Udp.Client.IOControl(unchecked((int)IOC_UDP_RESET), new byte[] { Convert.ToByte(false) }, null);
        }

        public void Send(byte[] data, int offset, int length, string id)
        {
            if (data.Length == 0) return;

            _Pool.Rent(Mtu);

            var ary = data.Chunk(Mts).ToList();
            var opt = _Entries.GetOrAdd(id, c => new OwUdpServerDataEntry() { Id = id });

            for (var i = 0; i < data.Length; i += Mts)
            {
                var buff = _Pool.Rent(Mts);
                Buffer.BlockCopy(data, i, buff, 0, Mts);
            }
        }

        /// <summary>
        /// 将序列的元素拆分为最大 size大小的区块。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset">每个分割块最前面保留的空余元素数量。</param>
        /// <param name="count">每个块的最大尺寸,不含偏移空白区域，单位:字节。</param>
        /// <param name="pool">池对象。</param>
        /// <returns></returns>
        public static IEnumerable<byte[]> Chunk(byte[] data, int offset, int count, ArrayPool<byte> pool = null)
        {
            pool ??= ArrayPool<byte>.Shared;
            for (var i = 0; i < data.Length; i += offset + count)
            {
                var buffer = pool.Rent(Mts);
                data.AsSpan(i, offset + count);
                yield return buffer;
            }
        }

    }

    public static class OwMemoryExtensions
    {
        public static Span<T> SafeAsSpan<T>(this T[] src, int start, int length)
        {
            return start + length <= src.Length ? src.AsSpan(start, length) : src.AsSpan(start, src.Length - start);
        }
    }
}
