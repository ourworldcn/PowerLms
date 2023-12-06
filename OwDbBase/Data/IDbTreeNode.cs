using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace OW.Data
{
    /// <summary>
    /// 存储在数据库中树状节点的基础接口。
    /// </summary>
    /// <typeparam name="TNode">节点类型。</typeparam>
    public interface IDbTreeNode<TNode> where TNode : IEntityWithSingleKey<Guid>
    {
        /// <summary>
        /// 所属槽导航属性。
        /// </summary>
        [JsonIgnore]
        [MaybeNull]
        public abstract TNode Parent { get; set; }

        /// <summary>
        /// 所属槽Id。
        /// </summary>
        [ForeignKey(nameof(Parent))]
        public abstract Guid? ParentId { get; set; }

        /// <summary>
        /// 拥有的子物品或槽。
        /// </summary>
        public abstract List<TNode> Children { get; set; }
    }

}
