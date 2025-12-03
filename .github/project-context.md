# PowerLms 项目上下文索引
<!-- AI必读:本文件提供项目全局导航,避免过度扫描文件 -->
<!-- 维护方式:人工维护核心索引,AI辅助补充细节 -->

## 🗂️ 1. 核心文档优先级(按顺序读取)
1. **开发规范**:`.github/copilot-instructions.md`
2. **项目配置**:`.github/project-specific.md`
3. **架构索引**:本文件
4. **业务需求**:`会议纪要.md`
5. **当前任务**:`TODO.md`
6. **历史变更**:`CHANGELOG.md`
7. **部署说明**:`部署指南.txt`

---

## 📚 2. 分层索引文档(按需深入)

| 层级 | 索引文档 | 内容概要 | 何时使用 |
|------|---------|----------|----------|
| **API层** | `.github/indexes/PowerLmsWebApi.md` | 15+控制器、权限码、DTO模式 | 修改API接口、查询权限码 |
| **业务层** | `.github/indexes/PowerLmsServer.md` | 16+Manager类、缓存策略、设计模式 | 实现业务逻辑、缓存管理 |
| **数据层** | `.github/indexes/PowerLmsData.md` | 60+实体类、字段说明、迁移记录 | 修改实体、数据库设计 |
| **基础设施** | 本文件§3 | OwDbBase/OwBaseCore/OwExtensions简介 | 了解通用组件功能 |

---

## 🏗️ 3. 基础设施层(Bak/目录)

### 3.1 OwDbBase项目
**位置**: `../Bak/OwDbBase/`
**功能**: 数据访问基础设施
- **OwDataUnit**: Excel批量导入导出工具(基于NPOI)
- **OwNpoiUnit**: NPOI扩展方法(读写Excel)
- **OwContext**: 用户上下文(包含Account、Token)
- **GuidKeyObjectBase**: Guid主键实体基类
- **ISoftDelete**: 软删除接口
- **OwDbContext**: DbContext基类

> ⚠️ **已废弃**: `NpoiManager`(使用OwDataUnit+OwNpoiUnit替代)

### 3.2 OwBaseCore项目
**位置**: `../Bak/OwBaseCore/`
**功能**: 核心扩展与工具
- **OwCacheExtensions**: 统一缓存管理(IMemoryCache扩展)
- **OwHelper**: 通用工具类(时间、GUID、随机数)
- **OwStringUtils**: 字符串工具(密码生成、格式化)
- **OwBatchDbWriter**: 批量数据库写入器
- **TaskDispatcher**: 任务调度器
- **SingletonLocker**: 单例锁

### 3.3 OwExtensions项目
**位置**: `../Bak/OwExtensions/`
**功能**: NPOI扩展方法
- **ReadEntities**: 从Excel Sheet读取实体集合
- **WriteEntities**: 将实体集合写入Excel Sheet
- **CellValueExtensions**: 单元格值读写扩展

---

## 🏗️ 4. 架构地图(快速定位)

### 4.1 分层结构
```
PowerLmsWebApi(API层)
  ├─ Controllers/          # 路由与校验,禁止写业务逻辑
  └─ Program.cs            # 启动配置

PowerLmsServer(业务层)
  ├─ Managers/             # 核心业务逻辑
  │   ├─ Auth/             # 权限管理(AccountManager, AuthorizationManager, RoleManager)
  │   ├─ System/           # 系统管理(OrgManager, EntityManager, OwSqlAppLogger)
  │   ├─ Business/         # 业务管理(JobManager, BusinessLogicManager)
  │   ├─ Financial/        # 财务管理(FinancialManager, ExchangeRateManager)
  │   ├─ Customer/         # 客户管理(CustomerManager)
  │   ├─ OA/               # OA管理(OaExpenseManager)
  │   └─ BaseData/         # 基础数据(DataDicManager, SystemResourceManager)
  ├─ Services/             # 服务层(ImportExportService)
  └─ 基础设施依赖 → Bak/

PowerLmsData(数据层)
  ├─ 账号/                 # Account
  ├─ 权限/                 # PlRole, PlPermission
  ├─ 机构/                 # PlMerchant, PlOrganization
  ├─ 客户资料/             # PlCustomer, PlCustomerContact, PlTaxInfo
  ├─ 业务/                 # PlJob, PlEaDoc, PlIaDoc, DocFee, DocBill
  ├─ 财务/                 # PlInvoices, DocFeeRequisition, TaxInvoiceInfo
  ├─ 基础数据/             # PlCountry, PlPort, PlCurrency, SimpleDataDic
  ├─ 流程/                 # OwWorkflow, OwWfTemplate
  ├─ OA/                   # OaExpenseRequisition
  ├─ 应用日志/             # OwAppLogStore, OwAppLogItemStore
  └─ Migrations/           # 数据库迁移(150+文件)

Bak/(基础设施)
  ├─ OwDbBase/             # 数据访问基类、Excel工具
  ├─ OwBaseCore/           # 核心扩展、缓存管理
  └─ OwExtensions/         # NPOI扩展方法
```

