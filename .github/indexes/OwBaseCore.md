# 📦 OwBaseCore 项目索引

> **项目路径**: `C:\Users\zc-home\source\ourworldcn\Bak\OwBaseCore\`  
> **项目类型**: .NET 6 类库  
> **角色定位**: PowerLms 架构的**核心基础设施层**，提供跨项目复用的通用工具和领域驱动设计(DDD)组件  
> **依赖关系**: 被 OwDbBase、PowerLmsServer、PowerLmsData 等上层项目引用

---

## 🎯 项目概述

OwBaseCore 是 PowerLms 解决方案的基础设施核心，提供：
- **DDD 基础组件**：实体基类、值对象、聚合根、仓储接口、事件总线
- **高性能集合**：并发安全集合、对象池化列表、优化字典
- **依赖注入增强**：自动注入特性、服务容器检查
- **缓存扩展**：优先级驱逐、取消令牌管理
- **通用工具**：字符串处理、类型转换、表达式树、流封装

---

## 📁 目录结构概览

```
OwBaseCore/
├── DDD/                          # 领域驱动设计组件
│   ├── Seedwork/                 # DDD 基石模式
│   ├── OwEventBus.cs             # 事件总线
│   ├── OwCommand.cs              # 命令模式基础
│   └── SyncCommandManager.cs     # 同步命令管理器
│
├── Microsoft.Extensions.*/       # .NET 扩展增强
│   ├── Caching.Memory/           # 内存缓存扩展
│   ├── DependencyInjection/      # 依赖注入增强
│   └── ObjectPool/               # 对象池扩展
│
├── System.Collections.*/         # 集合增强
│   ├── Generic/                  # 泛型集合扩展
│   ├── Concurrent/               # 并发集合
│   └── (其他System.Collections.*)# 集合工具类
│
├── System.*/                     # 系统类型扩展
│   ├── System.cs                 # 系统基础扩展
│   ├── System.Text.Json.*/       # JSON序列化扩展
│   ├── System.IO/                # IO流扩展
│   ├── System.Threading/         # 线程工具
│   └── System.Net.Sockets/       # 网络Socket工具
│
├── Server/                       # 服务器组件
│   └── OwScheduler.cs            # 调度器
│
├── OwHelper.cs                   # 核心静态工具类
├── OwConvert.cs                  # 类型转换工具
├── OwStringUtils.cs              # 字符串工具
├── StringUtils.cs                # 字符串辅助工具
├── DisposerWrapper.cs            # 释放包装器
└── OwObservableBase.cs           # 可观察对象基类
```

---

## 🔧 核心模块详解

### 1️⃣ DDD 基础设施 (`DDD/`)

#### 📌 **Seedwork 模式** (`DDD/Seedwork/`)
DDD 基石模式，定义领域对象的基本契约：

| 文件 | 类型 | 用途 |
|------|------|------|
| `EntityBase.cs` | 抽象类 | 实体基类，封装 Id、相等性比较 |
| `ValueObject.cs` | 抽象类 | 值对象基类，基于属性值比较 |
| `IAggregateRoot.cs` | 接口 | 聚合根标记接口 |
| `IRepository.cs` | 接口 | 仓储接口定义 |
| `Command.cs` | 抽象类 | 命令模式基类 |
| `Base.cs` | 基础类 | DDD 基础类型定义 |

**关键设计**：
- ✅ 实体使用 `Guid` 作为唯一标识
- ✅ 值对象通过属性值判断相等性
- ✅ 聚合根控制事务边界
- ✅ 仓储模式封装数据访问

#### 📌 **事件总线** (`OwEventBus.cs`)
轻量级进程内事件总线，支持发布-订阅模式：

```csharp
// 核心接口
public interface INotification { }
public interface INotificationHandler { void Handle(object data); }
public interface INotificationHandler<T> : INotificationHandler where T : INotification
{
    void Handle(T data);
}

// 使用示例
public class OrderCreatedEvent : INotification { /* 事件数据 */ }
public class OrderCreatedHandler : INotificationHandler<OrderCreatedEvent>
{
    public void Handle(OrderCreatedEvent data) { /* 处理逻辑 */ }
}

