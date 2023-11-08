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
    /// 账号管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class AccountManager
    {
        /// <summary>
        /// 获取上线文对象。暂时不考虑缓存。
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="token"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool GetAccountFromToken(IServiceProvider scope, Guid token, out Account account)
        {
            var db = scope.GetService<PowerLmsUserDbContext>();
            var user = db.Accounts.FirstOrDefault(c => c.Token == token);
            if (user is null) goto lbErr;
            account = user;
            var context = scope.GetRequiredService<OwContext>();
            context.Token = token;
            context.User = user;
            return true;
        lbErr:
            account = null;
            return false;
        }
    }

    /// <summary>
    /// 与Token生存期对应的上下文。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OwContext
    {
        /// <summary>
        /// 令牌。
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// 账号对象。
        /// </summary>
        public Account User { get; set; }

        //其它缓存数据
    }
}
