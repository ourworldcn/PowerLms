# ZC@WorkGroup 行动项计划 - 基于2025-08-09会议纪要

## 会议核心决议总结

### 三大主题
1. **主营业务结算单前端界面开发进度审查与UI细节调整**
2. **客户测试反馈问题的集中梳理、原因定位与任务分配**  
3. **新功能需求：为所有基础资料（字典）提供通用的导入导出功能**

---

## 🔥 高优先级任务 [立即执行]

### 任务一：客户测试反馈问题修复

#### 1.1 OA日常费用权限问题修复 ✅ **已完成**
**问题：** 调用"日常费用结算"接口时后端报权限错误  
**修复：** 修改为正确的权限代码，结算、确认和明细操作权限验证已修复  
**涉及接口：** `SettleOaExpenseRequisition`、`ConfirmOaExpenseRequisition`、`AddOaExpenseRequisitionItem`、`ModifyOaExpenseRequisitionItem`、`RemoveOaExpenseRequisitionItem`

**✅ 权限设计实现（已完成）：**
- `OA.1.1 - 日常费用结算确认`：**既是结算也是确认的权限**
  - 适用于主表结算和确认操作：`SettleOaExpenseRequisition`、`ConfirmOaExpenseRequisition`
  - 控制对申请单整体的结算确认流程权限
- `OA.1.2 - 日常费用拆分结算`：**OA日常费用申请表子表的增删改权限**  
  - 适用于申请单明细项的增加、删除、修改操作：`AddOaExpenseRequisitionItem`、`ModifyOaExpenseRequisitionItem`、`RemoveOaExpenseRequisitionItem`
  - 控制对申请单明细（子表）的编辑权限

**权限使用场景：**
- **结算操作**：使用`OA.1.1`权限，对申请单进行结算处理
- **确认操作**：使用`OA.1.1`权限，对已结算申请单进行确认
- **明细编辑**：使用`OA.1.2`权限，对申请单的明细项进行增删改操作
- **拆分结算**：使用`OA.1.2`权限，处理明细项的拆分和重新组织

**技术实现要点：**
- 结算和确认接口统一使用`OA.1.1`权限验证
- 明细项增删改接口统一使用`OA.1.2`权限验证
- 保持多租户数据隔离和状态检查
- 添加详细的权限检查日志记录

**状态：** ✅ **已完成**（权限逻辑、代码实现和业务需求均已完成）

#### 1.2 通用文件上传/下载功能统一 ✅ **已完成**
**问题：** 新上传文件无法下载，使用了废弃的旧版接口  
**修复：** 统一使用新版通用接口
**前端影响：** 需替换所有文件上传下载调用，详见API文档

#### 1.3 工作号手动录入唯一性校验 ✅ **已完成**
**需求：** 支持手动录入工作号以便新旧系统数据比对  
**实现：** 支持手动录入+自动生成，增加唯一性校验
**新增接口：** 工作号唯一性验证接口，前端实时验证使用

---

## 🎯 中优先级任务

### 任务二：通用导入导出功能开发 📋 **计划中**

#### 2.1 字典数据导入导出基础设施 🌟 核心功能
**业务价值：** 支持9个分公司快速配置基础数据，大幅提升实施效率
**预估工期：** 6天
**新增接口：** Excel导入导出接口组，前端需添加导入/导出按钮

**前端协作要点：**
- 各基础资料列表页面增加"导入"和"导出"按钮
- 导入导出功能使用统一的API接口
- 具体接口规范详见API文档

**预估工期：** 3天

#### 2.2 技术评估报告（详细）

**🔍 涉及实体清单及Code字段状态：**

