# 📋 PowerLms 待办任务清单

## 1. 🚀 紧急任务（本周交付）

### 1.1 主单领用登记模块——服务器端实现

#### 1.1.1 数据层（PowerLmsData）
- [x] **1.1.1.1 创建PlEaMawbInbound实体（空运出口主单领入表）** ✅
  - 位置：`PowerLmsData/主营业务/空运出口/PlEaMawbInbound.cs`
  - 字段：Id、OrgId、MawbNo（标准主单号）、MawbNoDisplay（显示主单号）、SourceType（来源类型:0航司登记/1过单代理）、AirlineId（航空公司，不建FK约束）、TransferAgentId（过单代理，不建FK约束）、RegisterDate（登记日期）、Remark（备注）、CreateBy、CreateDateTime
  - 索引：MawbNo索引、OrgId索引
  - 继承：GuidKeyObjectBase, ICreatorInfo
  - 注释：完整的XML文档注释和EF Comment特性

- [x] **1.1.1.2 创建PlEaMawbOutbound实体（空运出口主单领出表）** ✅
  - 位置：`PowerLmsData/主营业务/空运出口/PlEaMawbOutbound.cs`
  - 字段：Id、OrgId、MawbNo（标准主单号）、AgentId（领单代理，不建FK约束）、RecipientName（领用人）、IssueDate（领用日期）、PlannedReturnDate（预计返回日期）、ActualReturnDate（实际返回日期）、Remark（备注）、CreateBy、CreateDateTime
  - 索引：MawbNo索引、OrgId索引
  - 继承：GuidKeyObjectBase, ICreatorInfo
  - 注释：完整的XML文档注释和EF Comment特性

- [x] **1.1.1.3 更新PowerLmsContext DbSet配置** ✅
  - 添加：DbSet<PlEaMawbInbound>、DbSet<PlEaMawbOutbound>
  - 不配置外键关系（保持灵活性）
  - 优化区域组织：按业务类型分区（空运出口、空运进口、海运出口、海运进口）
  - 编译验证：通过 ✅

- [x] **1.1.1.4 优化项目文件夹结构** ✅
- 重命名：`业务/` → `主营业务/`
- 创建完整业务子文件夹：`空运出口/`、`空运进口/`、`海运出口/`、`海运进口/`
- 文件归类：
  - PlEaDoc.cs → `主营业务/空运出口/`
  - PlIaDoc.cs → `主营业务/空运进口/`
  - PlEsDoc.cs → `主营业务/海运出口/`
  - PlIsDoc.cs → `主营业务/海运进口/`
  - ContainerKindCount.cs → `主营业务/海运出口/`（独立文件）
- 更新README、CHANGELOG：完整项目结构说明
- 优化DbContext：按四大业务类型分区

- [ ] **1.1.1.5 手动创建数据库迁移**
  - 执行：`Add-Migration AddEaMawbTables`
  - 检查生成的迁移文件
  - 应用迁移：`Update-Database`

#### 1.1.2 业务层（PowerLmsServer）
- [x] **1.1.2.1 创建MawbManager类** ✅
  - 位置：`PowerLmsServer/Managers/Business/MawbManager.cs`
  - 依赖注入：PowerLmsContext、AuthorizationManager、OrgManager、AccountManager、ILogger
  - 编译验证：通过 ✅

**主单号校验与生成模块**
- [x] **1.1.2.2 实现ValidateMawbNo方法** ✅
  - 功能：校验主单号格式（3位前缀+"-"+8位数字）和校验位（前7位减第8位对7取模=0）
  - 输入：string mawbNo
  - 输出：(bool isValid, string errorMsg)
  - 兼容：输入可含空格，内部标准化处理

- [x] **1.1.2.3 实现GenerateNextMawbNo方法** ✅
  - 功能：根据当前主单号生成下一个主单号
  - 输入：string prefix（3位前缀）, string currentNo（当前8位数字）
  - 输出：string nextMawbNo
  - 算法：前7位+1并重算校验位，左侧补零

- [x] **1.1.2.4 实现BatchGenerateMawbNos方法** ✅
  - 功能：批量生成主单号序列
  - 输入：string prefix, string startNo, int count
  - 输出：List<string> mawbNos
  - 校验：起始号合法性验证

