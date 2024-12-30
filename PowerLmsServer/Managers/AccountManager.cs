using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using NPOI.POIFS.FileSystem;
using NPOI.SS.Formula.Functions;
using OW;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        readonly IMapper _Mapper;
        readonly IMemoryCache _MemoryCache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;


        /// <summary>
        /// 将令牌转换为用户Key的字典的缓存项的key。
        /// </summary>
        const string Token2KeyCacheKey = $"097E641E-03D5-45CE-A911-B4A1C7D2392B.Token2Key";

        /// <summary>
        /// 令牌到缓存键的映射字典。
        /// 键是令牌，值对象Id的字符串。
        /// </summary>
        public ConcurrentDictionary<Guid, string> Token2KeyDic
        {
            get
            {
                return _MemoryCache.GetOrCreate(Token2KeyCacheKey, c =>
                {
                    return new ConcurrentDictionary<Guid, string>();
                });
            }
        }

        /// <summary>
        /// 按指定令牌获取Id。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="db">检索用的数据库上下文。可以是null，在必要时会自动获取一个新的上下文。</param>
        /// <returns>指定令牌对象的Id，如果没有找到则返回null。</returns>
        public Guid? GetOrLoadIdByToken(Guid token, ref PowerLmsUserDbContext db)
        {
            if (!Token2KeyDic.TryGetValue(token, out var key))  //若缓存中没有指定Token
            {
                db ??= _DbContextFactory.CreateDbContext();
                if (db.Accounts.FirstOrDefault(c => c.Token == token) is Account user)
                    return user.Id;
                else
                    return null;
            }
            else if (Guid.TryParse(key, out var id))
                return id;
            return null;

        }

        /// <summary>
        /// 锁定，避免出现临界争用错误。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public DisposeHelper<string> Lock(string key, TimeSpan timeout)
        {
            return DisposeHelper.Create(SingletonLocker.TryEnter, SingletonLocker.Exit, key, timeout);
        }

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
        public OwContext GetOrLoadContextByToken(Guid token, IServiceProvider scope)
        {
            var ci = GetOrLoadByToken(token);
            if (ci is null) goto lbErr;

            var context = scope.GetRequiredService<OwContext>();
            context.Token = token;
            context.User = ci.Data;
            return context;
        lbErr:
            OwHelper.SetLastError(315);
            return null;
        }

        #region 加载用户对象及相关

        #region 用令牌获取账号

        #endregion
        /// <summary>
        /// 在缓存中按指定令牌获取缓存项。不会试图读取数据库。
        /// </summary>
        /// <param name="token"></param>
        /// <returns>指定令牌的缓存项，没有找到则返回null。</returns>
        public OwCacheItem<Account> GetByToken(Guid token)
        {
            if (!Token2KeyDic.TryGetValue(token, out var key))  //若缓存中没有指定Token
            {
                return null;
            }
            if (!Guid.TryParse(key, out var id)) return null;
            return _MemoryCache.Get<OwCacheItem<Account>>(OwCacheHelper.GetCacheKeyFromId<Account>(id));
        }

        /// <summary>
        /// 加载用户对象。
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Account LoadByToken(Guid token)
        {
            var db = _DbContextFactory.CreateDbContext();
            if (db.Accounts.FirstOrDefault(c => c.Token == token) is not Account user)
                return null;
            Loaded(user, db);
            return user;
        }

        /// <summary>
        /// 按指定令牌获取用户缓存项
        /// </summary>
        /// <param name="token"></param>
        /// <returns>指定用户的缓存项，没找到则返回null。</returns>
        public OwCacheItem<Account> GetOrLoadByToken(Guid token)
        {
            PowerLmsUserDbContext db = null;
            var id = GetOrLoadIdByToken(token, ref db);
            using var dw = db;
            if (id is null) return null;
            return GetOrLoadCacheItemById(id.Value);
        }

        /// <summary>
        /// 按证据获取用户缓存或加载。
        /// </summary>
        /// <param name="evidence"></param>
        /// <returns></returns>
        public Account GetOrLoadAccountByEvidence(IDictionary<string, string> evidence)
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
        /// 按证据获取用户缓存或加载。
        /// </summary>
        /// <param name="evidence"></param>
        /// <returns></returns>
        public Account LoadByEvidence(IDictionary<string, string> evidence)
        {
            Account user = null;
            PowerLmsUserDbContext db;
            if (evidence.TryGetValue(nameof(Account.LoginName), out var loginName) && evidence.TryGetValue("Pwd", out var pwd))   //用户登录名登录
            {
                db = _DbContextFactory.CreateDbContext();
                user = db.Accounts.FirstOrDefault(c => c.LoginName == loginName);
                if (user is null) goto lbErr;
                if (!user.IsPwd(pwd)) goto lbErr;
            }
            else
                return null;
            Loaded(user, db);
            return user;
        lbErr:
            return null;
        }

        #region 用Id获取账号及相关

        /// <summary>
        /// 加载用户对象。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual Account LoadById(Guid id)
        {
            var db = _DbContextFactory.CreateDbContext();
            Account user;
            lock (db)
                if (db.Accounts.FirstOrDefault(c => c.Id == id) is not Account tmp)
                {
                    using var dw = db;
                    return null;
                }
                else
                    user = tmp;
            Loaded(user, db);
            return user;
        }

        /// <summary>
        /// 从缓存中获取用户对象。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>指定id的缓存项，没有找到则返回null。</returns>
        public virtual OwCacheItem<Account> GetCacheItemById(Guid id)
        {
            return _MemoryCache.Get<OwCacheItem<Account>>(OwCacheHelper.GetCacheKeyFromId<Account>(id));
        }

        /// <summary>
        /// 获取缓存中的用户对象或加载。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>返回用户对象，没有找到则返回null。</returns>
        public virtual OwCacheItem<Account> GetOrLoadCacheItemById(Guid id)
        {
            using var dw = Lock(id.ToString(), Timeout.InfiniteTimeSpan);

            var result = _MemoryCache.GetOrCreate(OwCacheHelper.GetCacheKeyFromId<Account>(id), entry =>
            {
                var user = LoadById(OwCacheHelper.GetIdFromCacheKey(entry.Key as string).Value);
                SetEntry(entry);
                var r = new OwCacheItem<Account> { Data = user };
                r.SetCancellations(new CancellationTokenSource());
                return r;
            });
            SetCacheItem(result);
            return result;
        }

        #endregion 用Id获取账号及相关

        /// <summary>
        /// 进入缓存项的设置。
        /// </summary>
        /// <param name="entry"></param>
        protected ICacheEntry SetEntry(ICacheEntry entry)
        {
            return entry.SetSlidingExpiration(TimeSpan.FromMinutes(15))   //15分钟不用则逐出
                .RegisterPostEvictionCallback((k, v, r, s) =>  //逐出后清理
                {
                    if (v is OwCacheItem<Account> ci)
                    {
                        var key = ci.Data.IdString;
                        //if (SingletonLocker.TryEnter(key, Timeout.InfiniteTimeSpan))
                        //{
                        //    try
                        //    {

                        //    }
                        //    finally
                        //    {
                        //        SingletonLocker.Exit(key);
                        //    }
                        //}
                        using var dw = ci.Data?.DbContext;
                        if (!ci.CancellationTokenSource.IsCancellationRequested)
                            ci.CancellationTokenSource.Cancel();
                        if (ci.Data.Token.HasValue) //若需要取消Token映射
                            Token2KeyDic.TryRemove(ci.Data.Token.Value, out _);
                    }
                });
        }

        /// <summary>
        /// 第一次写入了缓存项后调用。
        /// </summary>
        /// <param name="cacheItem"></param>
        protected void SetCacheItem(OwCacheItem<Account> cacheItem)
        {
            Token2KeyDic.AddOrUpdate(cacheItem.Data.Token.Value, cacheItem.Data.IdString, (key, ov) => cacheItem.Data.IdString);
        }

        /// <summary>
        /// 加载用户对象后调用。
        /// </summary>
        /// <param name="user"></param>
        /// <param name="db"></param>
        public void Loaded(Account user, PowerLmsUserDbContext db)
        {
            user.DbContext = db;
        }

        #endregion 加载用户对象及相关

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
        public IServiceProvider ServiceProvider { get; }

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
