using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Data;
using OW.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace OwDbBase.Tasks
{
    /// <summary>通用长时间运行任务基础服务类（单例模式）。</summary>
    /// <typeparam name="TDbContext">数据库上下文类型，必须继承自OwDbContext。</typeparam>
    public class OwTaskService<TDbContext> where TDbContext : OwDbContext
    {
        #region 单例实现

        // 单例实例的字典，按数据库上下文类型区分不同实例
        private static readonly ConcurrentDictionary<Type, OwTaskService<TDbContext>> _Instances = new();

        // 用于同步单例创建的锁对象
        private static readonly object _InstanceLock = new();

        /// <summary>获取OwTaskService的单例实例。</summary>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <returns>OwTaskService的单例实例。</returns>
        public static OwTaskService<TDbContext> GetInstance(IServiceProvider serviceProvider)
        {
            if (_Instances.TryGetValue(typeof(TDbContext), out var instance))
                return instance;

            lock (_InstanceLock)
            {
                return _Instances.GetOrAdd(typeof(TDbContext), _ =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<OwTaskService<TDbContext>>>();
                    var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<TDbContext>>();
                    return new OwTaskService<TDbContext>(serviceProvider, logger, dbContextFactory);
                });
            }
        }

        #endregion

        #region 字段和属性

        private readonly IServiceProvider _ServiceProvider; // 服务提供者
        private readonly ILogger<OwTaskService<TDbContext>> _Logger; // 日志记录器
        private readonly IDbContextFactory<TDbContext> _DbContextFactory; // 数据库上下文工厂
        private readonly ConcurrentQueue<Guid> _PendingTaskIds = new(); // 排队等待的任务队列
        private readonly SemaphoreSlim _Semaphore; // 用于控制并发任务数的信号量
        private bool _IsRunning = true; // 标记服务是否正在运行

        /// <summary>获取当前正在执行的任务数量。</summary>
        public int CurrentRunningTaskCount => Environment.ProcessorCount - _Semaphore.CurrentCount;

        /// <summary>获取当前等待队列中的任务数量。</summary>
        public int PendingTaskCount => _PendingTaskIds.Count;

        #endregion

        #region 构造函数

        /// <summary>初始化任务服务实例（私有构造函数，仅通过GetInstance方法获取实例）。</summary>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="logger">日志记录器。</param>
        /// <param name="dbContextFactory">数据库上下文工厂。</param>
        private OwTaskService(
            IServiceProvider serviceProvider,
            ILogger<OwTaskService<TDbContext>> logger,
            IDbContextFactory<TDbContext> dbContextFactory)
        {
            _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _DbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));

            // 并发控制，最大并发数等于CPU核心数
            _Semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

            // 恢复数据库中未完成的任务
            RecoverPendingTasksFromDatabase();

            // 启动任务调度器
            var processorThread = new Thread(ProcessPendingTasks)
            {
                IsBackground = true,
                Name = $"OwTaskProcessor-{typeof(TDbContext).Name}"
            };
            processorThread.Start();

            _Logger.LogInformation("OwTaskService 单例已初始化，针对数据库上下文: {DbContext}", typeof(TDbContext).Name);
        }

        #endregion

        #region 公共接口

        /// <summary>创建并提交一个新任务。</summary>
        /// <param name="serviceType">服务类型。</param>
        /// <param name="methodName">方法名称。</param>
        /// <param name="parameters">任务参数。</param>
        /// <param name="creatorId">创建者ID。</param>
        /// <param name="tenantId">租户ID，可选。</param>
        /// <returns>新创建任务的ID。</returns>
        public Guid CreateTask(
            Type serviceType,
            string methodName,
            Dictionary<string, string> parameters,
            Guid creatorId,
            Guid? tenantId = null)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (string.IsNullOrEmpty(methodName)) throw new ArgumentNullException(nameof(methodName));

            // 创建任务记录
            using var dbContext = _DbContextFactory.CreateDbContext();

            var taskEntity = new OwTaskStore
            {
                Id = Guid.NewGuid(),
                ServiceTypeName = serviceType.FullName,
                MethodName = methodName,
                Parameters = parameters ?? new Dictionary<string, string>(),
                CreatedUtc = DateTime.UtcNow,
                StatusValue = (byte)OwTaskStatus.Pending, // 使用StatusValue而非Status
                CreatorId = creatorId,
                TenantId = tenantId
            };

            dbContext.Set<OwTaskStore>().Add(taskEntity);
            dbContext.SaveChanges();

            _Logger.LogDebug("任务 {TaskId} 已创建, 服务: {Service}, 方法: {Method}",
                taskEntity.Id, serviceType.Name, methodName);

            // 将任务添加到等待队列
            _PendingTaskIds.Enqueue(taskEntity.Id);

            return taskEntity.Id;
        }

        /// <summary>根据ID获取任务信息。</summary>
        /// <param name="taskId">任务ID。</param>
        /// <returns>任务实体。</returns>
        public OwTaskStore GetTask(Guid taskId)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            return dbContext.Set<OwTaskStore>().Find(taskId);
        }

        /// <summary>根据服务和方法查询任务列表。</summary>
        /// <param name="serviceTypeName">服务类型名称。</param>
        /// <param name="methodName">方法名称。</param>
        /// <returns>符合条件的任务列表。</returns>
        public List<OwTaskStore> QueryTasks(string serviceTypeName, string methodName)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            return dbContext.Set<OwTaskStore>()
                .Where(t => t.ServiceTypeName == serviceTypeName && t.MethodName == methodName)
                .ToList();
        }

        /// <summary>查询指定创建者的任务列表。</summary>
        /// <param name="creatorId">创建者ID。</param>
        /// <returns>该创建者的任务列表。</returns>
        public List<OwTaskStore> GetTasksByCreator(Guid creatorId)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            return dbContext.Set<OwTaskStore>()
                .Where(t => t.CreatorId == creatorId)
                .ToList();
        }

        /// <summary>停止任务服务。</summary>
        /// <param name="waitForCompletion">是否等待所有任务完成。</param>
        /// <param name="timeoutInSeconds">等待超时时间（秒）。</param>
        public void Shutdown(bool waitForCompletion = true, int timeoutInSeconds = 30)
        {
            _IsRunning = false;

            if (waitForCompletion)
            {
                var startTime = DateTime.UtcNow;
                while (_PendingTaskIds.Count > 0 && CurrentRunningTaskCount > 0)
                {
                    if ((DateTime.UtcNow - startTime).TotalSeconds > timeoutInSeconds)
                    {
                        _Logger.LogWarning("任务服务关闭超时，仍有 {Pending} 个任务等待中，{Running} 个任务执行中",
                            _PendingTaskIds.Count, CurrentRunningTaskCount);
                        break;
                    }
                    Thread.Sleep(100);
                }
            }

            _Logger.LogInformation("任务服务已关闭");
        }

        #endregion

        #region 内部实现

        /// <summary>从数据库恢复未完成的任务。</summary>
        private void RecoverPendingTasksFromDatabase()
        {
            try
            {
                using var dbContext = _DbContextFactory.CreateDbContext();

                // 查询所有处于待处理或运行中状态的任务
                var pendingTasks = dbContext.Set<OwTaskStore>()
                    .Where(t => t.StatusValue == (byte)OwTaskStatus.Pending ||
                                t.StatusValue == (byte)OwTaskStatus.Running)
                    .ToList();

                if (pendingTasks.Any())
                {
                    _Logger.LogInformation("正在恢复 {Count} 个未完成的任务", pendingTasks.Count);

                    foreach (var task in pendingTasks)
                    {
                        // 如果任务正在运行，则标记为失败并添加错误信息
                        if (task.StatusValue == (byte)OwTaskStatus.Running)
                        {
                            task.StatusValue = (byte)OwTaskStatus.Failed;
                            task.ErrorMessage = "服务重启时任务正在执行，已被中断";
                            task.CompletedUtc = DateTime.UtcNow;
                        }

                        // 将待处理的任务重新加入队列
                        if (task.StatusValue == (byte)OwTaskStatus.Pending)
                        {
                            _PendingTaskIds.Enqueue(task.Id);
                        }
                    }

                    dbContext.SaveChanges();
                    _Logger.LogInformation("未完成的任务已恢复到队列");
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "恢复未完成任务时出错");
            }
        }

        /// <summary>处理等待队列中的任务。</summary>
        private void ProcessPendingTasks()
        {
            _Logger.LogInformation("任务处理线程已启动");

            while (_IsRunning)
            {
                if (_PendingTaskIds.TryDequeue(out var taskId))
                {
                    _Semaphore.Wait(); // 使用信号量控制并发

                    // 以独立线程执行任务
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            ExecuteTask(taskId);
                        }
                        finally
                        {
                            _Semaphore.Release(); // 完成后释放信号量
                        }
                    })
                    {
                        IsBackground = true,
                        Name = $"TaskExecutor-{taskId}"
                    };
                    thread.Start();
                }
                else
                {
                    // 队列为空或达到最大并发数时等待一段时间再检查
                    Thread.Sleep(500);
                }
            }

            _Logger.LogInformation("任务处理线程已结束");
        }

        /// <summary>执行指定ID的任务。</summary>
        /// <param name="taskId">任务ID。</param>
        private void ExecuteTask(Guid taskId)
        {
            OwTaskStore taskEntity = null;

            try
            {
                // 获取任务详情
                using (var dbContext = _DbContextFactory.CreateDbContext())
                {
                    taskEntity = dbContext.Set<OwTaskStore>().Find(taskId);
                    if (taskEntity == null)
                    {
                        _Logger.LogWarning("未找到ID为 {TaskId} 的任务", taskId);
                        return;
                    }

                    // 更新任务状态为执行中
                    taskEntity.StatusValue = (byte)OwTaskStatus.Running;
                    taskEntity.StartUtc = DateTime.UtcNow;
                    dbContext.SaveChanges();
                }

                _Logger.LogDebug("开始执行任务 {TaskId}, 服务: {Service}, 方法: {Method}",
                    taskId, taskEntity.ServiceTypeName, taskEntity.MethodName);

                // 获取服务实例和方法
                var serviceType = Type.GetType(taskEntity.ServiceTypeName);
                if (serviceType == null)
                {
                    throw new InvalidOperationException($"无法找到类型: {taskEntity.ServiceTypeName}");
                }

                // 使用作用域服务提供程序，确保每个任务有自己的服务实例
                using var scope = _ServiceProvider.CreateScope();
                var service = scope.ServiceProvider.GetService(serviceType);

                if (service == null)
                {
                    throw new InvalidOperationException($"无法从DI容器中解析服务: {taskEntity.ServiceTypeName}");
                }

                var methodInfo = serviceType.GetMethod(taskEntity.MethodName);
                if (methodInfo == null)
                {
                    throw new InvalidOperationException($"找不到方法 {taskEntity.MethodName} 在服务 {taskEntity.ServiceTypeName} 中");
                }

                // 准备方法参数
                var parameters = PrepareMethodParameters(methodInfo, taskEntity.Parameters);

                // 执行方法
                object result = null;
                if (methodInfo.ReturnType == typeof(Task) ||
                    methodInfo.ReturnType.IsGenericType &&
                    methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    // 异步方法，但我们在同步上下文中执行它
                    dynamic task = methodInfo.Invoke(service, parameters);
                    task.Wait(); // 同步等待任务完成

                    if (methodInfo.ReturnType.IsGenericType)
                    {
                        // 如果有返回值
                        result = ((dynamic)task).Result;
                    }
                }
                else
                {
                    // 同步方法
                    result = methodInfo.Invoke(service, parameters);
                }

                // 任务成功完成，更新状态和结果
                UpdateTaskCompletion(taskId, result);

                _Logger.LogDebug("任务 {TaskId} 成功完成", taskId);
            }
            catch (Exception ex)
            {
                // 处理任务执行期间的异常
                var errorMessage = GetFullExceptionMessage(ex);
                UpdateTaskFailure(taskId, errorMessage);

                _Logger.LogWarning(ex, "任务 {TaskId} 执行失败: {Error}", taskId, errorMessage);
            }
        }

        /// <summary>准备方法参数。</summary>
        /// <param name="methodInfo">方法信息。</param>
        /// <param name="parameterDict">参数字典。</param>
        /// <returns>参数数组。</returns>
        private object[] PrepareMethodParameters(MethodInfo methodInfo, Dictionary<string, string> parameterDict)
        {
            var methodParams = methodInfo.GetParameters();
            var parameters = new object[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                var param = methodParams[i];
                if (parameterDict != null && parameterDict.TryGetValue(param.Name, out var paramValue))
                {
                    parameters[i] = Convert.ChangeType(paramValue, param.ParameterType);
                }
                else
                {
                    parameters[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                }
            }

            return parameters;
        }

        /// <summary>更新任务完成状态。</summary>
        /// <param name="taskId">任务ID。</param>
        /// <param name="result">任务执行结果。</param>
        private void UpdateTaskCompletion(Guid taskId, object result)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            var task = dbContext.Set<OwTaskStore>().Find(taskId);
            if (task != null)
            {
                task.StatusValue = (byte)OwTaskStatus.Completed;
                task.CompletedUtc = DateTime.UtcNow;

                // 如果有结果，序列化为字符串存储
                if (result != null)
                {
                    task.Result = new Dictionary<string, string>
                    {
                        { "Result", result.ToString() }
                    };
                }

                dbContext.SaveChanges();
            }
        }

        /// <summary>更新任务失败状态。</summary>
        /// <param name="taskId">任务ID。</param>
        /// <param name="errorMessage">错误信息。</param>
        private void UpdateTaskFailure(Guid taskId, string errorMessage)
        {
            using var dbContext = _DbContextFactory.CreateDbContext();
            var task = dbContext.Set<OwTaskStore>().Find(taskId);
            if (task != null)
            {
                task.StatusValue = (byte)OwTaskStatus.Failed;
                task.CompletedUtc = DateTime.UtcNow;
                task.ErrorMessage = errorMessage;
                dbContext.SaveChanges();
            }
        }

        /// <summary>获取完整的异常信息。</summary>
        /// <param name="ex">异常对象。</param>
        /// <returns>完整的异常信息字符串。</returns>
        private string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var currentEx = ex;

            while (currentEx != null)
            {
                messages.Add($"{currentEx.GetType().Name}: {currentEx.Message}\n{currentEx.StackTrace}");
                currentEx = currentEx.InnerException;
            }

            return string.Join("\n\n--- Inner Exception ---\n\n", messages);
        }

        #endregion
    }

    /// <summary>任务状态位标志枚举。</summary>
    [Flags]
    public enum OwTaskStatus : byte
    {
        /// <summary>待处理状态</summary>
        Pending = 1,    // 0001

        /// <summary>执行中状态</summary>
        Running = 2,    // 0010

        /// <summary>已完成状态</summary>
        Completed = 4,  // 0100

        /// <summary>失败状态</summary>
        Failed = 8,     // 1000

        /// <summary>已取消状态（保留）</summary>
        Cancelled = 16, // 10000

        /// <summary>已暂停状态（保留）</summary>
        Paused = 32,    // 100000

        /// <summary>已超时状态（保留）</summary>
        TimedOut = 64   // 1000000
    }

    /// <summary>任务实体类。</summary>
    [Comment("长时间运行任务的存储实体")]
    [Index(nameof(CreatorId))]
    [Index(nameof(TenantId))]
    [Index(nameof(ServiceTypeName), nameof(MethodName))]
    [Index(nameof(StatusValue))]
    public class OwTaskStore : GuidKeyObjectBase
    {
        public OwTaskStore()
        {
        }

        /// <summary>服务类型名称。</summary>
        [Comment("要执行的服务类型的完整名称")]
        public string ServiceTypeName { get; set; }

        /// <summary>方法名称。</summary>
        [Comment("要执行的方法名称")]
        public string MethodName { get; set; }

        /// <summary>任务参数的JSON序列化字符串。</summary>
        [Comment("任务参数，JSON格式的字符串")]
        public string ParametersJson { get; set; }

        /// <summary>任务参数，非数据库字段。</summary>
        [NotMapped]
        public Dictionary<string, string> Parameters
        {
            get => string.IsNullOrEmpty(ParametersJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(ParametersJson);
            set => ParametersJson = value != null
                ? JsonSerializer.Serialize(value)
                : null;
        }

        /// <summary>任务状态值（数据库中存储的原始字节）</summary>
        [Comment("任务当前执行状态值")]
        public byte StatusValue { get; set; }

        /// <summary>任务状态（非数据库字段）</summary>
        [NotMapped]
        public OwTaskStatus Status
        {
            get => (OwTaskStatus)StatusValue;
            set => StatusValue = (byte)value;
        }

        /// <summary>创建时间，UTC格式，精确到毫秒。</summary>
        [Comment("任务创建时间，UTC格式，精确到毫秒")]
        [Precision(3)] // 时间精度设为毫秒
        public DateTime CreatedUtc { get; set; }

        /// <summary>开始执行时间，UTC格式，精确到毫秒。</summary>
        [Comment("任务开始执行时间，UTC格式，精确到毫秒")]
        [Precision(3)] // 时间精度设为毫秒
        public DateTime? StartUtc { get; set; }

        /// <summary>完成时间，UTC格式，精确到毫秒。</summary>
        [Comment("任务完成时间，UTC格式，精确到毫秒")]
        [Precision(3)] // 时间精度设为毫秒
        public DateTime? CompletedUtc { get; set; }

        /// <summary>任务结果的JSON序列化字符串。</summary>
        [Comment("任务执行结果，JSON格式的字符串")]
        public string ResultJson { get; set; }

        /// <summary>任务结果，非数据库字段。</summary>
        [NotMapped]
        public Dictionary<string, string> Result
        {
            get => string.IsNullOrEmpty(ResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(ResultJson);
            set => ResultJson = value != null
                ? JsonSerializer.Serialize(value)
                : null;
        }

        /// <summary>错误信息。</summary>
        [Comment("任务执行失败时的错误信息")]
        public string ErrorMessage { get; set; }

        /// <summary>创建者ID。</summary>
        [Comment("创建此任务的用户ID")]
        public Guid CreatorId { get; set; }

        /// <summary>租户ID。</summary>
        [Comment("任务所属的租户ID")]
        public Guid? TenantId { get; set; }
    }

    /// <summary>为OwTaskService提供的扩展方法。</summary>
    public static class OwTaskServiceExtensions
    {
        /// <summary>向服务集合添加OwTaskService服务（单例模式）。</summary>
        /// <typeparam name="TDbContext">数据库上下文类型。</typeparam>
        /// <param name="services">服务集合。</param>
        /// <returns>服务集合，用于链式调用。</returns>
        public static IServiceCollection AddOwTaskService<TDbContext>(this IServiceCollection services)
            where TDbContext : OwDbContext =>
            services.AddSingleton(provider => OwTaskService<TDbContext>.GetInstance(provider));   // 注册为单例服务，但实际由工厂方法创建实例

        /// <summary>向服务集合添加OwTaskService服务（单例模式），同时配置DbContextFactory。</summary>
        /// <typeparam name="TDbContext">数据库上下文类型。</typeparam>
        /// <param name="services">服务集合。</param>
        /// <param name="optionsAction">数据库上下文配置动作。</param>
        /// <returns>服务集合，用于链式调用。</returns>
        public static IServiceCollection AddOwTaskService<TDbContext>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction)
            where TDbContext : OwDbContext
        {
            services.AddDbContextFactory<TDbContext>(optionsAction);

            // 注册为单例服务，但实际由工厂方法创建实例
            services.AddSingleton<OwTaskService<TDbContext>>(provider =>
                OwTaskService<TDbContext>.GetInstance(provider));

            return services;
        }
    }

    /// <summary>用于操作任务状态的辅助类。</summary>
    public static class OwTaskStatusHelper
    {
        /// <summary>检查任务是否包含指定状态。</summary>
        /// <param name="status">要检查的状态。</param>
        /// <param name="flag">要检查的标志。</param>
        /// <returns>如果包含则为true，否则为false。</returns>
        public static bool HasFlag(this byte status, OwTaskStatus flag) =>
            ((OwTaskStatus)status).HasFlag(flag);

        /// <summary>添加状态标志。</summary>
        /// <param name="status">原始状态。</param>
        /// <param name="flag">要添加的标志。</param>
        /// <returns>添加标志后的状态。</returns>
        public static byte AddFlag(this byte status, OwTaskStatus flag) =>
            (byte)((OwTaskStatus)status | flag);

        /// <summary>移除状态标志。</summary>
        /// <param name="status">原始状态。</param>
        /// <param name="flag">要移除的标志。</param>
        /// <returns>移除标志后的状态。</returns>
        public static byte RemoveFlag(this byte status, OwTaskStatus flag) =>
            (byte)((OwTaskStatus)status & ~flag);
    }
}
