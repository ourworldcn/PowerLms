# 📦 OwDbBase 项目索引

> **项目路径**: `C:\Users\zc-home\source\ourworldcn\Bak\OwDbBase\`  
> **项目类型**: .NET 6 类库  
> **角色定位**: **数据库基础设施层**，提供 EF Core 增强和数据访问通用能力  
> **依赖关系**: 依赖 OwBaseCore，被 PowerLmsData、PowerLmsServer 引用

---

## 🎯 项目概述

OwDbBase 是 PowerLms 解决方案的数据访问基础设施，提供：

- **EF Core 增强**: DbContext 扩展、批量写入、触发器支持
- **数据访问工具**: 通用查询、动态条件、数据单元处理
- **文件管理系统**: `OwFileService` - 企业级文件存储和权限控制 ⭐
- **基础数据类型**: `GuidKeyObjectBase`、`JsonDynamicPropertyBase`、树结构接口
- **批量操作**: 批量数据库写入器、数据转换工具

---

## 📁 项目结构

```
OwDbBase/
├── EntityFrameworkCore/          # EF Core 增强
│   ├── OwDbContext.cs            # 数据库上下文基类
│   ├── OwBatchDbWriter.cs        # 批量写入器
│   └── OwEfTriggers.cs           # EF 触发器
│
├── Data/                         # 数据访问基础
│   ├── GuidKeyObjectBase.cs      # Guid主键基类
│   ├── JsonDynamicPropertyBase.cs # JSON动态属性基类
│   ├── IDbTreeNode.cs            # 树结构接口
│   ├── IBeforeSave.cs            # 保存前回调接口
│   ├── OwDataUnit.cs             # 数据单元处理
│   └── SqlDependencyManager.cs   # SQL依赖管理
│
├── OwFileService.cs              # ⭐ 通用文件管理服务
├── OwQueryExtensions.cs          # 查询扩展方法
├── EfHelper.cs                   # EF辅助工具
├── DbContextExtensions.cs        # DbContext扩展
├── SqlServerHelper.cs            # SQL Server辅助工具
└── OwTaskService.cs              # 长时间运行任务服务
```

---

## 🔧 核心模块详解

### 1️⃣ **OwFileService** ⭐ (通用文件管理系统)

**设计理念**：
```
🎯 企业级文件管理的完整解决方案
├── 统一存储管理（磁盘存储 + 数据库元数据）
├── 权限控制（基于用户和组织的访问控制）
├── 多租户隔离（OrgId 数据隔离）
├── 配置热更新（文件大小、类型限制动态配置）
└── 资源安全（流管理、异常处理、事务支持）
```

#### **核心功能**：

##### **文件上传** (`CreateFile`)
```csharp
public PlFileInfo CreateFile(
    Stream fileStream,
    string fileName,
    string displayName,
    Guid? parentId,
    Guid? creatorId,
    string fileTypeId = null,
    string remark = null,
    string clientString = null)
```

**功能特点**：
- ✅ 自动文件大小验证（基于配置）
- ✅ 文件类型白名单检查
- ✅ 自动生成物理文件路径
- ✅ 数据库元数据记录
- ✅ 支持多租户数据隔离

---

##### **文件下载** (`GetFile`)
```csharp
public Stream GetFile(Guid fileId, Guid? userId = null)
```

**功能特点**：
- ✅ 权限验证（基于用户和组织）
- ✅ 多租户数据隔离
- ✅ 返回可读流
- ✅ 异常处理和日志记录

---

##### **文件删除** (`DeleteFile`)
```csharp
public bool DeleteFile(Guid fileId, Guid? userId = null)
```

**功能特点**：
- ✅ 删除物理文件
- ✅ 删除数据库记录
- ✅ 权限验证
- ✅ 事务支持（失败回滚）

---

##### **文件验证** (`ValidateFile`)
```csharp
private void ValidateFile(long fileSize, string fileName)
```

**功能特点**：
- ✅ 文件大小限制（基于配置 `MaxFileSizeMB`）
- ✅ 文件类型白名单（基于配置 `AllowedFileExtensions`）
- ✅ 详细的错误信息

---

#### **配置管理**：
```json
{
  "OwFileService": {
    "MaxFileSizeMB": 5,
    "AllowedFileExtensions": [
      ".pdf", ".doc", ".docx", ".xls", ".xlsx",
      ".jpg", ".jpeg", ".png", ".bmp", ".gif",
      ".txt", ".xml", ".ofd", ".json"
    ],
    "StorageRootPath": "Files"
  }
}
```

#### **应用场景**：
```csharp
// PowerLmsWebApi/Controllers/System/FileController.cs

