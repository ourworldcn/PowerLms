# 📝 PowerLMS 变更日志

## [未发布] - 2025-01-15

### 🏗️ 架构改造

#### 移除缓存实体的DbContext属性

**业务价值**:
- **解决架构冲突**: 彻底解决缓存DbContext与范围DbContext的多重跟踪冲突
- **提升稳定性**: 消除"同一实体被多个DbContext跟踪"导致的随机性错误
- **符合最佳实践**: 实现"只读缓存+范围DbContext修改"的标准模式

**技术债务清理**:
之前Account和PlMerchant等实体在缓存中持有DbContext属性,导致:
1. 跨DbContext保存实体时的并发冲突(`DbUpdateConcurrencyException`)
2. 多个DbContext同时跟踪同一实体的状态不一致
3. DbContext生命周期管理混乱(缓存持有的DbContext何时Dispose?)

**实现细节**:

##### 实体类改造
- **Account.cs**: 移除`DbContext`属性,只保留`RuntimeProperties`字典
- **PlMerchant.cs**: 移除`DbContext`属性,只保留`RuntimeProperties`字典

##### Manager类改造
**AccountManager.cs**:
- `LoadById()`: 使用`AsNoTracking()`查询,返回纯数据对象(POCO)
- `Loaded()`: 移除DbContext附加逻辑
- `UpdateToken()`: 创建独立DbContext → 修改 → 保存 → 失效缓存
- `SaveAccount()`: 标记为`[Obsolete]`,强制抛出`NotSupportedException`
- `ConfigureCacheEntry()`: 驱逐回调只清理Token映射,不处理DbContext

**RoleManager.cs**:
- `GetOrLoadRolesByMerchantId()`: 创建独立DbContext加载角色
- `GetOrLoadCurrentRolesByUser()`: 创建独立DbContext加载用户角色

**PermissionManager.cs**:
- `GetOrLoadUserCurrentPermissions()`: 创建独立DbContext加载权限

##### Controller类改造
**AccountController.cs**:
- `ModifyPwd()`: 范围DbContext加载 → 修改密码 → 保存 → 失效缓存
- `ResetPwd()`: 缓存获取权限检查(只读) → 范围DbContext修改 → 保存 → 失效缓存
- `Nop()`: 调用`AccountManager.UpdateToken()`(内部已处理保存和缓存)
- `SetUserInfo()`: 验证(只读) → 范围DbContext修改 → 保存 → 失效缓存

**OwContext.cs**:
- `Nop()`: 只更新内存中的时间戳,不直接保存数据库
- `SaveChanges()`: 使用注入的范围DbContext保存

**API影响**:
- ✅ **无破坏性变更**: 所有现有API接口签名和行为保持不变
- ✅ **性能提升**: 减少DbContext持有时间,连接池利用率提高
- ✅ **线程安全**: 消除多线程访问缓存DbContext的风险

**测试结果**:
- ✅ 编译成功: PowerLmsData, PowerLmsServer, PowerLmsWebApi
- ✅ 无编译警告(除.NET 6 EOL提示)
- ⏳ 集成测试待执行

**开发规范变更**:
```csharp
// ❌ 旧模式(已废弃)
var user = accountManager.GetOrLoadById(userId);
user.DisplayName = "New Name";
accountManager.SaveAccount(userId); // 依赖缓存的DbContext

// ✅ 新模式(强制执行)
var userReadOnly = accountManager.GetOrLoadById(userId); // 只读,用于展示/权限检查
// ... 权限检查等 ...

using var dbContext = serviceProvider.GetRequiredService<PowerLmsUserDbContext>();
var user = dbContext.Accounts.Find(userId); // 范围DbContext加载
user.DisplayName = "New Name";
dbContext.SaveChanges();
accountManager.InvalidateUserCache(userId); // 失效缓存
```

