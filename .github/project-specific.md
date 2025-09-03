# PowerLms 项目特定配置

<!-- PowerLms物流管理系统的项目特定配置和开发规范 -->

## 1. 🔒 权限体系设计

### 1.1 权限查找逻辑（重要）
基于 `权限.md` 文件分析，PowerLms系统采用**叶子节点权限**设计：

**权限查找原则：**
- **叶子权限优先**：只有权限.md中没有子权限的叶子节点才有实际意义
- **权限匹配策略**：搜索权限文件，如果有叶子权限符合要求就使用
- **无权限时处理**：如果没有找到合适的权限项，则暂时忽略权限问题
- **开发阶段容错**：功能开发优先，权限验证可后续完善

**具体实现：**
```csharp
// 权限验证模板
if (_AuthorizationManager.Demand(out err, "具体叶子权限ID"))
{
    // 有权限，继续执行
}
else
{
    // 开发阶段：记录TODO，暂时允许继续
    // TODO: 待确认具体权限节点后完善权限验证
}
```

### 1.2 重要权限节点分析

**财务管理相关权限：**
- `F.3.1` - 新建结算
- `F.3.2` - 修改结算  
- `F.3.3` - 撤销(删除)结算
- `F.3.4` - 结算单确认
- `F.3.5` - 结算单取消确认
- `F.6` - 财务接口（可能适用于导出金蝶功能）

**基础数据管理权限：**
- `B.0` - 数据字典（适用于导入导出功能)
- `B.1` - 本公司信息
- `B.3` - 币种
- `B.4` - 汇率
- `B.5` - 国家
- `B.6` - 港口

**OA办公自动化权限：**
- `OA.1` - 日常费用申请
- `OA.1.1` - 日常费用结算确认
- `OA.1.2` - 日常费用拆分结算
- `OA.1.3` - 日常费用撤销

### 1.3 权限验证代码模式

**标准权限验证：**
```csharp
public ActionResult<T> SomeAction(SomeParamsDto model)
{
    if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
        return Unauthorized();
    
    // 权限验证
    if (!_AuthorizationManager.Demand("具体权限节点"))
        return StatusCode((int)HttpStatusCode.Forbidden, "权限不足");
    
    // 业务逻辑
}
```

**多权限选择验证：**
```csharp
// 优先检查通用权限，再检查具体业务权限
if (!_AuthorizationManager.Demand(out err, "F.2.4.1"))  // 通用费用管理权限
{
    if (job.JobTypeId == ProjectContent.AeId)
    {
        if (!_AuthorizationManager.Demand(out err, "D0.6.1")) // 空运出口费用明细
            return StatusCode((int)HttpStatusCode.Forbidden, err);
    }
    // ... 其他业务类型权限检查
}
```

## 2. 🏗️ 架构设计原则

### 2.1 基础设施组件复用（强制）
- **文件管理** → `OwFileService` 
- **工作流引擎** → `OwWfManager`
- **权限管理** → `AuthorizationManager`
- **组织管理** → `OrgManager`
- **数据字典** → `DataDicManager`
- **Excel处理** → `OwDataUnit` + `OwNpoiUnit`（禁用废弃的NpoiManager）

### 2.2 分层架构标准
```
PowerLms解决方案架构
├── PowerLmsWebApi/     # 控制器层 - 只做参数验证、权限检查、异常处理
├── PowerLmsServer/     # 业务逻辑层 - Manager模式，核心业务逻辑
├── PowerLmsData/       # 数据访问层 - EF Core实体和DbContext
└── 基础设施 (../Bak/)  # OwDbBase + OwBaseCore核心组件
```

### 2.3 多租户数据隔离（强制）
- **数据查询**：必须添加 `OrgId` 过滤条件
- **权限验证**：通过 `AuthorizationManager.Demand()` 进行细粒度权限控制
- **组织过滤**：使用 `OrgManager` 进行数据范围控制

## 3. 🎯 开发模式和约束

### 3.1 Manager模式实现
```csharp
[OwAutoInjection(ServiceLifetime.Scoped)]
public class BusinessManager
{
    private readonly PowerLmsUserDbContext _dbContext;
    private readonly AuthorizationManager _authManager;
    private readonly OwWfManager _workflowManager;
    
    // 业务逻辑方法 - 抛出业务异常，由控制器统一处理
    public SomeResultDto DoSomething(SomeParamsDto parameters)
    {
        // 权限验证
        if (!_authManager.Demand("具体权限"))
            throw new UnauthorizedAccessException("权限不足");
            
        // 业务逻辑实现
        // ...
        
        return result;
    }
}
```

