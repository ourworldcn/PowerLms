# 📝 PowerLMS 变更日志

## [未发布] - 2025-01-31

### 🔧 文档修正-XML注释规范化

**修正目标**: 统一规范化项目中所有查询接口的`conditional`参数注释

**修正原则**:
- 明确说明参数类型为"查询条件字典"而非"查询的条件"或"支持通用查询"
- 统一使用"键格式: 实体名.字段名"的描述方式
- 示例中使用"字典中添加 键='XXX' 值='YYY'"的清晰表达
- 避免使用容易误导的`=`符号(如"PlJob.JobNo=XXX")

**已完成的文件** (共9个文件,修正30处注释):

#### 1. FinancialController.cs (6处修正)
- ✅ `GetAllDocFeeRequisition`: "查询的条件" → "查询条件字典"
- ✅ `GetAllDocFeeRequisitionWithWf`: "查询的条件" → "查询条件字典"
- ✅ `GetAllDocFeeRequisitionItem`: "条件使用..." → "查询条件字典,键格式: 实体名.字段名"
- ✅ `GetDocFeeRequisitionItem`: "条件使用..." → "查询条件字典,键格式: 实体名.字段名"
- ✅ `GetAllDocFeeTemplate`: "查询的条件" → "查询条件字典"
- ✅ `GetAllDocFeeTemplateItem`: "查询的条件" → "查询条件字典"

#### 2. PlInvoicesController.cs (已确认)
- ✅ 所有注释格式已正确,无需修改
- ✅ `GetAllPlInvoices`: 已使用"查询条件字典"格式
- ✅ `GetAllPlInvoicesItem`: 已使用"查询条件字典,键格式"规范
- ✅ `GetDocInvoicesItem`: 已使用规范格式(已废弃接口)

#### 3. PlJobController.DocFee.cs (3处修正)
- ✅ `GetAllDocFee`: 补充"查询条件字典"说明
- ✅ `GetAllDocFeeV2`: 规范化为"查询条件字典,键格式: 实体名.字段名"
- ✅ `GetDocFee`: 规范化键格式说明,去除多余空格

#### 4. PlJobController.DocBill.cs (1处修正)
- ✅ `GetAllDocBill`: "查询的条件" → "查询条件字典"

#### 5. CustomerController.cs (7处修正)
- ✅ `GetAllCustomer`: "查询的条件" → "查询条件字典"
- ✅ `GetAllCustomerContact`: "查询的条件" → "查询条件字典"
- ✅ `GetAllPlBusinessHeader`: "查询的条件" → "查询条件字典"
- ✅ `GetAllPlTaxInfo`: "查询的条件" → "查询条件字典"
- ✅ `GetAllPlTidan`: "查询的条件" → "查询条件字典"
- ✅ `GetAllCustomerBlacklist`: "查询的条件" → "查询条件字典"
- ✅ `GetAllPlLoadingAddr`: "查询的条件" → "查询条件字典"

#### 6. AdminController.cs (6处修正)
- ✅ `GetAllPlPort`: "支持通用查询条件" → "查询条件字典。支持通用查询条件"
- ✅ `GetAllPlCargoRoute`: "支持通用查询条件" → "查询条件字典。支持通用查询条件"
- ✅ `GetAllUnitConversion`: "支持通用查询条件" → "查询条件字典。支持通用查询条件"
- ✅ `GetAllFeesType`: "支持通用查询的条件" → "查询条件字典。支持通用查询条件"
- ✅ `GetAllShippingContainersKind`: "支持通用查询条件" → "查询条件字典。支持通用查询条件"
- ✅ `GetAllSystemLog`: "已支持通用查询..." → "查询条件字典。已支持通用查询..."

#### 7. AdminController.Base.cs (3处修正)
- ✅ `GetAllPlCountry`: "查询的条件。支持通用查询条件" → "查询条件字典。支持通用查询条件"
- ✅ `GetAllPlCurrency`: "支持通用查询条件" → "查询条件字典。支持通用查询条件"
- ✅ `GetAllPlExchangeRate`: "支持通用查询条件" → "查询条件字典。支持通用查询条件"

#### 8. SubjectConfigurationController.cs (1处修正)
- ✅ `GetAllSubjectConfiguration`: "查询条件" → "查询条件字典"