| 实体类名 | 数据库表名 | Code字段状态 | 优先级 | 备注 |
|---------|------------|-------------|--------|------|
| **PlCustomer** | PlCustomers | ✅ 有Code字段 | 🔥 最高 | **客户资料主表** |
| **PlCustomerContact** | PlCustomerContacts | ❌ 无Code字段需求 | 🟡 中等 | 客户联系人（关联到客户） |
| **PlBusinessHeader** | PlBusinessHeaders | ❌ 无Code字段需求 | 🟡 中等 | 业务负责人（关联到客户+用户+业务种类） |
| **PlTidan** | PlTidans | ❌ 无Code字段需求 | 🟡 中等 | 客户提单内容（关联到客户） |
| **CustomerBlacklist** | CustomerBlacklists | ❌ 无Code字段需求 | 🟡 中等 | 黑名单跟踪（关联到客户） |
| **PlLoadingAddr** | PlLoadingAddrs | ❌ 无Code字段需求 | 🟡 中等 | 装货地址（关联到客户） |
| **PlCurrency** | DD_PlCurrencys | ✅ 继承自NamedSpecialDataDicBase | 🔥 最高 | 客户拖欠限额币种关联 |
| **PlCountry** | DD_PlCountrys | ✅ 继承自NamedSpecialDataDicBase | 🔥 最高 | 客户地址国家关联 |
| **SimpleDataDic** | DD_SimpleDataDics | ✅ 继承自DataDicBase | 🔥 最高 | 多个简单字典关联 |
| **PlPort** | DD_PlPorts | ✅ 继承自NamedSpecialDataDicBase | 🔥 最高 | |
| **PlCargoRoute** | DD_PlCargoRoutes | ✅ 继承自NamedSpecialDataDicBase | 🔥 最高 | |
| **FeesType** | DD_FeesTypes | ✅ 继承自NamedSpecialDataDicBase | 🔥 最高 | |
| **PlExchangeRate** | DD_PlExchangeRates | ❌ 需要添加Code字段 | 🟡 中等 | |
| **UnitConversion** | DD_UnitConversions | ❌ 需要添加Code字段 | 🟡 中等 | |
| **JobNumberRule** | DD_JobNumberRules | ❓ 需实际验证 | 🟡 中等 | |
| **OtherNumberRule** | DD_OtherNumberRules | ❓ 需实际验证 | 🟡 中等 | |
| **ShippingContainersKind** | DD_ShippingContainersKinds | ❓ 需实际验证 | 🟡 中等 | |
| **BusinessTypeDataDic** | DD_BusinessTypeDataDics | ❓ 需实际验证 | 🟡 中等 | |

**🎯 客户资料关联分析：**

**客户资料实体群：**
- **PlCustomer**：主表，包含Code字段 ✅
- **客户关联子表**：PlCustomerContact、PlBusinessHeader、PlTidan、CustomerBlacklist、PlLoadingAddr
  - 这些子表通过CustomerId关联，无需独立的Code字段
  - 导入导出时作为客户资料的一部分处理

**客户资料的字典关联：**
- **Address_CountryId** → PlCountry (✅ 有Code字段)
- **BillingInfo_CurrtypeId** → PlCurrency (✅ 有Code字段)  
- **ShipperPropertyId** → SimpleDataDic (✅ 有Code字段)
- **CustomerLevelId** → SimpleDataDic (✅ 有Code字段)
- **Airlines_PayModeId** → SimpleDataDic (✅ 有Code字段)
- **Airlines_DocumentsPlaceId** → SimpleDataDic (✅ 有Code字段)
- **OrderTypeId** → BusinessTypeDataDic (❓ 需验证Code字段)

**🎯 核心发现：**
- ✅ **好消息**：客户资料主表及主要关联字典已有Code字段，可直接支持导入导出
- ✅ **关联完整**：客户资料的子表通过外键关联，可作为整体进行导入导出
- ⚠️ **需验证**：BusinessTypeDataDic（业务种类）是客户业务负责人表的关键关联，需确认Code字段状态
- ⚠️ **需补强**：PlExchangeRate、UnitConversion确认需要添加Code字段

**📊 外键关联分析：**
- **客户资料导入导出策略**：
  - 主表PlCustomer：使用自身Code字段
  - 国家关联：使用PlCountry.Code替代Address_CountryId
  - 币种关联：使用PlCurrency.Code替代BillingInfo_CurrtypeId
  - 简单字典关联：使用SimpleDataDic.Code替代各种xxxId
  - 子表数据：可选择性导入导出，通过客户Code关联

**⏱️ 修正后工期评估：**
- **Phase 1**: 基础设施开发（2天）
- **Phase 2**: 客户资料导入导出实现（1天）- 包含子表处理
- **Phase 3**: Code字段补强（1天）- 确认需要为2个实体添加Code字段
- **Phase 4**: 字典功能实现（1.5天）
- **Phase 5**: 测试和优化（0.5天）
- **总计**: 6天（已包含客户资料完整处理）

**⚠️ 主要风险：**
- **客户数据复杂度**：客户资料包含多个子表，需要设计合理的导入导出策略
- **Code字段缺失**：已确认PlExchangeRate、UnitConversion需要补强
- **数据完整性**：多租户数据隔离和权限控制
- **性能优化**：大批量客户数据导入的内存和事务处理

**🚀 实施策略：**
1. **优先验证**：立即确认BusinessTypeDataDic等4个实体的Code字段状态
2. **客户资料优先**：优先实现客户资料完整导入导出（包含子表）
3. **分阶段实施**：先实现已有Code字段的实体，再处理需要补强的实体
4. **关联处理**：设计统一的Code值关联策略，确保数据一致性

### 任务三：业务功能增强 🔄 **部分完成**

#### 3.1 港口类型区分功能 ✅ **已完成**
**实现：** 在PlPort实体中添加PortType枚举字段（1=空运，2=海运）
**新增接口能力：** 港口查询接口`AdminController.GetAllPlPort`支持PortType字段筛选
**前端应用：** 可在港口选择界面增加类型筛选，详见API文档

