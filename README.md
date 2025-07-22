# PowerLms
本助手将始终使用中文进行回复。
PowerLms 是一个专注于货代行业的管理系统（LMS），基于 C# 和 .NET6 技术栈开发。  
本项目旨在为货代企业提供高效、安全、可扩展的学习与培训解决方案。

---

## 项目特性

- 货代行业专属业务流程支持
- 现代化前后端分离架构
- 支持多角色、多组织
- 灵活的课程与考试管理系统
- 数据可视化与统计分析

---

## 快速开始

1. 克隆代码仓库  
   ```bash
   git clone https://github.com/ourworldcn/PowerLms.git
   ```

2. 打开 `PowerLms.sln`，使用 Visual Studio 2022 进行编译和运行。

3. 配置数据库连接等参数（详见 docs/INSTALL.md 或项目内注释）。

---

## 开发文档与规范

### ?? 核心开发文档
- **[代码惯例（CODE_CONVENTIONS）](Docs/CODE_CONVENTIONS.md)**  
  详细的代码规范、架构设计、业务规则实现指南，开发人员必读。

- **[编码规范（CODE_STYLE）](Docs/CODE_STYLE.md)**  
  基础编码风格和命名规范，请所有贡献者务必遵守。

- **[设计偏好指南（DESIGN_PREFERENCE_GUIDE）](Docs/DESIGN_PREFERENCE_GUIDE.md)**  
  了解团队在架构和设计上的主要偏好和原则。

### ?? 其他文档
- 更多技术文档详见 [Docs/](Docs/) 目录
- API文档和接口说明
- 数据库设计文档

---

## 贡献指南

欢迎各位参与贡献！请先阅读相关文档以了解开发规范：

### 开发准备
1. **必读**: [代码惯例](Docs/CODE_CONVENTIONS.md) - 了解项目的完整开发规范
2. **必读**: [编码规范](Docs/CODE_STYLE.md) - 掌握基础编码风格
3. **推荐**: [设计偏好指南](Docs/DESIGN_PREFERENCE_GUIDE.md) - 理解设计原则

### 贡献流程
1. Fork 这个仓库
2. 创建您的特性分支 (`git checkout -b feature/YourFeature`)
3. 遵循代码惯例进行开发
4. 提交您的更改 (`git commit -m 'Add some feature'`)
5. 推送到分支 (`git push origin feature/YourFeature`)
6. 创建一个新的 Pull Request

---

## 技术栈

- **.NET 6**: 核心开发框架
- **Entity Framework Core**: ORM数据访问
- **ASP.NET Core Web API**: RESTful API服务
- **AutoMapper**: 对象映射
- **Microsoft.Extensions.DI**: 依赖注入

---

## License

本项目采用 MIT License，详情见 [LICENSE](LICENSE) 文件。
