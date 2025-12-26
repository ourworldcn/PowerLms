using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Dto
{
    #region 结算单

    /// <summary>
    /// 获取所有结算单功能的返回值封装类。
    /// </summary>
    public class GetAllPlInvoicesReturnDto : PagingReturnDtoBase<PlInvoices>
    {
    }

    /// <summary>
    /// 增加新结算单功能参数封装类。
    /// </summary>
    public class AddPlInvoiceParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新结算单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlInvoices PlInvoices { get; set; }
    }

    /// <summary>
    /// 增加新结算单功能返回值封装类。
    /// </summary>
    public class AddPlInvoiceReturnDto : ReturnDtoBase
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

    #endregion 结算单

    #region 结算单明细

    /// <summary>
    /// 获取所有结算单明细功能的返回值封装类。
    /// </summary>
    public class GetAllPlInvoicesItemReturnDto : PagingReturnDtoBase<PlInvoicesItem>
    {
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
    /// 设置指定的结算单下所有明细功能的参数封装类。
    /// </summary>
    public class SetPlInvoicesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 结算单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 结算单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<PlInvoicesItem> Items { get; set; } = new List<PlInvoicesItem>();
    }

    /// <summary>
    /// 设置指定的结算单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetPlInvoicesItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定结算单下，所有明细的对象。
        /// </summary>
        public List<PlInvoicesItem> Result { get; set; } = new List<PlInvoicesItem>();
    }

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
    /// 获取结算单明细增强接口功能的返回值中的元素类型。
    /// </summary>
    public class GetPlInvoicesItemItem
    {
        /// <summary>
        /// 结算单明细项。
        /// </summary>
        public PlInvoicesItem InvoicesItem { get; set; }

        /// <summary>
        /// 结算单。
        /// </summary>
        public PlInvoices Invoices { get; set; }

        /// <summary>
        /// 相关的工作号对象。
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
        /// 相关的结算单对象（父级）。
        /// </summary>
        public PlInvoices Parent { get; set; }
    }

    #endregion 结算单明细
}
