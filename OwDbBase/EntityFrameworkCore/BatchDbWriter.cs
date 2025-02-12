using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OW.EntityFrameworkCore
{
    #region 枚举 DbOperationType
    /// <summary>
    /// 数据库操作类型枚举。
    /// </summary>
    public enum DbOperationType
    {
        Insert,
        Update,
        Delete
    }

    #endregion 枚举 DbOperationType

    #region 类 DbOperation
    /// <summary>
    /// 数据库操作项类。
    /// </summary>
    public class DbOperation
    {
        /// <summary>
        /// 获取或设置数据库操作类型。
        /// </summary>
        public DbOperationType OperationType { get; set; }

        /// <summary>
        /// 获取或设置操作的实体对象。
        /// </summary>
        public object Entity { get; set; }
    }

    #endregion 类 DbOperation

    #region 类 BatchDbWriterOptions
    /// <summary>
    /// 配置类，封装配置项，实现 IOptions&lt;T&gt; 接口。
    /// </summary>
    public class BatchDbWriterOptions
    {
        /// <summary>
        /// 处理间隔，单位为毫秒，默认值为 1000 毫秒（1 秒）。
        /// </summary>
        public int ProcessingInterval { get; set; } = 1000;
    }

    #endregion 类 BatchDbWriterOptions

    #region 类 BatchDbWriter
    /// <summary>
    /// 批量数据库写入器类。
    /// </summary>
    public class BatchDbWriter<TContext> : IDisposable where TContext : DbContext
    {
        #region 私有字段
        private readonly BlockingCollection<DbOperation> _Queue;
        private readonly Timer _Timer;
        private readonly IDbContextFactory<TContext> _DbContextFactory;
        private readonly IHostApplicationLifetime _ApplicationLifetime;
        private readonly ILogger<BatchDbWriter<TContext>> _Logger;
        private readonly BatchDbWriterOptions _Options;
        private bool _Disposed = false;
        #endregion 私有字段

        #region 构造函数
        /// <summary>
        /// 构造函数，使用依赖注入传入所有服务。
        /// </summary>
        public BatchDbWriter(
            IDbContextFactory<TContext> dbContextFactory,
            IHostApplicationLifetime applicationLifetime,
            IOptions<BatchDbWriterOptions> options,
            ILogger<BatchDbWriter<TContext>> logger)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _Options = options?.Value ?? new BatchDbWriterOptions();

            _Queue = new BlockingCollection<DbOperation>();

            // 注册应用程序停止事件
            _ApplicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
            _ApplicationLifetime.ApplicationStopped.Register(OnApplicationStopped);

            // 初始化定时器
            _Timer = new Timer(ProcessQueue, null, _Options.ProcessingInterval, _Options.ProcessingInterval);
        }

        #endregion 构造函数

        #region 公共方法
        /// <summary>
        /// 向内部队列添加数据库操作项。
        /// </summary>
        /// <param name="operation">数据库操作项。</param>
        public void AddItem(DbOperation operation)
        {
            if (!_Queue.IsAddingCompleted)
            {
                _Queue.Add(operation);
                _Logger.LogDebug("已将操作添加到队列。");
            }
        }
        #endregion 公共方法

        #region 私有方法
        /// <summary>
        /// 处理队列中的所有项。
        /// </summary>
        /// <param name="state">定时器状态（未使用）。</param>
        private void ProcessQueue(object state)
        {
            if (_Disposed) return;

            List<DbOperation> operationsToProcess = new List<DbOperation>();

            while (_Queue.TryTake(out var operation))
            {
                operationsToProcess.Add(operation);
            }

            if (operationsToProcess.Count > 0)
            {
                using (var dbContext = _DbContextFactory.CreateDbContext())
                {
                    foreach (var op in operationsToProcess)
                    {
                        try
                        {
                            switch (op.OperationType)
                            {
                                case DbOperationType.Insert:
                                    dbContext.Add(op.Entity);
                                    break;
                                case DbOperationType.Update:
                                    dbContext.Update(op.Entity);
                                    break;
                                case DbOperationType.Delete:
                                    dbContext.Remove(op.Entity);
                                    break;
                                default:
                                    throw new InvalidOperationException("未知的数据库操作类型。");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 记录详细的日志信息，并抛弃失败的操作
                            _Logger.LogError(ex, "处理操作时出错。操作类型：{OperationType}，实体：{Entity}", op.OperationType, op.Entity);
                        }
                    }

                    try
                    {
                        dbContext.SaveChanges();
                        _Logger.LogDebug("已处理 {Count} 个操作。", operationsToProcess.Count);
                    }
                    catch (Exception ex)
                    {
                        _Logger.LogError(ex, "保存更改到数据库时出错。丢弃所有操作。");
                        // 记录详细的日志信息，并抛弃所有操作
                        foreach (var op in operationsToProcess)
                        {
                            _Logger.LogError("操作类型：{OperationType}，实体：{Entity}", op.OperationType, op.Entity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 应用程序停止时的处理。
        /// </summary>
        private void OnApplicationStopping()
        {
            // 标注此方法，以备后续理解
            // 不生成代码
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnApplicationStopped()
        {
            _Queue.CompleteAdding();
            ProcessQueue(null); // 处理剩余的项
        }
        #endregion 私有方法

        #region IDisposable 实现
        /// <summary>
        /// 实现 IDisposable 接口，释放资源。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 标准的 IDisposable 模式实现。
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源。</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                // 释放托管资源
                _Timer?.Dispose();

                if (_Queue != null)
                {
                    _Queue.CompleteAdding();
                    ProcessQueue(null); // 处理剩余的项
                    _Queue.Dispose();
                }
            }

            // 释放非托管资源（如果有）

            _Disposed = true;
        }
        #endregion IDisposable 实现
    }

    #endregion 类 BatchDbWriter

    #region 扩展方法 BatchDbWriterExtensions
    public static class BatchDbWriterExtensions
    {
        /// <summary>
        /// 添加批量写入数据库服务。
        /// </summary>
        /// <typeparam name="TContext">数据库上下文类型。</typeparam>
        /// <param name="services">服务集合。</param>
        /// <returns>服务集合。</returns>
        public static IServiceCollection AddBatchDbWriter<TContext>(this IServiceCollection services) where TContext : DbContext
        {
            services.AddSingleton<BatchDbWriter<TContext>>();
            return services;
        }
    }
    #endregion 扩展方法 BatchDbWriterExtensions
}
