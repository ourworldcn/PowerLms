using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 日常费用种类相关

    /// <summary>
    /// 恢复日常费用种类记录的功能参数封装类。
    /// </summary>
    public class RestoreDailyFeesTypeParamsDto : RestoreParamsDtoBase
    {
    }

    /// <summary>
    /// 恢复日常费用种类记录的功能返回值封装类。
    /// </summary>
    public class RestoreDailyFeesTypeReturnDto : RestoreReturnDtoBase
    {
    }

    /// <summary>
    /// 删除日常费用种类记录的功能参数封装类。
    /// </summary>
    public class RemoveDailyFeesTypeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除日常费用种类记录的功能返回值封装类。
    /// </summary>
    public class RemoveDailyFeesTypeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 修改日常费用种类记录的功能参数封装类。
    /// </summary>
    public class ModifyDailyFeesTypeParamsDto : ModifyParamsDtoBase<DailyFeesType>
    {
    }

    /// <summary>
    /// 修改日常费用种类记录的功能返回值封装类。
    /// </summary>
    public class ModifyDailyFeesTypeReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 增加日常费用种类记录的功能参数封装类。
    /// </summary>
    public class AddDailyFeesTypeParamsDto : AddParamsDtoBase<DailyFeesType>
    {
        /// <summary>
        /// 是否同步到子公司/组织机构。适用于超管复制的数据到值中，商管或公司管理员同为普通用户。
        /// </summary>
        public bool CopyToChildren { get; set; }
    }

    /// <summary>
    /// 增加日常费用种类记录的功能返回值封装类。
    /// </summary>
    public class AddDailyFeesTypeReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 获取日常费用种类的功能返回值封装类。
    /// </summary>
    public class GetAllDailyFeesTypeReturnDto : PagingReturnDtoBase<DailyFeesType>
    {
    }

    #endregion 日常费用种类相关
}