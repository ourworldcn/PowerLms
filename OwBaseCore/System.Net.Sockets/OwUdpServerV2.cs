using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OW;
using OW.DDD;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public class OwUdpServerV2Options : IOptions<OwUdpServerV2Options>
    {
        public OwUdpServerV2Options Value => this;
    }

    internal class UdpDataEntry
    {
        public UdpDataEntry()
        {
        }

        /// <summary>
        /// 对方的唯一标识。
        /// </summary>
        public string Id { get; set; }

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
        /// </summary>
        public byte[] Buffer { get; set; } = ArrayPool<byte>.Shared.Rent(OwUdpServerV2.Mtu);

        /// <summary>
        /// 小于或等于<see cref="OwUdpServerV2.Mtu"/>。
        /// </summary>
        public int Count { get; set; }
    }

    public class OwUdpServerV2 : OwDisposableBase, IDisposable
    {

        public OwUdpServerV2(IOptions<OwUdpServerV2Options> options, ILogger<OwUdpServerV2> logger)
        {
            _Options = options;
            _Logger = logger;
        }

        /// <summary>
        /// 存储配置信息的字段。
        /// </summary>
        IOptions<OwUdpServerV2Options> _Options;

        /// <summary>
        /// 存储日志接口字段。
        /// </summary>
        ILogger<OwUdpServerV2> _Logger;

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
        /// 
        /// </summary>
        /// <param name="buffer">大小必须是<see cref="Mtu"/>个字节，且前5个字节必须空出。调用此函数后缓冲区不能再进行读写。该缓冲区需要用<see cref="ArrayPool{T}.Shared"/>分配。
        /// 在缓冲区不用后会自动还回池。</param>
        /// <param name="offset">必须是5.</param>
        /// <param name="count">指出有效字节数量且必须小于或等于<see cref="Mts"/>。</param>
        /// <param name="id">发送到对方的唯一标识符。</param>
        virtual protected void SendCore(byte[] buffer, int offset, int count, string id)
        {

        }

        #endregion 方法
    }
}
