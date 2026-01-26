/*
 * 项目：OwBaseCore基础设施组件
 * 模块：System.Threading - 任务调度器
 * 文件说明：
 * - 功能1：提供企业级任务调度服务，支持任务队列管理和合并
 * - 功能2：实现基于typeId的任务锁定机制，避免并发冲突
 * - 功能3：支持低优先级线程执行，不阻塞主线程性能
 * 
 * 核心功能需求：
 * - 队列结构：维护一个队列存放待执行任务，忽略任务优先级问题
 * - 相同类型任务合并：如果队列中已存在相同typeId的任务，则只保留最新任务的执行逻辑，合并处理
 * - 执行函数：签名为Func&lt;object, bool&gt;，返回true表示执行成功，false或抛出异常均视为失败
 * - 执行顺序：不需严格FIFO，但尽量保持先提交先执行
 * - 低优先级线程：使用一个低优先级线程顺序执行队列中的任务（可根据需求配置）
 * 
 * 锁定机制：
 * - SingletonLocker：在执行任务前，可调用静态类SingletonLocker根据typeId尝试加锁
 * - 若锁定失败则将任务放回队列，不阻塞其他任务
 * 
 * 线程与并发：
 * - 执行线程：可采用单线程或简化的多线程，避免过多并发冲突
 * - 队列容量：可通过配置指定队列上限，默认不限制
 * 
 * 服务生命周期：
 * - 依赖注入：支持Microsoft.Extensions.DependencyInjection
 * - 服务停止：通过检测IHostApplicationLifetime.Stopped，在停止前尽量执行完队列中剩余任务
 * 
 * 日志记录：
 * - 使用.NET 6日志框架记录任务执行情况，执行失败仅记录，无失败重试逻辑
 * 
 * 执行流程：
 * - 提交任务：Enqueue方法提交任务时指定typeId、执行逻辑及是否需要锁定
 * - 相同任务合并：若已存在同typeId的任务，则合并为一个，仅保留最新执行逻辑
 * - 从队列移除：当开始尝试执行任务时，立刻将其从队列中取出
 * - ProcessAll方法：依次执行队列中的任务，若需锁定則尝试SingletonLocker，锁定失败的任务将被放回队列，不阻塞其它任务
 * - 异常处理：任务执行失败后仅记录日志，并将其彻底移除或根据需求进行自定义处理
 * 
 * 结果处理：
 * - 完成后不考虑同类型：任务执行完成后无需判断队列中是否存在同类型任务，已执行的任务不再返回到队列
 * - 不提供手动移除接口：暂时不提供移除特定任务的功能
 * 
 * 非功能需求：
 * - 性能要求：在总体上保持先到先执行，不做严格FIFO限制
 * - 扩展性：为多线程、重试和监控等功能预留扩展空间
 * - 可维护性：压缩不必要的换行，注释尽量放在行尾
 * 
 * 技术要点：
 * - 基于.NET 6标准线程和同步机制
 * - 使用ConcurrentDictionary确保线程安全
 * - 支持配置化的队列容量和检查间隔
 * - 完整的资源释放和优雅停止机制
 * 作者：zc
 * 创建：2024年
 * 修改：2025-01-27 整合需求文档到代码注释中
 */

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
    /// 
    /// 配置说明：
    /// - MaxQueueSize：队列最大容量，可通过配置指定队列上限，默认不限制
    /// - CheckIntervalMs：执行线程检查间隔，控制任务处理的响应速度
    /// - LockTimeoutMs：锁定等待超时，配置SingletonLocker的等待时间
    /// - Logger：日志记录器，用于记录任务执行情况
    /// - CancellationToken：取消令牌，用于停止任务执行线程
    /// </summary>
    public class TaskDispatcherOptions : IOptions<TaskDispatcherOptions>
    {
        /// <summary>
        /// 队列最大容量，默认不限制
        /// 
        /// 队列容量说明：
        /// - 可通过配置指定队列上限，默认不限制
        /// - 当队列达到最大容量时，新任务将被拒绝
        /// - 设置为0或负数表示不限制容量
        /// </summary>
        public int MaxQueueSize { get; set; }

        /// <summary>
        /// 执行线程检查间隔（毫秒）
        /// 
        /// 检查间隔说明：
        /// - 控制低优先级线程检查队列的频率
        /// - 较小的值提供更好的响应性，但消耗更多CPU
        /// - 较大的值节省CPU资源，但响应性较差
        /// </summary>
        public int CheckIntervalMs { get; set; } = 200;

        /// <summary>
        /// 锁定等待超时（毫秒）
        /// 
        /// 锁定机制说明：
        /// - 在执行任务前，可调用静态类SingletonLocker根据typeId尝试加锁
        /// - 若锁定失败则将任务放回队列，不阻塞其他任务
        /// - 默认0，不等待，立即返回锁定结果
        /// </summary>
        /// <value>默认0，不等待</value>
        public int LockTimeoutMs { get; set; } = 0;

        /// <summary>
        /// 日志记录器
        /// 
        /// 日志记录说明：
        /// - 使用.NET 6日志框架记录任务执行情况
        /// - 执行失败仅记录，无失败重试逻辑
        /// - 记录任务提交、执行开始、执行完成、执行失败等关键事件
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 取消令牌，用于停止任务执行线程
        /// 
        /// 服务生命周期说明：
        /// - 支持Microsoft.Extensions.DependencyInjection
        /// - 通过检测IHostApplicationLifetime.Stopped，在停止前尽量执行完队列中剩余任务
        /// - 提供优雅停止机制，确保正在执行的任务能够完成
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TaskDispatcherOptions Value => this;
    }

    /// <summary>
    /// 任务调度器，管理和执行一系列任务，支持相同类型任务合并
    /// 
    /// 设计目标：
    /// - 任务存储与合并：维护一个队列存放待执行任务，忽略任务优先级问题
    /// - 相同类型任务合并：如果队列中已存在相同typeId的任务，则只保留最新任务的执行逻辑，合并处理
    /// - 任务定义与执行：执行函数签名为Func&lt;object, bool&gt;，返回true表示执行成功，false或抛出异常均视为失败
    /// - 执行顺序：不需严格FIFO，但尽量保持先提交先执行
    /// - 低优先级线程：使用一个低优先级线程顺序执行队列中的任务（可根据需求配置）
    /// 
    /// 核心特性：
    /// - 基于ConcurrentDictionary的线程安全队列结构
    /// - 支持typeId级别的任务合并，避免重复执行
    /// - 集成SingletonLocker锁定机制，防止并发冲突
    /// - 完整的生命周期管理和资源释放
    /// - 可配置的队列容量和检查间隔
    /// 
    /// 使用场景：
    /// - 后台任务处理，如文件处理、数据同步等
    /// - 防抖任务，避免短时间内重复执行相同操作
    /// - 低优先级任务调度，不影响主线程性能
    /// </summary>
    public class TaskDispatcher : IDisposable
    {
        private readonly ILogger _logger; // 日志记录器，用于记录任务执行情况
        private readonly ConcurrentDictionary<object, TaskItem> _taskQueue = new ConcurrentDictionary<object, TaskItem>(); // 任务队列，键为typeId，值为任务项
        private readonly Thread _executionThread; // 低优先级执行线程，顺序执行队列中的任务
        private readonly TaskDispatcherOptions _options; // 配置选项
        private readonly CancellationToken _cancellationToken; // 取消令牌，用于停止任务执行线程
        private bool _disposed; // 资源释放标志
        
        /// <summary>
        /// 任务调度器构造函数
        /// 
        /// 初始化说明：
        /// - 创建低优先级执行线程，避免影响主线程性能
        /// - 配置线程属性：后台线程、低优先级、命名
        /// - 支持依赖注入的配置选项
        /// - 自动启动执行线程
        /// </summary>
        /// <param name="options">配置选项，为空则使用默认配置</param>
        public TaskDispatcher(TaskDispatcherOptions options)
        {
            _options = options ?? new TaskDispatcherOptions();
            _logger = _options.Logger;
            _cancellationToken = _options.CancellationToken;

            // 创建低优先级执行线程
            _executionThread = new Thread(ExecuteLoop)
            {
                IsBackground = true, // 后台线程，不阻止应用程序退出
                Priority = ThreadPriority.BelowNormal, // 低优先级，不影响主线程性能
                Name = "TaskDispatcher_ExecutionThread" // 线程命名，便于调试
            };
            _executionThread.Start();
            
            _logger?.LogInformation("TaskDispatcher 已初始化，最大队列容量: {MaxQueueSize}",
                _options.MaxQueueSize > 0 ? _options.MaxQueueSize.ToString() : "不限制");
        }

        /// <summary>
        /// 提交任务到队列中，相同typeId的任务会被合并
        /// 
        /// 提交任务说明：
        /// - Enqueue方法：提交任务时指定typeId、执行逻辑及是否需要锁定
        /// - 相同任务合并：若已存在同typeId的任务，则合并为一个，仅保留最新执行逻辑
        /// - 队列容量检查：可通过配置指定队列上限，超出容量将拒绝新任务
        /// - 任务参数：支持传递任务参数，在执行时传递给执行函数
        /// </summary>
        /// <param name="typeId">任务类型标识</param>
        /// <param name="executeFunc">执行任务的函数</param>
        /// <param name="parameter">任务参数</param>
        /// <param name="needLock">是否需要锁定</param>
        /// <returns>是否成功添加到队列</returns>
        public bool Enqueue(object typeId, Func<object, bool> executeFunc, object parameter = null, bool needLock = true)
        {
            ArgumentNullException.ThrowIfNull(typeId);
            ArgumentNullException.ThrowIfNull(executeFunc);
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

            // 添加或更新任务（实现任务合并）
            _taskQueue[typeId] = taskItem;

            return true;
        }

        /// <summary>
        /// 手动处理队列中的所有任务，会在9秒内不断重试直到字典为空
        /// 
        /// ProcessAll方法说明：
        /// - 依次执行队列中的任务，若需锁定則尝试SingletonLocker
        /// - 锁定失败的任务将被放回队列，不阻塞其它任务
        /// - 设置超时机制，避免无限循环
        /// - 在服务停止时调用，确保剩余任务得到处理
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

        /// <summary>
        /// 执行循环，低优先级线程的主要工作方法
        /// 
        /// 执行线程说明：
        /// - 可采用单线程或简化的多线程，避免过多并发冲突
        /// - 使用低优先级线程顺序执行队列中的任务
        /// - 响应取消令牌，支持优雅停止
        /// - 在应用停止后尝试处理剩余任务
        /// </summary>
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

        /// <summary>
        /// 处理单个任务
        /// 
        /// 任务处理流程：
        /// - 从队列移除：当开始尝试执行任务时，立刻将其从队列中取出
        /// - 锁定检查：如果需要锁定，则尝试SingletonLocker，锁定失败将任务放回队列
        /// - 执行任务：调用执行函数，传递任务参数
        /// - 异常处理：任务执行失败后仅记录日志，并将其彻底移除
        /// - 结果处理：完成后不考虑同类型，已执行的任务不再返回到队列
        /// </summary>
        /// <param name="taskId">任务标识</param>
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
        /// 
        /// 幂等性支持：
        /// - 等待指定任务完成或超时
        /// - 支持幂等操作，可重复调用
        /// - 用于确保特定任务的完成状态
        /// </summary>
        /// <param name="typeId">任务类型ID</param>
        /// <param name="timeout">等待超时时间</param>
        /// <returns>如果任务完成或不存在返回true，超时返回false</returns>
        public bool EnsureCompleteIdempotent(object typeId, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(typeId);

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
        /// 
        /// 资源释放说明：
        /// - 等待执行线程完成，超时后强制结束
        /// - 确保所有资源得到正确释放
        /// - 记录资源释放状态
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
        /// 
        /// 幂等任务说明：
        /// - 如果相同typeId的任务已存在，则不添加新任务
        /// - 适用于防止重复提交的场景
        /// - 返回true表示任务已在队列中或成功添加
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
        /// 
        /// 任务项说明：
        /// - TypeId：任务类型标识，用于任务合并和锁定
        /// - ExecuteFunc：执行函数，签名为Func&lt;object, bool&gt;
        /// - Parameter：任务参数，在执行时传递给执行函数
        /// - NeedLock：是否需要锁定，控制是否使用SingletonLocker
        /// - EnqueueTime：入队时间，用于监控和调试
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
