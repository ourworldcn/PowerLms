﻿/*
文件名称: OwSqlAppLogger.Triggers2.cs
作者: OW
创建日期: 5 February 2025
修改日期: 8 February 2025
描述: 此文件包含 JobTrigger2 类及其扩展方法。JobTrigger2 类实现了 IAfterDbContextSaving 接口，在工作任务被修改或删除后触发相应的处理逻辑。
*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.Text.Json;
using PowerLmsServer.Managers;
using static System.Formats.Asn1.AsnWriter;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 工作任务修改或删除后的触发器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlJob>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlIaDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlIsDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlEsDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlEaDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFee>))]
    public class JobTrigger2 : IAfterDbContextSaving<PlJob>, IAfterDbContextSaving<PlIaDoc>, IAfterDbContextSaving<PlIsDoc>, IAfterDbContextSaving<PlEsDoc>, IAfterDbContextSaving<PlEaDoc>, IAfterDbContextSaving<DocFee>
    {
        private readonly IServiceProvider _ServiceProvider;

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化服务提供者。
        /// </summary>
        /// <param name="serviceProvider">服务提供者。</param>
        public JobTrigger2(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        #endregion 构造函数

        #region IAfterDbContextSaving 接口实现
        /// <summary>
        /// 实现 IAfterDbContextSaving 接口的方法，记录每个变化的 Job 的 ID。
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (states.TryGetValue(JobTriggerConstants.ChangedJobIdsKey, out var jobIdObj) && jobIdObj is HashSet<Guid> jobIds)
            {
                var logger = serviceProvider.GetService<OwSqlAppLogger>();
                logger.LogJobChangedOrRemoved(serviceProvider, dbContext, jobIds);
            }
        }
        #endregion IAfterDbContextSaving 接口实现
    }
}

public static class OwSqlAppLoggerExtensions
{
    /// <summary>
    /// 记录每个变化的 Job 的 ID。
    /// </summary>
    /// <param name="logger">应用日志服务实例。</param>
    /// <param name="serviceProvider">服务提供者。</param>
    /// <param name="dbContext">当前 DbContext 实例。</param>
    /// <param name="jobIds">变化的工作任务 ID 集合。</param>
    public static void LogJobChangedOrRemoved(this OwSqlAppLogger logger, IServiceProvider serviceProvider, DbContext dbContext, HashSet<Guid> jobIds)
    {
        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        var ipAddress = httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var context = serviceProvider.GetService<OwContext>();

        foreach (var jobId in jobIds)
        {
            var logItem = new OwAppLogItemStore
            {
                ParentId = jobId,
                CreateUtc = DateTime.UtcNow,
                ParamstersJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "JobId", jobId.ToString() },
                    { "IPAddress", ipAddress ?? "Unknown" },
                    {"UserId",context?.User?.Id.ToString() },
                })
            };

            logger.WriteLogItem(logItem);
        }
    }
}
