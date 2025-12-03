# PowerLmsServer 业务层索引

## 📋 Manager总览

| Manager类 | 文件路径 | 功能域 | 主要功能 |
|-----------|----------|--------|----------|
| `AccountManager` | Managers/Auth/AccountManager.cs | 账号管理 | 用户登录、令牌管理、账号CRUD、缓存管理 |
| `AuthorizationManager` | Managers/Auth/AuthorizationManager.cs | 权限管理 | 权限验证、角色权限映射、多租户隔离 |
| `RoleManager` | Managers/Auth/RoleManager.cs | 角色管理 | 角色CRUD、角色权限关联、用户角色缓存 |
| `PermissionManager` | Managers/Auth/PermissionManager.cs | 权限字典 | 权限树管理、权限查询 |
| `OrgManager` | Managers/System/OrgManager.cs | 组织架构 | 商户/机构/公司树、组织缓存、数据隔离 |
| `EntityManager` | Managers/System/EntityManager.cs | 实体管理 | 通用CRUD、分页查询、实体修改 |
| `DataDicManager` | Managers/BaseData/DataDicManager.cs | 数据字典 | 简单字典、字典分类、参数配置 |
| `OwWfManager` | Managers/Workflow/OwWfManager.cs | 工作流 | 工作流创建、审批、状态流转 |
| `OwSqlAppLogger` | Managers/System/OwSqlAppLogger.cs | 应用日志 | 操作日志记录、日志查询 |
| `CaptchaManager` | Managers/System/CaptchaManager.cs | 验证码 | 验证码生成、验证、生命周期管理 |
| `JobManager` | Managers/Business/JobManager.cs | 业务管理 | 工作号CRUD、业务流程 |
| `CustomerManager` | Managers/Customer/CustomerManager.cs | 客户管理 | 客户资料CRUD、联系人管理 |
| `FinancialManager` | Managers/Financial/FinancialManager.cs | 财务管理 | 结算单、发票、费用核算 |
| `ExchangeRateManager` | Managers/Financial/ExchangeRateManager.cs | 汇率管理 | 汇率查询、汇率更新 |
| `OaExpenseManager` | Managers/OA/OaExpenseManager.cs | OA费用 | 日常费用申请、审批、核销 |
| `ImportExportService` | Services/ImportExportService.cs | 导入导出 | Excel数据导入导出、简单字典处理 |

## 🔑 关键特性

### 依赖注入模式
所有Manager使用`[OwAutoInjection]`特性自动注册:
```csharp
[OwAutoInjection(ServiceLifetime.Scoped)]  // 范围服务(推荐)
[OwAutoInjection(ServiceLifetime.Singleton)] // 单例服务
```

### 缓存管理模式
统一使用`IMemoryCache`+`CancellationTokenSource`机制:
```csharp
// 获取或加载数据
var data = _Cache.GetOrCreate(cacheKey, entry => {
    entry.EnablePriorityEvictionCallback(_Cache); // 启用优先级驱逐
    return LoadData();
});

// 失效缓存
var cts = _Cache.GetCancellationTokenSource(cacheKey);
if (cts != null) cts.Cancel();
```

### 多租户隔离模式
所有业务查询都基于`OrgId`过滤:
```csharp
var data = _DbContext.TableName
    .Where(c => c.OrgId == context.User.OrgId)
    .AsNoTracking();
```

## 📁 Manager分类索引

### 1. 认证与授权 (Auth/)
- **AccountManager**: 账号管理
  - 核心方法: LoadById, GetOrLoadByToken, CreateNew, UpdateToken, InvalidateUserCache
  - 缓存策略: 用户对象缓存(15分钟滑动过期)、Token到Id映射(ConcurrentDictionary)
  - 关键特性: 分布式锁(SingletonLocker)、TaskDispatcher批量保存

- **AuthorizationManager**: 权限管理
  - 核心方法: Demand(权限验证)、GetPermissions(获取用户权限)
  - 权限模型: 角色→权限、用户→角色→权限
  - 多租户: 基于商户/机构的权限隔离

