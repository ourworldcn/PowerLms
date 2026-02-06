# 变更日志

## [未发布] - 2026-02-06

### ✨ 批量生成账单功能上线（阶段二）

#### 功能变更总览
**从费用批量生成账单**：实现一键从已审核费用自动创建账单，解决67个结算单位逐笔手动建账单的效率问题

#### 业务变更（面向项目经理）
- **功能**：工作号下已审核且未建账单的费用，点击"批量生成账单"后自动创建账单
- **分组规则**：按结算单位+收支方向自动分组（收入/支出分别生成）
- **字段自动带入**：从工作号自动带入主单号、港口、货物名称、重量等信息
- **币种处理**：默认人民币（CNY），同组费用必须同币种
- **原子化保证**：全部成功或全部失败，不会出现部分成功

#### API变更（面向前端）

**新增接口**：
- `POST /api/DocBill/AddDocBillsFromFees` - 从费用批量生成账单

**请求参数**（AddDocBillsFromFeesParamsDto）：
```json
{
  "token": "...",
  "feeIds": ["费用ID数组（必填）"],
  "defaultCurrency": "CNY（可选，默认CNY）"
}
```

**返回值**（AddDocBillsFromFeesReturnDto）：
```json
{
  "hasError": false,
  "createdBillIds": ["账单ID数组"]
}
```

**错误示例**：
```json
{
  "hasError": true,
  "errorCode": 400,
  "debugMessage": "结算单位 XXX 的 收入 费用存在多币种 (USD, CNY)，无法自动生成账单"
}
```

**业务规则**：
1. ✅ 只处理已审核（AuditDateTime!=null）的费用
2. ✅ 只处理未建账单（BillId==null）的费用
3. ✅ 只处理有结算单位（BalanceId!=null）的费用
4. ✅ 按 `JobId + BalanceId + IO` 三维分组（同工作号+同结算单位+同收支方向生成一个账单）
5. ✅ 同组费用必须币种一致，否则报错
6. ✅ 支持跨工作号批量操作（只要属于同一业务类型且用户有权限）

**权限要求**：
- 空运出口：D0.7.1（新建账单）
- 空运进口：D1.7.1
- 海运出口：D2.7.1
- 海运进口：D3.7.1

#### 架构调整（面向开发团队）

**新增方法**：
- `DocBillController.AddDocBillsFromFees` - 批量生成账单主方法
- `DocBillController.GenerateBillNo` - 账单号生成（待优化）

**新增DTO**：
- `AddDocBillsFromFeesParamsDto` - 批量生成参数
- `AddDocBillsFromFeesReturnDto` - 批量生成返回值

**从PlJob自动带入的字段**：
- JobNo → DocNo（业务编号）
- MblNo → MblNo（主单号）
- LoadingCode → LoadingCode（起运港）
- DestinationCode → DestinationCode（目的港）
- Etd → Etd（开航日期）
- ETA → Eta（到港日期）
- GoodsName → GoodsName（货物名称）
- PkgsCount → PkgsCount（件数）
- Weight → Weight（重量）
- ChargeWeight → ChargeWeight（计费重量）
- MeasureMent → MeasureMent（体积）
- Consignor → Consignor（发货人）
- Consignee → Consignee（收货人）
- CarrierNo → Carrier（承运人）

**账单号生成（临时实现）**：
```csharp
// 格式：BILL-yyyyMMddHHmmss-4位随机码
private string GenerateBillNo(Guid orgId, DateTime createTime)
{
    return $"BILL-{createTime:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
}
// TODO: 由开发人员后续实现业务规则的编号逻辑
```

**编译验证**：✅ 100%通过（.NET 6）

---

## [未发布] - 2026-02-06

### 🏗️ 账单控制器架构重构（阶段一）

#### 功能变更总览
**账单管理模块独立**：将账单管理功能从`PlJobController`重构独立为`DocBillController`，归属财务模块，语义更清晰，架构更合理

#### 业务变更（面向项目经理）
无业务功能变更，仅后端架构优化

#### API变更（面向前端）⚠️

