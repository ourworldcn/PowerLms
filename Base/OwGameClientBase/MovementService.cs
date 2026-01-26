using System;
using System.Numerics;

namespace OW.Game.Client
{
    /// <summary>
    /// MovementService 是 ECS 的移动系统。
    /// 
    /// 设计目标：
    /// 1. 批量更新所有 MoveState（连续内存，极高性能）。
    /// 2. 仅执行“理想位移”，不处理碰撞（碰撞系统未来扩展）。
    /// 3. 更新速度方向角（VelocityAngle）与速率（Speed）。
    /// 4. 保存上一帧位置（PreviousPosition），用于插值、预测、回滚。
    /// </summary>
    public sealed class MovementService
    {
        private readonly EcsStorageService _storage;

        /// <summary>
        /// 构造移动系统，依赖统一存储服务。
        /// </summary>
        public MovementService(EcsStorageService storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <summary>
        /// 更新所有移动组件。
        /// </summary>
        /// <param name="deltaTime">时间增量（秒）</param>
        public void Update(float deltaTime)
        {
            // 获取连续内存的 MoveState 列表
            var moves = _storage.Moves.AsSpan();

            for (int i = 0; i < moves.Length; i++)
            {
                ref var state = ref moves[i];

                // ? 跳过已软删除的实体
                if (state.Deleted)
                    continue;

                // 保存上一帧位置（用于插值、预测、回滚）
                state.PreviousPosition = state.Position;

                // 如果速度不为零，更新方向角与速率
                if (state.Velocity != Vector2.Zero)
                {
                    state.Speed = state.Velocity.Length();
                    state.VelocityAngle = MathF.Atan2(state.Velocity.Y, state.Velocity.X);
                }
                else
                {
                    state.Speed = 0f;
                    // VelocityAngle 保留上一次方向
                }

                // 理想位移
                state.Position += state.Velocity * deltaTime;

                // 处理场景边界
                ProcessBoundaries(ref state, _storage.Scene);
            }
        }

        /// <summary>
        /// 处理场景边界行为
        /// </summary>
        private static void ProcessBoundaries(ref MoveState state, SceneInfo scene)
        {
            // 左边界
            if (state.Position.X < scene.MinX)
            {
                switch (state.LeftBoundary)
                {
                    case BoundaryBehavior.Clamp:
                        state.Position.X = scene.MinX;
                        state.Velocity.X = 0;
                        break;
                    case BoundaryBehavior.Bounce:
                        state.Position.X = scene.MinX + (scene.MinX - state.Position.X);
                        state.Velocity.X = -state.Velocity.X;
                        break;
                    // BoundaryBehavior.Pass: 无操作，允许穿越
                }
            }

            // 右边界
            if (state.Position.X > scene.MaxX)
            {
                switch (state.RightBoundary)
                {
                    case BoundaryBehavior.Clamp:
                        state.Position.X = scene.MaxX;
                        state.Velocity.X = 0;
                        break;
                    case BoundaryBehavior.Bounce:
                        state.Position.X = scene.MaxX - (state.Position.X - scene.MaxX);
                        state.Velocity.X = -state.Velocity.X;
                        break;
                }
            }

            // 上边界
            if (state.Position.Y < scene.MinY)
            {
                switch (state.TopBoundary)
                {
                    case BoundaryBehavior.Clamp:
                        state.Position.Y = scene.MinY;
                        state.Velocity.Y = 0;
                        break;
                    case BoundaryBehavior.Bounce:
                        state.Position.Y = scene.MinY + (scene.MinY - state.Position.Y);
                        state.Velocity.Y = -state.Velocity.Y;
                        break;
                }
            }

            // 下边界
            if (state.Position.Y > scene.MaxY)
            {
                switch (state.BottomBoundary)
                {
                    case BoundaryBehavior.Clamp:
                        state.Position.Y = scene.MaxY;
                        state.Velocity.Y = 0;
                        break;
                    case BoundaryBehavior.Bounce:
                        state.Position.Y = scene.MaxY - (state.Position.Y - scene.MaxY);
                        state.Velocity.Y = -state.Velocity.Y;
                        break;
                }
            }
        }
    }
}
