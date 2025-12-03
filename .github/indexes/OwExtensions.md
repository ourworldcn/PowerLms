# 📦 OwExtensions 项目索引

> **项目路径**: `C:\Users\zc-home\source\ourworldcn\bak\OwExtensions\`  
> **项目类型**: .NET 6 类库  
> **角色定位**: **NPOI扩展库**，提供高性能Excel数据处理能力  
> **依赖关系**: 依赖 OwDbBase，被 PowerLmsServer 引用

---

## 🎯 项目概述

OwExtensions 是 PowerLms 解决方案中专门处理 **Excel 数据导入导出** 的扩展库：

- **核心功能**: 基于 NPOI 的 Excel 读写操作
- **性能优化**: 使用 PooledList 减少内存分配
- **技术特点**: 零装箱、流式处理、类型安全转换
- **应用场景**: 数据字典导入、客户资料导入、业务数据批量处理

---

## 📁 项目结构

```
OwExtensions/
└── NPOI/                         # NPOI 扩展模块
    ├── OwNpoiUnit.cs             # 核心工具类（高性能Excel处理）
    ├── OwNpoiDbUnit.cs           # 数据库集成扩展
    └── OwNpoiExtensions.cs       # 扩展方法集合
```

---

## 🔧 核心模块详解

### 📌 **OwNpoiUnit** (核心工具类)

**功能概述**：
提供高性能的 Excel 数据读写能力，支持零装箱和流式处理。

#### **主要方法**：

##### 1️⃣ **Excel → 实体集合**
```csharp
// ✅ 自动映射：Excel列名 → 实体属性
public static IEnumerable<T> GetSheet<T>(ISheet sheet) where T : class, new()
```

**功能**：
- 第一行作为表头，自动匹配实体属性
- 支持数值、字符串、日期、布尔值自动转换
- 忽略未匹配的列，允许部分字段映射

**使用示例**：
```csharp
var workbook = new HSSFWorkbook(fileStream);
var sheet = workbook.GetSheetAt(0);
var entities = OwNpoiUnit.GetSheet<PlCountry>(sheet);
```

---

##### 2️⃣ **Excel → JSON 流**
```csharp
// ✅ 高性能：直接写入流，零装箱
public static void WriteJsonToStream(ISheet sheet, int startIndex, Stream stream)
```

**功能**：
- 将 Excel 数据转换为 JSON 并写入流
- 使用 `WrapperStream` 确保流资源安全
- 零装箱技术，性能优异

---

##### 3️⃣ **实体集合 → Excel**
```csharp
// ✅ 类型安全：支持实体写入 Excel
public static void WriteToExcel<T>(IEnumerable<T> collection, string[] columnNames, ISheet sheet)
```

**功能**：
- 根据指定列名写入数据
- 自动处理各种数据类型
- 支持空集合（仅写入表头）

---

##### 4️⃣ **Excel → 字符串列表**
```csharp
// ✅ 通用：返回原始字符串数据
public static PooledList<PooledList<string>> GetStringList(ISheet sheet, out PooledList<string> columnHead)
```

**功能**：
- 第一行作为列头
- 返回所有数据行的字符串列表
- 使用 `PooledList` 减少内存分配

---

#### **性能优化特点**：
```csharp
✅ PooledList 对象池        - 减少 GC 压力
✅ 零装箱技术              - 直接写入流
✅ WrapperStream 流管理    - 确保资源安全
✅ 统一单元格值处理        - 代码复用
```

---

### 📌 **OwNpoiExtensions** (扩展方法)

**功能概述**：
为 NPOI 原生对象提供扩展方法，简化常用操作。

#### **主要扩展方法**：

```csharp
// ✅ 扩展：简化实体读取
public static void ReadEntities<T>(
    this ISheet sheet, 
    IList<T> collection, 
    string[] excludedProperties = null) where T : class, new()

