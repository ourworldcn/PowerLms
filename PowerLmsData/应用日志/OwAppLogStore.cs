/*
 * OwAppLogger.cs
 * 版权所有 (c) 2023 PowerLms. 保留所有权利。
 * 此文件包含应用日志相关的数据实体定义及扩展方法。
 * 作者: OW
 * 创建日期: 2023-10-10
 * 修改日期: 2023-12-11
 */
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
namespace PowerLms.Data
{
    #region 日志存储实体
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
        /// 日志级别，
        /// Trace (0)：包含最详细消息的日志，可能包含敏感数据，默认禁用，不应在生产环境中启用。
        /// Debug (1)：用于开发过程中的交互式调查日志，包含对调试有用的信息，无长期价值。
        /// Information (2)：跟踪应用程序常规流的日志，具有长期价值。
        /// Warning (3)：突出显示异常或意外事件的日志，不会导致应用程序停止。
        /// Error (4)：当前执行流因故障而停止时的日志，指示当前活动中的故障。
        /// Critical (5)：描述不可恢复的应用程序/系统崩溃或需要立即注意的灾难性故障的日志。
        /// None (6)：不用于写入日志消息，指定日志记录类别不应写入任何消息。。
        /// </summary>
        [Comment("Trace (0)：包含最详细消息的日志，可能包含敏感数据，默认禁用，不应在生产环境中启用。Debug (1)：用于开发过程中的交互式调查日志，包含对调试有用的信息，无长期价值。Information (2)：跟踪应用程序常规流的日志，具有长期价值。Warning (3)：突出显示异常或意外事件的日志，不会导致应用程序停止。Error (4)：当前执行流因故障而停止时的日志，指示当前活动中的故障。Critical (5)：描述不可恢复的应用程序/系统崩溃或需要立即注意的灾难性故障的日志。None (6)：不用于写入日志消息，指定日志记录类别不应写入任何消息。")]
        public Microsoft.Extensions.Logging.LogLevel LogLevel { get; set; }
        /// <summary>
        /// 格式字符串。如"用户{LoginName}登录成功"。
        /// </summary>
        [Comment("格式字符串。")]
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
        /// 父条目的ID。关联<see cref="OwAppLogStore"/>。
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 该日志条目的创建UTC时间。
        /// </summary>
        public DateTime CreateUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// 商户Id。
        /// </summary>
        public Guid? MerchantId { get; set; }
        /// <summary>
        /// Json字符串，存储参数字典。
        /// </summary>
        public string ParamstersJson { get; set; }
        /// <summary>
        /// 该条目记录的额外的二进制信息。
        /// </summary>
        public byte[] ExtraBytes { get; set; }
    }
    #endregion 日志存储实体
    /// <summary>
    /// 通用信息日志条目。
    /// </summary>
    public class GeneralInfoLogEntry
    {
        public readonly static Guid TypeId = new("{E410BC88-71B2-4530-9993-C0C0B1105617}");
        /// <summary>
        /// 操作类型，例如：登录、登出、创建、更新、删除。
        /// </summary>
        public string OperationType { get; set; }
        /// <summary>
        /// 操作人登录名，例如："admin"。 
        /// </summary>
        public string LoginName { get; set; }
        /// <summary>公司名</summary>
        public string CompanyName { get; set; }
        /// <summary>
        /// 操作人显示名，例如："管理员"。
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// 操作IP地址，例如："172.30.4.30"。
        /// </summary>
        public string OperationIp { get; set; }
        /// <summary>客户端浏览器类型，例如："Chrome"。</summary>
        public string ClientType { get; set; }
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
