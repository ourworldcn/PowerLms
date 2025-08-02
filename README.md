# PowerLms
货运物流业务管理系统，基于 .NET 6 构建。

## 核心特性
- 🚢 **货运物流专业**: 海运、空运、陆运、铁路等全流程管理
- 🏗️ **现代架构体系**: 前后端分离、RESTful API、微服务架构
- 🔐 **细粒级权限**: 角色管理、组织隔离、精细操作权限控制
- 📋 **业务流程**: 工作号、费用、结算、发票等多运输方式
- 💰 **财务集成**: 自动生成凭证、金蝶接口
- 📊 **数据分析**: 集成化统计和业务数据分析

## 📦 **项目结构与子包介绍**

### 🏗️ **核心业务包**
```
PowerLms/
├── PowerLmsWebApi/     # 🌐 API接口层
│   ├── Controllers/    # RESTful API控制器
│   ├── Dto/           # 数据传输对象
│   ├── Middleware/    # 中间件组件
│   └── Program.cs     # 应用程序入口
│
├── PowerLmsServer/     # 💼 业务逻辑层
│   ├── Managers/      # 业务管理器 (AccountManager, OrgManager等)
│   ├── Services/      # 业务服务类
│   └── EfData/        # EF数据库上下文
│
├── PowerLmsData/       # 🗄️ 数据访问层
│   ├── 客户资料/       # 客户相关实体 (PlCustomer)
│   ├── 业务/          # 业务单据实体 (PlJob, DocFee)
│   ├── 财务/          # 财务相关实体 (PlInvoices)
│   ├── 权限/          # 权限管理实体
│   ├── 机构/          # 组织机构实体
│   └── Migrations/    # 数据库迁移文件
└── Docs/              # 📚 设计文档
```

### 🔧 **基础设施包**
```
../Bak/
├── OwBaseCore/         # 🛠️ 核心工具库
│   ├── OwHelper        # 通用工具类
│   ├── OwStringUtils   # 字符串处理工具
│   ├── System.IO/      # I/O扩展
│   └── Extensions/     # .NET扩展方法
│
└── OwDbBase/          # 🗃️ 数据库基础库
    ├── EntityFrameworkCore/  # EF Core扩展
    ├── EfHelper        # 动态查询工具
    ├── OwTaskService   # 长时间运行任务服务
    └── SqlDependency/  # SQL依赖通知
```

### 📋 **子包功能详解**

#### 🌐 **PowerLmsWebApi** - API接口层
- **职责**: 提供RESTful API接口，处理HTTP请求
- **核心功能**: 
  - Swagger API文档生成
  - 请求路由和参数验证
  - 权限认证和授权
  - 异常处理和日志记录
- **技术栈**: ASP.NET Core Web API, Swagger, AutoMapper

#### 💼 **PowerLmsServer** - 业务逻辑层
- **职责**: 实现核心业务逻辑和领域服务
- **核心功能**:
  - 业务管理器 (AccountManager, OrgManager, AuthorizationManager)
  - 工作流引擎 (OwWfManager)
  - 文件服务 (OwFileService)
  - 外部系统集成 (金蝶、诺诺接口)
- **技术栈**: AutoMapper, EF Core, 依赖注入

#### 🗄️ **PowerLmsData** - 数据访问层
- **职责**: 定义数据模型和数据库访问
- **核心功能**:
  - 31个业务实体定义
  - 200+数据库迁移文件
  - 多租户数据隔离设计
  - 复杂业务关系建模
- **技术栈**: Entity Framework Core, SQL Server

#### 🛠️ **OwBaseCore** - 核心工具库
- **职责**: 提供通用工具类和扩展方法
- **核心功能**:
  - 字符串处理 (OwStringUtils)
  - 类型转换 (OwConvert)
  - 集合操作扩展
  - I/O流包装器
- **特点**: 无外部依赖，高性能优化

#### 🗃️ **OwDbBase** - 数据库基础库
- **职责**: 提供数据库操作增强功能
- **核心功能**:
  - 动态查询构建 (EfHelper)
  - 长时间运行任务 (OwTaskService)
  - SQL依赖通知 (SqlDependency)
  - 批量数据操作
- **技术栈**: Entity Framework Core, SQL Server

## 🔗 **子包依赖关系**

