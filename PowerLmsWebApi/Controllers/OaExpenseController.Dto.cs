using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Dto
{
    #region OA费用申请单主表

    /// <summary>
    /// 获取所有OA费用申请单功能的参数封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 搜索文本。可搜索相关客户、备注等。
        /// </summary>
        public string SearchText { get; set; }

        /// <summary>
        /// 开始日期过滤。
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期过滤。
        /// </summary>
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// 获取所有OA费用申请单功能的返回值封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionReturnDto : PagingReturnDtoBase<OaExpenseRequisition>
    {
    }

    /// <summary>
    /// 增加新OA费用申请单功能参数封装类。
    /// </summary>
    public class AddOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新OA费用申请单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        [Required]
        public OaExpenseRequisition OaExpenseRequisition { get; set; }

        /// <summary>
        /// 是否代为登记。true表示财务帮其他人登记，false表示用户主动申请。
        /// </summary>
        public bool IsRegisterForOthers { get; set; }
    }

    /// <summary>
    /// 增加新OA费用申请单功能返回值封装类。
    /// </summary>
    public class AddOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新OA费用申请单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改OA费用申请单信息功能参数封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// OA费用申请单数据。
        /// </summary>
        [Required]
        public OaExpenseRequisition OaExpenseRequisition { get; set; }
    }

    /// <summary>
    /// 修改OA费用申请单信息功能返回值封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除OA费用申请单功能的参数封装类。
    /// </summary>
    public class RemoveOaExpenseRequisitionParamsDto : RemoveItemsParamsDtoBase
    {
    }

    /// <summary>
    /// 删除OA费用申请单功能的返回值封装类。
    /// </summary>
    public class RemoveOaExpenseRequisitionReturnDto : RemoveItemsReturnDtoBase
    {
    }

    #endregion

    #region OA费用申请单明细

    /// <summary>
    /// 获取所有OA费用申请单明细功能的参数封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionItemParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 申请单Id。用于过滤指定申请单的明细。
        /// </summary>
        public Guid? RequisitionId { get; set; }
    }

    /// <summary>
    /// 获取所有OA费用申请单明细功能的返回值封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionItemReturnDto : PagingReturnDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// 增加新OA费用申请单明细功能参数封装类。
    /// </summary>
    public class AddOaExpenseRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新OA费用申请单明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        [Required]
        public OaExpenseRequisitionItem OaExpenseRequisitionItem { get; set; }
    }

    /// <summary>
    /// 增加新OA费用申请单明细功能返回值封装类。
    /// </summary>
    public class AddOaExpenseRequisitionItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新OA费用申请单明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改OA费用申请单明细信息功能参数封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// OA费用申请单明细数据。
        /// </summary>
        [Required]
        public OaExpenseRequisitionItem OaExpenseRequisitionItem { get; set; }
    }

    /// <summary>
    /// 修改OA费用申请单明细信息功能返回值封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionItemReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除OA费用申请单明细功能的参数封装类。
    /// </summary>
    public class RemoveOaExpenseRequisitionItemParamsDto : RemoveItemsParamsDtoBase
    {
    }

    /// <summary>
    /// 删除OA费用申请单明细功能的返回值封装类。
    /// </summary>
    public class RemoveOaExpenseRequisitionItemReturnDto : RemoveItemsReturnDtoBase
    {
    }

    #endregion

    #region 审核相关DTO

    /// <summary>
    /// 审核OA费用申请单参数DTO。
    /// </summary>
    public class AuditOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单Id。
        /// </summary>
        [Required]
        public Guid RequisitionId { get; set; }

        /// <summary>
        /// 审核标志，true审核通过，false取消审核。
        /// </summary>
        public bool IsAudit { get; set; }

        /// <summary>
        /// 结算方式。现金或银行转账，只能在审核时指定。
        /// </summary>
        public SettlementMethodType? SettlementMethod { get; set; }

        /// <summary>
        /// 银行账户Id。当结算方式是银行时，选择本公司信息中的银行账户id，只能在审核时指定。
        /// </summary>
        public Guid? BankAccountId { get; set; }
    }

    /// <summary>
    /// 审核OA费用申请单返回DTO。
    /// </summary>
    public class AuditOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
    }

    #endregion

    #region 扩展DTO

    /// <summary>
    /// OA费用申请单详细信息DTO。
    /// 包含申请单、明细项等完整信息。
    /// </summary>
    public class OaExpenseRequisitionDetailDto
    {
        /// <summary>
        /// 申请单主信息。
        /// </summary>
        public OaExpenseRequisition Requisition { get; set; }

        /// <summary>
        /// 费用明细项列表。
        /// </summary>
        public List<OaExpenseRequisitionItem> Items { get; set; } = new List<OaExpenseRequisitionItem>();

        /// <summary>
        /// 申请人信息。
        /// </summary>
        public Account Applicant { get; set; }

        /// <summary>
        /// 登记人信息。
        /// </summary>
        public Account Registrar { get; set; }
    }

    #endregion
}