/*
 * 项目：PowerLms | 模块：空运业务DTO
 * 功能：空运进口单和空运出口单的请求和响应数据传输对象
 * 技术要点：数据传输对象封装
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 空运进口API恢复
 */

using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 空运进口单相关

    /// <summary>
    /// 标记删除空运进口单功能的参数封装类。
    /// </summary>
    public class RemovePlIaDocParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运进口单功能的返回值封装类。
    /// </summary>
    public class RemovePlIaDocReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运进口单功能的返回值封装类。
    /// </summary>
    public class GetAllPlIaDocReturnDto : PagingReturnDtoBase<PlIaDoc>
    {
    }

    /// <summary>
    /// 增加新空运进口单功能参数封装类。
    /// </summary>
    public class AddPlIaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运进口单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlIaDoc PlIaDoc { get; set; }
    }

    /// <summary>
    /// 增加新空运进口单功能返回值封装类。
    /// </summary>
    public class AddPlIaDocReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运进口单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运进口单信息功能参数封装类。
    /// </summary>
    public class ModifyPlIaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运进口单数据。
        /// </summary>
        public PlIaDoc PlIaDoc { get; set; }
    }

    /// <summary>
    /// 修改空运进口单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlIaDocReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运进口单相关

    #region 空运出口单相关

    /// <summary>
    /// 标记删除空运出口单功能的参数封装类。
    /// </summary>
    public class RemovePlEaDocParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口单功能的返回值封装类。
    /// </summary>
    public class RemovePlEaDocReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口单功能的返回值封装类。
    /// </summary>
    public class GetAllPlEaDocReturnDto : PagingReturnDtoBase<PlEaDoc>
    {
    }

    /// <summary>
    /// 增加新空运出口单功能参数封装类。
    /// </summary>
    public class AddPlEaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlEaDoc PlEaDoc { get; set; }
    }

    /// <summary>
    /// 增加新空运出口单功能返回值封装类。
    /// </summary>
    public class AddPlEaDocReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口单信息功能参数封装类。
    /// </summary>
    public class ModifyPlEaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口单数据。
        /// </summary>
        public PlEaDoc PlEaDoc { get; set; }
    }

    /// <summary>
    /// 修改空运出口单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlEaDocReturnDto : ReturnDtoBase
    {
    }

    #endregion 空运出口单相关
}