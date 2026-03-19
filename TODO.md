# 📋 本周服务器端修改计划（2026-03-15 ~ 2026-03-21）

# 📋 TODO - 后端待办事项

> **项目**：PowerLmsServer.sln（.NET 6） | **角色**：ZC@WorkGroup（后端）

---

## 1 🔴 第一优先级

### 1.1 通用报表模板系统 - 后端实体与CRUD接口

**来源**：临时输入.md §6

#### 实体设计方案

继承 `JsonDynamicPropertyBase`（已含 `Id` + `JsonObjectString`），字段全部通用化：

| 字段名 | 类型 | 说明 |
|--------|------|------|
| `Id` | `Guid` | 主键（基类提供） |
| `JsonObjectString` | `string` | 内容字段，存储任意JSON字符串；对用户/调用方**可见**，由调用方自行解析和使用（基类提供） |
| `Genus` | `string` | 通用分类标识，最长128字符；含义由调用方自定义，可用于区分对象类型、业务分组等 |
| `ExtraString` | `string` | 通用扩展字符串，最长**128**字符 |
| `ExtraDateTime` | `DateTime?` | 通用扩展日期 |
| `ExtraGuid` | `Guid?` | 通用扩展Guid |
| `ParentId` | `Guid?` | 副ID；典型用途为存储**机构ID（OrgId）**，亦可指向任意关联对象，含义由调用方约定 |

> **设计说明**：
> - 所有字段均不预设业务含义，由调用方按需约定，实体本身保持通用
> - `JsonObjectString` 对外可读写，后端不解析其内容，前端/使用方自行处理
> - 需要超出上述字段的扩展数据，统一写入 `JsonObjectString`，实体层不再增加具名列

#### 实施步骤

1. 创建实体类 `PowerLmsData/报表/PlReportTemplate.cs`（继承 `JsonDynamicPropertyBase`）
   - 新增字段：`Genus`、`ExtraString`（MaxLength 128）、`ExtraDateTime`、`ExtraGuid`、`ParentId`
2. 注册DbSet `PowerLmsUserDbContext.cs`
3. 创建Controller `PowerLmsWebApi/Controllers/Report/ReportTemplateController.cs`（标准CRUD + 商管/超管权限）
4. 创建DTO `ReportTemplateController.Dto.cs`
5. ⚠️ 不生成数据库迁移

---

### 1.2 空运接口架构整理 - 消除EaDoc CRUD重复

**来源**：会议纪要§4.1

**现状**：`PlAirborneController.cs`（完整CRUD）与 `PlJobController.EaDoc.cs`（简陋重复）并存

**实施步骤**：
1. `PlJobController.EaDoc.cs` 中 EaDoc 方法添加 `[Obsolete]` 标记
2. 与前端确认迁移后删除重复方法（保留 EaDocItem 子表方法）
3. 编译验证

---

## 2 🟡 第二优先级

### 2.1 报关专用字典7个新表

**来源**：2026-03-08会议纪要

**待创建**：

| 实体名 | 中文名 | 基类 |
|--------|--------|------|
| CdDealKind | 成交方式 | NamedSpecialDataDicBase |
| CdTaxExemptionKind | 征免性质 | NamedSpecialDataDicBase |
| CdTaxCollectionKind | 征免方式 | NamedSpecialDataDicBase |
| CdTransportKind | 运输方式 | NamedSpecialDataDicBase |
| CdSupervisionKind | 监管方式 | NamedSpecialDataDicBase |
| CdCustomsPort | 报关专用港口 | NamedSpecialDataDicBase |
| CdTransportTool | 运输工具 | NamedSpecialDataDicBase |

**实施步骤**：
1. `PowerLmsData/报关/CustomsDictionaries.cs` 追加7个实体类
2. `PowerLmsUserDbContext.cs` 添加7个 DbSet
3. `CustomsDictionaryController.cs` 添加7组CRUD（参考CdHsCode）
4. `CustomsDictionaryController.Dto.cs` 添加DTO
5. `ImportExportService.cs` 注册新实体
6. ⚠️ 不生成数据库迁移

**阻塞项**：永昌石提供详细表结构和数据

---

## 3 🟢 第三优先级

### 3.1 报关单字段整理与补充

**来源**：临时输入.md §3

**现状**：`CustomsDeclaration.cs` 和控制器已存在，需按海关界面字段顺序整理并补充缺失字段

**阻塞项**：界面原型确认（字段编号33/39/55/65/118/131对应关系）

---

### 3.2 海运提单数据查询API增强

**来源**：临时输入.md §2+§4

**现状**：`EsMbl`（主提单）、`EsHbl`（分提单）实体已存在

**待做**：检查现有API是否满足套打报表取数需求，如缺少关联查询接口则补充

**阻塞项**：空单背景图与尺寸参数待提供

---

## 4 ⏸️ 暂缓

| 任务 | 原因 |
|------|------|
| 结算单导出到金蝶 | 决议暂缓 |

---

## 5 📝 风险提示

### 5.1 报表模板系统设计决策
- 先不加权限字段，模板类型先用字符串
- 背景图存URL，需确认上传接口路径策略
