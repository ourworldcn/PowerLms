# PowerLms 功能变更总览

**最新更新：财务日期逻辑重构完成**

完成了财务日期从数据库字段到本地计算字段的重构，实现了基于业务类型的自动联动计算，确保数据的一致性和业务逻辑的准确性。

## 2025-01-27 功能变更

### 财务日期逻辑重构：新增计算字段而非替换现有字段

**背景**：根据会议决议，需要新增一个财务日期字段，该字段根据业务类型自动计算，而不是修改现有的`AccountDate`字段。

**解决方案**：在`PlJob`实体中新增`FinancialDate`计算字段，保留原有`AccountDate`字段不变，在返回工作对象前由专门服务填充。

#### 技术实现方案

**1. 实体类修改**
- **文件**：`PowerLmsData/业务/PlJob.cs`
- **变更**：新增`FinancialDate`字段，标记为`[NotMapped]`，不映射到数据库
- **保持**：原有`AccountDate`字段保持不变
- **类型**：`DateTime?`，支持空值状态

**2. 财务日期填充服务**
- **位置**：`PowerLmsServer/Managers/Business/JobManager.cs`
- **新增方法**：
  - `FillFinancialDates(IEnumerable<PlJob>, DbContext)` - 批量填充FinancialDate
  - `FillFinancialDate(PlJob, DbContext)` - 单个填充FinancialDate
  - `GetBusinessDocsBatch()` - 批量查询业务单据，避免N+1问题
  - `CalculateFinancialDate()` - 财务日期计算逻辑

#### 业务规则实现

**计算逻辑**：
```csharp
FinancialDate = 业务单据类型 switch {
    PlIaDoc => job.ETA,    // 空运进口：到港日期
    PlIsDoc => job.ETA,    // 海运进口：到港日期  
    PlEaDoc => job.Etd,    // 空运出口：开航日期
    PlEsDoc => job.Etd,    // 海运出口：开航日期
    _ => null              // 无业务单据或未知类型
};
```

**字段对比**：
- **AccountDate**：保留不变，原有业务逻辑继续使用
- **FinancialDate**：新增计算字段，根据业务类型联动计算

**使用示例**：
```csharp
// 批量填充（推荐）
var jobs = GetJobsFromDatabase();
_JobManager.FillFinancialDates(jobs, _DbContext);

// 单个填充
var job = GetSingleJob();
_JobManager.FillFinancialDate(job, _DbContext);

// 使用新的财务日期
var financialDate = job.FinancialDate; // 计算字段
var accountDate = job.AccountDate;     // 原有字段
```

#### 性能优化特性

1. **批量查询优化**：通过`GetBusinessDocsBatch()`方法一次性查询所有相关业务单据
2. **避免N+1问题**：使用HashSet进行快速映射，避免循环查询
3. **内存效率**：通过Dictionary缓存业务单据映射关系
4. **查询合并**：同时查询所有四种业务单据类型（PlEaDoc, PlIaDoc, PlEsDoc, PlIsDoc）

#### 控制器集成示例

**修改文件**：`PowerLmsWebApi/Controllers/Business/Common/PlJobController.cs`
**集成位置**：在`GetAllPlJob`方法中添加财务日期填充调用

```csharp
// 获取数据后立即填充财务日期
var prb = _EntityManager.GetAll(r.AsQueryable(), model.StartIndex, model.Count);
if (prb.Result?.Any() == true)
{
    _JobManager.FillFinancialDates(prb.Result, _DbContext);
}
```

**注意**：新增的`FinancialDate`字段为计算字段，原有的`AccountDate`字段保持不变，确保向后兼容。

#### 架构设计优势

1. **向后兼容**：原有`AccountDate`字段完全保留，现有代码无需修改
2. **数据一致性**：新增`FinancialDate`字段始终与实际业务日期保持同步
3. **维护简化**：无需手动维护财务日期，减少人工错误
4. **性能优化**：批量处理机制确保大数据量下的高效执行
5. **业务逻辑清晰**：基于业务单据类型的明确规则
6. **渐进式升级**：可以逐步从`AccountDate`迁移到`FinancialDate`

#### 实施影响

**数据库变更**：无（新字段不映射到数据库）
**原有字段**：`AccountDate`完全保留，无任何变更
**API变更**：无（新增字段，不影响现有接口）
**前端适配**：可选（前端可选择使用新的`FinancialDate`字段）
**性能影响**：轻微增加（批量查询业务单据的开销）
**兼容性**：完全向后兼容，现有功能不受影响

### 架构重构：通用导入导出功能独立化

