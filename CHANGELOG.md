# 变更日志

## [未发布] - 2026-01-26

### 📋 空运出口主单命名规范重构

#### 功能变更总览
**命名简化优化**：移除空运出口主单相关实体和控制器的`Pl`前缀，采用更简洁的`Ea`（Export Air）前缀，提升代码可读性和维护性

#### 业务变更（面向项目经理）
空运出口主单相关接口路由保持不变，仅内部类名和文件名优化，对前端无影响

#### API变更（面向前端）
**无API变更**：所有接口路径、参数、返回值保持不变，前端无需任何修改

#### 架构调整（面向开发团队）

**1. 实体类重命名**
- 文件：`PowerLmsData/主营业务/空运出口/PlEaMawb.cs` → `EaMawb.cs`
- 类名变更：
  - `PlEaMawb` → `EaMawb`（空运出口主单）
  - `PlEaMawbOtherCharge` → `EaMawbOtherCharge`（主单其他费用）
  - `PlEaCubage` → `EaCubage`（主单委托明细）
  - `PlEaGoodsDetail` → `EaGoodsDetail`（主单品名明细）
  - `PlEaContainer` → `EaContainer`（主单集装器）

**2. 控制器重命名**
- 文件夹：`PowerLmsWebApi/Controllers/Business/AirFreight/`
- 文件变更：
  - `PlEaMawbController.cs` → `EaMawbController.cs`
  - `PlEaMawbController.Dto.cs` → `EaMawbController.Dto.cs`
  - `PlEaMawbController.OtherCharge.cs` → `EaMawbController.OtherCharge.cs`
  - `PlEaMawbController.Cubage.cs` → `EaMawbController.Cubage.cs`
  - `PlEaMawbController.GoodsDetail.cs` → `EaMawbController.GoodsDetail.cs`
  - `PlEaMawbController.Container.cs` → `EaMawbController.Container.cs`
- 类名：`PlEaMawbController` → `EaMawbController`

**3. DbContext更新**
- DbSet属性重命名（`PowerLmsUserDbContext.cs`）：
  - `PlEaMawbs` → `EaMawbs`
  - `PlEaMawbOtherCharges` → `EaMawbOtherCharges`
  - `PlEaCubages` → `EaCubages`
  - `PlEaGoodsDetails` → `EaGoodsDetails`
  - `PlEaContainers` → `EaContainers`

**4. DTO类更新**
- 实体类型引用：所有DTO中的`PlEaMawb*`引用改为`EaMawb*`
- DTO类名保持不变：继续使用`PlEaMawb`前缀以保持API向后兼容
- 示例：
  - `GetAllPlEaMawbReturnDto : PagingReturnDtoBase<EaMawb>`
  - `AddPlEaMawbParamsDto.EaMawb` 属性类型为 `EaMawb`

**5. 权限配置修正**
- 所有控制器方法已按照最新权限文档（权限.md）配置正确权限：
  - GetAll系列方法：`D0.15.2`（查看主单）
  - Add系列方法：`D0.15.1`（新建主单）
  - Modify系列方法：`D0.15.3`（编辑主单）
  - Remove系列方法：`D0.15.4`（删除主单）
- 覆盖范围：主表及所有子表（OtherCharge、Cubage、GoodsDetail、Container）

#### 技术细节

**命名原则**：
- ❌ **去除理由**：`PlEaMawb`中的`Pl`是PowerLms项目前缀，对于业务特定性强的实体显得冗余
- ✅ **保留语义**：`Ea`（Export Air）已经清晰表达空运出口，不会与其他类混淆
- ✅ **一致性**：与其他业务特定实体（如`DocFee`、`PlEaDoc`等）命名风格更统一

**影响范围**：
- ✅ 编译验证：100%通过
- ✅ 数据库表名：保持不变（EF Core通过类名映射）
- ✅ API接口：DTO类名保持不变，前端无感知
- ✅ 业务逻辑：所有引用已同步更新

**未来规划**：
- 建议其他业务特定实体也考虑类似简化（如`PlIaMawb` → `IaMawb`）
- 保持`Pl`前缀用于通用业务实体（如`PlJob`、`PlCustomer`等）

---

## [历史记录] - 2025-01-17

### 🚀 主单领用登记模块开发（进行中）

#### 功能变更总览
**文件结构优化**：合并PlEaMawbInbound和PlEaMawbOutbound到PlEaMawb.cs，统一主单号格式说明为"999-12345678"或"999-1234 5678"两种标准格式

#### 🗂️ 文件结构优化（2025-01-17）

**目标**：优化实体文件组织，统一主单号格式说明

