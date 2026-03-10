# 📋 TODO - 待办事项

> 来源：2026-03-08 会议纪要 | 负责人：ZC@WorkGroup（后端）

---

## 🔴 1 紧急任务

### 1.1 替换开发服务器SSL证书
- **内容**：开发服务器(IP.101)的SSL证书3月26日到期，已提供新证书（6月到期），需替换
- **协作**：前端(陈云霄)同步替换
- **截止**：尽快
- **性质**：运维操作，非代码任务

### 1.2 部署最新版本到开发和测试服务器
- **内容**：部署当前最新版本，以便下周开始内部测试
- **测试范围**：空运主分单、海运提单(主/分单)、进口仓单
- **截止**：周日
- **性质**：运维操作，非代码任务

---

## 📌 2 计划任务

### 2.1 ⭐ 报关模块专用字典表（核心开发任务）

#### 2.1.1 业务背景
报关模块需要一系列独立的专用字典表，不混入通用的"简单字典"。权限统一使用 `B.14`（报关基础字典，权限.md中已存在）。

#### 2.1.2 需创建的字典表清单

| 序号 | 字典名称 | 建议实体名 | 说明 |
|------|---------|-----------|------|
| 1 | 成交方式 | CdDealKind | 海关成交方式代码 |
| 2 | 征免性质 | CdTaxExemptionKind | 征免性质代码 |
| 3 | 征免方式 | CdTaxCollectionKind | 征免方式代码 |
| 4 | 运输方式 | CdTransportKind | 海关运输方式代码 |
| 5 | 监管方式 | CdSupervisionKind | 海关监管方式代码 |
| 6 | 报关专用港口 | CdCustomsPort | 海关特定代码体系（4位或6位），与通用港口(PlPort)不同 |
| 7 | 运输工具 | CdTransportTool | 报关专用运输工具字典 |

#### 2.1.3 实现要求
- 每个字典创建独立的表，继承 `NamedSpecialDataDicBase`（与币种PlCurrency一致）
- 提供标准CRUD操作（查/增/改/删/恢复），实现方式参考币种控制器（AdminController.Base.cs）
- 权限码统一为 `B.14`
- 需在 DbContext 中注册 DbSet
- 需在 `ImportExportService.IsIndependentDictionaryEntity()` 中添加支持，以实现批量导入/导出
- **数据结构详情**：待永昌石周一前提供

#### 2.1.4 技术实施步骤（待数据结构确认后执行）
1. 在 `PowerLmsData/基础数据/` 下创建7个实体类文件
2. 在 `PowerLmsUserDbContext.cs` 中添加7个 DbSet
3. 在 `AdminController.Base.cs` 中添加CRUD操作（或新建专用控制器）
4. 在 `AdminController.Dto.cs` 中添加对应DTO
5. 在 `ImportExportService.IsIndependentDictionaryEntity()` 中注册新实体
6. 编译验证

#### 2.1.5 当前状态
- ✅ **已完成**：6个实体类已创建（`PowerLmsData/基础数据/CustomsDictionaries.cs`）
- ✅ **已完成**：DbContext 已注册6个 DbSet（前缀 `CD_`）
- ✅ **已完成**：ImportExportService 已支持批量导入/导出
- ⏳ **等待**：永昌石提供详细表结构和数据，确认字段后补充（截止：周一）
- ⏳ **待做**：添加CRUD接口（AdminController 或新建控制器）
- ⏳ **待做**：数据库迁移（手工执行）
- 报关单主表实体（CustomsDeclaration.cs）和货物明细实体（CustomsGoodsList.cs）已存在
- 报关单控制器已存在（CustomsDeclarationController、CustomsGoodsListController）

---

## ⏸️ 3 暂缓任务

（暂无）
