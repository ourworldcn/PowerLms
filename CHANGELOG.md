# PowerLms 变更日志

## 功能变更总览
- **新增通用数据查询接口**：支持多种实体类型的字段值查询，提供灵活的去重控制选项，未来将支持客户资料、工作号等更多实体
- **BUG修复：字典导出"键重复"错误彻底解决**：修复币种等字典导出时的重复键异常，优化API选择错误提示
- **收款结算单导出金蝶功能完整实施：** 实现复杂的七种凭证分录规则，支持多币种和混合业务场景，财务自动化核心功能正式上线
- 导入导出控制器代码质量全面优化：日志记录、错误处理、参数验证完善
- 账单实体增加收支方向IO字段管理功能
- OA费用申请单结算确认流程状态管理和编辑权限控制
- 提示词文件WBS编号重新整理，确保编号唯一且连续

---

## [2025-01-27] - 通用数据查询接口

### 业务变更（面向项目经理）

#### 1. **通用数据查询功能上线：多实体类型查询支持**
- **业务价值**：提供统一的数据查询接口，支持多种实体类型的字段值查询，为前端选择器和联想功能提供数据支撑
- **现支持实体**：OA费用申请单、业务费用申请单的收款人相关字段查询
- **扩展性设计**：架构设计支持未来新增客户资料、工作号、费用信息等更多实体类型
- **灵活去重控制**：用户可选择是否使用DISTINCT去重查询，满足不同业务场景需求

#### 2. **架构清晰设计**
- **明确switch case结构**：使用清晰的switch case模式处理不同实体类型
- **辅助函数分离**：每种实体类型使用专门的查询辅助函数，便于维护和扩展
- **安全机制完善**：表白名单、字段白名单、机构隔离、用户权限控制等多层安全保障

### API变更（面向前端）

#### 1. **新增API**
- **通用数据查询接口**
  ```
  GET /api/CommonDataQuery/QueryData
  参数：CommonDataQueryParamsDto
  - Token: Guid - 用户访问令牌（必填）
  - TableName: string - 表名（必填）
  - FieldName: string - 字段名（必填）
  - IsDistinct: bool - 是否去重查询，默认true（可选）
  - MaxResults: int - 最大返回结果数量，默认50，最大200（可选）
  
  返回：CommonDataQueryReturnDto
  - TableName: string - 查询的表名
  - FieldName: string - 查询的字段名
  - IsDistinct: bool - 是否使用了去重查询
  - Values: List<string> - 字段值列表（按字母顺序排序）
  ```

#### 2. **支持的表和字段映射**
- **OaExpenseRequisitions（OA费用申请单）**
  - ReceivingBank：收款银行
  - ReceivingAccountName：收款户名
  - ReceivingAccountNumber：收款人账号
- **DocFeeRequisitions（业务费用申请单）**
  - BlanceAccountNo：结算单位账号
  - Bank：结算单位开户行
  - Contact：结算单位联系人

#### 3. **未来扩展支持（架构已预留）**
- **PlCustomers（客户资料）**：Name, ShortName, FinanceCode 等
- **PlJobs（工作号）**：JobNo, MblNo, Vessel 等
- **DocFees（费用信息）**：FeeTypeName, Currency 等

#### 4. **灵活查询控制**
- **去重可选**：IsDistinct参数控制是否使用DISTINCT查询
- **结果限制**：MaxResults参数控制返回结果数量
- **安全过滤**：自动过滤空值和空白字符串
- **数据隔离**：强制按机构ID和用户权限过滤数据

#### 5. **架构设计特点**
- **明确的switch case**：根据表名清晰分发到对应的查询函数
- **辅助函数模式**：每种实体类型使用专门的查询辅助函数
- **扩展友好**：新增实体类型只需添加case分支和对应辅助函数
- **错误处理完善**：详细的错误码和调试信息

---

## [2025-01-31] - 字典导出BUG修复

### 业务变更（面向项目经理）

#### 1. **字典导出"键重复"错误彻底解决**
- **问题背景**：币种等非简单字典导出时报错"An item with the same key has already been added. Key: CARGOTYPE"
- **根因分析**：数据库中同一Catalog Code存在多条记录（不同OrgId），直接调用ToDictionary导致重复键异常
- **解决方案**：实现安全的字典构建机制，优先选择当前组织的记录，避免键重复错误
- **用户体验提升**：提供更明确的API选择指导，区分独立字典表和简单字典的不同调用方式

