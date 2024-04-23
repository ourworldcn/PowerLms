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
    /// 流程实例顶层类。
    /// </summary>
    [Index(nameof(TemplateId))]
    [Index(nameof(DocId))]
    [Comment("流程实例顶层类。")]
    public class OwWf : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属模板的Id。
        /// </summary>
        [Comment("所属模板的Id。")]
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// 流程文档Id。如申请单Id。
        /// </summary>
        [Comment("流程文档Id。")]
        public Guid? DocId { get; set; }

        /// <summary>
        /// 记录创建此流程的显示名。任何时刻可更改不必与模板对应。
        /// </summary>
        [Comment("此流程的显示名。")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注。")]
        public string Remark { get; set; }

    }

    /// <summary>
    /// 记录工作流实例节点的表。
    /// </summary>
    [Index(nameof(ParentId))]
    [Comment("记录工作流实例节点的表")]
    public class OwWfNode : GuidKeyObjectBase
    {
        /// <summary>
        /// 流程Id。
        /// </summary>
        [Comment("流程Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 到达此节点的时间，如果是第一个节点则是创建并保存节点的时间。
        /// </summary>
        [Comment("到达此节点的时间，如果是第一个节点则是创建并保存节点的时间。")]
        public DateTime ArrivalDateTime { get; set; }
    }

    /// <summary>
    /// 工作流实例节点详细信息。
    /// </summary>
    [Index(nameof(ParentId))]
    [Index(nameof(OpertorId))]
    [Comment("工作流实例节点详细信息。")]
    public class OwWfNodeItem : GuidKeyObjectBase
    {
        /// <summary>
        /// 流程Id。
        /// </summary>
        [Comment("流程Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 文档当前操作人的Id。
        /// </summary>
        [Comment("文档当前操作人的Id。")]
        public Guid? OpertorId { get; set; }

        /// <summary>
        /// 这里冗余额外记录一个操作人的显示名称。可随时更改。
        /// </summary>
        [Comment("这里冗余额外记录一个操作人的显示名称。可随时更改。")]
        public string OpertorDisplayName { get; set; }

        /// <summary>
        /// 审核批示。对非审批人，则是意见。
        /// </summary>
        [Comment("审核批示。对非审批人，则是意见。")]
        public string Comment { get; set; }

        /// <summary>
        /// 操作人类型，目前保留为0(审批者)。预计1=抄送人。对模板定义的时点记录，不会再跟随流程模板而变化。
        /// </summary>
        [Comment("操作人类型，目前保留为0(审批者)。预计1=抄送人。")]
        public byte OperationKind { get; set; }

        /// <summary>
        /// 是否审核通过。目前 <see cref="OwWfNodeItem"/> 中有唯一的审批人，这里记录的是审批人的处理种类。
        /// 仅对 <see cref="OperationKind"/> 为0 才有意义。
        /// </summary>
        [Comment("是否审核通过")]
        public bool IsSuccess { get; set; }

    }
}
