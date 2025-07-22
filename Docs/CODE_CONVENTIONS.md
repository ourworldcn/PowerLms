# PowerLms 代码惯例文档

## 版本信息
- 创建日期：2024年1月
- 项目版本：.NET 6
- 最后更新：根据实际开发过程不断优化

---

## 1. 项目结构与架构

### 1.1 项目分层
- **PowerLmsData**: 数据层，包含实体类、枚举、数据模型
- **PowerLmsServer**: 服务层，包含业务逻辑、管理器类
- **PowerLmsWebApi**: API层，包含控制器、DTO类

### 1.2 文件组织
- 按业务模块分组：财务、OA、基础数据等
- 分部类（Partial Class）用于大型控制器的功能分离
- DTO类统一放在对应控制器的 `.Dto.cs` 文件中

---

## 2. 实体类设计规范

### 2.1 基础继承
```csharp
// 标准实体继承 GuidKeyObjectBase
public class EntityName : GuidKeyObjectBase, ISpecificOrg, ICreatorInfo
```

### 2.2 接口实现
- `ISpecificOrg`: 多租户支持，包含 `OrgId` 字段
- `ICreatorInfo`: 创建信息，包含 `CreateBy` 和 `CreateDateTime`

### 2.3 字段注释规范
```csharp
/// <summary>
/// 字段的业务含义。详细的业务规则说明。
/// </summary>
[Comment("数据库注释")]
[MaxLength(64)] // 字符串字段必须指定长度
[Required] // 必填字段
public string FieldName { get; set; }
```

### 2.4 特殊字段规范
- **OrgId**: 机构Id，增加时确定，不可修改
- **CreateBy**: 创建者Id，等同于登记人Id
- **CreateDateTime**: 默认使用 `OwHelper.WorldNow`
- **币种字段**: 默认值 "CNY"，长度4，使用 `Unicode(false)`
- **汇率字段**: 默认值 1.0m，精度 `[Precision(18, 6)]`
- **金额字段**: 精度 `[Precision(18, 2)]`

---

## 3. 控制器设计规范

### 3.1 控制器结构
```csharp
[ApiController]
[Route("api/[controller]/[action]")]
public partial class EntityController : ControllerBase
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly IServiceProvider _ServiceProvider;
    private readonly AccountManager _AccountManager;
    private readonly ILogger<EntityController> _Logger;
    private readonly EntityManager _EntityManager;
}
```

### 3.2 标准CRUD方法命名
- `GetAll{Entity}`: 获取列表（支持分页和条件查询）
- `Add{Entity}`: 新增
- `Modify{Entity}`: 修改
- `Remove{Entity}`: 删除
- `Audit{Entity}`: 审核操作

### 3.3 权限验证模式
```csharp
if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
    return Unauthorized();
```

### 3.4 条件查询规范
```csharp
// 确保条件字典不区分大小写
var normalizedConditional = conditional != null ?
    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
    null;

// 应用通用条件查询
var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);
```

---

## 4. DTO设计规范

### 4.1 DTO命名规范
- 参数DTO: `{Action}{Entity}ParamsDto`
- 返回DTO: `{Action}{Entity}ReturnDto`
- 继承基类: `TokenDtoBase`, `ReturnDtoBase`, `PagingParamsDtoBase`

### 4.2 标准DTO结构
```csharp
/// <summary>
/// 功能描述的参数封装类。
/// </summary>
public class ActionEntityParamsDto : TokenDtoBase
{
    /// <summary>
    /// 实体数据。其中Id可以是任何值，返回时会指定新值。
    /// </summary>
    [Required]
    public Entity Entity { get; set; }
}

/// <summary>
/// 功能描述的返回值封装类。
/// </summary>
public class ActionEntityReturnDto : ReturnDtoBase
{
    /// <summary>
    /// 如果成功，这里返回新实体的Id。
    /// </summary>
    public Guid Id { get; set; }
}
```

---

## 5. 数据操作规范

### 5.1 Id生成
```csharp
entity.GenerateNewId(); // 强制生成Id
```