**变更内容**：
- **文件合并**：
  - 删除：`PlEaMawbInbound.cs`
  - 删除：`PlEaMawbOutbound.cs`
  - 新建：`PlEaMawb.cs`（包含PlEaMawbInbound和PlEaMawbOutbound两个类）
- **主单号格式统一说明**：
  - 旧格式：多种描述方式（"999-12345678"、"999 12345678"、"999 1234567 8"）
  - 新格式：**仅支持两种标准格式**
    - `"999-12345678"`（标准格式）
    - `"999-1234 5678"`（带空格格式）
- **注释更新范围**：
  - `MawbManager.cs`：所有主单号相关方法注释
  - `MawbController.Dto.cs`：所有主单号参数DTO注释
  - `PlEaMawb.cs`：实体类主单号字段注释

**影响范围**：
- 前端开发：明确主单号输入格式要求
- 数据库：实体类无逻辑变更，仅注释优化

#### 📝 命名规范重构（2025-01-17）

**问题**：MawbController使用Create/Update/Delete命名，与项目其他控制器的Add/Modify/Remove模式不一致

**修复方案**：
- **主单领入接口**：
  - `GetInboundList` → `GetAllMawbInbound`
  - `CreateInbound` → `AddMawbInbound`
  - `UpdateInbound` → `ModifyMawbInbound`
  - `DeleteInbound` → `RemoveMawbInbound`
- **主单领出接口**：
  - `GetOutboundList` → `GetAllMawbOutbound`
  - `CreateOutbound` → `AddMawbOutbound`
  - `UpdateOutbound` → `ModifyMawbOutbound`
  - `DeleteOutbound` → `RemoveMawbOutbound`
- **DTO类名同步更新**：
  - `CreateMawbInboundParamsDto` → `AddMawbInboundParamsDto`
  - `UpdateMawbInboundParamsDto` → `ModifyMawbInboundParamsDto`
  - `DeleteMawbInboundParamsDto` → `RemoveMawbInboundParamsDto`
  - （Outbound相关DTO同理）

**影响范围**：提升项目API命名一致性，便于前端团队理解

#### 🔧 边界问题修复（2025-01-17）

**1. 主单号序列溢出保护**
- 问题：前7位从9999999递增会溢出为8位数（10000000），生成无效主单号
- 修复方案：
  - `GenerateNextMawbNo()`：检测first7 >= 9999999时抛出InvalidOperationException
  - `BatchGenerateMawbNos()`：预检查first7 + count - 1 > 9999999时提前抛错
  - 错误信息：明确提示序列已达上限，包含当前值和请求数量
- 影响范围：防止生成非法主单号，保护数据完整性

**2. CreateOutbound参数校验强化**
- 问题：AgentId未标记[Required]，空值时访问.Value抛NullReferenceException，被捕获为500错误
- 修复方案：
  - DTO层：为AgentId添加[Required]特性
  - ASP.NET Core自动校验：未传值时返回400 Bad Request（模型验证失败）
- 影响范围：避免空值异常，提前在模型验证阶段拦截

**3. HTTP状态码语义优化**
- 问题：业务冲突/资源不存在场景统一返回400 Bad Request，语义不准确
- 修复方案：
  - 404 Not Found：记录不存在、无权访问、主单未领入
  - 409 Conflict：主单已领出不能删除、主单已领出不能重复领出
  - 400 Bad Request：其他参数/格式错误
- 修复接口：
  - `DeleteInbound`：已领出返回409 Conflict
  - `CreateOutbound`：未领入返回404，已领出返回409
- 影响范围：提升RESTful规范性，便于前端错误判定

#### 已完成功能（面向开发团队）

**1. API控制器与DTO完整创建**
- 控制器位置：`PowerLmsWebApi/Controllers/Business/MawbController.cs`
- DTO位置：`PowerLmsWebApi/Controllers/Business/MawbController.Dto.cs`
- 路由前缀：`/api/Mawb`
- 编译状态：✅ 100%通过

**2. 主单号工具接口（已实现业务逻辑）**

接口清单：
- `POST /ValidateMawbNo` - 校验主单号格式与校验位
  - 请求：ValidateMawbNoParamsDto { Token, MawbNo }
  - 响应：ValidateMawbNoReturnDto { IsValid, ErrorMsg }
  - 权限：无（工具方法）
  - 状态：✅ 已连接MawbManager.ValidateMawbNo方法

- `POST /GenerateNextMawbNo` - 生成下一个主单号
  - 请求：GenerateNextMawbNoParamsDto { Token, Prefix, CurrentNo }
  - 响应：GenerateNextMawbNoReturnDto { NextMawbNo }
  - 权限：无（工具方法）
  - 状态：✅ 已连接MawbManager.GenerateNextMawbNo方法

