/*
 * 文件名：SqlChangeToken.cs
 * 作者：OW
 * 创建日期：2023年10月25日
 * 描述：该文件包含 SqlChangeToken 类的实现，用于监测 SQL 数据库变化。
 */

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace OW.Data
{
    /// <summary>
    /// SqlChangeToken 类，实现 IChangeToken 接口，用于监测 SQL 数据库变化。
    /// </summary>
    public class SqlChangeToken : IChangeToken
    {
        private readonly string _sqlQuery;
        private readonly string _connectionString;
        private readonly ILogger<SqlChangeToken> _logger;
        private SqlDependency _dependency;
        private CancellationTokenSource _cts;
        private static readonly ConcurrentDictionary<string, int> _connectionStringUsageCount = new();
        private static readonly ConcurrentDictionary<string, SqlConnection> _connections = new();
        private static readonly object _locker = new();

        /// <summary>
        /// 构造函数，初始化 SQL 查询和数据库连接字符串。
        /// </summary>
        /// <param name="sqlQuery">要监测的 SQL 查询字符串。</param>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <param name="logger">日志记录器，可选参数。</param>
        public SqlChangeToken(string sqlQuery, string connectionString, ILogger<SqlChangeToken> logger = null)
        {
            _sqlQuery = sqlQuery ?? throw new ArgumentNullException(nameof(sqlQuery));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        /// <summary>
        /// 开始监测 SQL 数据库变化。
        /// </summary>
        public void Start()
        {
            _cts = new CancellationTokenSource();
            lock (_locker)
            {
                if (_connectionStringUsageCount.AddOrUpdate(_connectionString, 1, (_, count) => count + 1) == 1)
                {
                    var builder = new SqlConnectionStringBuilder(_connectionString);
                    EnsureDatabaseEnabledBroker(_connectionString, builder.InitialCatalog ?? builder["Database"].ToString());
                    SqlDependency.Start(_connectionString);
                }
            }

            var connection = _connections.GetOrAdd(_connectionString, connStr => new SqlConnection(connStr));
            if (connection.State == ConnectionState.Closed)
            {
                lock (connection)
                    connection.Open();
            }
            using var command = new SqlCommand(_sqlQuery, connection);
            _dependency = new SqlDependency(command);
            _dependency.OnChange += OnDependencyChange;

            try
            {
                lock (connection)
                {
                    command.ExecuteReader();
                    //using var reader = command.ExecuteReader();
                    //while (reader.Read()) { }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行 SqlCommand 时出错。");
                connection.Close();
                throw;
            }
        }

        /// <summary>
        /// 确保数据库启用了 Broker。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <param name="databaseName">数据库名称。</param>
        private static void EnsureDatabaseEnabledBroker(string connectionString, string databaseName)
        {
            const string enableBrokerFormatString = "USE master; \r\n IF EXISTS (SELECT is_broker_enabled FROM sys.databases WHERE name = '{0}' AND is_broker_enabled = 0)\r\nBEGIN\r\n    ALTER DATABASE [{0}] SET ENABLE_BROKER;\r\nEND\r\n";
            const string enableBroker = "ALTER DATABASE {0} SET NEW_BROKER WITH ROLLBACK IMMEDIATE;";
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            var commandText = string.Format(enableBrokerFormatString, databaseName);
            using var command = new SqlCommand(commandText, connection);
            command.ExecuteNonQuery();

            commandText = string.Format(enableBroker, databaseName);
            using var command2 = new SqlCommand(commandText, connection);
            command2.ExecuteNonQuery();

            commandText = string.Format("GRANT SUBSCRIBE QUERY NOTIFICATIONS TO {0};", "zc");
            using var command3 = new SqlCommand(commandText, connection);
            //command3.ExecuteNonQuery();
        }

        /// <summary>
        /// SqlDependency 变化事件处理。
        /// </summary>
        private void OnDependencyChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                try
                {
                    _cts?.Cancel();
                    _logger?.LogDebug("结果集发生变化，已触发回调函数。");
                    Start(); // 自动重注册
                }
                catch (Exception excp)
                {
                    _logger?.LogDebug(excp, "结果集发生变化，已触发回调函数,但发生错误。");
                }
            }

            lock (_locker)
            {
                if (_connectionStringUsageCount.AddOrUpdate(_connectionString, 0, (_, count) => count - 1) == 0)
                {
                    SqlDependency.Stop(_connectionString);
                    if (_connections.TryRemove(_connectionString, out var connection))
                    {
                        connection.Close();
                    }
                }
            }
        }

        #region IChangeToken 实现
        /// <summary>
        /// 获取一个值，该值指示是否已检测到更改。
        /// </summary>
        public bool HasChanged => _cts?.IsCancellationRequested ?? false;

        /// <summary>
        /// 获取一个值，该值指示是否应主动使用回调。
        /// </summary>
        public bool ActiveChangeCallbacks => true;

        /// <summary>
        /// 注册更改回调。
        /// </summary>
        /// <param name="callback">回调函数。</param>
        /// <param name="state">回调函数的状态对象。</param>
        /// <returns>一个 IDisposable 对象，用于取消回调。</returns>
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return _cts.Token.Register(callback, state);
        }
        #endregion
    }
}

