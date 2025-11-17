# 📝 PowerLMS 变更日志

## [未发布] - 2025-01-28

### ✅ 本次完成的工作

#### 1. 商管看不见其他商管账户问题修复
- **问题**: 商管登录后只能看到自己，无法查看同商户下的其他商管
- **原因**: 查询逻辑只包含机构ID集合，未包含商户ID本身
- **修复**: 在查询时将商户ID加入机构ID集合
- **文件**: `PowerLmsWebApi/Controllers/System/AccountController.cs`

#### 2. 组织树延迟加载冲突修复
- **问题**: `GetAllSubItemsOfTree` 触发延迟加载导致 InvalidOperationException
- **修复**: 移除 `AsNoTracking()`，让 EF Core 自动加载导航属性
- **文件**: `PowerLmsServer/Managers/System/OrgManager.cs`

#### 3. 日常费用申请单增加申请编号字段
- **实现**: `OaExpenseRequisition` 表增加 `ApplicationNumber` 字段
- **编号生成**: 前端调用 `POST /api/JobNumber/GeneratedOtherNumber` 接口
- **配置要求**: 需在"其他编码规则"中配置（Code: `OAEXPENSE_APP_NO`）

#### 4. GetAllCustomer 权限验证临时注销
- **背景**: 审批人无权限时无法审批
- **方案**: 临时注释权限验证（C.1.2）
- **技术债务**: 后续需精细化权限设计

---

### 📋 API 变更（面向前端）

#### 修复的接口
- **GET /api/Account/GetAll**: 商管现在能正确返回所有同商户用户（包括其他商管）

#### 新增字段
- **OaExpenseRequisition.ApplicationNumber**: 申请编号（varchar(64)），支持模糊查询

#### 前端需调用的接口
- **POST /api/JobNumber/GeneratedOtherNumber**: 生成申请编号

---

### 🏗️ 架构改造（已完成）

#### 移除缓存实体的DbContext属性（2025-01-15）
- **目的**: 解决缓存DbContext与范围DbContext的多重跟踪冲突
- **实现**: "只读缓存+范围DbContext修改"模式
- **影响文件**: Account.cs, PlMerchant.cs, AccountManager.cs, AccountController.cs, RoleManager.cs, PermissionManager.cs, OwContext.cs
- **API影响**: 无破坏性变更

---

### 🔧 技术债务

#### 基础资料覆盖导入失败 [BUG #1]
- **状态**: ❌ 未修复
- **问题**: `BulkInsertOrUpdate` 不会删除Excel中未包含的旧数据
- **影响**: PlCountry, PlPort, PlCargoRoute 等12个实体类型

#### GetAllCustomer 权限验证临时注销
- **状态**: ✅ 已实施临时方案
- **后续**: 需精细化权限设计（按字段授权或独立查询接口）

---

**更新人**: ZC@AI协作  
**更新时间**: 2025-01-28
