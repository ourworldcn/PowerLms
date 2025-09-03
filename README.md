<!--
PowerLms Metadata
workspace: C:\Users\zc-home\source\ourworldcn\PowerLms\
framework: .NET 6
copilot_prompt: .github\prompts\me.prompt.md
copilot_reference: #prompt:'.github\prompts\me.prompt.md'
architecture: API→Server→Data→OwDbBase→OwBaseCore
code_lines: 317000
team: 3
projects: [PowerLmsWebApi, PowerLmsServer, PowerLmsData, OwDbBase, OwBaseCore]
infrastructure: [OwFileService, OwWfManager, AuthorizationManager, OrgManager, DataDicManager, OwDataUnit, OwNpoiUnit]
deprecated: [NpoiManager]
workspace_structure: 分层架构(API-业务-数据)，基础设施完备，基于Bak目录的核心组件依赖
-->

# PowerLms
货运物流业务管理系统 | 基于 .NET 6 构建的企业级平台 | 31.7万行代码 | 3人精英团队

[![.NET](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat&logo=.net)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-6.0-512BD4?style=flat&logo=microsoft)](https://docs.microsoft.com/en-us/ef/)
[![规模](https://img.shields.io/badge/代码规模-31.7万行-green?style=flat)](README.md)
[![团队](https://img.shields.io/badge/团队-3人精英-blue?style=flat)](README.md)

## ⚠️ 开发约束

- 🌏 **简体中文回答** | **禁止自动数据迁移** | **基础设施优先复用**
- 🤖 **GitHub Copilot集成** - 强制使用基础设施组件，严格代码风格，确保权限验证

## 🏗️ 系统架构

**专业货运物流TMS** - 海运/空运/陆运/铁路全流程，企业级基础设施完备

```
PowerLms解决方案 (API→Server→Data→OwDbBase→OwBaseCore)
├── PowerLmsWebApi/    # API层 (84文件) - RESTful + Swagger + JWT权限
├── PowerLmsServer/    # 业务层 (40文件) - Managers + 基础设施组件
├── PowerLmsData/      # 数据层 (274文件) - EF Core + 200+迁移文件
└── 基础设施 (Bak/)    # OwDbBase + OwBaseCore 核心组件
```

### 📁 实际解决方案结构
```
PowerLms解决方案
├── PowerLmsWebApi/          # 84个文件 - API控制器层
│   ├── Controllers/         # 分模块控制器
│   │   ├── Business/        # 业务模块(海运/空运/工作号)
│   │   ├── Customer/        # 客户资料管理
│   │   ├── Financial/       # 财务管理(费用/结算/金蝶)
│   │   ├── OA/             # 办公自动化
│   │   ├── System/         # 系统管理
│   │   ├── Tax/            # 税务发票
│   │   └── BaseData/       # 基础数据
│   └── Middleware/         # 异常处理中间件
├── PowerLmsServer/         # 40个文件 - 业务逻辑层
│   ├── Managers/           # 业务管理器
│   │   ├── Auth/          # 权限认证
│   │   ├── Business/      # 业务逻辑
│   │   ├── Financial/     # 财务管理
│   │   ├── Integration/   # 外部集成
│   │   └── System/        # 系统服务
│   └── Services/          # 通用服务
├── PowerLmsData/          # 274个文件 - 数据访问层
│   ├── Migrations/        # 200+迁移文件
│   ├── 业务/              # 业务实体
│   ├── 财务/              # 财务实体
│   ├── 客户资料/          # 客户实体
│   ├── 机构/              # 组织架构
│   └── 权限/              # 权限系统
└── 基础设施 (../Bak/)     # 核心组件依赖
    ├── OwDbBase/          # 数据库基础组件
    └── OwBaseCore/        # 核心基础组件
```

## ⚡ 基础设施组件

> **核心原则**: 新功能开发必须优先复用现有基础设施，禁止重复造轮子

| 组件 | 服务类 | 功能 | 使用场景 |
|------|--------|------|----------|
| **文件管理** | `OwFileService` | 存储+权限+元数据 | 发票上传、附件管理 |
| **工作流引擎** | `OwWfManager` | 多级审批+状态跟踪 | 费用审批、业务单据 |
| **权限管理** | `AuthorizationManager` | 细粒度权限+多租户 | 数据隔离、访问控制 |
| **组织管理** | `OrgManager` | 商户/公司/部门 | 多级组织架构 |
| **数据字典** | `DataDicManager` | 系统配置+枚举 | 业务参数配置 |
| **消息系统** | `OwMessageManager` | 内部消息+通知 | 系统消息推送 |
| **Excel处理** | `OwDataUnit` + `OwNpoiUnit` | 高性能导入导出 | 批量数据处理 |
| **分次收付** | `ActualFinancialTransaction` | 多笔收付记录 | 结算单分次付款 |

### 技术特性
- **自动注入**: `[OwAutoInjection]` | **配置监控**: `IOptionsMonitor<T>` | **数据库工厂**: `IDbContextFactory`
- **高性能**: 直接字符串数组处理，跳过JSON序列化 | **企业级**: 完整日志+异常处理+权限验证

## 🚀 业务核心 & 技术栈

### 业务模块
- **客户资料**: 客户+联系人+开票+装货地址+海关检疫
- **业务单据**: 海运/空运进出口+工作号+费用核算  
- **财务管理**: 结算单(PlInvoices)+发票+凭证+AR/AP编码对接
- **外部集成**: 金蝶财务+诺诺发票+多租户支持

### 技术栈
- **.NET 6** + **EF Core 6** + **ASP.NET Core** + **SQL Server 2016+**
- **RESTful API** + **Swagger** + **JWT** + **依赖注入** + **Microsoft.Extensions.Logging**

## 📋 当前开发状态 (2025年1月)

### ✅ 已完成 (重大功能)
- **申请单审批回退机制** ✅ - 完整的工作流清理+状态回退+权限控制 
- **账期管理机制** ✅ - 机构参数表+批量关闭+自动递增  
- **数据导入导出架构** ✅ - v2.0重构完成，统一服务+简单字典专用API
- **空运进口接口恢复** ✅ - 独立PlAirborneController创建完成
- **财务日期联动** ✅ - AccountDate字段正确配置为计算字段

### 🔴 紧急待修复 (阻塞项)
- **费用过滤Bug** ❌ - `fee_id`参数过滤未生效，`GetDocFeeRequisitionItem`方法需修复
- **OA申请单公司字段** ❌ - 需添加`CustomerId`字段关联客户资料表  
- **空运接口架构重复** ❌ - PlJobController.EaDoc.cs与PlAirborneController重复，需统一

### 🟡 功能增强任务 (中优先级)
- **费用列表申请单详情** ❌ - 点击已申请金额显示关联申请单信息
- **客户资料有效性管理** ❌ - 增加IsActive状态字段，软删除机制
- **客户选择器增强** ❌ - 弹窗式选择器，多字段显示+模糊搜索

### ⏸️ 暂缓功能
- **结算单导出金蝶** ⏸️ - 复杂业务逻辑，前端需预留"日常收款/付款"入口

### 🏗️ 架构完成状态

| 层级 | 完成状态 | 主要特征 |
|------|----------|----------|
| **API层** | ✅ 84文件完成 | 分模块控制器+统一异常处理+Swagger文档 |
| **业务层** | ✅ 40文件完成 | Manager模式+基础设施集成+权限验证 |
| **数据层** | ✅ 274文件完成 | EF Core实体+200+迁移+触发器支持 |
| **基础设施** | ✅ 核心组件完备 | 文件/工作流/权限/消息/Excel处理全覆盖 |

## ⚡ 快速开始

```bash
# 环境: .NET 6.0+ + SQL Server 2016+ + VS 2022/VS Code
git clone https://github.com/ourworldcn/PowerLms.git
# 配置 PowerLmsWebApi/appsettings.json 数据库连接
dotnet restore && dotnet run --project PowerLmsWebApi
# 访问 https://localhost:5001/swagger
```

## 🔧 开发规范

### 核心原则
1. **基础设施优先** - 复用现有组件，避免重复开发
2. **禁止自动迁移** - 手动规划数据库变更
3. **权限验证必须** - `AuthorizationManager.Demand()`
4. **多租户隔离** - 数据查询`OrgId`过滤
5. **Excel标准** - OwDataUnit + OwNpoiUnit (禁用废弃NpoiManager)

### 基础设施使用模板
```csharp
public class BusinessService
{
    private readonly OwFileService _fileService;
    private readonly OwWfManager _workflowManager;
    private readonly AuthorizationManager _authManager;
    
    // 文件上传 → OwFileService
    public async Task<FileInfo> UploadAsync(IFormFile file) 
        => await _fileService.SaveFileAsync(file, context);
    
    // 审批流程 → OwWfManager
    public async Task<bool> StartApprovalAsync(Guid entityId) 
        => await _workflowManager.StartWorkflowAsync(entityId, templateId);
}
```

### Excel处理规范
```csharp
// ✅ 推荐：高性能方案
var count = OwDataUnit.BulkInsertFromExcelWithStringList<T>(
    sheet, dbContext, ignoreExisting: true, logger, "操作描述");

// ❌ 禁止：废弃组件
NpoiManager.WriteToDb() // 已标记 [Obsolete]
```

### 开发检查清单
```markdown
✅ 新功能开发必检项
□ 文件操作 → OwFileService | □ 审批流程 → OwWfManager | □ 权限控制 → AuthorizationManager
□ 组织管理 → OrgManager | □ 系统配置 → DataDicManager | □ 消息通知 → OwMessageManager
□ Excel处理 → OwDataUnit + OwNpoiUnit | □ 分次收付 → ActualFinancialTransaction
□ 权限验证 | □ 多租户隔离 | □ 向后兼容 | □ 单元测试
```

---

**PowerLms** - 专业的货运物流业务管理系统  
*3人精英团队 | 31.7万行企业级代码 | 基础设施完备复用*  
*当前重点：解决阻塞性Bug | 完成功能增强 | 确保系统稳定性*
