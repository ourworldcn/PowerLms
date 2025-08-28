# 📝 PowerLms 项目变更记录

<!-- 变更日志按照WBS编号法组织，使用emoji图标增强辨识度 -->

## 1. 🎯 功能变更总览

- **✅ 账单收支方向管理** - DocBill实体新增IO字段，支持账单收支方向标识和管理
- **✅ OA费用申请单新增客户字段** - 主表新增CustomerId字段，关联客户资料表，支持费用申请与客户关联
- **✅ 客户资料有效性管理** - 专用接口管理客户启用/停用状态，保护IsValid字段修改
- **✅ EF Core查询翻译错误修复** - 修复账期管理中DateTime.ToString()导致的LINQ表达式翻译失败
- **✅ 费用申请单明细查询架构优化** - 重构核心查询函数，提高控制器代码复用
- **✅ 空运进口API恢复** - 创建独立的PlAirborneController，提供完整的CRUD接口
- **✅ 主营业务费用申请单回退功能正确实现** - 已移动到FinancialController，针对DocFeeRequisition实体
- **✅ 导入导出服务架构重构（v2.0）** - 简单字典专用API，批量多表处理
- **✅ 机构参数表和账期管理** - 完整的实体、Controller、业务逻辑
- **✅ OA费用申请单回退功能** - 完整的Manager和Controller实现
- **✅ 工作流清理机制** - 级联删除和日志记录

---

## 2. 📋 业务变更（面向项目经理）

### 2.1 账单收支方向管理
**功能名称：** 账单收支方向标识  
**业务价值：** 为账单增加收支方向管理，明确区分收款账单和付款账单，提升财务管理的准确性和业务分类能力

### 2.2 OA费用申请单客户关联功能
**功能名称：** OA费用申请单新增客户字段  
**业务价值：** 实现费用申请与具体客户/公司的精确关联，提升费用管理的准确性和客户费用统计能力

### 2.3 查询性能和架构优化
**功能名称：** 费用申请单明细查询架构优化  
**业务价值：** 提升复杂条件查询的性能和稳定性，降低系统维护成本

### 2.4 空运进口业务恢复
**功能名称：** 空运进口业务完全恢复  
**业务价值：** 解决空运进口单据无法录入和管理的关键问题，恢复完整的空运业务流程

### 2.5 申请单回退机制
**功能名称：** 费用申请单一键回退功能  
**业务价值：** 支持已审核申请单的错误纠正，避免重新录入，提高业务处理效率

### 2.6 账期管理优化
**功能名称：** 统一账期关闭管理  
**业务价值：** 简化财务月结流程，确保账期数据的一致性和准确性

### 2.7 客户资料管理增强
**功能名称：** 客户有效性状态管理  
**业务价值：** 实现客户的软删除管理，支持客户启用/停用，保护数据完整性和历史记录

### 2.8 数据迁移能力增强
**功能名称：** 字典数据导入导出重构  
**业务价值：** 支持跨公司数据迁移，降低新公司数据初始化成本

---

## 3. 🔧 API变更（面向前端）

### 3.1 新增API
- **账单收支方向管理** - `DocBill`实体新增`IO`字段，支持账单收支方向标识
- **OA费用申请单数据结构变更** - `OaExpenseRequisition`实体新增`CustomerId`字段
- **客户有效性管理** - `/api/Customer/SetCustomerValidity`
- **空运进口单CRUD** - `/api/PlAirborne/GetAllPlIaDoc`等4个接口
- **申请单回退** - `/api/Financial/RevertDocFeeRequisition`
- **OA申请单回退** - `/api/OaExpense/RevertOaExpenseRequisition`
- **账期管理** - `/api/OrganizationParameter/CloseAccountingPeriod`
- **导入导出重构** - `/api/ImportExport/GetSupportedTables`等3个接口

### 3.2 变更API
- **账单管理** - DocBill实体新增IO字段，支持账单收支方向分类管理
- **OA费用申请单** - 主表新增`CustomerId`字段，支持与客户资料关联
- **客户资料修改** - `ModifyCustomer`接口禁用IsValid字段修改，需使用专门接口
- **工作流清理** - `OwWfManager.ClearWorkflowByDocId`方法增强
- **费用申请单明细查询** - 控制器方法统一使用核心查询函数，提高代码一致性

