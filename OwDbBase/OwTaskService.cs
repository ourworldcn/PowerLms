using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OW.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Data
{
    /// <summary>任务状态枚举，支持位标志组合</summary>
    [Flags]
    public enum OwTaskStatus : byte
    {
        /// <summary>刚创建</summary>
        Created = 0,
        /// <summary>待处理</summary>
        Pending = 1,
        /// <summary>执行中</summary>
        Running = 2,
        /// <summary>已完成</summary>
        Completed = 4,
        /// <summary>失败</summary>
        Failed = 8,
    }

    /// <summary>
    /// 长时间运行任务的存储实体，用于持久化任务信息和状态
    /// </summary>
    [Comment("长时间运行任务的存储实体")]
    [Index(nameof(CreatorId))]
    [Index(nameof(TenantId))]
    [Index(nameof(ServiceTypeName), nameof(MethodName))]
    [Index(nameof(StatusValue))]
    public class OwTaskStore : GuidKeyObjectBase
    {
        /// <summary>要执行的服务类型的完整名称，用于反射调用</summary>
        [Comment("要执行的服务类型的完整名称")]
        public string ServiceTypeName { get; set; }

        /// <summary>要执行的方法名称，配合服务类型用于反射调用</summary>
        [Comment("要执行的方法名称")]
        public string MethodName { get; set; }

        /// <summary>任务参数的JSON字符串表示，存储在数据库中</summary>
        [Comment("任务参数，JSON格式")]
        public string ParametersJson { get; set; }

        /// <summary>
        /// 任务参数的字典形式，不存储在数据库中，通过JSON序列化/反序列化转换
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> Parameters
        {
            get => string.IsNullOrEmpty(ParametersJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(ParametersJson);
            set => ParametersJson = value != null ? JsonSerializer.Serialize(value) : null;
        }

        /// <summary>任务当前执行状态的字节值，存储在数据库中</summary>
        [Comment("任务当前执行状态")]
        public byte StatusValue { get; set; }

        /// <summary>
        /// 任务状态的枚举形式，不存储在数据库中，通过StatusValue转换
        /// </summary>
        [NotMapped]
        public OwTaskStatus Status
        {
            get => (OwTaskStatus)StatusValue;
            set => StatusValue = (byte)value;
        }

        /// <summary>任务创建时间，UTC格式，精度到毫秒</summary>
        [Comment("任务创建时间，UTC格式")]
        [Precision(3)]
        public DateTime CreatedUtc { get; set; }

        /// <summary>任务开始执行时间，UTC格式，精度到毫秒，可为null</summary>
        [Comment("任务开始执行时间，UTC格式")]
        [Precision(3)]
        public DateTime? StartUtc { get; set; }

        /// <summary>任务完成时间，UTC格式，精度到毫秒，可为null</summary>
        [Comment("任务完成时间，UTC格式")]
        [Precision(3)]
        public DateTime? CompletedUtc { get; set; }

        /// <summary>任务执行结果的JSON字符串表示，存储在数据库中</summary>
        [Comment("任务执行结果，JSON格式")]
        public string ResultJson { get; set; }

        /// <summary>
        /// 任务执行结果的字典形式，不存储在数据库中，通过JSON序列化/反序列化转换
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> Result
        {
            get => string.IsNullOrEmpty(ResultJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(ResultJson);
            set => ResultJson = value != null ? JsonSerializer.Serialize(value) : null;
        }

        /// <summary>任务执行失败时的错误信息，包含完整的异常堆栈</summary>
        [Comment("任务执行失败时的错误信息")]
        public string ErrorMessage { get; set; }

        /// <summary>创建此任务的用户ID，用于权限控制和审计</summary>
        [Comment("创建此任务的用户ID")]
        public Guid CreatorId { get; set; }

        /// <summary>任务所属的租户ID，支持多租户场景，可为null</summary>
        [Comment("任务所属的租户ID")]
        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// 通用长时间运行任务服务类，提供通用的任务执行管理核心功能
    /// 使用.NET 6标准BackgroundService模式，支持并发控制和资源管理
    /// 增强异常处理，确保执行失败的任务异常能够被正确记录，包含堆栈信息
    /// 
    /// 设计目标：
    /// - 提供通用、可扩展的长时间运行任务基础服务类，适用于业务系统扩展
    /// - 支持任务的创建、启动、状态查询
    /// - 控制并发任务数，单机并发不超过CPU核心数，超出自动排队
    /// 
    /// 架构与机制：
    /// - 服务入口：通过 .NET 6 标准依赖注入注册
    /// - 线程隔离：每个任务独立线程，业务异常不影响主流程
    /// - 并发控制：采用同步原语（如信号量）动态限制并发数，强制≤CPU核心数
    /// - 数据库持久化：任务数据、状态、结果、异常信息全部持久化，便于后续查询
    /// - 日志体系：调度、排队、开始、结束、一般异常等流程记录为 debug 级别日志；任务错误或异常结尾记录为 warning 级别日志
    /// - 服务类与方法调用：通过依赖注入查找并调用业务方法，业务代码在独立线程执行
    /// 
    /// 任务数据模型：
    /// - 唯一标识：任务ID（Guid）
    /// - 服务类名称、方法名：表征任务实际执行逻辑，数据库联合索引便于检索
    /// - 参数与结果：Dictionary&lt;string, string&gt;，内容长度无限制，键值定义由业务自定，本服务类不作限制
    /// - 状态：按位枚举（byte/ushort），如待处理、执行中、已完成、失败等
    /// - 错误信息：异常内容全部序列化为字符串存储，无需结构化，便于排查
    /// - 创建者：任务创建者身份仅用一个 GUID 字段记录，由调用者指定
    /// - 租户支持：预留一个租户 GUID 字段区分多租户，目前仅单一租户
    /// 
    /// 操作流程：
    /// 1. 任务创建：指定服务类、方法名、参数，生成ID，写入数据库，记录创建者GUID和租户GUID（如有）
    /// 2. 调度与执行：依赖注入获取服务类及方法，参数反序列化后在独立线程执行。并发数超出自动排队，由同步原语动态控制
    /// 3. 状态与错误更新：执行完毕后，更新状态与结果。若异常，完整异常信息（字符串）写入数据库，相关日志分级记录
    /// 4. 查询：实时查询任务状态和结果，无需身份验证，但记录发起人和租户信息
    /// 
    /// 约束与说明：
    /// - 不支持任务取消、暂停、恢复
    /// - 不考虑任务唯一性、进度上报、自动重试、分布式部署
    /// - 任务数量无限制，任务列表无限增长，无需归档、分表、清理
    /// - 不处理系统重启后的任务恢复
    /// - 参数和结果内容长度无限制
    /// - 仅作为服务类由业务代码调用，不涉及 API、gRPC 或消息接口
    /// - 敏感数据、脱敏、加密等由业务层自理
    /// 
    /// 技术选型：
    /// - 开发语言：C# (.NET 6)
    /// - ORM：Entity Framework Core
    /// - 数据库：关系型数据库（如 SQL Server、SQLite 等）
    /// - 并发控制：.NET Task/线程 + 同步原语
    /// - 日志：.NET 内置日志（8级别，debug/warning等）
    /// - 服务注册：.NET 6 标准依赖注入
    /// 
    /// 可扩展建议：
    /// - 可按业务需求扩展任务优先级、分组、标签、运维监控等功能
    /// - 多租户支持已预留字段，未来可扩展为多租户场景
    /// </summary>
    /// <typeparam name="TDbContext">数据库上下文类型，必须继承自OwDbContext</typeparam>
    public class OwTaskService<TDbContext> : BackgroundService where TDbContext : OwDbContext
    {
        #region 字段和属性

        private readonly IServiceProvider _serviceProvider; // 服务提供者，用于创建服务范围
        private readonly ILogger<OwTaskService<TDbContext>> _logger; // 日志记录器
        private readonly IDbContextFactory<TDbContext> _dbContextFactory; // 数据库上下文工厂
        private readonly ConcurrentQueue<Guid> _pendingTaskIds = new(); // 待执行任务队列
        private readonly SemaphoreSlim _semaphore; // 信号量，用于控制并发数

        /// <summary>当前正在执行的任务数量，通过信号量计算</summary>
        public int CurrentRunningTaskCount => Environment.ProcessorCount - _semaphore.CurrentCount;

        /// <summary>当前等待队列中的任务数量</summary>
        public int PendingTaskCount => _pendingTaskIds.Count;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化任务服务实例，配置依赖项和并发控制
        /// </summary>
        /// <param name="serviceProvider">服务提供者，用于依赖注入</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="dbContextFactory">数据库上下文工厂</param>
        /// <exception cref="ArgumentNullException">当任何参数为null时抛出</exception>
        public OwTaskService(IServiceProvider serviceProvider, ILogger<OwTaskService<TDbContext>> logger, IDbContextFactory<TDbContext> dbContextFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount); // 并发数不超过CPU核心数

            _logger.LogInformation("OwTaskService 已初始化，数据库上下文: {DbContext}", typeof(TDbContext).Name);
        }

        #endregion

        #region BackgroundService 实现

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OwTaskService 正在启动...");
            // 将未完成的任务从数据库加载到内存队列中
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var pendingTasks = dbContext.Set<OwTaskStore>()
                    .Where(t => t.StatusValue == (byte)OwTaskStatus.Pending || t.StatusValue == (byte)OwTaskStatus.Created)
                    .Select(t => t.Id)
                    .ToList();
                foreach (var taskId in pendingTasks)
                {
                    _pendingTaskIds.Enqueue(taskId);
                }
                _logger.LogInformation("已加载 {Count} 个待处理任务到队列", pendingTasks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载待处理任务时发生错误");
            }
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 执行后台任务处理循环，持续监听任务队列并分发执行
        /// 使用同步模式避免async/await复杂性
        /// </summary>
        /// <param name="stoppingToken">取消令牌，用于优雅停止服务</param>
        /// <returns>表示异步操作的Task</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("OwTaskService 后台处理已启动");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_pendingTaskIds.TryDequeue(out var taskId))
                        {
                            _semaphore.Wait(stoppingToken); // 使用同步等待

                            _ = Task.Run(() => // 在独立线程中执行任务
                            {
                                try
                                {
                                    ProcessTask(taskId);
                                }
                                finally
                                {
                                    _semaphore.Release();
                                }
                            }, stoppingToken);
                        }
                        else
                        {
                            // 使用同步延迟方式
                            if (!stoppingToken.IsCancellationRequested)
                            {
                                Thread.Sleep(500); // 队列为空时等待
                            }
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break; // 正常取消
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "任务处理循环中发生错误");
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            Thread.Sleep(1000); // 出错时等待后重试
                        }
                    }
                }

                _logger.LogInformation("OwTaskService 后台处理已结束");
            }, stoppingToken);
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 创建并提交新任务到执行队列
        /// </summary>
        /// <param name="serviceType">要执行的服务类型，必须已注册到DI容器</param>
        /// <param name="methodName">要调用的方法名称，必须是公共方法</param>
        /// <param name="parameters">方法参数字典，键为参数名，值为参数值的字符串表示</param>
        /// <param name="creatorId">创建者用户ID，用于审计和权限控制</param>
        /// <param name="tenantId">租户ID，可选，用于多租户场景</param>
        /// <returns>新创建任务的唯一标识ID</returns>
        /// <exception cref="ArgumentNullException">当serviceType为null时抛出</exception>
        /// <exception cref="ArgumentException">当methodName为空或null时抛出</exception>
        public Guid CreateTask(Type serviceType, string methodName, Dictionary<string, string> parameters, Guid creatorId, Guid? tenantId = null)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("方法名称不能为空", nameof(methodName));

            var taskId = Guid.NewGuid();

            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var taskEntity = new OwTaskStore
                {
                    Id = taskId,
                    ServiceTypeName = serviceType.FullName,
                    MethodName = methodName,
                    Parameters = parameters ?? new Dictionary<string, string>(),
                    CreatedUtc = DateTime.UtcNow,
                    StatusValue = (byte)OwTaskStatus.Pending,
                    CreatorId = creatorId,
                    TenantId = tenantId
                };

                dbContext.Set<OwTaskStore>().Add(taskEntity);
                dbContext.SaveChanges();

                _pendingTaskIds.Enqueue(taskId); // 加入执行队列

                _logger.LogDebug("任务 {TaskId} 已创建，服务: {Service}，方法: {Method}", taskId, serviceType.Name, methodName);
                return taskId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建任务失败，服务: {Service}，方法: {Method}", serviceType.Name, methodName);
                throw;
            }
        }

        /// <summary>
        /// 创建并提交新任务的泛型重载版本，提供编译时类型安全
        /// </summary>
        /// <typeparam name="TService">要执行的服务类型，编译时确定</typeparam>
        /// <param name="methodName">要调用的方法名称</param>
        /// <param name="parameters">方法参数字典</param>
        /// <param name="creatorId">创建者用户ID</param>
        /// <param name="tenantId">租户ID，可选</param>
        /// <returns>新创建任务的唯一标识ID</returns>
        public Guid CreateTask<TService>(string methodName, Dictionary<string, string> parameters, Guid creatorId, Guid? tenantId = null)
        {
            return CreateTask(typeof(TService), methodName, parameters, creatorId, tenantId);
        }

        #endregion

        #region 内部实现

        /// <summary>
        /// 处理指定任务，使用同步方式和范围服务包装确保资源隔离
        /// 增强异常处理，确保完整记录异常信息和堆栈跟踪
        /// </summary>
        /// <param name="taskId">要执行的任务ID</param>
        private void ProcessTask(Guid taskId)
        {
            OwTaskStore taskEntity = null;
            string currentStep = "初始化";
            bool taskStarted = false; // 标记任务是否已开始，用于确保状态更新的正确性

            try
            {
                currentStep = "查找任务";
                // 获取任务并更新状态为执行中
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    taskEntity = dbContext.Set<OwTaskStore>().Find(taskId);
                    if (taskEntity == null)
                    {
                        _logger.LogWarning("未找到任务 {TaskId}", taskId);
                        return;
                    }

                    currentStep = "更新任务状态为执行中";
                    taskEntity.StatusValue = (byte)OwTaskStatus.Running;
                    taskEntity.StartUtc = DateTime.UtcNow;
                    dbContext.SaveChanges();
                    taskStarted = true; // 标记任务已开始

                    _logger.LogDebug("任务 {TaskId} 状态已更新为执行中，开始时间: {StartTime}", taskId, taskEntity.StartUtc);
                }

                _logger.LogDebug("开始执行任务 {TaskId}，服务: {Service}，方法: {Method}", taskId, taskEntity.ServiceTypeName, taskEntity.MethodName);

                currentStep = "验证任务基本信息";
                // 验证基本参数
                if (string.IsNullOrWhiteSpace(taskEntity.ServiceTypeName))
                    throw new InvalidOperationException("任务的服务类型名称为空");
                if (string.IsNullOrWhiteSpace(taskEntity.MethodName))
                    throw new InvalidOperationException("任务的方法名称为空");

                currentStep = "查找服务类型";
                // 改进的类型查找机制
                var serviceType = FindTypeByName(taskEntity.ServiceTypeName) ?? throw new InvalidOperationException($"无法找到类型: {taskEntity.ServiceTypeName}");
                _logger.LogDebug("任务 {TaskId} 找到服务类型: {ServiceType}", taskId, serviceType.FullName);

                currentStep = "查找方法";
                var methodInfo = serviceType.GetMethod(taskEntity.MethodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (methodInfo == null)
                    throw new InvalidOperationException($"在服务 {taskEntity.ServiceTypeName} 中找不到方法 {taskEntity.MethodName}");

                _logger.LogDebug("任务 {TaskId} 找到方法: {Method}, 是否静态: {IsStatic}", taskId, methodInfo.Name, methodInfo.IsStatic);

                currentStep = "准备方法参数";
                object result;
                object[] parameters;
                
                try
                {
                    parameters = PrepareMethodParameters(methodInfo, taskEntity.Parameters);
                    _logger.LogDebug("任务 {TaskId} 参数准备完成，参数数量: {ParameterCount}", taskId, parameters?.Length ?? 0);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"准备方法参数时发生错误: {ex.Message}", ex);
                }

                currentStep = "执行方法";
                if (methodInfo.IsStatic)
                {
                    // 静态方法调用：检查是否需要注入服务提供者
                    if (HasServiceProviderParameter(methodInfo))
                    {
                        currentStep = "创建服务作用域并执行静态方法";
                        // 为静态方法注入服务提供者
                        using var scope = _serviceProvider.CreateScope();
                        var scopedProvider = scope.ServiceProvider;
                        if (scopedProvider == null)
                            throw new InvalidOperationException("无法创建服务作用域");

                        _logger.LogDebug("任务 {TaskId} 开始执行静态方法（含服务提供者）", taskId);
                        
                        result = InvokeMethodWithExceptionHandling(methodInfo, null, parameters, scopedProvider, taskId);
                    }
                    else
                    {
                        currentStep = "执行静态方法";
                        _logger.LogDebug("任务 {TaskId} 开始执行静态方法", taskId);
                        
                        result = InvokeMethodWithExceptionHandling(methodInfo, null, parameters, null, taskId);
                    }
                }
                else
                {
                    currentStep = "创建服务作用域并解析服务实例";
                    // 实例方法调用
                    using var scope = _serviceProvider.CreateScope();
                    var scopedProvider = scope.ServiceProvider;
                    if (scopedProvider == null)
                        throw new InvalidOperationException("无法创建服务作用域");

                    var service = scopedProvider.GetService(serviceType);
                    if (service == null)
                        throw new InvalidOperationException($"无法从DI容器解析服务: {taskEntity.ServiceTypeName}");

                    currentStep = "执行实例方法";
                    _logger.LogDebug("任务 {TaskId} 开始执行实例方法", taskId);
                    
                    result = InvokeMethodWithExceptionHandling(methodInfo, service, parameters, scopedProvider, taskId);
                }

                currentStep = "更新任务完成状态";
                _logger.LogDebug("任务 {TaskId} 执行完成，开始更新状态", taskId);
                UpdateTaskCompletion(taskId, result);

                _logger.LogDebug("任务 {TaskId} 执行成功", taskId);
            }
            catch (Exception ex)
            {
                // 增强的错误信息，包含当前执行步骤和完整的异常信息
                var contextualError = $"任务执行失败，当前步骤: {currentStep}";
                if (taskEntity != null)
                {
                    contextualError += $"\n任务详情: ID={taskId}, 服务={taskEntity.ServiceTypeName}, 方法={taskEntity.MethodName}";
                    contextualError += $"\n任务参数: {taskEntity.ParametersJson ?? "无"}";
                }
                
                // 记录详细的异常信息到日志
                _logger.LogWarning(ex, "任务 {TaskId} 在步骤 '{CurrentStep}' 执行失败", taskId, currentStep);

                // 创建包含上下文信息的异常，保留原始异常作为内部异常
                var wrappedException = new InvalidOperationException(contextualError, ex);
                
                // 添加额外的上下文信息到异常数据中
                wrappedException.Data["TaskId"] = taskId;
                wrappedException.Data["ExecutionStep"] = currentStep;
                wrappedException.Data["TaskStarted"] = taskStarted;
                wrappedException.Data["ExecutionTime"] = DateTime.UtcNow;
                
                if (taskEntity != null)
                {
                    wrappedException.Data["ServiceTypeName"] = taskEntity.ServiceTypeName;
                    wrappedException.Data["MethodName"] = taskEntity.MethodName;
                    wrappedException.Data["TaskCreatedUtc"] = taskEntity.CreatedUtc;
                    wrappedException.Data["TaskStartUtc"] = taskEntity.StartUtc;
                    wrappedException.Data["CreatorId"] = taskEntity.CreatorId;
                    wrappedException.Data["TenantId"] = taskEntity.TenantId;
                }
                
                // 获取完整的错误信息（包括堆栈跟踪）
                var errorMessage = GetCompleteExceptionMessage(wrappedException);

                // 确保任务状态被正确更新为失败
                try
                {
                    if (taskStarted)
                    {
                        UpdateTaskFailure(taskId, errorMessage);
                        _logger.LogDebug("任务 {TaskId} 失败状态已更新", taskId);
                    }
                    else
                    {
                        _logger.LogWarning("任务 {TaskId} 在开始前就失败了，状态可能需要手动检查", taskId);
                        UpdateTaskFailure(taskId, errorMessage);
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "更新任务 {TaskId} 失败状态时发生错误", taskId);
                }
            }
        }

        /// <summary>
        /// 改进的方法调用，包含完整的异常处理和堆栈信息保存
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        /// <param name="service">服务实例（静态方法为null）</param>
        /// <param name="parameters">方法参数</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="taskId">任务ID</param>
        /// <returns>方法执行结果</returns>
        private object InvokeMethodWithExceptionHandling(MethodInfo methodInfo, object service, object[] parameters, IServiceProvider serviceProvider, Guid taskId)
        {
            try
            {
                // 如果是静态方法且需要服务提供者，注入服务提供者
                if (methodInfo.IsStatic && serviceProvider != null && HasServiceProviderParameter(methodInfo))
                {
                    return InvokeStaticMethodWithServiceProvider(methodInfo, parameters, serviceProvider, taskId);
                }
                else
                {
                    return methodInfo.Invoke(service, parameters);
                }
            }
            catch (TargetInvocationException tie)
            {
                // 提取内部异常，同时保留原始异常的完整信息
                var innerException = tie.InnerException ?? tie;
                var enhancedException = new InvalidOperationException(
                    $"方法执行时发生异常: {innerException.Message}\n原始反射异常: {tie.Message}", 
                    innerException);
                
                // 将原始TargetInvocationException的信息添加到Data中
                enhancedException.Data["OriginalTargetInvocationException"] = tie.ToString();
                enhancedException.Data["OriginalStackTrace"] = tie.StackTrace;
                enhancedException.Data["TargetMethod"] = methodInfo.Name;
                enhancedException.Data["TargetType"] = methodInfo.DeclaringType?.FullName;
                enhancedException.Data["IsStatic"] = methodInfo.IsStatic;
                if (service != null)
                {
                    enhancedException.Data["ServiceInstance"] = service.GetType().FullName;
                }
                
                throw enhancedException;
            }
            catch (Exception ex)
            {
                // 为其他异常添加上下文信息
                var enhancedException = new InvalidOperationException(
                    $"方法执行时发生异常: {ex.Message}", ex);
                
                enhancedException.Data["TargetMethod"] = methodInfo.Name;
                enhancedException.Data["TargetType"] = methodInfo.DeclaringType?.FullName;
                enhancedException.Data["IsStatic"] = methodInfo.IsStatic;
                enhancedException.Data["MethodParameters"] = string.Join(", ", parameters?.Select(p => p?.ToString() ?? "null") ?? Array.Empty<string>());
                if (service != null)
                {
                    enhancedException.Data["ServiceInstance"] = service.GetType().FullName;
                }
                
                throw enhancedException;
            }
        }

        /// <summary>
        /// 从异常对象提取完整的错误信息，包括内部异常链和堆栈信息
        /// 确保所有异常信息（包括反射调用的异常）都能被完整记录
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <returns>格式化的完整错误信息字符串，包含堆栈跟踪</returns>
        private static string GetCompleteExceptionMessage(Exception ex)
        {
            if (ex == null)
                return "未知异常";

            var errorDetails = new List<string>();
            var currentEx = ex;
            var exceptionLevel = 0;

            // 遍历异常链，收集所有异常信息
            while (currentEx != null)
            {
                var exceptionInfo = new List<string>();

                // 异常基本信息
                exceptionInfo.Add($"异常级别: {exceptionLevel}");
                exceptionInfo.Add($"异常类型: {currentEx.GetType().FullName}");
                exceptionInfo.Add($"异常消息: {currentEx.Message}");

                // 如果有目标站点信息，添加它
                if (currentEx.TargetSite != null)
                {
                    exceptionInfo.Add($"目标方法: {currentEx.TargetSite.DeclaringType?.FullName}.{currentEx.TargetSite.Name}");
                    
                    // 获取目标方法的参数信息
                    var parameters = currentEx.TargetSite.GetParameters();
                    if (parameters.Length > 0)
                    {
                        var parameterInfo = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        exceptionInfo.Add($"目标方法参数: {parameterInfo}");
                    }
                }

                // 如果有源信息，添加它
                if (!string.IsNullOrEmpty(currentEx.Source))
                {
                    exceptionInfo.Add($"异常源: {currentEx.Source}");
                }

                // 记录HResult（如果有用）
                if (currentEx.HResult != 0)
                {
                    exceptionInfo.Add($"HResult: 0x{currentEx.HResult:X8}");
                }

                // 堆栈跟踪信息 - 这是最重要的部分
                if (!string.IsNullOrEmpty(currentEx.StackTrace))
                {
                    exceptionInfo.Add($"堆栈跟踪:\n{currentEx.StackTrace}");
                }
                else
                {
                    // 如果没有堆栈跟踪，尝试获取当前调用栈
                    try
                    {
                        var stackTrace = new System.Diagnostics.StackTrace(currentEx, true);
                        if (stackTrace.FrameCount > 0)
                        {
                            exceptionInfo.Add($"堆栈跟踪（通过StackTrace获取）:\n{stackTrace}");
                        }
                    }
                    catch
                    {
                        // 如果无法获取堆栈跟踪，至少记录这一点
                        exceptionInfo.Add("堆栈跟踪: 无法获取堆栈跟踪信息");
                    }
                }

                // 如果有附加数据，添加它
                if (currentEx.Data != null && currentEx.Data.Count > 0)
                {
                    var dataEntries = new List<string>();
                    foreach (var key in currentEx.Data.Keys)
                    {
                        try
                        {
                            dataEntries.Add($"  {key}: {currentEx.Data[key]}");
                        }
                        catch
                        {
                            dataEntries.Add($"  {key}: <无法序列化>");
                        }
                    }
                    exceptionInfo.Add($"附加数据:\n{string.Join("\n", dataEntries)}");
                }

                // 针对TargetInvocationException的特殊处理
                if (currentEx is TargetInvocationException tie)
                {
                    exceptionInfo.Add("注意: 这是一个反射调用异常，真正的异常在InnerException中");
                    if (tie.InnerException != null)
                    {
                        exceptionInfo.Add($"内部异常预览: {tie.InnerException.GetType().Name} - {tie.InnerException.Message}");
                    }
                }

                // 针对AggregateException的特殊处理
                if (currentEx is AggregateException aggEx)
                {
                    exceptionInfo.Add($"聚合异常包含 {aggEx.InnerExceptions.Count} 个内部异常");
                    for (int i = 0; i < aggEx.InnerExceptions.Count; i++)
                    {
                        var innerEx = aggEx.InnerExceptions[i];
                        exceptionInfo.Add($"  聚合异常[{i}]: {innerEx.GetType().Name} - {innerEx.Message}");
                    }
                }

                errorDetails.Add($"=== 异常 {exceptionLevel} ===\n{string.Join("\n", exceptionInfo)}");

                currentEx = currentEx.InnerException;
                exceptionLevel++;
            }

            // 添加时间戳和环境信息
            var environmentInfo = new List<string>
            {
                $"异常发生时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC",
                $"本地时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                $"机器名称: {Environment.MachineName}",
                $"用户域: {Environment.UserDomainName}",
                $"用户名: {Environment.UserName}",
                $"操作系统: {Environment.OSVersion}",
                $"进程ID: {Environment.ProcessId}",
                $"线程ID: {Thread.CurrentThread.ManagedThreadId}",
                $"线程名称: {Thread.CurrentThread.Name ?? "未命名"}",
                $"应用程序域: {AppDomain.CurrentDomain.FriendlyName}",
                $"工作目录: {Environment.CurrentDirectory}",
                $"CLR版本: {Environment.Version}",
                $"处理器数量: {Environment.ProcessorCount}",
                $"系统启动时间: {Environment.TickCount}ms"
            };

            // 添加当前调用栈（如果可用）
            try
            {
                var currentStackTrace = new System.Diagnostics.StackTrace(true);
                if (currentStackTrace.FrameCount > 0)
                {
                    environmentInfo.Add($"当前调用栈:\n{currentStackTrace}");
                }
            }
            catch
            {
                environmentInfo.Add("当前调用栈: 无法获取");
            }

            var fullErrorMessage = new List<string>
            {
                "=== 任务执行异常详情 ===",
                string.Join("\n", environmentInfo),
                "",
                string.Join("\n\n", errorDetails),
                "=== 异常详情结束 ==="
            };

            return string.Join("\n", fullErrorMessage);
        }

        /// <summary>
        /// 改进的类型查找方法，支持在所有已加载程序集中查找类型
        /// </summary>
        /// <param name="typeName">完整的类型名称</param>
        /// <returns>找到的类型，如果未找到则返回null</returns>
        private static Type FindTypeByName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            // 首先尝试标准的Type.GetType()
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            // 如果失败，遍历当前应用域中的所有程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return type;

                    // 也尝试不区分大小写的匹配
                    var types = assembly.GetTypes().Where(t =>
                        string.Equals(t.FullName, typeName, StringComparison.OrdinalIgnoreCase)).ToArray();

                    if (types.Length == 1)
                        return types[0];
                    else if (types.Length > 1)
                        throw new InvalidOperationException($"找到多个匹配的类型: {typeName}");
                }
                catch (ReflectionTypeLoadException)
                {
                    // 某些程序集可能无法加载所有类型，跳过这些异常
                    continue;
                }
                catch (Exception)
                {
                    // 跳过其他异常，继续搜索下一个程序集
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// 检查方法是否有服务提供者参数
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        /// <returns>如果方法需要服务提供者则返回true</returns>
        private static bool HasServiceProviderParameter(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            return parameters.Any(p => p.ParameterType == typeof(IServiceProvider));
        }

        /// <summary>
        /// 为静态方法调用注入服务提供者和任务ID
        /// 增强处理特殊参数注入（taskId, serviceProvider）
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        /// <param name="parameters">原始参数数组</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="taskId">任务ID</param>
        /// <returns>方法执行结果</returns>
        private object InvokeStaticMethodWithServiceProvider(MethodInfo methodInfo, object[] parameters, IServiceProvider serviceProvider, Guid taskId)
        {
            var methodParams = methodInfo.GetParameters();
            var enhancedParameters = new object[methodParams.Length];

            // 复制原有参数
            Array.Copy(parameters, enhancedParameters, Math.Min(parameters.Length, enhancedParameters.Length));

            // 查找并注入特殊参数
            for (int i = 0; i < methodParams.Length; i++)
            {
                // 注入服务提供者参数
                if (methodParams[i].ParameterType == typeof(IServiceProvider))
                {
                    enhancedParameters[i] = serviceProvider;
                }
                // 注入任务ID参数
                else if (methodParams[i].ParameterType == typeof(Guid) && 
                         string.Equals(methodParams[i].Name, "taskId", StringComparison.OrdinalIgnoreCase))
                {
                    enhancedParameters[i] = taskId;
                }
            }

            return methodInfo.Invoke(null, enhancedParameters);
        }

        /// <summary>
        /// 根据方法信息和参数字典准备方法调用参数数组
        /// 增强处理特殊参数匹配，特别是对于Dictionary<string, string>类型的参数
        /// </summary>
        /// <param name="methodInfo">目标方法的反射信息</param>
        /// <param name="parameterDict">参数名值对字典</param>
        /// <returns>准备好的参数数组，按方法签名顺序排列</returns>
        private static object[] PrepareMethodParameters(MethodInfo methodInfo, Dictionary<string, string> parameterDict)
        {
            var methodParams = methodInfo.GetParameters();
            var parameters = new object[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                var param = methodParams[i];
                
                // 特殊处理：如果参数类型是Dictionary<string, string>且参数名为"parameters"
                // 直接传递整个parameterDict，这是任务系统的约定
                if (param.ParameterType == typeof(Dictionary<string, string>) && 
                    string.Equals(param.Name, "parameters", StringComparison.OrdinalIgnoreCase))
                {
                    parameters[i] = parameterDict ?? new Dictionary<string, string>();
                    continue;
                }
                
                // 特殊处理：如果参数类型是Guid且参数名为"taskId"
                // 不从字典中查找，而是在后续由InvokeStaticMethodWithServiceProvider处理
                if (param.ParameterType == typeof(Guid) && 
                    string.Equals(param.Name, "taskId", StringComparison.OrdinalIgnoreCase))
                {
                    parameters[i] = Guid.Empty; // 临时值，会在InvokeStaticMethodWithServiceProvider中被正确设置
                    continue;
                }
                
                // 特殊处理：如果参数类型是IServiceProvider且参数名为"serviceProvider"
                // 不从字典中查找，而是在后续由InvokeStaticMethodWithServiceProvider处理
                if (param.ParameterType == typeof(IServiceProvider) && 
                    string.Equals(param.Name, "serviceProvider", StringComparison.OrdinalIgnoreCase))
                {
                    parameters[i] = null; // 临时值，会在InvokeStaticMethodWithServiceProvider中被正确设置
                    continue;
                }

                // 常规参数处理
                if (parameterDict?.TryGetValue(param.Name, out var paramValue) == true && !string.IsNullOrEmpty(paramValue))
                {
                    try
                    {
                        // 处理基本类型的转换
                        if (param.ParameterType == typeof(Guid) || param.ParameterType == typeof(Guid?))
                        {
                            if (param.ParameterType == typeof(Guid?))
                            {
                                parameters[i] = string.IsNullOrEmpty(paramValue) ? (Guid?)null : Guid.Parse(paramValue);
                            }
                            else
                            {
                                parameters[i] = Guid.Parse(paramValue);
                            }
                        }
                        else if (param.ParameterType == typeof(Dictionary<string, string>))
                        {
                            parameters[i] = JsonSerializer.Deserialize<Dictionary<string, string>>(paramValue);
                        }
                        else
                        {
                            parameters[i] = Convert.ChangeType(paramValue, param.ParameterType);
                        }
                    }
                    catch (Exception)
                    {
                        parameters[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                    }
                }
                else
                {
                    parameters[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                }
            }

            return parameters;
        }

        /// <summary>
        /// 更新任务为完成状态，并保存执行结果
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="result">任务执行结果</param>
        private void UpdateTaskCompletion(Guid taskId, object result)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var task = dbContext.Set<OwTaskStore>().Find(taskId);
                if (task != null)
                {
                    task.StatusValue = (byte)OwTaskStatus.Completed;
                    task.CompletedUtc = DateTime.UtcNow;

                    if (result != null)
                    {
                        try
                        {
                            // 尝试将结果序列化为JSON
                            var resultJson = JsonSerializer.Serialize(result);
                            task.Result = new Dictionary<string, string> { { "Result", resultJson } };
                        }
                        catch (Exception serializationEx)
                        {
                            _logger.LogWarning(serializationEx, "任务 {TaskId} 结果序列化失败，使用ToString()方法", taskId);
                            try
                            {
                                // 如果序列化失败，使用ToString()
                                task.Result = new Dictionary<string, string> { { "Result", result.ToString() } };
                            }
                            catch (Exception toStringEx)
                            {
                                _logger.LogWarning(toStringEx, "任务 {TaskId} 结果ToString()也失败，使用类型名称", taskId);
                                // 如果ToString()也失败，至少记录类型信息
                                task.Result = new Dictionary<string, string> { 
                                    { "Result", $"<序列化失败: {result.GetType().FullName}>" },
                                    { "SerializationError", serializationEx.Message }
                                };
                            }
                        }
                    }

                    dbContext.SaveChanges();
                    _logger.LogDebug("任务 {TaskId} 完成状态更新成功", taskId);
                }
                else
                {
                    _logger.LogWarning("尝试更新任务 {TaskId} 完成状态时，未找到该任务", taskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新任务 {TaskId} 完成状态失败", taskId);
                
                // 尝试简化的状态更新
                try
                {
                    using var dbContext = _dbContextFactory.CreateDbContext();
                    var task = dbContext.Set<OwTaskStore>().Find(taskId);
                    if (task != null)
                    {
                        task.StatusValue = (byte)OwTaskStatus.Completed;
                        task.CompletedUtc = DateTime.UtcNow;
                        // 不保存结果，只更新状态
                        dbContext.SaveChanges();
                        _logger.LogDebug("任务 {TaskId} 完成状态更新成功（简化版本，未保存结果）", taskId);
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "重试更新任务 {TaskId} 完成状态时也发生错误", taskId);
                }
            }
        }

        /// <summary>
        /// 更新任务为失败状态，并保存完整的错误信息
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="errorMessage">完整的错误信息，包括堆栈跟踪</param>
        private void UpdateTaskFailure(Guid taskId, string errorMessage)
        {
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var task = dbContext.Set<OwTaskStore>().Find(taskId);
                if (task != null)
                {
                    task.StatusValue = (byte)OwTaskStatus.Failed;
                    task.CompletedUtc = DateTime.UtcNow;
                    task.ErrorMessage = errorMessage;
                    
                    // 尝试保存更改
                    dbContext.SaveChanges();
                    _logger.LogDebug("任务 {TaskId} 失败状态更新成功", taskId);
                }
                else
                {
                    _logger.LogWarning("尝试更新任务 {TaskId} 失败状态时，未找到该任务", taskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新任务 {TaskId} 失败状态时发生错误", taskId);
                
                // 如果数据库更新失败，尝试记录到日志中
                _logger.LogError("任务 {TaskId} 的错误信息（因数据库更新失败而记录到日志）：\n{ErrorMessage}", taskId, errorMessage);
                
                // 尝试简化的状态更新
                try
                {
                    using var dbContext = _dbContextFactory.CreateDbContext();
                    var task = dbContext.Set<OwTaskStore>().Find(taskId);
                    if (task != null)
                    {
                        task.StatusValue = (byte)OwTaskStatus.Failed;
                        task.CompletedUtc = DateTime.UtcNow;
                        // 如果错误信息太长，截断它
                        task.ErrorMessage = errorMessage.Length > 8000 ? 
                            errorMessage.Substring(0, 8000) + "\n...[错误信息已截断，完整信息请查看日志]" : 
                            errorMessage;
                        
                        dbContext.SaveChanges();
                        _logger.LogDebug("任务 {TaskId} 失败状态更新成功（简化版本）", taskId);
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "重试更新任务 {TaskId} 失败状态时也发生错误", taskId);
                }
            }
        }

        #endregion

        #region 释放资源

        /// <summary>
        /// 释放托管资源，包括信号量等
        /// </summary>
        public override void Dispose()
        {
            _semaphore?.Dispose();
            base.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// OwTaskService的扩展方法，用于简化服务注册
    /// </summary>
    public static class OwTaskServiceExtensions
    {
        /// <summary>
        /// 向服务集合添加OwTaskService，使用.NET 6标准的HostedService模式
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>更新后的服务集合，支持链式调用</returns>
        public static IServiceCollection AddOwTaskService<TDbContext>(this IServiceCollection services)
            where TDbContext : OwDbContext
        {
            services.AddHostedService<OwTaskService<TDbContext>>();
            services.TryAddSingleton(provider =>
                (OwTaskService<TDbContext>)provider.GetRequiredService<IEnumerable<IHostedService>>().First(c => c is OwTaskService<TDbContext>));
            return services;
        }

        /// <summary>
        /// 向服务集合添加OwTaskService并同时配置DbContextFactory
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">数据库上下文配置委托</param>
        /// <returns>更新后的服务集合，支持链式调用</returns>
        public static IServiceCollection AddOwTaskService<TDbContext>(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
            where TDbContext : OwDbContext
        {
            services.AddDbContextFactory<TDbContext>(optionsAction);
            return services.AddOwTaskService<TDbContext>();
        }
    }
}