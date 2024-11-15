using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 权限管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class AuthorizationManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AuthorizationManager(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        readonly PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 获取所有指定用户Id的权限。会考虑其当前所属机构。
        /// </summary>
        /// <param name="account"></param>
        public IQueryable<PlPermission> GetPermissionsFromUser(Account account)
        {
            var roleIds = GetRolesFromUserId(account).Select(c => c.Id);
            return GetPermissionsFromRoleIds(roleIds);
        }

        /// <summary>
        /// 用角色Id集合获取所有的权限集合。
        /// </summary>
        /// <param name="roleIds">一组角色Id。</param>
        /// <returns></returns>
        public IQueryable<PlPermission> GetPermissionsFromRoleIds(IQueryable<Guid> roleIds)
        {
            var permIds = _DbContext.PlRolePermissions.Where(c => roleIds.Distinct().Contains(c.RoleId)).Select(c => c.PermissionId).Distinct();
            return _DbContext.PlPermissions.Where(c => permIds.Contains(c.Name));
        }

        /// <summary>
        /// 获取指定用户的所有所属角色。会考虑其当前所属机构。
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public IQueryable<PlRole> GetRolesFromUserId(Account account)
        {
            var roleIds = _DbContext.PlAccountRoles.Where(c => c.UserId == account.Id).Select(c => c.RoleId).Distinct();
            return _DbContext.PlRoles.Where(c => c.OrgId == account.OrgId && roleIds.Contains(c.Id));
        }

    }
}
