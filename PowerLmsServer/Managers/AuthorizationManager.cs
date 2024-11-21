using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
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
        public AuthorizationManager(PowerLmsUserDbContext dbContext, IMemoryCache cache)
        {
            _DbContext = dbContext;
            _Cache = cache;
        }

        readonly PowerLmsUserDbContext _DbContext;
        IMemoryCache _Cache;
        /// <summary>
        /// 缓存项的前缀。
        /// </summary>
        public const string CachePrefix = "Permissions.";

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

        /// <summary>
        /// 加载或获取指定用户的所有权限缓存项。
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Dictionary<string, PlPermission> GetOrLoadPermission(Account user)
        {
            var result = _Cache.GetOrCreate($"{CachePrefix}{user.IdString}", c =>
            {
                c.SlidingExpiration = TimeSpan.FromMinutes(10);
                return GetPermissionsFromUser(user).AsEnumerable().ToDictionary(c => c.Name);
            });
            return result;
        }

        /// <summary>
        /// 加载数据库中许可对象。
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Dictionary<string, PlPermission> LoadPermission(Account user)
        {
            var result = _Cache.Set($"{CachePrefix}{user.IdString}", GetPermissionsFromUser(user).AsEnumerable().ToDictionary(c => c.Name),
                new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });
            return result;
        }

        /// <summary>
        /// 指出指定用户的权限缓存已经无效。
        /// </summary>
        /// <param name="userId"></param>
        public void SetChange(Guid userId)
        {
            _Cache.Remove($"{CachePrefix}{userId}");
        }

        /// <summary>
        /// 是否拥有指定的一组权限。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pIds"></param>
        /// <returns>拥有指定的所有权限则返回true,否则返回false。</returns>
        public bool HasPermission(Account user, params string[] pIds)
        {
            var dic = GetOrLoadPermission(user);
            return pIds.All(c => dic.ContainsKey(c));
        }
    }
}
