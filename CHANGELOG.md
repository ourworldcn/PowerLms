# 📝 变更日志

## [未发布]-2026-02-27

### 📋 一句话总结
修复空运进口舱单新增时"关联失败:未找到主单号对应的空运出口主单"错误。

### 🎯 面向项目经理

#### 问题背景
在使用空运进口舱单功能时，用户反馈无法正常录入舱单数据，系统提示错误："**关联失败:未找到主单号「临时1」对应的空运出口主单，请确认主单号是否正确或该主单是否已在系统中创建**"。

经过业务需求确认，这是一个**验证逻辑错误**：
- **空运进口业务**：主单号和分单号来自**国外航司**，是国外段已经存在的运单号
- **空运出口业务**：主单号和分单号由**本公司制作**，需要在系统中先创建
- 原系统错误地要求：进口舱单的主单号必须先在"空运出口主单"模块中创建

#### 修复内容
1. **移除不合理的强制验证**：进口舱单录入时，不再要求主单号/分单号必须在出口主单/分单表中存在
2. **保留智能关联功能**：如果系统中恰好存在同号的出口主单/分单，会自动建立关联（便于数据统计和查询）
3. **格式验证保留**：仍然会对主单号/分单号进行格式标准化处理（去除空格、统一连字符等）

#### 业务价值
- ✅ **解决进口业务阻塞**：进口操作人员可以直接录入国外提供的主单号，无需额外操作
- ✅ **符合实际业务流程**：进口和出口是两个独立的业务流程，不应互相依赖
- ✅ **提升操作效率**：减少不必要的验证步骤，简化录入流程
- ✅ **保持数据灵活性**：支持先录入舱单，后续再补录出口主单（如果需要）

#### 使用场景示例
**场景1：纯进口业务**
- 国外航司提供主单号：`999-12345678`
- 操作人员直接在"空运进口舱单"模块录入
- 系统正常保存，MawbId字段为null
- **之前**：报错，要求先在"空运出口主单"创建
- **现在**：✅ 直接保存成功

**场景2：转口业务（既有出口又有进口）**
- 先在"空运出口主单"创建了主单：`999-12345678`
- 后续录入进口舱单时使用相同主单号
- 系统自动识别并建立关联，MawbId指向出口主单ID
- **业务价值**：便于统计同一票货物的进出口数据

### 💻 面向前端开发

#### API变更详情

**影响的API接口（共3个）：**

1. **`POST /api/IaManifest/AddManifest`** - 新增空运进口舱单（含明细）
2. **`POST /api/IaManifest/AddManifestDetail`** - 新增空运进口舱单明细
3. **`PUT /api/IaManifest/ModifyManifestDetail`** - 修改空运进口舱单明细

#### 请求参数变化
✅ **无变化** - 请求参数结构保持不变

#### 响应变化

**成功场景（新增/修改舱单明细）：**
```json
// 之前：如果主单号不存在于EaMawb表，返回400错误
{
  "hasError": true,
  "errorCode": 400,
  "debugMessage": "关联失败: 未找到主单号 [999-12345678] 对应的空运出口主单..."
}

// 现在：直接保存成功，返回200
{
  "hasError": false,
  "id": "新记录的GUID"
}
```

**数据库字段变化：**
- `IaManifestDetail.MawbId`字段：
  - **数据库定义**：`Guid?`（可空类型，无`[Required]`约束）✅
  - **之前行为**：必定有值（关联到EaMawb或EaHawb的ID）
  - **现在行为**：可能为`null`（未找到对应的出口主单/分单时）或有值（找到时自动关联）
  - **兼容性**：数据库结构无需调整，已支持NULL值

#### 前端需要调整的地方

**1. 错误处理逻辑（必须调整）**
```typescript
// ❌ 移除这个错误提示的处理逻辑
if (response.debugMessage?.includes('关联失败')) {
  // 这个错误不会再出现了
}

// ✅ 只需要处理其他验证错误（如主单号为空）
if (response.errorCode === 400) {
  // 处理其他业务验证错误
}
```

**2. 数据展示逻辑（可选优化）**
```typescript
// 如果需要展示是否已关联出口主单
interface IaManifestDetailDto {
  id: string;
  mawbNo: string;
  hblNo?: string;
  mawbId?: string | null;  // 注意：现在可能为null
}

// 展示关联状态（可选功能）
const hasLinkedMawb = detail.mawbId !== null;
const statusText = hasLinkedMawb 
  ? "已关联出口主单" 
  : "未关联（进口独立单）";
```

