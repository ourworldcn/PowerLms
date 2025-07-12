using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 角色
    /// <summary>
    /// 标记删除角色功能的参数封装类。
    /// </summary>
    public class RemovePlRoleParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除角色功能的返回值封装类。
    /// </summary>
    public class RemovePlRoleReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有角色功能的返回值封装类。
    /// </summary>
    public class GetAllPlRoleReturnDto : PagingReturnDtoBase<PlRole>
    {
    }

    /// <summary>
    /// 增加新角色功能参数封装类。省略PlRole.OrgId自动填充为调用者所在商户Id。
    /// </summary>
    public class AddPlRoleParamsDto : AddParamsDtoBase<PlRole>
    {
    }

    /// <summary>
    /// 增加新角色功能返回值封装类。
    /// </summary>
    public class AddPlRoleReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>
    /// 修改角色信息功能参数封装类。
    /// </summary>
    public class ModifyPlRoleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 角色数据。
        /// </summary>
        public PlRole PlRole { get; set; }
    }

    /// <summary>
    /// 修改角色信息功能返回值封装类。
    /// </summary>
    public class ModifyPlRoleReturnDto : ReturnDtoBase
    {
    }
    #endregion 角色
}