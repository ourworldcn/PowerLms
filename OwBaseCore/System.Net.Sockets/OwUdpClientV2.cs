using System;
using System.Buffers;
using System.Collections.Generic;
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
    public enum OwUdpDataFlags : int
    {
        /// <summary>
        /// 这是一个数据包。随后4字节是一个uint类型，用于指出包的序号。
        /// </summary>
        Data = 1 << 31,
        /// <summary>
        /// 这是一个命令字，随后发送的是已接受到的最大包号。
        /// </summary>
        Command = 2,
    }

    internal class UdpDataEntry
    {
        private int seq;

        public UdpDataEntry()
        {
        }

        /// <summary>
        /// 发送到对方的本机唯一标识符。
        /// </summary>
        public Guid Id
        {
            get
            {
                var tmp = new byte[16];
                Array.Copy(Buffer, 4, tmp, 0, 16);
                return new Guid(tmp);
            }

            set
            {
                Array.Copy(value.ToByteArray(), 0, Buffer, 4, 16);
            }
        }

        /// <summary>
        /// 包的序号。4字节含标志位，调用者需要自己处理标志位问题。
        /// </summary>
        public int Seq
        {
            get
            {
                IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Buffer, 0));
                return seq;
            }
            set
            {
                var tmp = IPAddress.HostToNetworkOrder(value);
                BitConverter.GetBytes(tmp).CopyTo(Buffer, 0);
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
        /// 缓冲区。第一个是标志字节，随后是4字节序号。后跟负载数据。
        /// 大小必须是<see cref="OwUdpClientV2.Mtu"/>个字节。第一个字节有特殊含义（D0-D7标识最低位到最高位。），如下：
        /// D7=0则这是一个数据包，此字节与随后3字节组成了一个int型代表包的书序号。
        /// D7=1且D6=0,说明这是一个纯信息报。
        /// </summary>
        public byte[] Buffer { get; set; } = ArrayPool<byte>.Shared.Rent(OwUdpServerV2.Mtu);

        /// <summary>
        /// 小于或等于<see cref="OwUdpServerV2.Mtu"/>。
        /// </summary>
        public int Count { get; set; }
    }

    public class OwUdpClientV2 : IDisposable
    {
        UdpClient _Udp;

        /// <summary>
        /// Internet上的标准MTU值为576字节，所以在进行Internet的UDP编程时，最好将UDP的数据长度控件在548字节(576-8-20)以内。
        /// </summary>
        internal const int Mtu = 548;

        /// <summary>
        /// 本类使用的负载长度，去掉1个标志字节，4位序号。
        /// </summary>
        const int Mts = Mtu - 4 - 1;

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
        /// <param name="udp"></param>
        public static void ResetError(UdpClient udp)
        {
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint IOC_UDP_RESET = IOC_IN | IOC_VENDOR | 12;
            udp.Client.IOControl((int)IOC_UDP_RESET, new byte[] { Convert.ToByte(false) }, null);
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
