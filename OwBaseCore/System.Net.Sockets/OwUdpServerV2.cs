using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Internet上的标准MTU值为576字节，所以在进行Internet的UDP编程时，最好将UDP的数据长度控件在548字节(576-8-20)以内。
        /// </summary>
        internal const int Mtu = 548;

        /// <summary>
        /// 本类使用的负载长度，去掉1个标志字节，4位序号。
        /// </summary>
        const int Mts = Mtu - 4 - 1;

        BlockingCollection<UdpDataEntry> _WaitSend = new BlockingCollection<UdpDataEntry>();

        ConcurrentQueue<UdpDataEntry> _Sended = new ConcurrentQueue<UdpDataEntry>();

        UdpClient _Udp;

        Task _IoTask;

        /// <summary>
        /// 侦听的实际端口号。
        /// </summary>
        public int ListernPort { get => (_Udp.Client.LocalEndPoint as IPEndPoint).Port; }
        #endregion 属性及相关

        #region 方法

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        internal virtual void SendCore(UdpDataEntry entry)
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
            for (frameCount = 0; frameCount < maxFrameCount && _Udp.Available > 0; frameCount++)
            {
                try
                {
                    buffer = _Udp.Receive(ref remote);
                }
                catch (Exception)
                {
                    frameCount--;
                    continue;
                }
                var s = buffer;
            }

            return frameCount;
        }

        #endregion 方法

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
