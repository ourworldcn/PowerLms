# SqlDependency 详解（.NET 6 / EF Core 6 场景适用）

## 1. 技术定位
SqlDependency 属于 **SQL Server Query Notification**（查询通知）客户端封装，借助数据库端的 **Service Broker** 把“结果集是否发生变化”事件推送到应用进程。典型用途：

1. ASP.NET / Windows Service 缓存过期通知  
2. 桌面程序实时刷新列表  
3. 轻量级发布–订阅（Pub/Sub）

> ⚠️ 仅能检测“与指定 SQL 结果集相关的行数据是否改变”，而不是返回更改内容；复杂变更场景需用 CDC / Debezium 等。

## 2. 运行机制

1. `SqlDependency.Start(connectionString)`  
   - 在本机注册静态侦听端口（SqlClient 监听服务），  
   - 解析连接字符串，保持到数据库的控制连接。

2. **注册查询**  
   - 创建 `SqlCommand` 并附加 `SqlDependency dependency = new(cmd);`  
   - 首次 `ExecuteReader()` 时，SqlClient 会向 SQL Server 发送 `sp_executesql` 带 `WITH (RECOMPILE, QUERYTRACEON 4199)` 等注解，声明需要“查询通知”。  

3. **数据库端**  
   - Query Notification Manager (QDS) 解析查询→检查是否可通知→在 Service Broker 队列 `AspNet_SqlDependency_Queue`（默认）创建一条订阅。  
   - 当表数据发生 Insert/Update/Delete 并满足命中条件，触发器向队列写入消息。

4. **Service Broker 通道**  
   - 队列 → 对话 → 发送到监听端口（TCP 4022 等，随配置）。

5. **客户端回调**  
   - 底层侦听器收到消息 → 解析→ 触发 `dependency.OnChange += (s,e)=>{ ... }`  
   - `e.Info`（Insert/Delete/Update/Expired），`e.Source`（Data/Timeout）。

6. **重新订阅**  
   - 通知只触发一次；在回调里必须 **重新执行查询** 以续期。

## 3. 环境准备

### 3.1 启用 Service Broker

```sql
-- 仅数据库级别；需独占模式
ALTER DATABASE MyDB SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE;
```

若多租户环境无法 `ENABLE_BROKER`：可为应用单独划分数据库。

### 3.2 权限

```sql
GRANT SUBSCRIBE QUERY NOTIFICATIONS TO [AppUser];
GRANT RECEIVE ON QueryNotificationErrorsQueue TO [AppUser];
```

> 若使用托管身份 (Azure) 或 AD 账户，同理授权。

### 3.3 防火墙

默认侦听本地端口（随机高位），可通过  
`SqlDependency.Start(conn, queue: null, listenAddress: "tcp://0.0.0.0:42425")`  
自定义；务必放行入站 TCP。

## 4. 基础代码示例 (.NET 6)

```csharp
using Microsoft.Data.SqlClient;

const string connStr =
    "Data Source=.;Initial Catalog=MyDB;Integrated Security=True;MultipleActiveResultSets=True";

public class Notifier : IDisposable
{
    public event Action? DataChanged;
    private SqlConnection? _conn;
    private SqlCommand? _cmd;
    private SqlDependency? _dep;

    public void Start()
    {
        SqlDependency.Start(connStr);          // 1. 全局初始化
        _conn = new SqlConnection(connStr);
        _cmd  = new SqlCommand(
            "SELECT Id, Name, Qty FROM dbo.Stock", _conn); // 2. 查询需显列名
        Register();                                       // 3. 首次订阅
    }

    private void Register()
    {
        _dep?.Dispose();
        _dep = new SqlDependency(_cmd);
        _dep.OnChange += OnChange;          // 4. 绑定事件

        _conn!.Open();
        using var rdr = _cmd!.ExecuteReader(); // 5. 执行查询 -> server 创建订阅
        while (rdr.Read()) { /* preload cache */ }
        _conn.Close();
    }

    private void OnChange(object? sender, SqlNotificationEventArgs e)
    {
        // 6. 一次性 → 立即续订
        Register();
        Console.WriteLine($"变更类型:{e.Info}, 来源:{e.Source}");
        DataChanged?.Invoke();
    }

    public void Dispose()
    {
        _dep?.Dispose();
        SqlDependency.Stop(connStr);
        _conn?.Dispose();
    }
}
```