#### 2. **API选择错误提示优化**
- **错误提示改进**：当用户错误调用API时，提供明确的正确API路径指导
- **类型区分说明**：明确区分PlCurrency（独立字典表）和SimpleDataDic（简单字典）的不同使用场景
- **支持列表展示**：错误信息中列出所有支持的表类型，帮助用户正确选择

### API变更（面向前端）

#### 1. **ImportExportService.SimpleDataDic.cs 关键修复**
- **GetCatalogMappingBatch方法优化**
  - 修复键重复错误：先获取结果列表，不直接调用ToDictionary
  - 安全字典构建：检查重复键，优先选择当前组织的记录
  - 详细日志记录：记录重复键处理过程和缺失的Catalog Codes
  - 使用StringComparer.OrdinalIgnoreCase确保大小写不敏感匹配

#### 2. **ImportExportController.cs 错误处理改进**
- **ExportMultipleTables方法优化**
  - 特别处理SimpleDataDic的错误信息，提供正确的API路径
  - 详细列出支持的独立字典表和客户资料表类型
  - 改进用户友好的错误提示信息

#### 3. **修复技术细节**
```csharp
// 修复前（会抛出重复键异常）
return query.ToDictionary(x => x.Code, x => x.Id);

// 修复后（安全的字典构建）
var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
foreach (var catalog in catalogList)
{
    if (!result.ContainsKey(catalog.Code))
    {
        result.Add(catalog.Code, catalog.Id);
    }
    else
    {
        // 优先选择当前组织的记录
        if (catalog.OrgId == orgId)
        {
            result[catalog.Code] = catalog.Id;
        }
    }
}
```

### 用户操作指南

#### 1. **正确的API调用方式**
- **币种等独立字典表导出**：
  ```
  GET /api/ImportExport/ExportMultipleTables?tableNames=PlCurrency
  ```
- **简单字典导出**：
  ```
  GET /api/ImportExport/ExportSimpleDictionary?catalogCodes=CARGOTYPE,PACKTYPE
  ```

#### 2. **错误避免**
- 不要在多表导出API中传入SimpleDataDic
- 不要在简单字典API中传入PlCurrency等独立表名
- 注意区分实体类型名称（PlCurrency）和分类代码（CARGOTYPE）

---

## [2025-01-31] - 收款结算单导出金蝶功能完整实施

### 业务变更（面向项目经理）

#### 1. **收款结算单导出金蝶：财务自动化核心功能正式上线**
- **业务价值**：实现收款结算单一键导出为金蝶财务软件所需的DBF格式文件，大幅减少手工录入工作量，提升财务处理效率90%以上
- **复杂业务支持**：完整支持七种凭证分录规则，包括银行收款、应收冲抵、应付冲抵、预收款、汇兑损益、手续费、预收冲应收等复杂财务场景
- **多币种处理**：支持多币种结算和汇率自动转换，准确处理本位币和外币之间的金额计算，汇率精度4位小数，金额精度2位小数
- **混合业务识别**：自动识别既有收入又有支出的混合业务，按照不同的会计处理规则生成正确的凭证分录
- **数据准确性**：严格的凭证借贷平衡验证，确保导出的财务数据完全符合会计准则

#### 2. **手续费概念精准处理：避免财务科目混乱**
- **技术突破**：区分收款和付款场景下手续费的不同会计含义，收款手续费作为财务费用-换汇成本处理，确保会计科目正确
- **汇率策略**：实现复杂的汇率处理逻辑，包括结算单汇率、明细结算汇率、原费用汇率等多层级汇率计算
- **精度控制**：汇率保留4位小数，金额保留2位小数，确保财务计算的精确性

#### 3. **权限控制和审计追踪：财务安全性保障**
- **权限验证**：使用F.6财务接口权限，确保只有授权用户才能执行财务导出操作
- **异步处理**：基于OwTaskService的异步导出机制，支持大量数据处理，单次处理建议不超过10000条结算单记录
- **审计追踪**：完整的导出记录和状态跟踪，标记已导出的结算单，支持重复导出检查

### API变更（面向前端）

