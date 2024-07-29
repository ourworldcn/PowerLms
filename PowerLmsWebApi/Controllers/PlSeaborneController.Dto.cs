using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 海运进口单相关

    /// <summary>
    /// 标记删除海运进口单功能的参数封装类。
    /// </summary>
    public class RemovePlIsDocParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除海运进口单功能的返回值封装类。
    /// </summary>
    public class RemovePlIsDocReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有海运进口单功能的返回值封装类。
    /// </summary>
    public class GetAllPlIsDocReturnDto : PagingReturnDtoBase<PlIsDoc>
    {
    }

    /// <summary>
    /// 增加新海运进口单功能参数封装类。
    /// </summary>
    public class AddPlIsDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新海运进口单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlIsDoc PlIsDoc { get; set; }
    }

    /// <summary>
    /// 增加新海运进口单功能返回值封装类。
    /// </summary>
    public class AddPlIsDocReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新海运进口单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改海运进口单信息功能参数封装类。
    /// </summary>
    public class ModifyPlIsDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 海运进口单数据。
        /// </summary>
        public PlIsDoc PlIsDoc { get; set; }
    }

    /// <summary>
    /// 修改海运进口单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlIsDocReturnDto : ReturnDtoBase
    {
    }
    #endregion  海运进口单相关

    #region 海运箱量相关

    /// <summary>
    /// 删除海运箱量功能的参数封装类。
    /// </summary>
    public class RemoveContainerKindCountParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除海运箱量功能的返回值封装类。
    /// </summary>
    public class RemoveContainerKindCountReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有海运箱量功能的返回值封装类。
    /// </summary>
    public class GetAllContainerKindCountReturnDto : PagingReturnDtoBase<ContainerKindCount>
    {
    }

    /// <summary>
    /// 增加新海运箱量功能参数封装类。
    /// </summary>
    public class AddContainerKindCountParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新海运箱量信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public ContainerKindCount ContainerKindCount { get; set; }
    }

    /// <summary>
    /// 增加新海运箱量功能返回值封装类。
    /// </summary>
    public class AddContainerKindCountReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新海运箱量的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改海运箱量信息功能参数封装类。
    /// </summary>
    public class ModifyContainerKindCountParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 海运箱量数据。
        /// </summary>
        public ContainerKindCount ContainerKindCount { get; set; }
    }

    /// <summary>
    /// 修改海运箱量信息功能返回值封装类。
    /// </summary>
    public class ModifyContainerKindCountReturnDto : ReturnDtoBase
    {
    }
    #endregion  海运箱量相关


}
