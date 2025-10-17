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

### 📁 实际解决方案结构
```
PowerLms解决方案
├── PowerLmsWebApi/          # 84个文件 - API控制器层
│   ├── Controllers/         # 分模块控制器
│   │   ├── Business/        # 业务模块(海运/空运/工作号)
│   │   ├── Customer/        # 客户资料管理
│   │   ├── Financial/       # 财务管理(费用/结算/金蝶)
│   │   ├── OA/             # 办公自动化
│   │   ├── System/         # 系统管理
│   │   ├── Tax/            # 税务发票
│   │   └── BaseData/       # 基础数据
│   └── Middleware/         # 异常处理中间件
├── PowerLmsServer/         # 40个文件 - 业务逻辑层
│   ├── Managers/           # 业务管理器
│   │   ├── Auth/          # 权限认证
│   │   ├── Business/      # 业务逻辑
│   │   ├── Financial/     # 财务管理
│   │   ├── Integration/   # 外部集成
│   │   └── System/        # 系统服务
│   └── Services/          # 通用服务
├── PowerLmsData/          # 274个文件 - 数据访问层
│   ├── Migrations/        # 200+迁移文件
│   ├── 业务/              # 业务实体
│   ├── 财务/              # 财务实体
│   ├── 客户资料/          # 客户实体
│   ├── 机构/              # 组织架构
│   └── 权限/              # 权限系统
└── 基础设施 (../Bak/)     # 核心组件依赖
    ├── OwDbBase/          # 数据库基础组件
    └── OwBaseCore/        # 核心基础组件
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

## 📋 当前开发状态 (2025年2月)

### 🔴 紧急Bug修复 (最高优先级) - 目标：2月6日完成

#### 1. 申请单移除费用后已申请金额未恢复 🔥
- **问题**: 费用从申请单移除后，`TotalAppliedAmount`字段未正确回写，导致费用无法再次添加
- **影响**: 阻塞费用申请流程，影响财务业务正常运转
- **预估**: 0.5天
- **文件**: `FinancialController.DocFeeRequisition.cs` (AddDocFeeRequisitionItem/RemoveDocFeeRequisitionItem)

#### 2. 通用进口工作号409错误 🔥
- **问题**: 新增工作号时接口返回409冲突错误，但数据实际已保存
- **影响**: 用户体验差，误导用户认为保存失败
- **预估**: 0.5天
- **技术**: 检查重复性校验逻辑

#### 3. 业务结算单商户隔离 🔥
- **问题**: 切换公司登录后，结算单数据未按OrgID隔离
- **影响**: 严重的多租户数据隔离问题
- **预估**: 1天
- **方案**: 结算单主表增加OrgID冗余字段

#### 4. 权限缓存加载异常 ⚠️
- **问题**: 用户登录后放置一天，权限显示不正确，需重启IIS恢复
- **影响**: 基础代码问题，影响系统稳定性
- **预估**: 2天（复杂度高）
- **技术**: 排查权限缓存的加载和过期机制

### 🟡 功能增强任务 (中优先级) - 计划2月14日完成

#### 1. OA申请单公司字段验证
- **需求**: 验证OA费用申请单的`CustomerId`字段集成
- **状态**: CustomerId字段已存在，需确认数据库迁移和前端对接
- **预估**: 0.5天

#### 2. 空运接口架构重复修正
- **问题**: `PlJobController.EaDoc.cs`与`PlAirborneController`功能重复
- **影响**: 维护成本高，容易产生路由冲突
- **预估**: 1天
- **方案**: 统一到PlAirborneController，保持架构一致

#### 3. 费用列表申请单详情接口
- **需求**: 点击"已申请金额"显示该费用在哪些申请单中被引用
- **接口**: `GET /api/Financial/GetFeeRequisitionDetails?feeId={id}`
- **预估**: 1天

#### 4. 客户资料有效性管理
- **需求**: 增加`IsActive`字段，实现客户软删除/停用功能
- **接口**: `POST /api/Customer/ToggleActiveStatus`
- **预估**: 1.5天

#### 5. 客户选择器查询优化
- **需求**: 弹窗式选择器，支持多维度搜索、分页、排序
- **接口**: `GET /api/Customer/GetCustomersForSelector`
- **预估**: 1天

#### 6. 查看所有申请单权限
- **需求**: 财务角色查看所有申请单，不受申请人限制
- **状态**: 权限已添加，需后端接口支持
- **预估**: 0.5天

#### 7. 科目设置快捷输入
- **需求**: "凭证字"和"核算类别"字段支持下拉选择
- **方案**: 类似银行账号快捷输入，数据源来自基础字典
- **预估**: 0.5天（后端部分）

### 🟠 金蝶导出重构 (待规则文件) - 预估5天

#### 金蝶凭证分录逻辑重构
- **背景**: 当前逻辑无法处理第三方付款和复杂费用拆分场景
- **核心变更**:
  - **银行分录拆分**: 多笔收付款独立分录，科目从银行账户动态获取
  - **应收/应付拆分**: 按国外客户、国内客户、关税三种情况拆分
- **实施要求**: 数据驱动设计，引入$(实体名.字段名)变量替换机制
- **当前状态**: 等待永昌石完成Excel规则文件
- **下一步**: 专题会议评审规则文件后开发

### ✅ 最近完成功能 (2025年1-2月)

#### 性能优化与架构改进
- **申请单明细已结算金额优化** ✅ - 直接使用TotalSettledAmount字段，移除动态计算
- **商户实体结构优化** ✅ - 地址属性展平重构，提升访问性能
- **商户查询功能增强** ✅ - 支持通用查询条件，与系统接口统一
- **OrgManager缓存优化** ✅ - 修复缓存失效机制，解决组织数据更新问题

#### 金蝶导出功能
- **收款结算单导出金蝶** ✅ - 七种凭证分录规则，多币种处理，混合业务识别
- **付款结算单导出金蝶** ✅ - 六种凭证分录规则，多笔付款优先，手续费双分录自平衡

#### 基础数据与系统功能
- **基础数据导入导出扩展** ✅ - 新增JobNumberRule、OtherNumberRule、SubjectConfiguration、DailyFeesType支持
- **通用数据查询接口** ✅ - 支持多实体类型字段查询，灵活去重控制
- **申请单审批回退机制** ✅ - 完整的工作流清理+状态回退+权限控制
- **账期管理机制** ✅ - 机构参数表+批量关闭+自动递增

#### Bug修复
- **日常费用种类重复记录** ✅ - 修复同步到子机构时创建重复记录的问题
- **字典导出键重复错误** ✅ - 实现安全字典构建机制
- **OtherNumberRule导入导出** ✅ - 修复Comment注释缺失导致的识别失败

### 🏗️ 架构完成状态

| 层级 | 完成状态 | 主要特征 |
|------|----------|----------|
| **API层** | ✅ 84文件完成 | 分模块控制器+统一异常处理+Swagger文档 |
| **业务层** | ✅ 40文件完成 | Manager模式+基础设施集成+权限验证 |
| **数据层** | ✅ 274文件完成 | EF Core实体+200+迁移+触发器支持 |
| **基础设施** | ✅ 核心组件完备 | 文件/工作流/权限/消息/Excel处理全覆盖 |

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

### 通用数据查询接口使用
```csharp
// 支持的查询类型
GET /api/CommonDataQuery/QueryData
参数：
- TableName: "OaExpenseRequisitions" | "DocFeeRequisitions"
- FieldName: "ReceivingBank" | "ReceivingAccountName" | "BlanceAccountNo"
- IsDistinct: true/false（是否去重）
- MaxResults: 50（默认）| 最大200