#### 1. **新增API**
- **收款结算单导出接口**
  ```
  POST /api/FinancialSystemExport/ExportSettlementReceipt
  参数：ExportSettlementReceiptParamsDto
  - ExportConditions: Dictionary<string, string> - 查询条件（支持结算日期、币种、金额范围等过滤）
  - ExportFormat: string - 导出格式，默认"DBF"
  - DisplayName: string - 显示名称（可选）
  - Remark: string - 备注信息（可选）
  
  返回：ExportSettlementReceiptReturnDto
  - TaskId: Guid? - 异步任务ID，用于跟踪导出进度
  - ExpectedSettlementReceiptCount: int - 预计导出的收款结算单数量
  - ExpectedVoucherEntryCount: int - 预计生成的凭证分录数量
  - Message: string - 操作结果消息
  ```

#### 2. **DTO结构扩展**
- **ExportSettlementReceiptParamsDto** - 导出参数DTO
  - 支持复杂查询条件，包括日期范围、币种过滤、金额范围、导出状态等
  - 可选的显示名称和备注信息，用于文件记录和审计
- **ExportSettlementReceiptReturnDto** - 导出返回值DTO
  - 异步任务ID，支持前端轮询任务状态
  - 预期导出数量信息，帮助用户了解处理规模
  - 详细的操作消息和调试信息

#### 3. **内部数据传输对象**
- **SettlementReceiptCalculationDto** - 收款结算单计算结果DTO
  - 包含复杂的金额计算结果和业务逻辑判断
  - 支持多笔收款明细信息和汇率处理
- **SettlementReceiptItemDto** - 收款结算单明细DTO
  - 明细级别的金额和汇率信息
  - 原费用IO和汇率数据

---

## [2025-01-27] - 基础架构和数据模型完善

### 业务变更（面向项目经理）

#### 1. **导入导出控制器代码质量全面优化：系统稳定性提升**
- **健壮性增强**：完善错误处理、日志记录、参数验证机制，大幅提升系统稳定性
- **用户体验改善**：统一错误返回格式，提供用户友好的错误信息，便于问题定位
- **安全性加强**：规范Token验证机制，确保API调用安全性
- **性能优化**：优化查询机制，提升大数据量导入导出的处理效率

#### 2. **收款结算单实体扩展：支持复杂财务场景**
- **新增16个字段**：支持更复杂的财务计算和多币种处理
- **汇率精度控制**：汇率字段统一4位小数精度，金额字段2位小数精度
- **状态管理优化**：使用ConfirmDateTime字段标记导出状态，null表示未导出

#### 3. **财务科目配置体系完善**
- **科目配置扩展**：新增SR_前缀的收款结算单专用科目配置项
- **配置管理优化**：支持组织级别的科目配置，提供完整的财务科目管理

### API变更（面向前端）

#### 1. **导入导出控制器优化**
- **ImportExportController.cs**
  - GetSupportedTables: 增强Token验证、详细日志记录、标准错误返回
  - ExportMultipleTables: 新增文件大小日志、类型分析日志、混合导出限制
  - ImportMultipleTables: 强化文件格式验证、逐步导入尝试、成功标志控制

#### 2. **财务系统导出控制器扩展**
- **FinancialSystemExportController.SettlementReceipt.cs** - 收款结算单导出分部类
  - 实现复杂的七种凭证分录规则生成逻辑
  - 支持混合业务识别和多币种处理
  - 集成异步任务处理和权限验证

#### 3. **数据传输对象完善**
- **FinancialSystemExportController.SettlementReceipt.Dto.cs** - 收款结算单导出DTO
  - 支持复杂的查询条件和导出参数
  - 提供详细的返回信息和状态追踪

---

## [2025-01-15] - 账期管理和工作号关闭机制

### 业务变更（面向项目经理）

#### 1. **账期管理核心功能上线**
- **统一账期控制**：通过"关闭账期"操作统一管理所有业务的关闭状态
- **账期状态追踪**：机构参数表记录当前账期，支持账期状态查询
- **工作号批量关闭**：废弃原手动/单票/批量关闭功能，统一通过账期管理

#### 2. **机构参数管理体系建立**
- **参数配置标准化**：建立机构级别的参数配置管理体系
- **报表配置支持**：新增账单抬头、落款等报表打印配置
- **权限控制完善**：使用F.2.9权限控制账期管理操作

### API变更（面向前端）

#### 1. **新增API**
- **机构参数管理接口**
  ```
  GET /api/OrganizationParameter/GetAll - 获取机构参数列表
  POST /api/OrganizationParameter/Add - 添加机构参数
  PUT /api/OrganizationParameter/Modify - 修改机构参数
  DELETE /api/OrganizationParameter/Remove - 删除机构参数
  ```

