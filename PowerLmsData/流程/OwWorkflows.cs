using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    /// <summary>
    /// 流程模板总表。
    /// </summary>
    [Index(nameof(OrgId))]
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
        public string DocTypeCode { get; set; }

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
    }

    /// <summary>
    /// 工作流模板内节点表。当前节点Id就是本对象自身Id。
    /// </summary>
    [Index(nameof(ParentId))]
    [Index(nameof(NextId))]
    public class OwWfTemplateNode : GuidKeyObjectBase
    {
        public OwWfTemplateNode()
        {
        }

        /// <summary>
        /// 流程Id。
        /// </summary>
        [Comment("流程Id")]
        public Guid ParentId { get; set; }

        /// <summary>
        /// 下一个节点的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。
        /// </summary>
        /// <remarks>如:
        /// <c>
        /// </c></remarks>
        [Comment("下一个节点的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。")]
        public Guid? NextId { get; set; }

        /// <summary>
        /// 拒绝后的操作，1 = 终止,2=回退
        /// </summary>
        public byte RejectOpertion { get; set; }

        /// <summary>
        /// 此节点的显示名。
        /// </summary>
        [Comment("此节点的显示名。")]
        public string DisplayName { get; set; }

        #region 前/后置守卫条件

        /// <summary>
        /// 前/后置守卫条件的Json字符串。暂未启用。
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

        #region 导航属性
        ///// <summary>
        ///// 详细项集合的导航属性。
        ///// </summary>
        //[InverseProperty(nameof(OwWorkflowTemplateNodeItem.ParentId))]
        //public virtual List<OwWorkflowTemplateNodeItem> Children { get; set; } = new List<OwWorkflowTemplateNodeItem>();
        #endregion 导航属性
    }

    /// <summary>
    /// 节点详细信息类。
    /// </summary>
    [Index(nameof(ParentId))]
    public class OwWfTemplateNodeItem : GuidKeyObjectBase
    {
        #region 导航属性
        /// <summary>
        /// 所属节点Id。
        /// </summary>
        [Comment("所属节点Id。")]
        public Guid ParentId { get; set; }
        #endregion 导航属性

        /// <summary>
        /// 参与者(员工)Id。
        /// </summary>
        [Comment("参与者(员工)Id。")]
        public Guid ActorId { get; set; }

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

    /// <summary>
    /// 记录工作流实例的表
    /// </summary>
    public class OwWorkflow
    {
        /// <summary>
        /// 流转的类型，当前保留为0.
        /// </summary>
        public byte WfType { get; set; }

        /// <summary>
        /// 文档的唯一Id。
        /// 与 CurrentNodeId 形成联合主键。
        /// </summary>
        public Guid DocId { get; set; }

        /// <summary>
        /// 文档当前所处节点。
        /// 与 DocId 形成联合主键。
        /// </summary>
        public Guid CurrentNodeId { get; set; }

        /// <summary>
        /// 审核批示。
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 是否审核通过。
        /// </summary>
        public bool IsSuccess { get; set; }
    }

    /// <summary>
    /// 工作流历史记录。冗余记录。员工离职 软删除。
    /// </summary>
    public class OwWorkflowHistory
    {
        /// <summary>
        /// 文档Id。
        /// </summary>
        public Guid DocId { get; set; }

        /// <summary>
        /// 所处节点Id。
        /// </summary>
        public Guid CurrentNodeId { get; set; }

        /// <summary>
        /// 到达该节点的时间。
        /// </summary>
        public DateTime WorldDateTime { get; set; }

    }
}