**重要提示**：所有账单相关接口的路径已变更，前端需要同步修改

**路由前缀变更**：
- 旧路由：`/api/PlJob/...`
- 新路由：`/api/DocBill/...`

**接口路径对照表**：

| 功能 | 旧路径 | 新路径 | 备注 |
|------|--------|--------|------|
| 查询账单列表 | `GET /api/PlJob/GetAllDocBill` | `GET /api/DocBill/GetAllDocBill` | ⚠️ 必须修改 |
| 新增账单 | `POST /api/PlJob/AddDocBill` | `POST /api/DocBill/AddDocBill` | ⚠️ 必须修改 |
| 修改账单 | `PUT /api/PlJob/ModifyDocBill` | `PUT /api/DocBill/ModifyDocBill` | ⚠️ 必须修改 |
| 删除账单 | `DELETE /api/PlJob/RemoveDocBill` | `DELETE /api/DocBill/RemoveDocBill` | ⚠️ 必须修改 |
| 按业务查询账单 | `GET /api/PlJob/GetDocBillsByJobIds` | `GET /api/DocBill/GetDocBillsByJobIds` | ⚠️ 必须修改 |

**DTO定义不变**：
- ✅ 所有参数和返回值DTO类名保持不变
- ✅ JSON字段名保持不变
- ✅ 权限代码保持不变

**前端修改指南**：
1. 全局搜索替换：`/api/PlJob/GetAllDocBill` → `/api/DocBill/GetAllDocBill`
2. 全局搜索替换：`/api/PlJob/AddDocBill` → `/api/DocBill/AddDocBill`
3. 全局搜索替换：`/api/PlJob/ModifyDocBill` → `/api/DocBill/ModifyDocBill`
4. 全局搜索替换：`/api/PlJob/RemoveDocBill` → `/api/DocBill/RemoveDocBill`
5. 全局搜索替换：`/api/PlJob/GetDocBillsByJobIds` → `/api/DocBill/GetDocBillsByJobIds`

#### 架构调整（面向开发团队）

**新增文件**：
- `PowerLmsWebApi/Controllers/Financial/DocBillController.cs` - 账单管理主控制器
- `PowerLmsWebApi/Controllers/Financial/DocBillController.Dto.cs` - 账单管理DTO定义

**删除文件**：
- `PowerLmsWebApi/Controllers/Business/Common/PlJobController.DocBill.cs` - 已迁移到DocBillController

**修改文件**：
- `PowerLmsWebApi/Controllers/Business/Common/PlJobController.Dto.cs` - 移除账单相关DTO

**架构优势**：
1. ✅ **语义清晰**：账单管理归属财务模块，符合业务语义
2. ✅ **职责单一**：PlJobController专注工作号管理，DocBillController专注账单管理
3. ✅ **模块聚合**：财务模块包含费用、账单、申请单、科目配置、导出等完整功能
4. ✅ **降低复杂度**：PlJobController减少一个部分类文件
5. ✅ **为批量生成做准备**：后续将在DocBillController中实现批量生成账单功能

**下一步计划**：
- ⏳ 等待账单号生成规则确认
- ⏳ 实现`CreateDocBillsFromFees`批量生成账单方法

**编译验证**：✅ 100%通过（.NET 6）

---

## [未发布] - 2026-02-06

### 🏗️ 费用控制器架构重构

#### 功能变更总览
**费用管理模块独立**：将费用管理功能从`PlJobController`重构独立为`DocFeeController`，归属财务模块，语义更清晰，架构更合理

#### 业务变更（面向项目经理）
无业务功能变更，仅后端架构优化

#### API变更（面向前端）⚠️

**重要提示**：所有费用相关接口的路径已变更，前端需要同步修改

**路由前缀变更**：
- 旧路由：`/api/PlJob/...`
- 新路由：`/api/DocFee/...`

**接口路径对照表**：

