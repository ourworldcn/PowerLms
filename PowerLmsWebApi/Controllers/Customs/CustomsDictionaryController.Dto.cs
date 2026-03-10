/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关专用字典控制器 DTO
 * 作者：zc | 创建：2026-03
 */
using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region CustomHsCode（HSCODE基础表）
    /// <summary>获取HSCODE基础表功能的返回值封装类。</summary>
    public class GetAllCustomHsCodeReturnDto : PagingReturnDtoBase<CdHsCode> { }
    /// <summary>增加HSCODE基础表记录的参数封装类。</summary>
    public class AddCustomHsCodeParamsDto : AddParamsDtoBase<CdHsCode> { }
    /// <summary>增加HSCODE基础表记录的返回值封装类。</summary>
    public class AddCustomHsCodeReturnDto : AddReturnDtoBase { }
    /// <summary>修改HSCODE基础表记录的参数封装类。</summary>
    public class ModifyCustomHsCodeParamsDto : ModifyParamsDtoBase<CdHsCode> { }
    /// <summary>修改HSCODE基础表记录的返回值封装类。</summary>
    public class ModifyCustomHsCodeReturnDto : ModifyReturnDtoBase { }
    /// <summary>删除HSCODE基础表记录的参数封装类。</summary>
    public class RemoveCustomHsCodeParamsDto : RemoveParamsDtoBase { }
    /// <summary>删除HSCODE基础表记录的返回值封装类。</summary>
    public class RemoveCustomHsCodeReturnDto : RemoveReturnDtoBase { }
    #endregion CustomHsCode

    #region CustomGoodsVsCiqCode（CIQCODE检疫代码表）
    /// <summary>获取CIQCODE检疫代码表功能的返回值封装类。</summary>
    public class GetAllCustomGoodsVsCiqCodeReturnDto : PagingReturnDtoBase<CdGoodsVsCiqCode> { }
    /// <summary>增加CIQCODE检疫代码表记录的参数封装类。</summary>
    public class AddCustomGoodsVsCiqCodeParamsDto : AddParamsDtoBase<CdGoodsVsCiqCode> { }
    /// <summary>增加CIQCODE检疫代码表记录的返回值封装类。</summary>
    public class AddCustomGoodsVsCiqCodeReturnDto : AddReturnDtoBase { }
    /// <summary>修改CIQCODE检疫代码表记录的参数封装类。</summary>
    public class ModifyCustomGoodsVsCiqCodeParamsDto : ModifyParamsDtoBase<CdGoodsVsCiqCode> { }
    /// <summary>修改CIQCODE检疫代码表记录的返回值封装类。</summary>
    public class ModifyCustomGoodsVsCiqCodeReturnDto : ModifyReturnDtoBase { }
    /// <summary>删除CIQCODE检疫代码表记录的参数封装类。</summary>
    public class RemoveCustomGoodsVsCiqCodeParamsDto : RemoveParamsDtoBase { }
    /// <summary>删除CIQCODE检疫代码表记录的返回值封装类。</summary>
    public class RemoveCustomGoodsVsCiqCodeReturnDto : RemoveReturnDtoBase { }
    #endregion CustomGoodsVsCiqCode

    #region CustomPlace（国内行政区划表）
    /// <summary>获取国内行政区划表功能的返回值封装类。</summary>
    public class GetAllCustomPlaceReturnDto : PagingReturnDtoBase<CdPlace> { }
    /// <summary>增加国内行政区划表记录的参数封装类。</summary>
    public class AddCustomPlaceParamsDto : AddParamsDtoBase<CdPlace> { }
    /// <summary>增加国内行政区划表记录的返回值封装类。</summary>
    public class AddCustomPlaceReturnDto : AddReturnDtoBase { }
    /// <summary>修改国内行政区划表记录的参数封装类。</summary>
    public class ModifyCustomPlaceParamsDto : ModifyParamsDtoBase<CdPlace> { }
    /// <summary>修改国内行政区划表记录的返回值封装类。</summary>
    public class ModifyCustomPlaceReturnDto : ModifyReturnDtoBase { }
    /// <summary>删除国内行政区划表记录的参数封装类。</summary>
    public class RemoveCustomPlaceParamsDto : RemoveParamsDtoBase { }
    /// <summary>删除国内行政区划表记录的返回值封装类。</summary>
    public class RemoveCustomPlaceReturnDto : RemoveReturnDtoBase { }
    #endregion CustomPlace

    #region CustomDomesticPort（国内口岸代码表）
    /// <summary>获取国内口岸代码表功能的返回值封装类。</summary>
    public class GetAllCustomDomesticPortReturnDto : PagingReturnDtoBase<CdDomesticPort> { }
    /// <summary>增加国内口岸代码表记录的参数封装类。</summary>
    public class AddCustomDomesticPortParamsDto : AddParamsDtoBase<CdDomesticPort> { }
    /// <summary>增加国内口岸代码表记录的返回值封装类。</summary>
    public class AddCustomDomesticPortReturnDto : AddReturnDtoBase { }
    /// <summary>修改国内口岸代码表记录的参数封装类。</summary>
    public class ModifyCustomDomesticPortParamsDto : ModifyParamsDtoBase<CdDomesticPort> { }
    /// <summary>修改国内口岸代码表记录的返回值封装类。</summary>
    public class ModifyCustomDomesticPortReturnDto : ModifyReturnDtoBase { }
    /// <summary>删除国内口岸代码表记录的参数封装类。</summary>
    public class RemoveCustomDomesticPortParamsDto : RemoveParamsDtoBase { }
    /// <summary>删除国内口岸代码表记录的返回值封装类。</summary>
    public class RemoveCustomDomesticPortReturnDto : RemoveReturnDtoBase { }
    #endregion CustomDomesticPort

    #region CustomInspectionPlace（国内地区代码（检疫用）表）
    /// <summary>获取国内地区代码（检疫用）表功能的返回值封装类。</summary>
    public class GetAllCustomInspectionPlaceReturnDto : PagingReturnDtoBase<CdInspectionPlace> { }
    /// <summary>增加国内地区代码（检疫用）表记录的参数封装类。</summary>
    public class AddCustomInspectionPlaceParamsDto : AddParamsDtoBase<CdInspectionPlace> { }
    /// <summary>增加国内地区代码（检疫用）表记录的返回值封装类。</summary>
    public class AddCustomInspectionPlaceReturnDto : AddReturnDtoBase { }
    /// <summary>修改国内地区代码（检疫用）表记录的参数封装类。</summary>
    public class ModifyCustomInspectionPlaceParamsDto : ModifyParamsDtoBase<CdInspectionPlace> { }
    /// <summary>修改国内地区代码（检疫用）表记录的返回值封装类。</summary>
    public class ModifyCustomInspectionPlaceReturnDto : ModifyReturnDtoBase { }
    /// <summary>删除国内地区代码（检疫用）表记录的参数封装类。</summary>
    public class RemoveCustomInspectionPlaceParamsDto : RemoveParamsDtoBase { }
    /// <summary>删除国内地区代码（检疫用）表记录的返回值封装类。</summary>
    public class RemoveCustomInspectionPlaceReturnDto : RemoveReturnDtoBase { }
    #endregion CustomInspectionPlace

    #region CustomPlPort（报关专用港口表）
    /// <summary>获取报关专用港口表功能的返回值封装类。</summary>
    public class GetAllCustomPlPortReturnDto : PagingReturnDtoBase<CdPort> { }
    /// <summary>增加报关专用港口表记录的参数封装类。</summary>
    public class AddCustomPlPortParamsDto : AddParamsDtoBase<CdPort> { }
    /// <summary>增加报关专用港口表记录的返回值封装类。</summary>
    public class AddCustomPlPortReturnDto : AddReturnDtoBase { }
    /// <summary>修改报关专用港口表记录的参数封装类。</summary>
    public class ModifyCustomPlPortParamsDto : ModifyParamsDtoBase<CdPort> { }
    /// <summary>修改报关专用港口表记录的返回值封装类。</summary>
    public class ModifyCustomPlPortReturnDto : ModifyReturnDtoBase { }
    /// <summary>删除报关专用港口表记录的参数封装类。</summary>
    public class RemoveCustomPlPortParamsDto : RemoveParamsDtoBase { }
    /// <summary>删除报关专用港口表记录的返回值封装类。</summary>
    public class RemoveCustomPlPortReturnDto : RemoveReturnDtoBase { }
    #endregion CustomPlPort

    #region 导入导出
    /// <summary>导入报关专用字典的参数封装类。</summary>
    public class ImportCustomsDictionariesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 是否删除现有数据。true=替换模式（先删后导）；false=更新模式（按Id匹配覆盖，默认）。
        /// </summary>
        public bool DeleteExisting { get; set; } = false;
    }
    /// <summary>导入报关专用字典的返回值封装类。</summary>
    public class ImportCustomsDictionariesReturnDto : ReturnDtoBase
    {
        /// <summary>总导入记录数。</summary>
        public int ImportedCount { get; set; }
        /// <summary>成功处理的Sheet数量。</summary>
        public int ProcessedSheets { get; set; }
    }
    #endregion 导入导出
}