### 3.2 控制器实现模式
```csharp
[HttpPost]
public ActionResult<T> SomeAction(SomeParamsDto model)
{
    if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
        return Unauthorized();
    
    try
    {
        var result = _businessManager.DoSomething(model);
        return result;
    }
    catch (UnauthorizedAccessException ex)
    {
        return StatusCode((int)HttpStatusCode.Forbidden, ex.Message);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        // 记录日志并返回通用错误
        return StatusCode((int)HttpStatusCode.InternalServerError, "操作失败");
    }
}
```

## 4. 🔧 技术规范

### 4.1 实体字段扩展
- **审计字段**：所有实体包含 `CreateBy`, `CreateDateTime`, `ModifyBy`, `ModifyDateTime`
- **多租户字段**：所有业务实体包含 `OrgId` 字段
- **软删除支持**：重要业务实体支持软删除机制

### 4.2 数据库操作规范
- **禁止自动迁移**：所有数据库变更通过手动迁移脚本
- **查询优化**：大数据量查询使用 `AsNoTracking()`
- **事务控制**：重要操作使用数据库事务确保一致性

### 4.3 Excel处理标准
```csharp
// ✅ 推荐：高性能方案
var count = OwDataUnit.BulkInsertFromExcelWithStringList<T>(
    sheet, dbContext, ignoreExisting: true, logger, "操作描述");

// ❌ 禁止：废弃组件
NpoiManager.WriteToDb() // 已标记 [Obsolete]
```

## 5. 📊 导入导出功能特定规范

### 5.1 权限验证逻辑
基于 TODO.md 中的技术债务，导入导出功能的权限验证需要：

```csharp
// TODO注释模板（当前使用）
// TODO: 搜索权限文件，如果有叶子权限符合要求就使用，如果没有就不进行权限验证

// 实现逻辑
private bool CheckImportExportPermission(string tableType)
{
    // 1. 优先检查具体的导入导出权限
    if (_authManager.Demand($"B.0.{tableType}.Import")) return true;
    if (_authManager.Demand($"B.0.{tableType}.Export")) return true;
    
    // 2. 检查通用数据字典权限
    if (_authManager.Demand("B.0")) return true;
    
    // 3. 没有找到合适权限时的处理
    // 开发阶段：记录警告，允许继续（基于项目配置要求）
    _logger.LogWarning($"未找到{tableType}的具体权限，暂时允许操作");
    return true; // 暂时忽略权限问题
}
```

### 5.2 Excel Sheet命名规范
- **统一使用实体类型名称**：如 `PlCountry`, `PlPort`, `SimpleDataDic`
- **不使用数据库表名**：避免 `pl_Countries` 等数据库表名
- **注释一致性**：代码注释必须与实际逻辑完全对应

## 6. 🚨 开发约束和风险控制

### 6.1 强制性约束
- **基础设施优先复用**：新功能必须优先考虑复用现有组件
- **禁止重复造轮子**：发现重复功能时必须重构统一
- **多租户严格隔离**：所有数据操作必须包含组织过滤
- **权限验证必需**：所有对外接口必须进行权限验证

### 6.2 代码质量要求
- **异常处理完整**：Manager层抛业务异常，Controller层统一处理
- **日志记录规范**：重要操作必须记录操作日志
- **测试覆盖要求**：核心业务逻辑需要单元测试覆盖
- **文档同步更新**：代码变更时同步更新相关文档

## 7. 📝 临时输出.md 集成说明

### 7.1 收款结算单导出金蝶功能权限分析
基于 `临时输出.md` 中的功能设计，**收款结算单导出金蝶功能**的权限建议：

**推荐权限节点：**
- **首选**：`F.6` - 财务接口（最符合功能定位）
- **备选**：`F.3.2` - 修改结算（如果导出认为是结算数据的修改操作）
- **新增**：`F.6.1` - 导出金蝶凭证（如需要更细粒度控制）

**权限验证实现：**
```csharp
// 收款结算单导出权限验证
public ActionResult<ExportSettlementReceiptReturnDto> ExportSettlementReceipt(ExportSettlementReceiptParamsDto model)
{
    if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
        return Unauthorized();
    
    // 权限验证：优先检查财务接口权限
    if (!_AuthorizationManager.Demand("F.6"))
    {
        // 备选：检查结算修改权限
        if (!_AuthorizationManager.Demand("F.3.2"))
            return StatusCode((int)HttpStatusCode.Forbidden, "权限不足：需要财务接口或结算修改权限");
    }
    
    // 业务逻辑...
}
```

### 7.2 调试vs生产环境权限差异
基于临时输出.md中的疑问，明确环境差异：

**调试环境：**
- 权限验证：记录警告但允许继续
- 数据验证：允许部分非关键错误
- 错误处理：详细错误信息用于调试

**生产环境：**
- 权限验证：严格验证，无权限直接拒绝
- 数据验证：严格验证，确保数据完整性
- 错误处理：用户友好的错误信息

---

**最后更新：** 2025-01-31  
**适用版本：** PowerLms v1.0+  
**维护者：** 开发团队