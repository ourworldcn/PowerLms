# PowerLms 变更日志

## 功能变更总览

**最近完成的重要功能：**
- **Bug修复**：业务结算单（DocBill）OrgId隔离问题，确保不同公司切换时数据正确隔离
- **Bug修复**：申请单移除费用后已申请金额未正确恢复问题
- **基础数据导入导出扩展**：新增对JobNumberRule、OtherNumberRule、SubjectConfiguration、DailyFeesType的完整导入导出支持
- **商户实体结构优化**：地址属性展平重构，提升实体访问性能
- **商户查询功能增强**：支持通用查询条件，与系统其他查询接口保持一致
- **性能优化**：申请单明细已结算金额改为直接使用实体字段TotalSettledAmount，大幅提升查询性能
- **收付款结算单导出金蝶**：实现完整的收款和付款结算单导出金蝶功能，支持复杂的凭证分录规则
- **通用数据查询接口**：支持多种实体类型的字段值查询，为前端选择器提供数据支撑
- **其他Bug修复**：日常费用种类重复记录问题、字典导出"键重复"错误、OtherNumberRule导入导出失败问题

---

## [2025-02-19] - 关键Bug修复

### Bug修复记录

#### 业务结算单（DocBill）未按商户(OrgID)隔离 ⭐
- **问题描述**：
  1. 切换不同公司登录后，在业务结算单（DocBill）选择界面看到的数据完全一样
  2. 用户A（公司甲）能看到用户B（公司乙）的业务结算单
  3. 跨公司操作账单时没有权限验证，存在数据安全风险

- **根本原因**：
  - `DocBill`实体没有直接的`OrgId`字段，无法直接按机构过滤
  - `GetAllDocBill`查询没有通过关联链条进行OrgId过滤
  - `AddDocBill`、`ModifyDocBill`、`RemoveDocBill`缺少OrgId一致性验证
  - `GetDocBillsByJobIds`批量查询也缺少OrgId过滤

- **数据关联链条**：
  ```
  DocBill (业务结算单)
    └─ DocFee (费用) [通过 BillId 关联]
        └─ PlJob (工作号) [通过 JobId 关联]
            └─ OrgId (机构ID) ✅ 存在于PlJob
  ```

- **解决方案**：通过LINQ关联查询实现OrgId间接隔离
  
  1. **GetAllDocBill查询接口修复**：
     ```csharp
     // 修复前（无隔离）
     var coll = _DbContext.DocBills.OrderBy(...).AsNoTracking();
     
     // 修复后（增加关联查询过滤）
     var query = from bill in _DbContext.DocBills
                 join fee in _DbContext.DocFees on bill.Id equals fee.BillId
                 join job in _DbContext.PlJobs on fee.JobId equals job.Id
                 where job.OrgId == context.User.OrgId
                 select bill;
     var coll = query.Distinct().OrderBy(...).AsNoTracking();
     ```
  
  2. **AddDocBill增加OrgId验证**：
     - 验证所有费用关联的工作号属于当前用户的机构
     - 防止跨机构创建账单
     - 增加审计日志记录机构ID
  
  3. **ModifyDocBill增加权限验证**：
     - 验证账单属于当前用户的机构
     - 验证新关联的费用属于当前用户的机构
     - 防止跨机构修改账单
  
  4. **RemoveDocBill增加权限验证**：
     - 验证账单属于当前用户的机构
     - 防止删除其他机构的账单
     - 增加审计日志记录
  
  5. **GetDocBillsByJobIds增加过滤**：
     - 验证所有工作号属于当前用户的机构
     - 查询时增加OrgId过滤条件

- **技术细节**：
  - ✅ 使用`Distinct()`去重（一个账单可能关联多个费用）
  - ✅ 关联查询有索引支持，性能无问题
  - ✅ 所有操作记录审计日志，包含机构ID
  - ✅ 符合现有业务规则（账单必须关联费用）

