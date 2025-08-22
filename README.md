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

## 📋 当前开发状态 (2025年8月)

### ✅ 已完成
- **OA费用申请Bug修复** - SaveChanges调用缺失问题解决
- **数据锁定功能** - 审批中单据主表锁定，状态驱动权限控制
- **字典导入导出架构** - API框架、DTO定义、多租户安全控制完成

### 🎯 重大需求评审 (8月16日会议决议)

#### 核心功能重构计划
基于客户测试反馈和业务需求变更，项目进入关键功能重构阶段：

| 重构模块 | 状态 | 复杂度 | 影响范围 |
|---------|------|--------|----------|
| **申请单审批流程** | 🔄 设计中 | 高 | 核心业务流程 |
| **账期管理机制** | 🔄 设计中 | 高 | 财务月结流程 |
| **财务日期逻辑** | 🔄 设计中 | 中 | 业务单据 |
| **字典导入导出** | 🔄 开发中 | 中 | 基础数据管理 |

#### 📋 正在进行 - 核心业务流程重构

**🔴 紧急开发任务：**

##### 1. 申请单一键回退功能 (2.5天)
- **业务场景**：已审批完成的申请单发现错误需要撤销修改
- **核心逻辑**：权限控制 → 状态快照 → 清空审批流 → 回退状态 → 释放锁定费用
- **技术挑战**：涉及工作流引擎核心逻辑，需要确保数据一致性
- **风险控制**：详细测试审批流状态变更和费用释放机制

##### 2. 账期管理与工作号关闭 (2.5天)
- **新增功能**：公司级账期概念 (YYYYMM格式)，统一月度关闭机制
- **架构变更**：新建机构参数表，废除原有单票关闭功能
- **业务价值**：与金蝶财务系统对齐，确保财务数据一致性
- **技术要点**：批量状态更新、账期自动递增、权限验证

##### 3. 财务日期联动重构 (1天)
- **逻辑变更**：财务日期改为只读，由到港/开航日期联动决定
- **业务规则**：进口=到港日期，出口=开航日期，不允许选择当前月份之前
- **数据清理**：处理现有 `2001-01-01` 无效默认值

#### 开发计划与里程碑

| 阶段 | 任务 | 工期 | 目标日期 | 责任人 |
|------|------|------|----------|--------|
| **Phase 1** | 申请单回退+账期管理 | 5天 | 8月23日 | 后端ZC |
| **Phase 2** | 财务日期+字典导入导出 | 3天 | 8月26日 | 全栈配合 |
| **Phase 3** | 界面优化+客户管理 | 4.5天 | 8月30日 | 前端主导 |
| **验收测试** | 集成测试+Bug修复 | 2天 | 9月2日 | 团队协作 |

**总工期**: 14.5天 | **目标上线**: 2025年9月2日 | **风险级别**: 中等

#### 🎯 会议关键决议

**业务逻辑优化：**
- ✅ **申请单回退**：支持主营业务+OA费用申请单，权限控制+消息通知
- ✅ **账期统一管理**：废除单票关闭，统一月结批量关闭机制
- ✅ **导入策略调整**：重复数据采用覆盖(Update)模式，按类型分别导出
- ✅ **客户资料软删除**：增加有效/无效状态，选择器默认显示有效客户

**用户体验提升：**
- ✅ **客户选择器**：改为弹窗模式，多字段显示+多维度搜索
- ✅ **费用明细增强**：点击已申请金额显示关联申请单详情
- ✅ **财务日期约束**：防止选择过期日期，确保数据时效性

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
*当前重点：核心业务流程重构 | 目标9月2日完成上线准备*
