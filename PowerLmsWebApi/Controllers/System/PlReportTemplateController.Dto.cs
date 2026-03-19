using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 获取报表模板列表

    /// <summary>
    /// 获取报表模板列表的参数。
    /// </summary>
    public class GetAllPlReportTemplateParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取报表模板列表的返回值。
    /// </summary>
    public class GetAllPlReportTemplateReturnDto : PagingReturnDtoBase<PlReportTemplate>
    {
    }

    #endregion 获取报表模板列表

    #region 增加报表模板

    /// <summary>
    /// 增加报表模板的参数。
    /// </summary>
    public class AddPlReportTemplateParamsDto : AddParamsDtoBase<PlReportTemplate>
    {
    }

    /// <summary>
    /// 增加报表模板的返回值。
    /// </summary>
    public class AddPlReportTemplateReturnDto : AddReturnDtoBase
    {
    }

    #endregion 增加报表模板

    #region 修改报表模板

    /// <summary>
    /// 修改报表模板的参数。
    /// </summary>
    public class ModifyPlReportTemplateParamsDto : ModifyParamsDtoBase<PlReportTemplate>
    {
    }

    /// <summary>
    /// 修改报表模板的返回值。
    /// </summary>
    public class ModifyPlReportTemplateReturnDto : ModifyReturnDtoBase
    {
    }

    #endregion 修改报表模板

    #region 删除报表模板

    /// <summary>
    /// 删除报表模板的参数。
    /// </summary>
    public class RemovePlReportTemplateParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除报表模板的返回值。
    /// </summary>
    public class RemovePlReportTemplateReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion 删除报表模板
}
