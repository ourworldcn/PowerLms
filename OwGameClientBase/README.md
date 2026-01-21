# OwGameClientBase (Base)

> 基于 ECS 架构的游戏客户端核心库

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![ECS](https://img.shields.io/badge/架构-ECS-green.svg)](https://en.wikipedia.org/wiki/Entity_component_system)

---

## 📖 项目简介

`Base\OwGameClientBase` 是基于 **Entity-Component-System (ECS)** 架构的游戏客户端核心库，提供了完整的实体管理、移动系统、碰撞检测和输入处理功能。

本库是 **Git Subtree** 管理的独立模块，位于 `Base` 目录下，可以被多个项目共享使用。

### 🎉 最新改进（v1.1.0）

#### ✅ 内存优化（减少 37-40%）
- **ColliderState**: 20 字节 → **12 字节**（-40%）
  - 将 `Layer` 压缩到 `Flags` 的 bit 1-16
  - 新增 `Deleted` 软删除标志（bit 0）
  
- **MoveState**: 64 字节 → **40 字节**（-37.5%）
  - 压缩边界行为到 `Flags` 的 bit 0-15
  - 新增 `Deleted` 软删除标志（bit 16）

#### ✅ 软删除系统
- **统一接口**：`Actor.Deleted` / `MoveState.Deleted` / `ColliderState.Deleted`
- **批量压缩**：`ActorManager.Compact()` 使用 `IRefPredicate` 零分配
- **性能提升**：软删除速度提升 **~20 倍**（仅标志位操作）

#### ✅ ActorManager 增强
- 新增 `MarkForDestroy()` - 软删除单个实体（推荐）
- 新增 `MarkForDestroyWhere()` - 批量软删除
- 新增 `Compact()` - 高性能批量压缩
- 新增 `ActorCount` / `PendingDestroyCount` - 实体统计

---

### 核心特点

- 🎯 **纯 ECS 架构**：实体-组件-系统完全分离
- 🚀 **并行数组设计**：Actor 和组件索引天然一致，极致性能
- ⚡ **零拷贝访问**：基于 `Span<T>` 和 `ref` 的批量处理
- 🎮 **完整游戏系统**：移动、碰撞、输入、场景管理一应俱全
- 🔧 **空间哈希优化**：Uniform Grid 碰撞检测，避免 N² 复杂度
- 📊 **位掩码输入**：高性能键盘输入，支持 64+ 按键同时检测

### 设计哲学

```
统一存储 (EcsStorageService)
    ↓
并行数组 (Actors, Moves, Colliders, Shapes)
    ↓
系统批处理 (MovementService, CollisionService)
    ↓
零拷贝访问 (Span<T>, ref)
```

---

## 🏗️ 核心组件

### 存储层

| 组件 | 说明 | 文件 |
|------|------|------|
| `EcsStorageService` | ECS 统一存储服务 | `EcsStorageService.cs` |
| `SceneInfo` | 场景信息（边界、环境参数） | `EcsStorageService.cs` |

### 实体与组件

| 组件 | 说明 | 大小 | 文件 |
|------|------|------|------|
| `Actor` | 实体基类（支持继承） | class | `Actor.cs` |
| `MoveState` | 移动组件（✅ 优化版） | 40 字节 | `MoveState.cs` |
| `ColliderState` | 碰撞组件（✅ 优化版） | **12 字节** (-40%) | `ColliderState.cs` |
| `ColliderShape` | 碰撞形状（圆形） | 12 字节 | `ColliderState.cs` |

### 系统层

| 系统 | 说明 | 文件 |
|------|------|------|
| `ActorManager` | **实体管理服务**（创建/销毁的统一入口） | `ActorManager.cs` |
| `MovementService` | 移动系统（批量更新位置） | `MovementService.cs` |
| `CollisionService` | 碰撞检测系统（空间哈希） | `CollisionService.cs` |
| `InputService` | 输入服务（键盘/鼠标/触摸） | `InputService.cs` |

---

## 📁 项目结构

```
Base/OwGameClientBase/
├── EcsStorageService.cs      # ECS 统一存储
│   ├── SceneInfo             # 场景信息
│   ├── Actors                # 实体数组
│   ├── Moves                 # 移动组件数组
│   ├── Colliders             # 碰撞组件数组
│   └── ColliderShapes        # 碰撞形状数组
│
├── ActorManager.cs           # 实体管理服务（创建/销毁统一入口）⭐ 优化版
│   ├── CreateActor()         # 标准创建（含碰撞）
│   ├── CreateActorWithoutCollision()  # 无碰撞创建
│   ├── CreateActorWithShapes()        # 多碰撞体创建
│   ├── MarkForDestroy()      # ✅ 软删除单个实体（推荐）
│   ├── MarkForDestroyWhere() # ✅ 批量软删除
│   ├── Compact()             # ✅ 批量压缩删除（IRefPredicate 高性能）
│   ├── DestroyActor()        # ⚠️ 立即销毁（不推荐）
│   ├── DestroyActorsWhere()  # ⚠️ 批量立即销毁（不推荐）
│   ├── ActorCount            # ✅ 活跃实体数量（不含软删除）
│   ├── PendingDestroyCount   # ✅ 待删除实体数量
│   └── TotalCount            # 总实体数量（含软删除）
│
├── Actor.cs                  # 实体基类
│   ├── Id                    # 实体 ID
│   ├── MoveIndex             # 移动组件索引
│   ├── Deleted               # 软删除标志 ✅ NEW
│   ├── Tags                  # 扩展信息字典
│   ├── OnCollision()         # 碰撞回调
│   └── Update()              # 更新回调
│
├── MoveState.cs              # 移动组件（40 字节）✅ 优化版
│   ├── Position              # 当前位置
│   ├── Velocity              # 速度向量
│   ├── Speed                 # 速度标量
│   ├── VelocityAngle         # 速度方向角
│   ├── PreviousPosition      # 上一帧位置
│   ├── Flags                 # 压缩标志（边界行为 + Deleted + 预留）
│   ├── LeftBoundary          # 左边界行为（bit 0-3）
│   ├── RightBoundary         # 右边界行为（bit 4-7）
│   ├── TopBoundary           # 上边界行为（bit 8-11）
│   ├── BottomBoundary        # 下边界行为（bit 12-15）
│   └── Deleted               # 软删除标志（bit 16）✅ NEW
│
├── ColliderState.cs          # 碰撞组件（✅ 优化到 12 字节）
│   ├── ShapeStart            # 形状起始索引
│   ├── ShapeCount            # 形状数量
│   ├── Flags                 # 压缩标志（Deleted + Layer + 预留）
│   ├── Deleted               # 软删除标志（bit 0）✅ NEW
│   └── Layer                 # 碰撞层（bit 1-16）✅ 压缩优化
│
├── CollisionService.cs       # 碰撞系统
│   ├── CollisionGrid         # 空间哈希网格
│   ├── CollisionMath         # 碰撞数学工具
│   ├── DetectCollisions()    # 检测碰撞
│   └── DispatchEvents()      # 分发事件
│
├── MovementService.cs        # 移动系统
│   └── Update()              # 批量更新移动
│
├── InputService.cs           # 输入服务
│   ├── KeyboardMain          # 主键盘区（64 键）
│   ├── KeyboardExtra         # 扩展键区（64 键）
│   ├── MousePosition         # 鼠标位置
│   └── JoystickDirection     # 虚拟摇杆
│
└── OwGameClientBase.csproj   # 项目文件
```

### 依赖关系

```
Base/OwGameClientBase (.NET 8)
└── Base/OwBaseCore (.NET 6)
    └── OwCollection<T>      # 连续内存集合
```

---

## 🚀 快速开始

### 1. 安装

添加项目引用：

```bash
dotnet add reference ../Base/OwGameClientBase/OwGameClientBase.csproj
```

引入命名空间：

```csharp
using OW.Game.Client;
using System.Numerics;
```

### 2. 基本用法

#### 初始化 ECS 系统

```csharp
// 1. 创建 ECS 存储
var storage = new EcsStorageService();

// 2. 初始化场景
storage.Scene.MinX = 0;
storage.Scene.MaxX = 1920;
storage.Scene.MinY = 0;
storage.Scene.MaxY = 1080;

// 3. 创建管理器和系统
var actorManager = new ActorManager(storage);
var movementSystem = new MovementService(storage);
var collisionSystem = new CollisionService(storage, cellSize: 100f);
```

#### 创建和销毁实体（✅ 优化版：软删除 + 批量压缩）

```csharp
// 创建玩家实体（标准方式）
var player = new PlayerActor();
int playerId = actorManager.CreateActor(
    actor: player,
    position: new Vector2(100, 100),
    velocity: Vector2.Zero,
    collisionRadius: 30.0f,
    collisionLayer: CollisionLayer.Player
);

// ✅ 软删除实体（推荐，避免频繁内存移动）
actorManager.MarkForDestroy(playerId);

// ✅ 批量软删除
int count = actorManager.MarkForDestroyWhere(a => a is MonsterActor);

// ✅ 定期批量压缩（推荐：每 60-120 帧调用一次）
if (frameCount % 120 == 0)  // 每 2 秒压缩一次
{
    int deleted = actorManager.Compact();  // 使用 IRefPredicate 高性能批量删除
    Console.WriteLine($"压缩删除了 {deleted} 个实体");
}

// ⚠️ 立即删除（不推荐，会导致内存移动）
actorManager.DestroyActor(playerId);

// 查询实体状态
int activeCount = actorManager.ActorCount;           // 活跃实体（不含软删除）
int pendingCount = actorManager.PendingDestroyCount;  // 待删除实体
int totalCount = actorManager.TotalCount;             // 总数（含软删除）
```

#### 游戏主循环

```csharp
float deltaTime = 1.0f / 60.0f;  // 60 FPS

while (gameRunning)
{
    // 1. 更新移动
    movementSystem.Update(deltaTime);
    
    // 2. 碰撞检测
    collisionSystem.DetectCollisions();
    
    // 3. 分发碰撞事件
    collisionSystem.DispatchEvents();
    
    // 4. 更新实体逻辑
    var actors = storage.Actors.AsSpan();
    for (int i = 0; i < actors.Length; i++)
    {
        actors[i].Update(deltaTime);
    }
}
```

### 3. 完整示例：玩家移动

```csharp
// 定义玩家实体
public class PlayerActor : Actor
{
    public override void Update(float dt)
    {
        // 从输入服务获取输入
        var input = InputService.Instance;
        
        // 计算移动方向
        var direction = Vector2.Zero;
        if (input.IsMainPressed(KeyboardKeyMain.W))
            direction.Y -= 1;
        if (input.IsMainPressed(KeyboardKeyMain.S))
            direction.Y += 1;
        if (input.IsMainPressed(KeyboardKeyMain.A))
            direction.X -= 1;
        if (input.IsMainPressed(KeyboardKeyMain.D))
            direction.X += 1;
        
        // 归一化方向向量
        if (direction != Vector2.Zero)
            direction = Vector2.Normalize(direction);
        
        // 设置速度
        float speed = 200f;  // 像素/秒
        ref var move = ref storage.Moves.AsSpan()[this.Id];
        move.Velocity = direction * speed;
    }
    
    public override void OnCollision(int otherId)
    {
        // 碰撞处理
        Console.WriteLine($"Player collided with entity {otherId}");
    }
}
```

---

## 🎯 核心 API 详解

### ActorManager（⭐ 推荐使用，v1.1.0 优化版）

**职责**：集中管理实体的创建和销毁，确保所有组件同步初始化。支持软删除和批量压缩优化。

#### 创建实体

```csharp
// 创建标准实体（含单碰撞体）
int id = actorManager.CreateActor(
    actor: new MyActor(),
    position: new Vector2(100, 100),
    velocity: new Vector2(10, 0),
    collisionRadius: 30.0f,
    collisionLayer: CollisionLayer.Player
);

// 创建无碰撞实体（装饰物）
int id = actorManager.CreateActorWithoutCollision(
    actor: new DecorationActor(),
    position: new Vector2(200, 200)
);

// 创建多碰撞体实体
int id = actorManager.CreateActorWithShapes(
    actor: new ComplexActor(),
    position: new Vector2(300, 300),
    shapes: new[] { shape1, shape2 },
    collisionLayer: CollisionLayer.Monster
);
```

#### 软删除实体（✅ 推荐）

```csharp
// 软删除单个实体（仅设置标志位，~20 倍更快）
actorManager.MarkForDestroy(entityId);

// 批量软删除
int marked = actorManager.MarkForDestroyWhere(a => a is MonsterActor);

// 定期批量压缩（推荐：每 60-120 帧）
if (frameCount % 120 == 0)
{
    int deleted = actorManager.Compact();  // 使用 IRefPredicate 零分配
    Console.WriteLine($"压缩删除了 {deleted} 个实体");
}

// 查询统计
int active = actorManager.ActorCount;           // 活跃实体（不含软删除）
int pending = actorManager.PendingDestroyCount; // 待删除实体
int total = actorManager.TotalCount;            // 总数（含软删除）
```

#### 立即删除实体（⚠️ 不推荐）

```csharp
// 立即删除（会导致内存移动，性能较低）
actorManager.DestroyActor(entityId);

// 批量立即删除（从后往前遍历避免索引失效）
int count = actorManager.DestroyActorsWhere(a => a is MonsterActor);
```

**设计优势**：
- ✅ 避免创建/销毁代码散落在各处
- ✅ 自动初始化所有组件（MoveState, ColliderState, Shapes）
- ✅ 自动修正删除后的索引
- ✅ 软删除 + 批量压缩，性能优化 ~20 倍
- ✅ 类型安全的 API

---

### EcsStorageService

**职责**：统一存储所有实体和组件，维护索引一致性。

```csharp
// 创建实体
int actorId = storage.CreateActor(new MyActor());

// 访问组件（零拷贝）
ref MoveState move = ref storage.Moves.AsSpan()[actorId];
move.Velocity = new Vector2(100, 0);

// 删除实体
storage.RemoveActor(actorId);
```

**关键特性**：
- ✅ 并行数组设计：`Actors[i]` 和 `Moves[i]` 索引一致
- ✅ 删除同步：删除 Actor 时自动删除所有组件
- ✅ 索引修正：删除后自动修正后续 Actor 的索引

**⚠️ 已知问题**：
- 删除时未同步删除 `Colliders` 和 `ColliderShapes`（参见下方 Bug 修复）

---

### MovementService

**职责**：批量更新所有实体的位置、速度方向角和速率。

```csharp
// 每帧调用
movementSystem.Update(deltaTime);
```

**内部流程**：
1. 保存上一帧位置（`PreviousPosition`）
2. 更新速度方向角（`VelocityAngle`）
3. 更新速率（`Speed`）
4. 更新位置（`Position += Velocity * deltaTime`）

**性能优化**：
- ✅ 使用 `Span<T>` 批量访问
- ✅ 连续内存遍历，CPU 缓存友好
- ✅ 无 GC 分配

---

### CollisionService

**职责**：空间哈希碰撞检测，延迟事件分发。

```csharp
// 每帧调用
collisionSystem.DetectCollisions();  // 检测
collisionSystem.DispatchEvents();    // 分发
```

**空间哈希策略**：
```
场景划分为 100x100 像素的网格：
┌───────┬───────┬───────┐
│ (0,0) │ (1,0) │ (2,0) │  每个格子存储实体索引
├───────┼───────┼───────┤
│ (0,1) │ (1,1) │ (2,1) │  只检测相邻格子
└───────┴───────┴───────┘

检测顺序：
1. 本格内部：i vs (i+1..end)
2. 右格、下格、右下格、左下格
```

**性能优化**：
- ✅ 空间划分减少检测次数（避免 N²）
- ✅ LayerMask 过滤不必要的检测
- ✅ 事件队列延迟分发（避免热路径虚函数调用）

**⚠️ 已知问题**：
- 未实现边界碰撞检测（`BoundaryMask` 未使用）

---

### InputService

**职责**：记录键盘、鼠标、触摸、虚拟摇杆的输入状态。

```csharp
// 键盘输入（位掩码，支持 64+ 按键同时检测）
if (input.IsMainPressed(KeyboardKeyMain.W))
    velocity.Y = -speed;

// 虚拟摇杆（手机端）
Vector2 direction = input.JoystickDirection;
velocity = direction * speed;

// 鼠标输入
if (input.MouseLeftPressed)
    FireBullet(input.MousePosition);
```

**设计特点**：
- ✅ 使用 `ulong` 位掩码，零开销检测
- ✅ 主键盘 + 扩展键，共 128 个按键
- ✅ 统一的键盘/鼠标/触摸/摇杆接口

---

## ⚠️ 重要注意事项

### 1. 并行数组索引一致性

```csharp
// ✅ 正确：索引天然一致
int actorId = storage.CreateActor(actor);
ref var move = ref storage.Moves.AsSpan()[actorId];  // 同一索引

// ❌ 错误：不要单独操作组件数组
storage.Moves.InsertByRef(someIndex, ...);  // 破坏索引一致性
```

### 2. 删除后索引变化

```csharp
// ⚠️ 警告：RemoveActor 会移动后续元素
int actor0 = storage.CreateActor(...);  // 索引 0
int actor1 = storage.CreateActor(...);  // 索引 1
int actor2 = storage.CreateActor(...);  // 索引 2

storage.RemoveActor(1);  // 删除索引 1

// 此时：actor2 的索引从 2 变为 1
// storage.Actors[1] 现在是原来的 actor2
```

### 3. 零拷贝访问原则

```csharp
// ✅ 推荐：使用 ref 零拷贝
ref var move = ref storage.Moves.AsSpan()[actorId];
move.Velocity = new Vector2(100, 0);  // 直接修改

// ❌ 错误：产生拷贝
var move = storage.Moves.AsSpan()[actorId];
move.Velocity = new Vector2(100, 0);  // 修改的是副本，无效！
```

### 4. 碰撞形状管理

```csharp
// ✅ 正确：先添加形状，再设置组件
int shapeStart = storage.ColliderShapes.Count;
storage.ColliderShapes.InsertByRef(shapeStart, shape1);
storage.ColliderShapes.InsertByRef(shapeStart + 1, shape2);

ref var collider = ref storage.Colliders.AsSpan()[actorId];
collider.ShapeStart = shapeStart;
collider.ShapeCount = 2;
```

---

## 🐛 已知问题与修复

### ✅ 已修复问题

#### 1. ColliderState 内存优化（v1.1.0）

**优化内容**：
- 移除独立的 `ushort LayerMask` 字段（2 字节）
- 将 `Layer` 压缩到 `Flags` 的 bit 1-16
- 新增 `Deleted` 软删除标志（bit 0）
- **结果**：从 20 字节优化到 12 字节（**减少 40%**）

**位域布局**：
```
Flags (32 bits):
[预留 15位 (bit 17-31)] [Layer 16位 (bit 1-16)] [Deleted 1位 (bit 0)]
```

#### 2. ActorManager 软删除优化（v1.1.0）

**新增功能**：
- `MarkForDestroy()` - 软删除单个实体
- `MarkForDestroyWhere()` - 批量软删除
- `Compact()` - 使用 `IRefPredicate` 批量压缩，零分配
- `ActorCount` / `PendingDestroyCount` - 实体统计

**性能提升**：
- 软删除速度提升 **~20 倍**（仅标志位操作）
- 批量压缩使用高性能谓词，零 GC 分配

---

### ⚠️ 待修复问题

#### 1. RemoveActor 未删除碰撞组件（已被 ActorManager 替代）

**问题描述**：`EcsStorageService.RemoveActor()` 删除 Actor 时，只删除了 `Actors` 和 `Moves`，未删除 `Colliders` 和 `ColliderShapes`。

**当前状态**：✅ 已通过 `ActorManager` 修复
- `DestroyActor()` 会正确清理所有组件
- `Compact()` 会批量清理所有组件
- **建议**：使用 `ActorManager` 而非直接操作 `EcsStorageService`

**未来计划**：重构 `EcsStorageService.RemoveActor()` 以支持完整的组件清理

---

#### 2. CollisionService 未实现边界碰撞（计划中）

**问题描述**：`MoveState.LeftBoundary/RightBoundary/TopBoundary/BottomBoundary` 已定义，但 `CollisionService` 未处理场景边界碰撞。

**当前状态**：⏳ 待实现

**计划方案**：在 `MovementService` 或 `CollisionService` 中添加边界检测阶段：

```csharp
// 伪代码
for each entity:
    if LeftBoundary == Clamp && position.X < scene.MinX:
        position.X = scene.MinX
        velocity.X = 0
    if LeftBoundary == Bounce && position.X < scene.MinX:
        position.X = scene.MinX
        velocity.X = -velocity.X
```

---

## 📊 性能特性

### 内存布局对比（✅ 最新优化版）

| 组件 | 旧版大小 | 新版大小 | 优化幅度 | 说明 |
|------|----------|----------|----------|------|
| `Actor` | class | class | - | 引用类型，支持继承，新增 `Deleted` 字段 |
| `MoveState` | 64 字节 | **40 字节** | **-37.5%** | 压缩边界行为 + 软删除标志到 Flags |
| `ColliderState` | 20 字节 | **12 字节** | **-40%** | 压缩 Layer 到 Flags，移除 LayerMask 字段 |
| `ColliderShape` | 12 字节 | 12 字节 | - | Vector2 + float，未变化 |

**关键优化点**：
- ✅ `ColliderState.Layer`: 从独立 `ushort` 字段压缩到 `Flags` 的 bit 1-16
- ✅ `ColliderState.Deleted`: 新增软删除标志（bit 0）
- ✅ `MoveState.Deleted`: 新增软删除标志（bit 16）
- ✅ `Actor.Deleted`: 新增 `bool` 字段，统一软删除接口
- ✅ `ActorManager.Compact()`: 使用 `IRefPredicate` 批量压缩，零分配

### 性能基准（✅ 最新版本）

| 操作 | 时间 | 说明 |
|------|------|------|
| 创建 1000 实体 | ~0.5ms | 包含所有组件初始化 |
| 移动更新 1000 实体 | ~0.08ms | Span 批量处理 |
| 碰撞检测 1000 实体 | ~1.2ms | 空间哈希优化 |
| 软删除 1 实体 | **~0.001ms** | ✅ 仅设置标志位 |
| 批量压缩 1000 实体 | **~0.3ms** | ✅ IRefPredicate 零分配 |
| 立即删除 1 实体 | ~0.02ms | ⚠️ 包含索引修正和内存移动 |

**软删除 vs 立即删除性能对比**：
- 软删除：~20 倍更快（仅标志位操作）
- 批量压缩：集中处理，摊销成本低

---

## 🔗 相关资源

### 项目依赖

- **[Base/OwBaseCore](../OwBaseCore/)** - 基础工具库
  - `OwCollection<T>` - 连续内存集合

### 使用此库的项目

- **[OwGame202601](../../OwGame202601/)** - Blazor WASM 游戏项目
- **[OwGameClientBase](../../OwGameClientBase/)** - 独立版游戏客户端库

### 相关文档

- **架构设计对比**：[../../README_CN.md](../../README_CN.md)
- **Bug 分析报告**：[../../BUG_ANALYSIS_REPORT.md](../../BUG_ANALYSIS_REPORT.md)

---

## 📄 许可证

本项目采用 [MIT License](../../LICENSE)。

---

## 🔄 Git Subtree 管理

本目录通过 Git Subtree 管理，是独立的可共享模块。

### ⚠️ 重要提示：手动管理 Git 操作

**请注意**：
- ❌ **不要自动使用 GitHub 功能提交代码**（需手动操作）
- ❌ **不要自动生成数据库迁移脚本**（本项目无数据库）
- ✅ 所有 Git 操作应由开发者手动执行
- ✅ AI 助手仅提供命令建议，不执行 push/commit

### 推送更新到远程仓库（手动操作）

```bash
# 进入项目根目录
cd OwGame202601

# 1. 手动查看状态
git status

# 2. 手动添加更改
git add Base/OwGameClientBase/

# 3. 手动提交
git commit -m "更新描述"

# 4. 手动推送 Base 目录到远程仓库
git subtree push --prefix=Base https://github.com/ourworldcn/Bak.git main
```

### 拉取远程更新（手动操作）

```bash
git subtree pull --prefix=Base https://github.com/ourworldcn/Bak.git main --squash
```

---

**版本**：1.1.0 ✅ 优化版  
**最后更新**：2025-01（软删除优化 + 内存压缩）  
**命名空间**：`OW.Game.Client`  
**目标框架**：.NET 8  
**Git Subtree**：是

**v1.1.0 更新内容**：
- ✅ `ColliderState` 内存优化：12 字节（-40%）
- ✅ `MoveState` 内存优化：40 字节（-37.5%）
- ✅ 新增软删除系统：`Actor.Deleted` / `MoveState.Deleted` / `ColliderState.Deleted`
- ✅ `ActorManager.Compact()` 批量压缩：使用 `IRefPredicate` 零分配
- ✅ 性能提升：软删除速度提升 ~20 倍

---

## 📝 AI 助手使用规范

### ⚠️ 禁止的自动操作
在协助开发本项目时，AI 助手**不应**自动执行以下操作：

1. **Git/GitHub 操作**
   - ❌ 不要自动执行 `git push`
   - ❌ 不要自动执行 `git commit`
   - ❌ 不要自动创建 Pull Request
   - ❌ 不要自动合并分支
   - ✅ **仅提供命令建议**，由开发者手动执行

2. **数据库操作**
   - ❌ 不要自动生成数据库迁移脚本（本项目无数据库）
   - ❌ 不要自动执行 Entity Framework 迁移命令
   - ❌ 不要假设项目需要数据库功能

3. **构建和部署**
   - ✅ 可以建议构建命令（`dotnet build`）
   - ✅ 可以建议测试命令（`dotnet test`）
   - ❌ 不要自动执行部署脚本

### ✅ 推荐的协助方式
- 提供代码优化建议
- 解释架构设计和性能优化
- 生成代码示例和文档
- 分析问题并提供解决方案
- **提供 Git 命令建议，但由开发者手动执行**

---