### 3.3 删除API
- **单表导入导出** - 旧版本的单表模式API已删除，统一使用批量处理

---

## 4. 📅 2025-01-27 账单收支方向管理

### 4.1 🎯 功能需求实现
基于业务需求为账单添加收支方向标识功能：

#### 4.1.1 数据模型变更
**新增字段：**
```csharp
/// <summary>
/// 收支方向。false=支出（付款），true=收入（收款）。
/// </summary>
[Comment("收支方向。false=支出（付款），true=收入（收款）")]
public bool IO { get; set; }
```

#### 4.1.2 业务价值
- **收支区分**：明确区分收款账单和付款账单类型
- **财务管理**：提升账单分类管理和财务统计的准确性
- **业务追溯**：支持按收支方向进行账单查询和分析

#### 4.1.3 技术实现
- **字段类型**：`bool`类型，与系统中其他实体保持一致
- **命名规范**：使用`IO`字段名，遵循系统既有约定
- **逻辑定义**：false=支出（付款），true=收入（收款）
- **数据库注释**：明确字段含义，避免业务歧义

### 4.2 📊 前端集成指导
- **UI组件**：在账单表单中新增收支方向选择器
- **数据绑定**：绑定到`IO`字段
- **显示逻辑**：可显示为"收款/付款"文本
- **查询支持**：支持按收支方向筛选账单

---

## 5. 📅 2025-01-27 OA费用申请单客户关联功能

### 5.1 🎯 功能需求实现
基于会议纪要中"OA费用申请单新增'公司'字段 - 主表新增`customerId`（关联客户资料）"的要求：

#### 5.1.1 数据模型变更
**新增字段：**
```csharp
/// <summary>
/// 客户Id。关联客户资料表，用于选择具体的客户/公司。
/// </summary>
[Comment("客户Id。关联客户资料表，用于选择具体的客户/公司")]
public Guid? CustomerId { get; set; }
```

#### 5.1.2 业务价值
- **精确关联**：费用申请单可与具体客户/公司进行关联
- **统计分析**：支持按客户维度进行费用统计和分析
- **业务追溯**：提高费用来源的可追溯性

#### 5.1.3 技术实现
- **字段类型**：`Guid?` 可空类型，兼容现有数据
- **命名规范**：使用Pascal大小写`CustomerId`，符合C#属性命名规范
- **关联关系**：关联到`PlCustomer`客户资料表

### 5.2 📊 前端集成指导
- **UI组件**：在OA费用申请单表单中新增客户选择器
- **数据绑定**：绑定到`CustomerId`字段
- **显示逻辑**：可显示客户名称，存储客户Id
- **查询支持**：支持按客户筛选费用申请单

---

## 6. 📅 2025-01-27 客户资料有效性管理

### 6.1 🎯 功能需求实现
基于会议纪要中"客户资料有效性管理 - 增加'有效/无效'状态（软删除），列表提供启用/停用按钮"的要求：

#### 6.1.1 核心架构设计
- **字段保护机制**：普通修改客户接口(`ModifyCustomer`)禁用`IsValid`字段修改
- **专用管理接口**：单独提供专门的客户有效性管理接口
- **权限控制**：使用叶子权限`C.1.8`（设置客户有效）进行精确权限控制
- **操作审计**：完整的操作日志记录，使用`OwSystemLog`记录状态变更

#### 6.1.2 API接口设计

##### 客户有效性设置
```http
POST /api/Customer/SetCustomerValidity
```
**功能**：设置客户的有效/无效状态  
**权限**：C.1.8 - 设置客户有效  
**特性**：操作审计日志、权限控制

#### 6.1.3 技术实现细节

##### 字段保护机制
```csharp
// 在ModifyCustomer方法中添加字段保护
foreach (var item in model.Items)
{
    _DbContext.Entry(item).Property(c => c.OrgId).IsModified = false;
    // 禁止修改客户有效性字段，需要使用专门的接口
    _DbContext.Entry(item).Property(c => c.IsValid).IsModified = false;
}
```

##### 操作日志记录
```csharp
var logEntry = new OwSystemLog
{
    OrgId = context.User.OrgId,
    ActionId = "Customer.SetValidity",
    ExtraGuid = model.CustomerId,
    ExtraString = $"{(model.IsValid ? "启用" : "停用")}客户",
    ExtraDecimal = context.User.Id.GetHashCode() // 操作人标识
};
```