- **RoleManager**: 角色管理
  - 核心方法: GetOrLoadRolesByMerchantId, GetOrLoadCurrentRolesByUser, InvalidateRoleCache
  - 缓存策略: 商户角色缓存(30分钟)、用户当前角色缓存(15分钟)
  - 依赖关系: 依赖OrgManager获取组织信息

- **PermissionManager**: 权限字典
  - 核心方法: LoadPermissionTree, GetPermissionById
  - 数据结构: PlPermission树形结构

### 2. 组织架构 (System/)
- **OrgManager**: 组织管理
  - 核心方法: GetOrLoadOrgCacheItem, GetMerchantIdByUserId, GetCompanyIdByOrgId
  - 数据结构: PlMerchant(商户)、PlOrganization(机构)、OrgCacheItem(缓存对象)
  - 缓存策略: 按商户ID缓存整个组织树(永久缓存+CTS失效)
  - 关键特性: 三层架构(商户→公司→机构)、批量加载优化

- **EntityManager**: 实体管理
  - 核心方法: GetAll(分页查询)、Modify(实体修改)、Remove(软删除)
  - 通用CRUD: 支持所有实现`GuidKeyObjectBase`的实体
  - 软删除: 实现`ISoftDelete`接口的实体支持软删除

### 3. 基础数据 (BaseData/)
- **DataDicManager**: 数据字典管理
  - 核心方法: GetSimpleDataDics, AddSimpleDataDic, GetDataDicCatalog
  - 数据结构: DataDicCatalog(分类)、SimpleDataDic(字典项)
  - 多租户: 支持全局字典和组织字典

- **SystemResourceManager**: 系统资源管理
  - 核心方法: GetSystemResources
  - 功能: 系统资源清单、版本管理

### 4. 工作流 (Workflow/)
- **OwWfManager**: 工作流管理
  - 核心方法: LoadWorkflowById, GetWfNodeItemByOpertorId, SetWorkflowState
  - 数据结构: OwWf(工作流实例)、OwWfTemplate(流程模板)、OwWfNode(流程节点)
  - 特性: 多级审批、状态流转、回调机制(IWorkflowCallback)

### 5. 业务管理 (Business/)
- **JobManager**: 业务管理
  - 核心方法: CreateJob, UpdateJob, ChangeJobState
  - 数据结构: PlJob(工作号)、DocFee(费用)
  - 业务类型: 空运出口(AE)、空运进口(AI)、海运出口(SE)、海运进口(SI)

- **BusinessLogicManager**: 业务逻辑管理
  - 核心方法: ValidateBusinessRule
  - 功能: 业务规则验证、业务流程控制

### 6. 财务管理 (Financial/)
- **FinancialManager**: 财务管理
  - 核心方法: CreateSettlement, ConfirmSettlement, CreateInvoice
  - 数据结构: Settlement(结算单)、Invoice(发票)、DocFee(费用)

- **ExchangeRateManager**: 汇率管理
  - 核心方法: GetCurrentOrgExchangeRate, ImportExchangeRate
  - 汇率策略: 按业务类型、有效时间查询汇率

- **DocFeeRequisitionManager**: 费用申请管理
  - 核心方法: CreateRequisition, ApproveRequisition
  - 数据结构: DocFeeRequisition(申请单)、DocFeeRequisitionItem(申请明细)

- **FinancialSystemExportManager**: 财务系统导出
  - 核心方法: ExportToKingdee(金蝶导出)
  - 格式: DBF凭证格式

### 7. OA管理 (OA/)
- **OaExpenseManager**: OA费用管理
  - 核心方法: CreateOaExpense, ApproveOaExpense
  - 数据结构: OaExpenseRequisition(费用申请单)

### 8. 客户管理 (Customer/)
- **CustomerManager**: 客户管理
  - 核心方法: CreateCustomer, UpdateCustomer, GetCustomerById
  - 数据结构: PlCustomer(客户)、PlCustomerContact(联系人)、PlTaxInfo(开票信息)

