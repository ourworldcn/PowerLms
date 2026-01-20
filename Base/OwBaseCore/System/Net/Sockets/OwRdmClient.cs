/*
 * 为保证 NETSTANDARD 2.0 可用，有些设计重复了高版本框架中已有功能。
 */
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*
 * 目标框架	符号	其他符号
（在 .NET 5+ SDK 中可用）	平台符号（仅
在指定特定于 OS 的 TFM 时可用）
.NET Framework	NETFRAMEWORK, NET48, NET472, NET471, NET47, NET462, NET461, NET46, NET452, NET451, NET45, NET40, NET35, NET20	NET48_OR_GREATER、NET472_OR_GREATER、NET471_OR_GREATER、NET47_OR_GREATER、NET462_OR_GREATER、NET461_OR_GREATER、NET46_OR_GREATER、NET452_OR_GREATER、NET451_OR_GREATER、NET45_OR_GREATER、NET40_OR_GREATER、NET35_OR_GREATER、NET20_OR_GREATER	
.NET Standard	NETSTANDARD, NETSTANDARD2_1, NETSTANDARD2_0, NETSTANDARD1_6, NETSTANDARD1_5, NETSTANDARD1_4, NETSTANDARD1_3, NETSTANDARD1_2, NETSTANDARD1_1, NETSTANDARD1_0	NETSTANDARD2_1_OR_GREATER、NETSTANDARD2_0_OR_GREATER、NETSTANDARD1_6_OR_GREATER、NETSTANDARD1_5_OR_GREATER、NETSTANDARD1_4_OR_GREATER、NETSTANDARD1_3_OR_GREATER、NETSTANDARD1_2_OR_GREATER、NETSTANDARD1_1_OR_GREATER、NETSTANDARD1_0_OR_GREATER	
.NET 5 及更高版本（和 .NET Core） NET、NET8_0、NET7_0、NET6_0、NET5_0、NETCOREAPP、NETCOREAPP3_1、NETCOREAPP3_0、NETCOREAPP2_2、NETCOREAPP2_1、NETCOREAPP2_0、NETCOREAPP1_1、NETCOREAPP1_0	NET8_0_OR_GREATER, NET7_0_OR_GREATER, NET6_0_OR_GREATER, NET5_0_OR_GREATER, NETCOREAPP3_1_OR_GREATER, NETCOREAPP3_0_OR_GREATER, NETCOREAPP2_2_OR_GREATER, NETCOREAPP2_1_OR_GREATER, NETCOREAPP2_0_OR_GREATER, NETCOREAPP1_1_OR_GREATER, NETCOREAPP1_0_OR_GREATER	ANDROID、BROWSER、IOS、MACCATALYST、MACOS、TVOS、WINDOWS
[OS][version]（例如，IOS15_1），
[OS][version]_OR_GREATER（例如，IOS15_1_OR_GREATER）
 */
#if !NET47_OR_GREATER && !NETCOREAPP2_1_OR_GREATER && !NET_STANDARD_2_0 && !NETSTANDARD2_0
#error 必须有 .NET Framework 4.7 或 .NET Core 2.1 或 NETSTANDARD 2.0 框架支持
#endif

namespace System.Net.Sockets
{
    /// <summary>
    /// 处理rdm数据帧的辅助类。
    /// </summary>
    public class RdmDgramQueue
    {
        #region 静态成员

        /// <summary>
        /// 池中元素最大数量。
        /// </summary>
        static readonly int MaxCount;

        static ConcurrentStack<LinkedListNode<OwRdmDgram>> _Pool;

        static RdmDgramQueue()
        {
#if NETCOREAPP
            MaxCount = Environment.ProcessorCount * 40;
#else
            MaxCount = 100;
#endif
            Interlocked.CompareExchange(ref _Pool, new ConcurrentStack<LinkedListNode<OwRdmDgram>>(), null);
        }

        /// <summary>
        /// 租用一个节点。
        /// </summary>
        /// <returns></returns>
        public static LinkedListNode<OwRdmDgram> Rent()
        {
            if (_Pool.TryPop(out var result)) return result;
            return new LinkedListNode<OwRdmDgram>(null);
        }

