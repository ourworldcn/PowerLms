using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 应用日志源条目。
    /// </summary>
    public class OwAppLoggerStore : GuidKeyObjectBase
    {
        public OwAppLoggerStore()
        {

        }

        /// <summary>
        /// 格式字符串。
        /// </summary>
        public string FormatString { get; set; }
    }

    /// <summary>
    /// 应用日志详细信息。
    /// </summary>
    [Index(nameof(ParentId), IsUnique = false)]
    [Index(nameof(CreateUtc), IsUnique = false)]
    public class OwAppLoggerItemStore : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwAppLoggerItemStore()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="parentId"></param>
        public OwAppLoggerItemStore(Guid parentId)
        {
            ParentId = parentId;
        }

        public Guid? ParentId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ParamstersJson { get; set; }

        /// <summary>
        /// 该日志条目的创建UTC时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

    }

    public class OwAppLoggerVO : GuidKeyObjectBase
    {
        /// <summary>
        /// 日志条目的类型Id。
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// 日志条目的字符串信息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 该条目记录的额外的二进制信息。
        /// </summary>
        public byte ExtraBytes { get; set; }

        /// <summary>
        /// 该日志条目的创建UTC时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;
    }
}
