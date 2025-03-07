/*
 * OwAppLogView.cs
 * 版权所有 (c) 2023 PowerLms. 保留所有权利。
 * 此文件包含应用日志视图实体定义。
 * 作者: OW
 * 创建日期: 2023-12-11
 * 修改日期: 2025-03-06
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PowerLms.Data
{
    /// <summary>
    /// 应用日志视图对象，联合OwAppLogStore和OwAppLogItemStore的数据。
    /// </summary>
    [Keyless]
    public class OwAppLogView
    {
        /// <summary>
        /// 应用日志详细信息Id。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 日志类型ID。
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// 日志级别。
        /// Trace (0)：包含最详细消息的日志，可能包含敏感数据，默认禁用，不应在生产环境中启用。
        /// Debug (1)：用于开发过程中的交互式调查日志，包含对调试有用的信息，无长期价值。
        /// Information (2)：跟踪应用程序常规流的日志，具有长期价值。
        /// Warning (3)：突出显示异常或意外事件的日志，不会导致应用程序停止。
        /// Error (4)：当前执行流因故障而停止时的日志，指示当前活动中的故障。
        /// Critical (5)：描述不可恢复的应用程序/系统崩溃或需要立即注意的灾难性故障的日志。
        /// None (6)：不用于写入日志消息，指定日志记录类别不应写入任何消息。。
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// 格式字符串。如"用户{LoginName}登录成功"。
        /// </summary>
        public string FormatString { get; set; }
        
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
        public DateTime CreateUtc { get; set; }
        
        /// <summary>
        /// 所属商户Id。
        /// </summary>
        public Guid? MerchantId { get; set; }
        
        /// <summary>
        /// 操作人登录名。
        /// </summary>
        public string LoginName { get; set; }
        
        /// <summary>
        /// 公司名称。
        /// </summary>
        public string CompanyName { get; set; }
        
        /// <summary>
        /// 操作人显示名称。
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 操作IP地址。
        /// </summary>
        public string OperationIp { get; set; }
        
        /// <summary>
        /// 操作类型。
        /// </summary>
        public string OperationType { get; set; }
        
        /// <summary>
        /// 客户端类型。
        /// </summary>
        public string ClientType { get; set; }

        private Dictionary<string, string> _ParamsDic;

        /// <summary>
        /// 参数字典。
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> ParamsDic
        {
            get
            {
                if (_ParamsDic == null && !string.IsNullOrEmpty(ParamstersJson))
                {
                    _ParamsDic = JsonSerializer.Deserialize<Dictionary<string, string>>(ParamstersJson);
                    _ParamsDic[nameof(CreateUtc)] = CreateUtc.ToString("s");
                }
                return _ParamsDic ?? new Dictionary<string, string>();
            }
        }

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
                    _Message = string.IsNullOrEmpty(FormatString) 
                        ? ParamstersJson 
                        : FormatString.FormatWith(ParamsDic);
                }
                return _Message;
            }
        }
    }
}
