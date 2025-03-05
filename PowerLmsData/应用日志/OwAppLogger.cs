using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 应用日志源条目。
    /// </summary>
    public class OwAppLogStore : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwAppLogStore()
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
    public class OwAppLogItemStore : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwAppLogItemStore()
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="parentId">父条目的ID。</param>
        public OwAppLogItemStore(Guid parentId)
        {
            ParentId = parentId;
        }

        /// <summary>
        /// 父条目的ID。
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Json字符串，存储参数字典。
        /// </summary>
        public string ParamstersJson { get; set; }

        /// <summary>
        /// 该条目记录的额外的二进制信息。
        /// </summary>
        public byte[] ExtraBytes { get; set; }

        /// <summary>
        /// 该日志条目的创建UTC时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 所属商户Id。
        /// </summary>
        public Guid? MerchantId { get; set; }
    }

    /// <summary>
    /// 应用日志视图对象。
    /// </summary>
    public class OwAppLogVO : GuidKeyObjectBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwAppLogVO()
        {

        }

        /// <summary>
        /// 日志条目的类型Id。
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// 格式字符串。
        /// </summary>
        public string FormatString { get; set; }

        private string _Message;

        /// <summary>
        /// 日志条目的字符串信息。
        /// </summary>
        [NotMapped]
        public string Message
        {
            get
            {
                if (_Message is null)
                {
                    _Message ??= (string.IsNullOrEmpty(FormatString) ? ParamstersJson : FormatString.FormatWith(ParamsDic));
                }
                return _Message;
            }
        }

        /// <summary>
        /// Json字符串，存储参数字典。
        /// </summary>
        public string ParamstersJson { get; set; }

        private Dictionary<string, string> _ParamsDic;

        /// <summary>
        /// 参数字典。
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> ParamsDic
        {
            get
            {
                if (_ParamsDic == null)
                {
                    _ParamsDic = JsonSerializer.Deserialize<Dictionary<string, string>>(ParamstersJson);
                    _ParamsDic[nameof(CreateUtc)] = CreateUtc.ToString("s");
                }
                return _ParamsDic;
            }
        }

        /// <summary>
        /// 该条目记录的额外的二进制信息。
        /// </summary>
        public byte[] ExtraBytes { get; set; }

        /// <summary>
        /// 该日志条目的创建UTC时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 操作IP地址。
        /// </summary>
        public string OperationIp { get; set; }

        /// <summary>
        /// 操作人ID。
        /// </summary>
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 所属商户Id。空则是系统级别日志
        /// </summary>
        public Guid? MerchantId { get; set; }
    }

    /// <summary>
    /// 提供字符串格式化扩展方法的静态类。
    /// </summary>
    public static class OwStringExtensions
    {
        /// <summary>
        /// 使用指定的键值对集合格式化字符串模板。
        /// </summary>
        /// <param name="template">要格式化的字符串模板。</param>
        /// <param name="values">包含键值对的字典，用于替换模板中的占位符。</param>
        /// <returns>格式化后的字符串。</returns>
        /// <example>
        /// <code>
        /// var template = "Hello, {Name}! Today is {Day}.";
        /// var values = new Dictionary&lt;string, string&gt;
        /// {
        ///     { "Name", "Alice" },
        ///     { "Day", "Monday" }
        /// };
        /// var result = template.FormatWith(values);
        /// // result: "Hello, Alice! Today is Monday."
        /// </code>
        /// </example>
        public static string FormatWith(this string template, IDictionary<string, string> values)
        {
            // 根据字典中键值对的数量进行不同的处理
            switch (values.Count)
            {
                case 0:
                    // 如果字典为空，直接返回模板字符串
                    return template;
                case 1:
                    {
                        // 如果字典中只有一个键值对，直接替换模板中的占位符
                        var kvp = values.First();
                        return template.Replace("{" + kvp.Key + "}", kvp.Value);
                    }
                case > 1:
                    {
                        // 如果字典中有多个键值对，使用StringBuilder进行替换
                        var sb = new StringBuilder(template);
                        foreach (var kvp in values)
                        {
                            // 替换模板中的每个占位符
                            sb.Replace("{" + kvp.Key + "}", kvp.Value);
                        }
                        return sb.ToString();
                    }
                default:
                    // 默认情况下，直接返回模板字符串
                    return template;
            }
        }
    }

}
