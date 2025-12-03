# PowerLmsWebApi API层索引

## 📋 控制器总览

| 控制器 | 文件路径 | 业务域 | 主要功能 |
|--------|----------|--------|----------|
| `PlControllerBase` | Controllers/PlControllerBase.cs | 基类 | 控制器基类,提供通用路由和权限方法 |
| `CustomerController` | Controllers/Customer/CustomerController.cs | 客户管理 | 客户资料CRUD、联系人、开票信息、提单、黑名单、装货地址 |
| `PlJobController` | Controllers/Business/Common/PlJobController.cs | 业务总表 | 工作号CRUD、状态管理、审核、复制、账期关闭 |
| `PlAirborneController` | Controllers/Business/AirFreight/PlAirborneController.cs | 空运业务 | 空运进口单、空运出口单CRUD |
| `PlSeaborneController` | Controllers/Business/SeaFreight/PlSeaborneController.cs | 海运业务 | 海运进口单、海运出口单、箱量CRUD |
| `FinancialController` | Controllers/Financial/FinancialController.cs | 财务管理 | 结算单、结算单明细、费用方案、费用方案明细 |
| `TaxController` | Controllers/Tax/TaxController.cs | 税务管理 | 机构渠道账号、发票信息、发票明细 |
| `AdminController` | Controllers/System/AdminController.cs | 系统管理 | 数据字典、港口、航线、国家、币种、汇率、费用种类、箱型、单位换算 |
| `AccountController` | Controllers/System/AccountController.cs | 账号管理 | 登录、用户CRUD、角色权限 |
| `OrganizationController` | Controllers/System/OrganizationController.cs | 组织架构 | 商户、机构、公司CRUD |
| `AuthorizationController` | Controllers/System/AuthorizationController.cs | 权限管理 | 权限分配、权限查询 |
| `WfController` | Controllers/System/WfController.cs | 工作流 | 审批流程、任务管理 |
| `FileController` | Controllers/System/FileController.cs | 文件管理 | 文件上传、下载、权限控制 |
| `CommonDataQueryController` | Controllers/System/CommonDataQueryController.cs | 通用查询 | 通用数据查询(支持OA费用申请单、业务费用申请单) |
| `PostController` | Controllers/Forum/PostController.cs | 论坛管理 | 论坛帖子CRUD |
| `ImportExportController` | Controllers/ImportExportController.cs | 导入导出 | Excel数据导入导出 |

## 🔑 关键特性

### 控制器基类 (`PlControllerBase`)
```csharp
路由: api/[controller]/[action]
继承: ControllerBase
特性:
- 提供GetOrgIds方法:根据账号获取可管理的机构ID列表
- 提供DotKeyDictionaryModelBinder:支持带点号的查询参数(如PlJob.jobNo)
```

### 权限验证模式
所有业务控制器都使用`AuthorizationManager.Demand()`进行权限验证:
```csharp
if (!_AuthorizationManager.Demand(out string err, "权限码")) 
    return StatusCode(403, err);
```

### 多租户隔离
所有数据查询都基于`context.User.OrgId`进行过滤:
```csharp
var coll = dbSet.Where(c => c.OrgId == context.User.OrgId);
```

## 📁 控制器分类索引

### 1. 客户管理类 (Customer/)
- **CustomerController**: 客户资料、联系人、开票信息、提单、黑名单、装货地址

### 2. 业务类 (Business/)
#### 2.1 通用业务
- **PlJobController**: 工作号管理(支持海运/空运/进出口)
  - 主要功能: GetAllPlJob, AddPlJob, ModifyPlJob, RemovePlJob, ChangeState, AuditJobAndDocFee, CopyJob
  - 特殊功能: CloseAccountingPeriod(账期关闭)

#### 2.2 空运业务 (AirFreight/)
- **PlAirborneController**: 空运进出口单管理
  - 空运出口: GetAllPlEaDoc, AddPlEaDoc, ModifyPlEaDoc, RemovePlEaDoc
  - 空运进口: GetAllPlIaDoc, AddPlIaDoc, ModifyPlIaDoc, RemovePlIaDoc

#### 2.3 海运业务 (SeaFreight/)
- **PlSeaborneController**: 海运进出口单及箱量管理
  - 海运出口: GetAllPlEsDoc, AddPlEsDoc, ModifyPlEsDoc, RemovePlEsDoc
  - 海运进口: GetAllPlIsDoc, AddPlIsDoc, ModifyPlIsDoc, RemovePlIsDoc
  - 箱量管理: GetAllContainerKindCount, SetContainerKindCount

### 3. 财务管理类 (Financial/)
- **FinancialController**: 结算单、费用方案
  - 结算单: GetAllPlInvoices, AddPlInvoice, ModifyPlInvoices, RemovePlInvoices, ConfirmPlInvoices
  - 结算单明细: GetAllPlInvoicesItem, AddPlInvoicesItem, ModifyPlInvoicesItem, RemovePlInvoicesItem
  - 费用方案: GetAllDocFeeTemplate, AddDocFeeTemplate, ModifyDocFeeTemplate, RemoveDocFeeTemplate
  - 费用方案明细: GetAllDocFeeTemplateItem, AddDocFeeTemplateItem, ModifyDocFeeTemplateItem

### 4. 税务管理类 (Tax/)
- **TaxController**: 发票、渠道账号
  - 机构渠道账号: GetAllOrgTaxChannelAccount, AddOrgTaxChannelAccount, ModifyOrgTaxChannelAccount
  - 发票信息: GetAllTaxInvoiceInfo, AddTaxInvoiceInfo, ModifyTaxInvoiceInfo, ChangeStateOfTaxInvoiceInfo
  - 发票明细: GetAllTaxInvoiceInfoItem, AddTaxInvoiceInfoItem, SetTaxInvoiceInfoItem