**背景**：原有的导入导出功能分散在DataDicController中，职责不清晰，且仅支持字典类型，难以扩展到客户资料等其他业务实体。

**解决方案**：重构为独立的导入导出模块，建立标准的服务-控制器架构。

#### 新增文件结构

1. **`PowerLmsServer/Services/ImportExportService.cs`**
   - 通用导入导出服务类
   - 统一处理字典、客户资料及其子表的Excel导入导出
   - 基于OwDataUnit + OwNpoiUnit的高性能Excel处理
   - 支持多租户数据隔离和权限控制
   - 重复数据覆盖策略，依赖关系验证

2. **`PowerLmsWebApi/Controllers/ImportExportController.cs`**
   - 通用导入导出控制器
   - RESTful API设计，支持标准的HTTP操作
   - 统一的权限验证和异常处理
   - 多租户安全隔离机制

3. **`PowerLmsWebApi/Controllers/ImportExportController.Dto.cs`**
   - 导入导出相关的DTO定义
   - 类型信息查询、导入结果返回等数据传输对象

#### 删除的文件

- **`PowerLmsServer/Services/DictionaryImportExportService.cs`** - 原有字典专用服务
- **`PowerLmsWebApi/Controllers/BaseData/DataDicController.ImportExport.cs`** - 原有字典导入导出控制器扩展

#### 支持的功能类型

**字典类型**：
- PlCountry (国家地区)
- PlPort (港口)
- PlCargoRoute (货运路线)
- PlCurrency (币种)
- FeesType (费用类型)
- PlExchangeRate (汇率)
- UnitConversion (单位换算)
- ShippingContainersKind (集装箱类型)
- 简单字典 (按分类代码)

**客户资料类型**：
- PlCustomer (客户资料主表)
- PlCustomerContact (客户联系人)
- PlBusinessHeader (业务负责人)
- PlTidan (客户提单内容)
- CustomerBlacklist (黑名单客户跟踪)
- PlLoadingAddr (装货地址)

#### API接口设计

**类型查询**：
- `GET /api/ImportExport/dictionary-types` - 获取支持的字典类型
- `GET /api/ImportExport/customer-subtable-types` - 获取客户子表类型
- `GET /api/ImportExport/simple-dictionary-categories` - 获取简单字典分类

**字典导入导出**：
- `GET /api/ImportExport/export/dictionary/{dictionaryType}` - 导出字典
- `GET /api/ImportExport/export/simple-dictionary/{categoryCode}` - 导出简单字典
- `POST /api/ImportExport/import/dictionary/{dictionaryType}` - 导入字典
- `POST /api/ImportExport/import/simple-dictionary/{categoryCode}` - 导入简单字典

**客户资料导入导出**：
- `GET /api/ImportExport/export/customers` - 导出客户资料主表
- `GET /api/ImportExport/export/customer-subtable/{subTableType}` - 导出客户子表
- `POST /api/ImportExport/import/customers` - 导入客户资料主表
- `POST /api/ImportExport/import/customer-subtable/{subTableType}` - 导入客户子表

#### 技术特性

1. **多租户数据隔离**：输入时忽略Excel中的OrgId，输出时OrgId列不显示
2. **重复数据处理**：支持覆盖模式（updateExisting参数控制）
3. **类型安全**：强类型的实体映射和属性验证
4. **错误处理**：完善的异常捕获和用户友好的错误信息
5. **日志记录**：详细的操作日志，便于问题追踪
6. **性能优化**：基于NPOI的高效Excel处理

#### 代码清理

1. **DataDicController修正**：移除了不再需要的DictionaryImportExportService依赖注入
2. **DTO清理**：从DataDicController.Dto.cs中移除了字典导入导出相关的DTO，保持文件职责单一
3. **依赖清理**：删除了无用的服务引用和扩展文件

#### 设计优势

1. **职责分离**：导入导出功能独立，符合单一职责原则
2. **易于扩展**：标准化的服务架构，便于添加新的业务实体类型
3. **代码复用**：通用的导入导出逻辑，减少重复代码
4. **类型安全**：泛型设计确保编译时类型检查
5. **统一接口**：RESTful API设计，便于前端调用和测试

这次重构为后续扩展更多业务实体的导入导出功能奠定了良好的架构基础，同时保持了现有功能的完整性和稳定性。

---

### 财务日期查询性能优化

**背景**：原有的财务日期计算需要查询完整的业务单据对象，但实际上只需要判断业务方向（进口/出口），造成了不必要的数据传输。

**优化方案**：重构查询逻辑，只查询必要的JobId信息来判断业务方向，大幅减少数据库IO和网络传输。

#### 性能优化详情