// 注册和使用
services.AddOwEventBus();
services.RegisterNotificationHandler(assemblies);
eventBus.Add(new OrderCreatedEvent());
eventBus.Raise(); // 触发所有处理器
```

**特点**：
- ✅ **单进程内**：不跨服务器边界
- ✅ **并发队列**：`ConcurrentQueue` 存储事件
- ✅ **自动发现**：通过反射注册处理器
- ✅ **异常隔离**：单个处理器异常不影响其他

#### 📌 **命令管理** (`OwCommand.cs`, `SyncCommandManager.cs`)
命令模式基础设施，支持同步命令调度。

---

### 2️⃣ 依赖注入增强 (`Microsoft.Extensions.DependencyInjection/`)

#### 📌 **自动注入特性** (`OwAutoInjection.cs`)

**核心特性**：
```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class OwAutoInjectionAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; set; } // 生命周期
    public bool AutoCreateFirst { get; set; }     // 是否自动创建首个实例
    // ...
}
```

**使用示例**：
```csharp
// ✅ 单例服务 - 全局唯一实例
[OwAutoInjection(ServiceLifetime.Singleton)]
public class CaptchaManager { }

// ✅ 范围服务 - 每次请求创建新实例
[OwAutoInjection(ServiceLifetime.Scoped)]
public class OwContext { }

// ✅ 自动创建首个实例
[OwAutoInjection(ServiceLifetime.Scoped, AutoCreateFirst = true)]
public class OwSqlAppLogger { }
```

**自动注册机制**：
- 扫描程序集，查找带 `[OwAutoInjection]` 特性的类
- 根据 `Lifetime` 自动注册到 DI 容器
- 支持 `AutoCreateFirst` 在应用启动时创建实例

#### 📌 **服务容器检查** (`ServiceProviderChecker.cs`)
提供服务注册验证和诊断工具。

---

### 3️⃣ 缓存扩展 (`Microsoft.Extensions.Caching.Memory/`)

#### 📌 **缓存扩展工具** (`OwCacheExtensions.cs`)

**核心功能**：
```csharp
public static class OwCacheExtensions
{
    // ✅ 从实体ID生成缓存键
    public static string GetCacheKeyFromId<T>(Guid id);
    
    // ✅ 从缓存键解析实体ID
    public static Guid? GetIdFromCacheKey(string cacheKey);
    
    // ✅ 启用优先级驱逐回调（自动注册CTS）
    public static void EnablePriorityEvictionCallback(this ICacheEntry entry, IMemoryCache cache);
    
    // ✅ 获取缓存项的取消令牌源（用于手动失效）
    public static CancellationTokenSource GetCancellationTokenSource(this IMemoryCache cache, string key);
}
```

**使用场景**：
```csharp
// 1. 生成缓存键
var cacheKey = OwCacheExtensions.GetCacheKeyFromId<Account>(userId);

// 2. 配置缓存项
var entry = _cache.CreateEntry(cacheKey);
entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));
entry.EnablePriorityEvictionCallback(_cache); // ✅ 自动管理CTS

// 3. 手动失效缓存
var cts = _cache.GetCancellationTokenSource(cacheKey);
if (cts != null) cts.Cancel();
```

**设计优势**：
- ✅ **统一键格式**：`{TypeName}.{Guid}`
- ✅ **自动CTS管理**：避免内存泄漏
- ✅ **优先级驱逐**：LRU 策略
- ✅ **级联失效**：支持依赖令牌

#### 📌 **数据对象缓存** (`DataObjectCache.cs`)
提供业务对象的缓存封装。

#### 📌 **内存缓存增强** (`OwMemoryCache.cs`)
提供额外的缓存管理功能。

---

### 4️⃣ 高性能集合 (`System.Collections.*/`)

#### 📌 **并发哈希集** (`ConcurrentHashSet.cs`)
线程安全的 HashSet 实现：
```csharp
public class ConcurrentHashSet<T> : ICollection<T>
{
    private readonly ConcurrentDictionary<T, byte> _dictionary;
    
