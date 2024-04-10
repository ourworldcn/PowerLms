using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW;
using OW.DDD;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public class OwRdmServerOptions : IOptions<OwRdmServerOptions>
    {
        public OwRdmServerOptions Value => this;

        /// <summary>
        /// 侦听地址。
        /// </summary>
        /// <value>默认侦听虚四段表示法中的 0.0.0.0。</value>
        public string ListernAddress { get; set; } = "0.0.0.0";

        /// <summary>
        /// 侦听端口。
        /// </summary>
        /// <value>默认端口0，即自动选定。</value>
        public short ListernPort { get; set; }
    }

    /// <summary>
    /// 远程客户端信息类
    /// </summary>
    public class OwRdmRemoteEntry
    {
        public OwRdmRemoteEntry()
        {
        }

        /// <summary>
        /// 远端的唯一标识。客户端是可能因为路由不同而在服务器端看来端点地址不同的。目前该版本仅低24位有效。大约支持400万客户端总数，未来可能考虑回收使用。
        /// </summary>
        public int Id { get; set; }

        #region 发送相关

        /// <summary>
        /// 已发送的数据。按收到的包号升序排序。
        /// 暂存这里等待确认到达后删除。
        /// </summary>
        public LinkedList<OwRdmDgram> SendedData { get; set; } = new LinkedList<OwRdmDgram>();

        /// <summary>
        /// 远程终结点。
        /// </summary>
        public volatile IPEndPoint RemoteEndPoint;

        /// <summary>
        /// 最后一次接到客户端发来数据的时间。
        /// </summary>
        public DateTime LastReceivedUtc { get; set; }

        /// <summary>
        /// 包序号，记录了已用的最大序号，可能需要回绕。
        /// </summary>
        public uint MaxSeq;

        /// <summary>
        /// 客户端确认收到的最大连续包的序号。
        /// </summary>
        public uint RemoteMaxReceivedSeq;
        #endregion 发送相关
    }

    /// <summary>
    /// 支持无连接、面向消息、以可靠方式发送的消息，并保留数据中的消息边界,底层使用Udp来实现。
    /// RDM（以可靠方式发送的消息）消息会依次到达，不会重复。 此外，如果消息丢失，将会通知发送方。 
    /// 如果使用 Rdm 初始化 Socket，则在发送和接收数据之前无需建立远程主机连接。 利用 Rdm，您可以与多个对方主机进行通信。
    /// </summary>
    public class OwRdmServer : OwDisposableBase, IDisposable
    {

        public OwRdmServer(IOptions<OwRdmServerOptions> options, ILogger<OwRdmServer> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _Options = options;
            _Logger = logger;
            _HostApplicationLifetime = hostApplicationLifetime;
            //初始化
            Initialize();
        }

        void Initialize()
        {
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _Socket.Bind(new IPEndPoint(IPAddress.Parse(_Options.Value.ListernAddress), _Options.Value.ListernPort));

            var dgram = OwRdmDgram.Rent();
            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs
            {
                UserToken = dgram, //试图回收此数据帧
                RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0),
            };
            eventArgs.SetBuffer(dgram.Buffer, dgram.Offset, dgram.Count);
            eventArgs.Completed += IO_Completed;
            _Socket.ReceiveMessageFromAsync(eventArgs);
        }

        #region 属性及相关

        /// <summary>
        /// 存储配置信息的字段。
        /// </summary>
        IOptions<OwRdmServerOptions> _Options;

        /// <summary>
        /// 存储日志接口字段。
        /// </summary>
        ILogger<OwRdmServer> _Logger;

        /// <summary>
        /// 允许通知使用者应用程序生存期事件。
        /// </summary>
        IHostApplicationLifetime _HostApplicationLifetime;
        ObjectPool<OwRdmDgram> _EntryPool = ObjectPool.Create(new DefaultPooledObjectPolicy<OwRdmDgram>());

        /// <summary>
        /// 每个客户端的信息。
        /// </summary>
        ConcurrentDictionary<int, OwRdmRemoteEntry> _Id2ClientEntry = new ConcurrentDictionary<int, OwRdmRemoteEntry>();

        /// <summary>
        /// 使用Socket对象。
        /// </summary>
        Socket _Socket;

        /// <summary>
        /// 侦听的端点。
        /// </summary>
        public EndPoint ListernEndPoing { get => _Socket.LocalEndPoint; }
        #endregion 属性及相关

        #region 方法

        public void Send(byte[] buffer, int startIndex, int count, int id)
        {
            Task.Run(() =>
            {
                var list = OwRdmDgram.Create(buffer, startIndex, count);
                while (!_Id2ClientEntry.ContainsKey(id))    //若尚未建立连接
                    Thread.Sleep(10);
                using var dw = GetOrAddEntry(id, out var clientEntry, Timeout.InfiniteTimeSpan);
                if (dw.IsEmpty) throw new InvalidOperationException("异常的锁定错误");  //若锁定超时，容错
                if (list.Count > 0)
                {
                    list[0].Kind |= OwRdmDgramKind.StartDgram;
                    list[^1].Kind |= OwRdmDgramKind.EndDgram;
                    foreach (var entry in list)
                    {
                        entry.Id = id;
                        entry.Seq = (int)Interlocked.Increment(ref clientEntry.MaxSeq);
                        SendCore(entry);
                    }
                }
            });
        }

        /// <summary>
        /// 发送一个数据帧。
        /// </summary>
        /// <param name="dgram">必须设置有效属性包括 Id 和 Seq。成功调用本函数后，此对象被接管，调用者不可用。
        /// 应尽可能使用<see cref="OwRdmDgram.Rent"/>获取，本类会在不使用后用 <see cref="OwRdmDgram.Return(OwRdmDgram)"/> 放入池中。 </param>
        internal protected virtual void SendCore(OwRdmDgram dgram)
        {
            var now = DateTime.UtcNow;
            using var dw = GetOrAddEntry(dgram.Id, out var clientEntry, Timeout.InfiniteTimeSpan);
            if (dw.IsEmpty) throw new InvalidOperationException("异常的锁定错误");  //若锁定超时，容错

            if (clientEntry.RemoteEndPoint is null)  //若未得到对方的端点地址，此时必然是尚未初始化
            {
                //存入已发送队列，后期补发,不存在此情况，因为调用者无法获知Id。
                throw new NotImplementedException();
            }
            else //若已有端点地址
            {
                //立即发送
                SocketAsyncEventArgs e = new SocketAsyncEventArgs
                {
                    UserToken = dgram, //试图回收此数据帧
                    RemoteEndPoint = clientEntry.RemoteEndPoint,  //不怕错，后续可以重发
                };
                e.Completed += IO_Completed;
                e.SetBuffer(dgram.Buffer, dgram.Offset, dgram.Count);
                var willRaiseEvent = _Socket.SendToAsync(e);    //如果 I/O 操作挂起，则为 true。 操作完成时，将引发 e 参数的 Completed 事件。
                //如果 I/O 操作同步完成，则为 false。
                //在这种情况下，将不会引发 e 参数的 Completed 事件，并且可能在方法调用返回后立即检查作为参数传递的 e 对象以检索操作的结果。
                if (!willRaiseEvent) ProcessSendTo(e);
            }
        }

        /// <summary>
        /// 在指定链表中增加一项数据，并使之按Seq升序排序。
        /// 仅对大概率递增Seq的情况，有较高效率。
        /// </summary>
        /// <param name="dgram">seq必须预先填充完毕，且不能与已有节点的值重复。</param>
        /// <param name="list"></param>
        /// <returns>添加的节点。</returns>
        private LinkedListNode<OwRdmDgram> AddDgram(OwRdmDgram dgram, LinkedList<OwRdmDgram> list)
        {
            LinkedListNode<OwRdmDgram> result = null;
            LinkedListNode<OwRdmDgram> node;
            for (node = list.Last; node != null; node = node.Previous)
            {
                if (node.Value.Seq < dgram.Seq)    //若找到位置
                {
                    result = list.AddAfter(node, dgram);
                    break;
                }
            }
            if (node is null)   //若没有找到合适位置
                result = list.AddFirst(dgram);
            return result;
        }

        /// <summary>
        /// 获取指定Id的远程端点信息，并锁定返回。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entry"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public DisposeHelper<OwRdmRemoteEntry> GetOrAddEntry(int id, out OwRdmRemoteEntry entry, TimeSpan timeSpan)
        {
            DisposeHelper<OwRdmRemoteEntry> result = DisposeHelper.Empty<OwRdmRemoteEntry>();
            entry = _Id2ClientEntry.GetOrAdd(id, c => new OwRdmRemoteEntry { Id = id });
            if (Monitor.TryEnter(entry, timeSpan))
            {
                if (_Id2ClientEntry.TryGetValue(id, out var entry2) && ReferenceEquals(entry2, entry))  //若成功锁定
                    result = DisposeHelper.Create(c => Monitor.Exit(c), entry);
                else
                    Monitor.Exit(entry);
            }
            return result;
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
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
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.ReceiveFrom:
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
        /// 按包序号排序的比较器。
        /// </summary>
        Comparer<OwRdmDgram> _Comp = Comparer<OwRdmDgram>.Create((l, r) => Comparer<uint>.Default.Compare((uint)l.Seq, (uint)r.Seq));

        /// <summary>
        /// 处理已接受到数据的情况。
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //increment the count of the total bytes receive by the server
                //Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                //Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

                //echo the data received back to the client
                var entry = _EntryPool.Get();
                entry.Buffer = e.Buffer;
                entry.Offset = e.Offset;
                entry.Count = e.BytesTransferred;
                var clientEntry = _Id2ClientEntry.GetOrAdd(entry.Id, c => new OwRdmRemoteEntry() { Id = c });
                lock (clientEntry)
                {
                    var now = OwHelper.WorldNow;
                    //if (clientEntry.LastReciveWorldDateTime < now)
                    //    clientEntry.LastReciveWorldDateTime = OwHelper.WorldNow;
                    //var kind = (OwRdmDgramKind)entry.Buffer[entry.Offset];
                    //if (kind.HasFlag(OwRdmDgramKind.CommandDgram))    //若是命令包
                    //{

                    //}
                    //var list = clientEntry.ReciveData;
                    //var index = list.BinarySearch(entry, _Comp);
                    //if (index < 0)   //若包未到达,忽略重复包
                    //{
                    //    list.Insert(~index, entry);
                    //    var ints = new PriorityQueue<int, int>(1);

                    //}
                }

                //重新侦听
                e.SetBuffer(ArrayPool<byte>.Shared.Rent(OwRdmDgram.Mtu), 0, OwRdmDgram.Mtu);
                var socket = (Socket)e.UserToken;
                bool willRaiseEvent = socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                ProcessError(e);
            }
        }

        /// <summary>
        /// 处理已经发送的情况。
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSendTo(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                var dgram = (OwRdmDgram)e.UserToken;
                // read the next block of data send from the client
                using var dw = GetOrAddEntry(dgram.Id, out var entry, Timeout.InfiniteTimeSpan);
                Trace.Assert(!dw.IsEmpty);
                AddDgram(dgram, entry.SendedData);
            }
            else
            {
                ProcessError(e);
            }
        }

        /// <summary>
        /// 处理错误。
        /// </summary>
        /// <param name="e"></param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                case SocketError.Success:
                case SocketError.Shutdown:
                case SocketError.ConnectionAborted:
                    break;
                case SocketError.SocketError:
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
                    break;
            }
        }
        #endregion 方法

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