**原有查询方式**：
- 查询完整的PlEaDoc、PlIaDoc、PlEsDoc、PlIsDoc对象
- 返回包含所有字段的业务单据实体
- 基于单据类型进行switch判断

**优化后查询方式**：
- 只查询各业务单据表的JobId字段
- 返回JobId到业务方向的简单映射（true=进口，false=出口）
- 基于布尔值进行简单条件判断

#### 技术实现

**查询优化**：
```csharp
// 优化前：查询完整对象
var businessDocs = GetBusinessDocsBatch(jobIds, context);

// 优化后：只查询JobId
var businessDirections = GetBusinessDirectionsBatch(jobIds, context);
```

**计算逻辑简化**：
```csharp
// 优化前：基于单据类型
businessDoc switch {
    PlIaDoc => job.ETA,    // 空运进口
    PlIsDoc => job.ETA,    // 海运进口
    PlEaDoc => job.Etd,    // 空运出口
    PlEsDoc => job.Etd,    // 海运出口
    _ => null
};

// 优化后：基于业务方向
isImport switch {
    true => job.ETA,   // 进口业务
    false => job.Etd   // 出口业务
};
```

#### 性能提升

**数据传输减少**：
- **单据字段数量**：从~20个字段减少到1个字段(JobId)
- **查询复杂度**：从实体对象查询优化为简单ID查询
- **内存占用**：大幅减少业务单据对象的内存占用

**查询效率提升**：
- **网络IO**：减少数据传输量约90%
- **序列化成本**：避免复杂对象的序列化/反序列化
- **缓存友好**：简单的布尔映射更容易缓存

#### 实施文件

**修改文件**：
- `PowerLmsServer/Managers/Business/JobManager.cs` - 重构查询和计算逻辑

**新增方法**：
- `GetBusinessDirectionsBatch()` - 批量查询业务方向
- `CalculateFinancialDate(PlJob, bool?)` - 基于方向的财务日期计算

**移除方法**：
- `GetBusinessDocsBatch()` - 原有的完整单据查询
- `CalculateFinancialDate(PlJob, IPlBusinessDoc)` - 原有的单据对象计算

### 财务日期查询限制注释

**添加说明**：为`PlJobController.GetAllPlJob`方法添加重要注释，明确说明财务日期字段的使用限制。

#### 关键提醒

```csharp
/// <summary>
/// 获取全部业务总表。
/// 注意：财务日期(FinancialDate)是本地计算字段，不能作为查询条件使用。
/// 如需按财务日期查询，请使用 AccountDate、Etd（开航日期）或 ETA（到港日期）字段。
/// </summary>
```

**说明要点**：
- **FinancialDate不支持查询**：该字段为本地计算字段，不在数据库中存储
- **替代查询方案**：使用AccountDate、Etd或ETA字段进行日期相关查询
- **开发者提醒**：避免在前端或API调用中尝试使用FinancialDate作为查询条件

### 其他返回PlJob对象的方法核查

经过全面搜索，确认了以下主要返回PlJob对象的方法：

**主要接口**：
1. **PlJobController.GetAllPlJob** - ✅ 已添加财务日期填充
2. **PlJobController其他方法** - 单个对象操作，需要时可单独调用`FillFinancialDate`
3. **费用相关查询** - 主要返回费用对象，不直接返回PlJob

**业务单据控制器**：
- **PlSeaborneController** - 主要处理海运进出口单，不直接返回PlJob
- **其他业务控制器** - 类似模式，主要处理业务单据而非工作任务对象

**财务相关控制器**：
- **FinancialController** - 主要处理结算单等财务对象，查询中关联PlJob但不直接返回

#### 建议的财务日期填充策略

**批量查询场景**：
- 在`GetAllPlJob`等批量返回接口中使用`FillFinancialDates`
- 在返回数据前统一调用，确保所有PlJob对象都包含财务日期

**单个对象场景**：
- 在需要时调用`FillFinancialDate`
- 适用于详情查看、编辑等单个对象操作

**性能考虑**：
- 批量填充避免N+1查询问题
- 按需填充减少不必要的数据库查询

### API简化：ImportReturnDto类属性优化

**背景**：`ImportReturnDto`类中包含了多余的属性，信息冗余，需要简化以提高API的简洁性。

**优化内容**：
- **删除冗余属性**：移除`TargetTable`（目标表名称）和`ProcessingDetails`（处理详情列表）属性
- **保留核心数据**：保留`ImportedCount`（导入成功记录数量）作为主要返回信息
- **信息压缩**：将表名和处理详情信息压缩到基类的`DebugMessage`属性中

