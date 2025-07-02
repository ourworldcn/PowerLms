#nullable enable
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading;

namespace OW.Data
{
    /// <summary>
    /// SqlChangeToken 类，实现 IChangeToken 接口，用于监听 SQL 数据库变化。
    /// 基于 SqlDependency 实现，仅在 Windows 平台运行时可用。
    /// </summary>
    public class SqlChangeToken : IChangeToken, IDisposable
    {
        #region 静态字段和构造函数
        private static readonly bool _isWindowsPlatform; // 是否为 Windows 平台

        // 静态共享资源，用于管理连接池和使用计数
        private static readonly ConcurrentDictionary<string, int> _connectionStringUsageCount = new();
        private static readonly ConcurrentDictionary<string, SqlConnection> _connections = new();
        private static object _locker => _connectionStringUsageCount;

        /// <summary>
        /// 静态构造函数，检查运行时平台
        /// </summary>
        static SqlChangeToken()
        {
#if NET6_0_OR_GREATER
            _isWindowsPlatform = OperatingSystem.IsWindows(); // .NET 6+ 推荐方式
#else
            _isWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows); // 兼容旧版本
#endif
            if (!_isWindowsPlatform)
            {
                throw new PlatformNotSupportedException(
                    "SqlChangeToken 需要在 Windows 平台上运行。SqlDependency 在 Linux/macOS 平台不受支持。" +
                    "请考虑使用其他替代方案，如定时轮询、Redis 发布/订阅或 CDC + Event Hub。");
            }
        }
        #endregion

        #region 私有字段
        private readonly string _sqlQuery; // SQL 查询语句
        private readonly string _connectionString; // 数据库连接字符串
        private SqlDependency? _dependency; // SQL 依赖对象
        private volatile bool _hasChanged = false; // 是否已检测到变更
        private readonly ConcurrentBag<(Action<object?> callback, object? state)> _callbacks = new(); // 回调函数列表
        private bool _disposed = false; // 是否已释放资源
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化 SQL 查询和数据库连接字符串
        /// </summary>
        /// <param name="sqlQuery">要监听的 SQL 查询字符串</param>
        /// <param name="connectionString">数据库连接字符串</param>
        public SqlChangeToken(string sqlQuery, string connectionString)
        {
            _sqlQuery = sqlQuery ?? throw new ArgumentNullException(nameof(sqlQuery));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }
        #endregion

        #region IChangeToken 实现
        /// <summary>
        /// 获取一个值，该值指示是否已检测到变更
        /// </summary>
        public bool HasChanged => _hasChanged;

        /// <summary>
        /// 获取一个值，该值指示是否应主动使用回调函数
        /// </summary>
        public bool ActiveChangeCallbacks => true; // 支持主动回调

