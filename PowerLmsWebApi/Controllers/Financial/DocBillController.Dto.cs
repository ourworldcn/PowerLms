/*
 * 项目：PowerLms WebApi | 模块：财务管理
 * 功能：账单管理DTO定义
 * 作者：zc | 创建：2026-02 | 修改：2026-02-06 从PlJobController.Dto重构独立
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    #region 账单CRUD相关DTO

    /// <summary>
    /// 获取全部账单的返回值封装类。
    /// </summary>
    public class GetAllDocBillReturnDto : PagingReturnDtoBase<DocBill>
    {
    }

    /// <summary>
    /// 增加账单的参数封装类。
    /// </summary>
    public class AddDocBillParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 账单对象。
        /// </summary>
        [Required]
        public DocBill DocBill { get; set; }

        /// <summary>
        /// 要关联到此账单的费用Id列表。
        /// </summary>
        [Required]
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 增加账单的返回值封装类。
    /// </summary>
    public class AddDocBillReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改账单的参数封装类。
    /// </summary>
    public class ModifyDocBillParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 账单对象。
        /// </summary>
        [Required]
        public DocBill DocBill { get; set; }

        /// <summary>
        /// 要关联到此账单的费用Id列表。
        /// </summary>
        [Required]
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 修改账单的返回值封装类。
    /// </summary>
    public class ModifyDocBillReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除账单的参数封装类。
    /// </summary>
    public class RemoveDocBillParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除账单的返回值封装类。
    /// </summary>
    public class RemoveDocBillReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 根据业务Id获取账单的参数封装类。
    /// </summary>
    public class GetDocBillsByJobIdParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务Id集合。
        /// </summary>
        [Required]
        public List<Guid> Ids { get; set; }
    }

    /// <summary>
    /// 根据业务Id获取账单的返回值封装类。
    /// </summary>
    public class GetDocBillsByJobIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 结果集合。
        /// </summary>
        public List<GetDocBillsByJobIdItemDto> Result { get; set; } = new List<GetDocBillsByJobIdItemDto>();
    }

    /// <summary>
    /// 根据业务Id获取账单的返回值项封装类。
    /// </summary>
    public class GetDocBillsByJobIdItemDto
    {
        /// <summary>
        /// 业务Id。
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// 账单集合。
        /// </summary>
        public List<DocBill> Bills { get; set; } = new List<DocBill>();
    }

    #endregion 账单CRUD相关DTO

    #region 批量生成账单相关DTO

    /// <summary>
    /// 从费用批量生成账单的参数封装类。
    /// </summary>
    public class AddDocBillsFromFeesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要生成账单的费用ID集合。
        /// 系统将自动过滤出已审核、未建账单、有结算单位的费用，按结算单位和收支方向分组生成账单。
        /// </summary>
        [Required]
        public List<Guid> FeeIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 默认币种。
        /// 如果不指定，默认使用"CNY"（人民币）。
        /// </summary>
        public string DefaultCurrency { get; set; } = "CNY";
    }

    /// <summary>
    /// 从费用批量生成账单的返回值封装类。
    /// </summary>
    public class AddDocBillsFromFeesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 生成的账单ID列表。
        /// </summary>
        public List<Guid> CreatedBillIds { get; set; } = new List<Guid>();
    }

    #endregion 批量生成账单相关DTO
}
