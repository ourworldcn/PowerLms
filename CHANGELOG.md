# 📝 PowerLMS 变更日志

## [未发布] - 2025-01-27

### 🔍 问题诊断

#### 基础资料覆盖导入失败 [BUG #1]
**状态**: ❌ **未修复** - 已完成深度检查，待实施修复方案

**问题根因**:
- `OwDataUnit.BulkInsert` 在 `ignoreExisting=false` 时调用 `BulkInsertOrUpdate`
- 实际行为: **UPSERT** (插入或更新)
- 用户预期: **覆盖** (先删除，再插入)
- 导致Excel中未包含的旧数据无法被删除

**技术细节**:
```csharp
// 当前实现（问题所在）
if (ignoreExisting)
    dbContext.BulkInsert(entityList, bulkConfig);
else
    dbContext.BulkInsertOrUpdate(entityList, bulkConfig); // ❌ 不会删除未包含的旧数据
```

**修复方案**:
1. 查询并删除已存在的记录
2. 批量插入新数据
3. 使用两次 `SaveChanges` 确保事务一致性

**影响范围**:
- 所有使用 `ImportDictionaries` 的基础资料导入功能
- PlCountry, PlPort, PlCargoRoute, PlCurrency, FeesType 等12个实体类型

**参考文档**: 详见 `临时输出.md` 深度检查报告

---

## API变更 (面向前端)

### 待修复接口

#### `POST /api/ImportExport/ImportDictionaries`
**当前问题**: `updateExisting=true` 不会删除Excel中未包含的旧数据

**修复后行为**:
- `updateExisting=true`: 删除已存在的记录 + 插入新数据（真正的覆盖）
- `updateExisting=false`: 仅插入新数据，跳过已存在记录（追加模式）

**前端无需修改**: 参数传递逻辑正确，仅后端逻辑需调整

---

## 技术债务

### 编译错误残留
**文件**: `..\Bak\OwDbBase\Data\OwDataUnit.cs`
**行数**: 171  
**问题**: 不完整的语句 `dbContext.UpdateRange`  
**优先级**: P0 - 立即修复  

---

## 下一步计划

### 本周任务
1. ✅ 完成基础资料覆盖导入BUG的深度检查
2. ⏳ 实施修复方案（预计2小时）
3. ⏳ 编写单元测试验证修复效果
4. ⏳ 测试环境验证（石永昌）

### 后续优化
- 添加导入预览功能（显示将新增/更新/删除的记录数）
- 优化批量删除性能（使用 `BulkDelete`）

---

**检查人**: GitHub Copilot  
**检查时间**: 2025-01-27  
