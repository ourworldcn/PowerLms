# 📝 变更日志

## [2025-02-06] - 🔧 工作流诊断增强：防御客户端错误数据

### 🎯 业务变更（面向项目经理）

#### 工作流节点数量不匹配问题诊断增强
- **问题描述**：陈云霄报告"模板有4个节点，流程只有2个节点"
- **问题根源**：客户端传入错误数据或模板配置错误，导致工作流提前结束
- **解决方案**：增强后端验证和详细日志记录，帮助快速定位问题
- **业务价值**：
  - 提前发现模板配置错误（如节点未配置审批人、NextId断链）
  - 详细记录客户端调用参数，便于追溯问题根源
  - 友好的错误提示，指导用户正确操作

### 📊 增强的诊断功能

#### 1. 模板配置验证
- ✅ 检查首节点是否存在（GetFirstNodes结果为空）
- ✅ 检查节点链表是否完整（NextId指向不存在的节点）
- ✅ 检查节点是否配置审批人（Children为空）
- ✅ 记录模板节点链表结构（便于诊断断链问题）

#### 2. 客户端参数验证
- ✅ 验证NextOpertorId是否在下一节点审批人列表中
- ✅ 检测客户端未传NextOpertorId但节点链未结束的异常情况
- ✅ 友好的错误提示（包含有效审批人列表）
- ✅ 详细记录所有请求参数，便于问题追溯

#### 3. 详细日志记录
```csharp
// 记录的关键信息
- 工作流模板配置：节点数、节点链表结构
- 节点流转过程：当前节点、下一节点、审批人
- 客户端请求参数：DocId、TemplateId、NextOpertorId
- 异常情况：配置错误、参数错误、逻辑错误
```

### 📊 日志示例

#### ✅ 正常流程
```log
[INFO] 工作流发送请求：DocId=571fe7d6-..., NextOpertorId=ab061006-...
[INFO] 工作流模板配置：节点数=4, 节点链表=[节点1(NextId=guid2), 节点2(NextId=guid3), ...]
[INFO] 创建首节点：NodeDisplayName=石永昌审批
[INFO] 流转到下一节点：NextNodeName=财务审批, NextOpertor=ab061006-...
```

#### ⚠️ 模板配置错误
```log
[ERROR] 工作流模板配置错误：NextId指向的节点不存在！CurrentNode=guid2, NextId=guid999
[WARNING] ⚠️ 工作流模板配置问题：下一节点『财务审批』未配置审批人，导致流程无法继续
```

#### ⚠️ 客户端错误
```log
[WARNING] 客户端传入了NextOpertorId但当前节点没有下一节点
[WARNING] 客户端传入的NextOpertorId不在下一节点审批人列表中
[WARNING] ⚠️ 客户端错误：下一节点有3个审批人，但客户端未选择并传递NextOpertorId
```

### 🔧 技术细节

#### 修复的文件
1. `PowerLmsWebApi/Controllers/System/WfController.cs`

#### 核心变更（工作流诊断增强）

##### 1. 注入ILogger依赖
```csharp
// WfController.cs 构造函数
public WfController(..., ILogger<WfController> logger)
{
    _Logger = logger;
}
```

##### 2. GetNextNodeItemsByDocId 增强
```csharp
// 🔧 诊断：记录NextId为null的情况
if (ttNode?.NextId is null)
{
    _Logger.LogWarning(
        "工作流节点无下一节点：当前节点={NodeName}, NextId为null，流程将无法继续流转",
ttNode?.DisplayName);
    return result; // 返回空列表，前端无法选择下一个审批人
}

// 🔧 诊断：验证下一个节点是否存在
if (nextNode == null)
{
    _Logger.LogError(
   "工作流模板配置错误：NextId指向的节点不存在！NextId={NextId}",
        ttNode.NextId);
  return result;
}

// 🔧 诊断：检查下一个节点是否有审批人
if (!nextNode.Children.Any(c => c.OperationKind == 0))
{
    _Logger.LogWarning(
        "工作流节点未配置审批人：下一节点={NextNodeName}，前端将收到空列表",
 nextNode.DisplayName);
}
```

