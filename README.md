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
infrastructure: [OwFileService, OwWfManager, AuthorizationManager, OrgManager, DataDicManager, OwDataUnit, OwNpoiUnit, MawbManager]
deprecated: [NpoiManager]
workspace_structure: 分层架构(API-业务-数据)，基础设施完备，基于Bak目录的核心组件依赖
naming_convention: Ea=Export Air(空运出口), Ia=Import Air(空运进口), Es=Export Seaborne(海运出口), Is=Import Seaborne(海运进口)
-->

<!-- 
🤖 AI快速索引导航 - 优先读取以下文件，避免全项目扫描
================================================================================
📋 核心文档（按优先级）：
  1. .github/project-context.md      - 架构索引、Manager定位表、FAQ
  2. .github/project-specific.md     - 项目配置、权限体系、技术约束
  3. .github/copilot-instructions.md - 开发规范、编程约束
  4. TODO.md                         - 当前任务和执行计划
  5. CHANGELOG.md                    - 历史变更记录
  6. 部署指南.txt                     - 部署运维说明

⚡ AI协作原则：
  - 90%场景通过索引定位，无需code_search
  - 优先查询 project-context.md 的Manager表格
  - 遵循 copilot-instructions.md 的强制约束
  - 人工维护索引，AI负责补充细节
================================================================================
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

### 📁 解决方案结构（详细）
```
PowerLms
├── PowerLmsWebApi/                # API层：RESTful + Swagger + JWT
│   └── Controllers/
│       ├── Business/              # 业务控制器
│       │   ├── AirFreight/        # 空运模块
│       │   │   ├── EaMawbController.cs     # 空运出口主单（Ea=Export Air）
│       │   │   ├── EaHawbController.cs     # 空运出口分单
│       │   │   ├── IaManifestController.cs # 空运进口舱单（Ia=Import Air）⭐
│       │   │   └── MawbController.cs       # 主单领用登记
│       │   ├── SeaFreight/        # 海运模块
│       │   │   ├── EsMblController.cs      # 海运出口主提单（Es=Export Seaborne）
│       │   │   ├── EsHblController.cs      # 海运出口分提单
│       │   │   └── ...
│       │   └── ...
│       ├── Customer/              # 客户资料
│       ├── Financial/             # 财务管理
│       │   ├── DocFeeController.cs        # 费用管理
│       │   ├── DocBillController.cs       # 账单管理
│       │   └── ...
│       └── ...
├── PowerLmsServer/                # 业务层：Managers + 基础设施集成
│   └── Managers/
│       ├── Business/              # 业务Manager
│       │   ├── MawbManager.cs     # 主单领用登记业务逻辑 ⭐
│       │   └── ...
│       ├── Financial/             # 财务Manager
│       ├── Customer/              # 客户Manager
│       └── ...
├── PowerLmsData/                  # 数据层：实体与迁移
│   ├── 主营业务/                  # 主营业务实体（按业务类型分类）
│   │   ├── PlJob.cs               # 业务总表
│   │   ├── DocFee.cs              # 费用表
│   │   ├── DocBill.cs             # 账单表
│   │   ├── 空运出口/              # 空运出口专属实体（Ea=Export Air）
│   │   │   ├── PlEaDoc.cs         # 空运出口单
│   │   │   ├── EaMawb.cs          # 主单
│   │   │   ├── EaMawbOtherCharge.cs    # 主单其他费用
│   │   │   ├── EaCubage.cs        # 主单委托明细
│   │   │   ├── EaGoodsDetail.cs   # 主单品名明细
│   │   │   ├── EaContainer.cs     # 主单集装器
│   │   │   ├── EaHawb.cs          # 分单
│   │   │   ├── EaHawbOtherCharge.cs    # 分单其他费用
│   │   │   ├── EaHawbCubage.cs    # 分单委托明细
│   │   │   └── PlEaMawb*.cs       # 主单领用登记（领入/领出）⭐
│   │   ├── 空运进口/              # 空运进口专属实体（Ia=Import Air）⭐
│   │   │   ├── PlIaDoc.cs         # 空运进口单
│   │   │   ├── IaManifest.cs      # 空运进口舱单主表 ⭐
│   │   │   └── IaManifestDetail.cs# 空运进口舱单明细 ⭐
│   │   ├── 海运出口/              # 海运出口专属实体（Es=Export Seaborne）
│   │   │   ├── PlEsDoc.cs         # 海运出口单
│   │   │   ├── EsMbl.cs           # 主提单
│   │   │   ├── EsHbl.cs           # 分提单
│   │   │   └── ContainerKindCount.cs # 箱型箱量子表
│   │   └── 海运进口/              # 海运进口专属实体（Is=Import Seaborne）
│   │       └── PlIsDoc.cs         # 海运进口单
│   ├── 客户资料/                  # 客户相关实体
│   ├── 财务/                      # 财务相关实体
│   ├── 权限/                      # 权限相关实体
│   ├── 机构/                      # 组织机构实体
│   ├── 基础数据/                  # 基础数据字典
│   ├── 流程/                      # 工作流实体
│   ├── OA/                        # OA相关实体
│   ├── 账号/                      # 账号相关
│   ├── 应用日志/                  # 应用日志
│   ├── PowerLmsUserDbContext.cs   # EF Core DbContext
│   └── Migrations/                # 数据库迁移（150+文件）
└── Base/                          # 基础设施：外部通用项目
    ├── OwDbBase/                  # 数据访问基类、Excel工具（OwDataUnit）
    ├── OwBaseCore/                # 核心扩展、缓存管理
    └── OwExtensions/              # NPOI扩展方法（OwNpoiUnit）
```

