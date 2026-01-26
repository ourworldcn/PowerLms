using System.Numerics;
using System.Runtime.CompilerServices;

namespace OW.Game.Client
{
    /// <summary>
    /// ColliderState 标志位定义（压缩存储在 Flags 字段中）
    /// </summary>
    internal static class ColliderFlags
    {
        // 布局：[其他标志 15位] [碰撞层 16位] [软删除 1位]
        
        /// <summary>
        /// 软删除标志（bit 0）
        /// </summary>
        public const int DeletedFlag = 1 << 0;
        
        /// <summary>
        /// 碰撞层掩码（bit 1-16）
        /// </summary>
        public const int LayerMaskShift = 1;
        public const int LayerMaskMask = 0xFFFF << LayerMaskShift;  // 16 位
        
        // 预留其他标志（bit 17-31，共 15 位）
        // 例如：
        // public const int DisabledFlag = 1 << 17;
        // public const int TriggerOnlyFlag = 1 << 18;
        // ... 最多可扩展到 bit 31
    }

    /// <summary>
    /// 碰撞形状（ColliderShape）
    /// 
    /// 设计语义：
    /// 1. 这是 ECS 中最小的碰撞单元（Hit Circle）。
    /// 2. 当前仅支持圆形（Circle），因为圆形计算最简单、最适合 WASM 和 ECS 批处理。
    /// 3. 所有字段均为值类型，适合连续内存存储（OwCollection）。
    /// 4. ColliderState 会引用一段连续的 ColliderShape（通过索引范围）。
    /// </summary>
    public struct ColliderShape
    {
        /// <summary>
        /// 圆形碰撞体的半径。
        /// </summary>
        public float Radius;

        /// <summary>
        /// 碰撞形状相对于实体位置<see cref="MoveState.Position"/>的偏移。
        /// 用于角色中心不等于碰撞中心的情况。
        /// </summary>
        public Vector2 Offset;
    }

    /// <summary>
    /// 碰撞组件（ColliderState）- 优化版本（12 字节）
    /// 
    /// 设计语义：
    /// 1. 一个实体（Actor）可以拥有多个碰撞形状（ColliderShape）。
    /// 2. 为了保持 ECS 的高性能，ColliderState 不直接持有引用类型，
    ///    而是通过 ShapeStart + ShapeCount 指向全局 ColliderShape 数组中的一段连续区域。
    /// 3. 这种"平铺数组 + 索引范围"的设计是专业 ECS 引擎（Unity DOTS、Flecs、Bevy）的标准做法。
    /// 4. Layer 用于碰撞过滤（玩家 vs 怪物、子弹 vs 地形等），压缩在 Flags 中（bit 1-16）。
    /// 5. ShapeCount = 0 表示体积为零的碰撞体（合法状态），不等于软删除。
    /// 6. Deleted 是独立的软删除标志（bit 0），与 ShapeCount 无关。
    /// 
    /// 内存优化：
    /// - 移除 ushort LayerMask（2 字节） → 压缩到 Flags（bit 1-16）
    /// - 从 20 字节优化到 12 字节（减少 40%）
    /// </summary>
    public struct ColliderState
    {
        /// <summary>
        /// 在全局 ColliderShape 数组中的起始索引。
        /// 例如：ShapeStart = 10 表示从 ColliderShapes[10] 开始。
        /// </summary>
        public int ShapeStart;

        /// <summary>
        /// 该实体拥有的碰撞形状数量。
        /// 例如：ShapeCount = 3 表示该实体有 3 个圆形碰撞体。
        /// 注意：ShapeCount = 0 表示体积为零（无碰撞图形），不等于软删除。
        /// </summary>
        public int ShapeCount;

        /// <summary>
        /// 碰撞组件标志位（压缩存储：软删除 + 碰撞层 + 其他标志）
        /// 布局：[其他标志 15位] [碰撞层 16位] [软删除 1位]
        /// </summary>
        public int Flags;

        /// <summary>
        /// 是否已删除（bit 0）
        /// 使用软删除避免频繁的内存移动，延迟到 Compact 时批量删除。
        /// 注意：软删除与 ShapeCount = 0 是两个独立的概念。
        /// </summary>
        public bool Deleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & ColliderFlags.DeletedFlag) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value)
                    Flags |= ColliderFlags.DeletedFlag;
                else
                    Flags &= ~ColliderFlags.DeletedFlag;
            }
        }

        /// <summary>
        /// 碰撞层（bit 1-16）
        /// 用于过滤碰撞，例如：
        /// - 玩家 vs 怪物
        /// - 子弹 vs 地形
        /// - 技能 vs 怪物
        /// </summary>
        public CollisionLayer Layer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (CollisionLayer)((Flags & ColliderFlags.LayerMaskMask) >> ColliderFlags.LayerMaskShift);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Flags = (Flags & ~ColliderFlags.LayerMaskMask) | ((int)value << ColliderFlags.LayerMaskShift);
        }
    }

    /// <summary>
    /// 碰撞层（CollisionLayer）
    /// 
    /// 设计语义：
    /// 1. 使用 bitmask（位掩码）进行碰撞过滤。
    /// 2. 例如：玩家子弹只与怪物层碰撞，不与玩家层碰撞。
    /// 3. 你可以根据项目需求扩展更多层。
    /// </summary>
    [Flags]
    public enum CollisionLayer : ushort
    {
        None = 0,
        Player = 1 << 0,
        Monster = 1 << 1,
        Bullet = 1 << 2,
        Terrain = 1 << 3,

        All = ushort.MaxValue
    }
}
