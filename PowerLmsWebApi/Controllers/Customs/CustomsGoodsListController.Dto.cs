/*
 * 项目：PowerLms | 模块：报关单货物明细DTO
 * 功能：报关单货物明细子表的请求和响应数据传输对象
 * 技术要点：数据传输对象封装
 * 作者：zc | 创建：2026-02
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 报关单货物明细相关
    /// <summary>
    /// 获取所有报关单货物明细功能的返回值封装类。
    /// </summary>
    public class GetAllCustomsGoodsListReturnDto : PagingReturnDtoBase<CustomsGoodsList>
    {
    }
    /// <summary>
    /// 增加新报关单货物明细功能参数封装类。
    /// </summary>
    public class AddCustomsGoodsListParamsDto : AddParamsDtoBase<CustomsGoodsList>
    {
    }
    /// <summary>
    /// 增加新报关单货物明细功能返回值封装类。
    /// </summary>
    public class AddCustomsGoodsListReturnDto : AddReturnDtoBase
    {
    }
    /// <summary>
    /// 修改报关单货物明细信息功能参数封装类。
    /// </summary>
    public class ModifyCustomsGoodsListParamsDto : ModifyParamsDtoBase<CustomsGoodsList>
    {
    }
    /// <summary>
    /// 修改报关单货物明细信息功能返回值封装类。
    /// </summary>
    public class ModifyCustomsGoodsListReturnDto : ModifyReturnDtoBase
    {
    }
    /// <summary>
    /// 删除报关单货物明细功能的参数封装类。
    /// </summary>
    public class RemoveCustomsGoodsListParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 删除报关单货物明细功能的返回值封装类。
    /// </summary>
    public class RemoveCustomsGoodsListReturnDto : RemoveReturnDtoBase
    {
    }
    #endregion 报关单货物明细相关
}
