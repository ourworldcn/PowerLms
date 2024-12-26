using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security;
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
        public AuthorizationManager(PowerLmsUserDbContext dbContext, IMemoryCache cache, PermissionManager permissionManager, OwContext owContext)
        {
            _DbContext = dbContext;
            _Cache = cache;
            _PermissionManager = permissionManager;
            _OwContext = owContext;
        }

        readonly PermissionManager _PermissionManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly IMemoryCache _Cache;
        readonly OwContext _OwContext;

        /// <summary>
        /// 缓存项的前缀。
        /// </summary>
        public const string CachePrefix = "Permissions.";

        /// <summary>
        /// 是否拥有指定的一组权限。对于超管，商管总是返回true。
        /// </summary>
        /// <param name="pIds"></param>
        /// <returns>拥有指定的所有权限则返回true,否则返回false。</returns>
        public bool Demand(params string[] pIds)
        {
            return Demand(out _, pIds);
        }

        /// <summary>
        /// 测试是否拥有指定的权限
        /// </summary>
        /// <param name="pIds"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool Demand(out string err, params string[] pIds)
        {
            err = null;
            var user = _OwContext.User;
            if (user.IsSuperAdmin) return true;
            if (user.IsMerchantAdmin)
            {
                return true;
            }
            var ci = _PermissionManager.GetOrLoadCurrentPermissionsByUser(user);
            var firstNo = pIds.FirstOrDefault(c => !ci.Data.ContainsKey(c));
            if (firstNo != null)
            {
                err = firstNo;
                return false;
            }
            return true;
        }
    }
}