#### 9. PlJobController.EaDoc.cs (1处修正)
- ✅ `GetAllHuochangChuchong`: "查询的条件" → "查询条件字典"

**修正前后对比示例**:

```xml
<!-- 修正前 -->
/// <param name="conditional">查询的条件。实体属性名不区分大小写。</param>
/// <param name="conditional">支持通用查询条件。</param>
/// <param name="conditional">条件使用 实体名.字段名 格式，值格式参见通用格式。</param>

<!-- 修正后 -->
/// <param name="conditional">查询条件字典。实体属性名不区分大小写。</param>
/// <param name="conditional">查询条件字典。支持通用查询条件。</param>
/// <param name="conditional">查询条件字典,键格式: 实体名.字段名。</param>
```

**使用示例说明**:

修正后的注释清晰地指出这是Dictionary<string, string>参数:

```csharp
// C# 代码使用方式
var conditional = new Dictionary<string, string>
{
    { "PlJob.JobNo", "ABC123" },              // 按工作号过滤
    { "DocFee.FeeTypeId", "guid-value" },     // 按费用类型过滤
};
var result = await GetAllDocFee(model, conditional);

// HTTP请求方式
GET /api/DocFee?token=xxx&PlJob.JobNo=ABC123&DocFee.FeeTypeId=guid-value
```

**影响范围**:
- 修改: 9个Controller文件
- 仅修正XML注释,不影响功能
- ✅ 编译成功验证

---

### 🔄 API功能迁移-结算单明细查询增强

**变更目标**: 统一API接口,简化前端调用

**变更内容**:
- ✅ `GetAllPlInvoicesItem`新增跨表查询功能(与`GetDocInvoicesItem`相同)
- ✅ `GetDocInvoicesItem`标记为废弃(`[Obsolete]`),但保留向后兼容
- ✅ 推荐使用`GetAllPlInvoicesItem`进行结算单明细查询

**GetAllPlInvoicesItem增强功能**:
1. **支持跨表查询条件**:
   - `PlInvoicesItem.字段名` - 本体条件
   - `PlInvoices.字段名` - 结算单总单条件
   - `DocFeeRequisitionItem.字段名` - 申请明细条件
   - `DocFeeRequisition.字段名` - 申请单条件
   - `DocFee.字段名` - 费用条件
   - `PlJob.字段名` - 工作号条件

2. **智能关联查询**:
   - 按关联距离自动优化JOIN顺序
   - 避免重复连接,提高查询性能
   - 自动去重,确保结果唯一性

3. **使用示例**:
   ```
   GET /api/PlInvoices/GetAllPlInvoicesItem?PlJob.JobNo=XXX&DocFee.FeeTypeId=YYY
   ```

**接口对比**:

| 特性 | GetDocInvoicesItem | GetAllPlInvoicesItem |
|------|-------------------|---------------------|
| 跨表查询 | ✅ 支持 | ✅ 支持(新增) |
| 返回关联实体 | ✅ 返回(Invoice/Job/Fee等) | ❌ 仅返回明细本体 |
| 状态 | ⚠️ 已废弃 | ✅ 推荐使用 |
| 性能 | 较慢(批量加载关联数据) | 较快(仅返回必要数据) |

**迁移指南**:
1. **如果只需要结算单明细数据**:
   - 使用`GetAllPlInvoicesItem`(推荐)
   - 性能更好,返回数据更简洁

2. **如果需要关联实体详细信息**:
   - 继续使用`GetDocInvoicesItem`
   - 或使用`GetAllPlInvoicesItem`后按ID批量查询关联实体

**示例代码**:
```javascript
// 推荐: 使用GetAllPlInvoicesItem
GET /api/PlInvoices/GetAllPlInvoicesItem?token=xxx&PlJob.JobNo=ABC123&DocFee.FeeTypeId=guid

// 废弃: GetDocInvoicesItem (仍可用但不推荐)
GET /api/PlInvoices/GetDocInvoicesItem?token=xxx&PlJob.JobNo=ABC123&DocFee.FeeTypeId=guid
```