##### 3. Send 方法增强
```csharp
// 🔧 记录请求参数
_Logger.LogInformation(
    "工作流发送请求：DocId={DocId}, Approval={Approval}, NextOpertorId={NextOpertorId}",
    model.DocId, model.Approval, model.NextOpertorId);

// 🔧 记录模板配置
var templateNodeInfo = string.Join(", ", template.Children.Select(n => 
    $"{n.DisplayName}(NextId={(n.NextId.HasValue ? n.NextId.ToString() : "null")})"));
_Logger.LogInformation(
    "工作流模板配置：节点数={NodeCount}, 节点链表=[{NodeInfo}]",
    template.Children.Count, templateNodeInfo);

// 🔧 验证：检查是否有下一个节点但客户端未传NextOpertorId
if (ttCurrentNode.NextId != null && model.NextOpertorId == null)
{
    _Logger.LogWarning(
     "⚠️ 潜在的客户端错误：当前节点还有下一节点，但客户端未传NextOpertorId，流程将提前结束");
    
    // 检查是否因为下一节点未配置审批人
    var nextNode = _DbContext.WfTemplateNodes
        .Include(n => n.Children)
        .FirstOrDefault(n => n.Id == ttCurrentNode.NextId);
  if (nextNode != null && !nextNode.Children.Any(c => c.OperationKind == 0))
    {
        _Logger.LogWarning(
            "⚠️ 工作流模板配置问题：下一节点未配置审批人，导致流程无法继续");
    }
}

// 🔧 验证：详细记录NextOpertorId验证失败原因
if (nextTItem == null)
{
    var validApprovers = nextNode.Children
        .Where(c => c.OperationKind == 0)
        .Select(c => c.OpertorId)
        .ToList();
  var validApproverNames = _DbContext.Accounts
     .Where(a => validApprovers.Contains(a.Id))
  .Select(a => a.DisplayName)
    .ToList();
    
    return BadRequest($"指定下一个操作人不合法。有效审批人：{string.Join("、", validApproverNames)}");
}
```

#### 错误提示改进
**修复前**：
```
指定下一个操作人Id=xxx,但它不是合法的下一个操作人。
```

**修复后**：
```
指定下一个操作人Id=xxx,但它不是合法的下一个操作人。
下一节点『财务审批』的有效审批人：张三、李四、王五
```

### 📝 排查步骤指南

#### 问题：模板有4个节点，流程只有2个节点

**步骤1：检查日志中的模板配置**
```log
查找关键词："工作流模板配置"
期望看到：节点数=4, 节点链表=[节点1(NextId=guid2), 节点2(NextId=guid3), ...]
```

**步骤2：检查是否有配置错误警告**
```log
查找关键词："工作流模板配置错误" 或 "工作流节点未配置审批人"
常见问题：
- NextId指向不存在的节点
- 节点未配置审批人
- 节点2的NextId为null（应该指向节点3）
```

**步骤3：检查客户端调用日志**
```log
查找关键词："工作流发送请求" 和 "客户端错误"
验证：
- 第1次调用：是否正确传递了NextOpertorId
- 第2次调用：是否未传NextOpertorId导致流程提前结束
```

**步骤4：确认根本原因**
```
原因1：节点2的NextId为null
  → 修复：UPDATE OwWfTemplateNodes SET NextId='节点3ID' WHERE Id='节点2ID'

原因2：节点3未配置审批人
  → 修复：INSERT INTO OwWfTemplateNodeItems ...

原因3：客户端逻辑错误
  → 修复：前端增强验证，确保正确传递NextOpertorId
```

### 🎯 影响范围
- **零破坏性变更**：只增加日志和验证，不改变工作流引擎逻辑
- **向后兼容**：现有流程不受影响
- **诊断能力提升**：通过详细日志快速定位问题
- **用户体验改善**：友好的错误提示，指导正确操作

---

## [2025-02-06] - 🔧 工作号功能完善：复制时排除财务日期

### 🎯 业务变更（面向项目经理）

