using Microsoft.EntityFrameworkCore;
using OW.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PowerLms.Data
{
    /// <summary>
    /// 流程类型表
    /// </summary>
    public class OwWfKindCodeDic
    {
        /// <summary>
        /// 文档类型Id(唯一键)。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个ASCII字符(建议仅用低127个字符，否则在json等UTF8编码时，视同多字节字符，客户端需要特别注意)。
        /// </summary>
        [Key]
        [MaxLength(16), Unicode(false)]
        [Comment("文档类型Id。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个字符，仅支持英文。")]
        public string Id { get; set; }

        /// <summary>
        /// 此流程的显示名。最长64个字符。
        /// </summary>
        [Comment("此流程的显示名。")]
        [MaxLength(64)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注。")]
        public string Remark { get; set; }

    }

    /// <summary>
    /// 流程模板总表。
    /// </summary>
    [Index(nameof(OrgId), nameof(KindCode))]
    [Comment("流程模板总表")]
    public class OwWfTemplate : GuidKeyObjectBase, ICreatorInfo
    {
        /// <summary>
        /// 所属机构Id。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 文档类型Id。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个字符，仅支持英文。
        /// </summary>
        [MaxLength(16), Unicode(false)]
        [Comment("文档类型Id。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个字符，仅支持英文。")]
        public string KindCode { get; set; }

        /// <summary>
        /// 此流程的显示名。
        /// </summary>
        [Comment("此流程的显示名。")]
        public string DisplayName { get; set; }

        #region ICreatorInfo接口

        /// <summary>
        /// 创建者的唯一标识。
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间。
        /// </summary>
        [Comment("创建的时间")]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        #endregion ICreatorInfo接口

        #region 导航属性
        public virtual List<OwWfTemplateNode> Children { get; set; } = new List<OwWfTemplateNode>();
        #endregion 导航属性
    }

    /// <summary>
    /// 工作流模板内节点表。
    /// </summary>
    [Index(nameof(NextId))]
    [Comment("工作流模板内节点表")]
    public class OwWfTemplateNode : GuidKeyObjectBase
    {
        public OwWfTemplateNode()
        {
        }

        #region 导航属性

        /// <summary>
        /// 流程模板Id。
        /// </summary>
        [Comment("流程模板Id")]
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 模板对象。
        /// </summary>
        [JsonIgnore]
        public virtual OwWfTemplate Parent { get; set; }

        /// <summary>
        /// 所有操作人的详细信息集合。
        /// </summary>
        public virtual List<OwWfTemplateNodeItem> Children { get; set; } = new List<OwWfTemplateNodeItem>();
        #endregion 导航属性

        /// <summary>
        /// 下一个操作人的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。为null标识最后一个节点。
        /// </summary>
        /// <remarks>如:
        /// <c>
        /// </c></remarks>
        [Comment("下一个操作人的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。为null标识最后一个节点。")]
        public Guid? NextId { get; set; }

        /*/// <summary>
        /// 拒绝后的操作，1 = 终止,2=回退
        /// </summary>
        [Comment("拒绝后的操作，1 = 终止,2=回退")]
        public byte RejectOperation { get; set; }   //直接终止
        */

        /// <summary>
        /// 此节点的显示名。
        /// </summary>
        [Comment("此节点的显示名。")]
        public string DisplayName { get; set; }

        #region 前/后置守卫条件

        /// <summary>
        /// 前/后置守卫条件的Json字符串。暂未启用，针对网状结构判断实例是否可流转的条件。
        /// </summary>
        /// <remarks>如 <c>
        /// [
        /// {
        /// "NumberCondition": {
        /// "PropertyName": "Count",
        /// "MinValue": 0,
        /// "MaxValue": null,
        /// "Subtrahend": 0,
        /// "Modulus": 42,
        /// "MinRemainder": 0,
        /// "MaxRemainder": 6
        /// }
        /// }
        /// ]
        /// </c></remarks>
        [Comment(" 前/后置守卫条件的Json字符串。暂未启用。")]
        [Unicode(false)]
        public string GuardJsonString { get; set; }
        #endregion 前/后置守卫条件

    }

    /// <summary>
    /// 节点详细信息类。
    /// </summary>
    [Comment("节点详细信息类")]
    public class OwWfTemplateNodeItem : GuidKeyObjectBase
    {
        #region 导航属性
        /// <summary>
        /// 所属节点Id。
        /// </summary>
        [Comment("所属节点Id。")]
        [ForeignKey(nameof(Parent))]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 模板节点对象。
        /// </summary>
        [JsonIgnore]
        public virtual OwWfTemplateNode Parent { get; set; }

        #endregion 导航属性

        /// <summary>
        /// 操作人Id。
        /// </summary>
        [Comment("操作人Id。")]
        public Guid? OpertorId { get; set; }

        /// <summary>
        /// 参与者类型，目前保留为0。
        /// 预计1=抄送人，此时则无视优先度——任意一个审批人得到文档时，所有抄送人同时得到此文档。
        /// </summary>
        [Comment("参与者类型，目前保留为0。预计1=抄送人，此时则无视优先度——任意一个审批人得到文档时，所有抄送人同时得到此文档。")]
        public byte OperationKind { get; set; }

        /// <summary>
        /// 优先级。0最高，1其次，以此类推...。仅当节点类型0时有效。
        /// </summary>
        [Comment("优先级。0最高，1其次，以此类推...。仅当节点类型0时有效。")]
        public int Priority { get; set; }

    }


}