**影响范围**:
- 修改: `PowerLmsWebApi/Controllers/Financial/PlInvoicesController.cs`
- 接口: `GET /api/PlInvoices/GetAllPlInvoicesItem` (功能增强)
- 接口: `GET /api/PlInvoices/GetDocInvoicesItem` (标记废弃)

---

### 🔧 代码优化-结算单明细查询重构

**优化目标**: 提升代码可读性和可维护性

**变更内容**:
- ✅ 重构`GetDocInvoicesItem`方法,按关联距离组织查询逻辑
- ✅ 使用连接状态标记避免重复JOIN
- ✅ 分块组织代码:本体过滤 → 直接关联 → 间接关联
- ✅ 优化批量加载逻辑,只加载未连接的关联数据
- ✅ 添加详细的行尾注释,提高代码可读性

**代码结构改进**:
1. **按关联距离分层**:
   - 第1层: PlInvoicesItem(本体) - 直接查询
   - 第2层: PlInvoices(结算单总单) - ParentId直接关联
   - 第3层: DocFeeRequisitionItem(申请明细) - RequisitionItemId直接关联
   - 第4层: DocFeeRequisition(申请单), DocFee(费用) - 间接关联
   - 第5层: PlJob(工作号) - 最远间接关联

2. **避免重复连接**:
   ```csharp
   // 使用标记变量记录连接状态
   var invoiceJoined = false;          // 结算单总单连接标记
   var requisitionItemJoined = false;  // 申请明细连接标记
   var feeJoined = false;              // 费用连接标记
   // ... 按需连接,避免重复
   ```

3. **行尾注释**:
   - 每个关键步骤都添加简洁的行尾注释
   - 标记变量说明其用途
   - 条件判断说明逻辑
   - 批量加载说明优化策略

**影响范围**:
- 修改: `PowerLmsWebApi/Controllers/Financial/PlInvoicesController.cs`
- 接口:`GET /api/PlInvoices/GetDocInvoicesItem`
- 功能和返回值完全不变,仅优化内部实现和代码注释

---

### 🔧 控制器重构-结算单功能独立

**重构目标**: 简化控制器结构,提高代码可维护性

**变更内容**:
- ✅ 新建`PlInvoicesController`结算单专用控制器
- ✅ 将结算单和结算单明细的11个API接口从`FinancialController`迁移到新控制器
- ✅ 新建`PlInvoicesController.Dto.cs`存放结算单专用DTO类(22个类)
- ✅ `FinancialController`现在只负责费用方案相关功能

**API路由变更** (前端适配请查阅):

| 接口名称 | 原路由 | 新路由 |
|---------|--------|--------|
| 获取结算单列表 | `/api/Financial/GetAllPlInvoices` | `/api/PlInvoices/GetAllPlInvoices` |
| 新增结算单 | `/api/Financial/AddPlInvoice` | `/api/PlInvoices/AddPlInvoice` |
| 修改结算单 | `/api/Financial/ModifyPlInvoices` | `/api/PlInvoices/ModifyPlInvoices` |
| 删除结算单 | `/api/Financial/RemovePlInvoices` | `/api/PlInvoices/RemovePlInvoices` |
| 确认结算单 | `/api/Financial/ConfirmPlInvoices` | `/api/PlInvoices/ConfirmPlInvoices` |
| 结算单明细增强查询 | `/api/Financial/GetDocInvoicesItem` | `/api/PlInvoices/GetDocInvoicesItem` |
| 获取结算单明细列表 | `/api/Financial/GetAllPlInvoicesItem` | `/api/PlInvoices/GetAllPlInvoicesItem` |
| 新增结算单明细 | `/api/Financial/AddPlInvoicesItem` | `/api/PlInvoices/AddPlInvoicesItem` |
| 修改结算单明细 | `/api/Financial/ModifyPlInvoicesItem` | `/api/PlInvoices/ModifyPlInvoicesItem` |
| 删除结算单明细 | `/api/Financial/RemovePlInvoicesItem` | `/api/PlInvoices/RemovePlInvoicesItem` |
| 批量设置结算单明细 | `/api/Financial/SetPlInvoicesItem` | `/api/PlInvoices/SetPlInvoicesItem` |

