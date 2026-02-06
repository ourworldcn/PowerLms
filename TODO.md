# 📋 PowerLms 待办任务清单

## 🔥 本周任务（服务器端）

### 1. 基础数据刷新优化
- **任务**：字典/港口信息自动刷新周期调整
- **详情**：将自动刷新周期从5分钟调整为1小时，减少90+MB数据频繁加载导致的卡顿
- **保留功能**：主动刷新入口，用户可手动刷新或重新登录获取最新数据

### 2. 申请单状态刷新问题修复 ✅ **已修复**
- **问题**：申请单批复完成后页面状态未刷新，导致仍可编辑
- **根因**：WfController.Send方法中SaveChanges()调用时机错误，工作流数据先提交，但回调修改的业务单据状态未保存
- **修复方案**：调整SaveChanges()调用顺序
  1. 先触发回调修改DbContext中的业务单据状态
  2. 再统一调用SaveChanges()提交所有修改（工作流数据+业务单据状态）
- **修复文件**：`PowerLmsWebApi/Controllers/System/WfController.cs`
- **验证状态**：✅ 代码已修复，待前端配合测试验证

### 3. 发票申请字段自动填充
- **问题**：发票申请选择购买方抬头后，推送手机号和Email未自动带出
- **需要**：检查字段映射/绑定逻辑，确保地址信息带出时同时带出联系方式

### 4. 费用批量处理功能
#### 4.1 批量审批/批量审核费用 ✅ **已完成**
- **状态**：功能已实现，通过升级现有AuditDocFee接口支持批量操作
- **实现方案**：
  - 参数：`FeeIds`列表（支持单个或多个费用Id）
  - 返回：包含成功/失败统计和每个费用的详细结果
  - 权限：复用现有D0.6.7等权限，每个费用独立验证
  - **策略：原子化操作** - 全部成功或全部失败，遇到第一个错误立即返回
- **修改文件**：
  - `PowerLmsWebApi/Controllers/Business/Common/PlJobController.DocFee.cs`
  - `PowerLmsWebApi/Controllers/Business/Common/PlJobController.Dto.cs`
- **向后兼容**：前端既可以传单个Id，也可以传多个Id
- **语义说明**：
  - 原函数是针对**指定ID的费用**审批（D0.6.7=审核单笔费用）
  - 与工作号级别审批（D0.6.6=审核费用完成）不同
  - 升级后保持原有语义，仅扩展为支持批量指定ID

#### 4.2 批量生成账单（新增）✅ **已完成**
- **功能**：批量审核费用后，增加"批量生成账单"功能
- **场景**：当出现67个单位费用时，避免一笔一笔手动新建账单
- **实施进度**：
  - ✅ **阶段一已完成**（账单控制器重构）
    - 创建独立的账单控制器：`DocBillController.cs`
    - 迁移现有CRUD方法到财务模块
    - 删除旧的分部文件：`PlJobController.DocBill.cs`
    - 更新DTO定义到独立文件
  - ✅ **阶段二已完成**（批量生成功能）
    - 实现`AddDocBillsFromFees`方法
    - 账单号生成封装为`GenerateBillNo`方法（待后续优化）
    - 按结算单位和收支方向自动分组
    - 从PlJob自动带入业务信息
- **实现要点**：
- 用户在费用列表勾选要建账单的费用，点击"批量生成账单"
- 筛选条件：已审核、未建账单、有结算单位
- 分组规则：按工作号(JobId) + 结算单位(BalanceId) + 收支方向(IO)三维分组
- 币种处理：同组费用必须同币种，默认CNY
- 字段自动带入：从PlJob带入主单号、港口、货物名称等信息
- 权限验证：按涉及的业务类型逐项验证（D0.7.1/D1.7.1等）
- 原子化操作：全部成功或全部失败，遇到错误立即回滚
- 灵活性强：支持选择性建账单，支持跨工作号（同类型）批量操作
- **前端API路径变更**：
  - `/api/PlJob/GetAllDocBill` → `/api/DocBill/GetAllDocBill`
  - `/api/PlJob/AddDocBill` → `/api/DocBill/AddDocBill`
  - `/api/PlJob/ModifyDocBill` → `/api/DocBill/ModifyDocBill`
  - `/api/PlJob/RemoveDocBill` → `/api/DocBill/RemoveDocBill`
  - `/api/PlJob/GetDocBillsByJobIds` → `/api/DocBill/GetDocBillsByJobIds`
  - （新增）→ `/api/DocBill/AddDocBillsFromFees`


