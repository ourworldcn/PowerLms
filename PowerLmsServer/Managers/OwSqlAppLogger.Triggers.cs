/*
文件名称: OwSqlAppLogger.Triggers.cs
作者: OW
创建日期: 2025年2月8日
修改日期: 2025年2月9日
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
    /// 用于存储 EntityId、操作类型和实体类型名的类。
    /// </summary>
    public class JobChange
    {
        /// <summary>
        /// 实体的 ID。
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// 操作类型（新增、修改、删除）。
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 实体类型名。
        /// </summary>
        public string EntityType { get; set; }
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
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeTemplate>))]
    public class JobTrigger :
        IDbContextSaving<PlJob>,
        IDbContextSaving<PlIaDoc>,
        IDbContextSaving<PlIsDoc>,
        IDbContextSaving<PlEsDoc>,
        IDbContextSaving<PlEaDoc>,
        IDbContextSaving<DocFee>,
        IDbContextSaving<DocFeeTemplate>,
        IDisposable
    {
        #region 私有字段

        /// <summary>
        /// 日志记录器。
        /// </summary>
        private readonly ILogger<JobTrigger> _Logger;

        /// <summary>
        /// 应用程序生命周期监测器。
        /// </summary>
        private readonly IHostApplicationLifetime _HostApplicationLifetime;

        /// <summary>
        /// 指示对象是否已释放。
        /// </summary>
        private bool _Disposed = false;

        #endregion 私有字段

        #region 构造函数

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

        #endregion 构造函数

        #region Saving方法

        /// <summary>
        /// 处理实体的增删改操作，并将相应的 JobId 添加到状态字典中。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            AddJobIdsToStates(entities, states);
        }

        #endregion Saving方法

        #region 私有方法

        /// <summary>
        /// 将 EntityId 添加到状态字典中。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="states">状态字典。</param>
        private void AddJobIdsToStates(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            EnsureHashSetInStates(states);

            var changedJobIds = (HashSet<JobChange>)states[JobTriggerConstants.ChangedJobIdsKey];

            foreach (var entry in entities)
            {
                var action = entry.State switch
                {
                    EntityState.Added => "新增",
                    EntityState.Modified => "修改",
                    EntityState.Deleted => "删除",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(action))
                {
                    Guid? jobId = entry.Entity switch
                    {
                        PlIaDoc plIaDocEntity when plIaDocEntity.JobId != null => plIaDocEntity.JobId,
                        PlIsDoc plIsDocEntity when plIsDocEntity.JobId != null => plIsDocEntity.JobId,
                        PlEsDoc plEsDocEntity when plEsDocEntity.JobId != null => plEsDocEntity.JobId,
                        PlEaDoc plEaDocEntity when plEaDocEntity.JobId != null => plEaDocEntity.JobId,
                        DocFeeTemplate docFeeTemplateEntity when entry.State == EntityState.Deleted => docFeeTemplateEntity.Id,
                        PlJob plJob => plJob.Id,
                        _ => null
                    };

                    if (jobId.HasValue)
                    {
                        changedJobIds.Add(new JobChange
                        {
                            EntityId = jobId.Value,
                            Action = action,
                            EntityType = entry.Metadata.ClrType.Name
                        });
                        _Logger.LogDebug("{EntityType} Entity ID {EntityId} 已被 {Action}，并添加到 changedJobIds。", entry.Entity.GetType().Name, jobId, action);
                    }
                }
            }
        }

        /// <summary>
        /// 获取实体的 JobId 属性值。
        /// </summary>
        /// <param name="entity">实体对象。</param>
        /// <returns>实体的 JobId 属性值。</returns>
        private Guid? GetEntityJobId(object entity)
        {
            var jobIdProperty = entity.GetType().GetProperty("JobId");
            if (jobIdProperty != null && jobIdProperty.GetValue(entity) is Guid jobId)
            {
                return jobId;
            }
            return null;
        }

        /// <summary>
        /// 确保状态字典包含一个 HashSet&lt;JobChange&gt;，以存储已更改的 EntityId。
        /// </summary>
        /// <param name="states">状态字典。</param>
        private void EnsureHashSetInStates(Dictionary<object, object> states)
        {
            if (!states.ContainsKey(JobTriggerConstants.ChangedJobIdsKey))
            {
                states[JobTriggerConstants.ChangedJobIdsKey] = new HashSet<JobChange>();
                _Logger.LogDebug("HashSet<JobChange> 已被添加到 states 中，键为 {ChangedJobIdsKey}。", JobTriggerConstants.ChangedJobIdsKey);
            }
        }

        /// <summary>
        /// 应用程序停止时的处理逻辑。
        /// </summary>
        private void OnApplicationStopping()
        {
            _Logger.LogDebug("Application is stopping.");
        }

        #endregion 私有方法

        #region Dispose方法

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

        #endregion Dispose方法
    }
}
