# 📝 PowerLms 项目变更记录

<!-- 变更日志按照WBS编号法组织，使用emoji图标增强辨识度 -->

## 1. 🎯 功能变更总览

- **✅ 空运进口API恢复** - 创建独立的PlAirborneController，提供完整的CRUD接口
- **✅ 主营业务费用申请单回退功能正确实现** - 已移动到FinancialController，针对DocFeeRequisition实体
- **✅ 导入导出服务架构重构（v2.0）** - 简单字典专用API，批量多表处理
- **✅ 机构参数表和账期管理** - 完整的实体、Controller、业务逻辑
- **✅ OA费用申请单回退功能** - 完整的Manager和Controller实现
- **✅ 工作流清理机制** - 级联删除和日志记录
- **❌ 费用过滤Bug修复** - fee_id参数过滤功能异常（待修复）

---

## 2. 📋 业务变更（面向项目经理）

### 2.1 空运进口业务恢复
**功能名称：** 空运进口业务完全恢复  
**业务价值：** 解决空运进口单据无法录入和管理的关键问题，恢复完整的空运业务流程

### 2.2 申请单回退机制
**功能名称：** 费用申请单一键回退功能  
**业务价值：** 支持已审核申请单的错误纠正，避免重新录入，提高业务处理效率

### 2.3 账期管理优化
**功能名称：** 统一账期关闭管理  
**业务价值：** 简化财务月结流程，确保账期数据的一致性和准确性

### 2.4 数据迁移能力增强
**功能名称：** 字典数据导入导出重构  
**业务价值：** 支持跨公司数据迁移，降低新公司数据初始化成本

---

## 3. 🔧 API变更（面向前端）

### 3.1 新增API
- **空运进口单CRUD** - `/api/PlAirborne/GetAllPlIaDoc`等4个接口
- **申请单回退** - `/api/Financial/RevertDocFeeRequisition`
- **OA申请单回退** - `/api/OaExpense/RevertOaExpenseRequisition`
- **账期管理** - `/api/OrganizationParameter/CloseAccountingPeriod`
- **导入导出重构** - `/api/ImportExport/GetSupportedTables`等3个接口

### 3.2 变更API
- **工作流清理** - `OwWfManager.ClearWorkflowByDocId`方法增强

### 3.3 删除API
- **单表导入导出** - 旧版本的单表模式API已删除，统一使用批量处理

---

## 4. 📅 2025-01-27 空运进口API完全恢复

### 4.1 ✅ 历史问题修复
经过Git历史核查，确认了空运进口单CRUD接口确实在历史版本中存在：
- **提交记录：** `38a1fe4` (2024年7月9日) - "增加空运进口单实体PlIaDoc，增加PlIaDoc的CRUD接口"
- **丢失原因：** 在后续的代码重构中被意外删除，可能发生在PlSeaborne控制器拆分时期
- **影响范围：** 空运进口业务功能完全无法使用，前端Swagger文档缺失相关接口

### 4.2 🔄 架构决策与实现

#### 4.2.1 独立控制器创建
参照海运业务的PlSeaborneController模式，创建了专门的空运业务控制器：
- **新控制器：** `PowerLmsWebApi/Controllers/Business/AirFreight/PlAirborneController.cs`
- **DTO文件：** `PowerLmsWebApi/Controllers/Business/AirFreight/PlAirborneController.Dto.cs`
- **业务范围：** 空运进口单和空运出口单的完整CRUD操作

#### 4.2.2 代码迁移与清理
- **DTO迁移：** 将PlJobController.Dto.cs中的空运相关DTO移动到独立文件
- **接口恢复：** 基于Git历史记录和现有DTO重新实现空运进口CRUD接口
- **代码清理：** 从PlJobController中移除已迁移的DTO定义，避免重复

### 4.3 📊 恢复的API接口

#### 4.3.1 空运进口单CRUD
```http
GET /api/PlAirborne/GetAllPlIaDoc     # 获取空运进口单列表
POST /api/PlAirborne/AddPlIaDoc       # 新增空运进口单
PUT /api/PlAirborne/ModifyPlIaDoc     # 修改空运进口单
DELETE /api/PlAirborne/RemovePlIaDoc  # 删除空运进口单
```

#### 4.3.2 空运出口单CRUD（同时提供）
```http
GET /api/PlAirborne/GetAllPlEaDoc     # 获取空运出口单列表
POST /api/PlAirborne/AddPlEaDoc       # 新增空运出口单
PUT /api/PlAirborne/ModifyPlEaDoc     # 修改空运出口单
DELETE /api/PlAirborne/RemovePlEaDoc  # 删除空运出口单
```

### 4.4 🔐 权限配置

#### 4.4.1 空运进口权限（D1系列）
- **D1.1.1.1** - 查看权限
- **D1.1.1.2** - 新增权限
- **D1.1.1.3** - 修改权限
- **D1.1.1.4** - 删除权限

#### 4.4.2 空运出口权限（D0系列）
- **D0.1.1.1** - 查看权限
- **D0.1.1.2** - 新增权限
- **D0.1.1.3** - 修改权限
- **D0.1.1.4** - 删除权限

