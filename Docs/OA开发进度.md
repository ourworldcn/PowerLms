# PowerLms 开发进度跟踪

## OA日常费用申请单模块开发状态

### ? 已完成功能

#### 数据模型
- **主单字段**: 收支类型、币种、汇率、费用种类已实现
- **明细字段**: 序号、结算时间、结算账号、费用种类、金额、员工、部门、凭证号、摘要、发票号、备注
- **审核机制**: AuditDateTime、AuditOperatorId审核状态控制

#### 业务逻辑
- **完整CRUD**: `OaExpenseController`提供申请单增删改查
- **明细管理**: `OaExpenseController.Item.cs`明细项操作
- **金额校验**: `ValidateAmountConsistency()`强制校验明细合计与主单金额一致
- **权限控制**: 基于OrgId和用户角色的数据隔离
- **审核流程**: 支持审核通过/取消，集成工作流系统

#### 基础设施复用
- **文件系统**: `OwFileService`文件上传下载，支持权限控制
- **工作流**: `OwWfManager`审批流程，状态管理完整
- **凭证生成**: `VoucherNumberGeneratorController`期间-凭证字-序号
- **权限管理**: 基于PlPermission的细粒度权限控制

### ?? 部分完成功能

#### 数据结构调整
- **待移除**: 明细表`Currency`和`ExchangeRate`字段
- **待添加**: 主单结算账号统一字段
- **待删除**: 主单`SettlementMethod`和`BankAccountId`废弃字段

### ? 待实现功能

#### 文件上传集成
- 申请阶段上传发票文件功能
- 文件与申请单的ParentId关联
- 前端文件上传界面调整

#### 工作流程模板
- 新增"日常费用收款流程"模板配置
- 新增"日常费用付款流程"模板配置
- 流程编码规范化配置

#### 前端界面优化
- 申请阶段简化界面(总金额+费用种类+文件上传)
- 财务结算阶段明细管理界面
- 两阶段流程用户体验优化

## 技术债务

### 数据库迁移
```sql
-- 需要手工执行的迁移SQL
ALTER TABLE OaExpenseRequisitionItems DROP COLUMN Currency;
ALTER TABLE OaExpenseRequisitionItems DROP COLUMN ExchangeRate;
ALTER TABLE OaExpenseRequisitions DROP COLUMN SettlementMethod;
ALTER TABLE OaExpenseRequisitions DROP COLUMN BankAccountId;
ALTER TABLE OaExpenseRequisitions ADD SettlementAccountId uniqueidentifier;
```

### 重构建议
- 明细表字段名称标准化(ExpenseDate → SettlementDateTime)
- 统一错误处理和日志记录规范
- API返回值结构优化

## 设计决策记录

### 金额校验策略
- **强制校验**: 审核时必须通过金额一致性检查
- **容差机制**: 允许0.01的浮点数误差
- **用户体验**: 提供详细的错误提示信息

### 工作流集成方案
- **复用现有**: 充分利用OwWf工作流框架
- **配置驱动**: 通过流程模板配置实现业务流程
- **状态统一**: 使用标准的工作流状态管理

### 文件管理策略
- **权限继承**: 文件权限继承申请单的业务权限
- **存储优化**: 复用OwFileService的文件存储机制
- **版本控制**: 支持文件版本管理和历史追踪

---
*更新时间: 2025-01-XX*