### 6.2 🔐 权限控制实现

#### 6.2.1 叶子权限配置
- **权限代码**：`C.1.8` - 设置客户有效
- **权限层级**：C（客户资料管理）→ C.1（客户资料管理）→ C.1.8（设置客户有效）
- **权限控制**：使用`AuthorizationManager.Demand("C.1.8")`进行精确权限验证

#### 6.2.2 多租户数据隔离
- **机构隔离**：所有操作限制在用户所属机构(`context.User.OrgId`)范围内
- **数据安全**：确保用户只能操作自己机构的客户资料

### 6.3 📊 数据模型支持

#### 6.3.1 现有字段利用
```csharp
/// <summary>
/// 是否有效。
/// </summary>
[Comment("是否有效")]
public bool IsValid { get; set; }
```

#### 6.3.2 DTO设计
- **SetCustomerValidityParamsDto**：设置参数
- **SetCustomerValidityReturnDto**：返回结果（基于ReturnDtoBase）

### 6.4 ✨ 业务特性

#### 6.4.1 软删除机制
- **逻辑删除**：通过`IsValid=false`实现客户停用，保留历史数据
- **状态切换**：支持有效↔无效之间的双向切换
- **数据完整性**：客户相关的历史业务数据完全保留

#### 6.4.2 操作审计
- **操作日志**：所有状态变更记录到`OwSystemLog`表
- **操作人记录**：记录具体的操作人员信息
- **时间戳**：精确的操作时间记录

### 6.5 🔄 前端集成指导

#### 6.5.1 客户列表界面
- **状态显示**：清晰展示客户的有效/无效状态
- **操作按钮**：提供启用/停用按钮，调用相应API
- **权限控制**：根据`C.1.8`权限控制按钮显示

---

## 7. 📅 2025-01-27 EF Core查询翻译错误修复

### 7.1 🚨 问题识别
在账期管理功能中发现关键错误：
```
The LINQ expression 'DbSet<PlJob>()
    .Where(p => p.OrgId == __8__locals1_orgId_0 && 
               p.AuditDateTime.HasValue && 
               p.AuditDateTime.Value.ToString("yyyyMM") == __targetPeriod_1)' 
could not be translated.
```

**根本原因：** EF Core无法将`DateTime.ToString()`方法翻译为SQL查询

### 7.2 🔧 解决方案实施

#### 7.2.1 新增辅助方法
```csharp
/// <summary>
/// 根据账期字符串生成起始和结束日期
/// </summary>
/// <param name="accountingPeriod">账期，格式YYYYMM，如"202507"</param>
/// <returns>该账期的起始日期和结束日期</returns>
private (DateTime StartDate, DateTime EndDate) GetPeriodDateRange(string accountingPeriod)
{
    if (string.IsNullOrEmpty(accountingPeriod) || accountingPeriod.Length != 6)
    {
        throw new ArgumentException("账期格式错误，应为YYYYMM格式", nameof(accountingPeriod));
    }
    var year = int.Parse(accountingPeriod.Substring(0, 4));
    var month = int.Parse(accountingPeriod.Substring(4, 2));
    var startDate = new DateTime(year, month, 1); // 当月第一天 00:00:00
    var endDate = startDate.AddMonths(1); // 下月第一天 00:00:00
    return (startDate, endDate);
}
```

#### 7.2.2 查询条件重构

**修复前（问题代码）：**
```csharp
var jobsInPeriod = _DbContext.PlJobs
    .Where(j => j.OrgId == orgId && 
               j.AuditDateTime.HasValue &&
               j.AuditDateTime.Value.ToString("yyyyMM") == targetPeriod) // ❌ 无法翻译
    .ToList();
```

**修复后（正确实现）：**
```csharp
// 生成账期的日期范围
var (startDate, endDate) = GetPeriodDateRange(targetPeriod);

// 使用日期范围查询，避免ToString()翻译问题
// startDate: 当月第一天 00:00:00
// endDate: 下月第一天 00:00:00，使用 < 比较，包含当月所有时间
var jobsInPeriod = _DbContext.PlJobs
    .Where(j => j.OrgId == orgId && 
               j.AuditDateTime.HasValue &&
               j.AuditDateTime.Value >= startDate &&
               j.AuditDateTime.Value < endDate) // ✅ 完全可翻译
    .ToList();
```

