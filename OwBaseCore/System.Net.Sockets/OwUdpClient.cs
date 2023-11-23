using Microsoft.Extensions.Options;
using OW;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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


namespace System.Net.Sockets
{

    public class OwUdpClientOptions : IOptions<OwUdpClientOptions>
    {
        public OwUdpClientOptions()
        {

        }

        public IPEndPoint LocalPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);

        public IPEndPoint RemotePoint { get; set; }

        public CancellationToken RequestStop { get; set; }

        /// <summary>
        /// 每秒发送多少个次数据。不精确，仅是大致的数字。
        /// </summary>
        public int SendPerSeconds { get; set; } = 10;

        public OwUdpClientOptions Value => this;
    }

    public class UdpDataRecivedEventArgs : EventArgs
    {
        public UdpDataRecivedEventArgs(byte[] data, IPEndPoint remotePoint)
        {
            Data = data;
            RemotePoint = remotePoint;
        }

        public byte[] Data { get; set; }

        public IPEndPoint RemotePoint { get; set; }
    }

    public class OwUdpClient : OwDisposableBase
    {
        public OwUdpClient()
        {
            _Options = new OwUdpClientOptions().Value;
            Initialize();
        }

        public OwUdpClient(IOptions<OwUdpClientOptions> options)
        {
            _Options = options.Value;
            Initialize();
        }

        void Initialize()
        {
            _RequestStop = CancellationTokenSource.CreateLinkedTokenSource(_Options.RequestStop);

            _IoWorker = Task.Factory.StartNew(IoWorker, TaskCreationOptions.LongRunning);
        }

        OwUdpClientOptions _Options;

        UdpClient _UdpClient;

        Task _IoWorker;

        /// <summary>
        /// 内部的取消标记。
        /// </summary>
        CancellationTokenSource _RequestStop;

        /// <summary>
        /// 收发复位终止的信号。
        /// </summary>
        CancellationTokenSource _Stopped = new CancellationTokenSource();

        public ConcurrentQueue<(byte[], IPEndPoint)> ReceiveQueue { get; set; } = new ConcurrentQueue<(byte[], IPEndPoint)>();

        public ConcurrentQueue<(byte[], IPEndPoint)> SendQueue { get; set; } = new ConcurrentQueue<(byte[], IPEndPoint)>();

        /// <summary>
        /// 数据到达事件。该事件可能在任何线程中引发。
        /// </summary>
        public event EventHandler<UdpDataRecivedEventArgs> UdpDataRecived;

        protected virtual void OnUdpDataRecived(UdpDataRecivedEventArgs e) => UdpDataRecived?.Invoke(this, e);

        /// <summary>
        /// 对指定端口发送数据。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remotePoint"></param>
        public void Send(byte[] data, IPEndPoint remotePoint)
        {
            SendQueue.Enqueue((data, remotePoint));
        }

        /// <summary>
        /// 用户收发网络数据的工作函数。
        /// </summary>
        void IoWorker()
        {
            TimeSpan delay = TimeSpan.FromMilliseconds(1000f / _Options.SendPerSeconds);    //默认延时
            _UdpClient = new UdpClient(_Options.LocalPoint);
            var ct = _RequestStop.Token;
            while (!_RequestStop.IsCancellationRequested)   //当未要求退出时
            {
                try
                {
                    while (_UdpClient.Available > 0)    //当有数据可以读取时
                    {
                        IPEndPoint listen = new IPEndPoint(_Options.LocalPoint.Address, _Options.LocalPoint.Port);
                        var data = _UdpClient.Receive(ref listen);
                        ReceiveQueue.Enqueue((data, listen));
                    }
                    if (ReceiveQueue.Count > 0) Task.Run(RaiseEvent);   //若需要引发事件
                    while (SendQueue.TryDequeue(out var data))
                    {
                        _UdpClient.Send(data.Item1, data.Item1.Length, data.Item2);
                    }
                }
                catch
                {
                    ResetError();
                    Thread.Yield();
                    continue;   //尽快重试
                }
                ct.WaitHandle.WaitOne(delay);   //等待到期再次轮询
            }
            _Stopped.Cancel();  //发出已终止信号
        }

        /// <summary>
        /// 引发事件。
        /// </summary>
        void RaiseEvent()
        {
        lbStart:
            try
            {
                while (ReceiveQueue.TryDequeue(out var item))
                {
                    OnUdpDataRecived(new UdpDataRecivedEventArgs(item.Item1, item.Item2));
                }
            }
            catch (Exception)
            {
                goto lbStart;
            }
        }

        void ResetError()
        {
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint IOC_UDP_RESET = IOC_IN | IOC_VENDOR | 12;
            _UdpClient.Client.IOControl((int)IOC_UDP_RESET, new byte[] { Convert.ToByte(false) }, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _RequestStop.Cancel();
                _Stopped.Token.WaitHandle.WaitOne(3000);
                _UdpClient?.Dispose();
                _UdpClient = null;
            }
        }
    }

}