### 4.2 关键基础设施(优先复用)
| 功能域 | 核心类 | 位置 | 用途 |
|--------|--------|------|------|
| 文件管理 | `OwFileService` | `PowerLmsServer/Managers/System/` | 文件存储、权限、元数据 |
| 工作流 | `OwWfManager` | `PowerLmsServer/Managers/Workflow/` | 多级审批、状态流转 |
| 权限 | `AuthorizationManager` | `PowerLmsServer/Managers/Auth/` | 细粒度权限+多租户 |
| 组织 | `OrgManager` | `PowerLmsServer/Managers/System/` | 商户/机构树+缓存 |
| 数据字典 | `DataDicManager` | `PowerLmsServer/Managers/BaseData/` | 参数配置 |
| Excel | `OwDataUnit`+`OwNpoiUnit` | `Bak/OwDbBase/` | 高性能导入导出 |
| 缓存 | `OwCacheExtensions` | `Bak/OwBaseCore/` | 统一缓存失效 |
| 批量写入 | `OwBatchDbWriter` | `Bak/OwBaseCore/` | 数据库批量操作 |
| 应用日志 | `OwSqlAppLogger` | `PowerLmsServer/Managers/System/` | 操作日志记录 |
| 验证码 | `CaptchaManager` | `PowerLmsServer/Managers/System/` | 验证码生成验证 |

### 4.3 业务模块索引
| 业务域 | Manager类 | 主要实体 | 位置 |
|--------|-----------|----------|------|
| 客户资料 | `CustomerManager` | `PlCustomer` | `PowerLmsServer/Managers/Customer/` |
| 海运业务 | (控制器直接调用) | `DocOceanExport` | `PowerLmsWebApi/Controllers/Business/SeaFreight/` |
| 空运业务 | (控制器直接调用) | `DocAirExport` | `PowerLmsWebApi/Controllers/Business/AirFreight/` |
| 工作号 | `JobManager` | `PlJob` | `PowerLmsServer/Managers/Business/` |
| 费用管理 | `FinancialManager` | `DocFee` | `PowerLmsServer/Managers/Financial/` |
| 结算单 | `FinancialManager` | `Settlement` | `PowerLmsServer/Managers/Financial/` |
| 发票 | `FinancialManager` | `Invoice` | `PowerLmsServer/Managers/Financial/` |
| OA费用 | `OaExpenseManager` | `OaExpenseRequisition` | `PowerLmsServer/Managers/OA/` |

---

## 🔍 5. 常见问题快速查找

### 5.1 如何添加新业务功能?
1. 查看 `.github/copilot-instructions.md` 第6节(函数实现约束)
2. 在 `PowerLmsServer/Managers/` 对应业务域创建Manager
3. 在 `PowerLmsWebApi/Controllers/` 创建控制器(仅校验+调用Manager)
4. 在 `PowerLmsData/` 创建或修改实体
5. 创建数据库迁移(手动执行)
6. 更新 `CHANGELOG.md` 和 `TODO.md`

