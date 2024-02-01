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
    public class OwUdpServerV2Options : IOptions<OwUdpServerV2Options>
    {
        public OwUdpServerV2Options Value => this;

        /// <summary>
        /// 侦听地址。
        /// </summary>
        /// <value>默认侦听虚四边形表示法中的 0.0.0.0。</value>
        public string ListernAddress { get; set; } = "0.0.0.0";

        /// <summary>
        /// 侦听端口。
        /// </summary>
        /// <value>默认端口0，即自动选定。</value>
        public short ListernPort { get; set; }
    }

    public class OwUdpServerV2 : OwDisposableBase, IDisposable
    {

        public OwUdpServerV2(IOptions<OwUdpServerV2Options> options, ILogger<OwUdpServerV2> logger, IHostApplicationLifetime hostApplicationLifetime)
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

            _SendTask = Task.Factory.StartNew(SendWorker, TaskCreationOptions.LongRunning);
        }

        #region 属性及相关

        /// <summary>
        /// 存储配置信息的字段。
        /// </summary>
        IOptions<OwUdpServerV2Options> _Options;

        /// <summary>
        /// 存储日志接口字段。
        /// </summary>
        ILogger<OwUdpServerV2> _Logger;

        /// <summary>
        /// 允许通知使用者应用程序生存期事件。
        /// </summary>
        IHostApplicationLifetime _HostApplicationLifetime;
        ObjectPool<OwUdpDataEntry> _EntryPool = ObjectPool.Create(new DefaultPooledObjectPolicy<OwUdpDataEntry>());

        /// <summary>
        /// 等待发送数据的队列。
        /// </summary>
        BlockingCollection<OwUdpDataEntry> _WaitSend = new BlockingCollection<OwUdpDataEntry>();

        Socket _Socket;
        Task _SendTask;

        /// <summary>
        /// 记录远程终结点-标识的对应关系。
        /// </summary>
        ConcurrentDictionary<IPEndPoint, Guid> _Remote2Id = new ConcurrentDictionary<IPEndPoint, Guid>();

        /// <summary>
        /// 每个客户端的信息。
        /// </summary>
        ConcurrentDictionary<int, OwUdpRemoteEntry> _Id2ClientEntry = new ConcurrentDictionary<int, OwUdpRemoteEntry>();

        /// <summary>
        /// 侦听的实际端口号。
        /// </summary>
        public int ListernPort { get => (_Socket.LocalEndPoint as IPEndPoint).Port; }
        #endregion 属性及相关

        #region 方法

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        internal virtual void SendCore(OwUdpDataEntry entry)
        {
            var now = OwHelper.WorldNow;
            var clientEntry = _Id2ClientEntry.GetOrAdd(entry.Id, c => new OwUdpRemoteEntry { Id = c, });
            lock (clientEntry)
            {
                clientEntry.LastReciveWorldDateTime = now;
                var list = clientEntry.ReciveData;
                entry.Seq = Interlocked.Increment(ref clientEntry.MaxSeq);
                var index = list.BinarySearch(entry, _Comp);
                if (index < 0)    //若包不重复
                {
                    list.Insert(~index, entry);
                }
            }
            _WaitSend.Add(entry);
        }

        /// <summary>
        /// 发送数据的工作函数。
        /// </summary>
        protected void SendWorker()
        {
            #region 发送数据
            OwUdpDataEntry item;
            while (true)
            {
                try
                {
                    item = _WaitSend.Take(_HostApplicationLifetime.ApplicationStarted);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                if (_Id2ClientEntry.TryGetValue(item.Id, out var entry))
                    try
                    {
                        lock (entry)
                        {
                            var now = OwHelper.WorldNow;
                            var tranCount = _Socket.SendTo(item.Buffer, item.Offset, item.Count, SocketFlags.None, entry.Remote);
                            if (item.FirstSendDateTime is null) item.FirstSendDateTime = now;
                            item.LastSendDateTime = now;
                        }
                    }
                    catch (Exception)
                    {
                        _WaitSend.TryAdd(item);
                        OwUdpClientV2.ResetError(_Socket);
                    }
            }
            #endregion 发送数据
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
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.SendPackets:
                    break;
                case SocketAsyncOperation.SendTo:
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        /// <summary>
        /// 按包序号排序的比较器。
        /// </summary>
        Comparer<OwUdpDataEntry> _Comp = Comparer<OwUdpDataEntry>.Create((l, r) => Comparer<uint>.Default.Compare((uint)l.Seq, (uint)r.Seq));

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
                var clientEntry = _Id2ClientEntry.GetOrAdd(entry.Id, c => new OwUdpRemoteEntry() { Id = c });
                lock (clientEntry)
                {
                    var now = OwHelper.WorldNow;
                    if (clientEntry.LastReciveWorldDateTime < now)
                        clientEntry.LastReciveWorldDateTime = OwHelper.WorldNow;
                    var kind = (OwUdpDataKind)entry.Buffer[entry.Offset];
                    if (kind.HasFlag(OwUdpDataKind.CommandDgram))    //若是命令包
                    {

                    }
                    var list = clientEntry.ReciveData;
                    var index = list.BinarySearch(entry, _Comp);
                    if (index < 0)   //若包未到达,忽略重复包
                    {
                        list.Insert(~index, entry);
                        var ints = new PriorityQueue<int, int>(1);

                    }
                }

                //重新侦听
                e.SetBuffer(ArrayPool<byte>.Shared.Rent(OwUdpDataEntry.Mtu), 0, OwUdpDataEntry.Mtu);
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
        /// 处理到达的数据。
        /// </summary>
        /// <param name="entry">对特定的客户端到达的数据进行扫描并处理。</param>
        private void ProcessDgram(OwUdpRemoteEntry entry)
        {
            var list = entry.ReciveData;
            for (int i = 0; i < list.Count; i++)
            {
                var dgram = list[i];
                OwUdpDataKind kind = (OwUdpDataKind)dgram.Buffer[dgram.Offset];
                if (kind.HasFlag(OwUdpDataKind.StartDgram | OwUdpDataKind.EndDgram))    //若是独立帧
                {
                }
            }
        }

        /// <summary>
        /// 处理已经发送的情况。
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                Socket socket = (Socket)e.UserToken;
                // read the next block of data send from the client
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
        /// 处理错误。
        /// </summary>
        /// <param name="e"></param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            //throw new NotImplementedException();
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