### 9. 系统服务 (System/)
- **OwSqlAppLogger**: 应用日志服务
  - 核心方法: LogGeneralInfo, WriteLogItem, Define
  - 数据结构: OwAppLogStore(日志源)、OwAppLogItemStore(日志项)
  - 特性: 批量写入(OwBatchDbWriter)、内存缓存优化

- **CaptchaManager**: 验证码管理
  - 核心方法: GetNew(生成验证码)、Verify(验证验证码)
  - 验证码类型: 数学运算题(加法)
  - 存储格式: JPEG图片+数据库记录

### 10. 集成服务 (Integration/)
- **NuoNuoInvoiceManager**: 诺诺发票管理
  - 核心方法: LoginNuoNuo, CreateInvoice, QueryInvoice
  - 功能: 电子发票开具、查询、作废

### 11. 导入导出服务 (Services/)
- **ImportExportService**: 导入导出服务
  - 核心方法: ImportDictionaries, ExportDictionaries, ImportSimpleDictionaries
  - 支持格式: Excel(NPOI)
  - 特性: 多Sheet处理、批量操作、错误隔离

## 🎯 核心设计模式

### 1. Manager模式
所有业务逻辑封装在Manager中:
```csharp
[OwAutoInjection(ServiceLifetime.Scoped)]
public class SomeManager
{
    private readonly PowerLmsUserDbContext _DbContext;
    private readonly IMemoryCache _Cache;
    
    // 业务逻辑方法
    public void DoSomething() { }
}
```

### 2. 缓存失效策略
使用CancellationTokenSource实现精确失效:
```csharp
// 注册缓存
entry.EnablePriorityEvictionCallback(_Cache);

// 失效缓存
var cts = _Cache.GetCancellationTokenSource(cacheKey);
if (cts != null) cts.Cancel();
```

### 3. DbContext工厂模式
使用`IDbContextFactory`创建独立DbContext:
```csharp
using var dbContext = _DbContextFactory.CreateDbContext();
var data = dbContext.Entities.AsNoTracking().ToList();
```

### 4. 批量写入模式
使用`OwBatchDbWriter`批量写入:
```csharp
var operation = new DbOperation
{
    OperationType = DbOperationType.Insert,
    Entity = entity
};
_BatchDbWriter.AddItem(operation);
```

## 📊 数据访问模式

### 查询优化
```csharp
// ✅ 推荐:使用AsNoTracking
var data = _DbContext.TableName.AsNoTracking().ToList();

// ✅ 推荐:使用using自动释放
using var db = _DbContextFactory.CreateDbContext();

// ❌ 避免:延迟加载
// 延迟加载已禁用,需要显式Include
```

### 多租户过滤
```csharp
// ✅ 所有业务查询必须包含OrgId过滤
var data = _DbContext.PlJobs
    .Where(c => c.OrgId == context.User.OrgId)
    .AsNoTracking();
```

### 软删除
```csharp
// ✅ 软删除查询需过滤IsDelete
var data = _DbContext.TableName
    .Where(c => !c.IsDelete)
    .AsNoTracking();
```

## 🔧 配置与初始化

### 服务注册
在`Program.cs`中自动注册:
```csharp
services.AutoRegister(assemblies);
services.AddAutoMapper(assemblies);
```

### 初始化服务
`InitializerService`负责:
- 数据库迁移(生产环境手动)
- 初始数据导入
- 系统资源初始化

## ⚠️ 重要约束

1. **Manager禁止直接返回DbContext附加的实体**
2. **所有业务方法必须检查多租户权限**
3. **缓存对象必须是只读的(AsNoTracking)**
4. **DbContext必须在范围内使用或使用using释放**
5. **禁止在缓存对象中存储DbContext引用**
6. **批量操作优先使用OwBatchDbWriter**

---

**索引更新时间**: 2025-01-31
**适用版本**: PowerLms v1.0+