- **影响范围**：
  - 修复文件：`PowerLmsWebApi\Controllers\Business\Common\PlJobController.DocBill.cs`
  - 影响接口：GetAllDocBill、AddDocBill、ModifyDocBill、RemoveDocBill、GetDocBillsByJobIds
  - 涉及实体：DocBill、DocFee、PlJob

- **数据风险处理**：
  - ⚠️ **孤立账单**：如果数据库中存在没有关联费用的历史账单，这些账单会被过滤掉
  - **检测SQL**：`SELECT * FROM DocBills WHERE Id NOT IN (SELECT DISTINCT BillId FROM DocFees WHERE BillId IS NOT NULL)`
  - **建议**：部署前运行检测SQL，确认是否存在孤立账单并进行数据清理

- **建议后续操作**：
  - 部署前运行孤立账单检测SQL
  - 如发现孤立账单，决策处理方式（删除/修复/保留）
  - 进行完整测试验证不同公司切换时的数据隔离效果

#### 申请单移除费用后已申请金额未正确恢复 ⭐
- **问题描述**：
  1. 将费用添加到申请单后，费用的`TotalRequestedAmount`字段正确增加
  2. 从申请单中移除该费用后，`TotalRequestedAmount`字段未减少
  3. 导致再次尝试添加时，系统认为费用已全部申请，无法重新添加

- **根本原因**：
  - 触发器`FeeTotalTriggerHandler`在计算`TotalRequestedAmount`时，查询结果包含了LocalCache中标记为Deleted的实体
  - EF Core在Saving阶段，被删除的明细仍在ChangeTracker中，导致计算时包含了即将删除的金额
  - `GetRequisitionItems()`方法返回的查询会合并数据库结果和LocalCache，包括Deleted状态的实体

- **解决方案**：
  - 修改`PowerLmsServer\Triggers\DocBill.Triggers.cs`中的`FeeTotalTriggerHandler.Saving`方法
  - 先执行`ToArray()`物化查询（执行SQL并加载到内存）
  - 再在内存中使用`Where()`过滤掉`EntityState.Deleted`状态的实体
  - 避免EF Core尝试将`Entry().State`翻译成SQL（会导致运行时错误）

- **技术细节**：
  ```csharp
  // 修复前（有bug）
  var rItems = fee.GetRequisitionItems(dbContext).ToArray();
  fee.TotalRequestedAmount = rItems.Sum(c => c.Amount);  // 包含了Deleted的明细
  
  // 修复后（正确）
  var allItems = fee.GetRequisitionItems(dbContext).ToArray();  // 先物化
  var rItems = allItems.Where(item => dbContext.Entry(item).State != EntityState.Deleted).ToArray();  // 内存过滤
  fee.TotalRequestedAmount = rItems.Sum(c => c.Amount);  // 正确排除Deleted明细
  ```

- **影响范围**：
  - 修复文件：`PowerLmsServer\Triggers\DocBill.Triggers.cs`
  - 影响功能：费用申请单明细的添加、修改、删除操作
  - 涉及字段：`DocFee.TotalRequestedAmount`、`DocFee.TotalSettledAmount`

- **建议后续操作**：
  - 执行数据修复脚本，修复历史不一致数据
  - 进行完整测试验证（添加/删除/修改明细）

---

## [2025-01-31] - 性能优化与功能增强

### 业务变更（面向项目经理）

#### 1. **申请单明细已结算金额性能优化**
- **性能提升**：移除动态计算已结算金额的逻辑，直接使用实体字段TotalSettledAmount，大幅提升查询性能
- **代码简化**：消除复杂的关联查询和聚合计算，简化业务逻辑，降低维护成本
- **架构优化**：采用冗余字段避免重复计算的性能优化策略，符合企业级应用的最佳实践

#### 2. **商户实体结构优化**
- **结构简化**：将PlMerchant实体中的复杂地址类型（PlSimpleOwnedAddress）展平为三个独立属性（Address_Tel、Address_Fax、Address_FullAddress）
- **性能提升**：简化实体属性访问，消除复杂类型的序列化开销，提升查询和绑定性能
- **兼容性保障**：数据库字段映射保持不变，确保现有数据完整性

