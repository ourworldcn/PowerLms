# PowerLms 开发规范概要
## 项目架构
- **PowerLmsData**: 数据层 - 实体类、枚举、数据模型
- **PowerLmsServer**: 服务层 - 业务逻辑、管理器类  
- **PowerLmsWebApi**: API层 - 控制器、DTO类
## 核心规范
### 实体设计
```csharp
// 标准实体继承
public class EntityName : GuidKeyObjectBase, ISpecificOrg, ICreatorInfo
{
    /// <summary>业务字段描述。详细业务规则说明。</summary>
    [Comment("数据库注释")]
    [MaxLength(64), Required]
    public string FieldName { get; set; }
}
```
**特殊字段规范**:
- **OrgId**: 机构Id，创建时确定，不可修改
- **CreateBy/CreateDateTime**: 审计字段，使用 `OwHelper.WorldNow`
- **币种字段**: "CNY"，长度4，`Unicode(false)`
- **汇率/金额**: `[Precision(18, 6)]` / `[Precision(18, 2)]`
### 控制器设计
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
    private readonly EntityManager _EntityManager;
}
```
**标准方法命名**:
- `GetAll{Entity}`: 获取列表（分页+条件查询）
- `Add{Entity}`: 新增
- `Modify{Entity}`: 修改  
- `Remove{Entity}`: 删除
- `Audit{Entity}`: 审核
### DTO设计
```csharp
// 参数DTO
public class ActionEntityParamsDto : TokenDtoBase
{
    [Required]
    public Entity Entity { get; set; }
}
// 返回DTO  
public class ActionEntityReturnDto : ReturnDtoBase
{
    public Guid Id { get; set; }
}
```
### 权限验证
```csharp
if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
    return Unauthorized();
```
### 数据操作
```csharp
// Id生成
entity.GenerateNewId();
// 字段保护
var entry = _DbContext.Entry(model.Entity);
entry.Property(e => e.OrgId).IsModified = false;
entry.Property(e => e.CreateBy).IsModified = false;
// 查询优化
coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
```
### 错误处理
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
    result.DebugMessage = $"操作失败: {ex.Message}";
}
```
### 业务规则
- **数据隔离**: 通过 `OrgId` 实现多租户隔离
- **审核流程**: `AuditDateTime` 判断审核状态
- **字段保护**: 关键审计字段不可修改
### 扩展方法
```csharp
public static class EntityExtensions
{
    public static bool CanEdit(this Entity entity, DbContext context = null)
    {
        return !entity.AuditDateTime.HasValue;
    }
}
```
## 命名规范
- **私有字段**: `_fieldName`
- **公共成员**: `PascalCase`  
- **文件命名**: `EntityController.cs`, `EntityController.Dto.cs`
## 性能优化
- 使用 `AsNoTracking()` 进行只读查询
- 大数据集使用分页查询
- 使用 `using` 语句管理资源
---
*详细规范请参考完整的开发文档*