**影响文件**:
- 新增: `PowerLmsWebApi/Controllers/Financial/PlInvoicesController.cs` (结算单控制器,11个接口)
- 新增: `PowerLmsWebApi/Controllers/Financial/PlInvoicesController.Dto.cs` (结算单DTO类,22个类)
- 修改: `PowerLmsWebApi/Controllers/Financial/FinancialController.cs` (移除结算单相关代码)
- 修改: `PowerLmsWebApi/Controllers/Financial/FinancialController.Dto.cs` (移除结算单DTO类,保留费用方案DTO)

**DTO类迁移详情**:
从`FinancialController.Dto.cs`迁移到`PlInvoicesController.Dto.cs`的22个DTO类:
- 结算单相关(8个): `GetAllPlInvoicesReturnDto`, `AddPlInvoiceParamsDto`, `AddPlInvoiceReturnDto`, `ModifyPlInvoicesParamsDto`, `ModifyPlInvoicesReturnDto`, `RemovePlInvoicesParamsDto`, `RemovePlInvoicesReturnDto`, `ConfirmPlInvoicesParamsDto`, `ConfirmPlInvoicesReturnDto`
- 结算单明细相关(14个): `GetAllPlInvoicesItemReturnDto`, `AddPlInvoicesItemParamsDto`, `AddPlInvoicesItemReturnDto`, `ModifyPlInvoicesItemParamsDto`, `ModifyPlInvoicesItemReturnDto`, `RemovePlInvoicesItemParamsDto`, `RemovePlInvoicesItemReturnDto`, `SetPlInvoicesItemParamsDto`, `SetPlInvoicesItemReturnDto`, `GetPlInvoicesItemParamsDto`, `GetPlInvoicesItemReturnDto`, `GetPlInvoicesItemItem`

**前端适配指南**:
1. 将所有调用`/api/Financial/`的结算单相关接口路径替换为`/api/PlInvoices/`
2. 接口参数、返回值、业务逻辑完全不变,只是控制器路由改变
3. 使用全局搜索替换:`/api/Financial/GetAllPlInvoices` → `/api/PlInvoices/GetAllPlInvoices` (依此类推)

**示例**:
```javascript
// 修改前
GET /api/Financial/GetAllPlInvoices?token=xxx&ParentId=xxx

// 修改后
GET /api/PlInvoices/GetAllPlInvoices?token=xxx&ParentId=xxx
```

---

### 🚀 结算单明细查询增强-支持按费用过滤

**需求背景**: 为"已结算金额"追溯功能提供完整数据支持,用户需要能够按费用(DocFee)和申请单明细(DocFeeRequisitionItem)属性查询相关结算明细

**实现方案**:

1. **智能查询策略**(`GetDocInvoicesItem`接口):
   - 检测conditional参数中是否包含`DocFee.`或`PlJob.`前缀的条件
   - **有DocFee/PlJob条件**: 使用JOIN方式直接关联费用表和工作号表进行过滤
   - **无DocFee/PlJob条件**: 使用轻量级查询,后批量加载关联信息(避免N+1查询)

2. **支持的查询条件格式**:
   ```
   DocFee.Id=XXX                     // 按费用ID查询
   DocFee.FeeTypeId=XXX              // 按费用种类查询
   DocFee.IO=true                    // 按收入/支出过滤
   PlJob.JobNo=XXX                   // 按工作号查询(保留原有支持)
   DocFeeRequisitionItem.Amount=100  // 按申请单明细金额查询
   DocFeeRequisition.FrNo=XX         // 按申请单号查询
   PlInvoices.IoDateTime=...         // 按结算日期查询
   ```

3. **性能优化**:
   - 使用条件判断,仅在必要时才执行跨表JOIN
   - 无费用/工作号条件时,批量加载关联数据(避免N+1问题)
   - 保持向后兼容,不影响现有查询性能

**数据关系链**:
```
PlInvoicesItem (结算单明细)
  ↓ N:1
DocFeeRequisitionItem (申请单明细)
  ↓ N:1 (通过FeeId)
DocFee (费用) ← 🆕新增支持
  ↓ N:1
PlJob (工作号) ← 保留原有支持
```

