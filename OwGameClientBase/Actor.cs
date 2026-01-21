using System.Collections.Generic;

namespace OW.Game.Client
{
    /// <summary>
    /// 角色（Actor）是 ECS 系统中的“行为载体”。
    /// 
    /// 设计原则：
    /// 1. Actor 是引用类型（class），因为它可能包含大量扩展信息、引用、字典等。
    /// 2. Actor 支持继承（非 sealed），以便玩家、Boss、NPC 等可以通过派生类实现复杂行为。
    /// 3. Actor 不存储具体数据（如位置、速度），这些属于 State（组件）。
    /// 4. Actor 存储组件索引（MoveIndex），用于快速定位对应的组件。
    /// 5. Actor 的行为通过虚方法实现，简单角色可 sealed 派生类以获得虚方法去虚拟化优化。
    /// </summary>
    public abstract class Actor
    {
        /// <summary>
        /// 角色的唯一 ID（由 <see cref="ActorManager"/> 分配）。
        /// </summary>
        public int Id;

        /// <summary>
        /// 移动组件在 VectorStorageService.Moves 中的索引。
        /// -1 表示该角色没有移动组件。
        /// </summary>
        public int MoveIndex = -1;

        /// <summary>
        /// 是否已删除（软删除标志）。
        /// 使用软删除避免频繁的内存移动，延迟到 Compact 时批量删除。
        /// </summary>
        public bool Deleted;

        /// <summary>
        /// 扩展信息字典，用于存储任意自定义数据。
        /// 例如：阵营、标签、任务状态、AI 参数等。
        /// </summary>
        public Dictionary<string, object> Tags = new();

        /// <summary>
        /// 碰撞时调用（未来扩展）。
        /// </summary>
        /// <param name="id"></param>
        public virtual void OnCollision(int id)
        {
        }

        /// <summary>
        /// 行为更新入口。
        /// 复杂角色（玩家、Boss）可以重写此方法实现自定义逻辑。
        /// 简单角色（NPC）可以使用 sealed 派生类以获得虚方法去虚拟化优化。
        /// </summary>
        public virtual void Update(float dt)
        {
        }

        /// <summary>
        /// 角色受到伤害时调用（未来扩展）。
        /// </summary>
        public virtual void OnDamage(int amount)
        {
        }

        /// <summary>
        /// 角色死亡时调用（未来扩展）。
        /// </summary>
        public virtual void OnDeath()
        {
        }
    }
}
