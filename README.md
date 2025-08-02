# PowerLms
货运物流业务管理系统，基于 .NET 6 构建的企业级平台。

## 系统概览

**PowerLms** 是一个专业的货运物流业务管理系统，覆盖海运、空运、陆运、铁路等全流程管理，具备完整的企业级基础设施。

### 核心特性
- 🚢 **物流全流程**: 支持海运、空运、陆运、铁路等多运输方式
- 🏗️ **企业级架构**: RESTful API、前后端分离、微服务就绪
- 🔐 **细粒度权限**: 基于角色和组织的访问控制，支持多租户隔离
- 💰 **财务集成**: 金蝶、诺诺等外部系统对接，自动生成凭证
- ⚡ **基础设施完备**: 工作流引擎、文件管理、权限系统企业级就绪

### 系统规模
- **代码规模**: 31.7万行 C# 代码
- **系统定级**: 大型企业级系统
- **项目组成**: 5个子项目，398个文件
- **数据复杂度**: 200+数据迁移文件，支持复杂业务建模

## 项目架构

### 核心子项目
```
PowerLms/
├── PowerLmsWebApi/     # API层: RESTful接口、Swagger文档
├── PowerLmsServer/     # 业务层: 管理器、服务、工作流
├── PowerLmsData/       # 数据层: EF实体、数据库上下文
└── Docs/              # 设计文档

../Bak/
├── OwBaseCore/         # 工具库: 字符串、类型转换、扩展方法
└── OwDbBase/          # 数据库库: EF扩展、动态查询、任务服务
```

### 依赖关系
```
PowerLmsWebApi → PowerLmsServer → PowerLmsData → OwDbBase → OwBaseCore
```

### 技术栈
- **.NET 6** + ASP.NET Core Web API
- **Entity Framework Core** + SQL Server  
- **AutoMapper** + 依赖注入
- **Swagger/OpenAPI** + RESTful API

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

### 使用指南
```csharp
// 文件操作 → 使用 OwFileService
// 审批流程 → 使用 OwWfManager  
// 权限验证 → 使用 AuthorizationManager
```

## 业务模块

### 核心业务
- **客户资料**: 客户信息、联系人、开票信息、装货地址
- **业务单据**: 海运/空运进出口、工作号管理、费用核算
- **财务管理**: 结算单、发票、费用方案、凭证生成
- **系统管理**: 用户权限、组织架构、数据字典、工作流

### 外部集成
- **金蝶接口**: 财务凭证数据导出
- **诺诺发票**: 电子发票开具集成
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

# 2. 配置数据库连接 (PowerLmsWebApi/appsettings.json)

# 3. 启动项目
dotnet run --project PowerLmsWebApi

# 4. 访问 API 文档
https://localhost:5001/swagger
```

### 数据库迁移
```bash
# ⚠️ 注意：系统禁止自动迁移，需手动执行
Add-Migration [MigrationName] -Context PowerLmsUserDbContext
Update-Database -Context PowerLmsUserDbContext
```

## 开发规范

### 设计原则
1. **基础设施优先**: 优先复用现有组件，避免重复开发
2. **稳定第一**: 渐进式修改，保持向后兼容
3. **分层架构**: 严格遵循 API → 业务 → 数据 层次结构
4. **权限控制**: 所有业务操作需要权限验证

### 关键约束
- **禁止自动数据迁移**: 所有数据库变更需手动规划
- **必须权限验证**: 重要操作需要 `AuthorizationManager.Demand()`
- **多租户隔离**: 数据查询需要 `OrgId` 过滤
- **异常处理**: 统一使用 `OwHelper.SetLastErrorAndMessage()`

### 开发文档
- [系统架构](Docs/系统架构.md) - 基础设施使用指南
- [编码规范](Docs/CODE_STYLE.md) - 代码风格和命名
- [设计原则](Docs/DESIGN_PREFERENCE_GUIDE.md) - 技术选型和原则

## 当前开发状态

### 最新进展
- ✅ **基础设施完备**: 文件管理、工作流、权限系统企业级就绪
- ✅ **OA费用申请**: 完整的申请→审批→结算→凭证流程
- ✅ **外部集成**: 金蝶、诺诺接口集成完成
- 🔄 **架构优化**: 工具类重构，依赖关系优化

### 技术债务
- ⚠️ 明细表币种/汇率字段需要移除
- ⚠️ 部分legacy代码需要重构
- ⚠️ 单元测试覆盖率需要提升

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
- 是否编写了必要的单元测试

---

**PowerLms** - 专业的货运物流业务管理系统  
*由3人精英团队开发，31.7万行高质量代码，企业级基础设施完备*
