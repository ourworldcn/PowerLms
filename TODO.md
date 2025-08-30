# 📋 PowerLms项目待办任务

<!-- 待办列表按照WBS编号法组织，使用emoji图标增强辨识度 -->

## 1. 🎯 当前优先级任务 (目标：9月2日前完成)

### 1.1 ✅ 已完成任务

#### 1.1.1 字典导入导出功能重构完成 ✅ **重大架构变更**

**会议决议执行：**
- ✅ **导出方式变更**：从一次性导出所有改为按类型分别导出
- ✅ **简单字典处理**：按分类导入导出，仅处理字典项不含分类本身
- ✅ **重复数据策略**：采用覆盖（Update）模式替代忽略模式
- ✅ **动态表发现**：从DbContext自动获取实体类型，无需硬编码

**技术实现完成：**
- ✅ **统一导入导出服务**：`ImportExportService`，采用泛型和动态发现机制
- ✅ **控制器接口优化**：3个核心接口，DTO参数封装，IFormFile独立处理
- ✅ **简单字典整体处理**：作为单一表整体导入导出，不按分类拆分
- ✅ **表注释验证**：仅支持有Comment注释的表，确保DisplayName可用
- ✅ **自然路径设计**：所有接口使用控制器自然路径，无特殊路由

**核心功能接口：**
1. **GetSupportedTables** - 获取支持的表列表（字典/客户/简单字典）
2. **Export** - 通用导出功能（参数DTO封装）
3. **Import** - 通用导入功能（IFormFile + 参数DTO）

**技术架构亮点：**
- **动态发现机制**：从`_DbContext.Model.GetEntityTypes()`获取所有实体类型
- **Comment注释驱动**：通过`entityType.GetComment()`获取中文显示名称
- **智能过滤规则**：`IsDictionaryEntity()`和`IsCustomerSubTableEntity()`自动分类
- **表名一致性**：确保Excel表名与数据库表名一一对应
- **多租户安全**：严格的OrgId隔离，导入时忽略Excel中OrgId，导出时OrgId列不显示
- **职责分离**：控制器只做校验和异常处理，业务逻辑在服务类
- **依赖注入**：服务类使用`[OwAutoInjection(ServiceLifetime.Scoped)]`自动注入

**API接口简化：**
```
GET /api/ImportExport/GetSupportedTables?token=xxx&category=dictionary     # 获取字典表列表
GET /api/ImportExport/GetSupportedTables?token=xxx&category=customer       # 获取客户子表列表  
GET /api/ImportExport/GetSupportedTables?token=xxx&category=simple         # 获取简单字典(返回SimpleDataDic)
GET /api/ImportExport/Export?token=xxx&tableType=PlCountry                 # 导出国家字典
GET /api/ImportExport/Export?token=xxx&tableType=SimpleDataDic             # 导出简单字典(整体)
POST /api/ImportExport/Import + FormData                                   # 导入功能(文件+DTO参数)
```

#### 1.1.2 申请单审批回退机制完成 ✅ **核心业务功能**

**技术实现完成：**
- ✅ **Manager层业务逻辑**：DocFeeRequisitionManager & OaExpenseManager
- ✅ **Controller API实现**：FinancialController.DocFeeRequisition & OaExpenseController
- ✅ **工作流清理机制**：OwWfManager.ClearWorkflowByDocId
- ✅ **权限验证完成**：专门权限控制回退操作

#### 1.1.3 账期管理与工作号关闭机制完成 ✅ **财务核心功能**

**技术实现完成：**
- ✅ **实体创建**：PlOrganizationParameter.cs（当前账期、报表抬头等字段）
- ✅ **控制器完成**：OrganizationParameterController.cs（CRUD+账期管理）
- ✅ **API实现**：CloseAccountingPeriod & PreviewAccountingPeriodClose
- ✅ **权限配置**：F.2.9权限控制关闭账期操作

#### 1.1.4 空运进口接口恢复完成 ✅ **API修复**

**技术实现完成：**
- ✅ **空运进口CRUD接口**：创建独立的PlAirborneController
- ✅ **DTO定义完成**：空运进口单和空运出口单的完整DTO
- ✅ **功能恢复**：GetAllPlIaDoc、AddPlIaDoc、ModifyPlIaDoc、RemovePlIaDoc

### 1.2 🔴 紧急待完成任务（会议决议）

#### 1.2.1 导入导出控制器代码质量完善 ✅ **优先级：最高**

**已完成技术改进：**
- ✅ **日志记录完善**：所有方法增加详细的开始/结束/错误日志
- ✅ **错误处理规范**：统一错误返回格式，用户友好的错误信息
- ✅ **Token验证标准化**：修正为系统标准的Unauthorized()返回方式
- ✅ **参数验证增强**：文件格式验证、参数非空验证、业务逻辑验证
- ✅ **代码逻辑健壮性**：异常处理覆盖、资源释放、错误隔离
- ✅ **性能优化**：AsNoTracking查询、批量操作、重复键检查优化

