using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;


namespace System.Threading
{
    /// <summary>
    /// 任务调度配置选项
    /// </summary>
    public class TaskDispatcherOptions : IOptions<TaskDispatcherOptions>
    {
        /// <summary>
        /// 队列最大容量，默认不限制
        /// </summary>
        public int MaxQueueSize { get; set; }

        /// <summary>
        /// 执行线程检查间隔（毫秒）
        /// </summary>
        public int CheckIntervalMs { get; set; } = 200;

        /// <summary>
        /// 锁定等待超时（毫秒）
        /// </summary>
        /// <value>默认0，不等待</value>
        public int LockTimeoutMs { get; set; } = 0;

        /// <summary>
        /// 日志记录器
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 取消令牌，用于停止任务执行线程
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TaskDispatcherOptions Value => this;
    }

    /// <summary>
    /// 任务调度器，管理和执行一系列任务，支持相同类型任务合并
    /// </summary>
    public class TaskDispatcher : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<object, TaskItem> _taskQueue = new ConcurrentDictionary<object, TaskItem>();
        private readonly Thread _executionThread;
        private readonly TaskDispatcherOptions _options;
        private readonly CancellationToken _cancellationToken;
        private bool _disposed;
        
        /// <summary>
        /// 任务调度器构造函数
        /// </summary>
        /// <param name="options">配置选项，为空则使用默认配置。</param>
        public TaskDispatcher(TaskDispatcherOptions options)
        {
            _options = options ?? new TaskDispatcherOptions();
            _logger = _options.Logger;
            _cancellationToken = _options.CancellationToken;

            // 创建低优先级执行线程
            _executionThread = new Thread(ExecuteLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "TaskDispatcher_ExecutionThread"
            };
            _executionThread.Start();
            
            _logger?.LogInformation("TaskDispatcher 已初始化，最大队列容量: {MaxQueueSize}",
                _options.MaxQueueSize > 0 ? _options.MaxQueueSize.ToString() : "不限制");
        }

        /// <summary>
        /// 提交任务到队列中，相同typeId的任务会被合并
        /// </summary>
        /// <param name="typeId">任务类型标识</param>
        /// <param name="executeFunc">执行任务的函数</param>
        /// <param name="parameter">任务参数</param>
        /// <param name="needLock">是否需要锁定</param>
        /// <returns>是否成功添加到队列</returns>
        public bool Enqueue(object typeId, Func<object, bool> executeFunc, object parameter = null, bool needLock = true)
        {
            if (typeId == null) throw new ArgumentNullException(nameof(typeId));
            if (executeFunc == null) throw new ArgumentNullException(nameof(executeFunc));
            if (_disposed) return false;

            // 检查队列容量限制
            if (_options.MaxQueueSize > 0 && _taskQueue.Count >= _options.MaxQueueSize)
            {
                _logger?.LogWarning("任务队列已达到最大容量 {MaxSize}，无法添加新任务: {TypeId}",
                    _options.MaxQueueSize, typeId);
                return false;
            }
            
            var taskItem = new TaskItem
            {
                TypeId = typeId,
                ExecuteFunc = executeFunc,
                Parameter = parameter,
                NeedLock = needLock,
                EnqueueTime = DateTime.UtcNow
            };

            // 添加或更新任务
            _taskQueue[typeId] = taskItem;

            return true;
        }

        /// <summary>
        /// 手动处理队列中的所有任务，会在9秒内不断重试直到字典为空
        /// </summary>
        /// <returns>是否所有任务都已处理完成</returns>
        public bool ProcessAll()
        {
            if (_disposed) return false;

            // 设置超时时间
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(9);

            // 连续尝试处理任务，直到队列为空或超时
            while (!_taskQueue.IsEmpty && DateTime.UtcNow - startTime < timeout && !_taskQueue.IsEmpty)
            {
                // 获取当前所有任务
                var tasksToProcess = _taskQueue.Keys.ToList();
                // 处理当前批次任务
                foreach (var taskId in tasksToProcess)
                {
                    ProcessTask(taskId);
                }

                // 系统终止时，应尽快完成处理
                if (_taskQueue.IsEmpty) break;
                Thread.Yield();
            }

            bool allTasksProcessed = _taskQueue.IsEmpty;

            if (!allTasksProcessed && DateTime.UtcNow - startTime >= timeout)
            {
                _logger?.LogWarning("任务处理超时，仍有 {RemainingTasks} 个任务未处理", _taskQueue.Count);
            }

            return allTasksProcessed;
        }

        /// <summary>
        /// 获取当前队列中任务数量
        /// </summary>
        public int Count => _taskQueue.Count;