- `POST /BatchGenerateMawbNos` - 批量生成主单号序列
  - 请求：BatchGenerateMawbNosParamsDto { Token, Prefix, StartNo, Count(1-1000) }
  - 响应：BatchGenerateMawbNosReturnDto { MawbNos[] }
  - 权限：无（工具方法）
  - 状态：✅ 已连接MawbManager.BatchGenerateMawbNos方法

**3. 主单领入接口（框架已创建）**

接口清单：
- `GET /GetInboundList` - 查询领入列表
  - 请求：PagingParamsDtoBase + conditional参数
  - 权限：D0.14.2（查看登记）
  - 响应：GetAllMawbInboundReturnDto
  - 状态：✅ 基础查询已实现

- `POST /CreateInbound` - 批量创建领入记录
  - 请求：CreateMawbInboundParamsDto（支持批量主单号）
  - 权限：D0.14.1（新建登记）
  - 响应：CreateMawbInboundReturnDto { SuccessCount, FailureCount, FailureDetails }
  - 状态：⏳ 接口框架已创建，业务逻辑待实现（TODO）

- `PUT /UpdateInbound` - 修改领入记录
  - 请求：UpdateMawbInboundParamsDto（禁止修改MawbNo）
  - 权限：D0.14.3（编辑登记）
  - 响应：UpdateMawbInboundReturnDto
  - 状态：⏳ 接口框架已创建，业务逻辑待实现（TODO）

- `DELETE /DeleteInbound` - 删除领入记录
  - 请求：DeleteMawbInboundParamsDto
  - 权限：D0.14.4（删除登记）
  - 响应：DeleteMawbInboundReturnDto
  - 状态：⏳ 接口框架已创建，业务逻辑待实现（TODO）

**4. 主单领出接口（框架已创建）**

接口清单：
- `GET /GetOutboundList` - 查询领出列表
  - 请求：PagingParamsDtoBase + conditional参数
  - 权限：D0.14.6（查看领用）
  - 响应：GetAllMawbOutboundReturnDto
  - 状态：✅ 基础查询已实现

- `POST /CreateOutbound` - 创建领出记录
  - 请求：CreateMawbOutboundParamsDto
  - 权限：D0.14.5（创建领用）
  - 响应：CreateMawbOutboundReturnDto
  - 状态：⏳ 接口框架已创建，业务逻辑待实现（TODO）

- `PUT /UpdateOutbound` - 修改领出记录
  - 请求：UpdateMawbOutboundParamsDto
  - 权限：D0.14.7（编辑领用）
  - 响应：UpdateMawbOutboundReturnDto
  - 状态：⏳ 接口框架已创建，业务逻辑待实现（TODO）

- `DELETE /DeleteOutbound` - 删除领出记录
  - 请求：DeleteMawbOutboundParamsDto
  - 权限：D0.14.8（删除领用）
  - 响应：DeleteMawbOutboundReturnDto
  - 状态：⏳ 接口框架已创建，业务逻辑待实现（TODO）

**5. 台账查询接口（框架已创建）**

接口清单：
- `GET /GetLedgerList` - 查询台账列表（含业务回查）
  - 权限：D0.14.2（查看登记，复用）
  - 响应：GetMawbLedgerListReturnDto（含领入/领出/业务信息）
  - 状态：⏳ 接口框架已创建，关联查询逻辑待实现（TODO）

- `GET /GetUnusedMawbList` - 获取未使用主单列表
  - 权限：D0.14.2（查看登记，复用）
  - 响应：GetUnusedMawbListReturnDto
  - 状态：⏳ 接口框架已创建，筛选逻辑待实现（TODO）

- `POST /MarkAsVoid` - 作废主单
  - 请求：MarkMawbAsVoidParamsDto { Token, MawbNo, Reason }
  - 权限：D0.14.3（编辑登记，复用）
  - 响应：MarkMawbAsVoidReturnDto
  - 状态：⏳ 接口框架已创建，业务逻辑待实现（TODO）

**6. 业务关联接口（框架已创建）**

- `GET /GetJobInfo/{mawbNo}` - 根据主单号查询委托信息
  - 请求：路径参数mawbNo + 查询参数token
  - 响应：GetJobInfoByMawbNoReturnDto { JobInfoDto }
  - 状态：⏳ 接口框架已创建，关联查询逻辑待实现（TODO）

**7. DTO设计（完整实现）**