**Token验证修正：**
```csharp
// 修正前（不符合系统标准）
if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context)
{
    result.HasError = true;
    result.ErrorCode = 401;
    result.DebugMessage = "身份验证失败，请重新登录";
    return Unauthorized(result);
}

// 修正后（符合系统标准）
if (_AccountManager.GetOrLoadContextByToken(paramsDto.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
```

**核心修正：**
```
ImportExportController.cs:
- GetSupportedTables: Token验证、详细日志、标准错误返回
- ExportMultipleTables: 文件大小日志、类型分析日志、混合导出限制  
- ImportMultipleTables: 文件格式验证、逐步导入尝试、成功标志控制

ImportExportService.cs:
- ImportEntityData: 行级错误处理、更新/新增计数、属性映射优化
- FindExistingEntity: 异常处理、查询日志、null安全检查
- GetEntityDataByOrgId: 性能日志、异常封装、多租户查询优化
```

#### 1.2.2 费用反查申请单明细过滤失败 🆕 **优先级：最高**

**问题描述：** 调用"获取费用申请单明细（fee-request-details）"时，按`fee_id`过滤未生效，返回所有明细

**技术细节：**
- **代码位置：** FinancialController.DocFeeRequisition.cs
- **方法名：** GetDocFeeRequisitionItem
- **问题表现：** 传入fee_id参数但过滤条件失效
- **影响范围：** 费用明细查询功能异常

**修复要求：**
1. 检查GetDocFeeRequisitionItem方法中的过滤逻辑
2. 确保fee_id参数正确应用到查询条件
3. 验证过滤结果的准确性
4. 添加相关的错误处理和日志

**预估工期：** 0.5天

#### 1.2.3 OA费用申请单增加公司字段 🆕 **优先级：最高**

**业务需求：** OA费用申请单主表需要增加一个"公司"字段，关联到客户资料表

**技术要求：**
1. **实体修改：** 在OA申请单实体中增加 `CustomerId` 字段（关联客户资料）
2. **数据库字段：** 添加外键关联到PlCustomer表
3. **DTO更新：** 更新相关的请求和响应DTO
4. **业务逻辑：** 在Manager层添加客户资料验证逻辑

**应用场景：** 用户会将一些个人费用（非公司客户）也录入客户资料，以便在此场景下选用

**预估工期：** 1天

#### 1.2.4 空运接口架构重复问题修正 🆕 **优先级：高**

**发现问题：** 空运出口单接口在PlJobController.EaDoc.cs与PlAirborneController中重复实现

**重复接口：**
- GetAllPlEaDoc() - 获取空运出口单
- AddPlEaDoc() - 新增空运出口单  
- ModifyPlEaDoc() - 修改空运出口单
- RemovePlEaDoc() - 删除空运出口单

**解决方案选择：**
- **方案A（推荐）：** 完全拆分空运控制器 - 将PlJobController.EaDoc.cs中的空运出口单接口移动到PlAirborneController
- **方案B：** 保持现有结构 - 删除PlAirborneController中的重复接口，在PlJobController中补充空运进口单接口

**架构统一要求：**
1. 与海运拆分的架构保持一致（PlSeaborneController模式）
2. 职责分离更清晰
3. 避免HTTP路由冲突

**预估工期：** 1天

## 2. 📋 中优先级后端任务（会议决议）

### 2.1 费用列表显示申请单详情功能 🆕

#### 2.1.1 费用关联申请单查询接口
**业务需求：** 在费用列表中，用户点击"已申请金额"时，能看到这笔费用在哪个/哪些申请单中被引用

**技术要求：**
1. **新增接口：** 根据费用ID查询关联的申请单信息
2. **返回数据：** 申请单号、申请金额、申请人、申请时间
3. **权限验证：** 确保用户有权查看相关申请单
4. **性能优化：** 考虑费用和申请单的多对多关系查询效率

**接口设计：**
```
GET /api/Financial/GetFeeRequisitionDetails?feeId={id}&token={token}
返回：List<FeeRequisitionDetailDto>
```

**预估工期：** 1天

### 2.2 客户资料有效性管理功能 🆕

#### 2.2.1 软删除状态管理
**业务需求：** 客户资料目前无法安全删除（因为存在关联业务），需要一种方式将其"停用"

**技术要求：**
1. **实体字段：** 在PlCustomer表增加 `IsActive` 字段（有效/无效状态）
2. **状态切换接口：** 单独的启用/停用API，不与其他编辑操作混合
3. **查询过滤：** 默认查询只返回有效客户，选择器默认只显示有效客户
4. **权限控制：** 状态切换操作需要专门权限
5. **业务验证：** 停用前检查是否存在关联的未完成业务

**接口设计：**
```
POST /api/Customer/ToggleActiveStatus
参数：{customerId, isActive, token}
```

