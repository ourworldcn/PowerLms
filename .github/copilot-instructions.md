# 企业级 .NET 6 项目提示词典（高密度分组）

## 语言与资料
- 答复用简体中文，技术词可保留英文
- 必要时限量引用权威英文技术资料

## 编程规范

### 推荐
- .NET 6与C# 10新特性优先用于提升代码简洁性和清晰度，如模式匹配、顶级语句、全局using、表达式成员、局部函数、解构等
- 风格统一，命名规范
- 代码文件中不要用emoji或表情符号，文档中推荐使用

### 限制
- EF Core实体禁止用record类型
- async/await仅用于真实I/O异步，避免影响线程池
- 新特性需兼容现有架构，采用前评估风险
- 本地函数集中文件末尾#region分组

## 架构与职责
- 分层：API→Server(Manager)→Data→基础设施
- 业务逻辑归Manager类，控制器只做校验和异常处理
- 命名清晰，职责分明

## 错误处理
- Manager抛业务异常，控制器统一捕获并返回标准响应
- 重要操作记录异常和日志
- 参数需校验，文件操作用正确编码
- 工具函数失败需有备用方案

## 代码质量
- 优先复用，减少重复，结构可维护
- 复杂逻辑有注释
- 头部注释模板见下方

## 工具与操作
- 不用生成数据库迁移脚本，我会手动迁移
- 文件审批等优先复用已有基础设施组件

## 数据安全与多租户
- 多租户数据隔离和权限校验，批量操作优化
- 敏感数据安全处理

## 变更与重构
- 变更前评估影响，基础库无回退机制
- 重要变更需专家评审
- 大重构分步验证

## 外部集成
- 对接外部系统或服务应使用专属接口或集成组件

## 代码参考
- 查找现有代码和文档，风格统一，优先复用

## 线程与异步
- 仅用.NET 6标准async/await，不用自定义线程调度/同步术语
- 异步实现需兼容现有线程和锁机制

## 🛠️ PowerShell终端规范

> PowerShell 与 Bash 语法完全不同，禁止混用。严格用 PowerShell 语法。不确定时查 Get-Help 或官方文档。

---

## 以下内容仅供人工阅读，AI 可跳过本节
### 禁止项（强制）
- 禁用 `&&`、`||`（Bash语法）
- 禁用Bash/CMD语法和命令（如`ls`、`cat`、`grep`、`export`、`source`）
- 禁用curl别名，仅用`curl.exe`
- 路径仅用正斜杠`/`或双反斜杠`\\`
- 脚本扩展名必须`.ps1`
- Bash到PowerShell需完全重写，不得抄用Bash语法

### 正确范式
```powershell
# 命令分步或用 `;`
cd MyApp
dotnet build
dotnet run

# 条件/错误处理
if (Test-Path $path) { dotnet build }
try { dotnet build; if ($LASTEXITCODE -eq 0) { dotnet run } } catch { Write-Error $_ }

# 环境变量
$env:ASPNETCORE_ENVIRONMENT = "Development"
Write-Output $env:PATH

# 脚本参数
param([string]$Project)

# 查找/处理文本
Get-Content .\log.txt | Select-String "error"
Get-Process | Where-Object { $_.CPU -gt 100 }
```

### 进程/端口/构建
```powershell
Stop-Process -Name "MyApp" -Force -ErrorAction SilentlyContinue
netstat -ano | Select-String ":5000"
dotnet clean; dotnet restore; dotnet build
```

### 常见错误与诊断
- `&&不是有效语句分隔符` → 用`;`或分行
- `文件被锁定` → Stop-Process
- `端口已被使用` → netstat查占用PID
- `找不到术语` → 检查拼写，必要时加`.exe`
- `命令不生效/参数异常` → 检查是否PowerShell写法
- `脚本无法运行` → 检查扩展名和编码（UTF-8 BOM）

### 建议
- 用`Get-Help 命令名`查官方用法
- 脚本保存为UTF-8 BOM编码
- Bash脚本迁移需彻底重写为PowerShell风格
- 如遇新语法不确定，先查询官方文档或写注释说明

## 以下内容 AI 必须阅读
## 头部注释模板

```
/*
 * 项目：[项目/服务]
 * 模块：[模块/功能]
 * 文件说明：
 * - 功能1：[简述]
 * - 功能2：[简述]
 * 技术要点：
 * - [依赖注入/性能优化等]
 * 作者：zc
 * 创建：[YYYY-MM]
 * 修改：[YYYY-MM-DD] [简述]
 */
```