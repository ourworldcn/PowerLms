# PowerLms
货运物流业务管理系统，基于 .NET 6 构建。

## 核心特性
- 🚢 **货运物流专业**: 海运、空运、陆运、铁路等全流程管理
- 🏗️ **现代架构体系**: 前后端分离、RESTful API、微服务架构
- 🔐 **细粒级权限**: 角色管理、组织隔离、精细操作权限控制
- 📋 **业务流程**: 工作号、费用、结算、发票等多运输方式
- 💰 **财务集成**: 自动生成凭证、金蝶接口
- 📊 **数据分析**: 集成化统计和业务数据分析

## 项目结构
```
PowerLms/
├── PowerLmsWebApi/     # API层 - RESTful接口
├── PowerLmsServer/     # 业务层 - 核心服务
├── PowerLmsData/       # 数据层 - EF模型
└── Docs/              # 设计文档
```

## 🏗️ **核心基础设施** ⭐

> **重要**: 系统已具备完整的基础设施组件，新功能开发请优先复用！

### 📁 通用文件管理
- **服务**: `OwFileService` - 完整的文件存储和权限控制
- **功能**: 文件上传、下载、删除、权限验证、元数据管理

### 🔄 审批工作流引擎  
- **服务**: `OwWfManager` - 企业级工作流框架
- **功能**: 多级审批、动态审批人、状态跟踪、流程模板配置
- **已集成**: 日常费用申请、市场计划审批等

### 🔐 权限管理系统
- **服务**: `AuthorizationManager` - 细粒度权限控制
- **功能**: 数百个权限节点、多租户数据隔离、基于角色的访问控制

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