**3. 测试建议**
- ✅ 测试录入一个不存在的主单号（如"临时测试"），应能正常保存
- ✅ 测试录入一个已存在的出口主单号，检查是否自动关联（MawbId有值）
- ✅ 测试修改舱单明细，更换主单号，检查关联状态是否正确更新

#### 兼容性说明
- ✅ **向后兼容**：前端现有代码无需强制修改（除非有硬编码的错误处理）
- ✅ **数据库兼容**：已有数据不受影响
- ⚠️ **注意**：如果前端有展示MawbId关联信息的逻辑，需要处理`null`值

---

### 技术变更（面向开发人员）

#### 修改代码
1. **IaManifestController.FindRelatedMawb方法** (PowerLmsWebApi\Controllers\Business\AirFreight\IaManifestController.cs)
   - 移除代码：删除验证主单号/分单号是否在`EaMawb`/`EaHawb`表中存在的强制逻辑
   - 移除的验证片段：
     ```csharp
     if (hawb == null)
     {
         return (null, $"未找到分单号 [{hawbNo}] 对应的空运出口分单，请确认分单号是否正确或该分单是否已在系统中创建");
     }
     ```
     ```csharp
     if (mawb == null)
     {
         return (null, $"未找到主单号 [{mawbNo}] 对应的空运出口主单，请确认主单号是否正确或该主单是否已在系统中创建");
     }
     ```
   - 保留逻辑：
     - 主单号/分单号格式标准化（去除空格、横杠等）
     - 可选的智能关联：如果找到对应的主单/分单则返回其ID，否则返回null
   - 业务逻辑变化：
     - 之前：找不到主单/分单时返回错误信息，调用方返回BadRequest拒绝保存
     - 现在：找不到主单/分单时返回null（MawbId为null），正常保存，不影响业务流程

### 影响范围
- **修改文件**：
  - PowerLmsWebApi\Controllers\Business\AirFreight\IaManifestController.cs
- **影响API**：
  - `POST /api/IaManifest/AddManifest`
  - `POST /api/IaManifest/AddManifestDetail`
  - `PUT /api/IaManifest/ModifyManifestDetail`
- **编译状态**：✅ 成功编译
- **数据库影响**：✅ 无需迁移（`MawbId`字段已定义为`Guid?`可空类型）
- **业务影响**：空运进口业务可直接录入舱单，无需先创建出口主单/分单

#### 数据建模验证（已核查）
| 字段 | 数据类型 | `[Required]`约束 | 前端是否必传 | 修复后影响 |
|------|---------|-----------------|-------------|-----------|
| `MawbNo` | `string` | ✅ **必填** | ✅ 是 | ❌ 无影响（控制器验证） |
| `HBLNO` | `string` | ❌ 可空 | ❌ 否 | ❌ 无影响 |
| `MawbId` | `Guid?` | ❌ 可空 | ❌ 否（系统自动填充） | ✅ **现在允许null** |

> ✅ **安全性确认**：所有必填字段均有控制器验证，`MawbId`字段数据库定义已支持NULL，无需执行数据库迁移。

---

## [已完成]-2026-02-27 (之前版本)

### 📋 一句话总结
移除空运主单领出功能中的过度验证，支持不经过领入直接领出主单。

### 🎯 面向项目经理
修复了空运主单领出功能的不合理限制。进口业务中，主单号由国外提供，不需要在系统中先"领入"再"领出"。现在系统只校验主单号格式，不再强制要求主单号必须先领入系统。

### 💻 面向前端开发
**变更影响API：**
- `POST /api/Mawb/AddMawbOutbound` - 新增主单领出记录
  - 移除：不再验证主单号是否在领入表中存在
  - 保留：主单号格式校验(IATA国际标准)
  - 保留：重复领出校验
  - 错误提示变化：
    - ~~"主单号不存在，请先进行领入登记"~~ (已移除)
    - ✅ "主单号格式错误: [具体错误信息]"
    - ✅ "主单已领出，不能重复领出"

---

### 业务变更（面向项目经理）

