/*
 * 项目：PowerLms货运物流管理系统 | 模块：主单领用登记DTO
 * 功能：主单（MAWB）领用登记相关的数据传输对象
 * 技术要点：
 *   - 主单号工具方法DTO（校验、生成）
 *   - 主单领入/领出CRUD操作DTO
 *   - 台账查询与管理DTO
 *   - 业务关联查询DTO
 * 作者：zc | 创建：2025-01 | 修改：2025-01-17 初始创建
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers.Business
{
    #region 主单号工具方法DTO

    /// <summary>
    /// 校验主单号的参数封装类。
    /// </summary>
    public class ValidateMawbNoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要校验的主单号（支持"999-12345678"或"999-1234 5678"两种格式）。
        /// </summary>
        [Required]
        public string MawbNo { get; set; }
    }

    /// <summary>
    /// 校验主单号的返回值封装类。
    /// </summary>
    public class ValidateMawbNoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 主单号是否有效。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息（有效时为空）。
        /// </summary>
        public string ErrorMsg { get; set; }
    }

    /// <summary>
    /// 生成下一个主单号的参数封装类。
    /// </summary>
    public class GenerateNextMawbNoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 3位前缀（如"999"）。
        /// </summary>
        [Required, StringLength(3, MinimumLength = 3)]
        public string Prefix { get; set; }

        /// <summary>
        /// 当前8位数字部分（如"12345678"）。
        /// </summary>
        [Required, StringLength(8, MinimumLength = 8)]
        public string CurrentNo { get; set; }
    }

    /// <summary>
    /// 生成下一个主单号的返回值封装类。
    /// </summary>
    public class GenerateNextMawbNoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 下一个完整主单号（如"999-12345685"）。
        /// </summary>
        public string NextMawbNo { get; set; }
    }

    /// <summary>
    /// 批量生成主单号的参数封装类。
    /// </summary>
    public class BatchGenerateMawbNosParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 3位航司代码（如"999"）。
        /// </summary>
        [Required, StringLength(3, MinimumLength = 3)]
        public string Prefix { get; set; }

        /// <summary>
        /// 起始主单号（8位数字部分）。
        /// <strong>重要说明：</strong>
        /// - 前端传入的是<strong>本次批量生成的第一个号</strong>（不是已存在的号）
        /// - 返回的主单号序列<strong>从该号开始</strong>，包含该号本身
        /// - 例如：传入"12345670"，返回的第一个号就是"999-12345670"
        /// - <strong>不需要</strong>查询数据库确认该号是否存在
        /// </summary>
        [Required, StringLength(8, MinimumLength = 8)]
        public string StartNo { get; set; }

        /// <summary>
        /// 生成数量（返回从StartNo开始的Count个主单号，包含StartNo本身）。
        /// </summary>
        [Required, Range(1, 1000)]
        public int Count { get; set; }
    }

    /// <summary>
    /// 批量生成主单号的返回值封装类。
    /// </summary>
    public class BatchGenerateMawbNosReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 主单号列表（从StartNo开始，共Count个，<strong>包含StartNo本身</strong>）。
        /// </summary>
        public List<string> MawbNos { get; set; } = new List<string>();
    }

    #endregion 主单号工具方法DTO

    #region 主单领入相关DTO

    /// <summary>
    /// 获取全部主单领入记录的返回值封装类。
    /// </summary>
    public class GetAllMawbInboundReturnDto : PagingReturnDtoBase<PlEaMawbInbound>
    {
    }

    /// <summary>
    /// 批量新增主单领入记录的参数封装类。
    /// </summary>
    public class AddMawbInboundParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 来源类型（0=航司登记，1=过单代理）。
        /// </summary>
        [Required, Range(0, 1)]
        public byte SourceType { get; set; }

        /// <summary>
        /// 航空公司Id（不建FK约束）。
        /// </summary>
        public Guid? AirlineId { get; set; }

        /// <summary>
        /// 过单代理Id（不建FK约束）。
        /// </summary>
        public Guid? TransferAgentId { get; set; }

        /// <summary>
        /// 登记日期。
        /// </summary>
        [Required]
        public DateTime RegisterDate { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 批量主单号列表（支持"999-12345678"或"999-1234 5678"格式）。
        /// </summary>
        [Required]
        public List<string> MawbNos { get; set; } = new List<string>();
    }

    /// <summary>
    /// 批量新增主单领入记录的返回值封装类。
    /// </summary>
    public class AddMawbInboundReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 成功创建的记录数。
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败的记录数。
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 失败详情列表。
        /// </summary>
        public List<string> FailureDetails { get; set; } = new List<string>();
    }

    /// <summary>
    /// 修改主单领入记录的参数封装类。
    /// </summary>
    public class ModifyMawbInboundParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改的记录Id。
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 航空公司Id。
        /// </summary>
        public Guid? AirlineId { get; set; }

        /// <summary>
        /// 过单代理Id。
        /// </summary>
        public Guid? TransferAgentId { get; set; }

        /// <summary>
        /// 登记日期。
        /// </summary>
        public DateTime? RegisterDate { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 修改主单领入记录的返回值封装类。
    /// </summary>
    public class ModifyMawbInboundReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除主单领入记录的参数封装类。
    /// </summary>
    public class RemoveMawbInboundParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除主单领入记录的返回值封装类。
    /// </summary>
    public class RemoveMawbInboundReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion 主单领入相关DTO

    #region 主单领出相关DTO

    /// <summary>
    /// 获取全部主单领出记录的返回值封装类。
    /// </summary>
    public class GetAllMawbOutboundReturnDto : PagingReturnDtoBase<PlEaMawbOutbound>
    {
    }

    /// <summary>
    /// 新增主单领出记录的参数封装类。
    /// </summary>
    public class AddMawbOutboundParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 主单号（支持"999-12345678"或"999-1234 5678"格式）。
        /// </summary>
        [Required]
        public string MawbNo { get; set; }

        /// <summary>
        /// 领单代理Id（不建FK约束）。
        /// </summary>
        [Required]
        public Guid? AgentId { get; set; }

        /// <summary>
        /// 领用人。
        /// </summary>
        [Required, StringLength(50)]
        public string RecipientName { get; set; }

        /// <summary>
        /// 领用日期。
        /// </summary>
        [Required]
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// 预计返回日期。
        /// </summary>
        public DateTime? PlannedReturnDate { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 新增主单领出记录的返回值封装类。
    /// </summary>
    public class AddMawbOutboundReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改主单领出记录的参数封装类。
    /// </summary>
    public class ModifyMawbOutboundParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改的记录Id。
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 领单代理Id。
        /// </summary>
        public Guid? AgentId { get; set; }

        /// <summary>
        /// 领用人。
        /// </summary>
        [StringLength(50)]
        public string RecipientName { get; set; }

        /// <summary>
        /// 领用日期。
        /// </summary>
        public DateTime? IssueDate { get; set; }

        /// <summary>
        /// 预计返回日期。
        /// </summary>
        public DateTime? PlannedReturnDate { get; set; }

        /// <summary>
        /// 实际返回日期。
        /// </summary>
        public DateTime? ActualReturnDate { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 修改主单领出记录的返回值封装类。
    /// </summary>
    public class ModifyMawbOutboundReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除主单领出记录的参数封装类。
    /// </summary>
    public class RemoveMawbOutboundParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除主单领出记录的返回值封装类。
    /// </summary>
    public class RemoveMawbOutboundReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion 主单领出相关DTO

    #region 台账查询相关DTO

    /// <summary>
    /// 主单台账数据传输对象（含领入/领出/业务信息）。
    /// </summary>
    public class MawbLedgerDto
    {
        /// <summary>
        /// 台账Id。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 组织机构Id。
        /// </summary>
        public Guid OrgId { get; set; }

        /// <summary>
        /// 标准主单号。
        /// </summary>
        public string MawbNo { get; set; }

        /// <summary>
        /// 显示主单号。
        /// </summary>
        public string MawbNoDisplay { get; set; }

        /// <summary>
        /// 使用状态（0=未使用，1=已使用，2=已作废）。
        /// </summary>
        public byte UseStatus { get; set; }

        /// <summary>
        /// 领入来源类型（0=航司登记，1=过单代理）。
        /// </summary>
        public byte? SourceType { get; set; }

        /// <summary>
        /// 航空公司Id。
        /// </summary>
        public Guid? AirlineId { get; set; }

        /// <summary>
        /// 过单代理Id。
        /// </summary>
        public Guid? TransferAgentId { get; set; }

        /// <summary>
        /// 登记日期。
        /// </summary>
        public DateTime? RegisterDate { get; set; }

        /// <summary>
        /// 领单代理Id。
        /// </summary>
        public Guid? AgentId { get; set; }

        /// <summary>
        /// 领用人。
        /// </summary>
        public string RecipientName { get; set; }

        /// <summary>
        /// 领用日期。
        /// </summary>
        public DateTime? IssueDate { get; set; }

        /// <summary>
        /// 业务委托号。
        /// </summary>
        public string JobNo { get; set; }

        /// <summary>
        /// 件数。
        /// </summary>
        public int? Pieces { get; set; }

        /// <summary>
        /// 重量（KG）。
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// 体积（CBM）。
        /// </summary>
        public decimal? Volume { get; set; }

        /// <summary>
        /// 计费重量（KG）。
        /// </summary>
        public decimal? ChargeableWeight { get; set; }

        /// <summary>
        /// 创建人Id。
        /// </summary>
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime? CreateDateTime { get; set; }
    }

    /// <summary>
    /// 获取主单台账列表的返回值封装类。
    /// </summary>
    public class GetMawbLedgerListReturnDto : PagingReturnDtoBase<MawbLedgerDto>
    {
    }

    /// <summary>
    /// 获取未使用主单列表的返回值封装类。
    /// </summary>
    public class GetUnusedMawbListReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 未使用主单列表。
        /// </summary>
        public List<MawbLedgerDto> Result { get; set; } = new List<MawbLedgerDto>();
    }

    /// <summary>
    /// 作废主单的参数封装类。
    /// </summary>
    public class MarkMawbAsVoidParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 主单号。
        /// </summary>
        [Required]
        public string MawbNo { get; set; }

        /// <summary>
        /// 作废原因。
        /// </summary>
        [Required, StringLength(500)]
        public string Reason { get; set; }
    }

    /// <summary>
    /// 作废主单的返回值封装类。
    /// </summary>
    public class MarkMawbAsVoidReturnDto : ReturnDtoBase
    {
    }

    #endregion 台账查询相关DTO

    #region 业务关联相关DTO

    /// <summary>
    /// 业务委托信息数据传输对象。
    /// </summary>
    public class JobInfoDto
    {
        /// <summary>
        /// 业务委托号。
        /// </summary>
        public string JobNo { get; set; }

        /// <summary>
        /// 件数。
        /// </summary>
        public int? Pieces { get; set; }

        /// <summary>
        /// 重量（KG）。
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// 体积（CBM）。
        /// </summary>
        public decimal? Volume { get; set; }

        /// <summary>
        /// 计费重量（KG）。
        /// </summary>
        public decimal? ChargeableWeight { get; set; }
    }

    /// <summary>
    /// 根据主单号查询委托信息的返回值封装类。
    /// </summary>
    public class GetJobInfoByMawbNoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 业务委托信息。
        /// </summary>
        public JobInfoDto Result { get; set; }
    }

    #endregion 业务关联相关DTO
}
