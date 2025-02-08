/*
文件名称: OwSqlAppLogger.Triggers.cs
作者: ow
创建日期: 2025年2月8日
描述: 这个文件包含一个 JobTrigger 类，它实现了 IDbContextSaving 接口，并在工作任务被修改或删除时触发相应的处理逻辑。
*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.Data;
using PowerLms.Data;
using System;
using System.Collections.Generic;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 包含 JobTrigger 类常量。
    /// </summary>
    public static class JobTriggerConstants
    {
        /// <summary>
        /// 用于标识已更改的 JobId 的键。
        /// </summary>
        public const string ChangedJobIdsKey = "ChangedJobIds";
    }

    /// <summary>
    /// 工作任务修改或删除时的触发器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlJob>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlIaDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlIsDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlEsDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlEaDoc>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFee>))]
    public class JobTrigger : IDbContextSaving<PlJob>, IDbContextSaving<PlIaDoc>, IDbContextSaving<PlIsDoc>, IDbContextSaving<PlEsDoc>, IDbContextSaving<PlEaDoc>, IDbContextSaving<DocFee>, IDisposable
    {
        private readonly ILogger<JobTrigger> _Logger;
        private readonly IHostApplicationLifetime _HostApplicationLifetime;
        private bool _Disposed = false;

        /// <summary>
        /// 构造函数，初始化日志记录器和应用程序生命周期监测器。
        /// </summary>
        /// <param name="logger">用于记录日志的记录器。</param>
        /// <param name="hostApplicationLifetime">用于监测应用程序生命周期的对象。</param>
        public JobTrigger(ILogger<JobTrigger> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _Logger = logger;
            _HostApplicationLifetime = hostApplicationLifetime;
            _HostApplicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
        }

        /// <summary>
        /// 保存 PlJob 实体的方法。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        void IDbContextSaving<PlJob>.Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            EnsureHashSetInStates(states);

            var changedJobIds = (HashSet<Guid>)states[JobTriggerConstants.ChangedJobIdsKey];

            foreach (var entry in entities)
            {
                if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    changedJobIds.Add(((PlJob)entry.Entity).Id);
                    _Logger.LogDebug("PlJob ID {JobId} has been added to changedJobIds.", ((PlJob)entry.Entity).Id);
                }
            }
        }

        /// <summary>
        /// 保存 PlIaDoc 实体的方法。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        void IDbContextSaving<PlIaDoc>.Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            AddJobIdsToStates(entities, states, nameof(PlIaDoc));
        }

        /// <summary>
        /// 保存 PlIsDoc 实体的方法。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        void IDbContextSaving<PlIsDoc>.Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            AddJobIdsToStates(entities, states, nameof(PlIsDoc));
        }

        /// <summary>
        /// 保存 PlEsDoc 实体的方法。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        void IDbContextSaving<PlEsDoc>.Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            AddJobIdsToStates(entities, states, nameof(PlEsDoc));
        }

        /// <summary>
        /// 保存 PlEaDoc 实体的方法。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        void IDbContextSaving<PlEaDoc>.Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            AddJobIdsToStates(entities, states, nameof(PlEaDoc));
        }

        /// <summary>
        /// 保存 DocFee 实体的方法。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        void IDbContextSaving<DocFee>.Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            AddJobIdsToStates(entities, states, nameof(DocFee));
        }

        /// <summary>
        /// 将 JobId 添加到状态字典中。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        /// <param name="entityType">实体类型名称。</param>
        private void AddJobIdsToStates(IEnumerable<EntityEntry> entities, Dictionary<object, object> states, string entityType)
        {
            EnsureHashSetInStates(states);

            var changedJobIds = (HashSet<Guid>)states[JobTriggerConstants.ChangedJobIdsKey];

            foreach (var entry in entities)
            {
                if (entry.State != EntityState.Added && (entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
                {
                    if (entry.Entity is PlJob job)
                    {
                        changedJobIds.Add(job.Id);
                        _Logger.LogDebug("PlJob ID {JobId} has been added to changedJobIds.", job.Id);
                    }
                    else
                    {
                        var jobIdProperty = entry.Entity.GetType().GetProperty("JobId");
                        if (jobIdProperty != null && jobIdProperty.GetValue(entry.Entity) is Guid jobId)
                        {
                            changedJobIds.Add(jobId);
                            _Logger.LogDebug("{EntityType} Job ID {JobId} has been added to changedJobIds.", entityType, jobId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 确保状态字典包含一个 HashSet&lt;Guid&gt;，以存储已更改的 JobId。
        /// </summary>
        /// <param name="states">状态字典。</param>
        private void EnsureHashSetInStates(Dictionary<object, object> states)
        {
            if (!states.ContainsKey(JobTriggerConstants.ChangedJobIdsKey))
            {
                states[JobTriggerConstants.ChangedJobIdsKey] = new HashSet<Guid>();
                _Logger.LogDebug("HashSet&lt;Guid&gt; has been added to states with key {ChangedJobIdsKey}.", JobTriggerConstants.ChangedJobIdsKey);
            }
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        /// <param name="disposing">指示是否释放托管资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // 释放托管资源。
                }
                // 释放非托管资源。
                _Disposed = true;
            }
        }

        /// <summary>
        /// 实现 IDisposable 接口的方法。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 应用程序停止时的处理逻辑。
        /// </summary>
        private void OnApplicationStopping()
        {
            // 在应用程序停止时执行的逻辑。
            _Logger.LogDebug("Application is stopping.");
        }
    }
}