**影响文件**:
- `PowerLmsWebApi/Controllers/Financial/FinancialController.cs`
  - `GetDocInvoicesItem`: 新增DocFee条件支持,保留PlJob条件支持,智能判断是否需要JOIN

**API接口**: `GET /api/Financial/GetDocInvoicesItem`

**使用示例**:
```
# 查询特定费用的所有结算明细
GET /api/Financial/GetDocInvoicesItem?DocFee.Id=xxx

# 查询特定费用种类的结算明细
GET /api/Financial/GetDocInvoicesItem?DocFee.FeeTypeId=xxx&DocFee.IO=true

# 查询特定工作号的所有结算明细(保留支持)
GET /api/Financial/GetDocInvoicesItem?PlJob.JobNo=JOB202501001

# 组合查询: 费用+工作号
GET /api/Financial/GetDocInvoicesItem?DocFee.IO=true&PlJob.CustomerId=xxx
```

---

### 🚀 结算单查询增强-支持费用和申请单明细条件过滤

**需求背景**: 为"已结算金额"追溯功能提供完整的数据查询能力,用户需要能够按费用(DocFee)和申请单明细(DocFeeRequisitionItem)条件查询结算单

**实现方案**:

1. **GetAllPlInvoices接口增强**:
   - 原有支持: `DocFeeRequisition.属性名`、`PlJob.属性名`
   - 🆕 新增支持: `DocFeeRequisitionItem.属性名`、`DocFee.属性名`

2. **支持的查询条件格式**:
   ```
   # 按费用查询
   DocFee.Id=XXX                      // 按费用ID查询
   DocFee.FeeTypeId=XXX               // 按费用种类查询
   DocFee.IO=true                     // 按收入/支出过滤
   
   # 按申请单明细查询
   DocFeeRequisitionItem.Id=XXX       // 按申请单明细ID查询
   DocFeeRequisitionItem.Amount=100   // 按申请金额查询
   
   # 按申请单查询
   DocFeeRequisition.FrNo=XXX         // 按申请单号查询
   
   # 按工作号查询
   PlJob.JobNo=XXX                    // 按工作号查询
   
   # 组合查询
   DocFee.IO=true&PlJob.CustomerId=XXX  // 收入费用+指定客户
   ```

3. **智能查询优化**:
   - 检测是否有子表条件(`needJoin`标志)
   - 有子表条件时:完整JOIN链条过滤
   - 无子表条件时:简单查询,性能最优

**数据关系链**:
```
PlInvoices (结算单)
  ↓ 1:N
PlInvoicesItem (结算单明细)
  ↓ N:1
DocFeeRequisitionItem (申请单明细) ← 🆕新增支持
  ↓ N:1
DocFeeRequisition (申请单)
  ↓ N:1 (通过FeeId)
DocFee (费用) ← 🆕新增支持
  ↓ N:1
PlJob (工作号)
```

**性能优化**:
- 使用`needJoin`标志避免不必要的跨表操作
- 只在存在子表条件时才执行JOIN
- `Distinct()`确保结算单不重复

**影响文件**:
- `PowerLmsWebApi/Controllers/Financial/FinancialController.cs`
  - `GetAllPlInvoices`: 新增DocFee和DocFeeRequisitionItem条件支持

**API接口**: `GET /api/Financial/GetAllPlInvoices`

**使用示例**:
```
# 查询特定费用关联的所有结算单
GET /api/Financial/GetAllPlInvoices?DocFee.Id=xxx

# 查询特定申请单明细的结算单
GET /api/Financial/GetAllPlInvoices?DocFeeRequisitionItem.Id=xxx

# 组合查询:某个工作号下的收入结算单
GET /api/Financial/GetAllPlInvoices?PlJob.JobNo=JOB202501001&DocFee.IO=true

# 查询某费用种类的所有结算单
GET /api/Financial/GetAllPlInvoices?DocFee.FeeTypeId=xxx
```

---

### 🆕 财务日期账期校验功能

**背景**: 在新建或修改工作号时,需要确保财务日期不早于当前已开放账期的起始日期

**业务规则**:
- **进口业务**: 财务日期以"到港日期"(ETA)为准
- **出口业务**: 财务日期以"开航日期"(Etd)为准
- **校验规则**: 财务日期不能早于当前账期起始日期(例如当前账期为2025年9月,财务日期不能选择9月1日之前)

