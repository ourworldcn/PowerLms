using System.Numerics;
using System.Runtime.CompilerServices;

namespace OW.Game.Client
{
    /// <summary>
    /// 边界行为类型
    /// </summary>
    public enum BoundaryBehavior : byte
    {
        /// <summary>
        /// 允许穿越边界（无限制）
        /// </summary>
        Pass = 0,

        /// <summary>
        /// 停留在边界内（位置被限制在边界范围内）
        /// </summary>
        Clamp = 1,

        /// <summary>
        /// 法线反弹（速度沿边界法线方向对称反弹）
        /// </summary>
        Bounce = 2
    }

    /// <summary>
    /// MoveState 标志位定义（压缩存储在 Flags 字段中）
    /// </summary>
    internal static class MoveFlags
    {
        // 边界行为使用 4 位存储（支持 16 种行为：0-15，保留扩展性）
        // 布局：[其他标志 16位] [下边界 4位] [上边界 4位] [右边界 4位] [左边界 4位]
        
        // 左边界行为（bit 0-3）
        public const int LeftBoundaryShift = 0;
        public const int LeftBoundaryMask = 0b1111 << LeftBoundaryShift;
        
        // 右边界行为（bit 4-7）
        public const int RightBoundaryShift = 4;
        public const int RightBoundaryMask = 0b1111 << RightBoundaryShift;
        
        // 上边界行为（bit 8-11）
        public const int TopBoundaryShift = 8;
        public const int TopBoundaryMask = 0b1111 << TopBoundaryShift;
        
        // 下边界行为（bit 12-15）
        public const int BottomBoundaryShift = 12;
        public const int BottomBoundaryMask = 0b1111 << BottomBoundaryShift;
        
        // 其他标志可从 bit 16 开始使用（预留 16 位）
        // ⭐ 软删除标志（bit 16）
        public const int DeletedFlag = 1 << 16;
        
        // 预留其他标志（bit 17-31）
        // 例如：
        // public const int IsGroundedFlag = 1 << 17;
        // public const int IsKnockedBackFlag = 1 << 18;
        // public const int IsDashingFlag = 1 << 19;
        // ... 最多可扩展到 bit 31
    }

    /// <summary>
    /// 移动组件（聚合结构，控制移动/速度/方向）
    /// 设计目标：
    /// 1. 小于等于 64 字节，保证一次 cache line 加载
    /// 2. 包含移动系统高度耦合的数据，减少跨组件访问
    /// 3. 速度方向角在速度为 0 时仍然保留，用于下一次加速
    /// </summary>
    public struct MoveState
    {
        /// <summary>
        /// 世界坐标（实体当前所在位置）
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 当前速度向量（单位：每秒）
        /// 注意：速度为零时方向信息会丢失，因此需要 VelocityAngle 作为补充
        /// </summary>
        public Vector2 Velocity;

        /// <summary>
        /// 当前速度大小（标量速率）
        /// 说明：速率变化通常不频繁，因此可以由系统在速度变化时更新
        /// </summary>
        public float Speed;

        /// <summary>
        /// 速度方向角（弧度制）
        /// 用途：
        /// 1. 当速度为 0 时仍然保留方向
        /// 2. 当速度从 0 → 非 0 时作为初始方向
        /// 3. 用于预测、回滚、动画朝向等
        /// </summary>
        public float VelocityAngle;

        /// <summary>
        /// 上一帧的位置（用于插值、预测、回滚）
        /// </summary>
        public Vector2 PreviousPosition;

        /// <summary>
        /// 移动标记（压缩存储：边界行为 + 其他标志）
        /// 布局：[其他标志 16位] [下 4位] [上 4位] [右 4位] [左 4位]
        /// 每个边界行为预留 4 位（支持 16 种行为扩展）
        /// </summary>
        public int Flags;

        /// <summary>
        /// 左边界行为（默认 Pass 允许穿越）
        /// 使用 Flags 的 bit 0-3 存储（4 位，支持 16 种行为）
        /// </summary>
        public BoundaryBehavior LeftBoundary
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (BoundaryBehavior)((Flags & MoveFlags.LeftBoundaryMask) >> MoveFlags.LeftBoundaryShift);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Flags = (Flags & ~MoveFlags.LeftBoundaryMask) | ((int)value << MoveFlags.LeftBoundaryShift);
        }

        /// <summary>
        /// 右边界行为（默认 Pass 允许穿越）
        /// 使用 Flags 的 bit 4-7 存储（4 位，支持 16 种行为）
        /// </summary>
        public BoundaryBehavior RightBoundary
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (BoundaryBehavior)((Flags & MoveFlags.RightBoundaryMask) >> MoveFlags.RightBoundaryShift);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Flags = (Flags & ~MoveFlags.RightBoundaryMask) | ((int)value << MoveFlags.RightBoundaryShift);
        }

        /// <summary>
        /// 上边界行为（默认 Pass 允许穿越）
        /// 使用 Flags 的 bit 8-11 存储（4 位，支持 16 种行为）
        /// </summary>
        public BoundaryBehavior TopBoundary
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (BoundaryBehavior)((Flags & MoveFlags.TopBoundaryMask) >> MoveFlags.TopBoundaryShift);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Flags = (Flags & ~MoveFlags.TopBoundaryMask) | ((int)value << MoveFlags.TopBoundaryShift);
        }

        /// <summary>
        /// 下边界行为（默认 Pass 允许穿越）
        /// 使用 Flags 的 bit 12-15 存储（4 位，支持 16 种行为）
        /// </summary>
        public BoundaryBehavior BottomBoundary
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (BoundaryBehavior)((Flags & MoveFlags.BottomBoundaryMask) >> MoveFlags.BottomBoundaryShift);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Flags = (Flags & ~MoveFlags.BottomBoundaryMask) | ((int)value << MoveFlags.BottomBoundaryShift);
        }

        /// <summary>
        /// 是否已删除（bit 16）
        /// 使用软删除避免频繁的内存移动，延迟到 Compact 时批量删除
        /// </summary>
        public bool Deleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Flags & MoveFlags.DeletedFlag) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value)
                    Flags |= MoveFlags.DeletedFlag;
                else
                    Flags &= ~MoveFlags.DeletedFlag;
            }
        }
    }

}
