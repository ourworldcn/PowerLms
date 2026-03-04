/*
 * 项目：PowerLms | 模块：报关单DTO
 * 功能：报关单主表的请求和响应数据传输对象
 * 技术要点：数据传输对象封装
 * 作者：zc | 创建：2026-02
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 报关单主表相关
    /// <summary>
    /// 获取所有报关单功能的返回值封装类。
    /// </summary>
    public class GetAllCustomsDeclarationReturnDto : PagingReturnDtoBase<CustomsDeclaration>
    {
    }
    /// <summary>
    /// 增加新报关单功能参数封装类。
    /// </summary>
    public class AddCustomsDeclarationParamsDto : AddParamsDtoBase<CustomsDeclaration>
    {
    }
    /// <summary>
    /// 增加新报关单功能返回值封装类。
    /// </summary>
    public class AddCustomsDeclarationReturnDto : AddReturnDtoBase
    {
    }
    /// <summary>
    /// 修改报关单信息功能参数封装类。
    /// </summary>
    public class ModifyCustomsDeclarationParamsDto : ModifyParamsDtoBase<CustomsDeclaration>
    {
    }
    /// <summary>
    /// 修改报关单信息功能返回值封装类。
    /// </summary>
    public class ModifyCustomsDeclarationReturnDto : ModifyReturnDtoBase
    {
    }
    /// <summary>
    /// 删除报关单功能的参数封装类。
    /// </summary>
    public class RemoveCustomsDeclarationParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 删除报关单功能的返回值封装类。
    /// </summary>
    public class RemoveCustomsDeclarationReturnDto : RemoveReturnDtoBase
    {
    }
    #endregion 报关单主表相关
}
