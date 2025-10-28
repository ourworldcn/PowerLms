# OwExtensions

**Ow系列基础库的第三方框架扩展包**

## 📋 项目定位

专门封装对**第三方框架和库**的增强功能（如 NPOI、AutoMapper、Newtonsoft.Json 等）。

> ⚠️ **重要区分**：
> - **OwBaseCore**：.NET 基础框架增强（如 `Microsoft.Extensions.*`、`System.*`）
> - **OwExtensions**：第三方框架增强（如 NPOI、其他第三方库）

### 架构分层
```
PowerLms项目
    ↓ 引用
OwExtensions (第三方框架扩展)
    ↓ 引用
OwBaseCore (.NET 基础框架增强 + 核心工具类)
    ↓ 引用
OwDbBase (数据访问基础)
```

## 🔧 功能模块

> 当前项目为空，等待第三方库扩展功能的添加。

### 未来可能的扩展方向

#### NPOI 扩展（Excel 处理）
增强 NPOI 库的功能，提供更便捷的 Excel 读写操作。

```csharp
// 示例：未来可能的扩展
public static class NpoiExtensions
{
    public static void AutoSizeColumns(this ISheet sheet)
    {
        // 自动调整列宽
    }
}
```

#### AutoMapper 扩展
提供常用的对象映射配置和辅助方法。

#### Newtonsoft.Json 扩展
JSON 序列化/反序列化的增强功能。

## 📦 依赖项

- .NET 6+
- OwDbBase (Ow系列基础库)

## 🏗️ 开发原则

### 命名空间设计
保持与被扩展库一致的命名空间，便于自动发现：

```csharp
// 示例
namespace NPOI.SS.UserModel  // ✅ 与 NPOI 原库一致
{
    public static class NpoiExtensions
    {
        // 扩展方法...
    }
}
```

### 代码规范
- ✅ **第三方库专注**：只封装第三方框架的增强
- ✅ **性能优先**：使用最佳实践和性能优化
- ✅ **文档完善**：详细的 XML 注释、设计文档、使用示例

## 📝 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| 1.0 | 2025-01-31 | 从 OwNpoiExtensions 重构为 OwExtensions |
| | | 明确项目定位：第三方框架扩展 |

## 📄 许可证

MIT License

---

**维护者**：Ow系列基础库开发团队  
**最后更新**：2025-01-31
