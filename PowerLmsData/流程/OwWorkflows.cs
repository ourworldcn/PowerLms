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
    /// <summary>
    /// 工作流模板主数据类。
    /// </summary>
    [Index(nameof(WfType), nameof(DocTypeCode), nameof(CurrentNodeId))]
    public class OwWorkflowTemplate : GuidKeyObjectBase
    {
        public OwWorkflowTemplate()
        {
        }

        /// <summary>
        /// 类型，目前保留为0。预计1=抄送，即会在文档转向下一个节点成功时，会抄送到某些节点，但并不需要这些节点进行动作。
        /// </summary>
        public byte WfType { get; set; }

        /// <summary>
        /// 文档类型Id。文档的类型Code,系统多方预先约定好，所有商户公用，最长16个字符，仅支持英文。
        /// </summary>
        [MaxLength(16), Unicode(false)]
        public string DocTypeCode { get; set; }

        /// <summary>
        /// 当前所处节点Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。
        /// 这是自父子关系的多方。即多个结点将归属到一个节点。
        /// </summary>
        public Guid CurrentNodeId { get; set; }

        /// <summary>
        /// 下一个节点的Id。通常都是职员Id。遇特殊情况，工作流引擎自行解释。
        /// 这是自父子关系的多方。即多个结点将归属到一个节点。
        /// </summary>
        public Guid? NextNodeId { get; set; }

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
    }

    /// <summary>
    /// 工作流历史记录，当前未启用。
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
