using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    #region 费用管理

    /// <summary>
    /// 按复杂的多表条件返回费用功能的返回值封装类。
    /// </summary>
    public class GetDocFeeReturnDto
    {
        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<DocFee> Result { get; set; } = new List<DocFee>();
    }

    /// <summary>
    /// 按复杂的多表条件返回费用功能的参数封装类。
    /// </summary>
    public class GetDocFeeParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 审核费用功能参数封装类（支持单笔和批量）。
    /// </summary>
    public class AuditDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要审核的费用Id列表。支持单个或多个费用Id。
        /// </summary>
        [Required]
        public List<Guid> FeeIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 审核标志，true审核完成，false取消审核完成。
        /// </summary>
        public bool IsAudit { get; set; }
    }

    /// <summary>
    /// 审核费用功能返回值封装类（支持单笔和批量）。
    /// </summary>
    public class AuditDocFeeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 成功审核的费用数量。
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败的费用数量。
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 操作结果详情列表。
        /// </summary>
        public List<AuditDocFeeResultItem> Results { get; set; } = new List<AuditDocFeeResultItem>();
    }

    /// <summary>
    /// 单个费用审核结果。
    /// </summary>
    public class AuditDocFeeResultItem
    {
        /// <summary>
        /// 费用Id。
        /// </summary>
        public Guid FeeId { get; set; }

        /// <summary>
        /// 是否成功。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（成功时为空）。
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 标记删除业务单的费用单功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务单的费用单功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 批量查询费用V2功能的参数封装类。
    /// </summary>
    public class GetAllDocFeeV2ParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 批量查询费用V2功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeV2ReturnDto : PagingReturnDtoBase<DocFee>
    {
    }

    /// <summary>
    /// 获取所有业务单的费用单功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeReturnDto : PagingReturnDtoBase<DocFee>
    {
    }

    /// <summary>
    /// 增加新业务单的费用单功能参数封装类。
    /// </summary>
    public class AddDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务单的费用单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFee DocFee { get; set; }
    }

    /// <summary>
    /// 增加新业务单的费用单功能返回值封装类。
    /// </summary>
    public class AddDocFeeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务单的费用单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务单的费用单信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务单的费用单数据。
        /// </summary>
        public DocFee DocFee { get; set; }
    }

    /// <summary>
    /// 修改业务单的费用单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeReturnDto : ReturnDtoBase
    {
    }

    #endregion 费用管理
}