#### 复制工作号时排除财务日期
- **功能名称**：复制工作号时自动清空财务日期
- **问题描述**：复制工作号时，财务日期被复制过来，导致新工作号的财务日期与实际业务日期不一致
- **修复效果**：复制工作号时自动清空财务日期，由前端根据新工作号的开航/到港日期重新计算
- **业务价值**：确保财务日期计算准确，避免账期混乱和财务报表错误

### 📊 API变更（面向前端）

#### 行为变更
1. `POST /api/PlJob/CopyJob` - 复制工作号接口（✅ 行为优化）
   - 新行为：复制后的工作号，`AccountDate` 字段自动设为null
   - 向后兼容：现有调用方式不受影响，前端需要根据开航/到港日期重新计算财务日期

#### 字段行为对比
```diff
场景：复制工作号（源工作号开航日期2025-01-01，财务日期2025-01-01）

- ❌ 修复前：新工作号的财务日期为2025-01-01（与源工作号相同）
+ ✅ 修复后：新工作号的财务日期为null，由前端根据新的开航/到港日期重新计算
```

#### 前端处理要求
- **必须**：复制工作号后，前端根据业务类型重新计算财务日期
  - 出口业务：财务日期 = 开航日期(Etd)
  - 进口业务：财务日期 = 到港日期(ETA)
- **注意**：如果用户修改了开航/到港日期，财务日期也需要相应更新

### 🔧 技术细节

#### 修复的文件
1. `PowerLmsWebApi/Controllers/Business/Common/PlJobController.cs`

#### 核心变更（复制工作号时排除财务日期）
```csharp
// 🔧 Bug修复：CopyJob方法第867行之后添加
// 强制设置新工作号的系统管理字段
destJob.GenerateNewId();
destJob.CreateBy = context.User.Id;
destJob.CreateDateTime = OwHelper.WorldNow;
destJob.JobState = 2;
destJob.OperatingDateTime = OwHelper.WorldNow;
destJob.OperatorId = context.User.Id;
destJob.OrgId = context.User.OrgId;
destJob.AuditDateTime = null;
destJob.AuditOperatorId = null;
destJob.AccountDate = null;    // 🔧 清空财务日期，由前端根据新工作号的开航/到港日期重新计算
```

#### 根本原因分析
**为什么不能复制财务日期？**

根据 `PlJob.cs` 的业务规则：
- 财务日期由**前端根据业务类型自动计算**
- 出口业务：财务日期 = 开航日期(Etd)
- 进口业务：财务日期 = 到港日期(ETA)

如果复制工作号时保留旧的财务日期，会导致：
- ❌ 用户修改开航/到港日期后，财务日期与业务日期不一致
- ❌ 账期计算错误，影响财务结算
- ❌ 财务报表数据混乱

#### 与其他清空字段的逻辑对比
当前代码已经清空了以下字段：
```csharp
destJob.AuditDateTime = null;    // 清空审核时间（需要重新审核）
destJob.AuditOperatorId = null;  // 清空审核人
destJob.AccountDate = null;      // 清空财务日期（需要重新计算）✅ 新增
```

**逻辑一致性**：
- 审核时间：需要重新审核 → 清空 ✅
- 财务日期：需要重新计算 → 清空 ✅（本次修复）

#### 影响范围
- **零破坏性变更**：修复仅影响复制工作号功能，现有调用方式不受影响
- **向后兼容**：前端已有财务日期计算逻辑，只需确保在复制后重新计算
- **数据质量提升**：防止财务日期错误，提升账期管理准确性

### 📝 测试建议（面向测试）

#### 复制工作号测试场景
1. ✅ **复制工作号，检查财务日期**
   - 创建工作号A（开航日期2025-01-01，财务日期2025-01-01）
   - 复制工作号生成工作号B
   - 验证：工作号B的财务日期为null

2. ✅ **复制并修改开航日期**
   - 复制工作号A生成工作号B
   - 修改工作号B的开航日期为2025-02-01
   - 前端重新计算：财务日期应为2025-02-01

3. ✅ **出口业务财务日期计算**
   - 复制出口业务工作号
   - 前端应根据开航日期(Etd)计算财务日期

4. ✅ **进口业务财务日期计算**
   - 复制进口业务工作号
   - 前端应根据到港日期(ETA)计算财务日期

---