**数据库变更：**
```sql
ALTER TABLE DD_PlPorts ADD [PortType] tinyint NULL;
-- 1=空运（Air），2=海运（Sea）
```

#### 3.2 单票审核列表字段补强 ✅ **已完成**（后端）
**实现：** PlJob实体添加ClosedBy字段，区分审核人和关闭人
**接口变更：** PlJob相关查询接口返回数据包含ClosedBy字段
**前端协作：** 需在单票审核列表添加"关闭人"和"关闭日期"列显示

**数据库变更：**
```sql
ALTER TABLE PlJobs ADD [ClosedBy] uniqueidentifier NULL;
```

#### 3.3 汇率获取业务种类确认 📋 **待验证**
**需求：** 确认工作号录入费用时，汇率获取按业务种类区分的逻辑
**前端验证：** 确认调用汇率接口时已传入业务种类参数

---

## 📊 时间规划与优先级

| 任务 | 优先级 | 预估工期 | 状态 | 前端影响 |
|------|--------|----------|------|----------|
| **已完成任务** ||||
| OA日常费用权限修复 | 🔥紧急 | 0.5天 | ⚠️**待确认** | 无影响 |
| 文件接口统一 | 🔥紧急 | 1天 | ✅**已完成** | 需更新调用 |
| 工作号手动录入 | 🔥紧急 | 0.5天 | ✅**已完成** | 新增验证接口 |
| 港口类型区分功能 | 🟡中等 | 0.5天 | ✅**已完成** | 新增筛选功能 |
| 单票审核字段补强 | 🟡中等 | 0.5天 | ✅**已完成**（后端） | 新增列显示 |
| **当前任务** ||||
| 通用导入导出开发 | 🌟重要 | 6天 | 📋计划中 | 新增导入导出按钮 |
| 汇率获取逻辑确认 | 🟡中等 | 0.2天 | 📋计划中 | 参数验证 |

**当前状态：**
- **已完成：** 2.5天（文件接口 + 工作号录入 + 港口功能 + 单票审核后端）
- **待确认：** 0.5天（OA权限问题的业务逻辑确认）
- **剩余工期：** 6.2天（导入导出6天 + 汇率逻辑确认0.2天）

---

## 🤝 协作计划

### 与前端开发协作
1. **接口变更通知：** 新增字段和接口定义完成后及时通知
2. **API文档同步：** 所有接口变更更新到API文档，前端参考Swagger文档
3. **功能联调：** 完成后端开发后安排前后端联调测试

**具体协作事项：**
- 文件上传下载接口替换（已完成，需前端配合更新）
- 港口类型筛选功能（新增接口能力，接口：`AdminController.GetAllPlPort`）
- 单票审核列表字段显示（后端已就绪，需前端添加列显示）
- 导入导出功能（计划中，需前端配合添加按钮和调用逻辑）

### 与产品经理协作
1. **权限设计澄清：** 确认OA.1.1和OA.1.2的具体使用场景
2. **导入导出需求确认：** 确认具体字典表范围和功能要求
3. **验收标准：** 明确各功能的验收标准

### 数据库变更协作
1. **PlPort表增强：** 添加PortType字段（已完成）
2. **PlJob表增强：** 添加ClosedBy字段（已完成）
3. **Code字段补强：** PlExchangeRate、UnitConversion等实体（计划中）

---

## ⚠️ 风险控制

### 技术风险评估
- **Code字段缺失：** 已确认PlExchangeRate、UnitConversion需要补强
- **性能风险：** 大批量数据导入需要分批处理和事务优化
- **数据一致性：** 多租户数据隔离和权限控制
- **接口兼容性：** 确保API变更不影响现有前端功能

### 应对策略
- **分阶段交付：** 先实现已有Code字段的实体，再处理需要补强的实体
- **专项测试：** 安排专门的测试时间确保质量
- **API版本管理：** 新增接口保持向后兼容

---

## 📝 开发规范

- **数据库变更：** 手动管理数据库迁移，禁用EF自动迁移
- **代码质量：** 保持现有代码风格，添加充分注释和错误处理
- **API文档：** 所有接口变更同步更新Swagger文档
- **前端接口：** 新增接口提供完整的参数说明和返回值定义

---

## ✅ 已完成任务总结

**高优先级客户反馈问题：** 2.5项完成，0.5项待确认
- OA日常费用权限问题修复（技术完成，业务逻辑待确认）
- 文件上传下载接口统一（前端需配合更新调用）
- 工作号手动录入唯一性校验（新增验证接口）

**业务功能增强：** 2项已完成（后端）
- 港口类型区分功能（查询接口`AdminController.GetAllPlPort`新增筛选能力）
- 单票审核字段补强（接口返回数据新增ClosedBy字段）

**前端协作事项：** 
- 已完成功能的前端适配（参见API文档）
- 计划中功能的前端准备（导入导出按钮等）

**执行状态：** 🚀 继续执行通用导入导出功能开发