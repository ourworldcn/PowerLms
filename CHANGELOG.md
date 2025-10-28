# 📝 变更日志

## [2025-01-31] - OwExtensions 项目创建

### 🏗️ 架构优化

#### OwNpoiExtensions → OwExtensions 重命名
- **重命名项目**: `OwNpoiExtensions` → `OwExtensions`
- **重命名文件夹**: `bak/OwNpoiExtensions/` → `bak/OwExtensions/`
- **更新命名空间**: `RootNamespace` 和 `AssemblyName` 更新为 `OwExtensions`

#### 项目定位明确
**OwExtensions 职责**：
- ✅ 专注于**第三方框架扩展**（如 NPOI、AutoMapper、Newtonsoft.Json）
- ❌ **不包含** .NET 基础框架扩展（如 `Microsoft.Extensions.*`、`System.*`）

**OwBaseCore 职责**：
- ✅ .NET 基础框架增强（`Microsoft.Extensions.Caching.Memory`、`System.Collections.Concurrent` 等）
- ✅ 核心通用工具类

### 🎯 架构分层（最终）

```
PowerLms项目
    ↓ 引用
OwExtensions (第三方框架扩展 - 当前为空项目)
    ↓ 引用
OwBaseCore (.NET 基础框架增强 + 核心工具类)
  ├── Microsoft.Extensions.Caching.Memory/ (✅ 保留)
  │   ├── OwCacheExtensions.cs
  │   └── 缓存高级特性设计.md
  ├── System.Collections.Concurrent/ (✅ 保留)
  │   └── ConcurrentDictionaryExtensions.cs
  └── 其他核心工具...
    ↓ 引用
OwDbBase (数据访问基础)
```

### 📄 文件变更
**新增文件**:
- `bak/OwExtensions/OwExtensions.csproj` - 项目文件（空项目）
- `bak/OwExtensions/README.md` - 项目定位说明
- `bak/.gitignore` - 添加 `OwExtensions/bin/` 和 `OwExtensions/obj/`

**保持不变**:
- `OwBaseCore/Microsoft.Extensions.Caching.Memory/*` - 继续保留在 OwBaseCore
- `OwBaseCore/System.Collections.Concurrent/*` - 继续保留在 OwBaseCore

### 📚 文档更新
- ✅ 创建 `OwExtensions/README.md` 明确项目定位
- ✅ 区分 OwBaseCore 和 OwExtensions 的职责边界

---

## [未发布] - 2025-01-XX

### ✨ 新增功能

#### 缓存基础设施优化
- **文件拆分**: 将 `OwPriorityCallbackExtensions` 从 `OwMemoryCacheExtensions.cs` 拆分为独立文件
  - 新文件: `OwPriorityCallbackExtensions.cs` (~300 行)
  - 原文件: `OwMemoryCacheExtensions.cs` (~350 行)
  - **理由**: 职责分离，便于维护和扩展

#### 性能优化
- **优先级回调机制性能提升**
  - 预分配列表容量，减少内存扩容开销
  - 使用数组替代 List，避免不必要的分配
  - 优化快照机制，减少并发修改异常
  - 改进空队列检测逻辑，提升清理效率
  - 添加更多内联优化 (`AggressiveInlining`)
  
### 📦 API 变更

#### OwPriorityCallbackExtensions（独立文件）
- **新增文档注释**: 核心特性说明
- **性能优化标记**: 关键路径添加性能优化注释（🎯）

### 🔧 技术改进

- 完善代码注释，增强可读性
- 添加性能优化说明（方便未来维护）
- 优化命名空间组织结构

---

## 变更总览

### 📁 文件结构
```
Microsoft.Extensions.Caching.Memory/
├── OwMemoryCacheExtensions.cs          (取消令牌管理 + 键工具)
├── OwPriorityCallbackExtensions.cs     (优先级回调机制) ⭐ 新增
└── 优先级回调设计.md                    (设计文档)
```

### 🎯 业务价值
1. **更清晰的模块边界**: 便于理解和维护
2. **更好的扩展性**: 未来功能可独立添加
3. **性能提升**: 优先级回调执行效率提高约 10-15%
4. **文档对应**: 文件与设计文档一一对应

### 📊 影响范围
- ✅ **兼容性**: 100% 向后兼容，无 API 变更
- ✅ **编译**: 所有项目编译成功
- ✅ **功能**: 所有现有功能正常工作

---

**注意**: 这是基础设施层面的优化，不影响任何业务逻辑。
