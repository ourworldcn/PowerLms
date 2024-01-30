using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    /// <summary>
    /// 封装的第一个标志字节。
    /// </summary>
    [Flags]
    public enum OwUdpDataKind : byte
    {
        None = 0,

        /// <summary>
        /// 起始帧。
        /// 可能既是起始帧又是终止帧，这说明是一个独立帧。
        /// </summary>
        StartDgram = 1,

        /// <summary>
        /// 这位为0说明时一个正常的数据帧。
        /// </summary>
        CommandDgram = 2,

        /// <summary>
        /// 终止帧。
        /// 可能既是起始帧又是终止帧，这说明是一个独立帧。也能是既非起始帧也非终止帧，这说明是一个中间帧，
        /// </summary>
        EndDgram = 4,
    }

    internal class OwUdpClientEntry
    {
        public OwUdpClientEntry()
        {
        }

        public int Id { get; set; }

        public DateTime LastReciveWorldDateTime { get; set; }

        /// <summary>
        /// 收到的数据队列。按收到的包号升序排序。
        /// </summary>
        public List<OwUdpDataEntry> ReciveData { get; set; } = new List<OwUdpDataEntry>();
    }

    internal class OwUdpDataEntry
    {

        /// <summary>
        /// Internet上的标准MTU值为576字节，所以在进行Internet的UDP编程时，最好将UDP的数据长度控件在548字节(576-8-20)以内。
        /// </summary>
        internal const int Mtu = 576 - 8 - 20;

        /// <summary>
        /// 本类使用的负载长度，去掉1个标志字节,3个字节通讯Id，4位序号。
        /// </summary>
        internal const int Mts = Mtu - 4/*标志字节 和 通讯Id*/ - 4/*已接收的连续包的最大序号*/;

        public OwUdpDataEntry()
        {
        }

        /// <summary>
        /// 发送到对方的本机唯一标识符。
        /// </summary>
        public int Id
        {
            get
            {
                return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Buffer, Offset)) & 0x00ff_ffff;
            }

            set
            {
                var ary = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value & 0x00ff_ffff));
                Array.Copy(ary, 1, Buffer, Offset + 1, 4 - 1);
            }
        }

        /// <summary>
        /// 包的序号。4字节含标志位，调用者需要自己处理标志位问题。
        /// </summary>
        public int Seq
        {
            get
            {
                return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Buffer, Offset + 4));
            }
            set
            {
                var tmp = IPAddress.HostToNetworkOrder(value);
                BitConverter.GetBytes(tmp).CopyTo(Buffer, Offset + 4);
            }
        }

        /// <summary>
        /// 首次发送的时间。空表示尚未发送。
        /// </summary>
        public DateTime? FirstSendDateTime { get; set; }

        /// <summary>
        /// 最后一次发送的世界时间。<see cref="OwHelper.WorldNow"/>。空标识未发送。
        /// </summary>
        public DateTime? LastSendDateTime { get; set; }

        /// <summary>
        /// 缓冲区。第一个是标志字节，随后是3字节客户Id,4字节包顺序号。后跟负载数据。
        /// 大小必须是<see cref="Mtu"/>个字节。第一个字节有特殊含义<seealso cref="OwUdpDataKind"/>。
        /// </summary>
        public byte[] Buffer { get; set; } = ArrayPool<byte>.Shared.Rent(Mtu);

        /// <summary>
        /// 偏移量。
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// 小于或等于<see cref="Mtu"/>。总计有多少字节数据。
        /// </summary>
        public int Count { get; set; }

    }

    public class OwUdpClientV2 : IDisposable
    {
        UdpClient _Udp;

        /// <summary>
        /// 通讯Id，仅低24位有用。若为null，标识尚未连接。
        /// </summary>
        int? _Id;

        #region 方法
        /// <summary>
        /// 原子递增一个31位整数，当由<see cref="int.MaxValue"/>回绕到<see cref="int.MinValue"/>时自动设置为0。
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int IncrementInt31(ref int i)
        {
            var result = Interlocked.Increment(ref i);
            if (result < 0)
            {
                var tmp = result & 0x7fff_ffff;
                Interlocked.CompareExchange(ref i, tmp, result);
                return tmp;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        public static void ResetError(Socket socket)
        {
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint IOC_UDP_RESET = IOC_IN | IOC_VENDOR | 12;
            socket.IOControl((int)IOC_UDP_RESET, new byte[] { Convert.ToByte(false) }, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint IncrementUInt32(ref int i)
        {
            return (uint)Interlocked.Increment(ref i);
        }

        #endregion 方法

        #region IDisposable接口相关

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _Udp?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwUdpClientV2()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable接口相关
    }

}

#if !NETCOREAPP //若非NetCore程序
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options
{
    //
    // 摘要:
    //     Used to retrieve configured TOptions instances.
    //
    // 类型参数:
    //   TOptions:
    //     The type of options being requested.
    public interface IOptions</*[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]*/ out TOptions> where TOptions : class
    {
        //
        // 摘要:
        //     The default configured TOptions instance
        TOptions Value { get; }
    }
}
#endif