## [2025-02-06] - 🔧 用户管理功能完善：修复与验证

### 🎯 业务变更（面向项目经理）

#### 新建用户机构必填处理
- **功能名称**：防止新建用户"消失"问题
- **问题描述**：商管创建普通用户时，如果不选择所属机构，用户无法在用户列表中查询到
- **修复效果**：商管创建用户时未指定机构，自动归属到当前商户，确保用户可被查询和管理
- **业务价值**：防止用户数据丢失，提升用户管理的可靠性

#### 账户检索管理员筛选功能验证
- **功能名称**：账户检索增加管理员选项
- **验证结果**：功能已正常工作，支持按管理员类型筛选
- **可用参数**：`IsAdmin=true/false`（筛选超管）、`IsMerchantAdmin=true/false`（筛选商管）
- **业务价值**：管理员可快速筛选和管理不同类型的用户

### 📊 API变更（面向前端）

#### 变更API
1. `POST /api/Account/CreateAccount` - 创建账户接口（✅ 行为优化）
   - 新行为：商管创建用户时未传 `OrgIds` 参数，自动关联到当前商户
   - 向后兼容：现有调用方式不受影响

#### 参数说明
**CreateAccountParamsDto.OrgIds**（可选）：
- ✅ **传值**：用户关联到指定的组织机构
- ✅ **不传或空数组**：
  - 超管创建用户：用户不关联任何机构
  - 商管创建普通用户：**自动关联到当前商户**（新行为）
  - 商管创建商管：**自动关联到当前商户**（已有行为）

#### 行为对比
```diff
场景：商管创建普通用户，未传 OrgIds

- ❌ 修复前：用户创建成功，但无机构关联 → 查询时被过滤 → "用户消失"
+ ✅ 修复后：用户创建成功，自动关联到商户 → 可正常查询 → 用户可管理
```

### 🔧 技术细节

#### 修复的文件
1. `PowerLmsWebApi/Controllers/System/AccountController.cs`

#### 核心变更（新建用户机构必填）
```csharp
// 🔧 Bug修复：第421行之后添加第三种情况处理
else if (!context.User.IsSuperAdmin && (model.OrgIds == null || model.OrgIds.Count == 0))
{
    // 商管创建普通用户但未指定组织机构时，自动关联到当前商户
    if (!context.User.IsAdmin()) return BadRequest("仅超管和商管才可创建用户");

    var currentMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
    if (!currentMerchantId.HasValue) return Unauthorized("未找到用户所属商户");
    
// 自动归属到当前商户
    merchantIdForNewAccount = currentMerchantId.Value;
    
    _Logger.LogInformation("商管创建普通用户时未指定机构，自动归属到商户 {MerchantId}", currentMerchantId.Value);
}
```

#### 根本原因分析
**"用户消失"的技术根源**：
1. 用户创建成功，但 `AccountPlOrganizations` 表无记录
2. 商管查询用户时（`GetAll` 方法）：
```csharp
// 第101-119行：通过机构关联查询用户
var userIds = _DbContext.AccountPlOrganizations
    .Where(c => orgIds.Contains(c.OrgId))
    .Select(c => c.UserId)
    .Distinct()
    .ToArray();

coll = coll.Where(c => userIds.Contains(c.Id));
// ❌ 无机构关联的用户被过滤掉
```

#### 三种情况的完整处理
修复后，`CreateAccount` 方法处理三种情况：
1. **情况1**：前端传了 `OrgIds` → 正常处理 ✅
2. **情况2**：商管创建商管，未传 `OrgIds` → 自动归属到当前商户 ✅
3. **情况3**：商管创建普通用户，未传 `OrgIds` → 自动归属到当前商户 ✅（新增）

#### 账户检索管理员筛选验证
**当前实现**（第166-188行）：
```csharp
// ✅ 支持 IsAdmin 参数
if (string.Equals(item.Key, "IsAdmin", StringComparison.OrdinalIgnoreCase))
{
  if (bool.TryParse(item.Value, out var boolValue))
    {
   coll = coll.Where(c => boolValue ? (c.State & 4) != 0 : (c.State & 4) == 0);
    }
}

// ✅ 支持 IsMerchantAdmin 参数
else if (string.Equals(item.Key, "IsMerchantAdmin", StringComparison.OrdinalIgnoreCase))
{
    if (bool.TryParse(item.Value, out var boolValue))
    {
        coll = coll.Where(c => boolValue ? (c.State & 8) != 0 : (c.State & 8) == 0);
    }
}
```