// 返回字段值列表，按字母排序
```

### 财务导出金蝶接口
```csharp
// 收款结算单导出
POST /api/FinancialSystemExport/ExportSettlementReceipt
参数：ExportSettlementReceiptParamsDto
- ExportConditions: 查询条件（日期、币种、金额范围等）
- ExportFormat: "DBF"（默认）
- DisplayName: 显示名称（可选）
- Remark: 备注信息（可选）

// 付款结算单导出
POST /api/FinancialSystemExport/ExportSettlementPayment
参数：ExportSettlementPaymentParamsDto
- 参数结构同收款结算单

// 返回异步任务ID和预期处理数量
```

### 开发检查清单
```markdown
✅ 新功能开发必检项
□ 文件操作 → OwFileService | □ 审批流程 → OwWfManager | □ 权限控制 → AuthorizationManager
□ 组织管理 → OrgManager | □ 系统配置 → DataDicManager | □ 消息通知 → OwMessageManager
□ Excel处理 → OwDataUnit + OwNpoiUnit | □ 分次收付 → ActualFinancialTransaction
□ 权限验证 | □ 多租户隔离 | □ 向后兼容 | □ 单元测试
```

## 🎯 当前开发计划

### 第一阶段：紧急Bug修复 (2月3-6日)
**目标：解决所有阻塞性Bug，确保系统正常运转**
1. 申请单移除费用金额回写问题 (0.5天) 🔥
2. 通用进口工作号409错误 (0.5天) 🔥
3. 业务结算单商户隔离 (1天) 🔥
4. 权限缓存加载异常 (2天) ⚠️

### 第二阶段：功能增强 (2月7-14日)
**目标：完成中优先级功能，提升系统易用性**
1. OA申请单公司字段验证 (0.5天)
2. 空运接口架构重复修正 (1天)
3. 费用列表申请单详情接口 (1天)
4. 客户资料有效性管理 (1.5天)
5. 客户选择器查询优化 (1天)
6. 查看所有申请单权限 (0.5天)
7. 科目设置快捷输入 (0.5天)

### 第三阶段：金蝶导出重构 (待规则文件确认)
**目标：实现数据驱动的复杂凭证分录生成**
- 等待永昌石完成Excel规则文件编写
- 召开专题会议评审规则文件
- 实施重构（预估5天）

### 风险评估
- **中等风险** - 权限缓存问题复杂度较高，可能需要更多时间
- **低风险** - 其他Bug和功能需求范围明确，技术方案清晰
- **待定风险** - 金蝶重构依赖规则文件质量，需充分评审

---

**PowerLms** - 专业的货运物流业务管理系统  
*3人精英团队 | 31.7万行企业级代码 | 基础设施完备复用*  
*当前重点：解决阻塞性Bug | 完成功能增强 | 准备金蝶导出重构*