#### 3. **商户查询功能增强**
- **查询灵活性提升**：商户列表查询现已支持通用查询条件，可按任意字段进行精确过滤
- **统一查询体验**：查询接口与系统其他模块保持一致
- **支持复杂条件**：支持区间查询、模糊匹配、null值约束等多种查询模式

#### 4. **基础数据导入导出功能扩展**
- **新增支持表类型**：
  - JobNumberRule（业务编码规则）
  - OtherNumberRule（其他编码规则）
  - SubjectConfiguration（财务科目配置）
  - DailyFeesType（日常费用种类）
- **数据模板提供**：即使表无数据也会导出表头模板，便于客户填写标准化数据

### API变更（面向前端）

#### 1. **申请单明细查询优化**
- **GetDocFeeRequisitionItem接口**：
  ```csharp
  // 优化后：直接使用实体字段计算余额
  resultItem.Remainder = item.Amount - item.TotalSettledAmount;
  ```
- **移除方法**：DocFeeRequisitionManager.GetInvoicedAmounts（动态计算方法已移除）
- **性能改进**：查询次数减少，响应时间预期提升50%以上

#### 2. **商户实体属性变更**
- **新属性结构**：
  ```csharp
  public string Address_Tel { get; set; }
  public string Address_Fax { get; set; }
  public string Address_FullAddress { get; set; }
  // 访问方式：merchant.Address_Tel（替代原来的merchant.Address.Tel）
  ```

#### 3. **商户查询接口增强**
- **GetAllMerchant接口**：
  ```
  GET /api/Merchant/GetAllMerchant?conditional[Name_Name]=xxx&conditional[StatusCode]=0
  
  支持的查询条件格式：
  - 字符串模糊查询：conditional[Name_Name]=商户名称
  - 精确匹配：conditional[StatusCode]=0
  - 区间查询：conditional[CreateDateTime]=2024-1-1,2024-12-31
  - Null约束：conditional[Description]=null
  ```

#### 4. **基础数据导入导出接口**
- **GetSupportedTables接口**：返回新增的四个表类型
- **ExportMultipleTables接口**：支持新增的TableNames参数值
- **ImportMultipleTables接口**：支持新增的Sheet名称，自动处理Id和OrgId字段

---

## [2025-01-31] - 金蝶导出功能完整实施

### 业务变更（面向项目经理）

#### 1. **付款结算单导出金蝶功能**
- **业务价值**：实现付款结算单一键导出为金蝶财务软件所需的DBF格式文件
- **复杂业务支持**：完整支持六种凭证分录规则（银行付款、应付冲销、应收增加、汇兑损益、手续费支出、手续费银行扣款）
- **多笔付款优先处理**：智能识别多笔付款记录，优先使用多笔付款明细生成分录
- **手续费双分录自平衡**：付款手续费生成配对的借贷分录，确保凭证自动平衡

#### 2. **收款结算单导出金蝶功能**
- **业务价值**：实现收款结算单一键导出为金蝶财务软件所需的DBF格式文件
- **复杂业务支持**：完整支持七种凭证分录规则（银行收款、应收冲抵、应付冲抵、预收款、汇兑损益、手续费、预收冲应收）
- **多币种处理**：支持多币种结算和汇率自动转换

### API变更（面向前端）

#### 1. **付款结算单导出接口**
```
POST /api/FinancialSystemExport/ExportSettlementPayment
参数：ExportSettlementPaymentParamsDto
- ExportConditions: 查询条件（支持结算日期、币种、金额范围等过滤）
- ExportFormat: 导出格式，默认"DBF"
- DisplayName: 显示名称（可选）
- Remark: 备注信息（可选）

返回：ExportSettlementPaymentReturnDto
- TaskId: 异步任务ID
- ExpectedSettlementPaymentCount: 预计导出的付款结算单数量
- ExpectedVoucherEntryCount: 预计生成的凭证分录数量
```

