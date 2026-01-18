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
        public List<PlPermission> Permissions { get; set; } = new List<PlPermission>();
    }
    /// <summary>
    /// 设置角色的许可权限功能的参数封装类。
    /// </summary>
    public class SetPermissionsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 角色的Id。
        /// </summary>
        public Guid RoleId { get; set; }
        /// <summary>
        /// 所属许可的Id的集合。未在此集合指定的与角色的关系均被删除。
        /// </summary>
        public List<string> PermissionIds { get; set; } = new List<string>();
    }
    /// <summary>
    /// 设置角色的许可权限功能的返回值封装类。
    /// </summary>
    public class SetPermissionsReturnDto : ReturnDtoBase
    {
    }
    /// <summary>
    /// 设置角色的所属用户功能的参数封装类。
    /// </summary>
    public class SetUsersParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 角色的Id。
        /// </summary>
        public Guid RoleId { get; set; }
        /// <summary>
        /// 所属用户的Id的集合。未在此集合指定的与用户的关系均被删除。
        /// </summary>
        public List<Guid> UserIds { get; set; } = new List<Guid>();
    }
    /// <summary>
    /// 设置角色的所属用户功能的返回值封装类。
    /// </summary>
    public class SetUsersReturnDto : ReturnDtoBase
    {
    }
    #region 角色-权限关系
    /// <summary>
    /// 标记删除角色-权限关系功能的参数封装类。
    /// </summary>
    public class RemoveRolePermissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 角色Id。
        /// </summary>
        public Guid RoleId { get; set; }
        /// <summary>
        /// 权限的Id。
        /// </summary>
        public string PermissionId { get; set; }
    }
    /// <summary>
    /// 标记删除角色-权限关系功能的返回值封装类。
    /// </summary>
    public class RemoveRolePermissionReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 获取所有角色-权限关系功能的返回值封装类。
    /// </summary>
    public class GetAllRolePermissionReturnDto : PagingReturnDtoBase<RolePermission>
    {
    }
    /// <summary>
    /// 增加新角色-权限关系功能参数封装类。
    /// </summary>
    public class AddRolePermissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新角色-权限关系信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public RolePermission RolePermission { get; set; }
    }
    /// <summary>
    /// 增加新角色-权限关系功能返回值封装类。
    /// </summary>
    public class AddRolePermissionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新角色-权限关系的Id。
        /// </summary>
        public Guid Id { get; set; }
    }
    #endregion 角色-权限关系
    #region 用户-角色关系
    /// <summary>
    /// 标记删除用户-角色关系功能的参数封装类。
    /// </summary>
    public class RemoveAccountRoleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 用户Id。
        /// </summary>
        public Guid UserId { get; set; }
        /// <summary>
        /// 角色Id。
        /// </summary>
        public Guid RoleId { get; set; }
    }
    /// <summary>
    /// 标记删除用户-角色关系功能的返回值封装类。
    /// </summary>
    public class RemoveAccountRoleReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 获取所有用户-角色关系功能的返回值封装类。
    /// </summary>
    public class GetAllAccountRoleReturnDto : PagingReturnDtoBase<AccountRole>
    {
    }
    /// <summary>
    /// 增加新用户-角色关系功能参数封装类。
    /// </summary>
    public class AddAccountRoleParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新用户-角色关系信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public AccountRole AccountRole { get; set; }
    }
    /// <summary>
    /// 增加新用户-角色关系功能返回值封装类。
    /// </summary>
    public class AddAccountRoleReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新用户-角色关系的Id。
        /// </summary>
        public Guid Id { get; set; }
    }
    #endregion 用户-角色关系
    #region 权限
    /// <summary>
    /// 标记删除权限功能的参数封装类。
    /// </summary>
    public class RemovePlPermissionParamsDto : RemoveParamsDtoBase
    {
    }
    /// <summary>
    /// 标记删除权限功能的返回值封装类。
    /// </summary>
    public class RemovePlPermissionReturnDto : RemoveReturnDtoBase
    {
    }
    /// <summary>
    /// 获取所有权限功能的返回值封装类。
    /// </summary>
    public class GetAllPlPermissionReturnDto : PagingReturnDtoBase<PlPermission>
    {
    }
    /// <summary>
    /// 增加新权限功能参数封装类。
    /// </summary>
    public class AddPlPermissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新权限信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlPermission PlPermission { get; set; }
    }
    /// <summary>
    /// 增加新权限功能返回值封装类。
    /// </summary>
    public class AddPlPermissionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新权限的Id。
        /// </summary>
        public string Id { get; set; }
    }
    /// <summary>
    /// 修改权限信息功能参数封装类。
    /// </summary>
    public class ModifyPlPermissionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 权限数据。
        /// </summary>
        public PlPermission Item { get; set; }
    }
    /// <summary>
    /// 修改权限信息功能返回值封装类。
    /// </summary>
    public class ModifyPlPermissionReturnDto : ReturnDtoBase
    {
    }
    #endregion 权限
}