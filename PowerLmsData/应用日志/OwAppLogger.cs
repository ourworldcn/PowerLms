using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    [Index(nameof(MerchantId), nameof(CreateUtc), IsUnique = false)]
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
        /*    modelBuilder.Entity<Product>()
        .Property(p => p.TotalValue)
        .HasComputedColumnSql("[Price] * [Quantity]"*/
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Json字符串，存储参数字典。
        /// </summary>
        public string ParamstersJson { get; set; }

        /// <summary>
        /// 该日志条目的创建UTC时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 所属商户Id。
        /// </summary>
        public Guid? MerchantId { get; set; }

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

        /// <summary>
        /// 所属商户Id。空则是系统级别日志
        /// </summary>
        public Guid? MerchantId { get; set; }

    }

    public static class OwStringExtensions
    {
        public static string FormatWith(this string template, IDictionary<string, string> values)
        {
            switch (values.Count)
            {
                case 0:
                    return template;
                case 1:
                    {
                        var kvp = values.First();
                        return template.Replace("{" + kvp.Key + "}", kvp.Value);
                    }
                case > 1:
                    {
                        var sb = new StringBuilder(template);
                        foreach (var kvp in values)
                        {
                            sb.Replace("{" + kvp.Key + "}", kvp.Value);
                        }
                        return sb.ToString();
                    }
                default:
                    return template;
            }
        }
    }
}