> ⭐ 标记：本次修复涉及的关键文件

## 🔩 基础设施能力
- **文件管理**：`OwFileService`（存储、权限、元数据）
- **工作流引擎**：`OwWfManager`（多级审批、状态流转）
- **权限系统**：`AuthorizationManager`（细粒度权限 + 多租户隔离）
- **组织管理**：`OrgManager`（商户/公司/机构树 + 缓存）
- **数据字典**：`DataDicManager`（参数与枚举配置）
- **Excel处理**：
  - `OwDataUnit`（位于OwDbBase） - EF Core + NPOI批量导入导出
  - `OwNpoiUnit`（位于OwExtensions） - 高性能Excel数据处理
- **费用分次收付**：`ActualFinancialTransaction`（支持分次结算）
- **主单管理**：`MawbManager`（主单号校验、批量生成、领入领出登记）

> 规范：禁止重复造轮子，优先复用以上基础设施。缓存体系统一使用 `OwCacheExtensions`。

## 🏷️ 命名规范（重要！）
**业务模块前缀**：
- `Ea` = Export Air（空运**出口**）
- `Ia` = Import Air（空运**进口**）
- `Es` = Export Seaborne（海运**出口**）
- `Is` = Import Seaborne（海运**进口**）

**示例**：
- `EaMawb` = 空运出口主单
- `EaHawb` = 空运出口分单
- `IaManifest` = 空运进口舱单
- `IaManifestDetail` = 空运进口舱单明细

> ⚠️ **AI协作注意**：修改代码前请先确认业务模块属于**出口**还是**进口**，避免混淆！

## 📦 业务能力（模块总览）
- **客户资料**：客户、联系人、开票信息、装货地址、海关检疫
- **业务单据**：
  - 海运/空运/进口/出口业务单据
  - 工作号（PlJob）统一管理
  - 费用核算与账单生成（支持批量操作）
  - 主单领用登记（空运出口）
  - 主单制作（EaMawb）与分单制作（EaHawb）
- **财务管理**：
  - 费用管理（DocFeeController）- 支持批量审核
  - 账单管理（DocBillController）- 支持从费用批量生成账单
  - 结算单、发票、凭证
  - AR/AP分次收付
  - 金蝶导出与科目配置
- **办公与流程**：
  - OA日常费用申请
  - 工作流模板与多级审批（已修复状态同步问题）
- **权限与组织**：
  - 角色权限精细控制
  - 商户/公司/机构多层级
  - 多租户数据隔离

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
- **风格统一**：遵循项目代码风格与命名规范（详见 `.github/copilot-instructions.md`）
- **实体命名约束**：
  - 禁止Type后缀（如 ~~`CustomerType`~~，应为 `CustomerCategory`）
  - 禁止Class后缀（如 ~~`ProductClass`~~，应为 `ProductCategory`）
  - 语义化命名，直接表达业务概念
- **分层约束**：
  - 业务逻辑写在 Manager
  - 控制器仅做校验与异常处理
- **数据约束**：
  - 不在实体使用 `record` 类型
  - 不采用自动迁移（手动执行Add-Migration和Update-Database）
  - DateTime字段统一使用 `[Precision(3)]` 特性（毫秒级精度）
- **提交规范**：
  - 功能完成→自测通过→更新 `CHANGELOG.md`（必要时）
  - 重大变更需同步更新文档

---

**PowerLms —— 专业的货运物流业务管理系统**  
专注架构一致性、基础设施复用与企业级质量。

**最后更新**：2026-02-06

---

## 🎯 最新功能（2026-02-06）

### ✨ 批量生成账单
- **从费用批量生成账单**：支持选择多个已审核费用，一键生成账单
- **智能分组**：按工作号+结算单位+收支方向三维分组
- **字段自动填充**：从工作号自动带入主单号、港口、货物信息等13个字段
- **原子化操作**：全部成功或全部失败，保证数据一致性
- **接口**：`POST /api/DocBill/AddDocBillsFromFees`

### 🏗️ 架构优化
- **财务模块重构**：
  - 费用管理独立为`DocFeeController`（路由：`/api/DocFee/*`）
  - 账单管理独立为`DocBillController`（路由：`/api/DocBill/*`）
  - 语义更清晰，职责更单一
- **批量审核升级**：费用审核接口支持批量操作

### 🐛 问题修复
- **工作流状态同步**：修复申请单审批完成后状态未刷新的问题
- **实体命名规范**：空运出口主单/分单字段名统一使用`Kind`/`Category`后缀

### 📋 空运出口模块
- **主单制作**：`EaMawbController` - 支持主单、其他费用、委托明细、品名、集装器
- **分单制作**：`EaHawbController` - 支持分单、其他费用、委托明细
- **命名规范**：移除`Pl`前缀，采用简洁的`Ea`（Export Air）前缀