    public bool Add(T item) => _dictionary.TryAdd(item, 0);
    public bool Contains(T item) => _dictionary.ContainsKey(item);
    // ...
}
```

**使用场景**：
- 多线程环境下的去重集合
- 并发订阅者列表
- 活跃连接跟踪

#### 📌 **对象池化列表** (`PooledList.cs`, `PooledListBase.cs`)
基于 `ObjectPool` 的高性能列表：
```csharp
public class PooledList<T> : PooledListBase<T>
{
    // ✅ 使用对象池减少 GC 压力
    // ✅ 适用于频繁创建/销毁的临时列表
}
```

**应用场景**：
- 热路径上的临时集合
- 批量操作的中间结果
- 高频查询的结果缓冲

#### 📌 **并发字典扩展** (`ConcurrentDictionaryExtensions.cs`)
提供并发字典的增强方法。

#### 📌 **优化字典** (`SingletonOptimizedDictionary.cs`)
针对单例模式优化的字典实现。

#### 📌 **原子操作** (`OwAtomic.cs`)
提供原子性数据结构。

#### 📌 **弱引用表** (`FirstKeyOptimizedWeakTable.cs`)
基于首键优化的弱引用表。

#### 📌 **字典工具** (`DictionaryUtil.cs`, `DictionaryExtensions.cs`, `StringDictionaryExtensions.cs`)
字典操作的扩展方法集合。

---

### 5️⃣ 线程与并发 (`System.Threading/`)

#### 📌 **单例锁** (`SingletonLocker.cs`)
全局单例锁管理器：
```csharp
public static class SingletonLocker
{
    public static bool TryEnter(string key, TimeSpan timeout);
    public static void Exit(string key);
}
```

**使用场景**：
```csharp
// AccountManager 中的使用示例
using var dw = Lock(userId.ToString(), Timeout.InfiniteTimeSpan);
// 临界区代码，确保同一用户的操作串行化
```

#### 📌 **键锁** (`KeyLocker.cs`)
提供基于键的细粒度锁。

#### 📌 **任务调度器** (`TaskDispatcher.cs`)
高级任务调度和排队机制：
```csharp
public class TaskDispatcher
{
    public TaskDispatcher(TaskDispatcherOptions options);
    public void Enqueue(string key, Action action); // 按键排队执行
}
```

**设计模式**：
- ✅ **按键排队**：同一键的任务串行执行
- ✅ **异步执行**：后台线程处理
- ✅ **取消支持**：响应应用关闭信号

---

### 6️⃣ 对象池 (`Microsoft.Extensions.ObjectPool/`)

#### 📌 **自动清理池** (`AutoClearPool.cs`)
提供自动清理机制的对象池实现。

---

### 7️⃣ 字符串与类型转换

#### 📌 **字符串工具** (`OwStringUtils.cs`, `StringUtils.cs`)

**核心功能**：
```csharp
public static class OwStringUtils
{
    // ✅ 生成随机密码
    public static string GeneratePassword(int length);
    
    // ✅ 字符串格式化（支持模板变量）
    public static string FormatWith(this string template, IDictionary<string, string> values);
}
```

**使用示例**：
```csharp
// 1. 生成密码
var pwd = OwStringUtils.GeneratePassword(6); // "aB3!xY"

// 2. 模板格式化
var template = "用户:{LoginName}({CompanyName}){OperationType}成功";
var values = new Dictionary<string, string>
{
    { "LoginName", "admin" },
    { "CompanyName", "北京公司" },
    { "OperationType", "登录" }
};
var result = template.FormatWith(values); // "用户:admin(北京公司)登录成功"
```

#### 📌 **类型转换** (`OwConvert.cs`)
提供安全的类型转换工具。

---

### 8️⃣ IO 流扩展 (`System.IO/`)

#### 📌 **流包装器** (`WrapperStream.cs`)
提供流的包装和扩展功能。

#### 📌 **内存流池** (`MemoryStreamPool.cs`)
池化 MemoryStream 减少 GC 压力。

---

### 9️⃣ 网络与Socket (`System.Net.Sockets/`)

#### 📌 **RDM 服务器/客户端** (`OwRdmServer.cs`, `OwRdmClient.cs`)
可靠数据报协议(RDM)的实现。

---

### 🔟 其他核心工具

#### 📌 **OwHelper** (`OwHelper.cs`)
核心静态工具类：
```csharp
public static class OwHelper
{
    public static DateTime WorldNow { get; }      // 世界标准时间
    public static Random Random { get; }          // 线程安全随机数
    
    // ✅ 树遍历工具
    public static IEnumerable<T> GetAllSubItemsOfTree<T>(
        IEnumerable<T> roots, 
        Func<T, IEnumerable<T>> getChildren);
    
    // ✅ 错误处理
    public static void SetLastError(int errorCode);
    public static void SetLastErrorAndMessage(int errorCode, string message);
}
```

**使用场景**：
```csharp
// 1. 获取组织树的所有子节点
var allOrgs = OwHelper.GetAllSubItemsOfTree(
    new[] { rootOrg }, 
    o => o.Children);

// 2. 设置业务错误码
if (duplicateFound)
{
    OwHelper.SetLastErrorAndMessage(409, "登录名已存在");
    return false;
}
```

#### 📌 **表达式树工具** (`System.Linq.Expressions/OwExpression.cs`)
提供表达式树构建和优化工具。

#### 📌 **JSON 转换器** (`System.Text.Json.Serialization/Converter.cs`)
自定义 JSON 序列化转换器。

#### 📌 **可观察对象** (`OwObservableBase.cs`)
实现 `INotifyPropertyChanged` 的基类。

#### 📌 **释放包装器** (`DisposerWrapper.cs`)
封装 IDisposable 模式的辅助类。

---

## 📦 NuGet 依赖

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="6.0.3" />
  <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="6.0.1" />
  <PackageReference Include="Microsoft.Extensions.ObjectPool" Version="6.0.36" />
</ItemGroup>
```