**主单领入模块**
- [x] **1.1.2.5 实现CreateInbound方法** ✅
  - 功能：批量创建领入记录
  - 输入：sourceType, airlineId, transferAgentId, registerDate, remark, mawbNos[], orgId, createBy
  - 业务规则：
    - 权限验证：D0.14.1（新建登记）
    - 多租户隔离：OrgId过滤
    - 主单号唯一性校验
    - 双字段存储：MawbNo（标准化）+ MawbNoDisplay（保留原格式）
  - 输出：(successCount, failureCount, failureDetails)

- [ ] **1.1.2.6 实现GetInboundList方法**
  - 功能：查询领入列表
  - 输入：筛选参数（OrgId、AirlineIds（多选OR）、TransferAgentIds（多选OR）、DateRange、MawbNo）
  - 权限：D0.14.2（查看登记）
  - 分页：支持PageIndex、PageSize
  - 输出：PagedResult<PlMawbInbound>

- [x] **1.1.2.7 实现UpdateInbound方法** ✅
  - 功能：修改领入记录
  - 输入：id, airlineId, transferAgentId, registerDate, remark, orgId
  - 权限：D0.14.3（编辑登记）
  - 业务规则：禁止修改MawbNo，可修改AirlineId、TransferAgentId、Remark等
  - 输出：bool success

- [x] **1.1.2.8 实现DeleteInbound方法** ✅
  - 功能：删除领入记录
  - 输入：id, orgId
  - 权限：D0.14.4（删除登记）
  - 业务规则：
    - 检查是否已领出
    - 事务删除PlEaMawbInbound记录
  - 输出：(success, errorMsg)

**主单领出模块**
- [x] **1.1.2.9 实现CreateOutbound方法** ✅
  - 功能：单张主单领出登记
  - 输入：mawbNo, agentId, recipientName, issueDate, plannedReturnDate, remark, orgId, createBy
  - 权限：D0.14.5（创建领用）
  - 业务规则：
    - 检查主单号是否已领入（存在PlEaMawbInbound记录）
    - 检查是否已领出（不能重复领出）
    - 使用标准化主单号（MawbNo字段）
  - 输出：(success, errorMsg, id)

- [ ] **1.1.2.10 实现GetOutboundList方法**
  - 功能：查询领出列表
  - 输入：筛选参数（OrgId、AgentIds（多选OR）、DateRange、MawbNo）
  - 权限：D0.14.6（查看领用）
  - 分页：支持
  - 输出：PagedResult<PlMawbOutbound>

- [x] **1.1.2.11 实现UpdateOutbound方法** ✅
  - 功能：修改领出记录
  - 输入：id, agentId, recipientName, issueDate, plannedReturnDate, actualReturnDate, remark, orgId
  - 权限：D0.14.7（编辑领用）
  - 业务规则：可修改AgentId、RecipientName、返回日期等
  - 输出：bool success

- [x] **1.1.2.12 实现DeleteOutbound方法** ✅
  - 功能：删除领出记录
  - 输入：id, orgId
  - 权限：D0.14.8（删除领用）
  - 输出：(success, errorMsg)

**台账管理模块**
- [ ] **1.1.2.13 实现GetLedgerList方法**
  - 功能：查询台账列表（含业务回查）
  - 输入：筛选参数（OrgId、UseStatus、MawbNo）
  - 权限：D0.14.2（查看登记，复用）
  - 关联查询：
    - Join PlMawbInbound获取领入信息
    - Join PlMawbOutbound获取领出信息
    - 根据MawbNo关联业务单据（PlJob/DocAirExport）获取件数/重量/体积/计费重量（只读显示，不写回）
  - 输出：PagedResult<MawbLedgerDto>（包含领入/领出/业务信息）

- [ ] **1.1.2.14 实现GetUnusedMawbList方法**
  - 功能：获取未使用主单列表（供业务单据选择）
  - 输入：OrgId
  - 权限：D0.14.2（查看登记，复用）
  - 筛选：UseStatus=0（未使用）
  - 输出：List<MawbLedgerDto>

- [ ] **1.1.2.15 实现MarkAsUsed方法**
  - 功能：标记主单已使用
  - 输入：string mawbNo, Guid jobId
  - 业务规则：
    - 更新PlMawbLedger.UseStatus=1
    - 可选：记录关联的JobId（需评估是否新增字段）
  - 调用时机：业务单据保存时选择主单号后调用

