using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using OW;
using OW.DDD;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public class OwRdmServerOptions : IOptions<OwRdmServerOptions>
    {
        public virtual OwRdmServerOptions Value => this;

        /// <summary>
        /// 侦听地址。
        /// 指定使用的本地终结点Ip,通常不用设置。
        /// </summary>
        /// <value>默认侦听虚四段表示法中的 0.0.0.0。</value>
        public string ListernAddress { get; set; } = "0.0.0.0";

        /// <summary>
        /// 使用的本机侦听端口。应通过配置指定端口，避免防火墙拒绝侦听请求。
        /// </summary>
        /// <value>默认值：0,自动选择。</value>
        public int ListernPort { get; set; }
    }

    /// <summary>
    /// 远程客户端信息类
    /// </summary>
    public class OwRdmRemoteEntry
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
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
        public OrderedQueue<OwRdmDgram> SendedData { get; set; } = new OrderedQueue<OwRdmDgram>();

        /// <summary>
        /// 远程终结点。
        /// </summary>
        public volatile EndPoint RemoteEndPoint;

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
    /// 支持无连接、面向消息、以可靠方式发送的消息，并保留数据中的消息边界。 
    /// RDM（以可靠方式发送的消息）消息会依次到达，不会重复。 此外，如果消息丢失，将会通知发送方。底层使用Udp来实现。
    /// 如果使用 Rdm 初始化 Socket，则在发送和接收数据之前无需建立远程主机连接。 利用 Rdm，您可以与多个对方主机进行通信。
    /// </summary>
    /// <remarks></remarks>
    public class OwRdmServer : SocketAsyncWrapper, IDisposable
    {

        public OwRdmServer(IOptions<OwRdmServerOptions> options, ILogger<OwRdmServer> logger, IHostApplicationLifetime hostApplicationLifetime)
            : base(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            _Options = options;
            _Logger = logger;
            _HostApplicationLifetime = hostApplicationLifetime;
            //初始化
            Initialize();
        }

        void Initialize()
        {
            Socket.Bind(new IPEndPoint(IPAddress.Parse(_Options.Value.ListernAddress), _Options.Value.ListernPort));
            Stopping.Token.Register(() =>
            {
                _Timer?.Dispose();
            });
            for (int i = 0; i < 2; i++) //暂定使用两个侦听，避免漏接
            {
                var dgram = OwRdmDgram.Rent();
                dgram.Count = dgram.Buffer.Length;
                ReceiveFromAsync(new ArraySegment<byte>(dgram.Buffer, dgram.Offset, dgram.Count), RemoteEndPoint, dgram);
            }
            _Timer = new Timer(c =>
            {
                var now = DateTime.UtcNow;
                var timeout = TimeSpan.FromSeconds(2);
                foreach (var key in _Id2ClientEntry.Keys)
                {
                    using var dw = GetOrAddEntry(key, out var entry, TimeSpan.Zero);
                    if (dw.IsEmpty) continue;
                    for (var node = entry.SendedData.First; node is not null; node = node.Next)
                    {
                        var dgram = node.Value.Item1;
                        var now2 = DateTime.UtcNow;
                        if (now2 - dgram.LastSendDateTime > timeout)  //若超时未得到回应
                        {
                            dgram.LastSendDateTime = DateTime.UtcNow;
                            SendToAsync(new ArraySegment<byte>(dgram.Buffer, dgram.Offset, dgram.Count), entry.RemoteEndPoint, null);
                        }
                        else //若找到一个尚未超时的包
                            break;
                    }
                }
            }, null, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));
            _HostApplicationLifetime.ApplicationStopping.Register(() =>
            {
                Stopping.Cancel();
            });
        }

        #region 属性及相关

        private Timer _Timer;

        /// <summary>
        /// 存储配置信息的字段。
        /// </summary>
        IOptions<OwRdmServerOptions> _Options;

        /// <summary>
        /// 存储日志接口字段。
        /// </summary>
        ILogger<OwRdmServer> _Logger;

        /// <summary>
        /// 日志接口。
        /// </summary>
        public ILogger<OwRdmServer> Logger { get => _Logger; }

        /// <summary>
        /// 允许通知使用者应用程序生存期事件。
        /// </summary>
        IHostApplicationLifetime _HostApplicationLifetime;

        /// <summary>
        /// 每个客户端的信息。
        /// </summary>
        ConcurrentDictionary<int, OwRdmRemoteEntry> _Id2ClientEntry = new ConcurrentDictionary<int, OwRdmRemoteEntry>();

        /// <summary>
        /// 已经使用的最大Id值。
        /// </summary>
        int _MaxId;

        /// <summary>
        /// 本地侦听使用的终结点。
        /// </summary>
        /// <value>默认值：new IPEndPoint(IPAddress.Any, 0),可通过配置指定。</value>
        public EndPoint ListernEndPoint { get => Socket?.LocalEndPoint; }

        /// <summary>
        /// 记录客户端名称与Id的映射关系。有隐患。
        /// </summary>
        ConcurrentDictionary<Guid, int> _Token2Id = new ConcurrentDictionary<Guid, int>();

        /// <summary>
        /// 侦听远程的终结点。
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; } = new IPEndPoint(IPAddress.Any, 0);

        #endregion 属性及相关

        #region 方法

        #region 发送及相关

        public void SendTo(byte[] buffer, int startIndex, int count, int id)
        {
            if (!_Id2ClientEntry.ContainsKey(id))    //若尚未建立连接
            {
                SendToAsync(buffer, startIndex, count, id);
                return;
            }
            using var dw = GetOrAddEntry(id, out var entry, TimeSpan.Zero);
            if (dw.IsEmpty) //若未能成功锁定
            {
                SendToAsync(buffer, startIndex, count, id);
                return;
            }
            var list = OwRdmDgram.Split(buffer, startIndex, count);
            if (list.Count > 0)
            {
                list[0].Kind |= OwRdmDgramKind.StartDgram;
                list[^1].Kind |= OwRdmDgramKind.EndDgram;
                foreach (var dgram in list)
                {
                    dgram.Id = id;
                    dgram.Seq = (int)Interlocked.Increment(ref entry.MaxSeq);
                    SendToAsync(new ArraySegment<byte>(dgram.Buffer, dgram.Offset, dgram.Count), entry.RemoteEndPoint, null);
                    dgram.LastSendDateTime = DateTime.UtcNow;
                    entry.SendedData.Insert((uint)dgram.Seq, dgram);
                }
            }
        }

        public async void SendToAsync(byte[] buffer, int startIndex, int count, int id)
        {
            var list = OwRdmDgram.Split(buffer, startIndex, count);
            await Task.Run(() =>
            {
                var now = DateTime.UtcNow;
                while (!_Id2ClientEntry.ContainsKey(id))    //若尚未建立连接
                {
                    if (DateTime.UtcNow - now > TimeSpan.FromMinutes(1))
                    {
                        _Logger.LogWarning("等待Id={Id}的客户端连接超时", id);
                        return;
                    }
                    Thread.Sleep(10);
                }
                using var dw = GetOrAddEntry(id, out var entry, Timeout.InfiniteTimeSpan);
                if (list.Count > 0)
                {
                    list[0].Kind |= OwRdmDgramKind.StartDgram;
                    list[^1].Kind |= OwRdmDgramKind.EndDgram;
                    foreach (var dgram in list)
                    {
                        dgram.Id = id;
                        dgram.Seq = (int)Interlocked.Increment(ref entry.MaxSeq);
                        SendToAsync(new ArraySegment<byte>(dgram.Buffer, dgram.Offset, dgram.Count), entry.RemoteEndPoint, null);
                        dgram.LastSendDateTime = DateTime.UtcNow;
                        entry.SendedData.Insert((uint)dgram.Seq, dgram);
                    }
                }
            });
        }

        #endregion 发送及相关

        /// <summary>
        /// 获取指定Id的远程端点信息，锁定并返回。
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
                {
                    Monitor.Exit(entry);
                    result = DisposeHelper.Empty<OwRdmRemoteEntry>();
                }
            }
            return result;
        }

        protected override void ProcessReceiveFrom(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0) goto goon;
            if (e.UserToken is not OwRdmDgram dgram || !dgram.Kind.HasFlag(OwRdmDgramKind.CommandDgram) ||
                !dgram.Kind.HasFlag(OwRdmDgramKind.StartDgram) || !dgram.Kind.HasFlag(OwRdmDgramKind.EndDgram)) goto goon;   //若非命令帧
            if (dgram.Id == 0)   //若是连接包
            {
                var id = Interlocked.Increment(ref _MaxId);
                using var dw = GetOrAddEntry(id, out var entry, TimeSpan.FromMilliseconds(1));
                if (dw.IsEmpty) goto goon;   //若无法锁定则忽略此连接包
                if (entry.RemoteEndPoint != null) goto goon; //若已被并发初始化或是重复连接包
                entry.RemoteEndPoint = e.RemoteEndPoint;

                var recvDgram = new OwRdmDgram
                {
                    Count = e.BytesTransferred,
                };
                Buffer.BlockCopy(e.Buffer, e.Offset, recvDgram.Buffer, recvDgram.Offset, e.BytesTransferred);
                recvDgram.Id = id;
                try
                {
                    OnRequestConnect(recvDgram, e.RemoteEndPoint);
                }
                catch (Exception excp)
                {
                    _Logger.LogInformation(excp, "收到连接请求包时,{mname}抛出异常", nameof(OnRequestConnect));
                }
                _Logger.LogDebug("收到连接请求,分配Id = {id}", id);

                var sendDgram = OwRdmDgram.Rent();
                sendDgram.Kind = OwRdmDgramKind.CommandDgram | OwRdmDgramKind.StartDgram | OwRdmDgramKind.EndDgram;
                sendDgram.Id = id;
                sendDgram.Seq = 0;
                sendDgram.Count = 8;
                SendToAsync(sendDgram.Buffer, entry.RemoteEndPoint, null);
            }
            else if (_Id2ClientEntry.ContainsKey(dgram.Id)) //若是心跳包
            {
                using var dw = GetOrAddEntry(dgram.Id, out var entry, TimeSpan.FromMilliseconds(1));
                if (dw.IsEmpty) goto goon;   //若无法锁定则忽略此连接包
                if (!entry.RemoteEndPoint.Equals(e.RemoteEndPoint))   //若远端端点改变
                {
                    _Logger.LogDebug("Id = {id} 的客户端 改变了可见端点 {oep} -> {nep}", entry.Id, entry.RemoteEndPoint, e.RemoteEndPoint);
                    entry.RemoteEndPoint = e.RemoteEndPoint;    //重置远程端点
                }
                if ((uint)dgram.Seq <= entry.MaxSeq)    //若客户端确认的包号合法
                {

                    var recvDgram = new OwRdmDgram
                    {
                        Count = e.BytesTransferred,
                    };
                    Buffer.BlockCopy(e.Buffer, e.Offset, recvDgram.Buffer, recvDgram.Offset, e.BytesTransferred);
                    try
                    {
                        OnHeartbeat(recvDgram, e.RemoteEndPoint);
                    }
                    catch (Exception excp)
                    {
                        _Logger.LogInformation(excp, "收到心跳包时，OnHeartbeat 成员抛出异常。");
                    }
                    while (entry.SendedData.First?.Value.Item1.Seq <= (uint)dgram.Seq)   //若客户端确认新的包已经到达
                    {
                        var tmp = entry.SendedData.First.Value.Item1;
                        entry.SendedData.RemoveFirst();
                        OwRdmDgram.Return(tmp);
                    }
                }
            }
        goon:
            base.ProcessReceiveFrom(e);
        }

        /// <summary>
        /// 当请求连接的包到达时。空操作。
        /// </summary>
        /// <param name="datas">负载内的数据。</param>
        /// <param name="remote">远端端点。</param>
        protected virtual void OnRequestConnect(OwRdmDgram datas, EndPoint remote)
        {

        }

        /// <summary>
        /// 当心跳包到达时。
        /// </summary>
        /// <param name="datas">负载内的数据。</param>
        /// <param name="remote">远端端点。</param>
        protected virtual void OnHeartbeat(OwRdmDgram datas, EndPoint remote)
        {
        }
        #endregion 方法

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Stopping.Cancel();
                }
                //将大型字段设置为null以便于释放空间
                base.Dispose(disposing);
            }
        }
    }

    public class TestRdm
    {
        public TestRdm(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
            Initialize();
        }

        IServiceProvider _ServiceProvider;

        OwRdmClient _Client;

        OwRdmServer _Server;

        public void Initialize()
        {
            _Server = _ServiceProvider.GetService<OwRdmServer>();
            _Client = new OwRdmClient();
            var serverOptions = _ServiceProvider.GetService<IOptions<OwRdmServerOptions>>().Value;
            _Client.Start(new IPEndPoint(IPAddress.Parse("192.168.0.104"), serverOptions.ListernPort));
        }

        public void Test()
        {
            var buffer = new byte[2048];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(i % byte.MaxValue + 1);
            _Client.OwUdpDataReceived += _Client_OwUdpDataReceived;
            _Server.SendTo(buffer, 0, buffer.Length, 1);
        }

        private void _Client_OwUdpDataReceived(object sender, OwRdmDataReceivedEventArgs e)
        {
            Debug.WriteLine($"{GetType().Name} 对象得到数据到达事件，有效长度 {e.Datas.Length} 字节");
        }
    }
}