---

## 🎯 核心设计理念

### 1️⃣ **DDD 领域驱动**
- ✅ 提供 Seedwork 基础模式
- ✅ 实体、值对象、聚合根清晰分离
- ✅ 仓储和命令模式标准化

### 2️⃣ **高性能优先**
- ✅ 对象池化减少 GC 压力
- ✅ 并发集合支持多线程场景
- ✅ 缓存扩展提升热路径性能

### 3️⃣ **依赖注入优先**
- ✅ `[OwAutoInjection]` 自动注册
- ✅ 支持 Singleton/Scoped/Transient
- ✅ 集成 .NET DI 容器

### 4️⃣ **线程安全**
- ✅ 单例锁管理
- ✅ 并发集合
- ✅ 任务调度器

### 5️⃣ **可扩展性**
- ✅ 扩展方法模式
- ✅ 接口驱动设计
- ✅ 事件总线解耦

---

## 📋 使用指南

### 典型应用场景

#### 1️⃣ **定义领域实体**
```csharp
using OW.DDD.Seedwork;

public class Order : EntityBase, IAggregateRoot
{
    public string OrderNo { get; set; }
    public decimal TotalAmount { get; set; }
    // ...
}
```

#### 2️⃣ **实现事件处理**
```csharp
public class OrderCreatedEvent : INotification
{
    public Guid OrderId { get; set; }
}

public class OrderCreatedHandler : INotificationHandler<OrderCreatedEvent>
{
    public void Handle(OrderCreatedEvent data)
    {
        // 发送通知、更新缓存等
    }
}
```

#### 3️⃣ **使用缓存扩展**
```csharp
var cacheKey = OwCacheExtensions.GetCacheKeyFromId<Order>(orderId);
var order = _cache.GetOrCreate(cacheKey, entry =>
{
    entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
    entry.EnablePriorityEvictionCallback(_cache);
    return LoadOrderFromDb(orderId);
});
```

#### 4️⃣ **并发安全集合**
```csharp
private readonly ConcurrentHashSet<Guid> _activeConnections = new();

public void AddConnection(Guid connectionId)
{
    _activeConnections.Add(connectionId);
}
```

#### 5️⃣ **任务调度**
```csharp
_taskDispatcher.Enqueue(userId.ToString(), () =>
{
    // 同一用户的操作串行执行
    SaveUserData(userId);
});
```

---

## ⚠️ 注意事项

### 1️⃣ **跨平台兼容性**
- ⚠️ 部分功能依赖 Windows（如 `System.Drawing`）
- ✅ 核心功能支持 Linux/macOS

### 2️⃣ **性能优化建议**
- ✅ 热路径使用 `PooledList` 减少分配
- ✅ 高频缓存启用优先级驱逐
- ✅ 并发场景使用 `Concurrent*` 集合

### 3️⃣ **内存管理**
- ✅ 缓存项使用 `EnablePriorityEvictionCallback` 自动管理 CTS
- ✅ 对象池主动归还资源
- ✅ 事件总线使用后调用 `Dispose()`

### 4️⃣ **线程安全**
- ✅ 跨线程操作使用 `SingletonLocker`
- ✅ 并发写入使用 `Concurrent*` 集合
- ✅ 任务调度通过 `TaskDispatcher` 排队

---

## 🔄 与上层项目的关系

```
OwBaseCore (基础设施)
    ↑ 引用
    |
    ├─ OwDbBase (数据库基础设施)
    ├─ PowerLmsData (数据层)
    ├─ PowerLmsServer (业务层)
    └─ PowerLmsWebApi (API层)
```

**关键依赖点**：
- `OwAutoInjection` → 所有 Manager 服务注册
- `OwCacheExtensions` → OrgManager、AccountManager 缓存管理
- `OwEventBus` → 领域事件发布/订阅
- `SingletonLocker` → 并发控制（如 AccountManager.Lock）
- `OwHelper` → 全局工具方法（如组织树遍历）

---

## 📊 统计信息

- **文件总数**: 约 48 个 C# 文件
- **核心模块**: 10 个（DDD、依赖注入、缓存、集合、线程等）
- **NuGet 包**: 3 个依赖
- **目标框架**: .NET 6.0

---

## 🔗 相关索引

- [OwDbBase 索引](OwDbBase.md) - 数据库基础设施
- [PowerLmsServer 索引](PowerLmsServer.md) - 业务层 Manager
- [项目架构总览](project-context.md) - 全局架构

---

**最后更新**: 2025-01-30  
**维护者**: AI 自动生成 + 人工校验  
**用途**: AI 上下文优化、开发者快速定位、新人入职引导