- [ ] **1.1.2.16 实现MarkAsVoid方法**
  - 功能：作废主单
  - 输入：string mawbNo, string reason
  - 权限：D0.14.3（编辑登记，复用）
  - 业务规则：
    - 检查是否已使用
    - 更新UseStatus=2
    - 记录作废原因到Remark
  - 输出：ActionResult

**业务关联模块**
- [ ] **1.1.2.17 实现GetJobInfoByMawbNo方法**
  - 功能：根据主单号查询委托信息
  - 输入：string mawbNo
  - 关联查询：PlJob、DocAirExport
  - 输出：JobInfoDto（件数、重量、体积、计费重量等）

#### 1.1.3 API层（PowerLmsWebApi）
- [x] **1.1.3.1 创建MawbController类** ✅
  - 位置：`PowerLmsWebApi/Controllers/Business/MawbController.cs`
  - 路由前缀：`/api/Mawb`
  - 依赖注入：MawbManager
  - DTO文件：`PowerLmsWebApi/Controllers/Business/MawbController.Dto.cs`
  - 编译验证：通过 ✅

**主单号工具接口**
- [x] **1.1.3.2 POST /ValidateMawbNo** ✅
  - 功能：校验主单号
  - 请求：ValidateMawbNoParamsDto { Token, MawbNo }
  - 响应：ValidateMawbNoReturnDto { IsValid, ErrorMsg }
  - 权限：无（工具方法）
  - 状态：接口已创建

- [x] **1.1.3.3 POST /GenerateNextMawbNo** ✅
  - 功能：生成下一个主单号
  - 请求：GenerateNextMawbNoParamsDto { Token, Prefix, CurrentNo }
  - 响应：GenerateNextMawbNoReturnDto { NextMawbNo }
  - 权限：无（工具方法）
  - 状态：接口已创建

- [x] **1.1.3.4 POST /BatchGenerateMawbNos** ✅
  - 功能：批量生成主单号
  - 请求：BatchGenerateMawbNosParamsDto { Token, Prefix, StartNo, Count }
  - 响应：BatchGenerateMawbNosReturnDto { MawbNos[] }
  - 权限：无（工具方法）
  - 状态：接口已创建

**主单领入接口**
- [x] **1.1.3.5 GET /GetInboundList** ✅
  - 功能：查询领入列表
  - 请求：PagingParamsDtoBase + conditional（支持OrgId、AirlineId、TransferAgentId、MawbNo等）
  - 权限：D0.14.2（查看登记）
  - 响应：GetAllMawbInboundReturnDto
  - 状态：接口框架已创建

- [ ] **1.1.3.6 POST /CreateInbound**
  - 功能：批量创建领入记录
  - 请求：CreateMawbInboundParamsDto（含MawbNos数组）
  - 权限：D0.14.1（新建登记）
  - 响应：CreateMawbInboundReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

- [ ] **1.1.3.7 PUT /UpdateInbound**
  - 功能：修改领入记录
  - 请求：UpdateMawbInboundParamsDto
  - 权限：D0.14.3（编辑登记）
  - 响应：UpdateMawbInboundReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

- [ ] **1.1.3.8 DELETE /DeleteInbound**
  - 功能：删除领入记录
  - 请求：DeleteMawbInboundParamsDto
  - 权限：D0.14.4（删除登记）
  - 响应：DeleteMawbInboundReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

**主单领出接口**
- [x] **1.1.3.9 GET /GetOutboundList** ✅
  - 功能：查询领出列表
  - 请求：PagingParamsDtoBase + conditional（支持OrgId、AgentId、MawbNo、IssueDate等）
  - 权限：D0.14.6（查看领用）
  - 响应：GetAllMawbOutboundReturnDto
  - 状态：接口框架已创建

- [ ] **1.1.3.10 POST /CreateOutbound**
  - 功能：创建领出记录
  - 请求：CreateMawbOutboundParamsDto
  - 权限：D0.14.5（创建领用）
  - 响应：CreateMawbOutboundReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

- [ ] **1.1.3.11 PUT /UpdateOutbound**
  - 功能：修改领出记录
  - 请求：UpdateMawbOutboundParamsDto
  - 权限：D0.14.7（编辑领用）
  - 响应：UpdateMawbOutboundReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

