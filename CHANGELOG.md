# 📝 PowerLMS 变更日志

## [未发布] - 2025-01-28

### ✅ 本次完成的工作

#### 1. 开发环境优化:登录验证简化(新增)
- **问题**:开发环境下每次登录都需要输入验证码和密码,降低开发效率
- **优化**:
  - DEBUG模式下自动跳过验证码验证
  - DEBUG模式下自动跳过密码验证(仅验证用户名存在)
- **实现**:使用条件编译指令`#if DEBUG`,Release版本仍保持完整安全验证
- **日志**:开发环境跳过验证时记录详细日志,方便追踪
- **安全**:生产环境(Release)保持完整的验证码+密码双重验证
- **文件**:`PowerLmsWebApi/Controllers/System/AccountController.cs`

#### 2. 缓存依赖关系修复
- **问题**: 所有Manager中的缓存配置都存在"自己监听自己CTS"的冗余代码
- **原因**: 误将 `EnablePriorityEvictionCallback` 和 `ExpirationTokens.Add(自己的CTS)` 混用
- **修复**: 
  - **PermissionManager**: 移除 `ConfigurePermissionsCacheEntry` 和 `ConfigureUserPermissionsCacheEntry` 中的冗余自监听
  - **RoleManager**: 移除 `ConfigureRolesCacheEntry` 中的冗余自监听，并为 `ConfigureCurrentRolesCacheEntry` **添加对商户角色缓存的正确依赖**
  - **OrgManager**: 移除 `ConfigureOrgCacheEntry` 和 `ConfigureIdLookupCacheEntry` 中的冗余自监听
  - **AccountManager**: 移除 `ConfigureCacheEntry` 中的冗余自监听
- **原理**: 
  - `EnablePriorityEvictionCallback` 已自动注册CTS用于**直接失效**（通过 `Invalidate*` 方法)
  - `ExpirationTokens` 只应添加**依赖源的CTS**，用于**级联失效**
  - 自己监听自己是循环引用，没有意义
- **影响**: 修复后缓存依赖关系更清晰，级联失效逻辑更正确
- **文件**: 
  - `PowerLmsServer/Managers/Auth/PermissionManager.cs`
  - `PowerLmsServer/Managers/Auth/RoleManager.cs`
  - `PowerLmsServer/Managers/System/OrgManager.cs`
  - `PowerLmsServer/Managers/Auth/AccountManager.cs`

#### 3. Manager层缓存配置代码重复修复
- **问题**: `PermissionManager` 和 `RoleManager` 中存在重复的 `EnablePriorityEvictionCallback` 调用
- **原因**: `GetOrLoad` 方法和 `Configure` 方法都调用了相同的配置代码
- **修复**: 
  - `PermissionManager.GetOrLoadUserCurrentPermissions`: 移除重复调用，统一在 `ConfigureUserPermissionsCacheEntry` 中配置
  - `RoleManager.GetOrLoadRolesByMerchantId`: 移除重复调用，统一在 `ConfigureRolesCacheEntry` 中配置
  - `RoleManager.GetOrLoadCurrentRolesByUser`: 移除重复调用，统一在 `ConfigureCurrentRolesCacheEntry` 中配置
- **优势**: 代码更简洁，职责清晰，易于维护
- **文件**: 
  - `PowerLmsServer/Managers/Auth/PermissionManager.cs`
  - `PowerLmsServer/Managers/Auth/RoleManager.cs`

#### 4. 权限延迟加载冲突修复
- **问题**: `GetAllPermissionsInCurrentUser` 接口触发 InvalidOperationException
- **原因**: `LoadPermission` 使用 `AsNoTracking()` 后，JSON序列化时访问 `Children` 导航属性触发延迟加载，但 DbContext 已释放
- **修复**: 使用双重保障策略（`Include` 预加载 + 手动触发延迟加载）
- **文件**: `PowerLmsServer/Managers/Auth/PermissionManager.cs`

#### 5. 商管看不见其他商管账户问题修复
- **问题**: 商管登录后只能看到自己，无法查看同商户下的其他商管
- **原因**: 查询逻辑只包含机构ID集合，未包含商户ID本身
- **修复**: 在查询时将商户ID加入机构ID集合
- **文件**: `PowerLmsWebApi/Controllers/System/AccountController.cs`

