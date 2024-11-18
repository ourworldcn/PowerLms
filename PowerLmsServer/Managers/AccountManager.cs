using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NPOI.POIFS.FileSystem;
using OW;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

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
        /// <param name="memoryCache"></param>
        /// <param name="dbContextFactory"></param>
        public AccountManager(PasswordGenerator passwordGenerator, IMapper mapper, IMemoryCache memoryCache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory)
        {
            _PasswordGenerator = passwordGenerator;
            _Mapper = mapper;
            _MemoryCache = memoryCache;
            _DbContextFactory = dbContextFactory;
        }

        readonly PasswordGenerator _PasswordGenerator;
        IMapper _Mapper;
        IMemoryCache _MemoryCache;
        IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;


        ConcurrentDictionary<Guid, string> _Token2Key = new ConcurrentDictionary<Guid, string> { };

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
        public OwContext GetOrLoadAccountFromToken(Guid token, IServiceProvider scope)
        {
            Account user;
            if (_Token2Key.TryGetValue(token, out var key))    //若找到token
                user = GetOrLoadAccount(Guid.Parse(key));
            else
            {
                using var db = scope.GetService<PowerLmsUserDbContext>();
                user = db.Accounts.FirstOrDefault(c => c.Token == token);
                if (user is null) goto lbErr;
                user = GetOrLoadAccount(user.Id);
                user.ExpirationTokenSource.Token.Register(c =>
                {
                    var u = c as Account;
                    _Token2Key.TryRemove(u.Token.Value, out _);
                }, user);
            }
            var context = scope.GetRequiredService<OwContext>();
            context.Token = token;
            context.User = user;
            _Token2Key[token] = user.Id.ToString();
            return context;
        lbErr:
            OwHelper.SetLastError(315);
            return null;
        }

        /// <summary>
        /// 按证据获取用户缓存或加载。
        /// </summary>
        /// <param name="evidence"></param>
        /// <returns></returns>
        public Account GetOrLoadAccountFromEvidence(IDictionary<string, string> evidence)
        {
            Account user = null;
            if (evidence.TryGetValue(nameof(Account.LoginName), out var loginName) && evidence.TryGetValue("Pwd", out var pwd))   //用户登录名登录
            {
                using var db = _DbContextFactory.CreateDbContext();
                user = db.Accounts.FirstOrDefault(c => c.LoginName == loginName);
                if (user is null) goto lbErr;
                if (!user.IsPwd(pwd)) goto lbErr;
            }
            return user;
        lbErr:
            return null;
        }

        /// <summary>
        /// 获取用户对象的键。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetKey(Guid id)
        {
            return $"Account.{id}";
        }

        /// <summary>
        /// 获取缓存中的用户对象或加载。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>返回用户对象，没有找到则返回null。</returns>
        public Account GetOrLoadAccount(Guid id)
        {
            var result = _MemoryCache.GetOrCreate(GetKey(id), entry =>
            {
                var db = _DbContextFactory.CreateDbContext();
                if (db.Accounts.FirstOrDefault(c => c.Id == Guid.Parse(entry.Key as string)) is not Account user)
                    return null;
                user.DbContext = db;
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));
                var ct = new CancellationTokenSource();
                user.ExpirationTokenSource = ct;
                entry.AddExpirationToken(new CancellationChangeToken(ct.Token));

                return user;
            });
            return result;
        }

        /// <summary>
        /// 设置缓存中的数据(新缓存项)。若有已有项不会自动调用过期事件。
        /// </summary>
        /// <param name="user"></param>
        public void SetAccount(Account user)
        {
            var key = GetKey(user.Id);

            var ct = user.ExpirationTokenSource;
            ct.Token.Register(c =>
            {
                var u = c as Account;
                u.DbContext?.Dispose();
            }, user);
            var entryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(15))
                .AddExpirationToken(new CancellationChangeToken(ct.Token));

            _MemoryCache.Set(key, user, entryOptions);
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