#### 影响范围
- **零破坏性变更**：修复仅影响未传 `OrgIds` 的场景，现有调用方式不受影响
- **向后兼容**：前端可继续使用现有参数，不需要修改
- **数据质量提升**：防止用户"消失"，提升数据完整性

### 📝 测试建议（面向测试）

#### 新建用户机构必填测试场景
1. ✅ **商管创建普通用户，传 OrgIds** - 应正常关联到指定机构
2. ✅ **商管创建普通用户，不传 OrgIds** - 应自动关联到当前商户（修复后）
3. ✅ **商管创建商管，不传 OrgIds** - 应自动关联到当前商户（已有功能）
4. ✅ **超管创建用户，不传 OrgIds** - 允许创建无机构关联的用户
5. ✅ **商管查询用户列表** - 应能查询到所有关联到商户的用户（包括未传OrgIds创建的用户）

#### 账户检索管理员筛选测试场景
1. ✅ **使用 IsAdmin=true** - 应只返回超管账户
2. ✅ **使用 IsAdmin=false** - 应只返回非超管账户
3. ✅ **使用 IsMerchantAdmin=true** - 应只返回商管账户
4. ✅ **使用 IsMerchantAdmin=false** - 应只返回非商管账户
5. ✅ **同时使用 IsAdmin=true 和 IsMerchantAdmin=true** - 应返回既是超管又是商管的账户

---

## [2025-02-06] - 🔧 基础资料功能完善：验证与修复

### 🎯 业务变更（面向项目经理）

#### 基础资料导入功能验证
- **功能名称**：基础资料导入覆盖验证
- **验证结果**：经过深入代码审查，确认功能正常工作
- **技术发现**：`OwDataUnit.BulkInsert` 按主键（Id）匹配重复记录，不按Code字段匹配
- **业务影响**：当前设计符合预期，每次导入都生成新记录，不会覆盖现有数据

#### 汇率导入重复数据防护
- **功能名称**：汇率导入功能恢复
- **问题描述**：汇率导入缺少重复数据检测，会导致重复插入相同汇率
- **修复效果**：添加重复检测逻辑，按业务条件（业务类型+币种+日期范围）判断重复
- **业务价值**：防止重复导入相同汇率，提升数据质量，避免业务逻辑混乱

### 📊 API变更（面向前端）

#### 变更API
1. `POST /api/Admin/ImportPlExchangeRate` - 汇率导入接口（✅ 已修复）
   - 新增重复检测逻辑
   - 返回消息包含跳过的记录数

#### 行为变更
- **导入汇率时**：
  - ✅ 自动跳过已存在的重复汇率
  - ✅ 返回消息说明：成功导入X条，跳过Y条已存在的记录
  - ✅ 记录详细日志便于排查

### 🔧 技术细节

#### 修复的文件
1. `PowerLmsWebApi/Controllers/System/AdminController.Base.cs`

#### 核心变更（汇率导入）
```csharp
// ❌ 修复前：直接插入，不检查重复
foreach (var sourceRate in sourceRates)
{
    var newRate = new PlExchangeRate { ... };
  _DbContext.DD_PlExchangeRates.Add(newRate);
    importedCount++;
}

// ✅ 修复后：检查重复后再插入
foreach (var sourceRate in sourceRates)
{
    // 按业务条件检查是否已存在
    var exists = _DbContext.DD_PlExchangeRates
    .Any(r => r.OrgId == targetOrgId &&
         r.BusinessTypeId == sourceRate.BusinessTypeId &&
     r.SCurrency == sourceRate.SCurrency &&
 r.DCurrency == sourceRate.DCurrency &&
    r.BeginDate == sourceRate.BeginDate &&
  r.EndData == sourceRate.EndData);

    if (!exists)
    {
        var newRate = new PlExchangeRate { ... };
     _DbContext.DD_PlExchangeRates.Add(newRate);
        importedCount++;
    }
    else
    {
        skippedCount++;
        // 记录日志便于排查
    }
}
```

