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
-->

# PowerLms
货运物流业务管理系统，基于 .NET 6 构建的企业级平台。

## 📋 GitHub Copilot 智能体配置

**重要**: 本项目已配置GitHub Copilot智能体开发规范，确保代码生成符合PowerLms企业级标准。

### 提示词文件位置
- **文件路径**: `.github\prompts\me.prompt.md`
- **引用方式**: `#prompt:'.github\prompts\me.prompt.md'`
- **自动应用**: GitHub Copilot会自动读取并应用企业级开发标准

### 智能体功能
- 强制使用基础设施组件（OwFileService、OwWfManager、AuthorizationManager）
- 严格遵循PowerLms代码风格要求
- 确保权限验证和多租户数据隔离
- 优先使用OwDataUnit + OwNpoiUnit高性能Excel处理方案
- 禁止使用废弃的NpoiManager组件

## 系统概览

**PowerLms** 是一个专业的货运物流业务管理系统，覆盖海运、空运、陆运、铁路等全流程管理，具备完整的企业级基础设施。

### 核心特性
- 🚢 **物流全流程**: 支持海运、空运、陆运、铁路等多运输方式
- 🏗️ **企业级架构**: RESTful API、前后端分离、微服务就绪
- 🔐 **细粒度权限**: 基于角色和组织的访问控制，支持多租户隔离
- 💰 **财务集成**: 金蝶、诺诺等外部系统对接，自动生成凭证
- ⚡ **基础设施完备**: 工作流引擎、文件管理、权限系统企业级就绪
- 🚀 **高性能数据处理**: OwDataUnit + OwNpoiUnit 优化的Excel批量处理方案

## 🏗️ 基础设施组件

> **重要**: 新功能开发请优先复用现有基础设施！

### 核心组件
| 组件 | 服务类 | 功能 |
|------|--------|------|
| **文件管理** | `OwFileService` | 文件存储、权限控制、元数据管理 |
| **工作流引擎** | `OwWfManager` | 多级审批、动态审批人、状态跟踪 |
| **权限管理** | `AuthorizationManager` | 细粒度权限、多租户隔离 |
| **组织管理** | `OrgManager` | 商户、公司、部门管理 |
| **数据字典** | `DataDicManager` | 系统配置、枚举管理 |
| **Excel处理** | `OwDataUnit` + `OwNpoiUnit` | 高性能Excel导入导出、批量数据处理 |

### 使用指南
```csharp
// 文件操作 → 使用 OwFileService
// 审批流程 → 使用 OwWfManager  
// 权限验证 → 使用 AuthorizationManager
// Excel处理 → 使用 OwDataUnit + OwNpoiUnit（替代废弃的NpoiManager）
```

## 业务模块

### 核心业务
- **客户资料**: 客户信息、联系人、开票信息、装货地址、海关检疫状态
- **业务单据**: 海运/空运进出口、工作号管理、费用核算
- **财务管理**: 结算单、发票、费用方案、凭证生成、AR/AP财务编码对接
- **系统管理**: 用户权限、组织架构、数据字典、工作流
- **数据处理**: 高性能Excel导入导出、批量数据同步

### 外部集成
- **金蝶接口**: 财务凭证数据导出，支持AR/AP编码精确匹配
- **诺诺发票**: 电子发票开具集成，完整回调处理
- **多租户**: 支持商户、公司、部门多级隔离

## 快速开始

### 环境要求
- .NET 6.0+
- SQL Server 2016+
- Visual Studio 2022 / VS Code

### 启动步骤
```bash
# 1. 克隆代码
git clone https://github.com/ourworldcn/PowerLms.git

# 2. 配置数据库连接（PowerLmsWebApi/appsettings.json）

# 3. 启动项目
dotnet run --project PowerLmsWebApi

# 4. 访问 API 文档
https://localhost:5001/swagger
```

## 开发规范

### 设计原则
1. **基础设施优先**: 优先复用现有组件，避免重复开发
2. **稳定第一**: 渐进式修改，保持向后兼容
3. **分层架构**: 严格遵循 API → 业务 → 数据 层次结构
4. **权限控制**: 所有业务操作需要权限验证
5. **性能优先**: 使用高性能的OwDataUnit处理大批量数据