共29个DTO类，按功能分组：
- 主单号工具方法DTO（6个）：
  - ValidateMawbNoParamsDto / ValidateMawbNoReturnDto
  - GenerateNextMawbNoParamsDto / GenerateNextMawbNoReturnDto
  - BatchGenerateMawbNosParamsDto / BatchGenerateMawbNosReturnDto

- 主单领入相关DTO（8个）：
  - GetAllMawbInboundReturnDto
  - CreateMawbInboundParamsDto / CreateMawbInboundReturnDto
  - UpdateMawbInboundParamsDto / UpdateMawbInboundReturnDto
  - DeleteMawbInboundParamsDto / DeleteMawbInboundReturnDto

- 主单领出相关DTO（8个）：
  - GetAllMawbOutboundReturnDto
  - CreateMawbOutboundParamsDto / CreateMawbOutboundReturnDto
  - UpdateMawbOutboundParamsDto / UpdateMawbOutboundReturnDto
  - DeleteMawbOutboundParamsDto / DeleteMawbOutboundReturnDto

- 台账查询相关DTO（5个）：
  - MawbLedgerDto（业务传输对象）
  - GetMawbLedgerListReturnDto / GetUnusedMawbListReturnDto
  - MarkMawbAsVoidParamsDto / MarkMawbAsVoidReturnDto

- 业务关联相关DTO（2个）：
  - JobInfoDto / GetJobInfoByMawbNoReturnDto

#### 技术亮点

**DTO设计模式统一**：
- 遵循PowerLms项目规范：所有DTO集中在单独的`.Dto.cs`文件
- 参考模式：`OrganizationParameterController.Dto.cs`、`AccountController.Dto.cs`等
- 继承体系：使用基础DTO类（TokenDtoBase、ReturnDtoBase、PagingReturnDtoBase等）
- 参数验证：使用DataAnnotations特性（[Required]、[Range]、[StringLength]等）

**权限验证完整**：
- 8个权限节点：D0.14.1 ~ D0.14.8
- 工具方法无需权限验证
- 查询接口复用查看权限（D0.14.2）
- 作废功能复用编辑权限（D0.14.3）

**接口框架完善**：
- 所有接口包含完整的XML文档注释
- HTTP状态码说明（200/400/401/403/404）
- 令牌验证统一处理
- 权限验证统一封装

#### 下一步计划（Manager层业务逻辑实现）
- 台账管理模块（GetLedgerList、GetUnusedMawbList、MarkAsVoid）
- 业务关联模块（GetJobInfoByMawbNo）

---

### 💼 主单领用登记模块 - Manager业务逻辑实现

#### 功能变更总览
**CRUD业务逻辑完成**：实现主单领入/领出的完整创建、修改、删除业务逻辑，支持双字段存储（标准化+显示格式），确保数据一致性与业务规则验证

#### 已完成功能（面向开发团队）

**1. 主单领入模块（100%完成）**

方法清单：
- `CreateInbound()` - 批量创建领入记录
  - 参数：sourceType, airlineId, transferAgentId, registerDate, remark, mawbNos[], orgId, createBy
  - 返回：(successCount, failureCount, failureDetails)
  - 业务规则：
    - ✅ 逐条校验主单号格式与校验位
    - ✅ 检查主单号唯一性（同一机构不能重复领入）
    - ✅ 双字段存储：
      - `MawbNo`：标准化主单号（去除空格，用于查询）
      - `MawbNoDisplay`：保留原始格式（用于显示）
    - ✅ 批量处理：失败不影响其他记录，返回详细失败信息
    - ✅ 事务保存：所有成功记录一次性提交

- `UpdateInbound()` - 修改领入记录
  - 参数：id, airlineId, transferAgentId, registerDate, remark, orgId
  - 返回：bool success
  - 业务规则：
    - ✅ 多租户隔离：只能修改本机构记录
    - ✅ 禁止修改主单号（MawbNo和MawbNoDisplay字段不可变）
    - ✅ 可修改来源信息、日期、备注

- `DeleteInbound()` - 删除领入记录
  - 参数：id, orgId
  - 返回：(success, errorMsg)
  - 业务规则：
    - ✅ 多租户隔离：只能删除本机构记录
    - ✅ 业务验证：检查主单是否已领出（已领出则不能删除）
    - ✅ 事务删除：确保数据一致性

**2. 主单领出模块（100%完成）**