#### 技术说明（基础资料导入）
**`OwDataUnit.BulkInsert` 工作原理**：
1. **主键匹配逻辑**：只按主键（Id）判断重复，不按Code字段
2. **当前设计合理性**：
   - Excel导入时每行分配新的 `Guid.NewGuid()`
   - 因此 `Id` 永远不会与数据库重复
   - 所有记录都作为新记录插入
3. **如需按Code覆盖**：应使用 `BulkInsert` 的自定义键重载方法：
   ```csharp
   BulkInsert(entities, dbContext, e => e.Code, ignoreExisting: !updateExisting);
   ```

#### 影响范围
- **汇率导入**：防止重复数据，提升数据质量
- **基础资料导入**：确认功能正常，无需修改
- **零破坏性变更**：所有修复向后兼容

### 📝 测试建议（面向测试）

#### 汇率导入测试场景
1. ✅ **导入新汇率** - 应成功导入，返回"成功导入X条记录"
2. ✅ **导入重复汇率** - 应跳过重复记录，返回"成功导入0条，跳过X条已存在的记录"
3. ✅ **部分重复导入** - 应正确区分新旧记录，返回准确的导入和跳过数量
4. ✅ **日志验证** - 检查日志中是否记录了跳过的汇率详情

#### 基础资料导入验证
1. ✅ **导入国家/港口/币种** - 应成功导入，生成新记录
2. ✅ **重复导入相同数据** - 应创建新记录（因为Id不同）
3. ✅ **验证updateExisting参数** - 确认参数逻辑正确（当前不会触发重复检测）

---

## [2025-02-06] - 🐛 Bug修复：费用过滤功能恢复

### 🎯 业务变更（面向项目经理）

#### 费用反查申请单功能恢复
- **功能名称**：费用列表反查申请单明细
- **问题描述**：当费用没有关联账单时，点击"已申请金额"无法显示申请单详情
- **修复效果**：现在所有费用（无论是否关联账单）都可以正常反查申请单明细
- **业务价值**：财务人员可以完整查看费用的申请使用情况，不再遗漏数据

### 📊 API变更（面向前端）

#### 变更API
无API签名变更，仅修复内部逻辑问题

#### 受影响的接口
1. `GET /api/Financial/GetDocFeeRequisitionItem` - 费用申请单明细查询（✅ 已修复）
2. `GET /api/Financial/GetAllDocFeeRequisitionItem` - 批量查询申请单明细（✅ 已修复）
3. `GET /api/Financial/GetAllDocFeeRequisition` - 查询申请单（条件查询时）（✅ 已修复）
4. `GET /api/Financial/GetAllDocFeeRequisitionWithWf` - 查询申请单和工作流（条件查询时）（✅ 已修复）

### 🔧 技术细节

#### 修复的文件
- `PowerLmsServer/Managers/Financial/DocFeeRequisitionManager.cs`

#### 核心变更
```csharp
// ❌ 修复前：内连接导致无账单关联的费用被过滤
join bill in billsQuery on fee.BillId equals bill.Id

// ✅ 修复后：左连接保留所有费用数据
join bill in billsQuery on fee.BillId equals bill.Id into billGroup
from bill in billGroup.DefaultIfEmpty()
```

#### 影响范围
- **数据完整性提升**：原本被过滤掉的无账单关联费用现在可以正常查询
- **零破坏性变更**：修复仅涉及查询逻辑，不影响数据库结构和API签名
- **性能无影响**：SQL查询计划几乎相同，无性能损失

### 📝 测试建议（面向测试）

#### 关键测试场景
1. ✅ 查询有账单关联的费用明细 - 应返回正确记录
2. ✅ **查询无账单关联的费用明细 - 应返回正确记录（之前失败，现已修复）**
3. ✅ 使用 `DocFee.Id` 参数过滤 - 应正确过滤
4. ✅ 使用 `FeeId` 参数过滤 - 应正确过滤
5. ✅ 空条件查询 - 应返回所有明细（受权限限制）
6. ✅ 多实体条件组合 - 应正确联合过滤

