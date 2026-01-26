using System;
using System.Collections.Generic;
using System.Numerics;

namespace OW.Game.Client
{
    /// <summary>
    /// 碰撞事件（用于延迟分发，避免在热路径中调用虚函数）。
    /// </summary>
    public struct CollisionEvent
    {
        public int A;
        public int B;

        public CollisionEvent(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    /// <summary>
    /// 碰撞网格（Uniform Grid）。
    /// 
    /// 设计语义：
    /// 1. 使用固定大小的网格对场景进行空间划分。
    /// 2. 每个格子存储一组实体索引（在 EcsStorageService 中的索引）。
    /// 3. 碰撞检测时只在“同格子 + 邻居格子”中做检测，避免全局 N^2。
    /// 4. 网格只在每帧构建一次，不在热路径中频繁重建。
    /// </summary>
    public sealed class CollisionGrid
    {
        /// <summary>
        /// 网格列数。
        /// </summary>
        public readonly int Columns;

        /// <summary>
        /// 网格行数。
        /// </summary>
        public readonly int Rows;

        /// <summary>
        /// 每个格子的宽度。
        /// </summary>
        public readonly float CellWidth;

        /// <summary>
        /// 每个格子的高度。
        /// </summary>
        public readonly float CellHeight;

        /// <summary>
        /// 每个格子中的实体索引列表。
        /// 索引为：cellIndex = row * Columns + col。
        /// </summary>
        private readonly List<int>[] _cells;

        public CollisionGrid(SceneInfo scene, float cellSize)
        {
            if (cellSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellSize));

            CellWidth = cellSize;
            CellHeight = cellSize;

            float width = scene.Width;
            float height = scene.Height;

            Columns = (int)(width / cellSize) + 1;
            Rows = (int)(height / cellSize) + 1;

            _cells = new List<int>[Columns * Rows];
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new List<int>(16);
        }

        /// <summary>
        /// 清空所有格子的实体列表。
        /// 每帧开始时调用一次。
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _cells.Length; i++)
                _cells[i].Clear();
        }

        /// <summary>
        /// 将一个实体加入对应的网格格子。
        /// position 使用 MoveState.Position。
        /// </summary>
        public void Add(int entityIndex, in Vector2 position, in SceneInfo scene)
        {
            int cx = (int)((position.X - scene.MinX) / CellWidth);
            int cy = (int)((position.Y - scene.MinY) / CellHeight);

            if (cx < 0 || cy < 0 || cx >= Columns || cy >= Rows)
                return;

            int cellIndex = cy * Columns + cx;
            _cells[cellIndex].Add(entityIndex);
        }

