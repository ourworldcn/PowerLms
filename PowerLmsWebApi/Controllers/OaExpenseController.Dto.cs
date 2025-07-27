using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsWebApi.Dto;
using OW.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    #region OA费用申请单主表

    /// <summary>
    /// 获取所有OA费用申请单功能的参数封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionParamsDto : PagingParamsDtoBase
    {
        // 移除了所有专用过滤字段，改为完全使用 conditional 参数
        // 
        // 过滤示例：
        // - 文本搜索：使用 conditional 参数的通配符匹配功能
        //   conditional["RelatedCustomer"] = "*关键词*" 或 conditional["Remark"] = "*关键词*"
        // - 时间范围：conditional["CreateDateTime"] = "2024-1-1,2024-12-31"
        // - 审核状态：conditional["AuditDateTime"] = "null" (未审核) 或 conditional["AuditDateTime"] = "!null" (已审核)
        // - 申请人：conditional["ApplicantId"] = "guid值"
        // - 币种：conditional["CurrencyCode"] = "USD"
        // - 金额范围：conditional["Amount"] = "100,1000"
        //
        // 这种设计保持了系统的一致性，所有控制器都使用相同的查询模式
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
    public class AddOaExpenseRequisitionParamsDto : AddParamsDtoBase<OaExpenseRequisition>
    {
        /// <summary>
        /// 是否为代记。true表示代他人登记，false表示用户自己申请。
        /// </summary>
        public bool IsRegisterForOthers { get; set; }
    }

    /// <summary>
    /// 增加新OA费用申请单功能返回值封装类。
    /// </summary>
    public class AddOaExpenseRequisitionReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改OA费用申请单信息功能参数封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionParamsDto : ModifyParamsDtoBase<OaExpenseRequisition>
    {
    }

    /// <summary>
    /// 修改OA费用申请单信息功能返回值封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionReturnDto : ModifyReturnDtoBase
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
    public class RemoveOaExpenseRequisitionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 审核OA费用申请单功能的参数封装类。
    /// </summary>
    public class AuditOaExpenseRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单ID。
        /// </summary>
        [Required]
        public Guid RequisitionId { get; set; }

        /// <summary>
        /// 是否审核通过。true表示审核通过，false表示取消审核。
        /// </summary>
        public bool IsAudit { get; set; }
    }

    /// <summary>
    /// 审核OA费用申请单功能的返回值封装类。
    /// </summary>
    public class AuditOaExpenseRequisitionReturnDto : ReturnDtoBase
    {
    }

    #endregion

    #region OA费用申请单明细

    /// <summary>
    /// 获取所有OA费用申请单明细功能的返回值封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionItemReturnDto : PagingReturnDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// 增加新OA费用申请单明细功能参数封装类。
    /// </summary>
    public class AddOaExpenseRequisitionItemParamsDto : AddParamsDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// 增加新OA费用申请单明细功能返回值封装类。
    /// </summary>
    public class AddOaExpenseRequisitionItemReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改OA费用申请单明细信息功能参数封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionItemParamsDto : ModifyParamsDtoBase<OaExpenseRequisitionItem>
    {
    }

    /// <summary>
    /// 修改OA费用申请单明细信息功能返回值封装类。
    /// </summary>
    public class ModifyOaExpenseRequisitionItemReturnDto : ModifyReturnDtoBase
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
    public class RemoveOaExpenseRequisitionItemReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion

    #region 详细信息DTO

    /// <summary>
    /// OA费用申请单详细信息DTO类。
    /// 包含申请单基本和明细的聚合信息。
    /// </summary>
    public class OaExpenseRequisitionDetailDto
    {
        /// <summary>
        /// 申请单基本信息。
        /// </summary>
        public OaExpenseRequisition Requisition { get; set; }

        /// <summary>
        /// 申请明细项列表。
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

    #region 审批流程相关DTO

    /// <summary>
    /// 获取当前用户相关的OA费用申请单和审批流状态的参数封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionWithWfParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取当前用户相关的OA费用申请单和审批流状态的返回值封装类。
    /// </summary>
    public class GetAllOaExpenseRequisitionWithWfReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetAllOaExpenseRequisitionWithWfReturnDto()
        {
            Result = new List<GetAllOaExpenseRequisitionWithWfItemDto>();
        }

        /// <summary>
        /// 返回的申请单和工作流信息集合。
        /// </summary>
        public List<GetAllOaExpenseRequisitionWithWfItemDto> Result { get; set; }

        /// <summary>
        /// 总数量。
        /// </summary>
        public int Total { get; set; }
    }

    /// <summary>
    /// OA费用申请单和工作流信息的组合项。
    /// </summary>
    public class GetAllOaExpenseRequisitionWithWfItemDto
    {
        /// <summary>
        /// 申请单信息。
        /// </summary>
        public OaExpenseRequisition Requisition { get; set; }

        /// <summary>
        /// 关联的工作流信息。
        /// </summary>
        public OwWfDto Wf { get; set; }
    }

    #endregion

    #region 凭证号生成相关DTO

    /// <summary>
    /// 生成凭证号功能的参数封装类。
    /// </summary>
    public class GenerateVoucherNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单ID。
        /// </summary>
        [Required]
        public Guid RequisitionId { get; set; }

        /// <summary>
        /// 结算账号ID。
        /// </summary>
        [Required]
        public Guid SettlementAccountId { get; set; }

        /// <summary>
        /// 结算时间。
        /// </summary>
        [Required]
        public DateTime SettlementDateTime { get; set; }
    }

    /// <summary>
    /// 生成凭证号功能的返回值封装类。
    /// </summary>
    public class GenerateVoucherNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 生成的凭证号。
        /// </summary>
        public string VoucherNumber { get; set; }

        /// <summary>
        /// 凭证字。
        /// </summary>
        public string VoucherCharacter { get; set; }

        /// <summary>
        /// 期间（月份）。
        /// </summary>
        public int Period { get; set; }

        /// <summary>
        /// 序号。
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 是否存在重号警告。
        /// </summary>
        public bool HasDuplicateWarning { get; set; }

        /// <summary>
        /// 重号警告消息。
        /// </summary>
        public string DuplicateWarningMessage { get; set; }
    }

    #endregion
}