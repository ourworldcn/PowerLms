/*
 * Web API层 - 工作流控制器
 * 工作流实例管理 - DTO定义
 * 
 * 功能说明：
 * - 工作流相关的参数和返回值DTO类定义
 * - 工作流模板节点项DTO映射
 * - 支持AutoMapper自动映射
 * 
 * 技术特点：
 * - 遵循项目DTO命名约定
 * - 支持Swagger API文档生成
 * - JsonIgnore处理循环引用
 * 
 * 作者：GitHub Copilot
 * 创建时间：2024
 * 最后修改：2024-12-19 - 从主文件分离DTO定义并合并WfTemplate相关DTO
 */

using AutoMapper;
using PowerLms.Data;
using System.Text.Json.Serialization;

namespace PowerLmsWebApi.Dto
{
    #region 工作流实例DTO类

    /// <summary>
    /// 获取指定文档下一组操作人的信息功能的参数封装类。
    /// </summary>
    public class GetNextNodeItemsByDocIdParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 文档的Id。
        /// </summary>
        public Guid DocId { get; set; }
    }

    /// <summary>
    /// 获取指定文档下一组操作人的信息功能的返回值封装类。
    /// </summary>
    public class GetNextNodeItemsByDocIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 发送的下一个操作人的集合。可能为空，因为该模板仅有单一节点或已经到达最后一个节点，无法向下发送。
        /// </summary>
        public List<OwWfTemplateNodeItemDto> Result { get; set; } = new List<OwWfTemplateNodeItemDto>();

        /// <summary>
        /// 所属流程模板信息。
        /// </summary>
        public OwWfTemplate Template { get; set; }
    }

    /// <summary>
    /// 获取人员相关流转信息的参数封装类。
    /// </summary>
    public class GetWfByOpertorIdParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 过滤流文档状态的参数，0=待审批，1=已审批但仍在流转中，2=已结束的流程。
        /// </summary>
        public byte State { get; set; }
    }

    /// <summary>
    /// 获取人员相关流转信息的返回值封装类。
    /// </summary>
    public class GetWfByOpertorIdReturnDto : PagingReturnDtoBase<OwWfDto>
    {
    }

    /// <summary>
    /// 获取文档相关的流程信息的参数封装类。
    /// </summary>
    public class GetWfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要获取工作流实例的相关文档ID。
        /// </summary>
        public Guid EntityId { get; set; }
    }

    /// <summary>
    /// 工作流实例的封装类。
    /// </summary>
    [AutoMap(typeof(OwWf))]
    public class OwWfDto : OwWf
    {
        /// <summary>
        /// 该工作流的创建时间。
        /// </summary>
        public DateTime CreateDateTime { get; set; }
    }

    /// <summary>
    /// 获取文档相关的流程信息的返回值封装类。
    /// </summary>
    public class GetWfReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 相关工作流实例的集合。
        /// </summary>
        public List<OwWfDto> Result { get; set; } = new List<OwWfDto>();
    }

    /// <summary>
    /// 发送工作流文档功能的参数封装类。
    /// </summary>
    public class WfSendParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 工作流模板的Id。
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 流程文档Id。如申请单Id。
        /// </summary>
        public Guid DocId { get; set; }

        /// <summary>
        /// 审批结果，0通过，1终止。对起始节点只能是0.
        /// </summary>
        public int Approval { get; set; }

        /// <summary>
        /// 审核批示。
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 指定发送的下一个操作人的Id。必须符合流程定义。
        /// 如果是null或省略，对起始节点是仅保存批示意见，不进行流转。对最后一个节点这个属性被忽视。
        /// </summary>
        public Guid? NextOpertorId { get; set; }
    }

    /// <summary>
    /// 发送工作流文档功能的返回值封装类。
    /// </summary>
    public class WfSendReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 工作流实例的Id。
        /// </summary>
        public Guid WfId { get; set; }
    }

    #endregion

    #region 工作流模板节点项DTO类

    /// <summary>
    /// 工作流模板节点项DTO类。
    /// </summary>
    [AutoMap(typeof(OwWfTemplateNodeItem), ReverseMap = true)]
    public class OwWfTemplateNodeItemDto : OwWfTemplateNodeItem
    {
        /// <summary>
        /// 父节点ID，使用JsonIgnore避免序列化循环引用。
        /// </summary>
        [JsonIgnore]
        public new Guid? ParentId { get => base.ParentId; set => base.ParentId = value; }
    }

    #endregion

    #region WfTemplate控制器相关DTO类

    /// <summary>
    /// 获取模板首次发送时节点的信息功能的参数封装类。
    /// </summary>
    public class GetNextNodeItemsByKindCodeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 工作流模板的KindCode。
        /// </summary>
        public string KindCode { get; set; }
    }

    /// <summary>
    /// 获取模板首次发送时节点的信息功能的返回值封装类。
    /// </summary>
    public class GetNextNodeItemsByKindCodeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 发送的下一个操作人的集合。可能为空，因为该模板仅有单一节点，第一个人无法向下发送。
        /// </summary>
        public List<OwWfTemplateNodeItemDto> Result { get; set; } = new List<OwWfTemplateNodeItemDto>();

        /// <summary>
        /// 所属模板数据。
        /// </summary>
        public OwWfTemplate Template { get; set; }
    }

    /// <summary>
    /// 查询工作流模板类型码返回值封装类。
    /// </summary>
    public class GetAllWfTemplateKindCodeReturnDto : PagingReturnDtoBase<OwWfKindCodeDic>
    {
    }

    /// <summary>
    /// 设置指定的流程模板节点下所有明细功能的参数封装类。
    /// </summary>
    public class SetWfTemplateNodeItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 流程模板节点的Id。
        /// </summary>
        public Guid ParentId { get; set; }

        /// <summary>
        /// 流程模板节点明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<OwWfTemplateNodeItemDto> Items { get; set; } = new List<OwWfTemplateNodeItemDto>();
    }

    /// <summary>
    /// 设置指定的流程模板节点下所有明细功能的返回值封装类。
    /// </summary>
    public class SetWfTemplateNodeItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定流程模板节点下，所有明细的对象。
        /// </summary>
        public List<OwWfTemplateNodeItem> Result { get; set; } = new List<OwWfTemplateNodeItem>();
    }

    /// <summary>
    /// 批量删除工作流模板节点详细功能参数封装类。
    /// </summary>
    public class RemoveWfTemplateNodeItemPatamsDto : RemoveItemsParamsDtoBase
    {
    }

    /// <summary>
    /// 批量删除工作流模板节点详细功能返回值封装类。
    /// </summary>
    public class RemoveWfTemplateNodeItemReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点详细功能的返回值封装类。
    /// </summary>
    public class ModifyWfTemplateNodeItemReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点详细功能的参数封装类。
    /// </summary>
    public class ModifyWfTemplateNodeItemParamsDto : ModifyParamsDtoBase<OwWfTemplateNodeItem>
    {
    }

    /// <summary>
    /// 查询工作流模板节点详细对象返回值封装类。
    /// </summary>
    public class GetAllWfTemplateNodeItemReturnDto : PagingReturnDtoBase<OwWfTemplateNodeItem>
    {
    }

    /// <summary>
    /// 增加工作流模板节点详细对象功能参数封装类。
    /// </summary>
    public class AddWfTemplateNodeItemParamsDto : AddParamsDtoBase<OwWfTemplateNodeItem>
    {
    }

    /// <summary>
    /// 增加工作流模板节点详细对象功能返回值封装类。
    /// </summary>
    public class AddWfTemplateNodeItemReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 批量删除工作流模板节点功能参数封装类。
    /// </summary>
    public class RemoveWfTemplateNodePatamsDto : RemoveItemsParamsDtoBase
    {
        /// <summary>
        /// 是否强制删除已有的子项，true=强制删除，false有子项则不能删除。
        /// </summary>
        public bool IsRemoveChildren { get; set; }
    }

    /// <summary>
    /// 批量删除工作流模板节点功能返回值封装类。
    /// </summary>
    public class RemoveWfTemplateNodeReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的返回值封装类。
    /// </summary>
    public class ModifyWfTemplateNodeReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的参数封装类。
    /// </summary>
    public class ModifyWfTemplateNodeParamsDto : ModifyParamsDtoBase<OwWfTemplateNode>
    {
    }

    /// <summary>
    /// 查询工作流模板节点对象返回值封装类。
    /// </summary>
    public class GetAllWfTemplateNodeReturnDto : PagingReturnDtoBase<OwWfTemplateNode>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能参数封装类。
    /// </summary>
    public class AddWfTemplateNodeParamsDto : AddParamsDtoBase<OwWfTemplateNode>
    {
        /// <summary>
        /// 前向节点的Id。省略或为空则不连接的前向节点。
        /// </summary>
        public Guid? PreNodeId { get; set; }
    }

    /// <summary>
    /// 增加工作流模板节点对象功能返回值封装类。
    /// </summary>
    public class AddWfTemplateNodeReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 批量删除工作流模板功能参数封装类。
    /// </summary>
    public class RemoveWorkflowTemplatePatamsDto : RemoveItemsParamsDtoBase
    {
    }

    /// <summary>
    /// 批量删除工作流模板功能返回值封装类。
    /// </summary>
    public class RemoveWorkflowTemplateReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板功能的返回值封装类。
    /// </summary>
    public class ModifyWorkflowTemplateReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板功能的参数封装类。
    /// </summary>
    public class ModifyWorkflowTemplateParamsDto : ModifyParamsDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 查询工作流模板对象返回值封装类。
    /// </summary>
    public class GetAllWorkflowTemplateReturnDto : PagingReturnDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 增加工作流模板对象功能参数封装类。
    /// </summary>
    public class AddWorkflowTemplateParamsDto : AddParamsDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 增加工作流模板对象功能返回值封装类。
    /// </summary>
    public class AddWorkflowTemplateReturnDto : AddReturnDtoBase
    {
    }

    #endregion
}