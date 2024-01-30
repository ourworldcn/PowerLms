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

            _IoTask = Task.Factory.StartNew(IoWorker, TaskCreationOptions.LongRunning);
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


        BlockingCollection<OwUdpDataEntry> _WaitSend = new BlockingCollection<OwUdpDataEntry>();

        ConcurrentQueue<OwUdpDataEntry> _Sended = new ConcurrentQueue<OwUdpDataEntry>();

        Socket _Socket;
        Task _IoTask;

        /// <summary>
        /// 记录标识-远程终结点的对应关系。
        /// </summary>
        ConcurrentDictionary<Guid, IPEndPoint> _Id2Remote = new ConcurrentDictionary<Guid, IPEndPoint>();

        /// <summary>
        /// 记录远程终结点-标识的对应关系。
        /// </summary>
        ConcurrentDictionary<IPEndPoint, Guid> _Remote2Id = new ConcurrentDictionary<IPEndPoint, Guid>();

        /// <summary>
        /// 每个客户端的信息。
        /// </summary>
        ConcurrentDictionary<int, OwUdpClientEntry> _Id2ClientEntry = new ConcurrentDictionary<int, OwUdpClientEntry>();

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
            _WaitSend.Add(entry);
        }

        /// <summary>
        /// IO工作函数。
        /// </summary>
        protected void IoWorker()
        {
            #region 接收数据
            #endregion 接收数据
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxFrameCount">最多处理多少帧。</param>
        /// <returns>实际处理的帧数。</returns>
        int Receive([Range(0, int.MaxValue)] int maxFrameCount = int.MaxValue)
        {
            Trace.Assert(maxFrameCount > 0);
            int frameCount;
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer;


            return 0;
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
        Comparer<OwUdpDataEntry> _Comp = Comparer<OwUdpDataEntry>.Create((l, r) => Comparer<int>.Default.Compare(l.Seq, r.Seq));

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
                var clientEntry = _Id2ClientEntry.GetOrAdd(entry.Id, c => new OwUdpClientEntry() { Id = c });
                lock (clientEntry)
                {
                    clientEntry.LastReciveWorldDateTime = OwHelper.WorldNow;
                    var list = clientEntry.ReciveData;
                    var index = list.BinarySearch(entry, _Comp);
                    if (index < 0)   //若包未到达
                        list.Insert(~index, entry);
                }

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
