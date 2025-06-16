using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 系统内消息表。记录每个用户的消息。
    /// </summary>
    [Comment("系统内消息")]
    [Index(nameof(UserId), nameof(ReadUtc), IsUnique = false)]
    [Index(nameof(UserId), nameof(CreateUtc), IsUnique = false)]
    public class OwMessage : GuidKeyObjectBase
    {
        /// <summary>
        /// 接收用户ID。
        /// </summary>
        [Comment("接收用户ID")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 消息标题。最长64字符。
        /// </summary>
        [Comment("消息标题。最长64字符。")]
        [MaxLength(64)]
        public string Title { get; set; }

        /// <summary>
        /// 消息内容。HTML格式。
        /// </summary>
        [Comment("消息内容。HTML格式")]
        public string Content { get; set; }

        /// <summary>
        /// 创建者ID。发送消息的用户ID。
        /// </summary>
        [Comment("创建者ID")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建时间。精确到毫秒。
        /// </summary>
        [Comment("创建时间")]
        [Precision(3)]
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 读取时间。未读则为null。精确到毫秒。
        /// </summary>
        [Comment("读取时间")]
        [Precision(3)]
        public DateTime? ReadUtc { get; set; }

        /// <summary>
        /// 是否是系统消息。
        /// </summary>
        [Comment("是否是系统消息")]
        public bool IsSystemMessage { get; set; }
    }
}