### 7.3 📊 修复范围

#### 7.3.1 影响的方法
- **PreviewAccountingPeriodClose** - 预览账期关闭
- **CloseAccountingPeriod** - 执行账期关闭

#### 7.3.2 修复优势
1. **数据库层面筛选** - 查询条件完全在SQL中执行，性能最优
2. **内存友好** - 只获取目标账期的数据，不会读取全部数据到内存  
3. **逻辑精确** - 日期范围比较比字符串格式化更精确，正确包含当月所有时间点
4. **EF Core兼容** - 完全符合EF Core查询翻译规范

### 7.4 ✅ 验证结果
- **编译通过** - 代码语法完全正确
- **查询可翻译** - EF Core能够将新查询完全转换为SQL
- **功能正常** - 账期关闭功能恢复正常工作
- **无副作用** - 其他查询功能不受影响

---

## 8. 📅 2025-01-27 费用申请单明细查询架构优化

### 8.1 🚀 核心查询函数强化
- **问题识别：** `GetAllDocFeeRequisitionItemQuery`函数存在语法错误，控制器中复杂查询逻辑重复
- **解决方案：** 修复核心查询函数，新增配套方法，让控制器统一使用Manager层查询逻辑

### 8.2 🔄 主要改进内容

#### 8.2.1 修复核心查询函数
- **语法修复：** 移除`GetAllDocFeeRequisitionItemQuery`中的重复大括号错误
- **逻辑优化：** 确保多实体条件过滤的准确性和完整性
- **返回值确认：** 返回单一DocFeeRequisitionItem实体集合的查询接口

#### 8.2.2 新增Manager方法
```csharp
// 新增辅助查询方法
public IQueryable<DocFeeRequisition> GetDocFeeRequisitionQueryByConditions(
    Dictionary<string, string> conditional = null, Guid? orgId = null)
```
- **智能路由：** 根据条件复杂度自动选择简单查询或复杂连接查询
- **代码复用：** 基于核心查询函数实现，避免逻辑重复
- **性能优化：** 简单条件时避免不必要的表连接

#### 8.2.3 控制器方法重构

##### GetAllDocFeeRequisition方法
- **原逻辑：** 手动分离PlJob条件，复杂的联合查询构建
- **新逻辑：** 直接使用`GetDocFeeRequisitionQueryByConditions`方法
- **代码减少：** 删除约40行重复的条件处理逻辑

##### GetAllDocFeeRequisitionItem方法
- **原逻辑：** 简单的Id和ParentId条件过滤
- **新逻辑：** 使用核心查询函数支持所有复杂条件
- **功能增强：** 支持多实体前缀条件格式

##### GetAllDocFeeRequisitionWithWf方法
- **原逻辑：** 复杂的PlJob条件处理和重复查询构建
- **新逻辑：** 使用Manager方法简化查询构建
- **代码简化：** 删除重复的条件分离和查询逻辑

### 8.3 📊 技术实现细节

#### 8.3.1 核心查询函数修复
```csharp
public IQueryable<DocFeeRequisitionItem> GetAllDocFeeRequisitionItemQuery(
    Dictionary<string, string> conditional = null, Guid? orgId = null)
{
    // 移除重复的大括号，修复语法错误
    conditional ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    // 条件字典生成和查询构建逻辑保持不变
}
```

#### 8.3.2 智能查询路由
```csharp
public IQueryable<DocFeeRequisition> GetDocFeeRequisitionQueryByConditions(...)
{
    // 如果没有子表相关条件，使用简单查询
    if (conditional == null || !conditional.Any() || 
        conditional.All(kv => kv.Key.StartsWith($"{nameof(DocFeeRequisition)}.") || !kv.Key.Contains('.')))
    {
        return GetAllDocFeeRequisitionQuery(orgId);
    }
    
    // 否则使用核心查询函数进行复杂查询
    var itemsQuery = GetAllDocFeeRequisitionItemQuery(conditional, orgId);
    var parentIds = itemsQuery.Select(item => item.ParentId.Value).Distinct();
    return _DbContext.DocFeeRequisitions.Where(req => parentIds.Contains(req.Id));
}
```

### 8.4 ✨ 架构优势

