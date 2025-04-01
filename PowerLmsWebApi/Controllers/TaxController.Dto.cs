using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    #region 税务发票渠道相关

    /// <summary>
    /// 获取指定ID的税务发票渠道的返回值封装类。
    /// </summary>
    public class GetAllTaxInvoiceChannelReturnDto : PagingReturnDtoBase<TaxInvoiceChannel>
    {
    }


    /// <summary>
    /// 修改税务发票渠道记录的功能参数封装类。
    /// </summary>
    public class ModifyTaxInvoiceChannelParamsDto : ModifyParamsDtoBase<TaxInvoiceChannel>
    {
    }

    /// <summary>
    /// 修改税务发票渠道记录的功能返回值封装类。
    /// </summary>
    public class ModifyTaxInvoiceChannelReturnDto : ModifyReturnDtoBase
    {
    }

    #endregion 税务发票渠道相关

    #region 税务发票渠道账号相关

    /// <summary>
    /// 获取税务发票渠道账号的功能返回值封装类。
    /// </summary>
    public class GetAllTaxInvoiceChannelAccountReturnDto : PagingReturnDtoBase<TaxInvoiceChannelAccount>
    {
    }

    /// <summary>
    /// 增加税务发票渠道账号记录的功能参数封装类。
    /// </summary>
    public class AddTaxInvoiceChannelAccountParamsDto : AddParamsDtoBase<TaxInvoiceChannelAccount>
    {
    }

    /// <summary>
    /// 增加税务发票渠道账号记录的功能返回值封装类。
    /// </summary>
    public class AddTaxInvoiceChannelAccountReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改税务发票渠道账号记录的功能参数封装类。
    /// </summary>
    public class ModifyTaxInvoiceChannelAccountParamsDto : ModifyParamsDtoBase<TaxInvoiceChannelAccount>
    {
    }

    /// <summary>
    /// 修改税务发票渠道账号记录的功能返回值封装类。
    /// </summary>
    public class ModifyTaxInvoiceChannelAccountReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 删除税务发票渠道账号记录的功能参数封装类。
    /// </summary>
    public class RemoveTaxInvoiceChannelAccountParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除税务发票渠道账号记录的功能返回值封装类。
    /// </summary>
    public class RemoveTaxInvoiceChannelAccountReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 恢复指定被删除税务发票渠道账号记录的功能参数封装类。
    /// </summary>
    public class RestoreTaxInvoiceChannelAccountParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复指定被删除税务发票渠道账号记录的功能返回值封装类。
    /// </summary>
    public class RestoreTaxInvoiceChannelAccountReturnDto : RestoreReturnDtoBase
    {
    }
    #endregion 税务发票渠道相关

    #region 机构渠道账号相关

    /// <summary>
    /// 获取全部机构渠道账号的返回值封装类。
    /// </summary>
    public class GetAllOrgTaxChannelAccountReturnDto : PagingReturnDtoBase<OrgTaxChannelAccount>
    {
    }

    /// <summary>
    /// 增加新机构渠道账号功能参数封装类。
    /// </summary>
    public class AddOrgTaxChannelAccountParamsDto : AddParamsDtoBase<OrgTaxChannelAccount>
    {
    }

    /// <summary>
    /// 增加新机构渠道账号功能返回值封装类。
    /// </summary>
    public class AddOrgTaxChannelAccountReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取指定机构渠道账号功能参数封装类。
    /// </summary>
    public class GetOrgTaxChannelAccountParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 渠道账号Id。
        /// </summary>
        [Required]
        public Guid ChannelAccountId { get; set; }
    }

    /// <summary>
    /// 获取指定机构渠道账号功能返回值封装类。
    /// </summary>
    public class GetOrgTaxChannelAccountReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 机构渠道账号信息。
        /// </summary>
        public OrgTaxChannelAccount OrgTaxChannelAccount { get; set; }
    }

    /// <summary>
    /// 修改机构渠道账号信息功能参数封装类。
    /// </summary>
    public class ModifyOrgTaxChannelAccountParamsDto : ModifyParamsDtoBase<OrgTaxChannelAccount>
    {
    }

    /// <summary>
    /// 修改机构渠道账号信息功能返回值封装类。
    /// </summary>
    public class ModifyOrgTaxChannelAccountReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 删除指定的机构渠道账号功能参数封装类。注意：机构渠道账号是两个字段的联合主键。
    /// </summary>
    public class RemoveOrgTaxChannelAccountParamsDto : RemoveAccountParamsDto
    {

    }

    /// <summary>
    /// 删除指定Id的机构渠道账号功能返回值封装类。
    /// </summary>
    public class RemoveOrgTaxChannelAccountReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 设置默认机构渠道账号功能参数封装类。
    /// </summary>
    public class SetDefaultOrgTaxChannelAccountParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要设为默认的渠道账号Id。
        /// </summary>
        [Required]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 设置默认机构渠道账号功能返回值封装类。
    /// </summary>
    public class SetDefaultOrgTaxChannelAccountReturnDto : ReturnDtoBase
    {
    }
    #endregion 机构渠道账号相关

    #region 发票相关
    /// <summary>
    /// 获取全部税务发票信息的返回值封装类。
    /// </summary>
    public class GetAllTaxInvoiceInfoReturnDto : PagingReturnDtoBase<TaxInvoiceInfo>
    {

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
    /// 更改税务发票信息状态功能参数封装类。
    /// </summary>
    public class ChangeStateOfTaxInvoiceInfoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要更改状态的税务发票信息Id。
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 新状态。
        /// 0=初始状态，1=待审核，2=已审核
        /// </summary>
        [Range(0, 2)]
        public byte NewState { get; set; }

        /// <summary>
        /// 原因说明。在变更为已驳回(3)或已作废(4)状态时必须提供。
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 是否使用沙箱模式。
        /// 当设置为true时，将在沙箱环境中测试开票流程，不会产生真实发票。
        /// 默认为false，表示使用正式环境。
        /// </summary>
        public bool UseSandbox { get; set; } = false;
    }

    /// <summary>
    /// 更改税务发票信息状态功能返回值封装类。
    /// </summary>
    public class ChangeStateOfTaxInvoiceInfoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 税务发票信息的Id。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 更改后的状态值。
        /// </summary>
        public byte NewState { get; set; }
    }

    /// <summary>
    /// 获取全部税务发票信息明细的返回值封装类。
    /// </summary>
    public class GetAllTaxInvoiceInfoItemReturnDto : PagingReturnDtoBase<TaxInvoiceInfoItem>
    {
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