        /// <summary>
        /// 返还节点。
        /// </summary>
        /// <param name="node">只能返还不在链表中的节点。</param>
        /// <returns>true 成功返回池，false由于种种原因未能返回池。</returns>
        public static bool Return(LinkedListNode<OwRdmDgram> node)
        {
            if (_Pool.Count > MaxCount) return false;
            if (node.List != null) return false;
            node.Value = null;
            _Pool.Push(node);
            //Span<byte> span = stackalloc byte[100];
            return true;
        }
        #endregion 静态成员

        /// <summary>
        /// 数据帧的节点集合。内部按帧的序号升序排序。
        /// </summary>
        LinkedList<OwRdmDgram> _List = new LinkedList<OwRdmDgram>();

        /// <summary>
        /// 追加一个数据帧。
        /// </summary>
        /// <param name="dgram"></param>
        /// <returns></returns>
        public LinkedListNode<OwRdmDgram> Add(OwRdmDgram dgram)
        {
            LinkedListNode<OwRdmDgram> result = Rent();
            result.Value = dgram;
            var tmp = Add(result);
            if (!ReferenceEquals(result, tmp))  //若是已存在节点
            {
                Return(result);
                result = tmp;
            }
            return result;
        }

        /// <summary>
        /// 追加一个数据帧。
        /// </summary>
        /// <param name="node"></param>
        /// <returns>成功插入则返回<paramref name="node"/>,如果有重复项则返回已有的节点。</returns>
        public virtual LinkedListNode<OwRdmDgram> Add(LinkedListNode<OwRdmDgram> node)
        {
            var key = node.Value.Seq;
            LinkedListNode<OwRdmDgram> tmp = null;
            for (tmp = _List.Last; tmp != null; tmp = tmp.Previous)
            {
                if (tmp.Value.Seq < key)   //若找到插入点
                {
                    _List.AddAfter(tmp, node);
                    break;
                }
                else if (tmp.Value.Seq == key)    //若找到重复项
                {
                    node = tmp;
                    break;
                }
            }
            if (tmp is null)  //若没有找到——须加入头部
                _List.AddFirst(node);
            return node;
        }

        /// <summary>
        /// 获取连续的数据包。
        /// </summary>
        /// <returns>返回连续的完整包集合。如果没有则返回空集合。</returns>
        public IList<OwRdmDgram> GetDgrams()
        {
            List<OwRdmDgram> result = new List<OwRdmDgram>();
            var node = _List.First;
            if (node is null) return result;
            //处理起始包
            if (!node.Value.Kind.HasFlag(OwRdmDgramKind.StartDgram))   //若不是起始包
                return result;
            var seq = node.Value.Seq;
            result.Add(node.Value);
            //处理后续包
            if (!node.Value.Kind.HasFlag(OwRdmDgramKind.EndDgram))  //若不是完整包
                for (node = node.Next; node != null; node = node.Next, seq++)
                {
                    if (node.Value.Seq != seq + 1)    //若检测到包顺序号不连续
                    {
                        result.Clear();
                        break;
                    }
                    else //若是连续包
                    {
                        result.Add(node.Value);
                        if (node.Value.Kind.HasFlag(OwRdmDgramKind.EndDgram))  //若检测到包已经完整
                            break;
                    }
                }
            //处理终止包
            if (result.Count > 0 && !result[^1].Kind.HasFlag(OwRdmDgramKind.EndDgram))    //若不是完整包
                result.Clear();
            for (int i = 0; i < result.Count; i++)   //移除已获取的包
            {
                var tmp = _List.First;
                _List.RemoveFirst();
                Return(tmp);
            }
            return result;
        }

        /// <summary>
        /// 强制移除小于或等于指定序号的数据帧。
        /// </summary>
        /// <param name="maxSeq">最大序号。</param>
        /// <param name="removed">记录删除的节点数据的集合，如果省略或为null，则不记录。</param>
        public void RemoveWhere(int maxSeq, ICollection<OwRdmDgram> removed = null)
        {
            for (var tmp = _List.First; tmp != null; tmp = _List.First)
            {
                if (tmp.Value.Seq <= maxSeq)    //若需要删除
                {
                    removed?.Add(tmp.Value);
                    _List.Remove(tmp);
                    Return(tmp);
                }
                else break;
            }
        }