## 5. 可通知查询规则

| 限制 | 说明 |
|------|------|
| 必须两级对象名 | `dbo.Table`，不能使用 `*`，必须显列 |
| 禁用 `SELECT INTO`、`DISTINCT`、`TOP (expr)`、`GROUP BY`、`HAVING`、`UNION`、`JOIN` 中的子查询聚合 | 破坏可追踪性 |
| 不得访问临时表、表变量、视图含不允许元素 | View 内部同样受控 |
| 必须 `SET NOCOUNT ON;`（SqlClient 自动加） | |
| `READ COMMITTED` 或更高 | `NOLOCK` 会被拒绝 |
| 结果集 ≤ 8KB 行大小 | 超出则不缓存 → 无法通知 |
| 使用数据库表或 Indexed View | 字段必须可索引 |

若查询不合法，`OnChange` 将返回 `e.Type = Subscribe | e.Info = Error`.

## 6. 性能与容量

1. **订阅数**：单库上限 ~2⁵³（实际受内存 / 事务日志影响）。  
2. **每次 DML**：若涉及 10 张表，将对其全部相关订阅 _逐一_ 发送消息；写放大明显。  
3. **CPU**：Service Broker 启用后，占用额外 “BROKER_RECEIVE_WAITFOR” 线程；并发高时需增配。  
4. 建议 **批量合并查询**，或使用 **缓存分区** + **定时轮询** 混合方案。

## 7. 与 EF Core 6 集成思路

EF Core 无原生封装；常见做法：

1. 订阅层（SqlDependency）＋MemoryCache/Redis 标记失效。  
2. 当 `OnChange` 触发 → 清理指定 cache key → 上层重新 `context.Set<TEntity>().AsNoTracking().ToListAsync()`。  
3. 避免把 DbContext 连接交给 SqlDependency（连接池会断开订阅）：  
   - 使用 **独立 SqlConnection**；  
   - EF Core 仍走自己的连接池。

## 8. 局限与坑

| 类型 | 说明 |
|------|------|
| 跨平台 | `.NET 6` 下使用 `Microsoft.Data.SqlClient`，**SqlDependency 只在 Windows 支持**（Linux 无 Service Broker 侦听器实现）。 |
| 延时 | 消息在事务提交后才到达客户端；未提交事务更改不会触发。 |
| 队列阻塞 | 若应用宕机未及时`END CONVERSATION`，队列增长；需定期 `ALTER QUEUE WITH STATUS = ON` 清理。 |
| 连接掉线 | 数据库重启或网络闪断需要重启订阅；务必在 `OnChange` 里 `try…catch` 并 `Start/Stop` 重连。 |
| 安全 | Service Broker 监听本机端口；若部署云环境需限制入站。 |

## 9. 替代方案

| 场景 | 建议 |
|------|------|
| 高吞吐实时流 | CDC ➜ Debezium ➜ Kafka / EventHub |
| Azure SQL / 跨平台 | Azure SQL Change Data Capture + Event Grid |
| SQL Server 低版本 / 无法启用 Broker | 輪詢 `rowversion` 字段 |
| Redis 缓存 | key 过期 + Pub/Sub 通知 |

---

### 参考链接

- Microsoft Docs – [Query Notifications](https://learn.microsoft.com/sql/relational-databases/notifications/query-notifications)
- Microsoft Docs – [Service Broker](https://learn.microsoft.com/sql/database-engine/configure-windows/service-broker)
- GitHub – [dotnet/SqlClient#208](https://github.com/dotnet/SqlClient/issues/208) （跨平台讨论）