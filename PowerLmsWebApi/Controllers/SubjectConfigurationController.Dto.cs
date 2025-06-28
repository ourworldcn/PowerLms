using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 获取全部财务科目设置

    /// <summary>
    /// 获取全部财务科目设置的参数封装类
    /// </summary>
    public class GetAllSubjectConfigurationParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取全部财务科目设置的返回值封装类
    /// </summary>
    public class GetAllSubjectConfigurationReturnDto : PagingReturnDtoBase<SubjectConfiguration>
    {
    }

    #endregion 获取全部财务科目设置

    #region 增加财务科目设置

    /// <summary>
    /// 增加财务科目设置的参数封装类
    /// </summary>
    public class AddSubjectConfigurationParamsDto : AddParamsDtoBase<SubjectConfiguration>
    {
    }

    /// <summary>
    /// 增加财务科目设置的返回值封装类
    /// </summary>
    public class AddSubjectConfigurationReturnDto : AddReturnDtoBase
    {
    }

    #endregion 增加财务科目设置

    #region 修改财务科目设置

    /// <summary>
    /// 修改财务科目设置的参数封装类
    /// </summary>
    public class ModifySubjectConfigurationParamsDto : ModifyParamsDtoBase<SubjectConfiguration>
    {
    }

    /// <summary>
    /// 修改财务科目设置的返回值封装类
    /// </summary>
    public class ModifySubjectConfigurationReturnDto : ModifyReturnDtoBase
    {
    }

    #endregion 修改财务科目设置

    #region 删除财务科目设置

    /// <summary>
    /// 删除财务科目设置的参数封装类
    /// </summary>
    public class RemoveSubjectConfigurationParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除财务科目设置的返回值封装类
    /// </summary>
    public class RemoveSubjectConfigurationReturnDto : RemoveReturnDtoBase
    {
    }

    #endregion 删除财务科目设置

    #region 恢复财务科目设置

    /// <summary>
    /// 恢复被删除财务科目设置的参数封装类
    /// </summary>
    public class RestoreSubjectConfigurationParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复被删除财务科目设置的返回值封装类
    /// </summary>
    public class RestoreSubjectConfigurationReturnDto : RestoreReturnDtoBase
    {
    }

    #endregion 恢复财务科目设置

    #region 获取科目编码字典

    /// <summary>
    /// 获取科目编码字典的参数封装类
    /// </summary>
    public class GetSubjectCodeDictionaryParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 获取科目编码字典的返回值封装类
    /// </summary>
    public class GetSubjectCodeDictionaryReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 科目编码字典，Key为编码，Value为显示名称
        /// </summary>
        public Dictionary<string, string> Result { get; set; } = new Dictionary<string, string>();
    }

    #endregion 获取科目编码字典
}