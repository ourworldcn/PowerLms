using System.Numerics;
using OW.Collections.Generic;

namespace OW.Game.Client
{

    /// <summary>
    /// SceneInfo 表示“当前场景”的逻辑信息。
    /// 
    /// 设计语义：
    /// 1. 场景（Scene）是逻辑世界的容器，不等于地图（Map）。
    ///    - 地图是场景的一部分
    ///    - 场景还可以包含非地图的逻辑区域、触发器、环境参数等
    /// 
    /// 2. SceneInfo 属于整个 ECS 世界，而不是某个实体或组件。
    ///    - 不应该放在 Actor 中（Actor 是个体）
    ///    - 不应该放在组件中（组件是局部数据）
    ///    - 不应该放在系统内部（多个系统都需要访问）
    /// 
    /// 3. SceneInfo 只包含逻辑层需要的“世界边界与环境参数”。
    ///    - 不包含渲染层的缩放、像素坐标、相机信息
    ///    - 不包含 UI 层信息
    /// 
    /// 4. 典型用途：
    ///    - MovementService：限制实体不越界
    ///    - CollisionService：判断是否撞到场景边界
    ///    - AIService：判断目标点是否在场景内
    ///    - GameWorld：加载/切换场景时重置 SceneInfo
    /// </summary>
    public sealed class SceneInfo
    {
        /// <summary>
        /// 世界坐标最小 X 值。
        /// 例如：0 或 -1000。
        /// </summary>
        public float MinX;

        /// <summary>
        /// 世界坐标最大 X 值。
        /// 例如：1920 或 1000。
        /// </summary>
        public float MaxX;

        /// <summary>
        /// 世界坐标最小 Y 值。
        /// </summary>
        public float MinY;

        /// <summary>
        /// 世界坐标最大 Y 值。
        /// </summary>
        public float MaxY;

        /// <summary>
        /// 世界宽度（MaxX - MinX）。
        /// </summary>
        public float Width => MaxX - MinX;

        /// <summary>
        /// 世界高度（MaxY - MinY）。
        /// </summary>
        public float Height => MaxY - MinY;

        /// <summary>
        /// 可选：逻辑层的 Tile 大小（如果使用网格地图）。
        /// 不使用 TileMap 时可以忽略。
        /// </summary>
        public float TileSize;

        /// <summary>
        /// 判断一个点是否在场景边界内。
        /// 常用于 AI、移动、碰撞等系统。
        /// </summary>
        public bool Contains(Vector2 pos)
        {
            return pos.X >= MinX && pos.X <= MaxX &&
                   pos.Y >= MinY && pos.Y <= MaxY;
        }

        /// <summary>
        /// 将一个点限制在场景边界内。
        /// MovementService 和 CollisionService 常用。
        /// </summary>
        public Vector2 Clamp(Vector2 pos)
        {
            float x = MathF.Min(MathF.Max(pos.X, MinX), MaxX);
            float y = MathF.Min(MathF.Max(pos.Y, MinY), MaxY);
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// VectorStorageService 是 ECS 的统一数据存储服务。
    /// 
    /// 设计目标：
    /// 1. 存储所有 Actor（实体）和所有移动组件（MoveState）。
    /// 2. Actor 与组件使用并行数组（Parallel Arrays），索引天然一致。
    /// 3. OwCollection 提供连续内存，适合高性能批处理（MovementService）。
    /// 4. 删除 Actor 时同步删除组件，保持索引一致性。
    /// 5. 所有系统（MovementService 等）都通过此服务访问组件数据。
    /// </summary>
    public sealed class EcsStorageService
    {
        /// <summary>
        /// 碰撞组件的连续存储。
        /// </summary>
        public readonly OwCollection<ColliderState> Colliders = new();

        /// <summary>
        /// 碰撞形状的连续存储。
        /// </summary>
        public readonly OwCollection<ColliderShape> ColliderShapes = new();

        /// <summary>
        /// 场景信息（当前逻辑世界的边界与环境参数）。
        /// </summary>
        public readonly SceneInfo Scene = new SceneInfo();

        /// <summary>
        /// 所有角色（Actor）的连续存储。
        /// Actor 是 class，可以包含复杂行为和扩展信息。
        /// </summary>
        public readonly OwCollection<Actor> Actors = new();

        /// <summary>
        /// 所有移动组件（MoveState）的连续存储。
        /// 与 Actors 按索引一一对应。
        /// </summary>
        public readonly OwCollection<MoveState> Moves = new();

        /// <summary>
        /// 创建一个新的 Actor，并为其分配移动组件的槽位。
        /// 返回 Actor 的索引（也是组件的索引）。
        /// </summary>
        public int CreateActor(Actor actor)
        {
            int index = Actors.Count;

            // 插入 Actor
            Actors.InsertByRef(index, in actor);

            // 插入 MoveState（默认值）
            MoveState empty = default; Moves.InsertByRef(index, in empty);
            // 建立组件索引
            actor.Id = index;
            actor.MoveIndex = index;

            return index;
        }

        /// <summary>
        /// 删除指定索引的 Actor 及其移动组件。
        /// OwCollection.RemoveRange 会移动后续元素，因此需要同步更新所有 Actor 的索引。
        /// </summary>
        public void RemoveActor(int index)
        {
            // 删除 Actor 和 MoveState
            Actors.RemoveRange(index, 1);
            Moves.RemoveRange(index, 1);

            // 修正所有 Actor 的索引（因为 RemoveRange 会移动元素）
            var span = Actors.AsSpan();
            for (int i = index; i < span.Length; i++)
            {
                span[i].Id = i;
                span[i].MoveIndex = i;
            }
        }
    }
}
