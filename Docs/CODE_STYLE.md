# 代码风格规范
## 命名规范
- **私有字段**: `_PascalCase`，如 `_ServiceProvider`
- **公共成员**: `PascalCase`，具有描述性
- **枚举值**: 位标志使用 1, 2, 4, 8（2的幂）
## 代码结构
- 使用 `#region` 分隔代码区块
- 按功能和访问修饰符组织成员
- 偏好单一功能的小型方法
## .NET 6 特性使用
### 项目配置
```xml
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
</PropertyGroup>
```
### 全局Using
```xml
<ItemGroup>
    <Using Include="OW.Data" />
</ItemGroup>
```
## 技术偏好
### 异步编程
- **当前实践**: 主要使用同步方法
- **原因**: 简化代码复杂度，避免异步上下文切换
- **例外**: I/O密集操作可考虑异步
### 并发控制
- 使用 `SemaphoreSlim` 控制并发访问
- 使用 `SingletonLocker` 锁定关键资源
- 采用 `DisposeHelper.Create` 管理锁资源
### 数据库交互
- 使用 `[NotMapped]` 标记非数据库字段
- 避免实体中直接使用枚举，使用基础类型
- 使用 `AsNoTracking()` 优化只读查询
- 通过 `IsModified = false` 保护关键字段
## Web API 规范
### 控制器结构
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
}
```
### HTTP方法约定
- `[HttpGet]`: 查询操作 - `GetAll{Entity}`
- `[HttpPost]`: 创建操作 - `Add{Entity}`
- `[HttpPut]`: 更新操作 - `Modify{Entity}`
- `[HttpDelete]`: 删除操作 - `Remove{Entity}`
### 响应文档规范
```csharp
/// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
/// <response code="401">无效令牌。</response>
/// <response code="403">权限不足。</response>
/// <response code="404">指定Id的实体不存在。</response>
```
## 日志记录
```csharp
_Logger.LogInformation("业务操作成功，操作人: {UserId}", context.User.Id);
_Logger.LogError(ex, "业务操作失败");
_Logger.LogWarning("尝试修改不存在的实体：{Id}", item.Id);
```
## 性能与安全
- 使用 `using` 语句管理资源
- 统一Token验证模式
- 基于权限代码的访问控制
- 数据隔离通过OrgId实现
---
*本规范基于 .NET 6 和项目实践制定*