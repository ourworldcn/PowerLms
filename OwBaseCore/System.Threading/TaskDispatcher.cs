using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>
    /// 任务调度器，管理和执行一系列任务，支持相同类型任务合并
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class TaskDispatcher : IDisposable
    {
        private readonly ILogger<TaskDispatcher> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ConcurrentDictionary<object, TaskItem> _taskQueue = new ConcurrentDictionary<object, TaskItem>();
        // 用于追踪执行顺序的队列
        private readonly ConcurrentQueue<object> _processingOrder = new ConcurrentQueue<object>();
        // 记录尝试失败的任务及其下次尝试时间
        private readonly ConcurrentDictionary<object, DateTime> _backoffTasks = new ConcurrentDictionary<object, DateTime>();
        private readonly Thread _executionThread;
        private readonly AutoResetEvent _queueSignal = new AutoResetEvent(false);
        private readonly int _maxQueueSize;
        private bool _disposed;
        // 任务回退时间，避免频繁重试
        private readonly TimeSpan _backoffTime = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// 任务调度器构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="applicationLifetime">应用程序生命周期</param>
        /// <param name="maxQueueSize">队列最大容量，默认不限制</param>
        public TaskDispatcher(ILogger<TaskDispatcher> logger, IHostApplicationLifetime applicationLifetime, int maxQueueSize = 0)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            _maxQueueSize = maxQueueSize;

            // 创建低优先级执行线程
            _executionThread = new Thread(ExecuteLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "TaskDispatcher_ExecutionThread"
            };
            _executionThread.Start();

            _logger.LogInformation("TaskDispatcher 已初始化，最大队列容量: {MaxQueueSize}", _maxQueueSize > 0 ? _maxQueueSize.ToString() : "不限制");
        }

        /// <summary>
        /// 提交任务到队列中，相同typeId的任务会被合并
        /// </summary>
        /// <param name="typeId">任务类型标识</param>
        /// <param name="executeFunc">执行任务的函数</param>
        /// <param name="needLock">是否需要锁定</param>
        /// <returns>是否成功添加到队列</returns>
        public bool Enqueue(object typeId, Func<object, bool> executeFunc, bool needLock = true)
        {
            if (typeId == null) throw new ArgumentNullException(nameof(typeId));
            if (executeFunc == null) throw new ArgumentNullException(nameof(executeFunc));
            if (_disposed) return false;
            
            // 检查队列容量限制
            if (_maxQueueSize > 0 && _taskQueue.Count >= _maxQueueSize)
            {
                _logger.LogWarning("任务队列已达到最大容量 {MaxSize}，无法添加新任务: {TypeId}", _maxQueueSize, typeId);
                return false;
            }

            var taskItem = new TaskItem
            {
                TypeId = typeId,
                ExecuteFunc = executeFunc,
                NeedLock = needLock,
                EnqueueTime = DateTime.UtcNow
            };

            bool isNewTask = !_taskQueue.ContainsKey(typeId);
            
            // 添加或更新任务
            _taskQueue[typeId] = taskItem;
            
            // 仅当是新任务时才添加到处理队列
            if (isNewTask)
            {
                _processingOrder.Enqueue(typeId);
            }
            
            _queueSignal.Set(); // 通知执行线程有新任务
            
            _logger.LogDebug("任务已添加到队列: {TypeId}", typeId);
            return true;
        }

        /// <summary>
        /// 手动处理队列中的所有任务
        /// </summary>
        public void ProcessAll()
        {
            if (_disposed) return;

            _logger.LogDebug("开始手动处理队列中的所有任务");
            
            // 创建一个临时列表存储当前所有任务
            var tasksToProcess = _taskQueue.Keys.ToList();
            foreach (var taskId in tasksToProcess)
            {
                ProcessTask(taskId);
            }
        }

        /// <summary>
        /// 获取当前队列中任务数量
        /// </summary>
        public int Count => _taskQueue.Count;

        private void ExecuteLoop()
        {
            _logger.LogInformation("任务执行线程已启动");
            
            try
            {
                while (!_applicationLifetime.ApplicationStopped.IsCancellationRequested)
                {
                    // 等待信号或超时
                    _queueSignal.WaitOne(200); // 缩短等待时间，更频繁地检查回退的任务
                    
                    // 检查回退任务是否可以重新执行
                    CheckBackoffTasks();
                    
                    // 处理队列中的任务
                    ProcessNextTask();
                }

                // 应用停止后尝试处理剩余任务
                _logger.LogInformation("应用程序正在停止，处理剩余任务");
                ProcessAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务执行线程发生异常");
            }
            
            _logger.LogInformation("任务执行线程已停止");
        }

        private void CheckBackoffTasks()
        {
            var now = DateTime.UtcNow;
            // 找出可以重新尝试的任务
            var tasksDueForRetry = _backoffTasks
                .Where(kvp => now >= kvp.Value && _taskQueue.ContainsKey(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var taskId in tasksDueForRetry)
            {
                // 将任务移回处理队列
                _processingOrder.Enqueue(taskId);
                // 从回退列表中移除
                _backoffTasks.TryRemove(taskId, out _);
            }
        }

        private void ProcessNextTask()
        {
            if (_processingOrder.IsEmpty || _taskQueue.IsEmpty)
                return;

            // 尝试从处理队列中获取下一个任务
            if (!_processingOrder.TryDequeue(out var taskId))
                return;

            // 如果该任务仍在队列中，则处理它
            if (_taskQueue.ContainsKey(taskId))
            {
                ProcessTask(taskId);
            }
        }

        private void ProcessTask(object taskId)
        {
            // 从任务队列中获取任务，但暂不移除
            if (!_taskQueue.TryGetValue(taskId, out var taskItem))
                return;

            try
            {
                // 如果需要锁定，则尝试获取锁
                if (taskItem.NeedLock)
                {
                    if (!SingletonLocker.TryEnter(taskItem.TypeId, TimeSpan.FromMilliseconds(10)))
                    {
                        _logger.LogDebug("无法锁定任务，添加到回退列表: {TypeId}", taskItem.TypeId);
                        // 锁定失败，放入回退列表，稍后再尝试
                        _backoffTasks[taskItem.TypeId] = DateTime.UtcNow.Add(_backoffTime);
                        return;
                    }
                }

                try
                {
                    // 现在可以安全移除任务
                    _taskQueue.TryRemove(taskItem.TypeId, out _);
                    
                    // 执行任务
                    bool result = taskItem.ExecuteFunc(taskItem.TypeId);
                    if (result)
                    {
                        _logger.LogDebug("任务执行成功: {TypeId}", taskItem.TypeId);
                    }
                    else
                    {
                        _logger.LogWarning("任务执行返回失败: {TypeId}", taskItem.TypeId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "任务执行时发生异常: {TypeId}", taskItem.TypeId);
                }
                finally
                {
                    // 释放锁
                    if (taskItem.NeedLock)
                    {
                        SingletonLocker.Exit(taskItem.TypeId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理任务过程中发生异常: {TypeId}", taskItem.TypeId);
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
            _queueSignal.Set(); // 唤醒执行线程以便退出
            
            if (_executionThread.IsAlive && !_executionThread.Join(3000))
            {
                _logger.LogWarning("执行线程未能在超时时间内结束");
            }
            
            _queueSignal.Dispose();
            _logger.LogInformation("TaskDispatcher 已释放资源");
        }

        /// <summary>
        /// 尝试添加幂等任务，如果相同任务已在队列中则什么也不做
        /// </summary>
        /// <param name="typeId">任务类型标识</param>
        /// <param name="executeFunc">执行任务的函数</param>
        /// <param name="needLock">是否需要锁定</param>
        /// <returns>如果成功添加或任务已存在返回true，其他情况返回false</returns>
        public bool TryAddIdempotent(object typeId, Func<object, bool> executeFunc, bool needLock = true)
        {
            if (_taskQueue.ContainsKey(typeId))
                return true;
                
            return Enqueue(typeId, executeFunc, needLock);
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
            
            /// <summary>是否需要锁定</summary>
            public bool NeedLock { get; set; }
            
            /// <summary>入队时间</summary>
            public DateTime EnqueueTime { get; set; }
        }
    }
}