**实现方案**:

1. **Manager层新增方法** (`PowerLmsServer/Managers/Business/JobManager.cs`):
   - `ValidateAccountDateAgainstPeriod`: 验证财务日期是否符合账期要求
   - 参数: 财务日期、公司ID、数据库上下文
   - 返回: (是否有效, 错误信息)

2. **Controller层集成校验** (`PowerLmsWebApi/Controllers/Business/Common/PlJobController.cs`):
   - `AddPlJob`: 新建工作号时校验财务日期
   - `ModifyPlJob`: 修改工作号时校验财务日期(仅当财务日期发生变更时)

3. **应用场景**:
   - 新建工作号时自动校验
   - 修改工作号财务日期时自动校验
   - 由前端计算财务日期,后端负责验证

**注意事项**:
- 财务日期由前端根据业务类型自动计算(进口用ETA,出口用Etd)
- 后端只负责校验财务日期是否在有效账期范围内
- 如果公司未配置账期参数或账期为空,则不进行校验(允许通过)
- 校验失败返回HTTP 400错误,附带详细错误信息

**影响文件**:
- `PowerLmsServer/Managers/Business/JobManager.cs` (新增验证方法)
- `PowerLmsWebApi/Controllers/Business/Common/PlJobController.cs` (集成校验逻辑)

---

### 🔧 账期反关闭权限修正

**问题**: 反关闭账期功能使用了错误的权限节点

**修正内容**:
- **修正前**: 使用 `F.2.9` (关闭账期权限)
- **修正后**: 使用 `F.2.10` (反关闭账期权限)

**权限说明**:
- `F.2.9` - 关闭账期: 批量关闭工作号并递增账期
- `F.2.10` - 反关闭账期: 设置目标账期并可选解关工作号

**修正原因**:
1. 符合权限文档设计规范
2. 提供更精细的权限控制
3. 允许单独授权反关闭功能

**影响文件**:
- `PowerLmsWebApi/Controllers/Business/Common/PlJobController.cs`

**API接口**: `POST /api/PlJob/ReopenAccountingPeriod`

---

## [未发布] - 2025-12-14

### ✅ 财务导出防重机制（完整实施）

**背景**: 财务数据导出后需要防止重复导出,避免数据重复和混乱

**解决方案**: 基于接口的统一防重机制 + 数据库字段支持

---

#### 1. 数据模型层改造

**定义防重接口**:
- 接口: `IFinancialExportable` (位于 `PowerLms.Data.Finance`)
- 核心字段:
  - `ExportedDateTime` (DateTime?): 导出时间戳,null表示未导出
  - `ExportedUserId` (Guid?): 导出用户ID,用于权限验证和审计

**实现接口的6个实体**:
1. `TaxInvoiceInfo` (发票)
2. `OaExpenseRequisition` (OA费用申请单主表)
3. `OaExpenseRequisitionItem` (OA费用申请单明细)
4. `DocFee` (费用记录-应收/应付计提)
5. `PlInvoices` (收款/付款结算单)

**数据库迁移**: `20251214231842_25121501.cs`

---

#### 2. 业务层通用方法

**位置**: `FinancialSystemExportManager.cs`

**已实现方法**:
- ✅ `MarkAsExported<T>`: 标记为已导出(记录时间和用户)
- ✅ `FilterUnexported<T>`: 过滤未导出数据
- ✅ `FilterExported<T>`: 过滤已导出数据

**预留方法**(下一个计划实现):
- 🔄 `UnmarkExported<T>`: 取消导出标记
- 🔄 `CanCancelExport<T>`: 验证取消权限

---

#### 3. API层改造(6个财务导出接口)

| 接口 | HTTP方法 | 改造内容 |
|-----|---------|---------|
| `ExportInvoiceToDbf` | POST | ✅ 查询前过滤+导出后标记 |
| `ExportOaExpenseToDbf` | POST | ✅ 查询前过滤+导出后标记 |
| `ExportArabToDbf` | POST | ✅ 查询前过滤+导出后标记 |
| `ExportApabToDbf` | POST | ✅ 查询前过滤+导出后标记 |
| `ExportSettlementReceipt` | POST | ✅ 查询前过滤+导出后标记+注释更新 |
| `ExportSettlementPayment` | POST | ✅ 查询前过滤+导出后标记+注释更新 |