        /// <summary>
        /// 强制移除时间过早的帧。
        /// </summary>
        /// <param name="minDatetime">最早的时间。</param>
        /// <param name="removed">记录删除的节点数据的集合，如果省略或为null，则不记录。</param>
        public void RemoveWhere(DateTime minDatetime, ICollection<OwRdmDgram> removed = null)
        {
            for (var tmp = _List.First; tmp != null; tmp = _List.First)
            {
                if (tmp.Value.LastSendDateTime < minDatetime)    //若需要删除
                {
                    removed?.Add(tmp.Value);
                    _List.Remove(tmp);
                    Return(tmp);
                }
                else break;
            }
        }
    }

    /// <summary>
    /// 包装一个socket对象， 提供基于 SocketAsyncEventArgs 异步收发功能的简化。
    /// <see cref="Stopping"/> 的 <see cref="CancellationTokenSource.Cancel()"/> 可以指示本对象进入清理状态。
    /// </summary>
    /// <remarks>如果使用面向连接的协议（如 TCP），则用于 ConnectAsync 与侦听主机连接。 使用 SendAsync 或 ReceiveAsync 异步通信数据。 可以使用 AcceptAsync.. 处理传入的连接请求。
    /// 如果使用无连接协议（如 UDP），则可以用于 SendToAsync 发送数据报和 ReceiveFromAsync接收数据报。
    /// 当前版本仅支持Udp协议。</remarks>
    public class SocketAsyncWrapper : IDisposable
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
        /// <param name="socket">调用此函数后，<paramref name="socket"/>不可再操作，仅能由本对象操作。</param>
        public SocketAsyncWrapper(Socket socket)
        {
            _Socket = socket;
            Initialize();
        }

        void Initialize()
        {
            _Stopping.Token.Register(() =>
            {
                _Socket?.Close(10);
                Stopped.CancelAfter(10);
            });
        }
        #endregion 构造函数

        volatile Socket _Socket;

        /// <summary>
        /// 是否已经被处置。
        /// </summary>
        private bool _Disposed;

        /// <summary>
        /// 是否已经被处置。
        /// </summary>
        public bool Disposed { get => _Disposed; }

        /// <summary>
        /// 使用的<see cref="Socket"/>对象。
        /// </summary>
        public Socket Socket { get => _Socket; }

        CancellationTokenSource _Stopping = new CancellationTokenSource();
        /// <summary>
        /// 通过该标记指示本类停止工作。
        /// </summary>
        public CancellationTokenSource Stopping { get => _Stopping; }

        CancellationTokenSource _Stopped = new CancellationTokenSource();
        /// <summary>
        /// 该标记标志本类已经停止工作。
        /// </summary>
        public CancellationTokenSource Stopped { get => _Stopped; }