#### 6. 组织树延迟加载冲突修复
- **问题**: `GetAllSubItemsOfTree` 触发延迟加载导致 InvalidOperationException
- **修复**: 移除 `AsNoTracking()`，让 EF Core 自动加载导航属性
- **文件**: `PowerLmsServer/Managers/System/OrgManager.cs`

#### 7. 日常费用申请单增加申请编号字段
- **实现**: `OaExpenseRequisition` 表增加 `ApplicationNumber` 字段
- **编号生成**: 前端调用 `POST /api/JobNumber/GeneratedOtherNumber` 接口
- **配置要求**: 需在"其他编码规则"中配置（Code: `OAEXPENSE_APP_NO`）

#### 8. GetAllCustomer 权限验证临时注销
- **背景**: 审批人无权限时无法审批
- **方案**: 临时注释权限验证（C.1.2）
- **技术债务**: 后续需精细化权限设计

---

### 📋 API 变更（面向前端）

#### 修复的接口
- **GET /api/Account/GetAll**:商管现在能正确返回所有同商户用户(包括其他商管)
- **GET /api/Authorization/GetAllPermissionsInCurrentUser**:修复延迟加载异常
- **POST /api/Account/Login**:开发环境下自动跳过验证码和密码验证(生产环境不影响)

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

#### Manager层代码质量优化（2025-01-28）
- **目的**: 消除代码重复，提升可维护性
- **实现**: 单一职责原则 - 缓存配置逻辑集中在 `Configure*CacheEntry` 方法
- **影响文件**: PermissionManager.cs, RoleManager.cs
- **优势**: 代码简洁，职责清晰，易于维护

#### 缓存依赖关系重构（2025-01-28）
- **目的**: 修正缓存依赖关系，消除冗余代码
- **问题**: 
  - 误用"自己监听自己CTS"导致循环引用
  - `RoleManager.ConfigureCurrentRolesCacheEntry` 缺少对商户角色缓存的依赖
- **修复**: 
  - 移除所有冗余的自监听代码（4个Manager共6个配置方法）
  - 建立正确的依赖关系：用户权限→全局权限+用户角色，用户角色→商户角色
  - 明确两种失效机制：直接失效（EnablePriorityEvictionCallback）和级联失效（AddExpirationToken）
- **影响文件**: PermissionManager.cs, RoleManager.cs, OrgManager.cs, AccountManager.cs
- **优势**: 
  - ✅ 缓存依赖关系更清晰
  - ✅ 级联失效逻辑更正确
  - ✅ 代码语义更明确
  - ✅ 避免循环引用
  - ✅ 减少内存开销

#### 开发体验优化（2025-01-28）
- **目的**:提升开发效率,简化调试流程
- **实现**:
  - DEBUG模式下跳过登录验证码验证
  - DEBUG模式下跳过登录密码验证(仅验证用户名)
- **影响文件**:AccountController.cs
- **优势**:
  - ✅ 开发环境下无需手动输入验证码
  - ✅ 开发环境下无需记忆测试密码
  - ✅ 只需输入用户名即可登录
  - ✅ 生产环境(Release)保持完整安全验证
  - ✅ 记录日志便于追踪

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

## [未发版本] - 2025-01-28

### 🎯 功能变更总览
1. **修复**:GetAllAccount接口商管查询范围修复
2. **修复**:GetAllPermissionsInCurrentUser延迟加载异常修复
3. **优化**:开发环境登录验证简化(跳过验证码和密码验证)
4. **架构修复**:纠正"只读缓存"原则 violations,确保缓存对象不可变

---

### 📋 详细变更

#### 1. 修复:GetAllAccount接口商管查询范围扩大
- **问题**:商户管理员调用GetAllAccount接口时,无法查询到其他商管账户(因为商管直属商户,不属于机构树)
- **根本原因**:查询逻辑仅遍历OrgCacheItem.Orgs机构树,未包含商户ID本身
- **修复方案**:在获取机构ID列表后,显式添加商户ID到查询范围
- **代码变更**:
  ```csharp
  var orgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
  orgIds.Add(merchantId.Value); // ✅ 关键修复
  ```
- **影响范围**:商管用户现在能正确查询到:
  - ✅ 所有下属机构员工
  - ✅ 所有商管账户(包括自己)
  - ✅ 直属商户的账户