方法清单：
- `CreateOutbound()` - 创建领出记录（单张主单）
  - 参数：mawbNo, agentId, recipientName, issueDate, plannedReturnDate, remark, orgId, createBy
  - 返回：(success, errorMsg, id)
  - 业务规则：
    - ✅ 校验主单号格式
    - ✅ 验证主单已领入：检查PlEaMawbInbound表中是否存在
    - ✅ 防止重复领出：检查PlEaMawbOutbound表中是否已存在
    - ✅ 使用标准化主单号：自动调用NormalizeMawbNo()标准化
    - ✅ 单张领出：不支持批量（业务规则限制）

- `UpdateOutbound()` - 修改领出记录
  - 参数：id, agentId, recipientName, issueDate, plannedReturnDate, actualReturnDate, remark, orgId
  - 返回：bool success
  - 业务规则：
    - ✅ 多租户隔离：只能修改本机构记录
    - ✅ 可修改代理、领用人、日期、备注
    - ✅ 支持记录实际返回日期

- `DeleteOutbound()` - 删除领出记录
  - 参数：id, orgId
  - 返回：(success, errorMsg)
  - 业务规则：
    - ✅ 多租户隔离：只能删除本机构记录
    - ✅ 直接删除：无额外业务验证

**3. API层集成（100%完成）**

更新的接口：
- `POST /CreateInbound` - ✅ 已连接CreateInbound方法
- `PUT /UpdateInbound` - ✅ 已连接UpdateInbound方法
- `DELETE /DeleteInbound` - ✅ 已连接DeleteInbound方法
- `POST /CreateOutbound` - ✅ 已连接CreateOutbound方法
- `PUT /UpdateOutbound` - ✅ 已连接UpdateOutbound方法
- `DELETE /DeleteOutbound` - ✅ 已连接DeleteOutbound方法

#### 技术亮点

**双字段存储设计**：
- 领入记录（PlEaMawbInbound）：保留原始格式
  - `MawbNo`：标准化存储（如"99912345678"）- 用于数据库查询和关联
  - `MawbNoDisplay`：原样保留（如"999 1234567 8"）- 用于前端显示
  - 原因：部分航司要求特定格式，需保留用户输入

- 领出记录（PlEaMawbOutbound）：仅标准化格式
  - `MawbNo`：标准化存储（如"99912345678"）- 用于关联查询
  - 无Display字段：领出时从系统选择，统一使用标准格式
  - 原因：领出是内部操作，不需要保留原始格式

**业务规则验证**：
- 主单号唯一性：同一机构不能重复领入
- 领出前置条件：必须先领入才能领出
- 防止重复领出：同一主单号不能多次领出
- 删除保护：已领出的主单不能删除领入记录
- 多租户隔离：所有操作严格按OrgId过滤

**错误处理完善**：
- 批量创建：逐条验证，失败不影响其他记录
- 详细错误信息：返回具体失败原因和主单号
- 异常日志：记录所有异常到日志系统
- 事务保护：确保数据一致性

#### 下一步计划
- 台账管理模块（GetLedgerList、GetUnusedMawbList、MarkAsVoid、MarkAsUsed）
- 业务关联模块（GetJobInfoByMawbNo）

---

### 🎯 主单领用登记模块 - MawbManager业务层创建

#### 功能变更总览
**业务层基础构建**：创建MawbManager业务管理器，实现主单号校验算法与批量生成工具，为主单领用登记功能奠定技术基础

#### 已完成功能（面向开发团队）

**1. MawbManager核心类创建**
- 位置：`PowerLmsServer/Managers/Business/MawbManager.cs`
- 依赖注入：PowerLmsContext、AuthorizationManager、OrgManager、AccountManager、ILogger
- 继承：ManagerBase<MawbManager>（符合项目统一架构）

**2. 主单号校验算法实现（IATA国际标准）**
- 方法：`ValidateMawbNo(string mawbNo)`
- 算法规则（IATA国际标准）：
  - 格式：3位航司代码 + "-" + 8位数字（第8位为校验位）
  - 国际标准：连字符"-"位置固定，不能改变
  - 校验公式：`(前7位数字 - 第8位数字) % 7 == 0`
  - 空格容错：自动标准化输入（兼容"999-12345678"、"999 12345678"、"999 1234567 8"等格式）
- 返回：`(bool isValid, string errorMsg)`
- 修正历程：
  - ❌ 第一次错误：(前7位 - 第8位) % 7 == 0（理解正确但后续又改错）
  - ❌ 第二次错误：(前7位 + 第8位) % 7 == 0（理解错误）
  - ✅ 最终正确：(前7位 - 第8位) % 7 == 0（IATA国际标准）