#### 功能优化
- **空运主单领出流程优化**：移除不合理的前置验证逻辑
  - 之前：必须先执行"领入"登记，才能"领出"主单，导致进口业务或临时测试时无法操作
  - 现在：只需主单号格式正确即可创建领出记录，简化业务流程
  - 适用场景：进口业务(主单由国外提供)、临时测试、快速登记等

### 技术变更（面向开发人员）

#### 修改代码
1. **MawbManager.CreateOutbound方法** (PowerLmsServer\Managers\Business\MawbManager.cs)
   - 移除代码：删除验证主单号是否在`PlEaMawbInbounds`表中存在的逻辑
   - 移除的验证片段：
     ```csharp
     var inbound = _DbContext.PlEaMawbInbounds
         .AsNoTracking()
         .FirstOrDefault(x => x.MawbNo == normalizedMawbNo && x.OrgId == orgId);
     if (inbound == null)
     {
         return (false, "主单号不存在，请先进行领入登记", null);
     }
     ```
   - 保留验证：
     - 主单号格式校验(`ValidateMawbNo`，符合IATA国际标准)
     - 重复领出校验(同一组织下同一主单号不能重复领出)
   - 业务逻辑变化：支持不经过领入直接创建领出记录

### 影响范围
- **修改文件**：
  - PowerLmsServer\Managers\Business\MawbManager.cs
- **影响API**：
  - `POST /api/Mawb/AddMawbOutbound`
- **编译状态**：✅ 成功编译
- **业务影响**：进口业务、临时测试场景可直接创建领出记录，无需先领入

---

## [已完成]-2026-02-23

### 📋 一句话总结
新增海运出口提单管理功能，支持主提单(船东提单)和分提单(货代提单)的完整CRUD操作。

### 🎯 面向项目经理
本次更新新增了海运出口业务中最核心的提单管理模块。支持录入、查询、修改船公司签发的主提单(MBL)和货代签发的分提单(HBL)，一个工作号可以对应一个主提单和多个分提单，满足海运业务的实际需求。

### 💻 面向前端开发
**新增API接口（共8个）：**

**主提单(EsMbl)：**
- `GET /api/EsMbl/GetAllEsMbl` - 分页查询主提单列表
- `POST /api/EsMbl/AddEsMbl` - 新增主提单
- `PUT /api/EsMbl/ModifyEsMbl` - 修改主提单
- `DELETE /api/EsMbl/RemoveEsMbl` - 删除主提单

**分提单(EsHbl)：**
- `GET /api/EsHbl/GetAllEsHbl` - 分页查询分提单列表
- `POST /api/EsHbl/AddEsHbl` - 新增分提单
- `PUT /api/EsHbl/ModifyEsHbl` - 修改分提单
- `DELETE /api/EsHbl/RemoveEsHbl` - 删除分提单

**注意事项：**
- 修改操作的请求参数从单个实体改为`Items`数组（支持批量修改，但当前建议每次只传一个）
- 新增操作的请求参数使用`Item`属性传递实体数据
- 权限码统一使用：D2.1.1.2(新增)、D2.1.1.3(修改)、D2.1.1.4(删除)

---

### 业务变更（面向项目经理）

#### 新增功能
- **海运出口主提单管理(船东提单)**：支持海运业务中船公司签发的主提单管理，包含完整的提单信息字段
- **海运出口分提单管理(货代提单)**：支持货运代理签发的分提单管理，字段结构与主单一致，支持一个工作号下多个分单

### 技术变更（面向开发人员）

#### 新增实体类
1. **EsMbl** (PowerLmsData\主营业务\海运出口\EsMbl.cs)
   - 海运出口主提单(船东提单)实体
   - 命名规范：采用Es前缀(Export Seaborne)，与空运提单命名风格一致(如EaMawb)
   - 关联关系：一个工作号仅关联一个主提单
   - 核心字段：
     - 提单编号、付款方式、运输条款、前程运输
     - 港口信息(收货地、装船港、中转港、目的港及其描述)
     - 船公司、船名、航次
     - 费用描述、装船日期、开船日期、到港日期
     - 发货人/收货人/通知人信息(名称+抬头)
     - 第二通知人抬头
     - 箱号、箱量、品名、唛头
     - 件数、包装类型、毛重、体积
     - 总计字段(箱量英文合计、件数英文合计)
     - 签发信息(正本张数、签发人、签发时间、签发地点)
     - 放货方式、备注、提单附页
   - 字段特性：大部分字段为字符串且可为空，DateTime类型字段均可为空
   - 数值字段：件数(int?)、正本张数(int?)、毛重(decimal?)、体积(decimal?)

