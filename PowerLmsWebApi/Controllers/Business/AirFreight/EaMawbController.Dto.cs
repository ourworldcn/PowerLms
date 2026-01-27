/*
 * 项目：PowerLms | 模块：空运出口主单DTO
 * 功能：空运出口主单的请求和响应数据传输对象
 * 技术要点：数据传输对象封装
 * 作者：zc | 创建：2026-01-26
 */

using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 空运出口主单相关

    /// <summary>
    /// 标记删除空运出口主单功能的参数封装类。
    /// </summary>
    public class RemovePlEaMawbParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口主单功能的返回值封装类。
    /// </summary>
    public class RemovePlEaMawbReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口主单功能的返回值封装类。
    /// </summary>
    public class GetAllPlEaMawbReturnDto : PagingReturnDtoBase<EaMawb>
    {
    }

    /// <summary>
    /// 增加新空运出口主单功能参数封装类。
    /// </summary>
    public class AddPlEaMawbParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口主单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaMawb EaMawb { get; set; }
    }

    /// <summary>
    /// 增加新空运出口主单功能返回值封装类。
    /// </summary>
    public class AddPlEaMawbReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口主单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单信息功能参数封装类。
    /// </summary>
    public class ModifyPlEaMawbParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口主单数据。
        /// </summary>
        public EaMawb EaMawb { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlEaMawbReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口主单相关

    #region 空运出口主单其他费用相关

    /// <summary>
    /// 标记删除空运出口主单其他费用功能的参数封装类。
    /// </summary>
    public class RemoveEaMawbOtherChargeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口主单其他费用功能的返回值封装类。
    /// </summary>
    public class RemoveEaMawbOtherChargeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口主单其他费用功能的返回值封装类。
    /// </summary>
    public class GetAllEaMawbOtherChargeReturnDto : PagingReturnDtoBase<EaMawbOtherCharge>
    {
    }

    /// <summary>
    /// 增加新空运出口主单其他费用功能参数封装类。
    /// </summary>
    public class AddEaMawbOtherChargeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口主单其他费用信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaMawbOtherCharge EaMawbOtherCharge { get; set; }
    }

    /// <summary>
    /// 增加新空运出口主单其他费用功能返回值封装类。
    /// </summary>
    public class AddEaMawbOtherChargeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口主单其他费用的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单其他费用信息功能参数封装类。
    /// </summary>
    public class ModifyEaMawbOtherChargeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口主单其他费用数据。
        /// </summary>
        public EaMawbOtherCharge EaMawbOtherCharge { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单其他费用信息功能返回值封装类。
    /// </summary>
    public class ModifyEaMawbOtherChargeReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口主单其他费用相关

    #region 空运出口主单委托明细相关

    /// <summary>
    /// 标记删除空运出口主单委托明细功能的参数封装类。
    /// </summary>
    public class RemoveEaCubageParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口主单委托明细功能的返回值封装类。
    /// </summary>
    public class RemoveEaCubageReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口主单委托明细功能的返回值封装类。
    /// </summary>
    public class GetAllEaCubageReturnDto : PagingReturnDtoBase<EaCubage>
    {
    }

    /// <summary>
    /// 增加新空运出口主单委托明细功能参数封装类。
    /// </summary>
    public class AddEaCubageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口主单委托明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaCubage EaCubage { get; set; }
    }

    /// <summary>
    /// 增加新空运出口主单委托明细功能返回值封装类。
    /// </summary>
    public class AddEaCubageReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口主单委托明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单委托明细信息功能参数封装类。
    /// </summary>
    public class ModifyEaCubageParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口主单委托明细数据。
        /// </summary>
        public EaCubage EaCubage { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单委托明细信息功能返回值封装类。
    /// </summary>
    public class ModifyEaCubageReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口主单委托明细相关

    #region 空运出口主单品名明细相关

    /// <summary>
    /// 标记删除空运出口主单品名明细功能的参数封装类。
    /// </summary>
    public class RemoveEaGoodsDetailParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口主单品名明细功能的返回值封装类。
    /// </summary>
    public class RemoveEaGoodsDetailReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口主单品名明细功能的返回值封装类。
    /// </summary>
    public class GetAllEaGoodsDetailReturnDto : PagingReturnDtoBase<EaGoodsDetail>
    {
    }

    /// <summary>
    /// 增加新空运出口主单品名明细功能参数封装类。
    /// </summary>
    public class AddEaGoodsDetailParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口主单品名明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaGoodsDetail EaGoodsDetail { get; set; }
    }

    /// <summary>
    /// 增加新空运出口主单品名明细功能返回值封装类。
    /// </summary>
    public class AddEaGoodsDetailReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口主单品名明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单品名明细信息功能参数封装类。
    /// </summary>
    public class ModifyEaGoodsDetailParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口主单品名明细数据。
        /// </summary>
        public EaGoodsDetail EaGoodsDetail { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单品名明细信息功能返回值封装类。
    /// </summary>
    public class ModifyEaGoodsDetailReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口主单品名明细相关

    #region 空运出口主单集装器相关

    /// <summary>
    /// 标记删除空运出口主单集装器功能的参数封装类。
    /// </summary>
    public class RemoveEaContainerParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口主单集装器功能的返回值封装类。
    /// </summary>
    public class RemoveEaContainerReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口主单集装器功能的返回值封装类。
    /// </summary>
    public class GetAllEaContainerReturnDto : PagingReturnDtoBase<EaContainer>
    {
    }

    /// <summary>
    /// 增加新空运出口主单集装器功能参数封装类。
    /// </summary>
    public class AddEaContainerParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口主单集装器信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public EaContainer EaContainer { get; set; }
    }

    /// <summary>
    /// 增加新空运出口主单集装器功能返回值封装类。
    /// </summary>
    public class AddEaContainerReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口主单集装器的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单集装器信息功能参数封装类。
    /// </summary>
    public class ModifyEaContainerParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口主单集装器数据。
        /// </summary>
        public EaContainer EaContainer { get; set; }
    }

    /// <summary>
    /// 修改空运出口主单集装器信息功能返回值封装类。
    /// </summary>
    public class ModifyEaContainerReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口主单集装器相关
}
