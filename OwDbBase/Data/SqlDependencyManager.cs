/*
 * 文件名：SqlDependencyManager.cs
 * 作者：OW
 * 创建日期：2023年10月25日
 * 描述：该文件包含 SqlDependencyManager 服务的实现，用于监测一个 IQueryable<T> 的结果集变化。
 * 当前文件内容概述：
 * - SqlDependencyManager：用于监测一个 IQueryable<T> 的结果集变化。
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
    /// SqlDependency 管理器，用于监测一个 SQL 字符串的结果集变化。
    /// </summary>
    public class SqlDependencyManager : BackgroundService
    {
        #region 私有字段
        public const string EnableBrokerFormatString = "USE master; \r\n IF EXISTS (SELECT is_broker_enabled FROM sys.databases WHERE name = '{0}' AND is_broker_enabled = 0)\r\nBEGIN\r\n    ALTER DATABASE [{0}] SET ENABLE_BROKER;\r\nEND\r\n";
        private readonly ILogger<SqlDependencyManager> _Logger;
        private readonly ConcurrentDictionary<string, int> _ConnectionStringUsageCount = new();
        private readonly ConcurrentDictionary<string, SqlConnection> _Connections = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _SqlCancellationTokenSources = new();
        public object Locker => _ConnectionStringUsageCount;
        #endregion 私有字段

        #region 静态方法
        /// <summary>
        /// 启用 SQL Broker。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <param name="databaseName">数据库名称。</param>
        public static void EnableSqlBroker(string connectionString, string databaseName)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            var commandText = string.Format(EnableBrokerFormatString, databaseName);
            using var command = new SqlCommand(commandText, connection);
            command.ExecuteNonQuery();
        }

        static HashSet<string> _EnabledBrokerDatabases = new();
        /// <summary>
        /// 确保数据库启用了 Broker。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <param name="databaseName">数据库名称。</param>
        public static void EnsureDatabaseEnabledBroker(string connectionString, string databaseName)
        {
            if (_EnabledBrokerDatabases.Add(databaseName))
                EnableSqlBroker(connectionString, databaseName);
        }
        #endregion 静态方法

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public SqlDependencyManager(ILogger<SqlDependencyManager> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion 构造函数

        #region 公共方法
        /// <summary>
        /// 注册 SqlDependency 监听。注册的侦听会在结果集发生变化时自动重注册。
        /// </summary>
        /// <param name="sqlQuery">要监测的 SQL 查询字符串。</param>
        /// <param name="connectionString">要使用的数据库连接字符串。</param>
        /// <returns>一个可取消对象，在检测到变化时关闭。</returns>
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
            using var command = new SqlCommand(sqlQuery, connection);
            var dependency = new SqlDependency(command);
            dependency.OnChange += (sender, e) => OnDependencyChange(sender, e, cts, connectionString, sqlQuery);

            try
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                using var reader = command.ExecuteReader();
                // 读取数据以触发 SqlDependency
                while (reader.Read()) { }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "执行 SqlCommand 时出错。");
                connection.Close();
                throw;
            }

            _SqlCancellationTokenSources[sqlQuery] = cts;
            return cts;
        }

        /// <summary>
        /// 停止指定 SQL 查询的数据库侦听。
        /// </summary>
        /// <param name="sqlQuery">要停止侦听的 SQL 查询字符串。</param>
        public void StopListening(string sqlQuery)
        {
            if (_SqlCancellationTokenSources.TryRemove(sqlQuery, out var cts))
            {
                cts.Cancel();
                _Logger.LogDebug("已停止 SQL 查询 {SqlQuery} 的数据库侦听。", sqlQuery);
            }
        }
        #endregion 公共方法

        #region 私有方法
        /// <summary>
        /// SqlDependency 变化事件处理。
        /// </summary>
        private void OnDependencyChange(object sender, SqlNotificationEventArgs e, CancellationTokenSource cts, string connectionString, string sqlQuery)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                try
                {
                    cts?.Cancel();
                    RegisterSqlDependency(sqlQuery, connectionString); // 自动重注册
                }
                catch (Exception excp)
                {
                    _Logger.LogDebug(excp, "结果集发生变化，已触发回调函数,但发生错误。");
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
        /// 停止数据库侦听。
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

            _Logger.LogDebug("已停止所有数据库侦听。");
        }
        #endregion 私有方法

        #region BackgroundService 方法
        /// <summary>
        /// 执行后台任务。
        /// </summary>
        /// <param name="stoppingToken">停止令牌。</param>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(StopDatabaseListening);
            return Task.CompletedTask;
        }
        #endregion BackgroundService 方法

        #region 释放资源
        /// <summary>
        /// 释放资源。
        /// </summary>
        public override void Dispose()
        {
            StopDatabaseListening();
            base.Dispose();
        }
        #endregion 释放资源
    }

    /// <summary>
    /// SqlDependencyManager 扩展方法。
    /// </summary>
    public static class SqlDependencyManagerExtensions
    {
        /// <summary>
        /// 添加 SqlDependencyManager 服务。
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddSqlDependencyManager(this IServiceCollection services)
        {
            services.AddSingleton<SqlDependencyManager>();
            return services;
        }
    }
}