```
依赖层次结构 (从上到下):
┌─────────────────┐
│  PowerLmsWebApi │  ← 表示层: RESTful API
└─────────────────┘
         │
         ▼
┌─────────────────┐
│ PowerLmsServer  │  ← 业务层: 业务逻辑管理
└─────────────────┘
         │
         ▼
┌─────────────────┐
│  PowerLmsData   │  ← 数据层: 实体和数据访问
└─────────────────┘
         │
         ▼
┌─────────────────┐
│    OwDbBase     │  ← 基础层: 数据库增强工具
└─────────────────┘
         │
         ▼
┌─────────────────┐
│   OwBaseCore    │  ← 工具层: 通用工具类
└─────────────────┘
```

> 📖 详细信息请参阅 [系统架构.md](系统架构.md)

## 快速开始

### 环境要求
- .NET 6.0+
- Visual Studio 2022 / VS Code
- SQL Server 2016+

### 启动步骤
```bash
# 1. 获取代码
git clone https://github.com/ourworldcn/PowerLms.git

# 2. 配置数据库连接字符串 (PowerLmsWebApi/appsettings.json)
# 3. 数据库迁移 (用户手动执行)
#    注意：请在实际环境配置后再执行数据库迁移操作
#    Add-Migration InitialCreate -Context PowerLmsUserDbContext
#    Update-Database -Context PowerLmsUserDbContext

# 4. 启动项目 (F5)
# 5. 访问 https://localhost:5001/swagger
```

## 技术栈
- **.NET 6** + ASP.NET Core Web API
- **Entity Framework Core** + SQL Server
- **AutoMapper** + Dependency Injection
- **Serilog** + Swagger/OpenAPI

## 业务模块
- **海运管理**: 海运出口、进口业务维护
- **基础数据**: 客户资料、港口、航线、汇率等数据
- **客户管理**: 客户信息、市场计划、投诉处理
- **业务操作**: 工作号、费用、结算、发票等执行业务流程
- **业务审批**: 工作流引擎审批
- **财务管理**: 费单管理、业务结算、财务发票
- **统计报表**: 多维度业务数据统计

## 开发规范
> 📖 **重点文档**: [开发设计规范](Docs/)
- [系统架构](Docs/系统架构.md) - 基础设施组件和使用指南 ⭐
- [架构设计](Docs/CODE_CONVENTIONS.md) - 总体架构和业务设计
- [编码规范](Docs/CODE_STYLE.md) - 代码风格和命名
- [设计原则](Docs/DESIGN_PREFERENCE_GUIDE.md) - 总体原则和技术选型

## 🚀 当前开发状态

### 基础工具类重构 (最新)
- ✅ **OwStringUtils 类**: 在 OwBaseCore 项目中创建统一的字符串工具类
- ⚡ **密码生成优化**: 使用内存池和栈分配提升性能，支持无混淆字符
- 🔄 **兼容性保持**: PasswordGenerator 类标记为过时但保持向后兼容
- 🔧 **依赖解耦**: 移除 AccountManager 对 PasswordGenerator 的依赖注入
- 🎯 **代码统一**: PowerLmsServer 的 StringUtils 委托给 OwBaseCore 实现
- 📈 **功能扩展**: 新增命名转换、安全截取、字符串格式化等实用方法

### OA日常费用申请单模块
- ✅ **基础CRUD**: 申请单创建、查询、修改、删除、审核
- 📝 **明细管理**: 财务人员专业费用拆分功能
- 💯 **金额校验**: 明细合计与主单金额强制一致性验证
- 🧾 **凭证生成**: 期间-凭证字-序号格式，支持重号警告
- 🔄 **工作流集成**: 复用现有OwWf审批流程框架
- ⚠️ **数据结构**: 需移除明细表币种/汇率字段
- 📎 **文件上传**: 申请阶段发票文件上传功能
- 📋 **流程模板**: 日常费用收/付款审批流程配置

### 核心基础设施
- 📁 **文件管理**: OwFileService完整文件存储和权限控制
- 🔄 **流程管理**: OwWfManager工作流框架，支持多级审批
- 🔐 **权限系统**: 基于角色和组织的细粒度访问控制
- 💼 **金蝶接口**: 财务系统凭证数据导出
- 🛠️ **工具类库**: StringUtils提供常用字符串处理和密码生成功能

## 贡献指南
```bash
# 标准流程
git checkout -b feature/新功能描述
# 遵循编码规范开发
git commit -m 'feat: 添加新功能'
git push origin feature/新功能描述
# 提交 Pull Request
```

## 许可证
MIT License - 参见 [LICENSE](LICENSE)

---
*PowerLms - 专业的货运物流业务管理系统*