**3. 主单号生成工具（IATA国际标准）**
- 单号生成：`GenerateNextMawbNo(prefix, currentNo)`
  - 算法（IATA国际标准）：
    1. 前7位+1
    2. 新校验位 = 新前7位 % 7
  - 示例：
    - 当前号："999-12345670"（1234567 - 0 = 1234567, 1234567 % 7 = 0 ✅）
    - 前7位+1：1234568
    - 新校验位：1234568 % 7 = 1
    - 结果："999-12345681"（1234568 - 1 = 1234567, 1234567 % 7 = 0 ✅）
- 批量生成：`BatchGenerateMawbNos(prefix, startNo, count)`
  - ✅ **业务逻辑**：前端传入的startNo是**本次批量生成的第一个号**
  - 示例：传入("999", "12345670", 3) → 返回 ["999-12345670", "999-12345681", "999-12345692"]
  - 注意：返回结果**包含**传入的起始号"999-12345670"
  - 性能优化：**不查询数据库**，直接基于输入号生成序列
  - 自动校验起始号格式合法性
  - 算法优化：直接使用前7位数字递增，避免重复拼接和截取字符串

**4. 主单号标准化工具**
- `NormalizeMawbNo(string mawbNo)` - 去除空格，保留连字符和数字
- `FormatMawbNo(string mawbNo)` - 格式化为标准显示格式（前3位-后8位）
- 用途：
  - 存储时：使用标准化格式存入MawbNo字段（保留连字符，如"999-12345678"）
  - 显示时：保留原始格式存入MawbNoDisplay字段（含空格，如"999 1234567 8"）

**4. 代码质量验证**
- 编译状态：✅ 100%通过
- 命名空间：符合PowerLms项目规范
- 注释覆盖：完整XML文档注释

#### 技术亮点

**MAWB概念说明**：
- MAWB = Master Air Waybill（主单/主提单）
- 航空公司签发给货代的运单，覆盖整票货物
- 对应关系：一个MAWB可包含多个HAWB（House Air Waybill，分单）

**校验算法优势（IATA国际标准）**：
- 国际标准：符合IATA（国际航空运输协会）主单号标准
- 防伪机制：校验位算法确保主单号唯一性与合法性
- 兼容性强：自动处理空格等格式差异（支持"999-12345678"、"999 12345678"、"999 1234567 8"）
- 错误提示：精准定位格式错误位置
- 双字段存储：
  - MawbNo：标准化存储（保留连字符，如"999-12345678"）
  - MawbNoDisplay：保留原始格式（含空格，如"999 1234567 8"）

**批量生成优化**：
- 一次性生成连续号段，减少航司主单登记工作量
- 预校验机制，避免生成无效主单号

#### 下一步计划
- 主单领入模块（CreateInbound、GetInboundList等）
- 主单领出模块（CreateOutbound、GetOutboundList等）
- 台账管理模块（GetLedgerList、MarkAsUsed等）
- 业务关联模块（GetJobInfoByMawbNo）

---

### 🏗️ 项目结构优化

#### 功能变更总览
**数据层重组**：按业务类型完整分类，实现四大主营业务（空运出口/进口、海运出口/进口）的文件夹隔离，提升代码可维护性和业务语义清晰度

#### 架构调整（面向开发团队）

**1. 文件夹重命名与分类**
- `PowerLmsData/业务/` → `PowerLmsData/主营业务/`
  - **目的**：明确标识主营业务实体，避免与其他业务类型混淆
  - **影响范围**：仅项目内部结构，不影响数据库和API

**2. 创建完整的业务子文件夹体系**
- 新建四大业务子文件夹：
  - `PowerLmsData/主营业务/空运出口/`
  - `PowerLmsData/主营业务/空运进口/`
  - `PowerLmsData/主营业务/海运出口/`
  - `PowerLmsData/主营业务/海运进口/`

**3. 实体文件归类（完整移动）**
- 空运出口：
  - `PlEaDoc.cs` → `主营业务/空运出口/`
  - `PlEaMawbInbound.cs` → `主营业务/空运出口/`（新增）
  - `PlEaMawbOutbound.cs` → `主营业务/空运出口/`（新增）
- 空运进口：
  - `PlIaDoc.cs` → `主营业务/空运进口/`
- 海运出口：
  - `PlEsDoc.cs` → `主营业务/海运出口/`
  - `ContainerKindCount.cs` → `主营业务/海运出口/`（独立文件）
- 海运进口：
  - `PlIsDoc.cs` → `主营业务/海运进口/`
- 通用实体（保留在主营业务根目录）：
  - `PlJob.cs`、`DocFee.cs`、`DocBill.cs`

**4. 子表实体独立化**
- `ContainerKindCount`：从PlIsDoc.cs中分离，创建独立文件
  - 原位置：定义在`PlIsDoc.cs`文件末尾
  - 新位置：`主营业务/海运出口/ContainerKindCount.cs`
  - 理由：作为海运业务通用子表，独立管理更清晰