        /// <summary>
        /// 获取指定格子的实体列表。
        /// </summary>
        public List<int> GetCell(int col, int row)
        {
            if (col < 0 || row < 0 || col >= Columns || row >= Rows)
                return null;

            return _cells[row * Columns + col];
        }
    }

    /// <summary>
    /// 碰撞数学工具（CollisionMath）。
    /// 
    /// 设计语义：
    /// 1. 提供纯数学的碰撞检测函数。
    /// 2. 所有函数为 public static，便于在特殊情况下被外部调用。
    /// 3. 不依赖任何引用类型，适合内联和 ECS 热路径。
    /// </summary>
    public static class CollisionMath
    {
        /// <summary>
        /// 检查两个实体是否发生碰撞（圆形 vs 圆形，多形状）。
        /// 
        /// 说明：
        /// - 使用 MoveState.Position 作为实体位置。
        /// - 使用 ColliderState.ShapeStart / ShapeCount 定位形状。
        /// - 使用 ColliderShape.Offset / Radius 计算圆心和半径。
        /// - 返回是否碰撞，并输出最小穿透深度（penetration）。
        /// </summary>
        public static bool CheckEntityCollision(
            int a,
            int b,
            Span<MoveState> moves,
            Span<ColliderState> colliders,
            Span<ColliderShape> shapes,
            out float penetration)
        {
            penetration = 0f;

            ref var colA = ref colliders[a];
            ref var colB = ref colliders[b];

            Vector2 posA = moves[a].Position;
            Vector2 posB = moves[b].Position;

            int startA = colA.ShapeStart;
            int startB = colB.ShapeStart;

            int countA = colA.ShapeCount;
            int countB = colB.ShapeCount;

            bool collided = false;
            float minPenetration = float.MaxValue;

            for (int i = 0; i < countA; i++)
            {
                ref var sA = ref shapes[startA + i];
                Vector2 centerA = posA + sA.Offset;

                for (int j = 0; j < countB; j++)
                {
                    ref var sB = ref shapes[startB + j];
                    Vector2 centerB = posB + sB.Offset;

                    float r = sA.Radius + sB.Radius;
                    Vector2 delta = centerB - centerA;
                    float distSq = delta.LengthSquared();

                    if (distSq <= r * r)
                    {
                        float dist = MathF.Sqrt(distSq);
                        float pen = r - dist;

                        if (pen < minPenetration)
                            minPenetration = pen;

                        collided = true;
                    }
                }
            }

            if (collided)
            {
                penetration = minPenetration;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 碰撞服务（CollisionService）
    /// 
    /// 设计语义：
    /// 1. 使用 CollisionGrid 进行空间划分，减少无意义的碰撞检测。
    /// 2. 使用 CollisionMath.CheckEntityCollision 进行圆形多形状检测。
    /// 3. 使用事件队列延迟分发碰撞（避免在热路径中调用虚函数）。
    /// 4. 不负责边界修正（由 MovementService 或其他系统处理）。
    /// </summary>
    public sealed class CollisionService
    {
        private readonly EcsStorageService _storage;
        private readonly CollisionGrid _grid;

        /// <summary>
        /// 碰撞事件队列。
        /// </summary>
        private readonly List<CollisionEvent> _events = new();

        public CollisionService(EcsStorageService storage, float cellSize)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _grid = new CollisionGrid(storage.Scene, cellSize);
        }

        /// <summary>
        /// 执行碰撞检测（热路径）。
        /// 
        /// 流程：
        /// 1. 清空网格。
        /// 2. 将所有启用碰撞的实体加入网格。
        /// 3. 遍历每个格子，只在“本格子 + 邻居格子”中做检测。
        /// 4. 使用 CollisionMath.CheckEntityCollision 进行检测。
        /// 5. 将碰撞结果写入事件队列。
        /// </summary>
        public void DetectCollisions()
        {
            var moves = _storage.Moves.AsSpan();
            var colliders = _storage.Colliders.AsSpan();
            var shapes = _storage.ColliderShapes.AsSpan();
            var scene = _storage.Scene;

            _events.Clear();
            _grid.Clear();

            int count = colliders.Length;

            // 1. 将所有有碰撞形状的实体加入网格
            for (int i = 0; i < count; i++)
            {
                ref var col = ref colliders[i];
                
                // ? 跳过已软删除的碰撞组件
                if (col.Deleted)
                    continue;
                
                // 跳过无碰撞形状的实体（体积为零）
                if (col.ShapeCount <= 0)
                    continue;

                Vector2 pos = moves[i].Position;
                _grid.Add(i, in pos, scene);
            }

            // 2. 遍历每个格子，做局部碰撞检测
            for (int row = 0; row < _grid.Rows; row++)
            {
                for (int col = 0; col < _grid.Columns; col++)
                {
                    var cell = _grid.GetCell(col, row);
                    if (cell == null || cell.Count == 0)
                        continue;

                    ProcessCell(cell, col, row, moves, colliders, shapes);
                }
            }
        }

        /// <summary>
        /// 处理一个格子及其邻居格子的碰撞检测。
        /// </summary>
        private void ProcessCell(
            List<int> cell,
            int col,
            int row,
            Span<MoveState> moves,
            Span<ColliderState> colliders,
            Span<ColliderShape> shapes)
        {
            // 1. 本格子内部两两检测
            int localCount = cell.Count;
            for (int i = 0; i < localCount; i++)
            {
                int a = cell[i];
                ref var colA = ref colliders[a];
                
                // ? 跳过已软删除或无碰撞形状的实体
                if (colA.Deleted || colA.ShapeCount <= 0)
                    continue;

                for (int j = i + 1; j < localCount; j++)
                {
                    int b = cell[j];
                    ref var colB = ref colliders[b];
                    
                    // ? 跳过已软删除或无碰撞形状的实体
                    if (colB.Deleted || colB.ShapeCount <= 0)
                        continue;

                    // LayerMask 过滤
                    if ((colA.Layer & colB.Layer) == 0)
                        continue;

                    if (CollisionMath.CheckEntityCollision(a, b, moves, colliders, shapes, out _))
                    {
                        _events.Add(new CollisionEvent(a, b));
                    }
                }
            }

            // 2. 与右、下、右下、左下格子检测（避免重复）
            ProcessNeighborCell(cell, col + 1, row, moves, colliders, shapes);
            ProcessNeighborCell(cell, col, row + 1, moves, colliders, shapes);
            ProcessNeighborCell(cell, col + 1, row + 1, moves, colliders, shapes);
            ProcessNeighborCell(cell, col - 1, row + 1, moves, colliders, shapes);
        }

        /// <summary>
        /// 处理与邻居格子的碰撞检测。
        /// </summary>
        private void ProcessNeighborCell(
            List<int> cell,
            int ncol,
            int nrow,
            Span<MoveState> moves,
            Span<ColliderState> colliders,
            Span<ColliderShape> shapes)
        {
            var neighbor = _grid.GetCell(ncol, nrow);
            if (neighbor == null || neighbor.Count == 0)
                return;

            int countA = cell.Count;
            int countB = neighbor.Count;

            for (int i = 0; i < countA; i++)
            {
                int a = cell[i];
                ref var colA = ref colliders[a];
                
                // ? 跳过已软删除或无碰撞形状的实体
                if (colA.Deleted || colA.ShapeCount <= 0)
                    continue;

                for (int j = 0; j < countB; j++)
                {
                    int b = neighbor[j];
                    ref var colB = ref colliders[b];
                    
                    // ? 跳过已软删除或无碰撞形状的实体
                    if (colB.Deleted || colB.ShapeCount <= 0)
                        continue;

                    // LayerMask 过滤
                    if ((colA.Layer & colB.Layer) == 0)
                        continue;

                    if (CollisionMath.CheckEntityCollision(a, b, moves, colliders, shapes, out _))
                    {
                        _events.Add(new CollisionEvent(a, b));
                    }
                }
            }
        }

        /// <summary>
        /// 分发碰撞事件（非热路径）。
        /// 
        /// 说明：
        /// - 允许调用虚函数（Actor.OnCollision）。
        /// - 允许执行复杂逻辑（伤害、击退、特效等）。
        /// </summary>
        public void DispatchEvents()
        {
            var actors = _storage.Actors.AsSpan();

            foreach (var evt in _events)
            {
                actors[evt.A].OnCollision(evt.B);
                actors[evt.B].OnCollision(evt.A);
            }

            _events.Clear();
        }
    }
}