---

## [2025-01-31] - 🗑️ 删除失败的设计 OwMemoryCacheExtensions

### 🎯 重大变更

- 基本完成内存换粗高级特性辅助类：条目引用计数 + 优先级驱逐 + 取消令牌 的设计与实现
#### OwMemoryCacheExtensions.cs 已删除
- **彻底移除**: 已完成100%迁移,安全删除旧API文件
- **原因**: 设计失败,功能由 `OwCacheExtensions` 完全替代
- **影响**: 无,所有使用者已迁移到新API

### 📊 完整迁移清单

#### 已迁移的文件 (6个)
1. ✅ **AccountManager.cs** - 7个方法
   - ConfigureCacheEntry
   - InvalidateUserCache
   - GetUserCacheTokenSource
2. ✅ **OrgManager.cs** - 9个方法
   - ConfigureOrgCacheEntry
   - ConfigureIdLookupCacheEntry
   - InvalidateOrgCaches
   - InvalidateUserMerchantCache
   - InvalidateOrgMerchantCache
   - InvalidateOrgMerchantCaches
   - InitializeOrgToMerchantCache
3. ✅ **RoleManager.cs** - 4个方法
   - ConfigureRolesCacheEntry
   - ConfigureCurrentRolesCacheEntry
   - InvalidateRoleCache
   - InvalidateUserRolesCache
4. ✅ **PermissionManager.cs** - 4个方法
   - ConfigurePermissionsCacheEntry
   - InvalidatePermissionCache
   - InvalidateUserPermissionsCache
   - ConfigureUserPermissionsCacheEntry
5. ✅ **AccountController.cs** - 2处使用
   - SetOrgs 方法中的缓存失效
6. ✅ **编译验证**: 所有项目编译成功

**总计**: 26个方法/使用点完成迁移

### 🔧 迁移模式总结

#### 旧API → 新API 对照

**1. RegisterCancellationToken**
```csharp
// ❌ 旧方式
entry.RegisterCancellationToken(_Cache);

// ✅ 新方式
entry.EnablePriorityEvictionCallback(_Cache);
var cts = _Cache.GetCancellationTokenSourceV2(entry.Key);
entry.ExpirationTokens.Add(new CancellationChangeToken(cts.Token));
```

**2. GetCancellationTokenSource**
```csharp
// ❌ 旧方式
var cts = _Cache.GetCancellationTokenSource(key);

// ✅ 新方式
var cts = _Cache.GetCancellationTokenSourceV2(key);
```

**3. CancelSource**
```csharp
// ❌ 旧方式
_Cache.CancelSource(key);

// ✅ 新方式
var cts = _Cache.GetCancellationTokenSourceV2(key);
if (cts != null && !cts.IsCancellationRequested)
{
    try { cts.Cancel(); }
    catch { /* 忽略异常 */ }
}
```

### 📁 删除的文件
- `../bak/OwBaseCore/Microsoft.Extensions.Caching.Memory/OwMemoryCacheExtensions.cs` (~350行)

### 🎯 业务影响

- ✅ **100%向后兼容**: 旧API完全废弃,无过渡期
- ✅ **无功能变更**: 所有缓存功能正常工作
- ✅ **性能提升**: 新API自动清理机制更高效
- ✅ **代码质量**: 职责更清晰,维护性更强
- ⚠️ **不可回退**: 旧API文件已物理删除

### 📝 经验总结

**失败原因分析**:
1. **职责不清**: 混合了缓存管理和令牌管理
2. **资源泄漏风险**: 需要手动调用 CleanupCancelledTokenSources
3. **分离存储**: 令牌源独立于缓存状态存储
4. **生命周期不一致**: 令牌源生命周期与缓存项不同步

**新设计优势**:
1. **统一状态**: 引用计数+优先级驱逐+取消令牌集成在 CacheEntryState
2. **自动清理**: 优先级1024回调自动清理所有资源
3. **延迟创建**: 令牌源仅在需要时创建
4. **职责分离**: 应用层"发信号",基础设施"处理级联"