// ✅ 文件上传
var fileInfo = _FileService.CreateFile(
    fileStream: model.File.OpenReadStream(),
    fileName: model.File.FileName,
    displayName: model.DisplayName,
    parentId: model.ParentId,
    creatorId: context.User.Id
);

// ✅ 文件下载
var stream = _FileService.GetFile(fileId, context.User.Id);

// ✅ 文件删除
var deleted = _FileService.DeleteFile(fileId, context.User.Id);
```

---

### 2️⃣ **OwDbContext** (数据库上下文基类)

**核心功能**：
```csharp
public abstract class OwDbContext : DbContext
{
    // ✅ 统一日志记录
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder);
    
    // ✅ SaveChanges 前回调支持
    public override int SaveChanges();
    
    // ✅ 事务管理支持
    // ✅ 审计字段自动填充
}
```

---

### 3️⃣ **OwBatchDbWriter** (批量数据库写入器)

**设计理念**：
- 批量操作减少数据库往返
- 异步写入提高性能
- 事务支持确保数据一致性

**核心方法**：
```csharp
public class OwBatchDbWriter<TDbContext> where TDbContext : DbContext
{
    // ✅ 添加操作到队列
    public void AddItem(DbOperation operation);
    
    // ✅ 批量执行写入
    public int Flush();
    
    // ✅ 异步批量写入
    public Task<int> FlushAsync();
}
```

**应用场景**：
```csharp
// PowerLmsServer/Managers/OwSqlAppLogger.cs
public void WriteLogItem(OwAppLogItemStore logItem)
{
    var dbOperation = new DbOperation
    {
        OperationType = DbOperationType.Insert,
        Entity = logItem
    };
    _BatchDbWriter.AddItem(dbOperation);
}
```

---

### 4️⃣ **GuidKeyObjectBase** (Guid主键基类)

**核心功能**：
```csharp
public class GuidKeyObjectBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [NotMapped]
    public string IdString => Id.ToString();
    
    [NotMapped]
    public string Base64IdString => Convert.ToBase64String(Id.ToByteArray());
}
```

**设计优势**：
- ✅ 自动生成 Guid 主键
- ✅ 提供字符串和 Base64 转换属性
- ✅ 所有实体的统一基类

---

### 5️⃣ **JsonDynamicPropertyBase** (JSON动态属性基类)

**核心功能**：
```csharp
public abstract class JsonDynamicPropertyBase : GuidKeyObjectBase
{
    // ✅ 存储动态属性的 JSON 字符串
    public string JsonProperties { get; set; }
    
    // ✅ 动态属性字典（内存中）
    [NotMapped]
    public ConcurrentDictionary<string, string> DynamicProperties { get; set; }
}
```

**应用场景**：
- 扩展实体属性而无需修改数据库结构
- 存储灵活的业务数据
- 支持动态配置

---

### 6️⃣ **EfHelper** (EF 辅助工具)

**核心功能**：

##### **动态条件生成** (`GenerateWhereAnd`)
```csharp
public static IQueryable<T> GenerateWhereAnd<T>(
    IQueryable<T> query, 
    Dictionary<string, string> conditional)
```

**功能特点**：
- ✅ 支持多字段条件查询
- ✅ 支持区间查询（逗号分隔）
- ✅ 支持 null 值查询
- ✅ 不区分大小写

**应用示例**：
```csharp
// PowerLmsWebApi/Controllers/FileController.cs
var coll = _DbContext.PlFileInfos.AsQueryable();
coll = EfHelper.GenerateWhereAnd(coll, conditional);
```

---

### 7️⃣ **OwDataUnit** (数据单元处理)

**核心功能**：
- DataTable 与实体转换
- Excel 数据导入
- 批量数据处理
- 数据验证和清洗

---

### 8️⃣ **OwTaskService** (长时间运行任务服务)

**设计理念**：
```
🎯 异步任务处理框架
├── 任务队列管理
├── 后台任务执行
├── 状态跟踪和日志
└── 错误处理和重试
```

**核心功能**：
```csharp
public class OwTaskService<TDbContext> where TDbContext : DbContext
{
    // ✅ 添加任务到队列
    public void QueueTask(OwTaskStore task);
    