| 功能 | 旧路径 | 新路径 | 备注 |
|------|--------|--------|------|
| 审核费用（批量） | `POST /api/PlJob/AuditDocFee` | `POST /api/DocFee/AuditDocFee` | ⚠️ 必须修改 |
| 查询费用列表 | `GET /api/PlJob/GetAllDocFee` | `GET /api/DocFee/GetAllDocFee` | ⚠️ 必须修改 |
| 查询费用V2 | `GET /api/PlJob/GetAllDocFeeV2` | `GET /api/DocFee/GetAllDocFeeV2` | ⚠️ 必须修改 |
| 复杂查询费用 | `GET /api/PlJob/GetDocFee` | `GET /api/DocFee/GetDocFee` | ⚠️ 必须修改 |
| 新增费用 | `POST /api/PlJob/AddDocFee` | `POST /api/DocFee/AddDocFee` | ⚠️ 必须修改 |
| 修改费用 | `PUT /api/PlJob/ModifyDocFee` | `PUT /api/DocFee/ModifyDocFee` | ⚠️ 必须修改 |
| 删除费用 | `DELETE /api/PlJob/RemoveDocFee` | `DELETE /api/DocFee/RemoveDocFee` | ⚠️ 必须修改 |

**DTO定义不变**：
- ✅ 所有参数和返回值DTO类名保持不变
- ✅ JSON字段名保持不变
- ✅ 权限代码保持不变

**前端修改指南**：
1. 全局搜索替换：`/api/PlJob/AuditDocFee` → `/api/DocFee/AuditDocFee`
2. 全局搜索替换：`/api/PlJob/GetAllDocFee` → `/api/DocFee/GetAllDocFee`
3. 全局搜索替换：`/api/PlJob/GetAllDocFeeV2` → `/api/DocFee/GetAllDocFeeV2`
4. 全局搜索替换：`/api/PlJob/GetDocFee` → `/api/DocFee/GetDocFee`
5. 全局搜索替换：`/api/PlJob/AddDocFee` → `/api/DocFee/AddDocFee`
6. 全局搜索替换：`/api/PlJob/ModifyDocFee` → `/api/DocFee/ModifyDocFee`
7. 全局搜索替换：`/api/PlJob/RemoveDocFee` → `/api/DocFee/RemoveDocFee`

#### 架构调整（面向开发团队）

**新增文件**：
- `PowerLmsWebApi/Controllers/Financial/DocFeeController.cs` - 费用管理主控制器
- `PowerLmsWebApi/Controllers/Financial/DocFeeController.Dto.cs` - 费用管理DTO定义

**删除文件**：
- `PowerLmsWebApi/Controllers/Business/Common/PlJobController.DocFee.cs` - 已迁移到DocFeeController

**修改文件**：
- `PowerLmsWebApi/Controllers/Business/Common/PlJobController.Dto.cs` - 移除费用相关DTO

**架构优势**：
1. ✅ **语义清晰**：费用管理归属财务模块，符合业务语义
2. ✅ **职责单一**：PlJobController专注工作号管理，DocFeeController专注费用管理
3. ✅ **模块聚合**：财务模块包含费用、申请单、科目配置、导出等完整功能
4. ✅ **降低复杂度**：PlJobController减少一个部分类文件

**编译验证**：✅ 100%通过（.NET 6）

---

## [未发布] - 2026-02-06

### ✨ 费用批量审核功能升级

#### 功能变更总览
升级费用审核接口，支持单笔和批量操作，解决67个单位费用逐笔审核效率低下的问题

#### 业务变更（面向项目经理）
- 原来只能单笔审核费用，现支持批量审核多笔费用
- 批量操作采用原子化策略：全部成功或全部失败，保证数据一致性
- 影响范围：空运出口/进口、海运出口/进口费用审核（D0.6.7、D1.6.7、D2.6.7、D3.6.7）

#### API变更（面向前端）

**接口**：`POST /api/DocFee/AuditDocFee`（路径已变更，见控制器重构说明）

**参数变更**：`feeId`（单个GUID）→ `feeIds`（GUID数组）

**返回值变更**：增加 `successCount`、`failureCount`、`results` 统计信息

**向后兼容**：前端可传单个Id数组实现单笔审核

---