- [ ] **1.1.3.12 DELETE /DeleteOutbound**
  - 功能：删除领出记录
  - 请求：DeleteMawbOutboundParamsDto
  - 权限：D0.14.8（删除领用）
  - 响应：DeleteMawbOutboundReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

**台账查询接口**
- [ ] **1.1.3.13 GET /GetLedgerList**
  - 功能：查询台账列表（含业务回查）
  - 请求：PagingParamsDtoBase + conditional（支持OrgId、UseStatus、MawbNo等）
  - 权限：D0.14.2（查看登记，复用）
  - 响应：GetMawbLedgerListReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

- [ ] **1.1.3.14 GET /GetUnusedMawbList**
  - 功能：获取未使用主单列表
  - 请求：TokenDtoBase
  - 权限：D0.14.2（查看登记，复用）
  - 响应：GetUnusedMawbListReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

- [ ] **1.1.3.15 POST /MarkAsVoid**
  - 功能：作废主单
  - 请求：MarkMawbAsVoidParamsDto { Token, MawbNo, Reason }
  - 权限：D0.14.3（编辑登记，复用）
  - 响应：MarkMawbAsVoidReturnDto
  - 状态：接口框架已创建，业务逻辑待实现

**业务关联接口**
- [ ] **1.1.3.16 GET /GetJobInfo/{mawbNo}**
  - 功能：根据主单号查询委托信息
  - 请求：路径参数mawbNo + 查询参数token
  - 响应：GetJobInfoByMawbNoReturnDto { JobInfoDto }
  - 状态：接口框架已创建，业务逻辑待实现

#### 1.1.4 权限配置
- [ ] **1.1.4.1 使用现有权限节点（权限.md）**
  - **父节点**: D0.14（主单领用登记）
  - **叶子权限**（8项，只有叶子节点在服务器端使用）:
    - D0.14.1（新建登记）- 对应主单领入Create
    - D0.14.2（查看登记）- 对应主单领入Read
    - D0.14.3（编辑登记）- 对应主单领入Update
    - D0.14.4（删除登记）- 对应主单领入Delete
    - D0.14.5（创建领用）- 对应主单领出Create
    - D0.14.6（查看领用）- 对应主单领出Read
    - D0.14.7（编辑领用）- 对应主单领出Update
    - D0.14.8（删除领用）- 对应主单领出Delete

- [ ] **1.1.4.2 权限码映射到Manager方法**
  - CreateInbound: D0.14.1
  - GetInboundList: D0.14.2
  - UpdateInbound: D0.14.3
  - DeleteInbound: D0.14.4
  - CreateOutbound: D0.14.5
  - GetOutboundList: D0.14.6
  - UpdateOutbound: D0.14.7
  - DeleteOutbound: D0.14.8
  - GetLedgerList/GetUnusedMawbList: D0.14.2（复用查看登记权限）
  - MarkAsVoid: D0.14.3（复用编辑登记权限）

- [ ] **1.1.4.3 预设角色配置建议**
  - 操作员：D0.14.1/D0.14.2/D0.14.5/D0.14.6（创建和查看）
  - 主管：全部8项权限
  - 只读：D0.14.2/D0.14.6（仅查看）

#### 1.1.5 DTO设计
- [x] **1.1.5.1 完整DTO文件创建** ✅
  - 位置：`PowerLmsWebApi/Controllers/Business/MawbController.Dto.cs`
  - 包含所有接口的请求/响应DTO
  - 主单号工具方法DTO（6个）
  - 主单领入相关DTO（8个）
  - 主单领出相关DTO（8个）
  - 台账查询相关DTO（5个）
  - 业务关联相关DTO（2个）
  - 编译验证：通过 ✅

#### 1.1.6 业务单据集成
- [ ] **1.1.6.1 修改DocAirExport实体**
  - 新增字段：MawbNo（string(20)，主单号）
  - 索引：MawbNo索引

- [ ] **1.1.6.2 修改空运出口业务保存逻辑**
  - 选择主单号后调用：MawbManager.MarkAsUsed(mawbNo, jobId)
  - 更新DocAirExport.MawbNo字段

#### 1.1.7 测试与验证
- [ ] **1.1.7.1 单元测试**
  - 主单号校验算法测试
  - 批量生成测试
  - 领入/领出业务规则测试

- [ ] **1.1.7.2 集成测试**
  - 完整流程：领入→领出→业务使用→台账查询
  - 权限验证测试
  - 多租户隔离测试

