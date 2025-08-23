# 企业级 .NET 6 项目开发规范

<!-- 以下是AI必须遵循的核心规范 -->

## 基础设定
- 答复用简体中文，技术词保留英文
- 基于 .NET 6 与 C# 10，优先使用新特性提升代码简洁性
- 架构分层：API→Server(Manager)→Data→基础设施
- 业务逻辑归Manager类，控制器只做校验和异常处理
- 修改代码后，请务必检查并确认代码整体能成功编译
- 查找现有代码保持风格统一
- 复用已有基础设施组件
- 不生成数据库迁移脚本（手动迁移）
- 输入容错:接受无标点或语音识别同音字错误，AI需自行理解。

## 编程约束
- **禁止项**：EF Core实体用record类型；非真实I/O场景滥用async/await
- **必需项**：风格统一，命名规范；新特性需兼容现有架构；本地函数集中文件末尾用#region分组
- **#region规范**：所有 `#region [标题]` 必须有对应的 `#endregion [标题]`，嵌套需正确缩进
- 函数体内避免空行。

## 质量与安全
- 优先复用，减少重复；Manager抛业务异常，控制器统一处理
- 多租户数据隔离和权限校验；敏感数据安全处理
- 重要操作记录异常和日志；变更前评估影响，基础库无回退机制

## PowerShell终端规范

> **PowerShell 与 Bash 语法完全不同，禁止混用,请严格使用 PowerShell 5.1 版本支持的命令与语法。不确定时查 Get-Help 或官方文档。注意避免输出时出现分页器悬挂问题。**
> **强制设置输出编码为 UTF-8，以避免乱码。**
> **curl 重定向处理：curl 命令调用 GET 方法时默认加 -L 参数自动跟随重定向。**
> **开始时用 Import-Module ImportExcel 加载模块，优先用它处理 Excel 文件，禁止使用内置工具。**

## JSON 文件处理规范
> **优先用 PowerShell 原生命令，处理 JSON 文件，对100KB以上的大型JSON文件，要避免直接读写带来可能超过上下文限制的问题。**

## Excel 文件处理规范
> **优先使用 ImportExcel 模块处理 Excel 文件，禁止使用内置的 Excel COM 对象。对于大文件或复杂操作，建议分批处理以避免内存溢出和超过上下文限制的问题。**

## 变更管理与文档
- **文档分离原则**：任务管理(TODO.md)和变更记录(CHANGELOG.md)分开管理，职责清晰
- **TODO.md结构要求**：专注当前待办任务、优先级和执行计划，包含详细的技术方案和剩余工作
- **CHANGELOG.md结构要求**：开头必须有功能变更总览，使用文本格式(非表格)便于复制分享，简要的记录所有已完成的变更历史
- **变更记录要求**：每次重要变更必须更新CHANGELOG.md文件，记录变更概要、技术细节和影响范围
- **架构重构记录**：重大架构调整需要详细记录变更前后对比，便于团队理解和维护
- **已完成任务规范**：对已完成的任务，不要列出具体代码，重点说明功能变更和影响范围
- 无明确指令时，不要新建文件，更改报告可合并入 CHANGELOG.md

## 头部注释模板
```
/*
 * 项目：[项目/服务] | 模块：[模块/功能]
 * 功能：[主要功能简述]
 * 技术要点：[依赖注入/性能优化等]
 * 作者：zc | 创建：[YYYY-MM] | 修改：[YYYY-MM-DD] [简述]
 */
```

<!-- 以下PowerShell和curl详细示例仅供人工参考，AI可跳过 -->

### **curl 重定向处理详细示例**（人工参考用）

#### **重定向检测方法**
```powershell
# 方法1: 查看响应头，检查状态码和Location
curl.exe -I "http://example.com/redirect"

# 方法2: 使用格式化输出检测重定向
curl.exe -w "%{http_code} %{redirect_url}\n" -o nul "http://example.com"

# 方法3: 显示详细信息包括重定向过程
curl.exe -v "http://example.com"
```

#### **重定向跟随策略**
```powershell
# 手动处理: 不跟随重定向，检查响应
curl.exe "http://example.com/redirect"  # 返回302和HTML重定向页面

# 自动跟随: 添加-L参数自动处理重定向（默认推荐）
curl.exe -L "http://example.com/redirect"  # 直接获取最终页面内容

# 限制重定向次数: 防止无限重定向
curl.exe -L --max-redirs 5 "http://example.com"
```

#### **实际应用示例**
```powershell
# 检查API端点是否有重定向
curl.exe -I "http://localhost:5000/api/Admin"

# 标准API调用（必须使用-L）
curl.exe -L -X POST "http://localhost:5000/api/Admin/ProcessFile" -H "Content-Type: application/json" -d "{\"test\":\"data\"}"

# 在脚本中处理重定向
$response = curl.exe -L -w "%{http_code}" -o response.json "http://localhost:5000/api"
if ($response -match "^3\d\d$") {
    Write-Host "检测到重定向，状态码: $response"
}
```

### **PowerShell语法要点**（人工参考用）

**禁止项**：`&&`、`||`等Bash语法；`ls`、`cat`、`grep`等Bash命令；路径只能用`\`或`\\`

**正确写法**：
- 命令分步或用`;`：`cd MyApp; dotnet build; dotnet run`
- 条件处理：`if (Test-Path $path) { dotnet build }`
- 环境变量：`$env:ASPNETCORE_ENVIRONMENT = "Development"`
- 进程管理：`Stop-Process -Name "MyApp" -Force -ErrorAction SilentlyContinue`

**Git操作最佳实践**：
- 避免分页器悬挂：`git --no-pager log --oneline -10`
- 查看变更文件：`git --no-pager show [commit] --name-only`
- 查看差异：`git --no-pager diff HEAD~1..HEAD`
- **悬挂诊断**：输出末尾出现 `:` 表示进入分页模式，使用 `--no-pager` 参数解决

**诊断方案**：
- `&&不是有效语句分隔符` → 用`;`或分行
- `文件被锁定` → Stop-Process
- `端口已被使用` → netstat查占用PID
- `找不到术语` → 检查拼写，必要时加`.exe`
- `git命令悬挂` → 使用 `--no-pager` 参数禁用分页器

<!-- PowerShell终端规范结束 -->