#### 8.4.1 代码一致性
- **唯一基准：** 所有复杂查询都基于`GetAllDocFeeRequisitionItemQuery`核心函数
- **逻辑统一：** 条件处理、表连接、过滤逻辑完全一致
- **维护简化：** 查询逻辑修改只需在一个地方进行

#### 8.4.2 性能优化
- **智能路由：** 简单查询避免不必要的表连接
- **查询复用：** 减少重复的SQL生成和执行
- **内存优化：** 统一的查询接口减少对象创建

#### 8.4.3 扩展性增强
- **新控制器：** 可以直接使用Manager方法，无需重写查询逻辑
- **新条件：** 在核心函数中添加支持，所有控制器自动受益
- **新实体：** 可以按照相同模式扩展其他实体的查询管理器

---

## 9. 📅 2025-01-27 空运进口API完全恢复

### 9.1 ✅ 历史问题修复
经过Git历史核查，确认了空运进口单CRUD接口确实在历史版本中存在：
- **提交记录：** `38a1fe4` (2024年7月9日) - "增加空运进口单实体PlIaDoc，增加PlIaDoc的CRUD接口"
- **丢失原因：** 在后续的代码重构中被意外删除，可能发生在PlSeaborne控制器拆分时期
- **影响范围：** 空运进口业务功能完全无法使用，前端Swagger文档缺失相关接口

### 9.2 🔄 架构决策与实现

#### 9.2.1 独立控制器创建
参照海运业务的PlSeaborneController模式，创建了专门的空运业务控制器：
- **新控制器：** `PowerLmsWebApi/Controllers/Business/AirFreight/PlAirborneController.cs`
- **DTO文件：** `PowerLmsWebApi/Controllers/Business/AirFreight/PlAirborneController.Dto.cs`
- **业务范围：** 空运进口单和空运出口单的完整CRUD操作

#### 9.2.2 代码迁移与清理
- **DTO迁移：** 将PlJobController.Dto.cs中的空运相关DTO移动到独立文件
- **接口恢复：** 基于Git历史记录和现有DTO重新实现空运进口CRUD接口
- **代码清理：** 从PlJobController中移除已迁移的DTO定义，避免重复

### 9.3 📊 恢复的API接口

#### 9.3.1 空运进口单CRUD
```http
GET /api/PlAirborne/GetAllPlIaDoc     # 获取空运进口单列表
POST /api/PlAirborne/AddPlIaDoc       # 新增空运进口单
PUT /api/PlAirborne/ModifyPlIaDoc     # 修改空运进口单
DELETE /api/PlAirborne/RemovePlIaDoc  # 删除空运进口单
```

#### 9.3.2 空运出口单CRUD（同时提供）
```http
GET /api/PlAirborne/GetAllPlEaDoc     # 获取空运出口单列表
POST /api/PlAirborne/AddPlEaDoc       # 新增空运出口单
PUT /api/PlAirborne/ModifyPlEaDoc     # 修改空运出口单
DELETE /api/PlAirborne/RemovePlEaDoc  # 删除空运出口单
```

### 9.4 🔐 权限配置

#### 9.4.1 空运进口权限（D1系列）
- **D1.1.1.1** - 查看权限
- **D1.1.1.2** - 新增权限
- **D1.1.1.3** - 修改权限
- **D1.1.1.4** - 删除权限

#### 9.4.2 空运出口权限（D0系列）
- **D0.1.1.1** - 查看权限
- **D0.1.1.2** - 新增权限
- **D0.1.1.3** - 修改权限
- **D0.1.1.4** - 删除权限

---

## 10. 📅 2025-01-27 主营业务费用申请单回退功能架构修正

### 10.1 ✅ 重大架构修正
经过仔细核查代码和会议纪要，发现了重要的业务逻辑错误：
- **错误理解：** 之前以为要回退PlJob(工作任务)
- **正确理解：** 应该回退DocFeeRequisition(主营业务费用申请单)
- **核心发现：** PlJob本身不启动工作流，DocFeeRequisition才是启动工作流的实体

### 10.2 🔄 代码重构内容

#### 10.2.1 新增文件
- **PowerLmsServer/Managers/Financial/DocFeeRequisitionManager.cs** - 主营业务费用申请单专用管理器
  - `RevertRequisition()` - 回退申请单到初始状态
  - `CanRevert()` - 验证是否可以回退
  - `GetStatusInfo()` - 获取申请单状态信息