- [ ] **1.1.7.3 编译验证**
  - 执行：`dotnet build`
  - 修复所有编译错误

---

## 2. 📅 计划任务（下周）

### 2.1 DateTime字段精度标注优化（大型任务）

#### 📊 任务概览（已完成统计）
- **范围**: PowerLmsData项目所有实体DateTime字段
- **目标**: 统一添加`[Precision(3)]`特性（毫秒级精度）
- **总计**: 107个DateTime字段
- **已完成**: 约10个（主营业务部分已有Precision标注）
- **待处理**: 约97个

#### 📋 各文件夹DateTime字段统计

| 文件夹 | DateTime字段数 | 优先级 | 批次 |
|--------|---------------|--------|------|
| 主营业务 | 44个 | ⭐⭐⭐⭐⭐ | 第一批 |
| 财务 | 22个 | ⭐⭐⭐⭐ | 第二批 |
| 基础数据 | 10个 | ⭐⭐⭐ | 第三批 |
| OA | 9个 | ⭐⭐⭐ | 第三批 |
| 航线管理 | 4个 | ⭐⭐ | 第四批 |
| 客户资料 | 3个 | ⭐⭐⭐⭐ | 第二批 |
| 权限 | 3个 | ⭐⭐ | 第四批 |
| 流程 | 2个 | ⭐⭐ | 第四批 |
| 机构 | 2个 | ⭐⭐⭐ | 第三批 |
| 消息系统 | 2个 | ⭐ | 第四批 |
| 应用日志 | 2个 | ⭐ | 第四批 |
| 账号 | 2个 | ⭐⭐ | 第四批 |
| 基础支持 | 2个 | ⭐ | 第四批 |

#### 📅 分批执行计划（优化版）

**第一批: 主营业务** (优先级: ⭐⭐⭐⭐⭐)
- [ ] **2.1.1 主营业务DateTime字段标注**
  - 字段数: 44个
  - 包含文件夹: 空运出口、空运进口、海运出口、海运进口、通用实体
  - 预计时间: 40分钟
  - 子任务:
    - 空运出口: PlEaDoc.cs, PlEaMawbInbound.cs, PlEaMawbOutbound.cs (6个字段)
    - 空运进口: PlIaDoc.cs (5个字段)
    - 海运出口: PlEsDoc.cs (7个字段)
    - 海运进口: PlIsDoc.cs (8个字段)
    - 通用: PlJob.cs, DocFee.cs, DocBill.cs (约18个字段)
  
- [ ] **2.1.2 编译验证（第一批）**

**第二批: 财务 + 客户资料** (优先级: ⭐⭐⭐⭐)
- [ ] **2.1.3 财务实体DateTime字段标注**
  - 字段数: 22个
  - 包含: 财务相关实体（ActualFinancialTransaction, PlInvoices, DocFeeRequisition等）
  - 预计时间: 25分钟
  
- [ ] **2.1.4 客户资料DateTime字段标注**
  - 字段数: 3个
  - 包含: PlCustomer及相关实体
  - 预计时间: 5分钟
  
- [ ] **2.1.5 编译验证（第二批）**

**第三批: 基础数据 + OA + 机构** (优先级: ⭐⭐⭐)
- [ ] **2.1.6 基础数据DateTime字段标注**
  - 字段数: 10个
  - 包含: 基础数据字典、汇率等
  - 预计时间: 10分钟
  
- [ ] **2.1.7 OA实体DateTime字段标注**
  - 字段数: 9个
  - 包含: OA日常费用申请等
  - 预计时间: 10分钟
  
- [ ] **2.1.8 机构实体DateTime字段标注**
  - 字段数: 2个
  - 包含: PlOrganization等
  - 预计时间: 3分钟
  
- [ ] **2.1.9 编译验证（第三批）**

**第四批: 其他文件夹** (优先级: ⭐⭐)
- [ ] **2.1.10 其余实体DateTime字段标注**
  - 字段数: 17个（航线4+权限3+流程2+消息2+日志2+账号2+支持2）
  - 包含: 航线管理、权限、流程、消息系统、应用日志、账号、基础支持
  - 预计时间: 15分钟
  
- [ ] **2.1.11 编译验证（第四批）**