---

## 5. 📅 2025-01-27 主营业务费用申请单回退功能架构修正

### 5.1 ✅ 重大架构修正
经过仔细核查代码和会议纪要，发现了重要的业务逻辑错误：
- **错误理解：** 之前以为要回退PlJob(工作任务)
- **正确理解：** 应该回退DocFeeRequisition(主营业务费用申请单)
- **核心发现：** PlJob本身不启动工作流，DocFeeRequisition才是启动工作流的实体

### 5.2 🔄 代码重构内容

#### 5.2.1 新增文件
- **PowerLmsServer/Managers/Financial/DocFeeRequisitionManager.cs** - 主营业务费用申请单专用管理器
  - `RevertRequisition()` - 回退申请单到初始状态
  - `CanRevert()` - 验证是否可以回退
  - `GetStatusInfo()` - 获取申请单状态信息

#### 5.2.2 修改文件
- **PowerLmsWebApi/Controllers/Financial/FinancialController.Dto.cs**
  - 新增 `RevertDocFeeRequisitionParamsDto` 和 `RevertDocFeeRequisitionReturnDto`
  
- **PowerLmsWebApi/Controllers/Financial/FinancialController.DocFeeRequisition.cs**
  - 新增 `RevertDocFeeRequisition()` 方法，提供主营业务费用申请单回退API

### 5.3 📊 API变更列表

#### 5.3.1 新增API接口
```http
POST /api/Financial/RevertDocFeeRequisition
```
**功能：** 回退主营业务费用申请单到初始状态
**位置：** PowerLmsWebApi.Controllers.FinancialController  
**权限：** F.3 (财务管理权限)

---

## 6. 📅 2025-01-27 导入导出服务架构重构（v2.0）

### 6.1 🔄 主要变更
- **架构重构**：删除单表模式，统一使用批量多表处理
- **功能分离**：简单字典独立为专门API，通用表字典保持批量处理
- **性能优化**：批量操作、流式处理、减少数据库往返

### 6.2 📊 API变更说明

#### 6.2.1 简单字典API（新增专用接口）
- `GET /ImportExport/GetSimpleDictionaryCatalogCodes` - 获取Catalog Code列表
- `GET /ImportExport/ExportSimpleDictionary` - 导出简单字典（支持多Catalog）
- `POST /ImportExport/ImportSimpleDictionary` - 导入简单字典

#### 6.2.2 通用表字典API（保留批量模式）
- `GET /ImportExport/GetSupportedTables` - 获取支持的表类型
- `GET /ImportExport/ExportMultipleTables` - 批量导出多表
- `POST /ImportExport/ImportMultipleTables` - 批量导入多表

#### 6.2.3 删除的API（不兼容变更）
- `ExportTable`、`ImportTable`、`GetSupportedTableTypes` 等单表模式API已删除

---

## 7. 📅 2025-01-26 机构参数表和账期管理

### 7.1 ✅ 完整功能实现

#### 7.1.1 新增实体
- **PlOrganizationParameter.cs** - 机构参数表实体
  - CurrentAccountingPeriod - 当前账期（YYYYMM格式）
  - BillHeader1, BillHeader2, BillFooter - 报表打印信息

#### 7.1.2 新增控制器
- **OrganizationParameterController.cs** - 机构参数管理API
  - 完整的CRUD操作
  - 权限控制和多租户安全

#### 7.1.3 账期管理功能
- **PreviewAccountingPeriodClose** - 预览账期关闭影响范围
- **CloseAccountingPeriod** - 执行账期关闭操作
- **自动账期递增** - 关闭后自动推进到下一月份

#### 7.1.4 权限配置
- **F.2.9** - 关闭账期专用权限
- **多级权限控制** - 机构参数编辑权限

---

## 8. 📅 2025-01-25 OA费用申请单回退功能

### 8.1 ✅ 完整功能实现

#### 8.1.1 Manager层实现
- **OaExpenseManager.RevertRequisition()** - 核心回退业务逻辑
- **状态枚举处理** - 正确处理OaExpenseRequisitionStatus状态
- **工作流清理** - 调用OwWfManager.ClearWorkflowByDocId()

#### 8.1.2 Controller层实现
- **OaExpenseController.RevertOaExpenseRequisition()** - HTTP API端点
- **完整的权限验证** - 使用F.4权限控制
- **错误处理和日志记录** - 详细的操作审计

#### 8.1.3 API接口详情
```http
POST /api/OaExpense/RevertOaExpenseRequisition
```
**权限：** F.4 (OA费用管理权限)

---

## 9. 📅 历史变更概要

### 9.1 PowerLms v1.0 基础功能
- 基础业务单据管理（空运/海运进出口）
- 费用管理和模板系统
- 客户资料和数据字典
- 基础权限和多租户支持

### 9.2 近期重要更新
- OaExpenseRequisition OA费用申请功能
- 工作号管理和唯一性约束
- 状态机和业务逻辑管理器
- 导入导出功能基础实现

---

**注意**：本文档记录所有重要的功能变更和技术决策，便于团队理解系统演进历程和维护代码