### 5.2 字段保护机制
```csharp
// 使用EntityManager进行修改
if (!_EntityManager.Modify(new[] { model.Entity }))
{
    result.HasError = true;
    result.ErrorCode = 404;
    result.DebugMessage = "修改失败，请检查数据";
    return result;
}

// 确保保护字段不被修改
var entry = _DbContext.Entry(model.Entity);
entry.Property(e => e.OrgId).IsModified = false; // 机构Id不可修改
entry.Property(e => e.CreateBy).IsModified = false;
entry.Property(e => e.CreateDateTime).IsModified = false;
```

### 5.3 查询优化
```csharp
// 使用无跟踪查询提高性能
coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

// 使用EntityManager进行分页
var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
```

---

## 6. 业务规则实现

### 6.1 权限控制
- 数据隔离：通过 `OrgId` 确保多租户数据隔离
- 操作权限：检查用户是否有权操作特定数据
- 状态控制：已审核数据不可编辑

### 6.2 审核流程
- 特定字段只能在审核时设置（如结算方式、银行账户）
- 审核状态通过 `AuditDateTime` 判断
- 取消审核时清空相关字段

### 6.3 数据完整性
```csharp
// 创建时设置必要字段
entity.OrgId = context.User.OrgId;
entity.CreateBy = context.User.Id;
entity.CreateDateTime = OwHelper.WorldNow;

// 清空审核相关字段
entity.AuditDateTime = null;
entity.AuditOperatorId = null;
```

---

## 7. 错误处理规范

### 7.1 标准错误响应
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

### 7.2 业务验证
```csharp
if (entity == null)
{
    result.HasError = true;
    result.ErrorCode = 404;
    result.DebugMessage = "指定的实体不存在";
    return result;
}

if (!entity.CanEdit(_DbContext))
{
    result.HasError = true;
    result.ErrorCode = 403;
    result.DebugMessage = "实体当前状态不允许编辑";
    return result;
}
```

---

## 8. 日志记录规范

### 8.1 关键操作日志
```csharp
_Logger.LogInformation("业务操作成功，操作人: {UserId}", context.User.Id);
_Logger.LogError(ex, "业务操作失败");
```

### 8.2 调试信息
- 在开发阶段提供详细的 `DebugMessage`
- 生产环境中避免暴露敏感信息

---

## 9. 扩展方法规范

### 9.1 实体扩展方法
```csharp
public static class EntityExtensions
{
    /// <summary>
    /// 获取关联数据。
    /// </summary>
    public static RelatedEntity GetRelated(this Entity entity, DbContext context)
    {
        return context.Set<RelatedEntity>().Find(entity.RelatedId);
    }

    /// <summary>
    /// 业务规则判断。
    /// </summary>
    public static bool CanEdit(this Entity entity, DbContext context = null)
    {
        return !entity.AuditDateTime.HasValue;
    }
}
```

---

## 10. 性能优化建议

### 10.1 查询优化
- 使用 `AsNoTracking()` 进行只读查询
- 合理使用 `Include()` 避免N+1查询
- 大数据集使用分页查询

### 10.2 内存管理
- 及时释放大对象
- 避免不必要的对象创建
- 使用 `using` 语句管理资源

---

## 11. 命名规范

### 11.1 文件命名
- 实体类：`EntityName.cs`
- 控制器：`EntityController.cs`
- 控制器分部类：`EntityController.Action.cs`
- DTO类：`EntityController.Dto.cs`

### 11.2 变量命名
- 私有字段：`_fieldName`
- 参数：`parameterName`
- 属性：`PropertyName`
- 方法：`MethodName`

---

## 12. 注释规范

### 12.1 XML文档注释
- 所有公开成员必须有XML注释
- 描述业务含义而非技术实现
- 包含参数说明和返回值说明

### 12.2 特殊注释
```csharp
// TODO: 待实现功能
// HACK: 临时解决方案
// NOTE: 重要说明
// FIXME: 需要修复的问题
```

---

## 13. 版本控制

### 13.1 提交规范
- feat: 新功能
- fix: 修复问题
- docs: 文档更新
- refactor: 代码重构
- test: 测试相关

### 13.2 分支管理
- main: 主分支
- develop: 开发分支
- feature/*: 功能分支
- hotfix/*: 修复分支

---

*本文档会根据项目发展持续更新，请开发团队严格遵守以上规范。*