**最终验证与迁移**
- [ ] **2.1.12 全项目编译验证**
- [ ] **2.1.13 生成数据库迁移**
  - 执行: `Add-Migration UpdateDateTimePrecision`
  - 检查迁移文件
  - 注意: 此迁移会修改所有DateTime列从datetime2(7)到datetime2(3)
- [ ] **2.1.14 更新文档**
  - 更新CHANGELOG.md
  - 记录变更说明

#### 🎯 修改规范

**标准格式**:
```csharp
/// <summary>
/// 创建时间。
/// </summary>
[Comment("创建时间")]
[Precision(3)]  // 毫秒级精度
public DateTime CreateDateTime { get; set; }
```

**统一原则**:
- 精度: `Precision(3)` - 毫秒级
- 位置: 在`[Comment]`特性之后，DateTime声明之前
- 适用: 所有业务时间字段（包括DateTime和DateTime?）

#### ⚠️ 注意事项
1. ✅ 排除迁移文件、Designer文件、Snapshot文件
2. ✅ 每批修改后执行编译验证
3. ✅ 完成所有修改后统一生成迁移
4. ⚠️ 新迁移会修改数据库列精度定义（107个字段）
5. ⚠️ 数据库升级时间可能较长，建议在维护窗口执行

#### 📊 进度追踪

- [ ] 第一批: 主营业务 (44个字段)
- [ ] 第二批: 财务+客户资料 (25个字段)
- [ ] 第三批: 基础数据+OA+机构 (21个字段)
- [ ] 第四批: 其他文件夹 (17个字段)
- [ ] 最终验证与迁移

**总进度**: 0/107 (0%)

### 2.2 主单/分单/舱单数据模型蓝图初稿
- [ ] **2.1.1 主单制作模块设计**
  - 数据模型：主表+子表结构
  - 字段：抬头（收/发/通知）、代理信息、港口/航班、重量体积计费、运费及附加费、账单状态
  - 接口对接：仓库系统、EasyCargo等

- [ ] **2.1.2 分单制作模块设计**
  - 关系：一主多分（或直单）
  - 业务维度：与货主合同关联

- [ ] **2.1.3 舱单管理模块设计**
  - 功能：汇总主/分单，形成托盘/位置映射
  - 发送：航司系统对接

- [ ] **2.1.4 EDI平台对接预研**
  - 对接方案：易飞瑞特等中间数据中心
  - 数据映射：业务数据→EDI标准格式

---

## 3. 🔄 暂缓任务

（暂无）

---

## 4. ✅ 已完成任务

（待添加）

---

## 5. 📝 技术备忘

### 5.1 主单号校验算法（IATA国际标准）
```
格式：3位航司代码 + "-" + 8位数字（第8位为校验位）
国际标准：连字符"-"位置固定，不能改变

示例：
  - "999-12345678"（标准格式）
  - "999 12345678"（带空格，系统自动处理）
  - "999 1234567 8"（多个空格，系统自动处理）

校验位算法（IATA标准）：(前7位数字 - 第8位数字) % 7 == 0
生成下一个号：
  1. 前7位+1
  2. 新校验位 = 新前7位 % 7
  
示例：
  当前："999-12345670"（1234567 - 0 = 1234567, 1234567 % 7 = 0 ✅）
  下一个：前7位+1 = 1234568, 校验位 = 1234568 % 7 = 1
  结果："999-12345681"（1234568 - 1 = 1234567, 1234567 % 7 = 0 ✅）

存储格式：
  - MawbNo：标准化主单号（去除所有空格，保留连字符，如"999-12345678"）
  - MawbNoDisplay：保留原始输入格式（含空格，如"999 1234567 8"）
```

### 5.2 显示格式处理
- 存储：标准主单号（去空格）
- 显示：MawbNoDisplay字段（保留空格）
- 输入校验：兼容空格输入，内部标准化

### 5.3 开放问题
- [ ] 代理筛选多选OR是否需要新增专用查询接口？
- [ ] 航司3位前缀是否引入校验字典与维护来源？
- [ ] PlMawbLedger是否新增JobId字段记录业务关联？
- [ ] 是否冗余写回UseStatus=1到台账（目前计划通过业务占用推导）？
- [ ] 领入修改功能是否提供（或仅支持删+重建）？

---

**最后更新**：2025-01-17
**负责人**：ZC
**预计完成时间**：本周五（数据层+业务层核心功能）
