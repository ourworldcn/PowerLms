using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/*
 * 目标框架	符号	其他符号
（在 .NET 5+ SDK 中可用）	平台符号（仅
在指定特定于 OS 的 TFM 时可用）
.NET Framework	NETFRAMEWORK, NET48, NET472, NET471, NET47, NET462, NET461, NET46, NET452, NET451, NET45, NET40, NET35, NET20	NET48_OR_GREATER、NET472_OR_GREATER、NET471_OR_GREATER、NET47_OR_GREATER、NET462_OR_GREATER、NET461_OR_GREATER、NET46_OR_GREATER、NET452_OR_GREATER、NET451_OR_GREATER、NET45_OR_GREATER、NET40_OR_GREATER、NET35_OR_GREATER、NET20_OR_GREATER	
.NET Standard	NETSTANDARD, NETSTANDARD2_1, NETSTANDARD2_0, NETSTANDARD1_6, NETSTANDARD1_5, NETSTANDARD1_4, NETSTANDARD1_3, NETSTANDARD1_2, NETSTANDARD1_1, NETSTANDARD1_0	NETSTANDARD2_1_OR_GREATER、NETSTANDARD2_0_OR_GREATER、NETSTANDARD1_6_OR_GREATER、NETSTANDARD1_5_OR_GREATER、NETSTANDARD1_4_OR_GREATER、NETSTANDARD1_3_OR_GREATER、NETSTANDARD1_2_OR_GREATER、NETSTANDARD1_1_OR_GREATER、NETSTANDARD1_0_OR_GREATER	
.NET 5 及更高版本（和 .NET Core）	NET、NET8_0、NET7_0、NET6_0、NET5_0、NETCOREAPP、NETCOREAPP3_1、NETCOREAPP3_0、NETCOREAPP2_2、NETCOREAPP2_1、NETCOREAPP2_0、NETCOREAPP1_1、NETCOREAPP1_0	NET8_0_OR_GREATER, NET7_0_OR_GREATER, NET6_0_OR_GREATER, NET5_0_OR_GREATER, NETCOREAPP3_1_OR_GREATER, NETCOREAPP3_0_OR_GREATER, NETCOREAPP2_2_OR_GREATER, NETCOREAPP2_1_OR_GREATER, NETCOREAPP2_0_OR_GREATER, NETCOREAPP1_1_OR_GREATER, NETCOREAPP1_0_OR_GREATER	ANDROID、BROWSER、IOS、MACCATALYST、MACOS、TVOS、WINDOWS
[OS][version]（例如，IOS15_1），
[OS][version]_OR_GREATER（例如，IOS15_1_OR_GREATER）
 */
#if !NET47_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
#error 必须有 .NET Framework 4.7 或 .NET Core 2.1 框架支持
#endif

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

    internal class OwUdpRemoteEntryBase
    {
        public int _Id;
        /// <summary>
        /// 终结点Id。仅低24位有效。
        /// </summary>
        [Range(0, 0x00ff_ffff)]
        public int Id
        {
            get => _Id;
            set
            {
                if (value > 0x00ff_ffff || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _Id = value;
            }
        }

    }

    internal class OwUdpRemoteEntry : OwUdpRemoteEntryBase
    {
        public OwUdpRemoteEntry()
        {
        }

        public DateTime LastReciveWorldDateTime { get; set; }

        /// <summary>
        /// 收到的数据队列。按收到的包号升序排序。
        /// </summary>
        public List<OwUdpDataEntry> ReciveData { get; set; } = new List<OwUdpDataEntry>();

        /// <summary>
        /// 包序号，记录了已用的最大序号，可能需要回绕。
        /// </summary>
        public int MaxSeq;

        /// <summary>
        /// 远程终结点。
        /// </summary>
        public volatile IPEndPoint Remote;
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
        /// 包的序号。
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
        public DateTime? FirstSendDateTime;

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

    /// <summary>
    /// udp客户端类。为支持Unity使用，仅使用.NET Framework 4.7支持的功能。
    /// </summary>
    public class OwUdpClientV2 : IDisposable
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwUdpClientV2()
        {

        }

        /// <summary>
        /// 通讯Id，仅低24位有用。若为null，标识尚未连接。
        /// </summary>
        int? _Id;

        UdpClient _UdpClient;

        #region 方法

        #region 静态方法

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
            const uint IOC_IN = 0x80000000;
            const uint IOC_VENDOR = 0x18000000;
            const uint IOC_UDP_RESET = IOC_IN | IOC_VENDOR | 12;
            socket.IOControl(unchecked((int)IOC_UDP_RESET), new byte[] { Convert.ToByte(false) }, null);
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
        #endregion 静态方法

        public void Init([Range(0, OwUdpDataEntry.Mts)] byte[] evidence)
        {
            if (evidence.Length > OwUdpDataEntry.Mts)
                throw new ArgumentException("超长", nameof(evidence));
            var dataEntry = new OwUdpDataEntry
            {
                Id = 0,
            };
            dataEntry.Buffer = new byte[OwUdpDataEntry.Mtu];
            dataEntry.Offset = 0;
            dataEntry.Count = OwUdpDataEntry.Mtu;

            dataEntry.Buffer[0] = (byte)(OwUdpDataKind.CommandDgram);

            evidence.CopyTo(dataEntry.Buffer, 8);
        }

        /// <summary>
        /// 连接服务器并"握手"
        /// </summary>
        /// <param name="server">服务器IP地址或DNS名。</param>
        /// <param name="port">端口号。</param>
        public void Connect(string server, short port)
        {
            var ips = Dns.GetHostAddresses(server);
            var ip = ips.FirstOrDefault(c => c.AddressFamily == AddressFamily.InterNetwork || c.AddressFamily == AddressFamily.InterNetworkV6);
            Connect(new IPEndPoint(ip, port));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint">服务器终结点地址。</param>
        /// <exception cref="SocketException">访问套接字时出错。</exception>
        /// <exception cref="ArgumentNullException">endPoint 为 null。</exception>
        virtual public void Connect(IPEndPoint endPoint)
        {
            _UdpClient?.Dispose();  //容错
            _UdpClient = new UdpClient(0);
            _UdpClient.Connect(endPoint);
            //自有初始化内容
            ConnectCore();
        }

        /// <summary>
        /// 自有初始化。
        /// </summary>
        virtual protected void ConnectCore()
        {
            var buffSend = new byte[] { 1, 2 };
            _UdpClient.Send(buffSend);
            var endPoint = (IPEndPoint)_UdpClient.Client.RemoteEndPoint;

            var buffRecv = _UdpClient.Receive(ref endPoint);
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