    // ✅ 执行任务
    public Task ExecuteTaskAsync(Guid taskId);
    
    // ✅ 获取任务状态
    public OwTaskStatus GetTaskStatus(Guid taskId);
}
```

---

## 🎯 核心设计理念

### 1️⃣ **统一基础类型**
- ✅ `GuidKeyObjectBase` - 所有实体的统一主键
- ✅ `JsonDynamicPropertyBase` - 支持动态属性扩展
- ✅ `IDbTreeNode` - 树结构数据统一接口

### 2️⃣ **批量操作优先**
- ✅ `OwBatchDbWriter` - 批量写入减少数据库往返
- ✅ 批量查询优化
- ✅ 事务支持确保一致性

### 3️⃣ **扩展性优先**
- ✅ 扩展方法模式
- ✅ 接口驱动设计
- ✅ 泛型约束确保类型安全

### 4️⃣ **资源管理**
- ✅ 流管理确保资源释放
- ✅ 数据库连接池化
- ✅ 异常处理和日志记录

---

## 📦 NuGet 依赖

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.36" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.36" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="6.0.0" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

---

## ⚠️ 注意事项

### 1️⃣ **OwFileService 使用规范** ⭐
```csharp
// ✅ 推荐：使用 OwFileService 进行文件操作
var fileInfo = _FileService.CreateFile(
    fileStream, fileName, displayName, 
    parentId, creatorId);

// ❌ 禁止：直接文件IO操作
File.WriteAllBytes(path, bytes); // 跳过权限验证
```

### 2️⃣ **批量写入优化**
```csharp
// ✅ 推荐：使用 OwBatchDbWriter
_BatchDbWriter.AddItem(operation);
// 批量提交
_BatchDbWriter.Flush();

// ❌ 避免：频繁 SaveChanges
foreach (var item in items)
{
    _DbContext.Add(item);
    _DbContext.SaveChanges(); // 性能差
}
```

### 3️⃣ **动态条件查询**
```csharp
// ✅ 推荐：使用 EfHelper.GenerateWhereAnd
var conditional = new Dictionary<string, string>
{
    { "ParentId", parentId.ToString() },
    { "CreateDate", "2024-01-01,2024-12-31" } // 区间
};
var query = EfHelper.GenerateWhereAnd(dbSet, conditional);
```

---

## 🔄 与上层项目的关系

```
PowerLmsServer/PowerLmsData (业务层/数据层)
    ↓ 使用
OwDbBase (数据库基础设施)
    ↓ 引用
OwBaseCore (核心基础设施)
```

**关键依赖点**：
- `OwFileService` → FileController (文件管理) ⭐
- `OwDbContext` → PowerLmsUserDbContext (数据库上下文)
- `OwBatchDbWriter` → OwSqlAppLogger (批量日志写入)
- `GuidKeyObjectBase` → 所有实体类
- `EfHelper` → 所有控制器的动态查询

---

## 📊 统计信息

- **文件总数**: 23 个 C# 文件
- **核心类**: 15+ 个
- **主要工具**: OwFileService, OwBatchDbWriter, EfHelper
- **依赖包**: 4 个 NuGet 包

---

## 🔗 相关索引

- [OwBaseCore 索引](OwBaseCore.md) - 基础设施核心
- [OwExtensions 索引](OwExtensions.md) - Excel扩展
- [PowerLmsData 索引](PowerLmsData.md) - 数据层

---

**最后更新**: 2025-01-30  
**维护者**: AI 自动生成 + 人工校验  
**用途**: AI 上下文优化、开发者快速定位、新人入职引导  
**关键亮点**: ⭐ **OwFileService - 企业级文件管理基础设施**