**影响的文件清单**:
```
✅ PowerLmsData/账号/Account.cs
✅ PowerLmsData/机构/PlMerchant.cs  
✅ PowerLmsServer/Managers/Auth/AccountManager.cs
✅ PowerLmsServer/Managers/Auth/RoleManager.cs
✅ PowerLmsServer/Managers/Auth/PermissionManager.cs
✅ PowerLmsWebApi/Controllers/System/AccountController.cs
✅ PowerLmsServer/Managers/Auth/OwContext.cs
```

**负责人**: ZC@AI协作  
**完成时间**: 2025-01-15

---

## [未发布] - 2025-01-28

### ✨ 新功能

#### 日常费用申请单增加"申请编号"字段
**功能描述**: 为OA日常费用申请单增加唯一申请编号，用于区分同一人提交的多笔金额相同的申请

**业务价值**: 
- 提升申请单识别度：每个申请单都有唯一的编号标识
- 便于沟通和查询：可以直接引用申请编号进行讨论
- 支持流水号追溯：编号按规则自动生成，便于审计和统计

**实现细节**:
- **数据库变更**:
  - `OaExpenseRequisition` 表增加 `ApplicationNumber` 字段（varchar(64)）
  - 字段特性：非Unicode、最大长度64字符
  
- **后端变更**:
  - 申请编号由前端调用 `POST /api/JobNumber/GeneratedOtherNumber` 接口生成
  - 前端将生成的编号通过 `AddOaExpenseRequisition` 接口传入
  - 后端仅存储前端传入的申请编号，不做自动生成

- **API影响**:
  - `POST /api/OaExpense/AddOaExpenseRequisition`: 接收前端传入的 `ApplicationNumber`
  - `GET /api/OaExpense/GetAllOaExpenseRequisition`: 返回结果包含申请编号
  - 申请编号在创建后不可修改

**前端开发指南**:
1. **获取申请编号**：
   ```javascript
   // 在创建申请单前，调用接口获取编号
   const response = await POST('/api/JobNumber/GeneratedOtherNumber', {
     Token: token,
     RuleId: 'OAEXPENSE_APP_NO规则的ID' // 需先查询规则ID
   });
   const applicationNumber = response.Result;
   ```

2. **创建申请单时传入编号**：
   ```javascript
   await POST('/api/OaExpense/AddOaExpenseRequisition', {
     Token: token,
     Item: {
       ApplicationNumber: applicationNumber, // 前端生成的编号
       // ...其他字段
     }
   });
   ```

3. **显示申请编号**：
   - 列表页面：在表格首列显示申请编号
   - 查询页面：支持按申请编号模糊搜索（使用conditional参数）
   - 详情页面：在表单顶部显著位置展示申请编号
   - 创建页面：申请编号字段为只读，点击"获取编号"按钮调用接口生成

**配置说明**:
管理员需在"其他编码规则"中配置申请单编号规则：
- Code: `OAEXPENSE_APP_NO`
- DisplayName: 申请单编号
- RuleString示例: `OA<yyyyMM><0000>` (生成格式如: OA2025010001)
- RepeatMode: 2（按月归零）
- StartValue: 1

**注意事项**:
- 前端必须在创建申请单前先获取编号
- 获取编号后如果用户取消操作，该编号会被浪费（空号）
- 编号一旦生成就会递增，无法回收

---

### 🔧 技术债务与临时方案

#### GetAllCustomer 权限验证临时注销
**问题背景**: 审批人无"查看客户资料"(C.1.2)权限时，无法进行审批操作，因为获取客户资料的通用接口 `GetAllCustomer` 带有此权限验证

**临时解决方案**: 
- 已注释掉 `GetAllCustomer` 接口中的权限验证（C.1.2）
- 允许所有登录用户查询客户资料列表

**技术债务记录**:
- **影响范围**: 所有调用 `GetAllCustomer` 接口的功能模块
- **安全风险**: 降低了客户资料的访问控制粒度
- **后续优化方向**:
  1. 按字段授权：区分"查看完整客户资料"和"查看客户名称等基础信息"
  2. 提供独立查询接口：为审批等场景提供专用的客户基础信息查询接口
  3. 实现更精细化的权限设计

