using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLms.Data
{
    public class OwWorkflowTemplate : GuidKeyObjectBase
    {
        /// <summary>
        /// 机构Id。
        /// </summary>
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 文档类型Id。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个字符，仅支持英文。
        /// </summary>
        [MaxLength(16), Unicode(false)]
        public string DocTypeCode { get; set; }

        /// <summary>
        /// 此流转的显示名。
        /// </summary>
        public string DisplayName { get; set; }


    }

    /// <summary>
    /// 工作流模板主数据类。
    /// </summary>
    [Index(nameof(WfNodeType), nameof(CurrentNodeId))]
    public class OwWorkflowTemplateNode : GuidKeyObjectBase
    {
        public OwWorkflowTemplateNode()
        {
        }

        /// <summary>
        /// 流程Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 类型，目前保留为0。预计1=抄送，即会在文档转向下一个节点成功时，会抄送到某些节点，但并不需要这些节点进行动作。
        /// </summary>
        public byte WfNodeType { get; set; }

        /// <summary>
        /// 当前所处节点Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。
        /// </summary>
        public Guid CurrentNodeId { get; set; }


        /// <summary>
        /// 下一个节点的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。
        /// </summary>
        /// <remarks>如:
        /// <c>
        /// a1,a2 => b1(0),x(1)
        /// x=>c1
        /// a3,a4=>b2
        /// b1,b2=>c1
        /// c1=>vp1
        /// vp1=>ceo
        /// </c></remarks>
        public Guid? NextNodeId { get; set; }

        /// <summary>
        /// 优先度。0最高，1其次，以此类推
        /// </summary>
        public int Prioriteit { get; set; }

        /// <summary>
        /// 拒绝后的操作，1 = 终止,2=回退
        /// </summary>
        public byte RejectOpertion { get; set; }

        /// <summary>
        /// 此流转的显示名。
        /// </summary>
        public string DisplayName { get; set; }

        #region 前/后置守卫条件

        /// <summary>
        /// 前/后置守卫条件的Json字符串。
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
        public string GuardJsonString { get; set; }
        #endregion 前/后置守卫条件
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
    /// 工作流历史记录。
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
