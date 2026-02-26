/*
 * 项目:PowerLms | 模块:海运出口分提单DTO
 * 功能:海运出口分提单的请求和响应数据传输对象
 * 技术要点:数据传输对象封装
 * 作者:zc | 创建:2026-02 | 修改:2026-02-23 初始创建
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 海运出口分提单相关
    /// <summary>
    /// 标记删除海运出口分提单功能的参数封装类。
    /// </summary>
    public class RemoveEsHblParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 标记删除海运出口分提单功能的返回值封装类。
    /// </summary>
    public class RemoveEsHblReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 获取所有海运出口分提单功能的返回值封装类。
    /// </summary>
    public class GetAllEsHblReturnDto : PagingReturnDtoBase<EsHbl>
    {
    }
    /// <summary>
    /// 增加新海运出口分提单功能参数封装类。
    /// </summary>
    public class AddEsHblParamsDto : AddParamsDtoBase<EsHbl>
    {
    }
    /// <summary>
    /// 增加新海运出口分提单功能返回值封装类。
    /// </summary>
    public class AddEsHblReturnDto : AddReturnDtoBase
    {
    }
    /// <summary>
    /// 修改海运出口分提单信息功能参数封装类。
    /// </summary>
    public class ModifyEsHblParamsDto : ModifyParamsDtoBase<EsHbl>
    {
    }
    /// <summary>
    /// 修改海运出口分提单信息功能返回值封装类。
    /// </summary>
    public class ModifyEsHblReturnDto : ModifyReturnDtoBase
    {
    }
    #endregion 海运出口分提单相关
}