**预估工期：** 1.5天

### 2.3 客户选择器查询优化 🆕

#### 2.3.1 增强查询接口
**业务需求：** 当前客户选择下拉搜索框信息太少，需要支持更丰富的弹出式选择器

**技术要求：**
1. **扩展返回字段：** 名称、简称、财务编码、是否超期、是否超额
2. **多维度搜索：** 支持按名称、简称、财务编码进行模糊搜索
3. **业务场景过滤：** 根据业务场景自动过滤客户类型
   - 选"委托单位"时只显示客户
   - 选"承运人"时只显示航空公司/船公司
4. **分页支持：** 支持大量客户数据的分页查询
5. **排序功能：** 支持按名称、编码等字段排序

**接口优化：**
```
GET /api/Customer/GetCustomersForSelector
参数：{searchText, customerType, pageIndex, pageSize, sortField, token}
返回：PagedResult<CustomerSelectorDto>
```

**预估工期：** 1天

## 3. 📊 任务优先级排序

| 序号 | 任务 | 优先级 | 状态 | 预估工期 | 目标完成时间 |
|------|------|--------|------|----------|-------------|
| 3.1 | 费用过滤Bug修复（fee_id） | 🔴 最高 | 新增 | 0.5天 | 1月28日 |
| 3.2 | OA申请单公司字段添加 | 🔴 最高 | 新增 | 1天 | 1月29日 |
| 3.3 | 空运接口架构重复修正 | 🔴 高 | 新增 | 1天 | 1月30日 |
| 3.4 | 费用列表申请单详情接口 | 🟡 中 | 新增 | 1天 | 1月31日 |
| 3.5 | 客户资料有效性管理 | 🟡 中 | 新增 | 1.5天 | 2月3日 |
| 3.6 | 客户选择器查询优化 | 🟡 中 | 新增 | 1天 | 2月4日 |
| 3.7 | 数据导入导出权限验证 | 🟡 中 | 进行中 | 0.5天 | 2月4日 |

**总计剩余工期：** 6.5个工作日  
**目标完成时间：** 2025年2月4日  
**风险评估：** 低 - 主要是Bug修复和功能增强，无重大架构变更

## 4. ⏸️ 暂缓任务（会议决议）

### 4.1 结算单导出到金蝶功能 🆕

#### 4.1.1 复杂业务逻辑暂缓
**问题描述：** 日常办公模块下收付款单（其他费用结算单）导出到金蝶尚未完成

**暂缓原因：** 逻辑复杂，优先级相对较低，先完成核心阻塞项

**预留要求：** 前端弹窗需新增"日常收款/日常付款"选项以预留入口

**预估工期：** 3天（待后续规划）

## 5. 🔧 技术债务和注意事项

### 5.1 权限验证待完善
- **位置**：所有导入导出接口中的TODO注释
- **要求**：实现"搜索权限文件，如果有叶子权限符合要求就使用，如果没有就不进行权限验证"的逻辑
- **影响**：影响导入导出功能的安全性

### 5.2 导入导出功能技术债务 🆕
- **大文件处理**：缺少文件大小限制，可能导致内存溢出
- **事务管理**：导入失败时的数据一致性保证不足
- **性能优化**：大数据量导入时缺少分批处理机制
- **错误恢复**：缺少导入失败后的数据回滚机制

### 5.3 空运接口历史遗留问题 🆕
- **架构重复**：PlJobController.EaDoc.cs与PlAirborneController功能重复
- **维护困难**：相同功能在多处实现，增加维护成本
- **路由冲突**：可能导致HTTP路由冲突问题

### 5.4 财务日期字段特殊处理 🆕
- **计算字段**：AccountDate现在是NotMapped字段，无法在数据库查询中直接使用
- **联动逻辑**：进口业务财务日期=到港日期，出口业务财务日期=开航日期
- **约束检查**：到港/开航日期不能选择当前月份之前的日期

## 6. 📝 开发指导和注意事项

### 6.1 会议决议执行要点
1. **优先处理阻塞项**：费用过滤Bug和OA申请单字段是当前最高优先级
2. **架构统一**：空运接口重复问题需要统一解决，建议采用完全拆分方案
3. **功能增强**：客户资料和选择器优化是重要的用户体验改进
4. **权限验证**：所有新增功能都需要完善的权限验证机制
5. **前后端协作**：很多功能需要前后端配合，确保接口设计满足前端需求

### 6.2 技术实现建议
1. **Bug修复优先**：先解决费用过滤问题，确保基础功能正常
2. **分步实施**：OA申请单字段添加可以分为实体修改、API更新、验证逻辑三个阶段
3. **架构重构谨慎**：空运接口重复问题需要仔细规划，避免影响现有功能
4. **增量开发**：新功能采用增量开发方式，逐步完善
5. **测试验证**：每个功能完成后都需要充分测试，确保稳定性