# PooledList<T> 设计与实现文档

## 1. 设计目标

- 实现一个类似 List<T> 的泛型集合类 PooledList<T>，其内部存储数组全部通过 ArrayPool<T>.Shared 进行池化分配与回收，以减少频繁分配大数组带来的 GC 压力和内存碎片，适用于高性能场景下大量临时数据的收集与处理。
- 应说明其应用场景，以避免对长期存在的对象使用该类。
---

## 2. 主要功能需求

- **通用泛型集合**：支持 Add、Insert、Remove、Clear、索引器、Count、Capacity 等常用操作
- **自动扩容与裁剪**：数据超出当前容量时自动扩容，不再使用时可主动裁剪
- **池化内存管理**：所有内部数组通过 ArrayPool<T>.Shared.Rent/Return 管理
- **Dispose 支持**：实现 IDisposable，归还内部数组资源
- **数据安全**：归还前清理未用槽位，防止数据泄漏
- **接口兼容**：尽量与 List<T> API 保持一致，公有成员要一致，方便迁移和替换
- **支持枚举**：实现 IEnumerable<T>，支持 foreach
- **基类**：从Collection<T>派生。
- **代码规范**：删除函数内不必要的空行以减少代码行数。注释尽量写在行尾以减少代码行数。但成员的注释要完整。


---

## 3. 设计要点与实现细节

### 3.1 字段与基本结构

- `private T[] _items;` —— 当前池租用的存储数组
- `private int _count;` —— 当前元素数量
- `private bool _disposed;` —— 防止重复释放
- 默认最小容量可设为 4 或由用户指定

### 3.2 构造与初始化

- 支持默认与自定义初始容量
- 初始化时通过 `ArrayPool<T>.Shared.Rent(capacity)` 获取数组

### 3.3 添加/插入/扩容

- 添加元素时，如果容量不足，自动扩容（如翻倍增长或跟随 List<T> 策略）
- 扩容时，租用新数组，将原有数据拷贝到新数组，归还旧数组

### 3.4 删除/裁剪/归还

- 支持 Remove、RemoveAt、Clear 等操作
- Clear/Dispose/TrimExcess 时将不用的数组归还池
- 归还前，将未用元素设为 default(T)，防止数据泄漏

### 3.5 Dispose 模式

- 实现 IDisposable，Dispose 时归还数组并置空引用，防止再次访问

### 3.6 枚举器

- 实现 IEnumerable<T> 以支持 foreach

---

## 4. API 设计示例

```csharp
public sealed class PooledList<T> : IDisposable, IEnumerable<T>
{
    public PooledList(int capacity = 4);
    public int Count { get; }
    public int Capacity { get; }
    public T this[int index] { get; set; }

    public void Add(T item);
    public void Insert(int index, T item);
    public bool Remove(T item);
    public void RemoveAt(int index);
    public void Clear();
    public void TrimExcess();

    public void Dispose();
    public IEnumerator<T> GetEnumerator();
}
```

---

## 5. 典型用法举例

```csharp
using var list = new PooledList<byte>(1024);
for (int i = 0; i < 10000; i++) list.Add((byte)i);
DoSomething(list);
// 退出 using 块或调用 Dispose 后，底层数组自动归还池
```

---

## 6. 注意事项与局限

- 归还数组后不得再访问
- 与 List<T> 不同，Dispose 后实例不可再用
- 不可跨线程访问（如需线程安全请自行加锁）
- 适用于短生命周期、大批量数据的场景，不建议长期持有

---

## 7. 单元测试建议

- 添加、删除、扩容、裁剪、Dispose 后行为
- 与 ArrayPool<T>.Shared 的正确交互
- 边界条件、异常等

---

## 8. 参考资料

- [List<T> 源码（.NET）](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs)
- [ArrayPool<T> 官方文档](https://learn.microsoft.com/zh-cn/dotnet/api/system.buffers.arraypool-1)
- [Why doesn't List<T> use ArrayPool<T>? (GitHub Issue)](https://github.com/dotnet/runtime/issues/21419)

---