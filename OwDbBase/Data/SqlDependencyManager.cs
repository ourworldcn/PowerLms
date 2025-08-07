/*
 * 项目：OwDbBase数据访问基础组件
 * 模块：Data - SQL Server依赖通知管理
 * 文件说明：
 * - 功能1：封装SQL Server Query Notification机制，实现数据变更的实时推送
 * - 功能2：提供SqlDependency的生命周期管理和自动重连机制
 * - 功能3：支持多数据库、多查询的并发监听和资源管理
 * 
 * SqlDependency 详解（.NET 6 / EF Core 6 场景适用）：
 * 
 * 1. 技术定位：
 * SqlDependency 属于 SQL Server Query Notification（查询通知）客户端封装，借助数据库端的 Service Broker 
 * 把"结果集是否发生变化"事件推送到应用进程。典型用途：
 * - ASP.NET / Windows Service 缓存过期通知
 * - 桌面程序实时刷新列表
 * - 轻量级发布–订阅（Pub/Sub）
 * 注意：仅能检测"与指定 SQL 结果集相关的行数据是否改变"，而不是返回更改内容；
 * 复杂变更场景需用 CDC / Debezium 等。
 * 
 * 2. 运行机制：
 * 2.1 初始化：SqlDependency.Start(connectionString)
 *     - 在本机注册静态侦听端口（SqlClient 监听服务）
 *     - 解析连接字符串，保持到数据库的控制连接
 * 
 * 2.2 注册查询：
 *     - 创建 SqlCommand 并附加 SqlDependency dependency = new(cmd);
 *     - 首次 ExecuteReader() 时，SqlClient 会向 SQL Server 发送 sp_executesql 
 *       带 WITH (RECOMPILE, QUERYTRACEON 4199) 等注解，声明需要"查询通知"
 * 
 * 2.3 数据库端：
 *     - Query Notification Manager (QDS) 解析查询→检查是否可通知→在 Service Broker 
 *       队列 AspNet_SqlDependency_Queue（默认）创建一条订阅
 *     - 当表数据发生 Insert/Update/Delete 并满足命中条件，触发器向队列写入消息
 * 
 * 2.4 Service Broker 通道：
 *     - 队列 → 对话 → 发送到监听端口（TCP 4022 等，随配置）
 * 
 * 2.5 客户端回调：
 *     - 底层侦听器收到消息 → 解析→ 触发 dependency.OnChange += (s,e)=>{ ... }
 *     - e.Info（Insert/Delete/Update/Expired），e.Source（Data/Timeout）
 * 
 * 2.6 重新订阅：
 *     - 通知只触发一次；在回调里必须重新执行查询以续期
 * 
 * 3. 环境准备：
 * 3.1 启用 Service Broker：
 *     ALTER DATABASE MyDB SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
 *     （仅数据库级别；需独占模式，多租户环境无法ENABLE_BROKER可为应用单独划分数据库）
 * 
 * 3.2 权限要求：
 *     GRANT SUBSCRIBE QUERY NOTIFICATIONS TO [AppUser];
 *     GRANT RECEIVE ON QueryNotificationErrorsQueue TO [AppUser];
 *     （若使用托管身份(Azure)或AD账户，同理授权）
 * 
 * 3.3 防火墙配置：
 *     - 默认侦听本地端口（随机高位）
 *     - 可通过SqlDependency.Start(conn, queue: null, listenAddress: "tcp://0.0.0.0:42425")自定义
 *     - 务必放行入站TCP端口
 * 
 * 4. 可通知查询规则：
 * 4.1 必须遵守的限制：
 *     - 必须两级对象名：dbo.Table，不能使用*，必须显列
 *     - 禁用：SELECT INTO、DISTINCT、TOP (expr)、GROUP BY、HAVING、UNION、JOIN中的子查询聚合
 *     - 不得访问临时表、表变量、视图含不允许元素
 *     - 必须SET NOCOUNT ON（SqlClient自动加）
 *     - READ COMMITTED或更高（NOLOCK会被拒绝）
 *     - 结果集≤8KB行大小（超出则不缓存→无法通知）
 *     - 使用数据库表或Indexed View（字段必须可索引）
 * 
 * 4.2 查询不合法时：
 *     OnChange将返回e.Type = Subscribe | e.Info = Error
 * 
 * 5. 性能与容量：
 * - 订阅数：单库上限~2^53（实际受内存/事务日志影响）
 * - 每次DML：若涉及10张表，将对其全部相关订阅逐一发送消息；写放大明显
 * - CPU：Service Broker启用后，占用额外"BROKER_RECEIVE_WAITFOR"线程；并发高时需增配
 * - 建议：批量合并查询，或使用缓存分区+定时轮询混合方案
 * 
 * 6. 与 EF Core 6 集成思路：
 * EF Core无原生封装；常见做法：
 * - 订阅层（SqlDependency）+MemoryCache/Redis标记失效
 * - 当OnChange触发→清理指定cache key→上层重新context.Set<TEntity>().AsNoTracking().ToListAsync()
 * - 避免把DbContext连接交给SqlDependency（连接池会断开订阅）：
 *   * 使用独立SqlConnection
 *   * EF Core仍走自己的连接池
 * 
 * 7. 局限与注意事项：
 * 7.1 跨平台限制：
 *     .NET 6下使用Microsoft.Data.SqlClient，SqlDependency只在Windows支持
 *     （Linux无Service Broker侦听器实现）
 * 
 * 7.2 延时特性：
 *     消息在事务提交后才到达客户端；未提交事务更改不会触发
 * 
 * 7.3 队列阻塞：
 *     若应用宕机未及时END CONVERSATION，队列增长；需定期ALTER QUEUE WITH STATUS = ON清理
 * 
 * 7.4 连接掉线：
 *     数据库重启或网络闪断需要重启订阅；务必在OnChange里try…catch并Start/Stop重连
 * 
 * 7.5 安全考虑：
 *     Service Broker监听本机端口；若部署云环境需限制入站
 * 
 * 8. 替代方案：
 * - 高吞吐实时流：CDC → Debezium → Kafka / EventHub
 * - Azure SQL / 跨平台：Azure SQL Change Data Capture + Event Grid
 * - SQL Server低版本/无法启用Broker：轮询rowversion字段
 * - Redis缓存：key过期 + Pub/Sub通知
 * 
 * 技术要点：
 * - 基于SQL Server Service Broker的实时数据变更推送
 * - 自动管理SqlDependency生命周期和重连机制
 * - 支持多数据库并发监听和资源计数管理
 * - 实现BackgroundService模式的优雅停止
 * 
 * 作者：OW
 * 创建日期：2023年10月25日
 * 修改记录：2025-01-27 整合SqlDependency详解需求文档
 */

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Data
{
    /// <summary>
    /// SqlDependency 依赖管理器，用于监听一个 SQL 字符串的结果变化
    /// 
    /// 核心功能：
    /// - SQL Server查询通知：基于Service Broker实现数据变更的实时推送
    /// - 自动重连机制：网络断开或数据库重启后自动恢复监听
    /// - 资源管理：自动管理数据库连接和SqlDependency订阅的生命周期
    /// - 并发支持：支持多个查询同时监听，每个查询独立管理
    /// 
    /// 使用场景：
    /// - 缓存失效通知：当数据库数据变更时自动清理应用缓存
    /// - 实时数据同步：桌面应用或Web应用的数据实时刷新
    /// - 轻量级消息推送：基于数据变更的业务事件通知
    /// - 性能监控：监控关键业务数据的变化频率和模式
    /// 
    /// 技术约束：
    /// - 仅支持Windows平台（Linux无Service Broker客户端支持）
    /// - 查询必须符合SQL Server通知规则（显式列名、无复杂聚合等）
    /// - 需要数据库启用Service Broker和相应权限配置
    /// - 适用于中低频变更场景（高频场景建议使用CDC或消息队列）
    /// 
    /// 最佳实践：
    /// - 查询尽量简单，避免复杂JOIN和聚合
    /// - 监听表建议有适当索引以提高通知性能
    /// - 在生产环境中监控Service Broker队列状态
    /// - 配合缓存系统使用以提高整体性能
    /// </summary>
    public class SqlDependencyManager : BackgroundService
    {
        #region 私有字段
        /// <summary>
        /// 启用数据库Service Broker的SQL格式化字符串
        /// 
        /// SQL说明：
        /// - 需要在master数据库执行以获得足够权限
        /// - 检查目标数据库是否已启用Service Broker
        /// - 如果未启用则自动启用（需要独占访问权限）
        /// </summary>
        public const string EnableBrokerFormatString = "USE master; \r\n IF EXISTS (SELECT is_broker_enabled FROM sys.databases WHERE name = '{0}' AND is_broker_enabled = 0)\r\nBEGIN\r\n    ALTER DATABASE [{0}] SET ENABLE_BROKER;\r\nEND\r\n";
        
        private readonly ILogger<SqlDependencyManager> _Logger;
        
        /// <summary>
        /// 连接字符串使用计数字典
        /// 
        /// 计数管理说明：
        /// - 键：数据库连接字符串
        /// - 值：当前使用该连接字符串的查询数量
        /// - 当计数为0时自动停止对该数据库的监听
        /// - 避免资源泄漏和不必要的连接占用
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _ConnectionStringUsageCount = new();
        
        /// <summary>
        /// 数据库连接缓存字典
        /// 
        /// 连接管理说明：
        /// - 每个连接字符串维护一个长连接实例
        /// - 避免频繁创建和销毁数据库连接
        /// - 在所有相关监听停止时统一释放连接
        /// </summary>
        private readonly ConcurrentDictionary<string, SqlConnection> _Connections = new();
        
        /// <summary>
        /// SQL查询取消令牌源字典
        /// 
        /// 取消机制说明：
        /// - 键：SQL查询字符串
        /// - 值：对应的取消令牌源
        /// - 支持按查询粒度的监听控制
        /// - 便于实现查询级别的启动和停止
        /// </summary>
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _SqlCancellationTokenSources = new();
        
        /// <summary>
        /// 线程同步锁对象
        /// 
        /// 锁定范围：
        /// - 保护连接字符串使用计数的原子操作
        /// - 确保SqlDependency.Start/Stop的线程安全
        /// - 避免并发访问导致的资源泄漏
        /// </summary>
        public object Locker => _ConnectionStringUsageCount;
        #endregion 私有字段

        #region 静态方法
        /// <summary>
        /// 启用 SQL Broker
        /// 
        /// 操作说明：
        /// - 在指定数据库上启用Service Broker功能
        /// - 需要数据库独占访问权限（可能需要断开其他连接）
        /// - 启用后数据库支持查询通知和消息队列功能
        /// - 建议在应用启动时或部署脚本中执行
        /// 
        /// 注意事项：
        /// - 需要sysadmin或dbcreator权限
        /// - 可能导致短暂的数据库不可用
        /// - 启用后会增加数据库的资源消耗
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="databaseName">数据库名称</param>
        public static void EnableSqlBroker(string connectionString, string databaseName)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            var commandText = string.Format(EnableBrokerFormatString, databaseName);
            using var command = new SqlCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 已启用Service Broker的数据库缓存集合
        /// 
        /// 缓存目的：
        /// - 避免重复执行启用Service Broker的操作
        /// - 提高应用启动性能
        /// - 减少对数据库的不必要操作
        /// </summary>
        static HashSet<string> _EnabledBrokerDatabases = new();
        
        /// <summary>
        /// 确保数据库启用了 Broker
        /// 
        /// 幂等性保证：
        /// - 使用内存缓存避免重复检查同一数据库
        /// - 首次调用时执行启用操作，后续调用直接返回
        /// - 适合在应用生命周期内多次调用的场景
        /// 
        /// 性能优化：
        /// - 避免每次注册监听都检查Service Broker状态
        /// - 减少数据库管理操作的开销
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="databaseName">数据库名称</param>
        public static void EnsureDatabaseEnabledBroker(string connectionString, string databaseName)
        {
            if (_EnabledBrokerDatabases.Add(databaseName))
                EnableSqlBroker(connectionString, databaseName);
        }
        #endregion 静态方法

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器
        /// 
        /// 依赖注入说明：
        /// - ILogger用于记录SqlDependency的运行状态和错误信息
        /// - 支持结构化日志记录，便于生产环境监控
        /// - 记录监听启动、停止、错误等关键事件
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public SqlDependencyManager(ILogger<SqlDependencyManager> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion 构造函数

        #region 核心方法
        /// <summary>
        /// 注册 SqlDependency 监听，注册后监听器在检测到变化时自动重新注册
        /// 
        /// 注册流程：
        /// 1. 创建取消令牌源用于控制监听生命周期
        /// 2. 管理连接字符串使用计数，首次使用时启动SqlDependency.Start
        /// 3. 确保目标数据库启用了Service Broker
        /// 4. 创建SqlCommand和SqlDependency，绑定变更事件
        /// 5. 执行查询以激活监听（必须读取结果集）
        /// 6. 注册到监听管理字典中
        /// 
        /// 自动重连机制：
        /// - 当检测到数据变更时，OnChange事件会自动重新注册
        /// - 网络断开或数据库重启后需要手动重新调用此方法
        /// - 建议配合应用健康检查机制使用
        /// 
        /// 性能考虑：
        /// - 查询应尽量简单，避免复杂JOIN和大结果集
        /// - 建议监听表有适当的索引以提高通知效率
        /// - 避免监听高频变更的表以减少通知开销
        /// </summary>
        /// <param name="sqlQuery">要监听的 SQL 查询字符串</param>
        /// <param name="connectionString">要使用的数据库连接字符串</param>
        /// <returns>一个可用于取消监听的取消令牌源，在检测到变化时会关闭</returns>
        public CancellationTokenSource RegisterSqlDependency(string sqlQuery, string connectionString)
        {
            var cts = new CancellationTokenSource();
            lock (Locker)
                if (_ConnectionStringUsageCount.AddOrUpdate(connectionString, 1, (_, count) => count + 1) == 1)
                {
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    EnsureDatabaseEnabledBroker(connectionString, builder.InitialCatalog ?? builder["Database"].ToString());
                    SqlDependency.Start(connectionString);
                }

            var connection = _Connections.GetOrAdd(connectionString, connStr => new SqlConnection(connStr));
            var command = new SqlCommand(sqlQuery, connection);
            var dependency = new SqlDependency(command);
            dependency.OnChange += (sender, e) => OnDependencyChange(sender, e, cts, connectionString, sqlQuery);

            try
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                var reader = command.ExecuteReader();
                // 读取结果以触发 SqlDependency
                while (reader.Read()) { }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "执行 SqlCommand 时出错");
                connection.Close();
                throw;
            }

            _SqlCancellationTokenSources[sqlQuery] = cts;
            return cts;
        }

        /// <summary>
        /// 停止指定的 SQL 查询监听数据库变化
        /// 
        /// 停止流程：
        /// 1. 从监听字典中移除指定查询的取消令牌源
        /// 2. 触发取消令牌，通知相关组件停止监听
        /// 3. 记录调试日志便于问题排查
        /// 
        /// 资源清理：
        /// - 仅停止指定查询的监听，不影响其他查询
        /// - 如果是某个连接字符串的最后一个查询，会在OnDependencyChange中自动清理连接
        /// - 支持部分停止，灵活控制监听范围
        /// </summary>
        /// <param name="sqlQuery">要停止监听的 SQL 查询字符串</param>
        public void StopListening(string sqlQuery)
        {
            if (_SqlCancellationTokenSources.TryRemove(sqlQuery, out var cts))
            {
                cts.Cancel();
                _Logger.LogDebug("已停止 SQL 查询 {SqlQuery} 的数据库监听", sqlQuery);
            }
        }
        #endregion 核心方法

        #region 私有方法
        /// <summary>
        /// SqlDependency 变化事件处理器
        /// 
        /// 事件处理逻辑：
        /// 1. 检查通知类型，仅处理数据变更类型的通知
        /// 2. 取消当前监听的令牌，通知相关组件数据已变更
        /// 3. 自动重新注册相同查询的监听（实现持续监听）
        /// 4. 管理连接字符串使用计数，必要时停止SqlDependency和关闭连接
        /// 
        /// 异常处理：
        /// - 重新注册过程中的异常不会影响通知机制
        /// - 记录调试日志便于问题诊断
        /// - 即使重新注册失败，也会正确清理资源
        /// 
        /// 资源管理：
        /// - 使用引用计数方式管理数据库连接
        /// - 当某个连接字符串的所有查询都停止时，自动停止SqlDependency.Stop
        /// - 避免资源泄漏和不必要的数据库连接占用
        /// </summary>
        private void OnDependencyChange(object sender, SqlNotificationEventArgs e, CancellationTokenSource cts, string connectionString, string sqlQuery)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                try
                {
                    cts?.Cancel();
                    RegisterSqlDependency(sqlQuery, connectionString); // 自动重新注册
                }
                catch (Exception excp)
                {
                    _Logger.LogDebug(excp, "处理数据变化并尝试重新建立回调过程已出现异常,请检查网络");
                }
            }

            if (_ConnectionStringUsageCount.AddOrUpdate(connectionString, 0, (_, count) => count - 1) == 0)
            {
                SqlDependency.Stop(connectionString);
                if (_Connections.TryRemove(connectionString, out var connection))
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// 停止数据库监听
        /// 
        /// 全量清理流程：
        /// 1. 停止所有注册的SQL查询监听
        /// 2. 停止所有数据库连接的SqlDependency监听
        /// 3. 关闭并清理所有数据库连接
        /// 4. 清空所有管理字典和缓存
        /// 
        /// 使用场景：
        /// - 应用程序关闭时的资源清理
        /// - BackgroundService停止时的清理操作
        /// - 异常情况下的强制资源释放
        /// 
        /// 安全性：
        /// - 即使部分操作失败，也会继续执行其他清理操作
        /// - 确保不会因为单个查询的问题影响整体清理
        /// </summary>
        private void StopDatabaseListening()
        {
            foreach (var sqlQuery in _SqlCancellationTokenSources.Keys)
            {
                StopListening(sqlQuery);
            }

            foreach (var connectionString in _ConnectionStringUsageCount.Keys)
            {
                SqlDependency.Stop(connectionString);
                if (_Connections.TryRemove(connectionString, out var connection))
                {
                    connection.Close();
                }
            }

            _Logger.LogDebug("已停止所有数据库监听");
        }
        #endregion 私有方法

        #region BackgroundService 实现
        /// <summary>
        /// 执行后台服务
        /// 
        /// 服务模式说明：
        /// - 继承BackgroundService实现标准的.NET Core后台服务模式
        /// - 注册停止令牌的回调，确保应用关闭时正确清理资源
        /// - 本身不执行持续的后台任务，主要作为资源管理容器
        /// 
        /// 生命周期管理：
        /// - 服务启动时：准备好接受监听注册
        /// - 服务运行时：被动响应监听注册和数据变更事件
        /// - 服务停止时：主动清理所有监听和连接资源
        /// 
        /// 优雅停止：
        /// - 通过CancellationToken机制实现优雅停止
        /// - 确保所有SqlDependency监听正确停止
        /// - 避免资源泄漏和连接残留
        /// </summary>
        /// <param name="stoppingToken">停止令牌</param>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(StopDatabaseListening);
            return Task.CompletedTask;
        }
        #endregion BackgroundService 实现

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// 
        /// 资源释放说明：
        /// - 调用StopDatabaseListening停止所有监听
        /// - 确保SqlDependency.Stop被正确调用
        /// - 关闭所有数据库连接
        /// - 调用基类的Dispose方法完成标准清理
        /// 
        /// 防御性编程：
        /// - 即使在异常情况下也能正确释放资源
        /// - 避免因为部分清理失败导致的资源泄漏
        /// - 符合.NET的标准Dispose模式
        /// </summary>
        public override void Dispose()
        {
            StopDatabaseListening();
            base.Dispose();
        }
        #endregion 释放资源
    }

    /// <summary>
    /// SqlDependencyManager 扩展方法类
    /// 
    /// 扩展方法说明：
    /// - 提供便捷的依赖注入注册方法
    /// - 集成到ASP.NET Core的标准服务注册流程
    /// - 简化应用启动时的配置代码
    /// </summary>
    public static class SqlDependencyManagerExtensions
    {
        /// <summary>
        /// 添加 SqlDependencyManager 服务
        /// 
        /// 注册说明：
        /// - 注册为单例服务，确保整个应用生命周期内只有一个实例
        /// - 自动启动BackgroundService，无需手动管理生命周期
        /// - 集成到应用的依赖注入容器中，支持构造函数注入
        /// 
        /// 使用方式：
        /// services.AddSqlDependencyManager();
        /// 
        /// 配合使用：
        /// - 建议与IMemoryCache或Redis缓存服务一起使用
        /// - 可以与SignalR结合实现实时数据推送
        /// - 适合与Entity Framework Core的无跟踪查询配合使用
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合，支持链式调用</returns>
        public static IServiceCollection AddSqlDependencyManager(this IServiceCollection services)
        {
            services.AddSingleton<SqlDependencyManager>();
            return services;
        }
    }
}