#### 2. 修复:GetAllPermissionsInCurrentUser延迟加载异常
- **问题**:接口返回500错误:"A tracking query projects owned entity without corresponding owner in result"
- **根本原因**:EF Core跟踪查询时,复杂类型(PlPermission.Name)的所有者(PlPermission)被投影排除导致异常
- **修复方案**:查询时使用AsNoTracking()避免跟踪,因为该接口返回只读数据
- **代码变更**:
  ```csharp
  return _DbContext.PlPermissions
      .AsNoTracking()  // ✅ 关键修复
      .Include(c => c.Children)
      .Where(c => c.ParentId == null)
      .ToList();
  ```
- **技术说明**:
  - 使用AsNoTracking避免EF Core跟踪实体状态
  - 适用于只读查询场景,提高性能
  - 彻底消除复杂类型投影问题

#### 3. 开发环境优化:登录验证简化(新增)
- **问题**:开发环境下每次登录都需要输入验证码和密码,降低开发效率
- **优化**:
  - DEBUG模式下自动跳过验证码验证
  - DEBUG模式下自动跳过密码验证(仅验证用户名存在)
- **实现**:使用条件编译指令`#if DEBUG`,Release版本仍保持完整安全验证
- **日志**:开发环境跳过验证时记录详细日志,方便追踪
- **安全**:生产环境(Release)保持完整的验证码+密码双重验证
- **文件**:`PowerLmsWebApi/Controllers/System/AccountController.cs`

#### 4. 🔥架构修复:"只读缓存"原则 violations纠正(重要)
- **问题**:Login方法中直接修改缓存返回的只读用户对象属性
  ```csharp
  ❌ user.CurrentLanguageTag = model.LanguageTag;  // 违反只读原则
  ❌ result.User.OrgId ??= merchId;                 // 违反只读原则
  ```
- **根本原因**:OwContext中的User对象是从缓存加载的只读对象,不应被修改
- **正确原则**:
  - ✅ 缓存中的Account对象应视为只读,不可修改
  - ✅ 修改用户属性应在范围DbContext中加载实体并保存
  - ✅ 修改后失效缓存,确保下次读取最新数据
- **修复方案**:移除所有对缓存用户对象的修改操作
- **影响范围**:
  - SetUserInfo:已正确实现(范围DbContext加载→修改→保存→失效缓存)
  - ModifyPwd:已正确实现
  - ResetPwd:已正确实现
  - UpdateToken:已正确实现
  - ❌ Login:已修复,移除对user对象的修改
- **架构意义**:
  - 🔥 确保缓存一致性,避免数据竞态
  - 🔥 明确"只读缓存"vs"可写DbContext"边界
  - 🔥 所有修改操作必须遵循"加载→修改→保存→失效"四步骤

---

### 🔧 修复的接口
- **GET /api/Account/GetAll**:商管现在能正确返回所有同商户用户(包括其他商管)
- **GET /api/Authorization/GetAllPermissionsInCurrentUser**:修复延迟加载异常
- **POST /api/Account/Login**:
  - 开发环境下自动跳过验证码和密码验证(生产环境不影响)
  - ✅ **架构修复**:移除对缓存user对象的违规修改,符合"只读缓存"原则

---

### 📝 变更日志(2025-01-28)

#### Bug修复
1. **GetAllAccount商管查询范围修复**:商管能正确查询到同商户所有用户(包括其他商管)
2. **GetAllPermissionsInCurrentUser异常修复**:使用AsNoTracking避免EF Core延迟加载跟踪异常

#### 架构改造
- **"只读缓存"原则强化**:
  - ✅ 明确OwContext.User为只读对象,禁止修改
  - ✅ 修复Login方法违反原则的代码
  - ✅ 统一"加载→修改→保存→失效"修改模式
  - ✅ 确保缓存一致性和数据正确性

#### 开发体验优化(2025-01-28)
- **目的**:提升开发效率,简化调试流程
- **实现**:
  - DEBUG模式下跳过登录验证码验证
  - DEBUG模式下跳过登录密码验证(仅验证用户名)
- **影响文件**:AccountController.cs
- **优势**:
  - ✅ 开发环境下无需手动输入验证码
  - ✅ 开发环境下无需记忆测试密码
  - ✅ 只需输入用户名即可登录
  - ✅ 生产环境(Release)保持完整安全验证
  - ✅ 记录日志便于追踪

---

### 🔍 问题追踪
- **GetAllAccount商管查询范围问题**:已解决✅
- **GetAllPermissionsInCurrentUser延迟加载异常**:已解决✅
- **架构"只读缓存"原则违反**:已修复✅
- **开发环境登录调试效率低**:已优化✅
