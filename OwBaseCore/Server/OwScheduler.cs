using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Server
{
    /// <summary>任务状态信息类，提供监控指标</summary>
    public class OwTaskSchedulerStatus
    {
        /// <summary>当前队列长度</summary>
        public int QueueLength { get; set; }
        /// <summary>任务执行成功率</summary>
        public double SuccessRate { get; set; }
        /// <summary>任务平均执行时间</summary>
        public TimeSpan AverageExecutionTime { get; set; }
        /// <summary>已执行任务总数</summary>
        public int TotalTasksExecuted { get; set; }
        /// <summary>成功执行任务数</summary>
        public int SuccessfulTasksExecuted { get; set; }
        /// <summary>总执行时间(毫秒)</summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>更新状态的成功率和平均执行时间</summary>
        public void UpdateStats()
        {
            SuccessRate = TotalTasksExecuted > 0 ? (double)SuccessfulTasksExecuted / TotalTasksExecuted : 0;
            AverageExecutionTime = SuccessfulTasksExecuted > 0 ?
                TimeSpan.FromMilliseconds(TotalExecutionTimeMs / SuccessfulTasksExecuted) : TimeSpan.Zero;
        }
    }

    /// <summary>任务项的详细配置类</summary>
    public class OwSchedulerEntry
    {
        /// <summary>任务的类型ID</summary>
        public string TypeId { get; set; }
        /// <summary>任务执行函数</summary>
        public Func<object, bool> TaskCallback { get; set; }
        /// <summary>传递给执行函数的参数</summary>
        public object State { get; set; }
    }

    /// <summary>任务调度器配置选项</summary>
    public class OwSchedulerOptions : IOptions<OwSchedulerOptions>
    {
        /// <summary>重试间隔，默认1秒</summary>
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);
        /// <summary>最大重试次数，默认3次</summary>
        public int MaxRetryCount { get; set; } = 3;
        /// <summary>获取选项值</summary>
        public OwSchedulerOptions Value => this;
    }

    /// <summary>单线程任务调度器，按添加顺序执行任务。相同TypeId的任务只保留最新的一个。</summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class OwScheduler : IDisposable
    {
        private readonly ILogger<OwScheduler> _logger; // 日志记录器
        private readonly object _syncLock = new(); // 同步锁
        private readonly OwTaskSchedulerStatus _status = new(); // 调度器状态信息
        private readonly LinkedList<OwSchedulerEntry> _taskQueue = new(); // 任务队列
        private readonly Dictionary<string, LinkedListNode<OwSchedulerEntry>> _taskIndex = new(); // 任务索引
        private readonly CancellationTokenSource _cts = new(); // 用于取消处理线程的标记
        private readonly Thread _processingThread; // 处理任务的后台线程
        private bool _disposed; // 是否已释放资源

        /// <summary>初始化任务调度器并启动处理线程</summary>
        public OwScheduler(ILogger<OwScheduler> logger, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            appLifetime.ApplicationStopping.Register(OnApplicationStopping); // 注册应用程序停止时的回调
            _processingThread = new Thread(ProcessTasks) { IsBackground = true, Name = "任务调度器线程" };
            _processingThread.Start();
            _logger.LogInformation("任务调度器已初始化");
        }

        /// <summary>在应用程序停止时处理未完成任务</summary>
        private void OnApplicationStopping()
        {
            _logger.LogInformation("应用程序正在停止，开始处理剩余任务");
            ExecuteRemainingTasks();
        }

        /// <summary>添加任务到调度队列，相同TypeId的任务会被替换</summary>
        public bool Enqueue(string typeId, Func<object, bool> callback, object state = null)
        {
            if (string.IsNullOrEmpty(typeId)) throw new ArgumentNullException(nameof(typeId));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (_disposed) throw new ObjectDisposedException(nameof(OwScheduler));

            var entry = new OwSchedulerEntry { TypeId = typeId, TaskCallback = callback, State = state };

            lock (_syncLock)
            {
                // 移除同ID的现有任务
                if (_taskIndex.TryGetValue(typeId, out var existingNode))
                {
                    _taskQueue.Remove(existingNode);
                    _taskIndex.Remove(typeId);
                }

                // 添加新任务到队列尾部
                var node = _taskQueue.AddLast(entry);
                _taskIndex[typeId] = node;
                _status.QueueLength = _taskQueue.Count; // 更新队列长度
                _logger.LogDebug("已添加任务: {TypeId}", typeId);
                Monitor.Pulse(_syncLock); // 唤醒处理线程
                return true;
            }
        }

        /// <summary>检查任务是否在队列中</summary>
        public bool Contains(string typeId)
        {
            lock (_syncLock)
            {
                return _taskIndex.ContainsKey(typeId);
            }
        }

        /// <summary>获取当前调度器状态</summary>
        public OwTaskSchedulerStatus GetStatus()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OwScheduler));

            lock (_syncLock)
            {
                _status.QueueLength = _taskQueue.Count; // 确保队列长度是最新的
                _status.UpdateStats(); // 更新计算的状态值
                return new OwTaskSchedulerStatus // 返回状态副本
                {
                    QueueLength = _status.QueueLength,
                    SuccessRate = _status.SuccessRate,
                    AverageExecutionTime = _status.AverageExecutionTime,
                    TotalTasksExecuted = _status.TotalTasksExecuted,
                    SuccessfulTasksExecuted = _status.SuccessfulTasksExecuted,
                    TotalExecutionTimeMs = _status.TotalExecutionTimeMs
                };
            }
        }

        /// <summary>执行单个任务并处理结果</summary>
        private void ExecuteTask(OwSchedulerEntry task)
        {
            _logger.LogDebug("执行任务: {TypeId}", task.TypeId);
            var stopwatch = Stopwatch.StartNew();
            bool success = false;

            try
            {
                success = task.TaskCallback(task.State); // 执行任务回调
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务执行异常: {TypeId}", task.TypeId);
                success = false;
            }
            finally
            {
                stopwatch.Stop();
            }

            lock (_syncLock)
            {
                _status.TotalTasksExecuted++; // 更新执行总数

                if (success)
                {
                    _status.SuccessfulTasksExecuted++; // 更新成功数
                    _status.TotalExecutionTimeMs += stopwatch.ElapsedMilliseconds; // 累加执行时间
                    _logger.LogInformation("任务成功: {TypeId}, 耗时: {ElapsedMs}ms", task.TypeId, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError("任务失败: {TypeId}", task.TypeId);
                }

                _taskIndex.Remove(task.TypeId); // 从索引中移除任务
            }
        }

        /// <summary>执行队列中的所有剩余任务</summary>
        private void ExecuteRemainingTasks()
        {
            List<OwSchedulerEntry> remainingTasks;

            lock (_syncLock)
            {
                _logger.LogInformation("开始执行剩余的 {Count} 个任务", _taskQueue.Count);
                remainingTasks = new List<OwSchedulerEntry>(_taskQueue); // 复制任务列表
                _taskQueue.Clear();
                _taskIndex.Clear();
                _status.QueueLength = 0;
            }

            foreach (var task in remainingTasks)
            {
                _logger.LogInformation("执行剩余任务: {TypeId}", task.TypeId);
                ExecuteTask(task);
            }

            _logger.LogInformation("所有剩余任务处理完成");
        }

        /// <summary>任务处理线程的主循环</summary>
        private void ProcessTasks()
        {
            _logger.LogInformation("任务处理线程已启动");

            try
            {
                while (!_cts.IsCancellationRequested) // 循环直到被取消
                {
                    OwSchedulerEntry taskToExecute = null;

                    lock (_syncLock)
                    {
                        if (_taskQueue.Count > 0)
                        {
                            var node = _taskQueue.First; // 获取第一个任务
                            taskToExecute = node.Value;
                            _taskQueue.RemoveFirst(); // 从队列中移除
                            _taskIndex.Remove(taskToExecute.TypeId); // 从索引中移除
                            _status.QueueLength = _taskQueue.Count; // 更新队列长度
                        }
                        else
                        {
                            Monitor.Wait(_syncLock, 200); // 队列为空时等待
                            continue;
                        }
                    }

                    if (taskToExecute != null)
                    {
                        ExecuteTask(taskToExecute); // 执行任务
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务处理线程异常");
            }
            finally
            {
                _logger.LogInformation("任务处理线程已停止");
            }
        }

        /// <summary>释放资源</summary>
        public void Dispose()
        {
            if (_disposed) return; // 避免重复释放

            _logger.LogInformation("正在关闭任务调度器...");
            _cts.Cancel(); // 发送取消信号

            try
            {
                // 等待处理线程结束
                if (_processingThread != null && _processingThread.IsAlive)
                {
                    _processingThread.Join(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "关闭任务调度器时发生异常");
            }

            ExecuteRemainingTasks(); // 处理剩余任务
            _cts.Dispose();
            _disposed = true;
            _logger.LogInformation("任务调度器已关闭");
        }
    }

    /// <summary>任务调度器扩展方法</summary>
    public static class OwSchedulerExtensions
    {
        /// <summary>向服务集合添加任务调度器</summary>
        public static IServiceCollection AddOwScheduler(this IServiceCollection services)
        {
            services.AddSingleton<OwScheduler>();
            return services;
        }
    }
}