        /// <summary>
        /// 是否正在复位错误。
        /// </summary>
        volatile bool _Reseting;
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
            SendToAsync(e);
        }

        /// <summary>
        /// 兼顾同步处理的异步发送。
        /// </summary>
        /// <param name="e"></param>
        protected void SendToAsync(SocketAsyncEventArgs e)
        {
            var b = Socket.SendToAsync(e);//如果 I/O 操作挂起，则为 true。 操作完成时，将引发 e 参数的 Completed 事件。
                                          //如果 I/O 操作同步完成，则为 false。
                                          //在这种情况下，将不会引发 e 参数的 Completed 事件，并且可能在方法调用返回后立即检查作为参数传递的 e 对象以检索操作的结果。
            if (!b)
                if (e.SocketError == SocketError.Success)
                    ProcessSendTo(e);
                else
                    ProcessError(e);
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
            ReceiveFromAsync(e);
        }

        /// <summary>
        /// 兼顾同步完成情况的接收数据。
        /// </summary>
        /// <param name="e"></param>
        protected void ReceiveFromAsync(SocketAsyncEventArgs e)
        {
            var b = Socket.ReceiveFromAsync(e);
            if (!b)
                if (e.SocketError == SocketError.Success)
                    ProcessReceiveFrom(e);
                else
                    ProcessError(e);
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
            ReceiveFromAsync(e);
        }

        /// <summary>
        /// 处理错误。
        /// </summary>
        /// <param name="e">除 <see cref="SocketError.Success"/> 以外的情况会送到这里处理。当前仅考虑udp的情况</param>
        protected virtual void ProcessError(SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                case SocketError.Success:
                    //成功则不会到达这里,如果到达这里说明派生类已经处理了错误，并再次调用了基类的处理函数。
                    break;
                case SocketError.Shutdown:
                case SocketError.ConnectionAborted:
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
                case SocketError.SocketError:
                    if (_Stopping.IsCancellationRequested || _Stopped.IsCancellationRequested) //若已经请求终止
                        break;
                    Trace.WriteLine("Rdm传输发生错误——{err}", e.SocketError.ToString());
                    if (!_Reseting)
                    {
                        _Reseting = true;
                        Task.Factory.StartNew(c =>  //避免处理过于频繁，栈溢出
                        {
                            try
                            {
                                if (_Stopping.IsCancellationRequested || _Stopped.IsCancellationRequested) //若已经请求终止
                                    return;
                                switch (e.LastOperation)
                                {
                                    case SocketAsyncOperation.SendTo:
                                        OwRdmClient.ResetError(Socket);
                                        Task.Run(() => SendToAsync(e));
                                        break;
                                    case SocketAsyncOperation.ReceiveFrom:
                                        OwRdmClient.ResetError(Socket);
                                        Task.Run(() => _Socket.ReceiveFromAsync(e));
                                        break;
                                    default:
                                        OwRdmClient.ResetError(Socket);
                                        break;
                                }
                            }
                            finally
                            {
                                _Reseting = false;
                            }
                        }, e, _Stopping.Token);
                    }
                    break;
            }
        }

        #endregion IO处理操作

        #region IDisposable接口及相关

        /// <summary>
        /// 不能并发调用。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    _Socket?.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _Socket = null;
                _Disposed = true;
            }
        }

        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SocketAsyncWrapper()
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
        #endregion IDisposable接口及相关

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

    /// <summary>
    /// Rdm的数据帧类。
    /// </summary>
    public class OwRdmDgram : IComparable<OwRdmDgram>
    {
        #region 静态成员

        /// <summary>
        /// Internet上的标准MTU值为576字节，所以在进行Internet的UDP编程时，最好将UDP的数据长度控件在548字节(576-8-20)以内。
        /// </summary>
        public const int RdmMtu = 576 - 8 - 20;

        /// <summary>
        /// 本类使用的负载长度，去掉1个标志字节,3个字节通讯Id，4位序号。
        /// </summary>
        public const int RdmMts = RdmMtu - 4/*标志字节 和 通讯Id(防路由端口映射出现变化)*/ - 4/*已接收的连续包的最大序号*/;

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
            if (!_Pool.TryPop(out var result))
                result = new OwRdmDgram();
            result.Count = result.Buffer.Length - result.Offset;
            return result;
        }

        /// <summary>
        /// 将数据全部清0并返回池中。
        /// 此缓冲区将借给调用方，应使用 Return 方法返回，以便在后续调用 Rent 方法时重复使用。 
        /// 无法返回租用的缓冲区不是致命错误。 但是，这可能会导致应用程序性能下降，因为池可能需要创建新的缓冲区来替换丢失的缓冲区。
        /// </summary>
        /// <param name="entry"></param>
        public static bool Return(OwRdmDgram entry)
        {
            if (entry.Buffer.Length < RdmMtu) return false; //若不符合要求

            for (int i = 0; i < entry.Buffer.Length; i++)
                entry.Buffer[i] = 0;
            _Pool.Push(entry);
            return true;
        }

        /// <summary>
        /// 将一个大缓冲区的数据拆分为多个小包的数据。
        /// 只负责复制数据区，调用者正确设置其它属性。
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="startIndex">起始偏移，基于0.</param>
        /// <param name="count">总有效数据长度。</param>
        /// <returns>拆分的条目对象列表（维持顺序稳定），如果是空数据则返回集合。</returns>
        /// <exception cref="ArgumentOutOfRangeException">index 或 count 为负。</exception>
        /// <exception cref="ArgumentException">缓冲区长度减去 index 小于 count。。</exception>
        /// <exception cref="ArgumentNullException">buffer 为 null。</exception>
        public static List<OwRdmDgram> Split(byte[] buffer, int startIndex, int count)
        {
            var result = new List<OwRdmDgram>();
#pragma warning disable IDE0063 // 使用简单的 "using" 语句
            using (var ms = new MemoryStream(buffer, startIndex, count))
                return Split(ms);
#pragma warning restore IDE0063 // 使用简单的 "using" 语句
        }

        /// <summary>
        /// 将流当前位置到最终的所有数据拆分为多个小包的数据。
        /// 只负责复制数据区，正确设置<see cref="Count"/>。
        /// </summary>
        /// <param name="stream">数据的当前位置到最终的数据将被读取，调用者要负责对象的处置。</param>
        /// <returns>拆分的条目对象列表（维持顺序稳定），如果是空数据则返回集合。</returns>
        /// <exception cref="ArgumentNullException">stream 为 null。</exception>
        public static List<OwRdmDgram> Split(Stream stream)
        {
            int length;
            var result = new List<OwRdmDgram>();
            do
            {
                var entry = Rent(); Debug.Assert(entry != null && entry.Buffer?.Length >= RdmMtu);
                length = stream.Read(entry.Buffer, 8, RdmMts);
                if (length == 0)   //若已到达内存流结尾
                {
                    Return(entry);
                    break;
                }
                entry.Count = length + 8;
                result.Add(entry);
            } while (length == RdmMts); //若可能未到达内存流结尾（排除了恰巧读了一个完整长度到达末尾的情况）
            return result;
        }

        /// <summary>
        /// 将一组包的数据负载合成为一个字节数组。
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        public static byte[] ToArray(IList<OwRdmDgram> entries)
        {
            var count = entries.Sum(c => c.Count - 8);
            var ary = ArrayPool<byte>.Shared.Rent(count);
            MemoryStream ms;
            using (ms = new MemoryStream(ary, 0, count, true))
            {
                foreach (var entry in entries)
                    ms.Write(entry.Buffer, entry.Offset + 8, entry.Count - 8);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// 从头部获取连续的分包且必须是一个完整包。
        /// </summary>
        /// <param name="list"></param>
        /// <returns>返回按顺序获取的一个完整包。</returns>
        public static List<OwRdmDgram> RemoveFirstRange(OrderedQueue<OwRdmDgram> list)
        {
            List<OwRdmDgram> result = new List<OwRdmDgram>();
            var node = list.First;
            if (node is null) return result;
            //处理起始包
            if (!node.Value.Item1.Kind.HasFlag(OwRdmDgramKind.StartDgram))   //若不是起始包
                return result;
            var seq = node.Value.Item1.Seq;
            result.Add(node.Value.Item1);
            //处理后续包
            if (!node.Value.Item1.Kind.HasFlag(OwRdmDgramKind.EndDgram))  //若不是完整包
                for (node = node.Next; node != null; node = node.Next, seq++)
                {
                    if (node.Value.Item1.Seq != seq + 1)    //若检测到包顺序号不连续
                    {
                        result.Clear();
                        break;
                    }
                    else //若是连续包
                    {
                        result.Add(node.Value.Item1);
                        if (node.Value.Item1.Kind.HasFlag(OwRdmDgramKind.EndDgram))  //若检测到包已经完整
                            break;
                    }
                }
            //处理终止包
            if (result.Count > 0 && !result[^1].Kind.HasFlag(OwRdmDgramKind.EndDgram))    //若不是完整包
                result.Clear();
            for (int i = 0; i < result.Count; i++) list.RemoveFirst();  //移除已获取的包
            return result;
        }

        #endregion 静态成员

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwRdmDgram()
        {
            Buffer = new byte[RdmMtu];
            Count = RdmMtu;
        }

        /// <summary>
        /// 获取包头。
        /// </summary>
        public ulong Header
        {
            get
            {
                return (ulong)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(Buffer, Offset));
            }
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
        /// 大小必须是<see cref="RdmMtu"/>个字节。第一个字节有特殊含义<seealso cref="OwRdmDgramKind"/>。
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// 偏移量。保留为0.
        /// </summary>
        public int Offset { get => 0; protected set => throw new NotImplementedException(); }

        /// <summary>
        /// 小于或等于<see cref="RdmMtu"/>。总计有多少字节数据。包含头部8字节。
        /// </summary>
        /// <value>默认值:<see cref="RdmMtu"/></value>
        public int Count { get; set; }

        /// <summary>
        /// 有效的负载数据。会根据 Count 和 Offset 属性设置在 Buffer 上的片段。可读写。
        /// </summary>
        public ArraySegment<byte> LoadData { get => new ArraySegment<byte>(Buffer, Offset + 8, Count - 8); }

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

    /// <summary>
    /// 记录收发数据的链表。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OrderedQueue<T> : LinkedList<(T, uint)>
    {
        public OrderedQueue()
        {

        }

        /// <summary>
        /// 加入一个项，并保证节点按key升序排序。
        /// </summary>
        /// <remarks>从末尾进行搜索，故追加效率取决于key是否比较按顺序添加。</remarks>
        /// <param name="key">key若重复则返回已有项。</param>
        /// <param name="item"></param>
        /// <returns>加入的节点。key若重复则返回已有节点。</returns>
        public LinkedListNode<(T, uint)> Insert(uint key, T item)
        {
            LinkedListNode<(T, uint)> result = null;
            for (var node = Last; node != null; node = node.Previous)
            {
                if (node.Value.Item2 < key)
                {
                    result = AddAfter(node, (item, key));
                    break;
                }
                else if (node.Value.Item2 == key)    //若找到重复项
                {
                    result = node;
                    break;
                }
            }
            result ??= AddFirst((item, key));
            return result;
        }

        public void GetNext()
        {

        }
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
    /// 设置 <see cref="LoggerCallback"/> 用于获取日志。
    /// </summary>
    /// <remarks>支持无连接、面向消息、以可靠方式发送的消息，并保留数据中的消息边界。 
    /// RDM（以可靠方式发送的消息）消息会依次到达，不会重复。 此外，如果消息丢失，将会通知发送方。</remarks>
    public class OwRdmClient : SocketAsyncWrapper, IDisposable
    {
        #region 构造函数及相关

        /// <summary>
        /// 
        /// </summary>
        public OwRdmClient() : base(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            Initialize();
        }

        /// <summary>
        /// 构造函数的辅助初始化函数。
        /// </summary>
        private void Initialize()
        {
            Socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            Stopping.Token.Register(() =>
            {
                _Timer?.Dispose();
            });
            _Timer = new Threading.Timer(c =>
            {
                OnTimer();
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
        #endregion 构造函数及相关


        Threading.Timer _Timer;

        /// <summary>
        /// 服务器地址。如果不是null，说明用了dns名指定了服务器地址。
        /// </summary>
        string _Server;

        DateTime _LastDnsUtc;
        short _Port;

        /// <summary>
        /// 连接的服务器端地址。若为null，标识尚未开始工作。
        /// </summary>
        volatile EndPoint _RemoteEndPoing;

        /// <summary>
        /// 接受数据的缓存队列。键是包序号 ，值数据条目。需要锁定使用。
        /// </summary>
        OrderedQueue<OwRdmDgram> _RecvData = new OrderedQueue<OwRdmDgram>();

        /// <summary>
        /// 通讯Id，仅低24位有用。若为null，标识尚未成功连接。
        /// </summary>
        int? _Id;

        /// <summary>
        /// 当前收到的最大连续包号。
        /// </summary>
        uint? _AckSeq;

        /// <summary>
        /// 获取或设置日志使用的回调函数。
        /// </summary>
        public Action<string> LoggerCallback { get; set; }

        string _Name;
        /// <summary>
        /// 设置客户端的名字。在连接包 和 心跳包时，会补上该字符串。
        /// 最长64个字符。
        /// </summary>
        public string Name
        {
            get => _Name;
            set
            {
                if (Encoding.UTF8.GetByteCount(value) > OwRdmDgram.RdmMts) throw new ArgumentException("最长64了字符。", nameof(value));
                _Name = value;
            }
        }

        /// <summary>
        /// 连接服务器时。
        /// </summary>
        public byte[] ConnectLoadData { get; set; }
        #region 方法

        #region 静态成员

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

        #endregion 静态成员

        /// <summary>
        /// 连接到特定的地址端口。
        /// </summary>
        /// <remarks>如果</remarks>
        /// <param name="server">可以是IP地址的四段表示法："xxx.xxx.xxx.xxx"，也可以是DNS名；无论怎样，都会自动定期解析名称。</param>
        /// <param name="port"></param>
        public void Start(string server, short port)
        {
            var ip = Dns.GetHostAddresses(server).First(c => c.AddressFamily == AddressFamily.InterNetwork);
            var endPoint = new IPEndPoint(ip, port);
            _LastDnsUtc = DateTime.UtcNow;
            _Server = server;
            _Port = port;
            Start(endPoint);
        }

        /// <summary>
        /// 要连接的远程端口号。
        /// </summary>
        /// <param name="remote"></param>
        public void Start(EndPoint remote)
        {
            _RemoteEndPoing = remote;
            var dgram = OwRdmDgram.Rent();
            ReceiveFromAsync(new ArraySegment<byte>(dgram.Buffer), _RemoteEndPoing, null);
            SendToConnect();
        }

        /// <summary>
        /// 自动回收已用的rdm数据包对象。
        /// </summary>
        /// <param name="e"></param>
        protected override void ProcessSendTo(SocketAsyncEventArgs e)
        {
            base.ProcessSendTo(e);
            var dgram = e.UserToken as OwRdmDgram;
            if (dgram != null) OwRdmDgram.Return(dgram);
        }

        /// <summary>
        /// 数据到达的回调函数。
        /// </summary>
        /// <param name="e"></param>
        protected override void ProcessReceiveFrom(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > OwRdmDgram.RdmMtu || e.BytesTransferred <= 0) //若无效的包长度则丢弃
            {
                base.ProcessReceiveFrom(e);
                return;
            }
            var entry = new OwRdmDgram
            {
            };
            Buffer.BlockCopy(e.Buffer, e.Offset, entry.Buffer, entry.Offset, e.BytesTransferred);
            entry.Count = e.BytesTransferred;
            base.ProcessReceiveFrom(e); //尽快开始获取数据帧
            if (entry.Kind.HasFlag(OwRdmDgramKind.CommandDgram))  //若是一个命令帧
            {
                if (!(entry.Kind.HasFlag(OwRdmDgramKind.StartDgram) && entry.Kind.HasFlag(OwRdmDgramKind.EndDgram)))  //仅能处理单帧命令
                {
                    //丢弃非法帧
                    Trace.WriteLine($"非法命令帧来自{e.RemoteEndPoint}");
                    return;
                }
                OnCommandDgram(entry);
            }
            else //若是数据帧
                lock (_RecvData)
                {
                    if (_AckSeq.GetValueOrDefault() >= (uint)entry.Seq)    //若是重复包
                        return;
                    //加入队列
                    LoggerCallback?.Invoke($"收到数据帧——Seq = {(uint)entry.Seq}，总负载长度={entry.Count}");
                    _RecvData.Insert((uint)entry.Seq, entry);

                }
            ScanQueue();
        }

        /// <summary>
        /// 扫描接收队列，处理完整包——引发数据到达的事件。会试图锁定队列。
        /// </summary>
        internal void ScanQueue()
        {
            if (!IsConnected()) return;
            if (_AckSeq.GetValueOrDefault() + 1 < (uint)(_RecvData.First?.Value.Item1.Seq ?? 0)) return;   //若没有须处理的第一个包
            else if (_AckSeq.GetValueOrDefault() + 1 > (uint)(_RecvData.First?.Value.Item1.Seq ?? 0))
            {
                //TODO 需要处理重复到达已被处理的最小包的情况
            }
            List<OwRdmDataReceivedEventArgs> eventArgs = new List<OwRdmDataReceivedEventArgs>();
            lock (_RecvData)
            {
                for (var list = OwRdmDgram.RemoveFirstRange(_RecvData); list.Count > 0; list = OwRdmDgram.RemoveFirstRange(_RecvData))    //获取完整包
                {
                    _AckSeq = Math.Max((uint)list[^1].Seq, _AckSeq.GetValueOrDefault());
                    var buff = OwRdmDgram.ToArray(list);
                    LoggerCallback?.Invoke($"确认完整数据包—— Seq =[{string.Join(",", list.Select(c => c))}]，总有效数据长度={buff.Length:0,0}字节");

                    var e = new OwRdmDataReceivedEventArgs
                    {
                        Datas = buff,
                        RemoteEndPoing = _RemoteEndPoing,
                    };
                    list.ForEach(c => OwRdmDgram.Return(c));    //回收对象
                    eventArgs.Add(e);
                }
            }
            eventArgs.ForEach(c => OnOwUdpDataReceived(c));
        }

        /// <summary>
        /// 处理命令包。客户端目前仅能处理初始化回置。
        /// </summary>
        /// <param name="entry"></param>
        internal virtual void OnCommandDgram(OwRdmDgram entry)
        {
            if (!IsConnected())  //避免重复到达的初始化包
            {
                LoggerCallback?.Invoke($"获取到初始化恢复帧——Id: {_Id} -> {entry.Id};AckSeq {_AckSeq} -> {(uint)entry.Seq}");
                _Id = entry.Id;
                _AckSeq = (uint)entry.Seq;  //设置起始包号。
            }
        }

        /// <summary>
        /// 发送心跳包。
        /// </summary>
        /// <returns>true发送成功，否则为false。</returns>
        protected bool SendToHeartbeat()
        {
            if (!IsConnected()) return false;    //若尚未成功连接
            var dgram = OwRdmDgram.Rent();
            if (Name != null)
            {
                var buff = Encoding.UTF8.GetBytes(Name);   //TODO 须评估每次生成 和 一次性生成的优略
                Buffer.BlockCopy(buff, 0, dgram.Buffer, dgram.Offset, buff.Length);
                dgram.Count = buff.Length;
            }
            else
                dgram.Count = 0;
            dgram.Kind = OwRdmDgramKind.StartDgram | OwRdmDgramKind.EndDgram | OwRdmDgramKind.CommandDgram;
            dgram.Id = _Id.Value;
            dgram.Seq = (int)_AckSeq.GetValueOrDefault();
            SendToAsync(new ArraySegment<byte>(dgram.Buffer, dgram.Offset, dgram.Count), _RemoteEndPoing, null);
            return true;
        }

        /// <summary>
        /// 发送试图连接服务器的包。
        /// </summary>
        protected void SendToConnect()
        {
            var dgram = OwRdmDgram.Rent();
            if (Name != null)
            {
                var buff = Encoding.UTF8.GetBytes(Name);   //TODO 须评估每次生成 和 一次性生成的优略
                Buffer.BlockCopy(buff, 0, dgram.LoadData.Array, dgram.LoadData.Offset, buff.Length);
                dgram.Count = buff.Length;
            }
            else
                dgram.Count = 0;

            dgram.Kind = (OwRdmDgramKind.CommandDgram | OwRdmDgramKind.StartDgram | OwRdmDgramKind.EndDgram);
            dgram.Id = 0;
            dgram.Seq = 0;
            dgram.Count += 8;
            SendToAsync(new ArraySegment<byte>(dgram.Buffer, dgram.Offset, dgram.Count), _RemoteEndPoing, null);
        }

        /// <summary>
        /// 测试是否成功连接。
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return _Id.HasValue;
        }

        /// <summary>
        /// 定时触发，用于发送心跳包和试图重连。
        /// </summary>
        private void OnTimer()
        {
            if (!string.IsNullOrWhiteSpace(_Server) && DateTime.UtcNow - _LastDnsUtc > TimeSpan.FromMinutes(1))
            {
                var ip = Dns.GetHostAddresses(_Server).First(c => c.AddressFamily == AddressFamily.InterNetwork);
                var endPoint = new IPEndPoint(ip, _Port);
                if (!endPoint.Equals(_RemoteEndPoing))
                    _RemoteEndPoing = endPoint;
            }
            if (_RemoteEndPoing != null)    //若已经有了服务器地址
            {
                if (IsConnected())   //若已经成功连接
                    SendToHeartbeat();
                else
                    SendToConnect();
            }
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
        protected virtual void OnOwUdpDataReceived(OwRdmDataReceivedEventArgs e)
        {
            try
            {
                OwUdpDataReceived?.Invoke(this, e);
            }
            catch (Exception excp)
            {
                LoggerCallback?.Invoke($"OwRdmDataReceived事件处理程序抛出异常——Seq = {excp.Message}");
            }
        }
        #endregion 事件及相关

        #region IDisposable接口相关

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    Stopping.Cancel(false);
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                _RecvData = null;
            }
            base.Dispose(disposing);
        }

        // // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~OwUdpClientV2()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        #endregion IDisposable接口相关
    }

}