2. **EsHbl** (PowerLmsData\主营业务\海运出口\EsHbl.cs)
- 海运出口分提单(货代提单)实体
- 命名规范：采用Es前缀，与主提单命名风格一致
   - 关联关系：一个工作号下可有多个分单，分单与主单无直接外键关联
   - 字段结构：与主单完全一致，额外增加预付运费(PPD_AMT)和到付运费(CCT_AMT)字段
   - 提单编号：由货代生成，可通过"其他编码规则"模块自动生成

#### 数据库变更
- 在PowerLmsUserDbContext中注册两个新DbSet：
  - `DbSet<EsMbl> EsMbls`
  - `DbSet<EsHbl> EsHbls`
- 两个实体均在JobId字段上建立索引

#### 新增控制器和DTO
1. **EsMblController** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.cs)
   - 海运出口主提单控制器
   - 实现完整CRUD操作(GetAllEsMbl、AddEsMbl、ModifyEsMbl、RemoveEsMbl)
   - 权限码：D2.1.1.2(新增)、D2.1.1.3(修改)、D2.1.1.4(删除)
   - 日志记录：创建、修改、删除操作均记录日志

2. **EsMblController.Dto** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.Dto.cs)
   - 主提单DTO定义
   - DTO基类规范：
     - AddEsMblParamsDto继承自AddParamsDtoBase<EsMbl>，使用Item属性
     - AddEsMblReturnDto继承自AddReturnDtoBase，自动包含Id属性
     - ModifyEsMblParamsDto继承自ModifyParamsDtoBase<EsMbl>，使用Items属性
     - ModifyEsMblReturnDto继承自ModifyReturnDtoBase
   - 包含：GetAllEsMblReturnDto、AddEsMblParamsDto/ReturnDto、ModifyEsMblParamsDto/ReturnDto、RemoveEsMblParamsDto/ReturnDto

3. **EsHblController** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.cs)
   - 海运出口分提单控制器
   - 实现完整CRUD操作(GetAllEsHbl、AddEsHbl、ModifyEsHbl、RemoveEsHbl)
   - 权限码：D2.1.1.2(新增)、D2.1.1.3(修改)、D2.1.1.4(删除)
   - 日志记录：创建、修改、删除操作均记录日志

4. **EsHblController.Dto** (PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.Dto.cs)
   - 分提单DTO定义
   - DTO基类规范：与主提单DTO相同，从正确的泛型基类派生
   - 包含：GetAllEsHblReturnDto、AddEsHblParamsDto/ReturnDto、ModifyEsHblParamsDto/ReturnDto、RemoveEsHblParamsDto/ReturnDto

#### DTO基类修复说明
- **问题**：原始实现中Add和Modify参数DTO直接继承TokenDtoBase，没有使用标准的泛型基类
- **修复**：
  - Add参数DTO改为继承AddParamsDtoBase<T>，删除自定义实体属性，使用基类的Item属性
  - Modify参数DTO改为继承ModifyParamsDtoBase<T>，删除自定义实体属性，使用基类的Items属性
  - Add返回DTO改为继承AddReturnDtoBase，删除重复的Id属性定义
  - Modify返回DTO改为继承ModifyReturnDtoBase
- **优势**：符合框架规范，支持批量修改，便于将来迁移到GenericEfController

#### 待办事项
- 需要手工执行数据库迁移(Add-Migration和Update-Database)
- 需要实现从工作号订舱信息预填充箱型箱量、件/重/体数据的逻辑

### 影响范围
- **新增文件**：
  - PowerLmsData\主营业务\海运出口\EsMbl.cs
  - PowerLmsData\主营业务\海运出口\EsHbl.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsMblController.Dto.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.cs
  - PowerLmsWebApi\Controllers\Business\SeaFreight\EsHblController.Dto.cs
- **修改文件**：
  - PowerLmsData\PowerLmsUserDbContext.cs (新增两个DbSet注册)
- **编译状态**：✅ 成功编译
- **数据库迁移**：⚠️ 需手工执行(按照开发规范，AI不生成迁移文件)
- **命名变更说明**：实体和控制器采用Es前缀(Export Seaborne)，与空运提单命名风格一致

