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
        /// <param name="pIds">权限ID数组</param>
        /// <returns>拥有指定的所有权限则返回true,否则返回false。</returns>
        public bool Demand(params string[] pIds)
        {
            return Demand(out _, pIds);
        }
        /// <summary>
        /// 测试是否拥有指定的权限
        /// </summary>
        /// <param name="pIds">权限ID数组</param>
        /// <param name="err">错误信息输出</param>
        /// <returns>拥有所有指定权限返回true，否则返回false</returns>
        public bool Demand(out string err, params string[] pIds)
        {
            err = null;
            var user = _OwContext.User;
            // 超级管理员和商户管理员默认拥有所有权限
            if (user.IsSuperAdmin || user.IsMerchantAdmin)
            {
                return true;
            }
            // 获取用户当前的权限集合
            // 修复：使用正确的方法名称 GetOrLoadUserCurrentPermissions
            var permissions = _PermissionManager.GetOrLoadUserCurrentPermissions(user);
            // 检查用户是否拥有所有指定的权限
            foreach (var permissionId in pIds)
            {
                if (!permissions.ContainsKey(permissionId))
                {
                    // 尝试获取权限的详细信息来提供更有用的错误消息
                    var allPermissions = _PermissionManager.GetOrLoadPermissions();
                    if (allPermissions.TryGetValue(permissionId, out var permission))
                    {
                        err = $"缺少权限：{permission.Name}({permission.DisplayName})";
                    }
                    else
                    {
                        err = $"试图断言一个不存在的权限项：{permissionId}";
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
