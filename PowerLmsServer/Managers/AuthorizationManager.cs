using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
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
        public AuthorizationManager(PowerLmsUserDbContext dbContext, IMemoryCache cache, PermissionManager permissionManager)
        {
            _DbContext = dbContext;
            _Cache = cache;
            _PermissionManager = permissionManager;
        }

        readonly PermissionManager _PermissionManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly IMemoryCache _Cache;

        /// <summary>
        /// 缓存项的前缀。
        /// </summary>
        public const string CachePrefix = "Permissions.";

        /// <summary>
        /// 是否拥有指定的一组权限。对于超管，商管总是返回true。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pIds"></param>
        /// <returns>拥有指定的所有权限则返回true,否则返回false。</returns>
        public bool HasPermission(Account user, params string[] pIds)
        {
            if (user.IsSuperAdmin) return true;
            if (user.IsMerchantAdmin)
            {
                return true;
            }
            var ci = _PermissionManager.GetOrLoadCurrentPermissionsByUser(user);
            return pIds.All(c => ci.Data.ContainsKey(c));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="orgId"></param>
        /// <param name="pIds"></param>
        /// <returns></returns>
        public bool HasPermission(Account user, Guid orgId, params string[] pIds)
        {
            if (user.IsSuperAdmin) return true;
            if (user.IsMerchantAdmin)
            {
                return true;
            }
            var ci = _PermissionManager.GetOrLoadCurrentPermissionsByUser(user);
            return pIds.All(c => ci.Data.ContainsKey(c));
        }
    }
}
