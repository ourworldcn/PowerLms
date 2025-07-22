# 代码规范和风格

## 一、命名规范

- **私有字段**：使用 `_PascalCase` 格式，如 `_ServiceProvider`，并附加行尾注释。
- **公共成员**：使用 `PascalCase` 格式，名称具描述性。
- **枚举值**：如用于位标志，首选值为 1, 2, 4, 8 等（2 的幂）。

## 二、代码结构

- 使用 `#region` 分隔代码区块。
- 按功能和访问修饰符组织成员。
- 公共接口与内部实现明确分离。
- 偏好单一功能的小型方法。

## 三、注释风格

- XML 文档注释保持在同一行，并以句号结尾。
- 行尾注释说明实现细节。
- 详细记录参数、返回值和功能。

## 四、技术偏好

### 4.1 异步编程
- **当前实践**：项目主要使用同步方法，避免不必要的 `async/await`
- **原因**：简化代码复杂度，避免异步上下文切换开销
- **例外情况**：I/O密集型操作可考虑异步，但需权衡复杂度

### 4.2 并发控制
- 使用 `SemaphoreSlim` 控制并发访问
- 使用 `SingletonLocker` 进行关键资源锁定
- 采用 `DisposeHelper.Create` 模式管理锁资源

### 4.3 数据库交互
- 使用 `[NotMapped]` 标记非数据库字段
- 避免在实体中直接使用枚举，使用基础类型并提供转换属性
- 设置适当索引提升查询性能
- 使用 `AsNoTracking()` 优化只读查询
- 通过 `IsModified = false` 保护关键字段

## 五、.NET 6 特性使用

### 5.1 项目配置
```xml
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>disable</Nullable>        <!-- 明确禁用可空引用类型 -->
    <ImplicitUsings>enable</ImplicitUsings>  <!-- 启用隐式using -->
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
</PropertyGroup>
```

### 5.2 全局Using声明
- 在项目中使用 `<Using Include="OW.Data" />` 简化引用
- 避免在每个文件中重复using声明

### 5.3 代码分析抑制
- 使用 `GlobalSuppressions.cs` 统一管理代码分析规则
- 适当抑制不必要的警告，如 `IDE0059:不需要赋值`

## 六、设计模式与架构

### 6.1 依赖注入
- 通过构造函数参数注入依赖
- 使用标准的 .NET 6 依赖注入容器
- 避免服务定位器模式

### 6.2 扩展方法
- 增强现有类型功能，简化服务注册
- 为实体类提供业务逻辑扩展方法
- 例如：`CanEdit()`, `GetRelated()` 等

### 6.3 领域驱动设计
- 实体类与服务分离，业务逻辑与数据访问层区分
- 使用扩展方法实现实体行为
- 保持数据层的纯净性

## 七、Web API 规范

### 7.1 控制器结构
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly IServiceProvider _ServiceProvider;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
}
```

### 7.2 HTTP 方法约定
- `[HttpGet]`: 查询操作，使用 `GetAll{Entity}` 命名
- `[HttpPost]`: 创建操作，使用 `Add{Entity}` 命名  
- `[HttpPut]`: 更新操作，使用 `Modify{Entity}` 命名
- `[HttpDelete]`: 删除操作，使用 `Remove{Entity}` 命名

### 7.3 响应文档规范
```csharp
/// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
/// <response code="401">无效令牌。</response>
/// <response code="403">权限不足。</response>
/// <response code="404">指定Id的实体不存在。</response>
```

## 八、错误处理

### 8.1 异常捕获模式
```csharp
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    _Logger.LogError(ex, "操作描述时发生错误");
    result.HasError = true;
    result.ErrorCode = 500;
    result.DebugMessage = $"操作描述时发生错误: {ex.Message}";
}
```

### 8.2 业务验证
- 使用统一的错误响应格式
- 提供详细的错误码和调试信息
- 区分系统错误和业务错误

## 九、日志记录

### 9.1 结构化日志
```csharp
_Logger.LogInformation("业务操作成功，操作人: {UserId}", context.User.Id);
_Logger.LogError(ex, "业务操作失败");
_Logger.LogWarning("尝试修改不存在的实体：{Id}", item.Id);
```

### 9.2 日志级别使用
- `LogError`: 系统错误和异常
- `LogWarning`: 业务警告和异常情况  
- `LogInformation`: 关键业务操作记录
- `LogDebug`: 开发调试信息

## 十、性能考量

### 10.1 资源管理
- 使用 `using` 语句确保资源及时释放
- 使用 `DisposeHelper.Create` 模式管理复杂资源
- 及时释放数据库连接和锁资源

### 10.2 查询优化
- 限制时间类型精度（如精确到毫秒）以节省存储空间
- 使用 `AsNoTracking()` 进行只读查询
- 合理使用分页避免大结果集

### 10.3 并发优化  
- 使用锁机制避免数据竞争
- 设置合理的锁超时时间
- 优化数据库索引提升查询性能

## 十一、安全规范

### 11.1 权限验证
- 统一的Token验证模式
- 基于权限代码的访问控制
- 数据隔离通过OrgId实现

### 11.2 数据保护
- 关键字段修改保护
- 审计字段的不可变性
- 敏感信息的日志脱敏

---

*本规范基于 .NET 6 和当前项目实践制定，会根据技术发展持续更新*