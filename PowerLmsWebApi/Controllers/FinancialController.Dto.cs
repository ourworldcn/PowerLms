﻿using PowerLms.Data;
using PowerLmsWebApi.Controllers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Dto
{
    #region 结算单明细

    /// <summary>
    /// 获取结算单明细增强接口功能参数封装类。
    /// </summary>
    public class GetPlInvoicesItemParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取结算单明细增强接口功能返回值封装类。
    /// </summary>
    public class GetPlInvoicesItemReturnDto : PagingReturnDtoBase<GetPlInvoicesItemItem>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetPlInvoicesItemItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetPlInvoicesItemItem()
        {
            //申请单 job 费用实体 申请明细的余额（未结算）
        }

        /// <summary>
        /// 结算单详细项
        /// </summary>
        public PlInvoicesItem InvoicesItem { get; set; }

        /// <summary>
        /// 结算单。
        /// </summary>
        public PlInvoices Invoices { get; set; }

        /// <summary>
        /// 相关的任务对象。
        /// </summary>
        public PlJob PlJob { get; set; }

        /// <summary>
        /// 相关的申请单对象。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }

        /// <summary>
        /// 申请单明细对象。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }

        /// <summary>
        /// 相关的结算单对象。
        /// </summary>
        public PlInvoices Parent { get; set; }
    }

    /// <summary>
    /// 结算单确认功能参数封装类。
    /// </summary>
    public class ConfirmPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 结算单的Id集合。
        /// </summary>
        public List<Guid> Ids { get; set; }

        /// <summary>
        /// 是否确认结算单。true 表示确认，false 表示取消确认。
        /// </summary>
        public bool IsConfirm { get; set; }
    }

    /// <summary>
    /// 结算单确认功能返回值封装类。
    /// </summary>
    public class ConfirmPlInvoicesReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetPlInvoicesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<PlInvoicesItem> Items { get; set; } = new List<PlInvoicesItem>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetPlInvoicesItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<PlInvoicesItem> Result { get; set; } = new List<PlInvoicesItem>();
    }

    /// <summary>
    /// 标记删除结算单明细功能的参数封装类。
    /// </summary>
    public class RemovePlInvoicesItemParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除结算单明细功能的返回值封装类。
    /// </summary>
    public class RemovePlInvoicesItemReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有结算单明细功能的返回值封装类。
    /// </summary>
    public class GetAllPlInvoicesItemReturnDto : PagingReturnDtoBase<PlInvoicesItem>
    {
    }

    /// <summary>
    /// 获取申请单明细增强接口功能参数封装类。
    /// </summary>
    public class GetDocFeeRequisitionItemParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取申请单明细增强接口功能返回值封装类。
    /// </summary>
    public class GetDocFeeRequisitionItemReturnDto : PagingReturnDtoBase<GetDocFeeRequisitionItemItem>
    {

    }

    /// <summary>
    /// 获取申请单明细增强接口功能的返回值中的元素类型。
    /// </summary>
    public class GetDocFeeRequisitionItemItem
    {
        /// <summary>
        /// 申请单明细对象。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }

        /// <summary>
        /// 相关的任务对象。
        /// </summary>
        public PlJob PlJob { get; set; }

        /// <summary>
        /// 相关的申请单对象。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }

        /// <summary>
        /// 相关的费用对象。
        /// </summary>
        public DocFee DocFee { get; set; }

        /// <summary>
        /// 申请单明细对象未结算的剩余费用。
        /// </summary>
        public decimal Remainder { get; set; }
    }


    /// <summary>
    /// 增加新结算单明细功能参数封装类。
    /// </summary>
    public class AddPlInvoicesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新结算单明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlInvoicesItem PlInvoicesItem { get; set; }
    }

    /// <summary>
    /// 增加新结算单明细功能返回值封装类。
    /// </summary>
    public class AddPlInvoicesItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新结算单明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改结算单明细信息功能参数封装类。
    /// </summary>
    public class ModifyPlInvoicesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 结算单明细数据。
        /// </summary>
        public PlInvoicesItem PlInvoicesItem { get; set; }
    }

    /// <summary>
    /// 修改结算单明细信息功能返回值封装类。
    /// </summary>
    public class ModifyPlInvoicesItemReturnDto : ReturnDtoBase
    {
    }
    #endregion 结算单明细

    #region 结算单

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<PlInvoices> Items { get; set; } = new List<PlInvoices>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetPlInvoicesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<PlInvoices> Result { get; set; } = new List<PlInvoices>();
    }

    /// <summary>
    /// 标记删除结算单功能的参数封装类。
    /// </summary>
    public class RemovePlInvoicesParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除结算单功能的返回值封装类。
    /// </summary>
    public class RemovePlInvoicesReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有结算单功能的返回值封装类。
    /// </summary>
    public class GetAllPlInvoicesReturnDto : PagingReturnDtoBase<PlInvoices>
    {
    }

    /// <summary>
    /// 增加新结算单功能参数封装类。
    /// </summary>
    public class AddPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新结算单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlInvoices PlInvoices { get; set; }
    }

    /// <summary>
    /// 增加新结算单功能返回值封装类。
    /// </summary>
    public class AddPlInvoicesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新结算单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改结算单信息功能参数封装类。
    /// </summary>
    public class ModifyPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 结算单数据。
        /// </summary>
        public PlInvoices PlInvoices { get; set; }
    }

    /// <summary>
    /// 修改结算单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlInvoicesReturnDto : ReturnDtoBase
    {
    }
    #endregion 结算单

    #region 业务费用申请单明细

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetDocFeeRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<DocFeeRequisitionItem> Items { get; set; } = new List<DocFeeRequisitionItem>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetDocFeeRequisitionItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<DocFeeRequisitionItem> Result { get; set; } = new List<DocFeeRequisitionItem>();
    }

    /// <summary>
    /// 标记删除业务费用申请单明细功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionItemParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务费用申请单明细功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionItemReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有业务费用申请单明细功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionItemReturnDto : PagingReturnDtoBase<DocFeeRequisitionItem>
    {
    }

    /// <summary>
    /// 增加新业务费用申请单明细功能参数封装类。
    /// </summary>
    public class AddDocFeeRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务费用申请单明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }
    }

    /// <summary>
    /// 增加新业务费用申请单明细功能返回值封装类。
    /// </summary>
    public class AddDocFeeRequisitionItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务费用申请单明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单明细信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务费用申请单明细数据。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单明细信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionItemReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务费用申请单明细

    #region 业务费用申请单

    /// <summary>
    /// 获取指定费用的剩余未申请金额参数封装类。
    /// </summary>
    public class GetFeeRemainingParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 费用的Id集合。
        /// </summary>
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 获取指定费用的剩余未申请金额功能返回值封装类。
    /// </summary>
    public class GetFeeRemainingItemReturnDto
    {
        /// <summary>
        /// 关联的费用的对象。
        /// </summary>
        public DocFee Fee { get; set; }

        /// <summary>
        /// 剩余未申请的费用。
        /// </summary>
        public decimal Remaining { get; set; }

        /// <summary>
        /// 费用关联的任务对象。
        /// </summary>
        public PlJob Job { get; set; }

    }

    /// <summary>
    /// 获取指定费用的剩余未申请金额功能返回值封装类。
    /// </summary>
    public class GetFeeRemainingReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 费用单据的额外信息。
        /// </summary>
        public List<GetFeeRemainingItemReturnDto> Result { get; set; } = new List<GetFeeRemainingItemReturnDto>();
    }

    /// <summary>
    /// 标记删除业务费用申请单功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务费用申请单功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取当前用户相关的业务费用申请单和审批流状态功能的参数封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionWithWfParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 限定流程状态。省略或为null则不限定。若限定流程状态，则操作人默认当前登录用户。
        /// 1=正等待指定操作者审批，2=指定操作者已审批但仍在流转中，4=指定操作者参与的且已成功结束的流程,8=指定操作者参与的且已失败结束的流程。
        /// 12=指定操作者参与的且已结束的流程（包括成功/失败）
        /// </summary>
        public byte? WfState { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetAllDocFeeRequisitionWithWfItemDto
    {
        /// <summary>
        /// 申请单对象。
        /// </summary>
        public DocFeeRequisition Requisition { get; set; }

        /// <summary>
        /// 相关流程对象。
        /// </summary>
        public OwWfDto Wf { get; set; }
    }

    /// <summary>
    /// 获取当前用户相关的业务费用申请单和审批流状态的返回值封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionWithWfReturnDto
    {
        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<GetAllDocFeeRequisitionWithWfItemDto> Result { get; set; } = new List<GetAllDocFeeRequisitionWithWfItemDto>();
    }

    /// <summary>
    /// 获取全部业务费用申请单功能的参数封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetAllDocFeeRequisitionParamsDto()
        {

        }

        /// <summary>
        /// 限定流程状态。省略或为null则不限定。若限定流程状态，则操作人默认去当前登录用户。
        /// 1=正等待指定操作者审批，2=指定操作者已审批但仍在流转中，4=指定操作者参与的且已成功结束的流程,8=指定操作者参与的且已失败结束的流程。
        /// 12=指定操作者参与的且已结束的流程（包括成功/失败）
        /// </summary>
        public byte? WfState { get; set; }
    }


    /// <summary>
    /// 获取所有业务费用申请单功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionReturnDto : PagingReturnDtoBase<DocFeeRequisition>
    {
    }

    /// <summary>
    /// 增加新业务费用申请单功能参数封装类。
    /// </summary>
    public class AddDocFeeRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务费用申请单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }
    }

    /// <summary>
    /// 增加新业务费用申请单功能返回值封装类。
    /// </summary>
    public class AddDocFeeRequisitionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务费用申请单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务费用申请单数据。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务费用申请单

    #region 费用方案明细

    /// <summary>
    /// 获取费用方案明细增强接口功能参数封装类。
    /// </summary>
    public class GetDocFeeTemplateItemParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取费用方案明细增强接口功能返回值封装类。
    /// </summary>
    public class GetDocFeeTemplateItemReturnDto : PagingReturnDtoBase<GetDocFeeTemplateItemItem>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetDocFeeTemplateItemItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetDocFeeTemplateItemItem()
        {
            //申请单 job 费用实体 申请明细的余额（未结算）
        }

        /// <summary>
        /// 费用方案详细项
        /// </summary>
        public DocFeeTemplateItem InvoicesItem { get; set; }

        /// <summary>
        /// 费用方案。
        /// </summary>
        public DocFeeTemplate Invoices { get; set; }

        /// <summary>
        /// 相关的任务对象。
        /// </summary>
        public PlJob PlJob { get; set; }

        /// <summary>
        /// 相关的申请单对象。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }

        /// <summary>
        /// 申请单明细对象。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }

        /// <summary>
        /// 相关的费用方案对象。
        /// </summary>
        public DocFeeTemplate Parent { get; set; }
    }

    /// <summary>
    /// 费用方案确认功能参数封装类。
    /// </summary>
    public class ConfirmDocFeeTemplateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 费用方案的Id集合。
        /// </summary>
        public List<Guid> Ids { get; set; }
    }

    /// <summary>
    /// 费用方案确认功能返回值封装类。
    /// </summary>
    public class ConfirmDocFeeTemplateReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetDocFeeTemplateItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 费用方案的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<DocFeeTemplateItem> Items { get; set; } = new List<DocFeeTemplateItem>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetDocFeeTemplateItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<DocFeeTemplateItem> Result { get; set; } = new List<DocFeeTemplateItem>();
    }

    /// <summary>
    /// 标记删除费用方案明细功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeTemplateItemParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除费用方案明细功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeTemplateItemReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有费用方案明细功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeTemplateItemReturnDto : PagingReturnDtoBase<DocFeeTemplateItem>
    {
    }

    ///// <summary>
    ///// 获取申请单明细增强接口功能参数封装类。
    ///// </summary>
    //public class GetDocFeeRequisitionItemParamsDto : PagingParamsDtoBase
    //{
    //}

    ///// <summary>
    ///// 获取申请单明细增强接口功能返回值封装类。
    ///// </summary>
    //public class GetDocFeeRequisitionItemReturnDto : PagingReturnDtoBase<GetDocFeeRequisitionItemItem>
    //{

    //}

    ///// <summary>
    ///// 获取申请单明细增强接口功能的返回值中的元素类型。
    ///// </summary>
    //public class GetDocFeeRequisitionItemItem
    //{
    //    /// <summary>
    //    /// 申请单明细对象。
    //    /// </summary>
    //    public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }

    //    /// <summary>
    //    /// 相关的任务对象。
    //    /// </summary>
    //    public PlJob PlJob { get; set; }

    //    /// <summary>
    //    /// 相关的申请单对象。
    //    /// </summary>
    //    public DocFeeRequisition DocFeeRequisition { get; set; }

    //    /// <summary>
    //    /// 相关的费用对象。
    //    /// </summary>
    //    public DocFee DocFee { get; set; }

    //    /// <summary>
    //    /// 申请单明细对象未结算的剩余费用。
    //    /// </summary>
    //    public decimal Remainder { get; set; }
    //}


    /// <summary>
    /// 增加新费用方案明细功能参数封装类。
    /// </summary>
    public class AddDocFeeTemplateItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新费用方案明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFeeTemplateItem DocFeeTemplateItem { get; set; }
    }

    /// <summary>
    /// 增加新费用方案明细功能返回值封装类。
    /// </summary>
    public class AddDocFeeTemplateItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新费用方案明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改费用方案明细信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeTemplateItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 费用方案明细数据。
        /// </summary>
        public DocFeeTemplateItem DocFeeTemplateItem { get; set; }
    }

    /// <summary>
    /// 修改费用方案明细信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeTemplateItemReturnDto : ReturnDtoBase
    {
    }
    #endregion 费用方案明细

    #region 费用方案

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetDocFeeTemplateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<DocFeeTemplate> Items { get; set; } = new List<DocFeeTemplate>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetDocFeeTemplateReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<DocFeeTemplate> Result { get; set; } = new List<DocFeeTemplate>();
    }

    /// <summary>
    /// 标记删除费用方案功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeTemplateParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除费用方案功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeTemplateReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有费用方案功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeTemplateReturnDto : PagingReturnDtoBase<DocFeeTemplate>
    {
    }

    /// <summary>
    /// 增加新费用方案功能参数封装类。
    /// </summary>
    public class AddDocFeeTemplateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新费用方案信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFeeTemplate DocFeeTemplate { get; set; }
    }

    /// <summary>
    /// 增加新费用方案功能返回值封装类。
    /// </summary>
    public class AddDocFeeTemplateReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新费用方案的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改费用方案信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeTemplateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 费用方案数据。
        /// </summary>
        public DocFeeTemplate DocFeeTemplate { get; set; }
    }

    /// <summary>
    /// 修改费用方案信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeTemplateReturnDto : ReturnDtoBase
    {
    }
    #endregion 费用方案

    #region 发票相关
    /// <summary>
    /// 获取全部税务发票信息的返回值封装类。
    /// </summary>
    public class GetAllTaxInvoiceInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<TaxInvoiceInfo> Result { get; set; } = new();
    }

    /// <summary>
    /// 增加新税务发票信息功能参数封装类。
    /// </summary>
    public class AddTaxInvoiceInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新税务发票信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public TaxInvoiceInfo TaxInvoiceInfo { get; set; }
    }

    /// <summary>
    /// 增加新税务发票信息功能返回值封装类。
    /// </summary>
    public class AddTaxInvoiceInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新税务发票信息的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改税务发票信息功能参数封装类。
    /// </summary>
    public class ModifyTaxInvoiceInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 税务发票信息数据。
        /// </summary>
        public TaxInvoiceInfo TaxInvoiceInfo { get; set; }
    }

    /// <summary>
    /// 修改税务发票信息功能返回值封装类。
    /// </summary>
    public class ModifyTaxInvoiceInfoReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除税务发票信息功能参数封装类。
    /// </summary>
    public class RemoveTaxInvoiceInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要删除的税务发票信息的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 删除税务发票信息功能返回值封装类。
    /// </summary>
    public class RemoveTaxInvoiceInfoReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取全部税务发票信息明细的返回值封装类。
    /// </summary>
    public class GetAllTaxInvoiceInfoItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<TaxInvoiceInfoItem> Result { get; set; } = new();
    }

    /// <summary>
    /// 增加新税务发票信息明细功能参数封装类。
    /// </summary>
    public class AddTaxInvoiceInfoItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新税务发票信息明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public TaxInvoiceInfoItem TaxInvoiceInfoItem { get; set; }
    }

    /// <summary>
    /// 增加新税务发票信息明细功能返回值封装类。
    /// </summary>
    public class AddTaxInvoiceInfoItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新税务发票信息明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改税务发票信息明细功能参数封装类。
    /// </summary>
    public class ModifyTaxInvoiceInfoItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 税务发票信息明细数据。
        /// </summary>
        public TaxInvoiceInfoItem TaxInvoiceInfoItem { get; set; }
    }

    /// <summary>
    /// 修改税务发票信息明细功能返回值封装类。
    /// </summary>
    public class ModifyTaxInvoiceInfoItemReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除税务发票信息明细功能参数封装类。
    /// </summary>
    public class RemoveTaxInvoiceInfoItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要删除的税务发票信息明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 删除税务发票信息明细功能返回值封装类。
    /// </summary>
    public class RemoveTaxInvoiceInfoItemReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 设置指定的税务发票信息下所有明细功能的参数封装类。
    /// </summary>
    public class SetTaxInvoiceInfoItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 税务发票信息的Id。
        /// </summary>
        public Guid TaxInvoiceInfoId { get; set; }
        /// <summary>
        /// 税务发票信息明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<TaxInvoiceInfoItem> Items { get; set; }
    }

    /// <summary>
    /// 设置指定的税务发票信息下所有明细功能的返回值封装类。
    /// </summary>
    public class SetTaxInvoiceInfoItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定税务发票信息下，所有明细的对象。
        /// </summary>
        public List<TaxInvoiceInfoItem> Result { get; set; }
    }

    #endregion 发票相关
}
