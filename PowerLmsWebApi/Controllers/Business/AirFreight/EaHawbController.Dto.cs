/*
 * 项目：PowerLms | 模块：空运出口分单DTO
 * 功能：空运出口分单的请求和响应数据传输对象
 * 技术要点：数据传输对象封装
 * 作者：zc | 创建：2026-02-01
 */

using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 空运出口分单相关

    /// <summary>
    /// 标记删除空运出口分单功能的参数封装类。
    /// </summary>
    public class RemovePlEaHawbParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口分单功能的返回值封装类。
    /// </summary>
    public class RemovePlEaHawbReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口分单功能的返回值封装类。
    /// </summary>
    public class GetAllPlEaHawbReturnDto : PagingReturnDtoBase<EaHawb>
    {
    }

    /// <summary>
    /// 增加新空运出口分单功能参数封装类。
    /// </summary>
    public class AddPlEaHawbParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口分单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaHawb EaHawb { get; set; }
    }

    /// <summary>
    /// 增加新空运出口分单功能返回值封装类。
    /// </summary>
    public class AddPlEaHawbReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口分单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口分单信息功能参数封装类。
    /// </summary>
    public class ModifyPlEaHawbParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口分单数据。
        /// </summary>
        public EaHawb EaHawb { get; set; }
    }

    /// <summary>
    /// 修改空运出口分单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlEaHawbReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口分单相关

    #region 空运出口分单其他费用相关

    /// <summary>
    /// 标记删除空运出口分单其他费用功能的参数封装类。
    /// </summary>
    public class RemoveEaHawbOtherChargeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口分单其他费用功能的返回值封装类。
    /// </summary>
    public class RemoveEaHawbOtherChargeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口分单其他费用功能的返回值封装类。
    /// </summary>
    public class GetAllEaHawbOtherChargeReturnDto : PagingReturnDtoBase<EaHawbOtherCharge>
    {
    }

    /// <summary>
    /// 增加新空运出口分单其他费用功能参数封装类。
    /// </summary>
    public class AddEaHawbOtherChargeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口分单其他费用信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaHawbOtherCharge EaHawbOtherCharge { get; set; }
    }

    /// <summary>
    /// 增加新空运出口分单其他费用功能返回值封装类。
    /// </summary>
    public class AddEaHawbOtherChargeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口分单其他费用的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口分单其他费用信息功能参数封装类。
    /// </summary>
    public class ModifyEaHawbOtherChargeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口分单其他费用数据。
        /// </summary>
        public EaHawbOtherCharge EaHawbOtherCharge { get; set; }
    }

    /// <summary>
    /// 修改空运出口分单其他费用信息功能返回值封装类。
    /// </summary>
    public class ModifyEaHawbOtherChargeReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口分单其他费用相关

    #region 空运出口分单委托明细相关

    /// <summary>
    /// 标记删除空运出口分单委托明细功能的参数封装类。
    /// </summary>
    public class RemoveEaHawbCubageParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口分单委托明细功能的返回值封装类。
    /// </summary>
    public class RemoveEaHawbCubageReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口分单委托明细功能的返回值封装类。
    /// </summary>
    public class GetAllEaHawbCubageReturnDto : PagingReturnDtoBase<EaHawbCubage>
    {
    }

    /// <summary>
    /// 增加新空运出口分单委托明细功能参数封装类。
    /// </summary>
    public class AddEaHawbCubageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口分单委托明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaHawbCubage EaHawbCubage { get; set; }
    }

    /// <summary>
    /// 增加新空运出口分单委托明细功能返回值封装类。
    /// </summary>
    public class AddEaHawbCubageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口分单委托明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口分单委托明细信息功能参数封装类。
    /// </summary>
    public class ModifyEaHawbCubageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口分单委托明细数据。
        /// </summary>
        public EaHawbCubage EaHawbCubage { get; set; }
    }

    /// <summary>
    /// 修改空运出口分单委托明细信息功能返回值封装类。
    /// </summary>
    public class ModifyEaHawbCubageReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口分单委托明细相关
}