### 🐛 申请单状态刷新问题修复

#### 功能变更总览
**工作流状态同步修复**：修正审批完成后申请单状态未刷新的问题，确保工作流状态和业务单据状态原子性提交

#### 业务变更（面向项目经理）
- **问题现象**：申请单审批完成后，前端页面状态未刷新，用户仍可编辑已审批的申请单
- **修复效果**：审批完成后，前端立即获取到最新状态，编辑功能自动禁用
- **影响范围**：
  - ✅ OA日常费用申请单（OaExpenseRequisition）
  - ✅ 主营业务费用申请单（DocFeeRequisition）

#### API变更（面向前端）
- **无API接口变更**：仅修复后端数据提交逻辑
- **前端验证要点**：
  1. 申请单审批通过后，页面状态正确显示为"已审批"
  2. 已审批的申请单，编辑按钮自动禁用
  3. 申请单拒绝后，状态正确显示为"已拒绝"

#### 架构调整（面向开发团队）

**问题根因**：
```csharp
// ❌ 错误的执行顺序（修复前）
_DbContext.SaveChanges(); // 仅保存工作流数据
_WfManager.SetWorkflowState(wf.Id, newState); // 回调修改业务单据状态（未保存）
// ❌ 缺少第二次SaveChanges()
```

**修复方案**：
```csharp
// ✅ 正确的执行顺序（修复后）
_WfManager.SetWorkflowState(wf.Id, newState); // 先触发回调修改DbContext
_DbContext.SaveChanges(); // 统一保存所有修改
```

**修改文件**：
- `PowerLmsWebApi/Controllers/System/WfController.cs` - 调整SaveChanges()调用顺序

**技术细节**：
- ✅ 工作流状态和业务单据状态在同一事务中提交
- ✅ 回调方法使用ChangeTracker和Local缓存，无额外数据库查询
- ✅ 保证事务原子性（ACID）

**验证状态**：
- ✅ 后端代码已修复
- ✅ 编译验证通过
- ⏳ 待前端测试验证

---

## [未发布] - 2026-01-27

### 📋 空运出口实体命名规范统一

#### 功能变更总览
**实体字段命名规范化**：主单和分单实体全面修正违反命名规范的字段名，统一使用`Kind`和`Category`后缀，提升代码可维护性

#### 业务变更（面向项目经理）
无业务功能变更，仅实体字段命名优化，数据库迁移后前端需同步更新字段名

#### API变更（面向前端）

**主单和分单字段名变更**：
- `CnrLType` → `CnrLinkKind`（发货人联系人类型）
- `CneLType` → `CneLinkKind`（收货人联系人类型）
- `NtLType` → `NtLinkKind`（通知货人联系人类型）
- `RateClass` → `RateCategory`（运价等级）
- `PkgsType` → `PkgsKind`（包装方式）
- `FreightClass` → `FreightCategory`（服务等级）

**影响范围**：
- ✅ 主单实体（EaMawb）
- ✅ 分单实体（EaHawb）
- ⚠️ 前端需同步修改JSON字段名

#### 架构调整（面向开发团队）

**命名规范（编程规范3.2节）**：
- ❌ **禁止Type后缀**：字段名不得以`Type`结尾
- ❌ **禁止Class后缀**：字段名不得以`Class`结尾
- ✅ **推荐后缀**：使用`Kind`、`Category`、`Group`等语义化后缀

**修改清单**：

| 旧字段名 | 新字段名 | 实体 | 说明 |
|---------|---------|------|------|
| CnrLType | CnrLinkKind | EaMawb/EaHawb | 发货人联系人类型 |
| CneLType | CneLinkKind | EaMawb/EaHawb | 收货人联系人类型 |
| NtLType | NtLinkKind | EaMawb/EaHawb | 通知货人联系人类型 |
| RateClass | RateCategory | EaMawb/EaHawb | 运价等级 |
| PkgsType | PkgsKind | EaMawb/EaHawb | 包装方式 |
| FreightClass | FreightCategory | EaMawb/EaHawb | 服务等级 |

**编译验证**：✅ 100%通过（.NET 6）