### 5.2 多租户数据隔离在哪里实现?
- **核心逻辑**: `OrgManager.cs` 的组织树过滤
- **权限校验**: `AuthorizationManager.cs`
- **应用位置**: 所有Manager的查询/修改方法
- **数据库层**: 所有业务实体包含OrgId字段

### 5.3 缓存如何使用?
- **统一入口**: `OwCacheExtensions`(禁止直接用IMemoryCache)
- **失效机制**: CancellationToken + 优先级回收
- **典型示例**: `OrgManager` 的组织树缓存、`AccountManager` 的用户缓存
- **详细文档**: 见 `.github/indexes/PowerLmsServer.md` §缓存管理模式

### 5.4 文件上传/下载如何处理?
- **统一服务**: `OwFileService`
- **权限控制**: 基于多租户的文件隔离
- **元数据管理**: 文件与业务单据关联

### 5.5 如何导入导出Excel?
- **推荐工具**: `OwDataUnit` + `OwNpoiUnit`(Bak/OwDbBase/)
- **简单字典**: `ImportExportService.ImportSimpleDictionaries()`
- **通用实体**: `ImportExportService.ImportDictionaries<T>()`
- **详细文档**: 见 `.github/indexes/PowerLmsServer.md` §导入导出服务

### 5.6 权限验证如何实现?
- **Manager层**: `_AuthorizationManager.Demand(out string err, "权限码")`
- **控制器层**: 返回 `StatusCode(403, err)`
- **权限码查询**: 见 `.github/indexes/PowerLmsWebApi.md` §权限码索引
- **权限配置**: 见 `.github/project-specific.md` §权限体系设计

### 5.7 如何记录操作日志?
- **通用日志**: `_SqlAppLogger.LogGeneralInfo("操作类型")`
- **自定义日志**: `_SqlAppLogger.WriteLogItem(new OwAppLogItemStore {...})`
- **日志查询**: `OwAppLogView` 视图
- **详细文档**: 见 `.github/indexes/PowerLmsServer.md` §应用日志服务

---

## 📌 6. AI协作提示

### 6.1 修改代码前必读
1. 检查 `.github/copilot-instructions.md` 的约束(尤其第6节)
2. 查询对应层级的索引文档(API/业务/数据)
3. 搜索现有类名/方法名(`code_search`),保持风格一致
4. 优先复用基础设施(见§4.2表格)

### 6.2 文档更新规则
- **代码变更** → 立即更新 `TODO.md` 对应任务状态
- **功能完成** → 更新 `CHANGELOG.md`(业务价值+API变更)
- **架构调整** → 更新本文件(`project-context.md`)
- **实体新增** → 更新 `.github/indexes/PowerLmsData.md`

### 6.3 避免重复扫描的技巧
- **组织架构问题** → 直接看 `.github/indexes/PowerLmsServer.md` §OrgManager
- **权限问题** → 直接看 `.github/indexes/PowerLmsWebApi.md` §权限码索引
- **工作流问题** → 直接看 `.github/indexes/PowerLmsServer.md` §OwWfManager
- **缓存问题** → 直接看 `.github/indexes/PowerLmsServer.md` §缓存失效策略
- **实体字段** → 直接看 `.github/indexes/PowerLmsData.md` 对应实体

---

## 🚀 7. 快速命令参考

```bash
# 查找特定Manager
code_search ["ManagerName"]

# 查找实体定义
code_search ["EntityName"]

# 读取开发规范
get_file ".github/copilot-instructions.md"

# 读取项目特定配置
get_file ".github/project-specific.md"

# 读取API层索引
get_file ".github/indexes/PowerLmsWebApi.md"

# 读取业务层索引
get_file ".github/indexes/PowerLmsServer.md"

# 读取数据层索引
get_file ".github/indexes/PowerLmsData.md"
```

---

**使用原则**:AI每次协作时优先读取本文件,根据索引定位目标,避免全项目扫描。人工维护核心索引(§4.2业务模块表格),AI负责补充细节和格式化。

**索引更新时间**:2025-01-31
**适用版本**:PowerLms v1.0+
