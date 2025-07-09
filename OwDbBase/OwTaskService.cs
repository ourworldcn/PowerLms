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
        /// <summary>待处理</summary>
        Pending = 1,
        /// <summary>执行中</summary>
        Running = 2,
        /// <summary>已完成</summary>
        Completed = 4,
        /// <summary>失败</summary>
        Failed = 8
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
    /// 通用长时间运行任务基础服务类，仅提供创建和执行任务的核心功能
    /// 使用.NET 6标准的BackgroundService模式，支持并发控制和资源隔离
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
        /// </summary>
        /// <param name="taskId">要执行的任务ID</param>
        private void ProcessTask(Guid taskId)
        {
            OwTaskStore taskEntity = null;
            
            try
            {
                // 获取任务并更新状态为执行中
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    taskEntity = dbContext.Set<OwTaskStore>().Find(taskId);
                    if (taskEntity == null)
                    {
                        _logger.LogWarning("未找到任务 {TaskId}", taskId);
                        return;
                    }
                    
                    taskEntity.StatusValue = (byte)OwTaskStatus.Running;
                    taskEntity.StartUtc = DateTime.UtcNow;
                    dbContext.SaveChanges();
                }
                
                _logger.LogDebug("开始执行任务 {TaskId}，服务: {Service}，方法: {Method}", taskId, taskEntity.ServiceTypeName, taskEntity.MethodName);
                
                var serviceType = Type.GetType(taskEntity.ServiceTypeName);
                if (serviceType == null)
                    throw new InvalidOperationException($"无法找到类型: {taskEntity.ServiceTypeName}");
                
                var methodInfo = serviceType.GetMethod(taskEntity.MethodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (methodInfo == null)
                    throw new InvalidOperationException($"在服务 {taskEntity.ServiceTypeName} 中找不到方法 {taskEntity.MethodName}");
                
                object result;
                var parameters = PrepareMethodParameters(methodInfo, taskEntity.Parameters);
                
                if (methodInfo.IsStatic)
                {
                    // 静态方法调用：检查是否需要注入服务提供者
                    if (HasServiceProviderParameter(methodInfo))
                    {
                        // 为静态方法注入服务提供者
                        using var scope = _serviceProvider.CreateScope();
                        var scopedProvider = scope.ServiceProvider;
                        result = InvokeStaticMethodWithServiceProvider(methodInfo, parameters, scopedProvider);
                    }
                    else
                    {
                        // 普通静态方法调用
                        result = methodInfo.Invoke(null, parameters);
                    }
                }
                else
                {
                    // 实例方法调用（原有逻辑）
                    using var scope = _serviceProvider.CreateScope();
                    var scopedProvider = scope.ServiceProvider;
                    
                    var service = scopedProvider.GetService(serviceType);
                    if (service == null)
                        throw new InvalidOperationException($"无法从DI容器解析服务: {taskEntity.ServiceTypeName}");
                    
                    result = methodInfo.Invoke(service, parameters);
                }
                
                UpdateTaskCompletion(taskId, result);
                
                _logger.LogDebug("任务 {TaskId} 执行成功", taskId);
            }
            catch (Exception ex)
            {
                var errorMessage = GetExceptionMessage(ex);
                UpdateTaskFailure(taskId, errorMessage);
                _logger.LogWarning(ex, "任务 {TaskId} 执行失败: {Error}", taskId, errorMessage);
            }
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
        /// 为静态方法调用注入服务提供者
        /// </summary>
        /// <param name="methodInfo">方法信息</param>
        /// <param name="parameters">原始参数数组</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <returns>方法执行结果</returns>
        private static object InvokeStaticMethodWithServiceProvider(MethodInfo methodInfo, object[] parameters, IServiceProvider serviceProvider)
        {
            var methodParams = methodInfo.GetParameters();
            var enhancedParameters = new object[methodParams.Length];
            
            // 复制原有参数
            Array.Copy(parameters, enhancedParameters, Math.Min(parameters.Length, enhancedParameters.Length));
            
            // 查找并注入服务提供者参数
            for (int i = 0; i < methodParams.Length; i++)
            {
                if (methodParams[i].ParameterType == typeof(IServiceProvider))
                {
                    enhancedParameters[i] = serviceProvider;
                    break;
                }
            }
            
            return methodInfo.Invoke(null, enhancedParameters);
        }

        /// <summary>
        /// 根据方法信息和参数字典准备方法调用参数数组
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
                        catch (Exception)
                        {
                            // 如果序列化失败，使用ToString()
                            task.Result = new Dictionary<string, string> { { "Result", result.ToString() } };
                        }
                    }
                    
                    dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新任务完成状态失败，任务ID: {TaskId}", taskId);
            }
        }

        /// <summary>
        /// 更新任务为失败状态，并保存错误信息
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="errorMessage">错误信息</param>
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
                    dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新任务失败状态失败，任务ID: {TaskId}", taskId);
            }
        }

        /// <summary>
        /// 从异常对象提取完整的错误信息，包括内部异常链
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <returns>格式化的错误信息字符串</returns>
        private static string GetExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var currentEx = ex;
            
            while (currentEx != null)
            {
                messages.Add($"{currentEx.GetType().Name}: {currentEx.Message}");
                currentEx = currentEx.InnerException;
            }
            
            return string.Join(" -> ", messages);
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

        #region 使用示例（注释形式）

        /*
        // 静态任务调用示例 - 在OwTaskService中的使用
        
        // 1. 注册静态任务处理器类型（在Program.cs或启动配置中）
        services.AddOwTaskService<PowerLmsUserDbContext>();
        
        // 2. 在控制器中创建静态任务
        var taskService = serviceProvider.GetRequiredService<OwTaskService<PowerLmsUserDbContext>>();
        var taskId = taskService.CreateTask(
            typeof(FinancialSystemExportTaskProcessor),
            nameof(FinancialSystemExportTaskProcessor.ProcessInvoiceDbfExportTask),
            taskParameters,
            userId,
            orgId
        );
        
        // 3. OwTaskService自动检测静态方法并调用：
        // - 检测到方法是静态的
        // - 检测到方法需要IServiceProvider参数
        // - 自动注入服务提供者并调用静态方法
        
        // 4. 静态方法的优势：
        // - 避免依赖控制器实例，减少内存占用
        // - 更好的线程安全性
        // - 明确的依赖关系（通过参数显式声明）
        // - 更容易进行单元测试
        */

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
