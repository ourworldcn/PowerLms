using System;
using System.Numerics;
using OW.Collections.Generic;

namespace OW.Game.Client
{
    /// <summary>
    /// 软删除谓词（用于高性能批量压缩）
    /// 实现为 readonly struct 以获得最佳性能（零分配、内联优化）
    /// </summary>
    internal readonly struct DeletedPredicate : IRefPredicate<Actor>
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Match(in Actor item) => item.Deleted;
    }

    /// <summary>
    /// MoveState 软删除谓词
    /// 实现为 readonly struct 以获得最佳性能（零分配、内联优化）
    /// </summary>
    internal readonly struct MoveMarkedPredicate : IRefPredicate<MoveState>
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Match(in MoveState item) => item.Deleted;
    }

    /// <summary>
    /// ColliderState 软删除谓词
    /// 实现为 readonly struct 以获得最佳性能（零分配、内联优化）
    /// 注意：使用独立的 Deleted 标志，而不是 ShapeCount = 0
    /// </summary>
    internal readonly struct ColliderMarkedPredicate : IRefPredicate<ColliderState>
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Match(in ColliderState item) => item.Deleted;
    }

    /// <summary>
    /// Actor 管理服务（实体创建与销毁的统一入口）
    /// 
    /// 设计职责：
    /// 1. 集中管理 Actor 的创建和销毁
    /// 2. 协调 EcsStorageService 的组件初始化
    /// 3. 维护实体索引一致性
    /// 4. 提供类型安全的创建 API
    /// 
    /// 设计理念：
    /// - 避免创建/销毁代码散落在各个系统中
    /// - 确保所有组件（Moves, Colliders, Shapes）同步创建/销毁
    /// - 作为 ECS 存储层和业务逻辑层之间的桥梁
    /// </summary>
    public sealed class ActorManager
    {
        private readonly EcsStorageService _storage;

        /// <summary>
        /// 构造 Actor 管理器。
        /// </summary>
        /// <param name="storage">ECS 统一存储服务</param>
        public ActorManager(EcsStorageService storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <summary>
        /// 创建 Actor 并初始化所有关联组件。
        /// </summary>
        /// <param name="actor">Actor 实例（支持继承）</param>
        /// <param name="position">初始位置</param>
        /// <param name="velocity">初始速度（默认为零）</param>
        /// <param name="collisionRadius">碰撞半径（默认 20）</param>
        /// <param name="collisionLayer">碰撞层掩码（默认 All）</param>
        /// <returns>Actor 的索引 ID</returns>
        /// <remarks>
        /// 此方法会自动初始化：
        /// - MoveState（位置、速度、速率、方向角）
        /// - ColliderState（默认单圆形碰撞体）
        /// - ColliderShape（圆形半径）
        /// </remarks>
        public int CreateActor(
            Actor actor,
            Vector2 position,
            Vector2? velocity = null,
            float collisionRadius = 20.0f,
            CollisionLayer collisionLayer = CollisionLayer.All)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            // 1. 调用存储服务创建 Actor（自动初始化并行数组）
            int actorId = _storage.CreateActor(actor);

            // 2. 初始化 MoveState
            ref var move = ref _storage.Moves.AsSpan()[actorId];
            move.Position = position;
            move.Velocity = velocity ?? Vector2.Zero;
            move.Speed = move.Velocity.Length();
            move.VelocityAngle = move.Velocity != Vector2.Zero
                ? MathF.Atan2(move.Velocity.Y, move.Velocity.X)
                : 0f;
            move.PreviousPosition = position;
            move.Flags = 0;  // 默认所有边界行为为 Pass

            // 3. 初始化 ColliderState
            ref var collider = ref _storage.Colliders.AsSpan()[actorId];
            collider.ShapeStart = _storage.ColliderShapes.Count;
            collider.ShapeCount = 1;
            collider.Layer = collisionLayer;

            // 4. 添加默认碰撞形状（圆形）
            _storage.ColliderShapes.InsertByRef(
                _storage.ColliderShapes.Count,
                new ColliderShape
                {
                    Radius = collisionRadius,
                    Offset = Vector2.Zero
                }
            );

            return actorId;
        }

        /// <summary>
        /// 创建 Actor（无碰撞体版本）。
        /// </summary>
        /// <param name="actor">Actor 实例</param>
        /// <param name="position">初始位置</param>
        /// <param name="velocity">初始速度（默认为零）</param>
        /// <returns>Actor 的索引 ID</returns>
        /// <remarks>
        /// 适用于装饰物、粒子特效等不需要碰撞的实体。
        /// </remarks>
        public int CreateActorWithoutCollision(
            Actor actor,
            Vector2 position,
            Vector2? velocity = null)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            // 1. 创建 Actor
            int actorId = _storage.CreateActor(actor);

            // 2. 初始化 MoveState
            ref var move = ref _storage.Moves.AsSpan()[actorId];
            move.Position = position;
            move.Velocity = velocity ?? Vector2.Zero;
            move.Speed = move.Velocity.Length();
            move.VelocityAngle = move.Velocity != Vector2.Zero
                ? MathF.Atan2(move.Velocity.Y, move.Velocity.X)
                : 0f;
            move.PreviousPosition = position;

            // 3. ColliderState 保持默认值（ShapeCount = 0，无碰撞）
            ref var collider = ref _storage.Colliders.AsSpan()[actorId];
            collider.ShapeStart = 0;
            collider.ShapeCount = 0;  // 无碰撞形状
            collider.Layer = 0;

            return actorId;
        }

        /// <summary>
        /// 创建 Actor（自定义多碰撞形状版本）。
        /// </summary>
        /// <param name="actor">Actor 实例</param>
        /// <param name="position">初始位置</param>
        /// <param name="shapes">碰撞形状数组</param>
        /// <param name="velocity">初始速度（默认为零）</param>
        /// <param name="collisionLayer">碰撞层掩码（默认 All）</param>
        /// <returns>Actor 的索引 ID</returns>
        /// <remarks>
        /// 适用于需要多个碰撞体的复杂实体（如角色头部、躯干分别碰撞）。
        /// </remarks>
        public int CreateActorWithShapes(
            Actor actor,
            Vector2 position,
            ColliderShape[] shapes,
            Vector2? velocity = null,
            CollisionLayer collisionLayer = CollisionLayer.All)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (shapes == null || shapes.Length == 0)
                throw new ArgumentException("碰撞形状数组不能为空", nameof(shapes));

            // 1. 创建 Actor
            int actorId = _storage.CreateActor(actor);

            // 2. 初始化 MoveState
            ref var move = ref _storage.Moves.AsSpan()[actorId];
            move.Position = position;
            move.Velocity = velocity ?? Vector2.Zero;
            move.Speed = move.Velocity.Length();
            move.VelocityAngle = move.Velocity != Vector2.Zero
                ? MathF.Atan2(move.Velocity.Y, move.Velocity.X)
                : 0f;
            move.PreviousPosition = position;

            // 3. 初始化 ColliderState
            ref var collider = ref _storage.Colliders.AsSpan()[actorId];
            collider.ShapeStart = _storage.ColliderShapes.Count;
            collider.ShapeCount = shapes.Length;
            collider.Layer = collisionLayer;

            // 4. 添加所有碰撞形状
            foreach (var shape in shapes)
            {
                _storage.ColliderShapes.InsertByRef(
                    _storage.ColliderShapes.Count,
                    shape
                );
            }

            return actorId;
        }

        /// <summary>
        /// 标记 Actor 为软删除（推荐使用，避免频繁内存移动）。
        /// </summary>
        /// <param name="actorId">Actor 的索引 ID</param>
        /// <remarks>
        /// 软删除不会立即删除实体，而是标记为待删除。
        /// 系统在处理时会跳过已标记的实体。
        /// 调用 Compact() 时才会批量删除。
        /// </remarks>
        public void MarkForDestroy(int actorId)
        {
            if (actorId < 0 || actorId >= _storage.Actors.Count)
                throw new ArgumentOutOfRangeException(nameof(actorId));

            // 标记 Actor
            _storage.Actors.AsSpan()[actorId].Deleted = true;

            // 标记 MoveState
            if (actorId < _storage.Moves.Count)
            {
                ref var move = ref _storage.Moves.AsSpan()[actorId];
                move.Deleted = true;
            }

            // 标记 ColliderState（使用独立的 Deleted 标志）
            if (actorId < _storage.Colliders.Count)
            {
                ref var collider = ref _storage.Colliders.AsSpan()[actorId];
                collider.Deleted = true;
            }
        }

        /// <summary>
        /// 批量标记符合条件的 Actor 为软删除。
        /// </summary>
        /// <param name="predicate">判断函数（返回 true 表示需要销毁）</param>
        /// <returns>实际标记的数量</returns>
        public int MarkForDestroyWhere(Func<Actor, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            int markedCount = 0;
            var actors = _storage.Actors.AsSpan();

            for (int i = 0; i < actors.Length; i++)
            {
                if (!actors[i].Deleted && predicate(actors[i]))
                {
                    MarkForDestroy(i);
                    markedCount++;
                }
            }

            return markedCount;
        }

        /// <summary>
        /// 压缩存储：批量删除所有已标记为软删除的 Actor。
        /// </summary>
        /// <returns>实际删除的数量</returns>
        /// <remarks>
        /// 此方法使用高性能谓词接口批量压缩：
        /// 1. 清理已标记 Actor 的 ColliderShapes
        /// 2. 使用 IRefPredicate 批量压缩 Actors、Moves、Colliders 数组
        /// 3. 自动修正所有索引
        /// 
        /// 建议：
        /// - 每 60-120 帧调用一次
        /// - 或当待删除数量超过 20% 时调用
        /// </remarks>
        public int Compact()
        {
            int deletedCount = 0;

            // 1. 先清理 ColliderShapes（必须在删除 Collider 之前）
            // 从后往前遍历，避免索引失效
            var colliders = _storage.Colliders.AsSpan();
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                ref var collider = ref colliders[i];
                
                // 检查是否被标记为软删除（ShapeCount 为 0 的特殊情况除外）
                var actor = _storage.Actors.AsSpan()[i];
                if (!actor.Deleted)
                    continue;

                // 删除对应的 Shapes
                if (collider.ShapeStart >= 0 && collider.ShapeCount > 0)
                {
                    _storage.ColliderShapes.RemoveRange(
                        collider.ShapeStart,
                        collider.ShapeCount
                    );

                    // 修正后续 Collider 的 ShapeStart
                    var colliderSpan = _storage.Colliders.AsSpan();
                    for (int j = 0; j < colliderSpan.Length; j++)
                    {
                        if (colliderSpan[j].ShapeStart > collider.ShapeStart)
                        {
                            colliderSpan[j].ShapeStart -= collider.ShapeCount;
                        }
                    }
                }
            }

            // 2. 使用高性能谓词批量压缩各个组件数组
            var actorPredicate = new DeletedPredicate();
            var movePredicate = new MoveMarkedPredicate();
            var colliderPredicate = new ColliderMarkedPredicate();

            // ? 使用 IRefPredicate 接口的高性能批量删除
            int actorRemoved = _storage.Actors.RemoveRange(0, _storage.Actors.Count, in actorPredicate);
            int moveRemoved = _storage.Moves.RemoveRange(0, _storage.Moves.Count, in movePredicate);
            int colliderRemoved = _storage.Colliders.RemoveRange(0, _storage.Colliders.Count, in colliderPredicate);

            deletedCount = actorRemoved;

            // 3. 修正 Actor 的索引（ID 和 MoveIndex）
            var actors = _storage.Actors.AsSpan();
            for (int i = 0; i < actors.Length; i++)
            {
                actors[i].Id = i;
                actors[i].MoveIndex = i;
            }

            return deletedCount;
        }

        /// <summary>
        /// 立即销毁 Actor（不推荐，除非必要）。
        /// </summary>
        /// <param name="actorId">Actor 的索引 ID</param>
        /// <remarks>
        /// ?? 警告：此方法会立即删除实体，导致内存移动。
        /// 推荐使用 MarkForDestroy() + Compact() 代替。
        /// 
        /// 此方法会自动：
        /// 1. 删除 ColliderShapes（批量移除）
        /// 2. 修正后续 Collider 的 ShapeStart 索引
        /// 3. 删除 Actor、MoveState、ColliderState
        /// 4. 修正后续 Actor 的索引
        /// </remarks>
        public void DestroyActor(int actorId)
        {
            if (actorId < 0 || actorId >= _storage.Actors.Count)
                throw new ArgumentOutOfRangeException(nameof(actorId));

            // 1. 清理 ColliderShapes（重要：避免内存泄漏）
            if (actorId < _storage.Colliders.Count)
            {
                ref var collider = ref _storage.Colliders.AsSpan()[actorId];
                if (collider.ShapeCount > 0)
                {
                    // 删除形状数组中的片段
                    _storage.ColliderShapes.RemoveRange(
                        collider.ShapeStart,
                        collider.ShapeCount
                    );

                    // 修正后续 Collider 的 ShapeStart
                    var colliderSpan = _storage.Colliders.AsSpan();
                    for (int i = 0; i < colliderSpan.Length; i++)
                    {
                        if (colliderSpan[i].ShapeStart > collider.ShapeStart)
                        {
                            colliderSpan[i].ShapeStart -= collider.ShapeCount;
                        }
                    }
                }
            }

            // 2. 调用存储服务删除 Actor（自动删除 Moves、Colliders、修正索引）
            _storage.RemoveActor(actorId);
        }

        /// <summary>
        /// 批量销毁符合条件的 Actor。
        /// </summary>
        /// <param name="predicate">判断函数（返回 true 表示需要销毁）</param>
        /// <returns>实际销毁的数量</returns>
        /// <remarks>
        /// 从后往前遍历，避免索引失效问题。
        /// </remarks>
        public int DestroyActorsWhere(Func<Actor, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            int destroyedCount = 0;
            var actors = _storage.Actors.AsSpan();

            // 从后往前遍历（避免索引失效）
            for (int i = actors.Length - 1; i >= 0; i--)
            {
                if (predicate(actors[i]))
                {
                    DestroyActor(i);
                    destroyedCount++;
                }
            }

            return destroyedCount;
        }

        /// <summary>
        /// 获取 Actor 数量（不包含已软删除的）。
        /// </summary>
        public int ActorCount
        {
            get
            {
                int count = 0;
                var actors = _storage.Actors.AsSpan();
                for (int i = 0; i < actors.Length; i++)
                {
                    if (!actors[i].Deleted)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取待删除的 Actor 数量。
        /// </summary>
        public int PendingDestroyCount
        {
            get
            {
                int count = 0;
                var actors = _storage.Actors.AsSpan();
                for (int i = 0; i < actors.Length; i++)
                {
                    if (actors[i].Deleted)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取 Actor 总数量（包含已软删除的）。
        /// </summary>
        public int TotalCount => _storage.Actors.Count;

        /// <summary>
        /// 获取指定索引的 Actor。
        /// </summary>
        public Actor GetActor(int actorId)
        {
            return _storage.Actors.AsSpan()[actorId];
        }

        /// <summary>
        /// 获取所有 Actor（只读访问）。
        /// </summary>
        public ReadOnlySpan<Actor> GetAllActors()
        {
            return _storage.Actors.AsSpan();
        }
    }
}