---

#### 4. 架构设计优势

- **类型安全**: 泛型约束确保只处理实现`IFinancialExportable`的实体
- **事务一致性**: 标记方法不调用SaveChanges,由调用方控制事务
- **并发控制**: 实体已包含`RowVersion`字段自动检测冲突
- **可扩展性**: 新增财务导出只需实现接口

---

#### 5. 防重机制工作流程

```
用户发起导出请求
  ↓
1. 查询数据时自动过滤: WHERE ExportedDateTime IS NULL
2. 生成导出文件
3. 标记数据为已导出:
   - ExportedDateTime = UTC当前时间
   - ExportedUserId = 当前用户ID
4. SaveChanges() 提交事务
```

**数据状态表**:
| ExportedDateTime | 含义 | 行为 |
|-----------------|------|------|
| NULL | 未导出 | 允许导出 |
| 非NULL | 已导出 | 自动过滤 |

---

### 📋 API 变更(面向前端)

#### 行为变更
- **所有财务导出接口**:
  - ✅ 自动过滤已导出数据(ExportedDateTime不为空的记录)
  - ✅ 导出成功后自动标记导出时间和用户
  - ✅ 返回结果新增 `MarkedCount` 字段

#### 字段变更
- **付款结算单** (`PlInvoices` where IO=false):
  - ⚠️ `ConfirmDateTime` 字段逻辑已废弃(字段保留但不再使用)
  - ✅ 统一使用 `ExportedDateTime` 和 `ExportedUserId`
  - 前端查询应检查 `ExportedDateTime` 而非 `ConfirmDateTime`

---

### 📁 影响文件清单

**数据模型层**:
- `PowerLms.Data/Finance/IFinancialExportable.cs`
- `PowerLms.Data/Finance/TaxInvoiceInfo.cs`
- `PowerLms.Data/OA/OaExpenseRequisition.cs`
- `PowerLms.Data/Finance/DocFee.cs`
- `PowerLms.Data/Finance/PlInvoices.cs`
- `PowerLmsData/Migrations/20251214231842_25121501.cs`

**业务层**:
- `PowerLmsServer/Managers/Financial/FinancialSystemExportManager.cs`

**API层**:
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.cs`
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.OaExpense.cs`
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.Arab.cs`
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.Apab.cs`
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.SettlementReceipt.cs`
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.SettlementPayment.cs`

---

### 🔄 统一的取消导出标记接口（新增功能）

**实现日期**: 2025-12-14

#### 功能概述
提供统一的API接口用于取消已标记为导出状态的财务单据，使其可以重新导出。

#### 🔐 组织权限过滤完善（2025-12-14更新）

**问题**: OA_EXPENSE和ARAB/APAB的取消导出及导出任务缺少组织权限过滤

**解决方案**: 

**1. 取消导出接口（CancelFinancialExport）**:
- ✅ 添加 `GetOrgIdsForCurrentUser` 辅助方法（统一获取用户有权访问的组织机构ID）
- ✅ 添加 `ApplyOrganizationFilterForOaExpense` 实例方法（OA费用申请单权限过滤）
- ✅ 添加 `ApplyOrganizationFilterForFees` 实例方法（DocFee权限过滤，通过Job关联）
- ✅ 在 `CancelFinancialExport` 的 OA_EXPENSE、ARAB、APAB case 中应用权限过滤

**2. 导出任务静态方法**:
- ✅ 添加 `ApplyOrganizationFilterForOaExpenseStatic` 静态方法（OA费用申请单导出任务权限过滤）
- ✅ 添加 `ApplyOrganizationFilterForFeesStatic` 静态方法（DocFee导出任务权限过滤，定义在Arab.cs中）
- ✅ 修复 `ProcessOaExpenseRequisitionDbfExportTask` 中错误使用Manager泛型方法的问题

**权限过滤逻辑**:
- **超级管理员**: 可访问所有数据（无需过滤）
- **商户管理员**: 可访问整个商户下的所有组织机构数据
- **普通用户**: 只能访问当前登录的公司及下属所有非公司机构的数据（使用 `OrgManager.GetOrgIdsByCompanyId` 自动排除子公司）