### 关键约束
- **禁止自动数据迁移**: 所有数据库变更需手动规划
- **必须权限验证**: 重要操作需要 `AuthorizationManager.Demand()`
- **多租户隔离**: 数据查询需要 `OrgId` 过滤
- **异常处理**: 统一使用 `OwHelper.SetLastErrorAndMessage()`
- **Excel处理标准**: 使用OwDataUnit + OwNpoiUnit，避免使用废弃的NpoiManager

## 🚀 最新技术改进（2024年）

### Excel处理架构重构
- **废弃组件**: `NpoiManager` 类已完全废弃，标记为 `[Obsolete]`
- **新架构**: `OwDataUnit` + `OwNpoiUnit` 高性能组合
- **性能提升**: 
  - 跳过JSON序列化，直接字符串数组处理
  - 支持批量插入优化，自动处理重复数据
  - 使用PooledList减少内存分配

#### 最佳实践示例
```csharp
// ❌ 废弃方式（NpoiManager）
_npoiManager.WriteToDb(sheet, context, dbSet);

// ✅ 推荐方式（OwDataUnit + OwNpoiUnit）
var count = OwDataUnit.BulkInsertFromExcelWithStringList<T>(
    sheet, dbContext, ignoreExisting: true, logger, "操作描述");

// ✅ 高性能读取方式
using var allRows = OwNpoiUnit.GetStringList(sheet, out var headers);
var entities = OwNpoiUnit.GetSheet<T>(sheet);
```

## 当前开发状态

### 最新进展
- ✅ **基础设施完备**: 文件管理、工作流、权限系统企业级就绪
- ✅ **OA费用申请**: 完整的申请→审批→结算→凭证流程
- ✅ **外部集成**: 金蝶、诺诺接口集成完成
- ✅ **Excel处理重构**: OwDataUnit + OwNpoiUnit高性能方案
- ✅ **财务编码优化**: AR/AP编码精确对接金蝶系统
- ✅ **智能体集成**: GitHub Copilot 提示词配置完成

### 技术债务处理
- ✅ NpoiManager类已废弃，完成无用引用清理
- ✅ Excel处理性能优化，统一使用新架构
- ✅ GitHub Copilot 智能体开发规范建立
- ⚠️ 明细表币种/汇率字段需要移除
- ⚠️ 部分legacy代码需要重构
- ⚠️ 单元测试覆盖率需要提升

### 业务功能增强
- ✅ 海关检疫业务支持（`IsCustomsQuarantine`）
- ✅ 财务编码双轨制（AR客户编码 + AP供应商编码）
- ✅ 诺诺发票完整生命周期管理
- ✅ 航线数据高性能批量导入

## 贡献指南

### 开发流程
```bash
git checkout -b feature/新功能描述
# 开发完成后
git commit -m 'feat: 添加新功能'
git push origin feature/新功能描述
# 提交 Pull Request
```

### 审查要点
- 是否复用了现有基础设施组件
- 是否遵循了权限验证要求
- 是否保持了数据库向后兼容
- 是否使用了推荐的Excel处理方案
- 是否符合GitHub Copilot智能体提示词要求
- 是否编写了必要的单元测试

### Excel处理规范
```csharp
// ✅ 推荐：使用新的高性能方案
OwDataUnit.BulkInsertFromExcelWithStringList<Entity>()
OwNpoiUnit.GetSheet<Entity>()
OwNpoiUnit.GetStringList()

// ❌ 禁止：使用废弃的NpoiManager
NpoiManager.WriteToDb() // 已标记 [Obsolete]
NpoiManager.GetJson()   // 已标记 [Obsolete]
```

---

**PowerLms** - 专业的货运物流业务管理系统  
*由3人精英团队开发，31.7万行高质量代码，企业级基础设施完备*  
*2024年技术架构优化：高性能Excel处理，财务系统深度集成，GitHub Copilot智能体集成*