        private void ExecuteLoop()
        {
            _logger?.LogInformation("任务执行线程已启动");

            try
            {
                while (!_cancellationToken.IsCancellationRequested && !_disposed)
                {
                    // 处理队列中的所有任务
                    if (!_taskQueue.IsEmpty)
                    {
                        // 复制当前任务列表以避免迭代时的修改问题
                        var currentTasks = _taskQueue.Keys.ToList();
                        foreach (var taskId in currentTasks)
                        {
                            // 每次处理任务前检查是否要停止
                            if (_cancellationToken.IsCancellationRequested || _disposed)
                                break;

                            ProcessTask(taskId);
                        }
                    }

                    // 快速响应取消
                    try
                    {
                        // 等待间隔时间或取消信号
                        WaitHandle.WaitAny(
                            new[] { _cancellationToken.WaitHandle },
                            _options.CheckIntervalMs);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }

                // 应用停止后尝试处理剩余任务
                ProcessAll();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "任务执行线程发生异常");
            }

            _logger?.LogInformation("任务执行线程已停止");
        }

        private void ProcessTask(object taskId)
        {
            // 直接尝试从队列中移除任务
            if (!_taskQueue.TryRemove(taskId, out var taskItem))
                return;

            try
            {
                // 如果需要锁定，则尝试获取锁
                var isLock = taskItem.NeedLock;
                if (isLock)
                {
                    if (!SingletonLocker.TryEnter(taskItem.TypeId, TimeSpan.FromMilliseconds(_options.LockTimeoutMs)))
                    {
                        // 锁定失败，将任务重新加入队列，但如果队列中已存在相同任务则不再添加
                        _taskQueue.TryAdd(taskItem.TypeId, taskItem);
                        return;
                    }
                }

                try
                {
                    // 执行任务，使用任务参数而不是类型ID
                    bool result = taskItem.ExecuteFunc(taskItem.Parameter ?? taskItem.TypeId);
                    if (!result)
                    {
                        _logger?.LogWarning("任务执行返回失败: {TypeId}", taskItem.TypeId);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "任务执行时发生异常: {TypeId}", taskItem.TypeId);
                }
                finally
                {
                    // 释放锁
                    if (isLock)
                    {
                        SingletonLocker.Exit(taskItem.TypeId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理任务过程中发生异常: {TypeId}", taskItem.TypeId);
                // 重新将任务添加到队列
                _taskQueue.TryAdd(taskItem.TypeId, taskItem);
            }
        }

        /// <summary>
        /// 检查指定类型的任务是否在队列中
        /// </summary>
        /// <param name="typeId">任务类型ID</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        public bool Contains(object typeId)
        {
            return _taskQueue.ContainsKey(typeId);
        }

        /// <summary>
        /// 确保指定类型任务在队列中完成或不存在
        /// </summary>
        /// <param name="typeId">任务类型ID</param>
        /// <param name="timeout">等待超时时间</param>
        /// <returns>如果任务完成或不存在返回true，超时返回false</returns>
        public bool EnsureCompleteIdempotent(object typeId, TimeSpan timeout)
        {
            if (typeId == null) throw new ArgumentNullException(nameof(typeId));

            if (!Contains(typeId))
                return true;

            DateTime startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                if (!Contains(typeId))
                    return true;

                Thread.Sleep(10);
            }

            return !Contains(typeId);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            if (_executionThread.IsAlive && !_executionThread.Join(10000))
            {
                _logger?.LogWarning("执行线程未能在超时时间内结束");
            }

            _logger?.LogInformation("TaskDispatcher 已释放资源");
        }

        /// <summary>
        /// 尝试添加幂等任务，如果相同任务已在队列中则什么也不做
        /// </summary>
        /// <param name="typeId">任务类型标识</param>
        /// <param name="executeFunc">执行任务的函数</param>
        /// <param name="parameter">任务参数</param>
        /// <param name="needLock">是否需要锁定</param>
        /// <returns>如果成功添加或任务已存在返回true，其他情况返回false</returns>
        public bool TryAddIdempotent(object typeId, Func<object, bool> executeFunc, object parameter = null, bool needLock = true)
        {
            if (_taskQueue.ContainsKey(typeId))
                return true;

            return Enqueue(typeId, executeFunc, parameter, needLock);
        }

        /// <summary>
        /// 表示队列中的任务项
        /// </summary>
        private class TaskItem
        {
            /// <summary>任务类型标识</summary>
            public object TypeId { get; set; }

            /// <summary>执行函数</summary>
            public Func<object, bool> ExecuteFunc { get; set; }

            /// <summary>任务参数</summary>
            public object Parameter { get; set; }

            /// <summary>是否需要锁定</summary>
            public bool NeedLock { get; set; }

            /// <summary>入队时间</summary>
            public DateTime EnqueueTime { get; set; }
        }
    }

}