---

### 📋 空运出口分单CRUD控制器开发

#### 功能变更总览
**分单制作模块完成**：参照主单控制器架构，完成空运出口分单（HAWB）及其子表的完整CRUD控制器和DTO开发

#### 业务变更（面向项目经理）
空运出口分单制作模块上线，支持分单的创建、查询、修改、删除操作

#### API变更（面向前端）

**新增接口（分单主表）**：
- `GET /api/EaHawb/GetAllPlEaHawb` - 查询分单列表（权限：D0.16.2）
- `POST /api/EaHawb/AddPlEaHawb` - 创建分单（权限：D0.16.1）
- `PUT /api/EaHawb/ModifyPlEaHawb` - 修改分单（权限：D0.16.3）
- `DELETE /api/EaHawb/RemovePlEaHawb` - 删除分单（权限：D0.16.4）

**新增接口（分单其他费用）**：
- `GET /api/EaHawb/GetAllEaHawbOtherCharge` - 查询分单其他费用列表（权限：D0.16.2）
- `POST /api/EaHawb/AddEaHawbOtherCharge` - 创建分单其他费用（权限：D0.16.1）
- `PUT /api/EaHawb/ModifyEaHawbOtherCharge` - 修改分单其他费用（权限：D0.16.3）
- `DELETE /api/EaHawb/RemoveEaHawbOtherCharge` - 删除分单其他费用（权限：D0.16.4）

**新增接口（分单委托明细）**：
- `GET /api/EaHawb/GetAllEaHawbCubage` - 查询分单委托明细列表（权限：D0.16.2）
- `POST /api/EaHawb/AddEaHawbCubage` - 创建分单委托明细（权限：D0.16.1）
- `PUT /api/EaHawb/ModifyEaHawbCubage` - 修改分单委托明细（权限：D0.16.3）
- `DELETE /api/EaHawb/RemoveEaHawbCubage` - 删除分单委托明细（权限：D0.16.4）

#### 架构调整（面向开发团队）

**控制器与DTO**：
- 主控制器：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.cs`
- 其他费用子表：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.OtherCharge.cs`
- 委托明细子表：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.Cubage.cs`
- DTO定义：`PowerLmsWebApi/Controllers/Business/AirFreight/EaHawbController.Dto.cs`
- 路由前缀：`/api/EaHawb`

**DbContext扩展**：
- `DbSet<EaHawb> EaHawbs`（空运出口分单表）
- `DbSet<EaHawbOtherCharge> EaHawbOtherCharges`（分单其他费用表）
- `DbSet<EaHawbCubage> EaHawbCubages`（分单委托明细表）

**权限配置**：
- D0.16.1（新建分单）
- D0.16.2（查看分单）
- D0.16.3（编辑分单）
- D0.16.4（删除分单）

---

## [历史记录] - 2026-01-26

### 📋 空运出口主单命名规范重构

#### 功能变更总览
移除空运出口主单相关实体的`Pl`前缀，采用更简洁的`Ea`（Export Air）前缀

#### 架构调整

**实体类重命名**：
- `PlEaMawb` → `EaMawb`（空运出口主单）
- `PlEaMawbOtherCharge` → `EaMawbOtherCharge`（主单其他费用）
- `PlEaCubage` → `EaCubage`（主单委托明细）
- `PlEaGoodsDetail` → `EaGoodsDetail`（主单品名明细）
- `PlEaContainer` → `EaContainer`（主单集装器）

**控制器重命名**：
- `PlEaMawbController` → `EaMawbController`
- 文件夹：`PowerLmsWebApi/Controllers/Business/AirFreight/`

**DbContext更新**：
- `PlEaMawbs` → `EaMawbs`
- `PlEaMawbOtherCharges` → `EaMawbOtherCharges`
- `PlEaCubages` → `EaCubages`
- `PlEaGoodsDetails` → `EaGoodsDetails`
- `PlEaContainers` → `EaContainers`

**权限配置**：
- 所有控制器方法已按照权限.md配置正确权限（D0.15.1~D0.15.4）