### 5. 系统管理类 (System/)
- **AdminController**: 基础数据管理
  - 国家: GetAllPlCountry, AddPlCountry, ModifyPlCountry, RemovePlCountry, RestorePlCountry
  - 币种: GetAllPlCurrency, AddPlCurrency, ModifyPlCurrency, RemovePlCurrency, RestorePlCurrency
  - 汇率: GetAllPlExchangeRate, AddPlExchangeRate, ModifyPlExchangeRate, ImportPlExchangeRate
  - 港口: GetAllPlPort, AddPlPort, ModifyPlPort, RemovePlPort, RestorePlPort
  - 航线: GetAllPlCargoRoute, AddPlCargoRoute, ModifyPlCargoRoute, RemovePlCargoRoute
  - 费用种类: GetAllFeesType, AddFeesType, ModifyFeesType, RemoveFeesType, RestoreFeesType
  - 箱型: GetAllShippingContainersKind, AddShippingContainersKind, ModifyShippingContainersKind
  - 单位换算: GetAllUnitConversion, AddUnitConversion, ModifyUnitConversion

- **CommonDataQueryController**: 通用数据查询
  - GetData: 支持表白名单机制的通用查询(当前支持OaExpenseRequisitions、DocFeeRequisitions)

### 6. 论坛管理类 (Forum/)
- **PostController**: 论坛帖子
  - 功能: GetAllOwPost, AddOwPost, ModifyOwPost, RemoveOwPost, GetOwPostById
  - 特性: 支持商户隔离、自动作者信息填充

## 🛡️ 权限码索引

### 客户管理 (C.*)
- C.1.1: 新增客户
- C.1.2: 查询客户
- C.1.3: 修改客户
- C.1.4: 删除客户
- C.1.5: 业务负责人管理

### 空运出口 (D0.*)
- D0.1.1.1: 查询权限(分个人/部门/全部)
- D0.1.1.2: 新增工作号
- D0.1.1.3: 修改工作号
- D0.1.1.4: 删除工作号
- D0.6.6: 审核费用
- D0.6.10: 取消审核费用

### 空运进口 (D1.*)
- D1.1.1.1-4: 空运进口工作号CRUD
- D1.6.6: 审核费用
- D1.6.10: 取消审核费用

### 海运出口 (D2.*)
- D2.1.1.1-4: 海运出口工作号CRUD

### 海运进口 (D3.*)
- D3.1.1.1-4: 海运进口工作号CRUD

### 财务管理 (F.*)
- F.2: 查看所有工作号财务数据
- F.2.8: 审核工作号及费用
- F.2.9: 关闭账期
- F.3.1: 新增结算单
- F.3.2: 修改结算单
- F.3.3: 删除结算单
- F.3.4: 结算单确认
- F.6: 财务接口(导出金蝶等)

### 基础数据 (B.*)
- B.0: 数据字典
- B.3: 币种管理
- B.4: 汇率管理
- B.5: 国家管理
- B.6: 港口管理
- B.7: 航线管理
- B.8: 费用种类管理
- B.9: 箱型管理
- B.10: 单位换算管理

## 📝 常见DTO模式

### 查询DTO (GetAll*)
```csharp
GetAll*ParamsDto: 继承PagingParamsDtoBase
  - Token: Guid
  - StartIndex: int
  - Count: int
  - OrderFieldName: string
  - IsDesc: bool
  - conditional: Dictionary<string, string> // 通用查询条件

GetAll*ReturnDto: 继承PagingReturnDtoBase<T>
  - Result: List<T>
  - Total: int
```

### 新增DTO (Add*)
```csharp
Add*ParamsDto: 继承AddParamsDtoBase<T>
  - Token: Guid
  - Item: T // 要新增的实体

Add*ReturnDto: 继承AddReturnDtoBase
  - Id: Guid // 新增成功后的ID
```

### 修改DTO (Modify*)
```csharp
Modify*ParamsDto: 继承ModifyParamsDtoBase<T>
  - Token: Guid
  - Items: List<T> // 要修改的实体列表(通常只传1个)

Modify*ReturnDto: 继承ModifyReturnDtoBase
```

### 删除DTO (Remove*)
```csharp
Remove*ParamsDto: 继承RemoveParamsDtoBase
  - Token: Guid
  - Id: Guid

Remove*ReturnDto: 继承RemoveReturnDtoBase
```

## 🔄 通用查询条件说明

所有控制器的GetAll*方法都支持通用查询条件(conditional参数):
- **格式**: `Dictionary<string, string>`
- **用法**: 字段名=值
- **字符串类型**: 模糊查询(LIKE)
- **其他类型**: 精确匹配
- **区间查询**: 用逗号分隔 "2024-1-1,2024-1-2"
- **强制null**: 值写"null"
- **关联查询**: 支持 "实体名.字段名" 格式(如PlJob.jobNo)

示例:
```
conditional = {
  "CustomerId": "guid值",
  "CreateDateTime": "2024-01-01,2024-12-31",
  "PlJob.JobNo": "JOB001"
}
```

## ⚠️ 重要约束

1. **所有控制器方法都需要Token验证**
2. **所有业务数据都基于OrgId进行多租户隔离**
3. **控制器只做参数验证和权限检查,业务逻辑写在Manager层**
4. **删除操作都是慎用操作,部分有业务状态限制**
5. **审核后的数据通常不可修改/删除**
6. **账期关闭后的工作号不可修改**

---

**索引更新时间**: 2025-01-31
**适用版本**: PowerLms v1.0+
