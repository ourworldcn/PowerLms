﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OW;
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
        /// 构造函数。
        /// </summary>
        /// <param name="passwordGenerator"></param>
        /// <param name="mapper"></param>
        public AccountManager(PasswordGenerator passwordGenerator, IMapper mapper)
        {
            _PasswordGenerator = passwordGenerator;
            _Mapper = mapper;
        }

        readonly PasswordGenerator _PasswordGenerator;
        IMapper _Mapper;

        /// <summary>
        /// 创建一个新账号。
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="id"></param>
        /// <param name="service">当前范围的服务容器。</param>
        /// <param name="template"></param>
        public bool CreateNew(string loginName, ref string pwd, out Guid id, IServiceProvider service, Account template)
        {
            var db = service.GetRequiredService<PowerLmsUserDbContext>();
            if (db.Accounts.Any(c => c.LoginName == loginName))
            {
                OwHelper.SetLastErrorAndMessage(400, "登录名重复");
                id = Guid.Empty;
                return false;
            }
            if (string.IsNullOrEmpty(pwd)) pwd = _PasswordGenerator.Generate(6);    //若需要生成密码

            var user = _Mapper.Map<Account>(template);
            user.GenerateNewId();
            user.SetPwd(pwd);
            id = user.Id;
            db.Add(user);
            db.SaveChanges();
            return true;
        }

        /// <summary>
        /// 获取缓存上下文。当前版本未实现缓存，未来将使用缓存加速。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="scope">范围服务容器。</param>
        /// <returns>上下文对象，可能是null如果出错。</returns>
        public OwContext GetAccountFromToken(Guid token, IServiceProvider scope)
        {
            var db = scope.GetService<PowerLmsUserDbContext>();
            var user = db.Accounts.FirstOrDefault(c => c.Token == token);
            if (user is null) goto lbErr;
            var context = scope.GetRequiredService<OwContext>();
            context.Token = token;
            context.User = user;
            return context;
        lbErr:
            OwHelper.SetLastError(315);
            return null;
        }

        /// <summary>
        /// 是否是超管。
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsAdmin(Account user)
        {
            return (user.State & 4) != 0;
        }

        /// <summary>
        /// 是否是商管。
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsMerchantAdmin(Account user)
        {
            return (user.State & 8) != 0;
        }
    }

    /// <summary>
    /// 与Token生存期对应的上下文。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OwContext : OwDisposableBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="serviceProvider"></param>
        public OwContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 令牌。
        /// </summary>
        public Guid Token { get; set; }

        /// <summary>
        /// 账号对象。
        /// </summary>
        public Account User { get; set; }

        /// <summary>
        /// 这次工作上下文的创建时间。
        /// </summary>
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;

        /// <summary>
        /// 当前使用的范围服务容器。
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }

        #region 方法

        /// <summary>
        /// 标记当前进行了一次有效操作，这将导致延迟清理时间。
        /// </summary>
        public void Nop()
        {
            User.LastModifyDateTimeUtc = OwHelper.WorldNow;
        }

        /// <summary>
        /// 保存变化。
        /// </summary>
        /// <returns></returns>
        public int SaveChanges()
        {
            int result = ServiceProvider.GetRequiredService<PowerLmsUserDbContext>().SaveChanges();
            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        #endregion 方法
    }
}