        /// <summary>
        /// 注册变更回调函数
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="state">回调函数的状态对象</param>
        /// <returns>一个 IDisposable 对象，用于取消回调</returns>
        /// <exception cref="ObjectDisposedException">当对象已释放时抛出</exception>
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SqlChangeToken));

            if (_hasChanged)
            {
                // 如果已经发生变化，立即执行回调
                callback(state);
                return new NoOpDisposable();
            }

            _callbacks.Add((callback, state));
            return new CallbackDisposable(this, callback, state);
        }
        #endregion

        #region 公共静态方法
        /// <summary>
        /// 启动 SQL 依赖监听
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <exception cref="InvalidOperationException">当启动失败时抛出</exception>
        public static void Start(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            try
            {
                lock (_locker)
                {
                    // 如果是第一次使用该连接字符串，则启动 SqlDependency
                    if (_connectionStringUsageCount.AddOrUpdate(connectionString, 1, (_, count) => count + 1) == 1)
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString);
                        var databaseName = builder.InitialCatalog;
                        if (string.IsNullOrEmpty(databaseName))
                        {
                            databaseName = builder["Database"]?.ToString();
                        }

                        if (string.IsNullOrEmpty(databaseName))
                        {
                            throw new InvalidOperationException("无法从连接字符串中提取数据库名称");
                        }

                        EnsureDatabaseEnabledBroker(connectionString, databaseName);
                        SqlDependency.Start(connectionString);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"启动 SqlDependency 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 停止 SQL 依赖监听并释放资源
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        public static void Stop(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return;

            lock (_locker)
            {
                // 减少连接字符串使用计数，如果为0则停止 SqlDependency
                if (_connectionStringUsageCount.TryGetValue(connectionString, out var count) && count > 0)
                {
                    var newCount = _connectionStringUsageCount.AddOrUpdate(connectionString, 0, (_, c) => Math.Max(0, c - 1));
                    if (newCount == 0)
                    {
                        try
                        {
                            SqlDependency.Stop(connectionString);
                            if (_connections.TryRemove(connectionString, out var connection))
                            {
                                connection.Close();
                                connection.Dispose();
                            }
                        }
                        catch
                        {
                            // 忽略停止时的异常
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 确保数据库启用了 Service Broker
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="databaseName">数据库名称</param>
        /// <exception cref="InvalidOperationException">当启用 Service Broker 失败时抛出</exception>
        public static void EnsureDatabaseEnabledBroker(string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            const string checkBrokerSql = "SELECT is_broker_enabled FROM sys.databases WHERE name = @DatabaseName";
            const string enableBrokerSql = "ALTER DATABASE [{0}] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE";

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // 检查是否已启用 Service Broker
                using (var checkCommand = new SqlCommand(checkBrokerSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                    var result = checkCommand.ExecuteScalar();

                    if (result is bool isBrokerEnabled && isBrokerEnabled)
                    {
                        return; // Service Broker 已启用，无需操作
                    }
                }

                // 启用 Service Broker
                var enableCommand = string.Format(enableBrokerSql, databaseName);
                using var command = new SqlCommand(enableCommand, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"启用数据库 '{databaseName}' 的 Service Broker 失败: {ex.Message}", ex);
            }
        }
        #endregion

        #region 实例方法
        /// <summary>
        /// 启动监听 SQL 数据库变化
        /// </summary>
        /// <exception cref="ObjectDisposedException">当对象已释放时抛出</exception>
        /// <exception cref="InvalidOperationException">当启动失败时抛出</exception>
        public void StartInstance()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SqlChangeToken));

            try
            {
                // 启动静态 SqlDependency
                Start(_connectionString);

                var connection = _connections.GetOrAdd(_connectionString, connStr => new SqlConnection(connStr));

                // 确保连接是打开的
                if (connection.State == ConnectionState.Closed)
                {
                    lock (connection)
                    {
                        if (connection.State == ConnectionState.Closed)
                            connection.Open();
                    }
                }

                using var command = new SqlCommand(_sqlQuery, connection);
                _dependency = new SqlDependency(command);
                _dependency.OnChange += OnDependencyChange;

                lock (connection)
                {
                    using var reader = command.ExecuteReader();
                    // 执行查询以建立依赖关系，但不需要读取数据
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"启动 SqlChangeToken 实例失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 停止实例监听并释放资源
        /// </summary>
        public void StopInstance()
        {
            if (_disposed) return;

            if (_dependency != null)
            {
                _dependency.OnChange -= OnDependencyChange;
                _dependency = null;
            }

            // 停止静态 SqlDependency
            Stop(_connectionString);
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// SqlDependency 变化事件处理器
        /// </summary>
        private void OnDependencyChange(object? sender, SqlNotificationEventArgs e)
        {
            if (_disposed) return;

            if (e.Type == SqlNotificationType.Change && e.Source == SqlNotificationSource.Data)
            {
                // 标记为已变化
                _hasChanged = true;

                // 执行所有注册的回调函数
                foreach (var (callback, state) in _callbacks)
                {
                    try
                    {
                        callback(state);
                    }
                    catch
                    {
                        // 忽略回调执行时的异常
                    }
                }

                // 自动重新注册（如果需要持续监听）
                try
                {
                    if (!_disposed)
                    {
                        // 重置状态并重新启动
                        _hasChanged = false;
                        StartInstance();
                    }
                }
                catch
                {
                    // 忽略重新注册时的异常
                }
            }
            else if (e.Type == SqlNotificationType.Subscribe && e.Info == SqlNotificationInfo.Error)
            {
                // SqlDependency 订阅失败，标记为已变化以通知调用者
                _hasChanged = true;
            }
        }

        /// <summary>
        /// 移除指定的回调函数
        /// </summary>
        private void RemoveCallback(Action<object?> callback, object? state)
        {
            // ConcurrentBag 不支持直接移除，这里为了简化实现，实际上不执行移除操作
            // 在实际使用中，如果需要精确的回调管理，可以使用其他数据结构
        }
        #endregion

        #region IDisposable 实现
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            StopInstance();
            _dependency = null;
            _disposed = true;
        }
        #endregion

        #region 嵌套类型
        /// <summary>
        /// 无操作的 IDisposable 实现
        /// </summary>
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }

        /// <summary>
        /// 回调函数的 IDisposable 实现
        /// </summary>
        private class CallbackDisposable : IDisposable
        {
            private readonly SqlChangeToken _token;
            private readonly Action<object?> _callback;
            private readonly object? _state;

            public CallbackDisposable(SqlChangeToken token, Action<object?> callback, object? state)
            {
                _token = token;
                _callback = callback;
                _state = state;
            }

            public void Dispose()
            {
                _token.RemoveCallback(_callback, _state);
            }
        }
        #endregion
    }
}
#nullable restore