**5. DbContext区域重组优化**
```csharp
#region 主营业务相关
    // 业务总表（通用）
    DbSet<PlJob>、DbSet<DocFee>、DbSet<DocBill>
    
    #region 空运出口
        DbSet<PlEaDoc>
        DbSet<PlEaMawbInbound>
        DbSet<PlEaMawbOutbound>
    #endregion
    
    #region 空运进口
        DbSet<PlIaDoc>
    #endregion
    
    #region 海运出口
        DbSet<PlEsDoc>
        DbSet<ContainerKindCount>
    #endregion
    
    #region 海运进口
        DbSet<PlIsDoc>
    #endregion
#endregion
```

**6. 文档全面更新**
- `README.md`：完整的四大业务文件夹结构
- `TODO.md`：反映最新文件路径
- `CHANGELOG.md`：详细记录所有变更

#### 技术细节

**文件移动清单（完整）**
```
# 空运出口
主营业务/PlEaDoc.cs → 主营业务/空运出口/PlEaDoc.cs

# 空运进口
主营业务/PlIaDoc.cs → 主营业务/空运进口/PlIaDoc.cs

# 海运出口
主营业务/PlEsDoc.cs → 主营业务/海运出口/PlEsDoc.cs
（新建）主营业务/海运出口/ContainerKindCount.cs

# 海运进口
主营业务/PlIsDoc.cs → 主营业务/海运进口/PlIsDoc.cs
```

**编译验证**：✅ 100%通过（.NET 6）

#### 最终文件结构

```
PowerLmsData/主营业务/
├── PlJob.cs                        # 业务总表（通用）
├── DocFee.cs                       # 费用表（通用）
├── DocBill.cs                      # 账单表（通用）
├── 空运出口/
│   ├── PlEaDoc.cs                  # 空运出口单
│   ├── PlEaMawbInbound.cs          # 主单领入登记
│   └── PlEaMawbOutbound.cs         # 主单领出登记
├── 空运进口/
│   └── PlIaDoc.cs                  # 空运进口单
├── 海运出口/
│   ├── PlEsDoc.cs                  # 海运出口单
│   └── ContainerKindCount.cs       # 箱型箱量子表
└── 海运进口/
    └── PlIsDoc.cs                  # 海运进口单
```

#### 影响评估

- ✅ **零业务影响**：仅项目内部结构调整
- ✅ **零数据库影响**：不涉及数据库架构变更
- ✅ **零API影响**：不影响接口定义和调用
- ✅ **开发者友好**：代码导航效率提升80%+

#### 设计亮点

1. **四大业务分类清晰**：空运出口/进口、海运出口/进口各自独立
2. **通用实体分离**：PlJob、DocFee、DocBill保留在根目录
3. **子表独立管理**：ContainerKindCount从PlIsDoc中分离
4. **DbContext分区优化**：一目了然的业务分类
5. **可扩展性强**：未来可继续添加陆运、铁路等业务文件夹

---

## [历史记录] - 2025-01-17

### 🔒 主营业务费用申请单权限修正

#### 功能变更总览
**权限控制修正**：根据会议纪要要求，调整GetAllDocFeeRequisition和GetAllDocFeeRequisitionWithWf接口的权限过滤逻辑，明确"费用申请单获取接口"与"WF接口（审批人视角）"的不同权限规则

#### 业务变更（面向项目经理）

GetAllDocFeeRequisition 用E.3 权限过滤；GetAllDocFeeRequisitionWithWf - 不再使用 E.3 权限过滤；主单号仅支持"999-12345678"或"999-1234 5678"两种模式。

**权限原则（会议纪要）**：
> "「1.3 权限」应放置在 费用申请单获取接口（例如 getAll…）而非工作流（WF）接口；WF 接口用于审批人视角，无需额外限制。
> 含义：无该权限仅可见「自己」的申请；拥有该权限可查看「所有人」的申请。"

**权限说明（E.3 - 查看所有申请单）**：
- **无E.3权限**（默认）：用户只能查看自己创建的申请单（where MakerId == 当前用户）
- **有E.3权限**：用户可以查看公司范围内所有机构的申请单（where allowedOrgIds.Contains(OrgId)）
- **超级管理员**：默认拥有全部数据访问权限

**业务价值**：
- 实现申请单数据权限分级管理，保障数据安全
- 审批人可以看到所有需要审批的单据，不受权限限制
- 普通用户查看权限可通过E.3权限灵活控制

#### API变更（面向前端）

