/*
 * 为保证 NETSTANDARD 2.0 可用，有些设计重复了高版本框架中已有功能。
 */
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// 包装一个socket对象， 提供基于 SocketAsyncEventArgs 异步收发功能的简化。
    /// </summary>
    /// <remarks>如果使用面向连接的协议（如 TCP），则用于 ConnectAsync 与侦听主机连接。 使用 SendAsync 或 ReceiveAsync 异步通信数据。 可以使用 AcceptAsync.. 处理传入的连接请求。
    /// 如果使用无连接协议（如 UDP），则可以用于 SendToAsync 发送数据报和 ReceiveFromAsync接收数据报。
    /// 当前版本仅支持Udp协议。</remarks>
    public class SocketAsyncWrapper
    {
        #region 静态成员

        /// <summary>
        /// 为避免客户端无法使用对象池等netcore特有功能，这里自己管理对象 和 内存池。
        /// </summary>
        static ConcurrentStack<SocketAsyncEventArgs> _Pool = new ConcurrentStack<SocketAsyncEventArgs>();

        static ConcurrentStack<byte[]> _Pool1024 = new ConcurrentStack<byte[]>();

        #endregion 静态成员

        #region 构造函数

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="socket"></param>
        public SocketAsyncWrapper(Socket socket)
        {
            Socket = socket;
            Socket.ReceiveBufferSize = Math.Max(Socket.ReceiveBufferSize, Environment.ProcessorCount * Environment.SystemPageSize);
        }

        #endregion 构造函数

        volatile Socket _Socket;

        public Socket Socket { get => _Socket; protected set => _Socket = value; }

        #region 方法

        #region 缓冲区控制

        public SocketAsyncEventArgs RentEventArgs()
        {
            if (!_Pool.TryPop(out var result))
            {
                result = new SocketAsyncEventArgs();
                result.Completed += IO_Completed;
            }
            return result;
        }

        public void ReturnEventArgs(SocketAsyncEventArgs eventArgs)
        {
            _Pool.Push(eventArgs);
        }


        /// <summary>
        /// 获取一个缓冲区。
        /// </summary>
        /// <remarks>默认分配程序为给 Unity 使用，其它程序可考虑自定义该函数。</remarks>
        /// <param name="minimumLength">要在1-1024之间。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public virtual byte[] RentBuffer(int minimumLength)
        {
            if (minimumLength <= 0 || minimumLength > 1024) throw new ArgumentException("默认的缓冲区分配程序仅能处理1-1024字节尺寸的请求", nameof(minimumLength));
            if (!_Pool1024.TryPop(out var result)) result = new byte[1024];
            return result;
        }

        /// <summary>
        /// 返还缓冲区。
        /// </summary>
        /// <remarks>默认回收程序为给 Unity 使用，其它程序可考虑自定义该函数。</remarks>
        /// <param name="buffer"></param>
        /// <param name="clearBuffer"></param>
        public virtual void ReturnBuffer(byte[] buffer, bool clearBuffer = false)
        {
            if (buffer.Length == 1024)  //若符合回收条件
            {
                if (clearBuffer) for (int i = 0; i < 1024; i++) buffer[i] = 0;  //清理缓冲区
                _Pool1024.Push(buffer);
            }
        }

        #endregion 缓冲区控制

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer">不能发送 1024 以上的长度数据。另，实际情况最好不要使用自动拆包功能，以避免丢包和重复问题。
        /// 指向的数据在此函数调用后被此函数接管，不可以再使用。
        /// </param>
        /// <param name="remote"></param>
        /// <param name="userToken"></param>
        public void SendToAsync(ArraySegment<byte> buffer, EndPoint remote, object userToken)
        {
            if (buffer.Count > 1024) throw new ArgumentException("缓冲区过长。", nameof(buffer));
            //准备参数
            var e = RentEventArgs();
            e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
            e.RemoteEndPoint = remote;
            e.UserToken = userToken;
            //发送
            var b = Socket.SendToAsync(e);
            if (!b) ProcessSendTo(e);
        }

        /// <summary>
        /// 异步接收指定端点的数据报。
        /// </summary>
        /// <param name="buffer">缓冲区。</param>
        /// <param name="remote"></param>
        /// <param name="userToken"></param>
        public void ReceiveFromAsync(ArraySegment<byte> buffer, EndPoint remote, object userToken)
        {
            var e = RentEventArgs();
            e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
            e.RemoteEndPoint = remote;
            e.UserToken = userToken;
            var b = Socket.ReceiveFromAsync(e);
            if (!b) ProcessReceiveFrom(e);
        }

        #region IO处理操作
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            if (e.SocketError != SocketError.Success)
                ProcessError(e);
            else
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.None:
                        break;
                    case SocketAsyncOperation.Accept:
                        break;
                    case SocketAsyncOperation.Connect:
                        break;
                    case SocketAsyncOperation.Disconnect:
                        break;
                    case SocketAsyncOperation.Receive:
                        break;
                    case SocketAsyncOperation.ReceiveFrom:
                        ProcessReceiveFrom(e);
                        break;
                    case SocketAsyncOperation.ReceiveMessageFrom:
                        break;
                    case SocketAsyncOperation.Send:
                        break;
                    case SocketAsyncOperation.SendPackets:
                        break;
                    case SocketAsyncOperation.SendTo:
                        ProcessSendTo(e);
                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
        }

        /// <summary>
        /// 数据发送完成的回调函数。目前是空操作。
        /// </summary>
        /// <param name="e"></param>
        protected virtual void ProcessSendTo(SocketAsyncEventArgs e)
        {
        }

        /// <summary>
        /// 数据到达的回调函数。自动使用参数重新侦听。
        /// </summary>
        /// <param name="e"></param>
        protected virtual void ProcessReceiveFrom(SocketAsyncEventArgs e)
        {
            //OnReceiveFrom(new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred), e.RemoteEndPoint, e.UserToken);
            Socket.ReceiveFromAsync(e);
        }

        /// <summary>
        /// 处理错误。
        /// </summary>
        /// <param name="e"></param>
        protected virtual void ProcessError(SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                case SocketError.Success:
                case SocketError.Shutdown:
                case SocketError.ConnectionAborted:
                    break;
                case SocketError.OperationAborted:

                case SocketError.IOPending:

                case SocketError.Interrupted:

                case SocketError.AccessDenied:

                case SocketError.Fault:

                case SocketError.InvalidArgument:

                case SocketError.TooManyOpenSockets:

                case SocketError.WouldBlock:

                case SocketError.InProgress:

                case SocketError.AlreadyInProgress:

                case SocketError.NotSocket:

                case SocketError.DestinationAddressRequired:

                case SocketError.MessageSize:

                case SocketError.ProtocolType:

                case SocketError.ProtocolOption:

                case SocketError.ProtocolNotSupported:

                case SocketError.SocketNotSupported:

                case SocketError.OperationNotSupported:

                case SocketError.ProtocolFamilyNotSupported:

                case SocketError.AddressFamilyNotSupported:

                case SocketError.AddressAlreadyInUse:

                case SocketError.AddressNotAvailable:

                case SocketError.NetworkDown:

                case SocketError.NetworkUnreachable:

                case SocketError.NetworkReset:

                case SocketError.ConnectionReset:

                case SocketError.NoBufferSpaceAvailable:

                case SocketError.IsConnected:

                case SocketError.NotConnected:

                case SocketError.TimedOut:

                case SocketError.ConnectionRefused:

                case SocketError.HostDown:

                case SocketError.HostUnreachable:

                case SocketError.ProcessLimit:

                case SocketError.SystemNotReady:

                case SocketError.VersionNotSupported:

                case SocketError.NotInitialized:

                case SocketError.Disconnecting:

                case SocketError.TypeNotFound:

                case SocketError.HostNotFound:

                case SocketError.TryAgain:

                case SocketError.NoRecovery:

                case SocketError.NoData:
                    //_Logger.LogWarning("Rdm传输发生错误——{err}", e.SocketError.ToString());
                    //if (!_HostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
                    //    OwRdmClient.ResetError(_Socket);
                    break;
                case SocketError.SocketError:
                    OwRdmClient.ResetError(Socket);
                    break;
            }
        }

        #endregion IO处理操作

        #endregion 方法
    }

    /// <summary>
    /// 封装的第一个标志字节。
    /// </summary>
    [Flags]
    public enum OwRdmDgramKind : byte
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

    public class OwRdmDgram : IComparable<OwRdmDgram>
    {

        /// <summary>
        /// Internet上的标准MTU值为576字节，所以在进行Internet的UDP编程时，最好将UDP的数据长度控件在548字节(576-8-20)以内。
        /// </summary>
        public const int Mtu = 576 - 8 - 20;

        /// <summary>
        /// 本类使用的负载长度，去掉1个标志字节,3个字节通讯Id，4位序号。
        /// </summary>
        public const int Mts = Mtu - 4/*标志字节 和 通讯Id(防路由端口映射出现变化)*/ - 4/*已接收的连续包的最大序号*/;

        /// <summary>
        /// 用于简化池化本对象的情况。
        /// </summary>
        static ConcurrentStack<OwRdmDgram> _Pool = new ConcurrentStack<OwRdmDgram>();

        /// <summary>
        /// 检索的缓冲区。
        /// 此缓冲区将借给调用方，应使用 Return 方法返回，以便在后续调用 Rent 方法时重复使用。 
        /// 无法返回租用的缓冲区不是致命错误。 但是，这可能会导致应用程序性能下降，因为池可能需要创建新的缓冲区来替换丢失的缓冲区。
        /// 此方法返回的数组是用零初始化的。
        /// </summary>
        /// <returns></returns>
        public static OwRdmDgram Rent()
        {
            if (_Pool.TryPop(out var result)) return result;
            return new OwRdmDgram();
        }

        /// <summary>
        /// 将数据全部清0并返回池中。
        /// 此缓冲区将借给调用方，应使用 Return 方法返回，以便在后续调用 Rent 方法时重复使用。 
        /// 无法返回租用的缓冲区不是致命错误。 但是，这可能会导致应用程序性能下降，因为池可能需要创建新的缓冲区来替换丢失的缓冲区。
        /// </summary>
        /// <param name="entry"></param>
        public static void Return(OwRdmDgram entry)
        {
            if (entry.Buffer.Length < Mtu) return; //若不符合要求

            for (int i = 0; i < entry.Buffer.Length; i++)
                entry.Buffer[i] = 0;
            _Pool.Push(entry);
        }

        /// <summary>
        /// 将一个大缓冲区的数据拆分为多个小包的数据。
        /// 只负责复制数据区，正确设置其它属性。
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="startIndex">起始偏移，基于0.</param>
        /// <param name="count">总有效数据长度。</param>
        /// <returns>拆分的条目对象列表（维持顺序稳定），如果是空数据则返回集合。</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 为负。</exception>
        /// <exception cref="ArgumentException">缓冲区长度减去 index 小于 count。。</exception>
        /// <exception cref="ArgumentNullException">buffer 为 null。</exception>
        public static List<OwRdmDgram> Create(byte[] buffer, int startIndex, int count)
        {
            var result = new List<OwRdmDgram>();
#pragma warning disable IDE0063 // 使用简单的 "using" 语句
            using (var ms = new MemoryStream(buffer, startIndex, count))
                return Create(ms);
#pragma warning restore IDE0063 // 使用简单的 "using" 语句
        }

        /// <summary>
        /// 将流当前位置到最终的所有数据拆分为多个小包的数据。
        /// 只负责复制数据区，正确设置<see cref="Count"/>。
        /// </summary>
        /// <param name="stream">数据的当前位置到最终的数据将被读取，调用者要负责对象的处置。</param>
        /// <returns>拆分的条目对象列表（维持顺序稳定），如果是空数据则返回集合。</returns>
        /// <exception cref="ArgumentNullException">stream 为 null。</exception>
        public static List<OwRdmDgram> Create(Stream stream)
        {
            int length;
            var result = new List<OwRdmDgram>();
            do
            {
                var entry = Rent(); Debug.Assert(entry != null && entry.Buffer?.Length >= Mtu);
                length = stream.Read(entry.Buffer, 8, Mts);
                if (length == 0)   //若已到达内存流结尾
                {
                    Return(entry);
                    break;
                }
                entry.Count = length + 8;
                result.Add(entry);
            } while (length == Mts); //若可能未到达内存流结尾（排除了恰巧读了一个完整长度到达末尾的情况）
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static byte[] ToArray(IList<OwRdmDgram> entries)
        {
            var count = entries.Sum(c => c.Count - 8);
            var ary = ArrayPool<byte>.Shared.Rent(count);
            MemoryStream ms;
            using (ms = new MemoryStream(ary, true))
            {
                entries.ForEach(c => ms.Write(c.Buffer, 8, c.Count - 8));
            }
            return ms.ToArray();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwRdmDgram()
        {

        }

        /// <summary>
        /// 包的类型。
        /// </summary>
        public OwRdmDgramKind Kind { get => (OwRdmDgramKind)Buffer[0]; set => Buffer[0] = (byte)value; }

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
                var ary = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value & 0x00ff_ffff | ((byte)Kind << 24)));
                Array.Copy(ary, 0, Buffer, Offset, 4);
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
        /// 最后一次发送的世界时间。<see cref="DateTime.UtcNow"/>。空标识未发送。
        /// </summary>
        public DateTime? LastSendDateTime { get; set; }

        /// <summary>
        /// 缓冲区。第一个是标志字节，随后是3字节客户Id,4字节包顺序号。后跟负载数据。
        /// 大小必须是<see cref="Mtu"/>个字节。第一个字节有特殊含义<seealso cref="OwRdmDgramKind"/>。
        /// </summary>
        public byte[] Buffer { get; set; } = new byte[Mtu];

        /// <summary>
        /// 偏移量。保留为0.
        /// </summary>
        public int Offset { get => 0; set => throw new NotImplementedException(); }

        /// <summary>
        /// 小于或等于<see cref="Mtu"/>。总计有多少字节数据。包含头部8字节。
        /// </summary>
        public int Count { get; set; } = Mtu;

        #region IComparable<OwUdpDataEntry> 接口及相关
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(OwRdmDgram other)
        {
            return Seq.CompareTo(other.Seq);
        }

        #endregion IComparable<OwUdpDataEntry> 接口及相关
    }

    public class OwRdmDataReceivedEventArgs
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwRdmDataReceivedEventArgs()
        {

        }

        public OwRdmDataReceivedEventArgs(ArraySegment<byte> data)
        {
            Datas = data.ToArray();
        }

        /// <summary>
        /// 收到的数据。
        /// </summary>
        public byte[] Datas { get; set; }

        /// <summary>
        /// 远程端点网络地址。当前总是 <see cref="IPEndPoint"/> 类。
        /// </summary>
        public EndPoint RemoteEndPoing { get; set; }
    }

    /// <summary>
    /// udp客户端类。为支持Unity使用，仅使用.NET Framework 4.7支持的功能。
    /// 当前版本一个客户端对象仅能和一个Server通讯。
    /// </summary>
    public class OwRdmClient : SocketAsyncWrapper, IDisposable
    {

        public OwRdmClient(string server, short port) : base(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            var ip = Dns.GetHostAddresses(server).First(c => c.AddressFamily == AddressFamily.InterNetwork);
            var endPoint = new IPEndPoint(ip, port);
            _RemoteEndPoing = endPoint;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="remote">远程服务器端点。</param>
        public OwRdmClient(IPEndPoint remote) : base(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            _RemoteEndPoing = remote;
        }

        /// <summary>
        /// 连接的服务器端地址。
        /// </summary>
        IPEndPoint _RemoteEndPoing;

        /// <summary>
        /// 接受数据的缓存队列。键是包序号 ，值数据条目。需要锁定使用。
        /// </summary>
        ConcurrentDictionary<uint, OwRdmDgram> _RecvData = new ConcurrentDictionary<uint, OwRdmDgram>();

        /// <summary>
        /// 通讯Id，仅低24位有用。若为null，标识尚未连接。
        /// </summary>
        int? _Id;

        /// <summary>
        /// 当前收到的最大连续包号。
        /// </summary>
        uint? _AckSeq;

        /// <summary>
        /// <see cref="_RecvData"/>中最小包号。
        /// </summary>
        uint _MinSeq;

        /// <summary>
        /// <see cref="_RecvData"/>中最大包号。
        /// </summary>
        uint _MaxSeq;

        /// <summary>
        /// 请求结束任务的标记。
        /// </summary>
        CancellationTokenSource _Stopping;

        /// <summary>
        /// 后台线程已经完成操作。
        /// </summary>
        CancellationTokenSource _Stopped;

        #region 方法

        #region 静态方法

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

        #endregion 静态方法

        /// <summary>
        /// 自有初始化。
        /// </summary>
        /// <remarks>此时 <see cref="Socket"/> 应已经正确初始化。</remarks>
        virtual protected void ConnectCore()
        {
            var entry = OwRdmDgram.Rent();
            try
            {
                entry.Kind = (OwRdmDgramKind.CommandDgram | OwRdmDgramKind.StartDgram | OwRdmDgramKind.EndDgram);
                entry.Id = 0;
                entry.Count = 0;
                SendToAsync(entry);

            }
            finally
            {
                OwRdmDgram.Return(entry);
            }
        }

        /// <summary>
        /// 立即同步发送一个数据条目，并做相应处理。
        /// <see cref="OwRdmDgram.Kind"/> 和 <see cref="OwRdmDgram"/>中的数据要实现设定好。
        /// 本函数仅设置 <see cref="OwRdmDgram.Id"/> 和 <see cref="OwRdmDgram.Seq"/> 。
        /// </summary>
        /// <param name="entry"></param>
        public virtual void SendToAsync(OwRdmDgram entry)
        {
            entry.Id = _Id.GetValueOrDefault();
            entry.Seq = (int)_AckSeq.GetValueOrDefault();
            SendToAsync(entry.Buffer, _RemoteEndPoing, entry.Buffer);
        }

        /// <summary>
        /// 数据到达的回调函数。
        /// </summary>
        /// <param name="e"></param>
        protected override void ProcessReceiveFrom(SocketAsyncEventArgs e)
        {
            var entry = new OwRdmDgram
            {
            };
            Buffer.BlockCopy(e.Buffer, e.Offset, entry.Buffer, entry.Offset, e.BytesTransferred);
            base.ProcessReceiveFrom(e);
            if (entry.Kind.HasFlag(OwRdmDgramKind.CommandDgram))  //若是一个命令帧
            {
                Debug.Assert(entry.Kind.HasFlag(OwRdmDgramKind.StartDgram) && entry.Kind.HasFlag(OwRdmDgramKind.EndDgram));   //仅能处理单帧命令
                OnCommandPkg(entry);
            }
            else
                lock (_RecvData)
                {
                    if (entry.Seq <= _AckSeq || !_RecvData.TryAdd((uint)entry.Seq, entry)) //避免重复到达的包
                        Debug.WriteLine("检测到重复到达的包，Seq = " + entry.Seq);
                    else //若增加成功
                    {
                        if (_RecvData.Count == 1)   //若仅此一项
                            _MinSeq = _MaxSeq = (uint)entry.Seq;
                        else
                        {
                            _MinSeq = Math.Min(_MinSeq, (uint)entry.Seq);
                            _MaxSeq = Math.Max(_MaxSeq, (uint)entry.Seq);
                        }
                    }
                }
            //扫描接受队列
            ScanQueue();
        }

        /// <summary>
        /// 扫描接收队列，处理完整包——引发数据到达的事件。
        /// </summary>
        internal void ScanQueue()
        {
            lock (_RecvData)
            {
                for (var list = GetDram(); list.Count > 0; list = GetDram())    //获取完整包
                {
                    var totalCount = list.Sum(c => c.Count - 8);  //计算总长度
                    var buff = OwRdmDgram.ToArray(list);
                    var e = new OwRdmDataReceivedEventArgs
                    {
                        Datas = buff,
                        RemoteEndPoing = _RemoteEndPoing,
                    };
                    list.ForEach(c => OwRdmDgram.Return(c));    //回收对象
                    OnOwUdpDataReceived(e);
                }
            }
        }

        /// <summary>
        /// 获取序号最低的完整包，若没有则返回空集合。
        /// 调用者要锁定<see cref="_RecvData"/>对象才能调用此函数。
        /// </summary>
        /// <returns>如果没有完整数据则返回空集合。</returns>
        private List<OwRdmDgram> GetDram()
        {
            Debug.Assert(Monitor.IsEntered(_RecvData));

            List<OwRdmDgram> result = new List<OwRdmDgram>();
            if (_RecvData.IsEmpty) return result;

            if (!_RecvData.TryGetValue(_MinSeq, out var firstEntry))
                firstEntry = null;

            if (firstEntry.Kind.HasFlag(OwRdmDgramKind.StartDgram))  //若有起始包标志
            {
                if (firstEntry.Kind.HasFlag(OwRdmDgramKind.EndDgram))   //若是单一包
                {
                    result.Add(firstEntry);
                    if (_RecvData.TryRemove(_MinSeq, out _))    //若移除成功，容错
                    {
                        _MaxSeq = _RecvData.Max(c => c.Key);
                        _MinSeq = _RecvData.Min(c => c.Key);
                        _AckSeq = (uint)firstEntry.Seq;
                    }
                }
                else //非单一包
                {
                    uint? lastSeq = null; //终止包的序号号
                    for (var i = _MinSeq + 1; i <= _MaxSeq; i++)    //TODO 回绕问题
                    {
                        if (!_RecvData.TryGetValue(i, out var dgram))  //若包不连续
                            break;
                        if (dgram.Kind.HasFlag(OwRdmDgramKind.StartDgram))    //若遇到新的起始包
                            break;
                        if (dgram.Kind.HasFlag(OwRdmDgramKind.EndDgram))   //若找到终止包
                        {
                            lastSeq = i;
                            break;
                        }
                    }
                    if (lastSeq.HasValue)  //若有任何一个完整包
                    {
                        for (var i = _MinSeq; i <= lastSeq.Value; i++)
                        {
                            _RecvData.TryRemove(i, out var entry);
                            result.Add(entry);
                        }
                        _MaxSeq = _RecvData.Max(c => c.Key);
                        _MinSeq = _RecvData.Min(c => c.Key);
                        _AckSeq = lastSeq;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 处理命令包。客户端目前仅能处理初始化回置。
        /// </summary>
        /// <param name="entry"></param>
        internal virtual void OnCommandPkg(OwRdmDgram entry)
        {
            _Id = entry.Id;
            _AckSeq = (uint)entry.Seq;  //设置起始包号。
        }

        /// <summary>
        /// 发送心跳包。
        /// </summary>
        /// <returns>true发送成功，否则为false。</returns>
        public bool Heartbeat()
        {
            if (!_Id.HasValue) return false;    //若尚未初始化成功
            var dgram = OwRdmDgram.Rent();
            dgram.Kind = OwRdmDgramKind.StartDgram | OwRdmDgramKind.EndDgram | OwRdmDgramKind.CommandDgram;
            SendToAsync(dgram);
            return true;
        }

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Init()
        {

        }

        /// <summary>
        /// 测试是否成功初始化。
        /// </summary>
        /// <returns></returns>
        public bool IsInit()
        {
            return _Id.HasValue;
        }

        #endregion 方法

        #region 事件及相关
        /// <summary>
        /// 有数据到达的事件。此事件可能发生在任何线程。
        /// </summary>
        public event EventHandler<OwRdmDataReceivedEventArgs> OwUdpDataReceived;

        /// <summary>
        /// 引发 <see cref="OwUdpDataReceived"/> 事件。
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnOwUdpDataReceived(OwRdmDataReceivedEventArgs e) => OwUdpDataReceived?.Invoke(this, e);
        #endregion 事件及相关

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