**影响文件**:
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.cs` （取消导出+辅助方法）
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.OaExpense.cs` （OA导出任务权限过滤）
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.Arab.cs` （ARAB导出任务权限过滤）
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.Apab.cs` （APAB导出任务复用Arab的静态方法）

#### API接口详情

**接口路径**: `POST /api/FinancialSystemExport/CancelFinancialExport`

**权限要求**: F.6（财务接口权限）

**支持的导出业务类型代码**:
| 代码 | 说明 | 实体类型 |
|------|------|---------|
| `INVOICE` | 发票导出 | `TaxInvoiceInfo` |
| `OA_EXPENSE` | OA日常费用申请单导出 | `OaExpenseRequisition` + `OaExpenseRequisitionItem` |
| `ARAB` | A账应收本位币挂账导出 | `DocFee` (IO=true) |
| `APAB` | A账应付本位币挂账导出 | `DocFee` (IO=false) |
| `SETTLEMENT_RECEIPT` | 收款结算单导出 | `PlInvoices` (IO=true) |
| `SETTLEMENT_PAYMENT` | 付款结算单导出 | `PlInvoices` (IO=false) |

#### 请求参数（CancelFinancialExportParamsDto）
```json
{
  "token": "用户令牌",
  "exportTypeCode": "INVOICE",
  "exportedDateTimeStart": "2025-01-01T00:00:00Z",
  "exportedDateTimeEnd": "2025-01-31T23:59:59Z",
  "additionalConditions": {
    "ExportedUserId": "guid"
  },
  "reason": "数据错误需重新导出"
}
```

**参数说明**:
- `exportTypeCode` (必填): 导出业务类型代码
- `exportedDateTimeStart` (必填): 导出时间范围开始时间（ISO 8601格式）
- `exportedDateTimeEnd` (必填): 导出时间范围结束时间（ISO 8601格式）
- `additionalConditions` (可选): 额外的过滤条件字典，用于进一步缩小取消范围
- `reason` (可选): 取消原因，用于审计追踪

**典型使用场景**:
- 取消本月所有导出: `exportedDateTimeStart` = 本月1号0点, `exportedDateTimeEnd` = 本月最后一天23:59:59
- 取消今天的导出: `exportedDateTimeStart` = 今天0点, `exportedDateTimeEnd` = 今天23:59:59
- 取消指定用户的导出: 使用 `additionalConditions: { "ExportedUserId": "guid" }`

#### 返回结果（CancelFinancialExportReturnDto）
```json
{
  "hasError": false,
  "successCount": 3,
  "failedCount": 0,
  "failedIds": [],
  "message": "成功取消3条记录的导出标记"
}
```

**字段说明**:
- `successCount`: 成功取消的记录数量
- `failedCount`: 取消失败的记录数量
- `failedIds`: 取消失败的实体ID列表
- `message`: 操作结果消息

#### 业务规则
1. **基于时间范围**: 通过导出时间范围批量取消，避免传递大量实体ID
2. **组织权限隔离**: 自动应用组织权限过滤，确保用户只能取消有权限的数据
3. **状态清空**: 取消后，`ExportedDateTime` 和 `ExportedUserId` 字段将被清空
4. **安全上限**: 默认最多取消10000条记录，防止误操作
5. **审计追踪**: 可选的取消原因记录到日志
6. **子表联动**: OA费用申请单取消时自动处理子表记录

#### 使用场景
- 数据错误需要重新导出
- 导出文件丢失需要重新生成
- 测试环境清理导出标记
- 批量重置导出状态

#### 影响文件
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.cs`
- `PowerLmsWebApi/Controllers/Financial/FinancialSystemExportController.Dto.cs`

---

### 🔄 后续计划

**已完成任务**:
- ✅ 统一的取消导出接口设计和实现
- ✅ 完善权限验证和审计日志

**待完成任务**:
- 🔄 扩展到其他财务导出场景（按需添加）
- 🔄 前端界面集成（取消导出按钮和批量操作）

---

**更新人**: ZC@AI协作  
**更新时间**: 2025-12-14
