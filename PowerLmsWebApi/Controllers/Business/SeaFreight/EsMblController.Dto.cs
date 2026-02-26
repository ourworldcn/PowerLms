/*
 * 项目:PowerLms | 模块:海运出口主提单DTO
 * 功能:海运出口主提单的请求和响应数据传输对象
 * 技术要点:数据传输对象封装
 * 作者:zc | 创建:2026-02 | 修改:2026-02-23 初始创建
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 海运出口主提单相关
    /// <summary>
    /// 标记删除海运出口主提单功能的参数封装类。
    /// </summary>
    public class RemoveEsMblParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 标记删除海运出口主提单功能的返回值封装类。
    /// </summary>
    public class RemoveEsMblReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 获取所有海运出口主提单功能的返回值封装类。
    /// </summary>
    public class GetAllEsMblReturnDto : PagingReturnDtoBase<EsMbl>
    {
    }
    /// <summary>
    /// 增加新海运出口主提单功能参数封装类。
    /// </summary>
    public class AddEsMblParamsDto : AddParamsDtoBase<EsMbl>
    {
    }
    /// <summary>
    /// 增加新海运出口主提单功能返回值封装类。
    /// </summary>
    public class AddEsMblReturnDto : AddReturnDtoBase
    {
    }
    /// <summary>
    /// 修改海运出口主提单信息功能参数封装类。
    /// </summary>
    public class ModifyEsMblParamsDto : ModifyParamsDtoBase<EsMbl>
    {
    }
    /// <summary>
    /// 修改海运出口主提单信息功能返回值封装类。
    /// </summary>
    public class ModifyEsMblReturnDto : ModifyReturnDtoBase
    {
    }
    #endregion 海运出口主提单相关
}