#### 2. **收款结算单导出接口**
```
POST /api/FinancialSystemExport/ExportSettlementReceipt
参数：ExportSettlementReceiptParamsDto
返回：ExportSettlementReceiptReturnDto
```

---

## [2025-01-27] - 通用数据查询接口

### 业务变更（面向项目经理）

#### 1. **通用数据查询功能上线**
- **业务价值**：提供统一的数据查询接口，支持多种实体类型的字段值查询
- **现支持实体**：OA费用申请单、业务费用申请单的收款人相关字段查询
- **扩展性设计**：架构设计支持未来新增客户资料、工作号、费用信息等更多实体类型

### API变更（面向前端）

#### 1. **通用数据查询接口**
```
GET /api/CommonDataQuery/QueryData
参数：
- TableName: 表名（必填）
- FieldName: 字段名（必填）
- IsDistinct: 是否去重查询，默认true（可选）
- MaxResults: 最大返回结果数量，默认50，最大200（可选）

返回：字段值列表（按字母顺序排序）
```

---

## Bug修复记录

### [2025-02-19] 业务结算单（DocBill）未按商户(OrgID)隔离Bug修复
- **问题描述**：切换不同公司登录后，在业务结算单选择界面看到的数据完全一样，没有按公司隔离
- **解决方案**：通过LINQ关联查询（DocBill → DocFee → PlJob → OrgId）实现数据隔离，所有增删改查接口增加OrgId验证

### [2025-02-19] 申请单移除费用后已申请金额未正确恢复Bug修复
- **问题描述**：申请单移除费用后，费用的`TotalRequestedAmount`字段未正确减少
- **解决方案**：修改触发器`FeeTotalTriggerHandler`，在计算`TotalRequestedAmount`时排除已删除的费用明细

### [2025-01-31] 日常费用种类重复记录Bug修复
- **问题描述**：新增"日常费用种类"时，勾选"同步到子机构"选项后，系统会在当前机构创建两条重复记录
- **解决方案**：在遍历公司型组织机构时，排除当前用户的组织机构，避免重复创建

### [2025-01-31] 字典导出"键重复"错误修复
- **问题描述**：币种等非简单字典导出时报错"An item with the same key has already been added"
- **解决方案**：实现安全的字典构建机制，优先选择当前组织的记录，避免键重复错误

### [2025-01-31] OtherNumberRule导入导出失败修复
- **问题描述**：四个基础数据实体（JobNumberRule、OtherNumberRule、SubjectConfiguration、DailyFeesType）无法通过导入导出功能识别
- **解决方案**：在代码中硬编码支持的基础数据类型列表，确保功能立即可用

---

## 技术架构变更记录

### 数据隔离安全增强
- **业务结算单OrgId隔离**：通过LINQ关联查询实现DocBill按OrgId过滤，确保多租户数据安全

### 实体结构优化
- **商户地址属性展平**：将PlMerchant中的复杂地址类型展平为独立属性，提升实体访问性能

### 性能优化
- **申请单明细已结算金额直接使用实体字段**：消除动态计算逻辑，提升查询性能

### 查询接口统一
- **商户查询功能增强**：为GetAllMerchant接口添加通用查询支持，与系统其他查询接口保持一致

### 分部类架构设计
- **FinancialSystemExportController**：采用分部类模式，支持收款和付款结算单导出功能
- **CommonDataQueryController**：通用数据查询控制器，支持多种实体类型的字段值查询

### 数据模型扩展
- **DocFeeRequisitionItem.TotalSettledAmount**：已结算金额字段直接使用，不再动态计算
- **PlInvoices**：实体新增16个财务相关字段，支持复杂的财务计算

### 基础设施优化
- **ImportExportService**：统一导入导出服务，修复字典导出键重复错误
- **OwTaskService**：异步任务处理机制，支持大数据量处理
- **权限验证体系**：完善的权限控制和审计追踪机制