#### 10.2.2 修改文件
- **PowerLmsWebApi/Controllers/Financial/FinancialController.Dto.cs**
  - 新增 `RevertDocFeeRequisitionParamsDto` 和 `RevertDocFeeRequisitionReturnDto`
  
- **PowerLmsWebApi/Controllers/Financial/FinancialController.DocFeeRequisition.cs**
  - 新增 `RevertDocFeeRequisition()` 方法，提供主营业务费用申请单回退API

### 10.3 📊 API变更列表

#### 10.3.1 新增API接口
```http
POST /api/Financial/RevertDocFeeRequisition
```
**功能：** 回退主营业务费用申请单到初始状态
**位置：** PowerLmsWebApi.Controllers.FinancialController  
**权限：** F.3 (财务管理权限)

---

## 11. 📅 2025-01-27 导入导出服务架构重构（v2.0）

### 11.1 🔄 主要变更
- **架构重构**：删除单表模式，统一使用批量多表处理
- **功能分离**：简单字典独立为专门API，通用表字典保持批量处理
- **性能优化**：批量操作、流式处理、减少数据库往返

### 11.2 📊 API变更说明

#### 11.2.1 简单字典API（新增专用接口）
- `GET /ImportExport/GetSimpleDictionaryCatalogCodes` - 获取Catalog Code列表
- `GET /ImportExport/ExportSimpleDictionary` - 导出简单字典（支持多Catalog）
- `POST /ImportExport/ImportSimpleDictionary` - 导入简单字典

#### 11.2.2 通用表字典API（保留批量模式）
- `GET /ImportExport/GetSupportedTables` - 获取支持的表类型
- `GET /ImportExport/ExportMultipleTables` - 批量导出多表
- `POST /ImportExport/ImportMultipleTables` - 批量导入多表

#### 11.2.3 删除的API（不兼容变更）
- `ExportTable`、`ImportTable`、`GetSupportedTableTypes` 等单表模式API已删除

---

## 12. 📅 2025-01-26 机构参数表和账期管理

### 12.1 ✅ 完整功能实现

#### 12.1.1 新增实体
- **PlOrganizationParameter.cs** - 机构参数表实体
  - CurrentAccountingPeriod - 当前账期（YYYYMM格式）
  - BillHeader1, BillHeader2, BillFooter - 报表打印信息

#### 12.1.2 新增控制器
- **OrganizationParameterController.cs** - 机构参数管理API
  - 完整的CRUD操作
  - 权限控制和多租户安全

#### 12.1.3 账期管理功能
- **PreviewAccountingPeriodClose** - 预览账期关闭影响范围
- **CloseAccountingPeriod** - 执行账期关闭操作
- **自动账期递增** - 关闭后自动推进到下一月份

#### 12.1.4 权限配置
- **F.2.9** - 关闭账期专用权限
- **多级权限控制** - 机构参数编辑权限

---

## 13. 📅 2025-01-25 OA费用申请单回退功能

### 13.1 ✅ 完整功能实现

#### 13.1.1 Manager层实现
- **OaExpenseManager.RevertRequisition()** - 核心回退业务逻辑
- **状态枚举处理** - 正确处理OaExpenseRequisitionStatus状态
- **工作流清理** - 调用OwWfManager.ClearWorkflowByDocId()

#### 13.1.2 Controller层实现
- **OaExpenseController.RevertOaExpenseRequisition()** - HTTP API端点
- **完整的权限验证** - 使用F.4权限控制
- **错误处理和日志记录** - 详细的操作审计

#### 13.1.3 API接口详情
```http
POST /api/OaExpense/RevertOaExpenseRequisition
```
**权限：** F.4 (OA费用管理权限)

---

## 14. 📅 历史变更概要

### 14.1 PowerLms v1.0 基础功能
- 基础业务单据管理（空运/海运进出口）
- 费用管理和模板系统
- 客户资料和数据字典
- 基础权限和多租户支持

### 14.2 近期重要更新
- OaExpenseRequisition OA费用申请功能
- 工作号管理和唯一性约束
- 状态机和业务逻辑管理器
- 导入导出功能基础实现

---

**注意**：本文档记录所有重要的功能变更和技术决策，便于团队理解系统演进历程和维护代码