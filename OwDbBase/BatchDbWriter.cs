using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace OwDbBase
{
    /// <summary>
    /// 数据库操作类型枚举
    /// </summary>
    public enum DbOperationType
    {
        Insert,
        Update,
        Delete
    }

    /// <summary>
    /// 数据库操作项类
    /// </summary>
    public class DbOperation
    {
        public DbOperationType OperationType { get; set; }
        public object Entity { get; set; }
    }

    /// <summary>
    /// 配置类，封装配置项，实现 IOptions<T> 接口
    /// </summary>
    public class BatchDbWriterOptions
    {
        /// <summary>
        /// 处理间隔，单位为毫秒，默认值为 1000 毫秒（1 秒）
        /// </summary>
        public int ProcessingInterval { get; set; } = 1000;
    }

    /// <summary>
    /// 批量数据库写入器类。
    /// </summary>
    public class BatchDbWriter : IDisposable
    {
        #region 私有字段

        private readonly BlockingCollection<DbOperation> _Queue;
        private readonly Timer _Timer;
        private readonly IDbContextFactory<DbContext> _DbContextFactory;
        private readonly IHostApplicationLifetime _ApplicationLifetime;
        private readonly int _ProcessingInterval;
        private bool _Disposed = false;

        #endregion // 私有字段

        #region 构造函数

        /// <summary>
        /// 构造函数，使用依赖注入传入所有服务
        /// </summary>
        public BatchDbWriter(
            IDbContextFactory<DbContext> dbContextFactory,
            IHostApplicationLifetime applicationLifetime,
            IOptions<BatchDbWriterOptions> options)
        {
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            if (options == null || options.Value == null)
                throw new ArgumentNullException(nameof(options));

            _ProcessingInterval = options.Value.ProcessingInterval > 0 ? options.Value.ProcessingInterval : 1000;

            _Queue = new BlockingCollection<DbOperation>();

            // 注册应用程序停止事件
            _ApplicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

            // 初始化定时器
            _Timer = new Timer(ProcessQueue, null, _ProcessingInterval, _ProcessingInterval);
        }

        #endregion // 构造函数

        #region 公共方法

        /// <summary>
        /// 向内部队列添加数据库操作项
        /// </summary>
        /// <param name="operation">数据库操作项</param>
        public void EnqueueItem(DbOperation operation)
        {
            if (!_Queue.IsAddingCompleted)
            {
                _Queue.Add(operation);
            }
        }

        #endregion // 公共方法

        #region 私有方法

        /// <summary>
        /// 处理队列中的所有项
        /// </summary>
        /// <param name="state">定时器状态（未使用）</param>
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

                    dbContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// 应用程序停止时的处理
        /// </summary>
        private void OnApplicationStopping()
        {
            Dispose();
        }

        #endregion // 私有方法

        #region IDisposable 实现

        /// <summary>
        /// 实现 IDisposable 接口，释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 标准的 IDisposable 模式实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
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

        #endregion // IDisposable 实现
    }

}