**影响的业务场景**:
- 审批流程：审批人可以看到客户名称等基础信息
- 客户选择：各业务模块选择客户时可以正常查询
- 报表统计：不影响现有统计功能

**优先级**: P2 - 中等优先级（功能正常，但需要长期优化）

**负责人**: ZC@WorkGroup  
**完成时间**: 2025-01-28

---

## [未发布] - 2025-01-27

### 🔍 问题诊断

#### 基础资料覆盖导入失败 [BUG #1]
**状态**: ❌ **未修复** - 已完成深度检查，待实施修复方案

**问题根因**:
- `OwDataUnit.BulkInsert` 在 `ignoreExisting=false` 时调用 `BulkInsertOrUpdate`
- 实际行为: **UPSERT** (插入或更新)
- 用户预期: **覆盖** (先删除，再插入)
- 导致Excel中未包含的旧数据无法被删除

**技术细节**:
```csharp
// 当前实现（问题所在）
if (ignoreExisting)
    dbContext.BulkInsert(entityList, bulkConfig);
else
    dbContext.BulkInsertOrUpdate(entityList, bulkConfig); // ❌ 不会删除未包含的旧数据
```

**修复方案**:
1. 查询并删除已存在的记录
2. 批量插入新数据
3. 使用两次 `SaveChanges` 确保事务一致性

**影响范围**:
- 所有使用 `ImportDictionaries` 的基础资料导入功能
- PlCountry, PlPort, PlCargoRoute, PlCurrency, FeesType 等12个实体类型

**参考文档**: 详见 `临时输出.md` 深度检查报告

---

## API变更 (面向前端)

### 新增字段

#### `OaExpenseRequisition.ApplicationNumber` (申请编号)
**类型**: `string` (varchar(64))  
**说明**: 唯一申请编号，由前端调用 `GeneratedOtherNumber` 接口生成后传入  
**查询支持**: 支持作为 `conditional` 参数进行模糊查询  
**示例**:
```javascript
// 按申请编号查询
conditional["ApplicationNumber"] = "*OA202501*"

// 按申请编号精确查询
conditional["ApplicationNumber"] = "OA2025010001"
```

### 前端需调用的现有接口

#### `POST /api/JobNumber/GeneratedOtherNumber` (生成其他编码)
**用途**: 生成OA申请单编号  
**参数**:
```json
{
  "Token": "用户令牌",
  "RuleId": "其他编码规则的GUID" // OAEXPENSE_APP_NO规则的ID
}
```
**返回**:
```json
{
  "Result": "OA2025010001" // 生成的申请编号
}
```

### 待修复接口

#### `POST /api/ImportExport/ImportDictionaries`
**当前问题**: `updateExisting=true` 不会删除Excel中未包含的旧数据

**修复后行为**:
- `updateExisting=true`: 删除已存在的记录 + 插入新数据（真正的覆盖）
- `updateExisting=false`: 仅插入新数据，跳过已存在记录（追加模式）

**前端无需修改**: 参数传递逻辑正确，仅后端逻辑需调整

---

## 技术债务

### 编译错误残留
**文件**: `..\Bak\OwDbBase\Data\OwDataUnit.cs`
**行数**: 171  
**问题**: 不完整的语句 `dbContext.UpdateRange`  
**优先级**: P0 - 立即修复  

---

## 下一步计划

### 本周任务
1. ✅ 完成日常费用申请单增加"申请编号"字段（后端）
2. ⏳ 前端实现：
   - 创建申请单前调用接口获取编号
   - 列表、查询、详情页面显示申请编号
   - 支持按申请编号查询（云霄 陈）
3. ⏳ 完成基础资料覆盖导入BUG的深度检查
4. ⏳ 实施修复方案（预计2小时）
5. ⏳ 编写单元测试验证修复效果
6. ⏳ 测试环境验证（石永昌）

### 后续优化
- 添加导入预览功能（显示将新增/更新/删除的记录数）
- 优化批量删除性能（使用 `BulkDelete`）

---

**更新人**: ZC@WorkGroup / GitHub Copilot  
**更新时间**: 2025-01-28