// 功能：
// - 自动读取Sheet数据并填充到集合
// - 支持排除指定属性（如Id、OrgId）
// - 自动处理复杂类型
```

**应用场景**：
```csharp
// 在 ImportExportService 中的使用
var entities = new List<PlCountry>();
sheet.ReadEntities(entities, new[] { "Id", "OrgId" });
```

---

### 📌 **OwNpoiDbUnit** (数据库集成)

**功能概述**：
将 NPOI 与 Entity Framework Core 集成，提供批量导入导出能力。

**核心功能**：
- 批量导入数据到数据库
- 批量导出数据库数据到 Excel
- 事务支持和错误处理

---

## 🎯 核心设计理念

### 1️⃣ **高性能优先**
- ✅ 使用 `PooledList` 代替 `List`
- ✅ 零装箱技术减少内存分配
- ✅ 流式处理大文件

### 2️⃣ **类型安全**
- ✅ 泛型方法支持强类型转换
- ✅ 自动类型推断和转换
- ✅ 详细的异常信息

### 3️⃣ **资源管理**
- ✅ `WrapperStream` 确保流不被意外关闭
- ✅ `PooledList` 自动归还对象池
- ✅ `using` 语句确保资源释放

### 4️⃣ **错误处理**
- ✅ 详细的异常信息（行号、列号、原因）
- ✅ 区分空值、类型错误、公式错误
- ✅ 提供有用的错误提示

---

## 📋 应用场景

### 🔄 **数据字典导入**
```csharp
// PowerLmsServer/Services/ImportExportService.cs
private List<T> ReadEntitiesFromSheet<T>(ISheet sheet, Guid? orgId) where T : class, new()
{
    var entities = new List<T>();
    sheet.ReadEntities(entities, new[] { "Id", "OrgId" });
    // 自动设置 OrgId
    foreach (var entity in entities)
    {
        SetOrgId(entity, orgId);
    }
    return entities;
}
```

### 📤 **数据导出**
```csharp
// PowerLmsServer/Services/ImportExportService.cs
private int ExportEntityData<T>(ISheet sheet, Guid? orgId) where T : class
{
    var data = GetEntityDataByOrgId<T>(orgId);
    var properties = GetExportProperties<T>();
    var columnNames = properties.Select(p => p.Name).ToArray();
    
    OwNpoiUnit.WriteToExcel(data, columnNames, sheet);
    return data.Count;
}
```

---

## 📦 NuGet 依赖

```xml
<ItemGroup>
  <PackageReference Include="NPOI" Version="2.6.2" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="6.0.36" />
</ItemGroup>
```

---

## ⚠️ 注意事项

### 1️⃣ **单元格类型处理**
- ⚠️ 公式单元格使用 `CachedFormulaResultType` 获取值
- ⚠️ 日期单元格需检查 `DateUtil.IsCellDateFormatted(cell)`
- ⚠️ 错误单元格会抛出 `InvalidOperationException`

### 2️⃣ **性能优化建议**
- ✅ 大文件使用流式处理（`WriteJsonToStream`）
- ✅ 频繁操作使用 `PooledList`
- ✅ 避免重复读取同一 Sheet

### 3️⃣ **内存管理**
- ✅ `PooledList` 使用后主动 `Dispose()`
- ✅ `WrapperStream` 确保流不被意外关闭
- ✅ 大数据处理分批进行

### 4️⃣ **错误处理**
```csharp
try
{
    var entities = OwNpoiUnit.GetSheet<PlCountry>(sheet);
}
catch (InvalidOperationException ex)
{
    // 处理行列级别的错误
    // 错误信息包含：工作表名、行号、列号、错误原因
}
```

---

## 🔄 与上层项目的关系

```
PowerLmsServer (业务层)
    ↓ 使用
OwExtensions (Excel扩展)
    ↓ 引用
OwDbBase (数据库基础设施)
    ↓ 引用
OwBaseCore (核心基础设施)
```

**关键依赖点**：
- `OwNpoiUnit` → ImportExportService (数据导入导出)
- `PooledList` → 高性能集合处理
- `WrapperStream` → 流资源管理

---

## 📊 统计信息

- **文件总数**: 3 个 C# 文件
- **核心类**: 3 个 (OwNpoiUnit, OwNpoiExtensions, OwNpoiDbUnit)
- **主要方法**: 10+ 个核心方法
- **依赖包**: 2 个 NuGet 包

---

## 🔗 相关索引

- [OwBaseCore 索引](OwBaseCore.md) - 基础设施核心
- [OwDbBase 索引](OwDbBase.md) - 数据库基础设施
- [PowerLmsServer 索引](PowerLmsServer.md) - 业务层

---

**最后更新**: 2025-01-30  
**维护者**: AI 自动生成 + 人工校验  
**用途**: AI 上下文优化、开发者快速定位、新人入职引导