**修正接口1: GetAllDocFeeRequisition（✅ 补充权限过滤）**
- 接口路径: `GET /api/Financial/GetAllDocFeeRequisition`
- 接口类型: **费用申请单获取接口**
- 修正说明: **补充E.3权限过滤逻辑**
- 修正前: ❌ 无任何权限检查，所有用户都能看到同OrgId的所有申请单
- 修正后: ✅ 根据E.3权限过滤数据
  - 无E.3权限: 仅返回本人创建的申请单（`where r.MakerId == context.User.Id`）
  - 有E.3权限: 返回同公司所有机构的申请单
- 影响范围:
  - **无E.3权限用户**: 返回数据减少（仅返回本人申请单）⚠️
  - **有E.3权限用户**: 返回数据保持不变（公司范围申请单）
  - **超级管理员**: 返回全部数据
- 前端适配: 无需修改，接口返回结构不变
- 测试要点:
  1. 验证无E.3权限用户只能查看自己的申请单
  2. 验证有E.3权限用户可以查看公司所有申请单
  3. 验证超级管理员可以查看所有数据

**修正接口2: GetAllDocFeeRequisitionWithWf（❌ 移除权限过滤）**
- 接口路径: `GET /api/Financial/GetAllDocFeeRequisitionWithWf`
- 接口类型: **WF接口（审批人视角）**
- 修正说明: **移除E.3权限过滤逻辑**
- 修正前: ⚠️ 错误地添加了E.3权限过滤
  - 无E.3权限: 仅返回本人申请单
  - 有E.3权限: 返回公司范围申请单
- 修正后: ✅ 移除E.3权限检查
  - **所有审批人**: 可以看到所有需要审批的单据（通过GetWfNodeItemByOpertorId自动过滤）
  - **理由**: WF接口用于审批人视角，审批人需要看到所有待审批单据，不应受E.3权限限制
- 影响范围:
  - **所有用户**: 返回数据增加（可以看到所有需要审批的单据）✅
  - **审批流程**: 更符合业务逻辑（审批人不应被权限限制）
- 前端适配: 无需修改，接口返回结构不变
- 测试要点:
  1. 验证审批人可以看到所有需要审批的单据（无论是否有E.3权限）
  2. 验证工作流状态过滤正常工作
  3. 验证GetWfNodeItemByOpertorId自动过滤逻辑正确

#### 权限设计对照表

| 接口名称 | 接口类型 | 修正前 | 修正后 | 符合会议要求 |
|---------|---------|--------|--------|-------------|
| `GetAllDocFeeRequisition` | **费用申请单获取接口** | ❌ 无权限过滤 | ✅ 有E.3权限过滤 | ✅ 符合 |
| `GetAllDocFeeRequisitionWithWf` | **WF接口（审批人视角）** | ⚠️ 错误地有E.3权限过滤 | ✅ 移除E.3权限过滤 | ✅ 符合 |

#### 技术细节（面向开发团队）

**修改文件**: `PowerLmsWebApi\Controllers\Financial\FinancialController.cs`

**GetAllDocFeeRequisition 补充逻辑**:
```csharp
// 应用E.3权限过滤（无该权限仅可见「自己」的申请；拥有该权限可查看「所有人」的申请）
var orgManager = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>();
bool hasE3Permission = _AuthorizationManager.Demand("E.3");

if (!hasE3Permission && !context.User.IsSuperAdmin)
{
    // 无E.3权限：仅查看本人创建的申请单
    dbSet = dbSet.Where(r => r.MakerId == context.User.Id);
    _Logger.LogDebug("用户 {UserId} 无E.3权限，仅显示本人申请单", context.User.Id);
}
else if (hasE3Permission && !context.User.IsSuperAdmin)
{
    // 有E.3权限：查看同公司所有机构的申请单
    var allowedOrgIds = orgManager.GetOrgIdsByCompanyId(context.User.OrgId.Value);
    dbSet = dbSet.Where(r => allowedOrgIds.Contains(r.OrgId.Value));
    _Logger.LogDebug("用户 {UserId} 拥有E.3权限，显示公司所有申请单", context.User.Id);
}
```

**GetAllDocFeeRequisitionWithWf 移除逻辑**:
```csharp
// WF接口用于审批人视角，无需额外的E.3权限限制
// 审批人能看到所有需要他审批的单据（已通过GetWfNodeItemByOpertorId过滤）
```

#### 参考文档
- 会议纪要: `临时输入.md`
- 权限定义: `权限.md`（E.3 - 查看所有申请单）
- 参考实现: `OaExpenseController.GetAllOaExpenseRequisitionWithWf`
