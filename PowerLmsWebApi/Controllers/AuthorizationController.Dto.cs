using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 获取当前用户在当前机构下的所有权限功能的参数封装类。
    /// </summary>
    public class GetAllPermissionsInCurrentUserParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 获取当前用户在当前机构下的所有权限功能的返回值封装类。
    /// </summary>
    public class GetAllPermissionsInCurrentUserReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 权限集合，无重复。
        /// </summary>
        public List<PlPermission> Permissions { get; set; }=new List<PlPermission>();
    }

}