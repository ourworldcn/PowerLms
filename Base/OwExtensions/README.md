# OwExtensions

**Ow系列基础库的第三方框架扩展包**

## 📋 项目定位

专门封装对**第三方框架和库**的增强功能（如 NPOI、AutoMapper、Newtonsoft.Json 等）。

> ⚠️ **重要区分**：
> - **OwBaseCore**：.NET 基础框架增强（如 `Microsoft.Extensions.*`、`System.*`）
> - **OwExtensions**：第三方框架增强（如 NPOI 等纯第三方库）
> - **OwDbBase**：数据访问基础（EF Core + NPOI集成 = OwDataUnit）

### 架构分层
```
OwDbBase (数据访问 - 含OwDataUnit批量操作)
    ↓ 引用
OwExtensions (NPOI纯Excel处理 - 含OwNpoiUnit)
↓ 引用
OwBaseCore (.NET 基础框架增强)
```

## 🔧 功能模块

### ✅ NPOI 扩展（Excel 处理）

#### `OwNpoiUnit` - 高性能Excel数据处理核心组件

**位置**：`OwExtensions\NPOI\OwNpoiUnit.cs`

```csharp
using NPOI.SS.UserModel;
using NPOI;

// 读取Excel为字符串列表（高性能PooledList）
using var allRows = OwNpoiUnit.GetStringList(sheet, out var columnHeaders);

// 读取Excel为实体集合
var entities = OwNpoiUnit.GetSheet<MyEntity>(sheet);

// 写入实体集合到Excel
OwNpoiUnit.WriteToExcel(entities, new[] { "Name", "Age" }, sheet);

// Excel转JSON流（零装箱）
OwNpoiUnit.WriteJsonToStream(sheet, startIndex: 0, stream);
```

**技术特点**：
- 使用PooledList减少内存分配
- 零装箱技术确保高性能
- WrapperStream确保流资源安全
- 统一的单元格值处理逻辑

**命名空间**：`NPOI`（与NPOI原库一致，扩展方法自动发现）

#### `OwDataUnit` - NPOI + EF Core 批量操作集成

**位置**：`OwDbBase\Data\OwDataUnit.cs`（因EF Core依赖放在OwDbBase）

```csharp
using OW.Data;
using Microsoft.EntityFrameworkCore;

// 从Excel批量导入到数据库
var count = OwDataUnit.BulkInsert<MyEntity>(
 sheet, dbContext, ignoreExisting: true);

// 批量插入实体集合
var count = OwDataUnit.BulkInsert(
    entities, dbContext, ignoreExisting: true);
```

**技术特点**：
- 使用EFCore.BulkExtensions实现高性能批量操作
- 框架自动检测主键实现智能重复数据处理
- 支持泛型和非泛型的灵活调用方式
- 内存优化的字符串数组到实体转换

**命名空间**：`OW.Data`（位于OwDbBase项目）

### 🔜 未来可能的扩展方向

#### AutoMapper 扩展
提供常用的对象映射配置和辅助方法。

#### Newtonsoft.Json 扩展
JSON 序列化/反序列化的增强功能。

## 📦 依赖项

### OwExtensions项目
- .NET 6+
- **NPOI** 2.6.2 (Excel处理)
- OwBaseCore (Ow系列核心工具库)

### OwDbBase项目（包含OwDataUnit）
- .NET 6+
- **EFCore.BulkExtensions.SqlServer** 6.8.1 (批量操作)
- **NPOI** 2.6.2 (通过OwExtensions传递)
- OwExtensions (NPOI扩展)
- OwBaseCore (核心工具库)

## 🏗️ 开发原则

### 命名空间设计
保持与被扩展库一致的命名空间，便于自动发现：

```csharp
// ✅ OwExtensions - 与 NPOI 原库一致
namespace NPOI
{
    public static class OwNpoiUnit
    {
        // Excel处理扩展方法...
    }
}

// ✅ OwDbBase - 与 OW.Data 保持一致
namespace OW.Data
{
    public static class OwDataUnit
    {
        // EF Core + NPOI 批量操作...
    }
}
```

### 架构原则
- ✅ **OwExtensions**: 只依赖第三方库本身（如NPOI），不依赖EF Core
- ✅ **OwDbBase**: 集成EF Core + NPOI，提供批量数据库操作
- ✅ **职责分离**: Excel处理（OwExtensions） vs 数据库操作（OwDbBase）
- ✅ **避免循环引用**: OwDbBase → OwExtensions → OwBaseCore

### 代码规范
- ✅ **第三方库专注**：只封装第三方框架的增强
- ✅ **性能优先**：使用最佳实践和性能优化
- ✅ **独立可复用**：不依赖业务项目（如PowerLms）
- ✅ **文档完善**：详细的 XML 注释、设计文档、使用示例

## 📝 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| 2.1 | 2025-02-05 | **架构调整**：解决循环引用问题 |
| | | - OwDataUnit移回OwDbBase（因EF Core依赖） |
| | | - OwNpoiUnit保留在OwExtensions（纯NPOI扩展） |
| | | - 移除OwExtensions对OwDbBase的引用 |
| 2.0 | 2025-02-05 | **重大重构**：从业务项目独立为通用基础库 |
| | | - 移入OwNpoiUnit（从OwBaseCore） |
| | | - 移入OwDataUnit（从OwDbBase） |
| | | - 修正所有文件头注释（移除PowerLms引用） |
| | | - 移除OwBaseCore的NPOI包引用 |
| 1.0 | 2025-01-31 | 从 OwNpoiExtensions 重构为 OwExtensions |
| | | 明确项目定位：第三方框架扩展 |

## 📄 许可证

MIT License

---

**维护者**：Ow系列基础库开发团队  
**最后更新**：2025-02-05