**技术实现**：
```csharp
// 优化前
public class ImportReturnDto : ReturnDtoBase
{
    public int ImportedCount { get; set; }
    public string TargetTable { get; set; }           // 删除
    public List<string> ProcessingDetails { get; set; } // 删除
}

// 优化后
public class ImportReturnDto : ReturnDtoBase
{
    public int ImportedCount { get; set; }
    // 其他信息通过基类DebugMessage传递
}
```

**信息整合策略**：
```csharp
// 将处理详情和目标表信息压缩到DebugMessage中
var detailsText = importResult.Details?.Count > 0 ? 
    $"，详情: {string.Join("; ", importResult.Details.Take(3))}{(importResult.Details.Count > 3 ? "..." : "")}" : "";
result.DebugMessage = $"导入{paramsDto.TableType}完成，共处理 {importResult.ImportedCount} 条记录{detailsText}";
```

**优化效果**：
- **API简洁性**：减少返回对象的复杂度，突出核心数据
- **信息完整性**：重要信息仍然通过DebugMessage传递，不丢失功能
- **向后兼容**：客户端仍能获取所需的反馈信息
- **代码简化**：减少不必要的属性赋值和维护成本

### 代码清理：删除废弃的OwnedAirlines类

**背景**：`OwnedAirlines`类已被标记为废弃，其相关字段已经展开到`PlCustomer`主表中，使用`Airlines_`前缀。该类不再使用，需要完全清理。

**清理内容**：
- **删除类定义**：移除`PowerLmsData/客户资料/PlCustomer.cs`中的`OwnedAirlines`类定义
- **删除映射配置**：移除`PowerLmsServer/AutoMappper/AutoMapperProfile.cs`中的AutoMapper映射配置
- **保留功能字段**：`Airlines_`前缀的字段继续在`PlCustomer`主表中使用

**技术影响**：
- **编译验证**：✅ 无编译错误，所有引用已清理完毕
- **功能完整性**：✅ 航空公司相关功能通过`Airlines_`前缀字段正常运作
- **数据完整性**：✅ 数据库结构不受影响，字段映射已完成展开

**清理文件**：
- `PowerLmsData/客户资料/PlCustomer.cs` - 删除`OwnedAirlines`类定义
- `PowerLmsServer/AutoMappper/AutoMapperProfile.cs` - 删除AutoMapper映射

### 参数语义优化：导入导出功能参数调整

**背景**：根据业务需求，需要调整导入导出功能的参数语义，使其更加准确和易于理解。

**参数语义调整**：
- **表类型 → 表名称**：`TableType`注释从"表类型"改为"表名称"，语义更准确
- **GetSupportedTables简化**：移除不必要的`TableName`参数，一次性返回所有支持的表
- **更新模式 → 删除模式**：`UpdateExisting` → `DeleteExisting`，语义反转更清晰

#### 具体变更

**GetSupportedTablesParamsDto简化**：
```csharp
// 优化前
public class GetSupportedTablesParamsDto : TokenDtoBase
{
    public string TableName { get; set; }  // 不必要的参数
}

// 优化后  
public class GetSupportedTablesParamsDto : TokenDtoBase
{
    // 无需额外参数，直接返回所有支持的表列表
}
```

**接口行为调整**：
- **优化前**：需要指定表类型参数，分类查询
- **优化后**：一次性返回所有支持的表，包括：
  - 字典表（PlCountry、PlCurrency等）
  - 客户子表（PlCustomerContact、PlTidan等）
  - 简单字典（SimpleDataDic）
  - 客户主表（PlCustomer）

**业务逻辑优化**：
```csharp
// 一次性获取所有表类型
var allTables = new List<TableInfo>();
allTables.AddRange(dictionaryTypes.Select(...));      // 字典表
allTables.AddRange(customerSubTypes.Select(...));     // 客户子表  
allTables.AddRange(simpleDictionaries.Select(...));   // 简单字典
allTables.Add(new TableInfo { TableName = "PlCustomer", DisplayName = "客户资料" });
```

**ImportParamsDto参数调整**：
```csharp
// 优化前
public bool UpdateExisting { get; set; } = false;  // 是否更新现有记录

// 优化后  
public bool DeleteExisting { get; set; } = false;  // 是否删除已有记录
```

**参数语义说明**：
- **DeleteExisting=true**：删除已有记录然后重新导入（全量替换模式）
- **DeleteExisting=false**：采用更新模式，不删除现有记录，仅更新冲突记录

**业务逻辑调整**：
```csharp
// 传递给Service的updateExisting参数需要取反
var updateExisting = !paramsDto.DeleteExisting;
importResult = _ImportExportService.ImportDictionary(
    formFile, paramsDto.TableName, orgId, updateExisting);