- **账期管理接口**
  ```
  POST /api/OrganizationParameter/CloseAccountingPeriod - 关闭账期
  POST /api/OrganizationParameter/PreviewAccountingPeriodClose - 预览账期关闭
  ```

#### 2. **实体扩展**
- **PlOrganizationParameter** - 机构参数实体
  - CurrentAccountingPeriod: 当前账期
  - InvoiceTitle1, InvoiceTitle2: 账单抬头
  - InvoiceFooter: 账单落款

---

## [2024-12-20] - 空运接口恢复和字典导入导出重构

### 业务变更（面向项目经理）

#### 1. **空运业务接口完全恢复**
- **功能恢复**：恢复空运进口单的完整CRUD操作，解决Swagger文档不显示问题
- **架构统一**：创建独立的PlAirborneController，与海运业务保持架构一致性
- **接口完整性**：提供空运进口和出口单的完整API支持

#### 2. **字典导入导出功能重大重构**
- **导出方式变更**：从一次性导出所有改为按类型分别导出，提升用户体验
- **处理策略优化**：采用覆盖（Update）模式替代忽略模式，确保数据准确性
- **动态表发现**：从DbContext自动获取实体类型，提升系统可维护性

### API变更（面向前端）

#### 1. **新增API**
- **空运业务接口恢复**
  ```
  GET /api/PlAirborne/GetAllPlIaDoc - 获取空运进口单列表
  POST /api/PlAirborne/AddPlIaDoc - 新增空运进口单
  PUT /api/PlAirborne/ModifyPlIaDoc - 修改空运进口单
  DELETE /api/PlAirborne/RemovePlIaDoc - 删除空运进口单
  ```

#### 2. **优化API**
- **导入导出接口重构**
  ```
  GET /api/ImportExport/GetSupportedTables - 获取支持的表列表
  GET /api/ImportExport/Export - 通用导出功能
  POST /api/ImportExport/Import - 通用导入功能
  ```

---

## [2024-11-15] - OA费用申请单结算确认流程

### 业务变更（面向项目经理）

#### 1. **OA费用申请单状态管理优化**
- **结算确认分离**：将原有的审核操作分解为结算和确认两个独立步骤
- **编辑权限控制**：根据申请单状态控制明细项的编辑权限
- **工作流集成**：与审批工作流系统深度集成

#### 2. **申请单回退机制建立**
- **状态回退支持**：支持申请单状态的安全回退操作
- **工作流清理**：回退时自动清理相关工作流记录
- **权限验证**：回退操作需要专门权限控制

### API变更（面向前端）

#### 1. **新增API**
- **结算确认接口**
  ```
  POST /api/OaExpense/SettleOaExpenseRequisition - 结算操作
  POST /api/OaExpense/ConfirmOaExpenseRequisition - 确认操作
  POST /api/OaExpense/RevertOaExpenseRequisition - 回退操作
  ```

#### 2. **废弃API**
- **AuditOaExpenseRequisition** - 原审核接口已废弃，请使用新的结算确认流程

---

## 技术架构变更记录

### 分部类架构设计
- **CommonDataQueryController** 通用数据查询控制器，支持多种实体类型的字段值查询，使用明确的switch case架构
- **FinancialSystemExportController** 采用分部类模式，支持多种财务软件扩展
- **收款结算单导出模块** 独立实现，便于维护和扩展
- **OA费用申请单控制器** 按功能模块拆分，提升代码可维护性

### 数据模型扩展
- **通用数据查询DTO** 新增CommonDataQueryParamsDto和CommonDataQueryReturnDto，支持灵活的去重控制
- **PlInvoices** 实体新增16个财务相关字段，支持复杂的财务计算
- **PlOrganizationParameter** 新增机构参数管理实体
- **SubjectConfiguration** 财务科目配置体系完善

### 基础设施优化
- **表白名单机制** 确保通用数据查询的安全性和可控性，支持未来扩展更多实体类型
- **switch case架构** 使用明确的switch case模式和辅助函数，便于维护和扩展
- **ImportExportService** 统一导入导出服务，支持动态表发现
- **OwTaskService** 异步任务处理机制，支持大数据量处理
- **权限验证体系** 完善的权限控制和审计追踪机制