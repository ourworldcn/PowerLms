<!--
PowerLms Metadata
workspace: C:\Users\zc-home\source\ourworldcn\PowerLms\
framework: .NET 6
copilot_prompt: .github\copilot-instructions.md
copilot_reference: #prompt:'.github\copilot-instructions.md'
architecture: API→Server→Data→OwDbBase→OwBaseCore
code_lines: 91941
team: 3
projects: [PowerLmsWebApi, PowerLmsServer, PowerLmsData, OwDbBase, OwBaseCore]
infrastructure: [OwFileService, OwWfManager, AuthorizationManager, OrgManager, DataDicManager, OwDataUnit, OwNpoiUnit]
deprecated: [NpoiManager]
workspace_structure: 分层架构(API-业务-数据)，基础设施完备，基于Bak目录的核心组件依赖
-->

# PowerLms
货运物流业务管理系统 | 基于 .NET 6 构建的企业级平台 | 约9.2万行代码 | 3人精英团队

[![.NET](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat&logo=.net)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-6.0-512BD4?style=flat&logo=microsoft)](https://docs.microsoft.com/en-us/ef/)
[![规模](https://img.shields.io/badge/代码规模-9.2万行-green?style=flat)](README.md)
[![团队](https://img.shields.io/badge/团队-3人精英-blue?style=flat)](README.md)

PowerLms 是面向货运与综合物流企业的多租户 TMS 平台，覆盖海运/空运/陆运/铁路等全流程，提供权限管理、组织架构、工作流审批、财务集成与高性能数据处理能力。

## ✨ 核心价值
- 一体化：客户资料、业务单据、费用结算、发票、工作流与报表统一平台
- 多租户：按商户/公司/机构分层的数据与权限隔离
- 可扩展：基于 Manager 基础设施复用，支持领域模块拓展与外部系统集成
- 高可靠：严谨权限模型、日志追踪、可观测与缓存体系

## 🏗️ 总体架构

分层设计，职责清晰，基础设施优先复用：

```
API 层（PowerLmsWebApi）
  └─ Server 业务层（PowerLmsServer，Managers）
       └─ Data 数据层（PowerLmsData，EF Core）
            └─ 基础设施（Bak/OwDbBase, Bak/OwBaseCore, bak/OwExtensions）
```

### 📁 解决方案结构（概要）
```
PowerLms
├── PowerLmsWebApi/      # API层：RESTful + Swagger + JWT
├── PowerLmsServer/      # 业务层：Managers + 基础设施集成
├── PowerLmsData/        # 数据层：实体与迁移
└── Bak/                 # 基础设施：OwDbBase, OwBaseCore, OwExtensions
```

## 🔩 基础设施能力
- 文件管理：`OwFileService`（存储、权限、元数据）
- 工作流引擎：`OwWfManager`（多级审批、状态流转）
- 权限系统：`AuthorizationManager`（细粒度权限 + 多租户隔离）
- 组织管理：`OrgManager`（商户/公司/机构树 + 缓存）
- 数据字典：`DataDicManager`（参数与枚举配置）
- Excel处理：`OwDataUnit` + `OwNpoiUnit`（高性能导入/导出）
- 费用分次收付：`ActualFinancialTransaction`

> 规范：禁止重复造轮子，优先复用以上基础设施。缓存体系统一使用 `OwCacheExtensions`。

## 📦 业务能力（模块总览）
- 客户资料：客户、联系人、开票信息、装货地址、海关检疫
- 业务单据：海运/空运/进口/出口/工作号与费用核算
- 财务管理：结算单、发票、凭证、AR/AP、金蝶导出
- 办公与流程：OA日常费用、流程模板与审批
- 权限与组织：角色权限、商户/公司/机构多层级

## 🧰 技术栈
- 运行时：.NET 6, ASP.NET Core, EF Core 6, SQL Server 2016+
- 架构：依赖注入、Manager模式、分层清晰、统一日志
- API：RESTful + Swagger，JWT 鉴权，模型验证

## ⚙️ 开发与运行

### 环境要求
- .NET 6 SDK
- SQL Server 2016+
- Visual Studio 2022 或 VS Code

### 快速开始
```bash
# 克隆
git clone https://github.com/ourworldcn/PowerLms.git
# 配置 PowerLmsWebApi/appsettings.json 中的数据库连接
# 运行
dotnet restore && dotnet run --project PowerLmsWebApi
# 访问 Swagger
https://localhost:5001/swagger
```

### 配置要点（概要）
- 数据库：`appsettings.json` 指定连接串（禁止自动迁移，按发布节奏手动迁移）
- 鉴权：JWT 配置、跨域策略
- 日志：`Microsoft.Extensions.Logging` 统一输出
- 缓存：`OwCacheExtensions` 统一失效与优先级回收

## 🔒 安全与多租户
- 鉴权与授权：JWT + 角色/权限模型
- 多租户隔离：按商户/公司/机构层级过滤与校验
- 敏感数据：密码哈希、最小权限、审计日志

## ⚡ 性能与质量
- 缓存：组织与权限等热点数据集中缓存，支持取消令牌失效
- 数据：EF Core 优化、批处理导入导出、异步任务存储
- 观测：系统日志与应用日志视图，关键路径追踪

## 📚 文档导航
- 变更记录：`CHANGELOG.md`
- 待办与计划：`TODO.md`
- 团队开发规范：`.github/copilot-instructions.md`
- API 自描述：Swagger（运行后访问 `/swagger`）

> 说明：README 仅保留全局与概要信息。具体问题、任务与阶段计划请见 `CHANGELOG.md` 与 `TODO.md`。

## 🤝 贡献与协作
- 风格统一：遵循项目代码风格与命名规范
- 分层约束：业务逻辑写在 Manager；控制器仅做校验与异常处理
- 数据约束：不在实体使用 `record`；不采用自动迁移
- 提交规范：功能完成→自测通过→更新 `CHANGELOG.md`（必要时）

---

PowerLms —— 专业的货运物流业务管理系统。专注架构一致性、基础设施复用与企业级质量。
