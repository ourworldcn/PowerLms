/*
文件名称: OwSqlAppLogger.Triggers2.cs
作者: OW
创建日期: 2025年2月5日
修改日期: 2025年2月8日
描述: 此文件包含 JobTrigger2 类及其扩展方法。JobTrigger2 类实现了 IAfterDbContextSaving 接口，在工作任务被修改或删除后触发相应的处理逻辑。
*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.Text.Json;
using PowerLmsServer.Managers;
using OW.EntityFrameworkCore;

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
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFeeTemplate>))]
    public class JobTrigger2 :
        IAfterDbContextSaving<PlJob>,
        IAfterDbContextSaving<PlIaDoc>,
        IAfterDbContextSaving<PlIsDoc>,
        IAfterDbContextSaving<PlEsDoc>,
        IAfterDbContextSaving<PlEaDoc>,
        IAfterDbContextSaving<DocFee>,
        IAfterDbContextSaving<DocFeeTemplate>
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
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (states.TryGetValue(JobTriggerConstants.ChangedJobIdsKey, out var jobIdObj) && jobIdObj is HashSet<JobChange> jobChanges)
            {
                var logger = serviceProvider.GetService<OwSqlAppLogger>();
                logger.LogJobChangedOrRemoved(serviceProvider, dbContext, jobChanges);
            }
        }
        #endregion IAfterDbContextSaving 接口实现
    }

    /// <summary>
    /// 提供 OwSqlAppLogger 的扩展方法，用于记录工作任务的变化。
    /// </summary>
    public static class OwSqlAppLoggerExtensions
    {
        /// <summary>
        /// 记录每个变化的 Job 的详细信息。
        /// </summary>
        /// <param name="logger">应用日志服务实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="jobChanges">变化的工作任务详细信息集合。</param>
        public static void LogJobChangedOrRemoved(this OwSqlAppLogger logger, IServiceProvider serviceProvider, DbContext dbContext, HashSet<JobChange> jobChanges)
        {
            // 遍历所有变化的工作任务详细信息，并记录日志项
            foreach (var jobChange in jobChanges)
            {
                // 创建新的日志项
                logger.LogGeneralInfo($"{jobChange.Action}工作号（Id={jobChange.EntityId}）");
            }
        }
    }
}

