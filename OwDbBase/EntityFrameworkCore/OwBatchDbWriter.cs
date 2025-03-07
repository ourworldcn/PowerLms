using System;
using System.Buffers;
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
    #region 枚举和类定义
    /// <summary>数据库操作类型枚举。</summary>
    public enum DbOperationType
    {
        /// <summary>插入操作。</summary>
        Insert,
        /// <summary>更新操作。</summary>
        Update,
        /// <summary>删除操作。</summary>
        Delete,
        /// <summary>执行SQL语句。</summary>
        ExecuteSql
    }

    /// <summary>数据库操作项类。</summary>
    public class DbOperation
    {
        /// <summary>获取或设置数据库操作类型。</summary>
        public DbOperationType OperationType { get; set; }

        /// <summary>获取或设置操作的实体对象。</summary>
        public object Entity { get; set; }

        /// <summary>获取或设置SQL语句（当OperationType为ExecuteSql时使用）。</summary>
        public string SqlCommand { get; set; }

        /// <summary>获取或设置SQL参数（当OperationType为ExecuteSql时使用）。</summary>
        public object[] SqlParameters { get; set; }
    }

    /// <summary>配置类，封装配置项。</summary>
    public class OwBatchDbWriterOptions : IOptions<OwBatchDbWriterOptions>
    {
        /// <summary>处理间隔，单位为毫秒，默认值为 1000 毫秒（1 秒）。</summary>
        public int ProcessingInterval { get; set; } = 1000;

        /// <summary>触发立即写入的队列长度阈值，默认为500条。</summary>
        public int QueueThresholdForImmediateProcessing { get; set; } = 500;

        /// <summary>单批次处理的最大操作数量，默认为500。</summary>
        public int MaxBatchSize { get; set; } = 500;

        public OwBatchDbWriterOptions Value => this;
    }

    #endregion

    /// <summary>
    /// 通用批量数据库写入器，用于高效处理大量数据库操作，特别适用于日志记录等高频写入场景。
    /// 该类通过队列缓冲和定时批处理机制，将频繁的数据库操作整合后批量执行，从而减少数据库连接开销，提高系统性能。
    /// </summary>
    /// <remarks>
    /// 适合非即时性要求的操作（如日志、统计数据等）。
    /// 不保证数据库操作的即时性，存在延迟（最多为处理间隔时间）。
    /// 在应用程序意外崩溃时，可能丢失尚未处理的队列项。
    /// 对于需要立即反馈的业务操作不建议使用此模式。
    /// </remarks>
    public class OwBatchDbWriter<TContext> : IDisposable where TContext : DbContext
    {
        #region 私有字段
        private readonly BlockingCollection<DbOperation> _Queue;
        private readonly Timer _Timer;
        private readonly IDbContextFactory<TContext> _DbContextFactory;
        private readonly IHostApplicationLifetime _ApplicationLifetime;
        private readonly ILogger<OwBatchDbWriter<TContext>> _Logger;
        private readonly OwBatchDbWriterOptions _Options;
        private bool _Disposed = false;
        private readonly object _ProcessLock = new object();
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数，使用依赖注入传入所有服务。
        /// </summary>
        /// <param name="dbContextFactory">数据库上下文工厂。</param>
        /// <param name="applicationLifetime">应用程序生命周期。</param>
        /// <param name="options">批处理配置选项。</param>
        /// <param name="logger">日志记录器。</param>
        public OwBatchDbWriter(
            IDbContextFactory<TContext> dbContextFactory,
            IHostApplicationLifetime applicationLifetime,
            IOptions<OwBatchDbWriterOptions> options,
            ILogger<OwBatchDbWriter<TContext>> logger)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _Options = options?.Value ?? new OwBatchDbWriterOptions();

            _Queue = new BlockingCollection<DbOperation>();

            // 注册应用程序停止事件
            _ApplicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
            _ApplicationLifetime.ApplicationStopped.Register(OnApplicationStopped);

            // 初始化定时器
            _Timer = new Timer(ProcessQueue, null, _Options.ProcessingInterval, _Options.ProcessingInterval);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 向内部队列添加数据库操作项。
        /// </summary>
        /// <param name="operation">数据库操作项。</param>
        /// <param name="immediateFlush">是否立即刷新写入。当队列中项目较多或特殊操作时可设为true。</param>
        /// <returns>是否成功添加到队列。</returns>
        /// <exception cref="ArgumentNullException">当operation参数为null时抛出。</exception>
        public bool AddItem(DbOperation operation, bool immediateFlush = false)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (!_Queue.IsAddingCompleted)
            {
                _Queue.Add(operation);
                int queueCount = _Queue.Count;
                _Logger.LogDebug("已将操作添加到队列。当前队列长度：{QueueCount}。", queueCount);

                // 根据队列长度或显式要求决定是否立即刷新
                if (immediateFlush || queueCount >= _Options.QueueThresholdForImmediateProcessing)
                {
                    _Logger.LogDebug("触发立即写入，队列长度：{QueueCount}。", queueCount);
                    ThreadPool.QueueUserWorkItem(_ => FlushNow());
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// 向内部队列添加SQL执行操作。
        /// </summary>
        /// <param name="sqlCommand">要执行的SQL命令。</param>
        /// <param name="immediateFlush">是否立即刷新写入。对于重要操作可设为true。</param>
        /// <param name="parameters">SQL命令的参数。</param>
        /// <returns>是否成功添加到队列。</returns>
        /// <exception cref="ArgumentException">当sqlCommand为null或空字符串时抛出。</exception>
        public bool AddSqlCommand(string sqlCommand, bool immediateFlush = false, params object[] parameters)
        {
            if (string.IsNullOrEmpty(sqlCommand))
                throw new ArgumentException("SQL命令不能为空。", nameof(sqlCommand));

            var operation = new DbOperation
            {
                OperationType = DbOperationType.ExecuteSql,
                SqlCommand = sqlCommand,
                SqlParameters = parameters
            };

            return AddItem(operation, immediateFlush);
        }

        /// <summary>
        /// 强制立即处理队列中的所有项。
        /// </summary>
        /// <returns>处理的操作数量。</returns>
        public int FlushNow()
        {
            _Logger.LogInformation("手动触发队列处理。");
            return ProcessQueueInternal();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 处理队列中的所有项。
        /// </summary>
        /// <param name="state">定时器状态（未使用）。</param>
        private void ProcessQueue(object state)
        {
            if (_Disposed) return;
            ProcessQueueInternal();
        }

        /// <summary>
        /// 内部处理队列方法，供定时器回调和手动刷新调用。
        /// 优化了处理逻辑，确保SQL语句执行前先保存实体操作，保持操作顺序。
        /// </summary>
        /// <returns>处理的操作数量。</returns>
        private int ProcessQueueInternal()
        {
            if (_Disposed) return 0;

            // 防止并发处理
            if (!Monitor.TryEnter(_ProcessLock))
            {
                _Logger.LogDebug("另一个处理操作正在进行中，跳过本次处理。");
                return 0;
            }

            try
            {
                int totalProcessed = 0;
                int currentBatchSize = 0; // 当前批次中的实体数量

                using var dbContext = _DbContextFactory.CreateDbContext();
                bool hasChanges = false;

                // 保存当前上下文中的所有更改
                Action saveCurrentChanges = () =>
                {
                    if (hasChanges)
                    {
                        try
                        {
                            dbContext.SaveChanges();
                            _Logger.LogDebug("已保存实体更改，数量：{Count}。", currentBatchSize);
                            currentBatchSize = 0;
                            hasChanges = false;
                        }
                        catch (Exception ex)
                        {
                            _Logger.LogError(ex, "保存实体更改时出错。");
                            throw; // 重新抛出异常以便外层处理
                        }
                    }
                };

                try
                {
                    while (!_Queue.IsCompleted && _Queue.TryTake(out var operation))
                    {
                        try
                        {
                            // SQL操作需要先保存之前累积的实体操作
                            if (operation.OperationType == DbOperationType.ExecuteSql)
                            {
                                // 先保存之前累积的实体操作
                                saveCurrentChanges();

                                // 执行SQL语句
                                try
                                {
                                    dbContext.Database.ExecuteSqlRaw(operation.SqlCommand, operation.SqlParameters ?? Array.Empty<object>());
                                    _Logger.LogDebug("已执行SQL命令：{SqlCommand}。", operation.SqlCommand);
                                    totalProcessed++;
                                }
                                catch (Exception ex)
                                {
                                    _Logger.LogError(ex, "执行SQL语句时出错。SQL：{SqlCommand}。", operation.SqlCommand);
                                }
                            }
                            else // 实体操作
                            {
                                try
                                {
                                    // 处理实体操作
                                    switch (operation.OperationType)
                                    {
                                        case DbOperationType.Insert:
                                            dbContext.Add(operation.Entity);
                                            break;
                                        case DbOperationType.Update:
                                            dbContext.Update(operation.Entity);
                                            break;
                                        case DbOperationType.Delete:
                                            dbContext.Remove(operation.Entity);
                                            break;
                                        default:
                                            throw new InvalidOperationException("未知的数据库操作类型。");
                                    }

                                    hasChanges = true;
                                    currentBatchSize++;
                                    totalProcessed++;

                                    // 达到批处理阈值时保存
                                    if (currentBatchSize >= _Options.MaxBatchSize)
                                    {
                                        saveCurrentChanges();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _Logger.LogError(ex, "处理实体操作时出错。操作类型：{OperationType}，实体类型：{EntityType}。",
                                        operation.OperationType, operation.Entity?.GetType().Name ?? "未知");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _Logger.LogError(ex, "处理队列项时发生未预期的错误。");
                        }
                    }

                    // 处理完队列后保存剩余的更改
                    if (hasChanges)
                    {
                        saveCurrentChanges();
                    }
                }
                catch (Exception ex)
                {
                    _Logger.LogError(ex, "队列处理过程中发生严重错误。");
                }

                if (totalProcessed > 0)
                {
                    _Logger.LogInformation("队列处理完成，共处理 {Count} 个操作。", totalProcessed);
                }

                return totalProcessed;
            }
            finally
            {
                Monitor.Exit(_ProcessLock);
            }
        }

        /// <summary>
        /// 应用程序停止时的处理。
        /// </summary>
        private void OnApplicationStopping()
        {
            _Logger.LogInformation("应用程序正在停止，准备处理剩余队列项。");
        }

        /// <summary>
        /// 应用程序停止后的处理。
        /// </summary>
        private void OnApplicationStopped()
        {
            _Queue.CompleteAdding();
            ProcessQueueInternal(); // 处理剩余的项
            _Logger.LogInformation("应用程序已停止，已处理所有剩余队列项。");
        }
        #endregion

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
                    ProcessQueueInternal(); // 处理剩余的项
                    _Queue.Dispose();
                }
            }

            _Disposed = true;
        }
        #endregion
    }

    /// <summary>批量写入器扩展方法。</summary>
    public static class BatchDbWriterExtensions
    {
        /// <summary>
        /// 添加批量写入数据库服务到依赖注入容器。
        /// </summary>
        /// <typeparam name="TContext">数据库上下文类型。</typeparam>
        /// <param name="services">服务集合。</param>
        /// <returns>更新后的服务集合，支持方法链调用。</returns>
        public static IServiceCollection AddOwBatchDbWriter<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddSingleton<OwBatchDbWriter<TContext>>();
        }
    }

}

