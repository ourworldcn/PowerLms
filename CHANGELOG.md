# 📝 PowerLMS 变更日志

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
