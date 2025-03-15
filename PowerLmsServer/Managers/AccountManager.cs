using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using OW;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
            if (!Token2KeyDic.TryGetValue(token, out var key)) // 若缓存中没有指定Token
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
            if (string.IsNullOrEmpty(pwd)) pwd = _PasswordGenerator.Generate(6); // 若需要生成密码

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
            var account = GetOrLoadAccountByToken(token);
            if (account is null) goto lbErr;

            var context = scope.GetRequiredService<OwContext>();
            context.Token = token;
            context.User = account;
            return context;
        lbErr:
            OwHelper.SetLastError(315);
            return null;
        }

        #region 加载用户对象及相关

        /// <summary>
        /// 在缓存中按指定令牌获取账号。不会试图读取数据库。
        /// </summary>
        /// <param name="token"></param>
        /// <returns>指定令牌的账号，没有找到则返回null。</returns>
        public Account GetAccountByToken(Guid token)
        {
            if (!Token2KeyDic.TryGetValue(token, out var key)) // 若缓存中没有指定Token
            {
                return null;
            }
            if (!Guid.TryParse(key, out var id)) return null;
            return GetAccountById(id);
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
        /// 按指定令牌获取用户
        /// </summary>
        /// <param name="token"></param>
        /// <returns>指定用户，没找到则返回null。</returns>
        public Account GetOrLoadAccountByToken(Guid token)
        {
            PowerLmsUserDbContext db = null;
            var id = GetOrLoadIdByToken(token, ref db);
            using var dw = db;
            if (id is null) return null;
            return GetOrLoadAccountById(id.Value);
        }

        /// <summary>
        /// 按证据获取用户或加载。
        /// </summary>
        /// <param name="evidence"></param>
        /// <returns></returns>
        public Account GetOrLoadAccountByEvidence(IDictionary<string, string> evidence)
        {
            Account user = null;
            if (evidence.TryGetValue(nameof(Account.LoginName), out var loginName) && evidence.TryGetValue("Pwd", out var pwd)) // 用户登录名登录
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
        /// 按证据加载用户。
        /// </summary>
        /// <param name="evidence"></param>
        /// <returns></returns>
        public Account LoadByEvidence(IDictionary<string, string> evidence)
        {
            Account user = null;
            PowerLmsUserDbContext db;
            if (evidence.TryGetValue(nameof(Account.LoginName), out var loginName) && evidence.TryGetValue("Pwd", out var pwd)) // 用户登录名登录
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
        /// <returns>指定id的账号，没有找到则返回null。</returns>
        public virtual Account GetAccountById(Guid id)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<Account>(id);
            return _MemoryCache.Get<Account>(cacheKey);
        }

        /// <summary>
        /// 获取缓存中的用户对象或加载。
        /// </summary>
        /// <param name="id"></param>
        /// <returns>返回用户对象，没有找到则返回null。</returns>
        public virtual Account GetOrLoadAccountById(Guid id)
        {
            using var dw = Lock(id.ToString(), Timeout.InfiniteTimeSpan);

            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<Account>(id);

            return _MemoryCache.GetOrCreate(cacheKey, entry =>
            {
                var user = LoadById(OwMemoryCacheExtensions.GetIdFromCacheKey(entry.Key as string) ?? id);
                if (user == null) return null;

                // 配置缓存项
                ConfigureCacheEntry(entry, user);

                return user;
            });
        }

        /// <summary>
        /// 配置缓存条目属性
        /// </summary>
        /// <param name="entry">缓存条目</param>
        /// <param name="account">账号对象</param>
        private void ConfigureCacheEntry(ICacheEntry entry, Account account)
        {
            // 设置滑动过期时间
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(15));

            // 创建取消令牌源并注册到缓存中
            // 使用OwMemoryCacheExtensions的RegisterCancellationToken方法
            // 此方法会自动处理令牌源的生命周期和回调
            entry.RegisterCancellationToken(_MemoryCache);

            // 注册逐出回调处理Token映射和资源释放
            entry.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (value is Account acc && acc?.Token.HasValue == true)
                {
                    // 释放数据库上下文
                    using var dbContext = acc?.DbContext;

                    // 从Token映射字典中移除
                    Token2KeyDic.TryRemove(acc.Token.Value, out _);
                }
            });

            // 更新Token到Id的映射
            if (account?.Token.HasValue == true)
            {
                Token2KeyDic.AddOrUpdate(account.Token.Value, account.IdString, (_, _) => account.IdString);
            }
        }

        #endregion 用Id获取账号及相关

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
        /// <summary>
        /// 使指定用户Id的缓存失效。
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateUserCache(Guid userId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<Account>(userId);

            // 使用OwMemoryCacheExtensions的CancelSource方法触发取消令牌
            // 这将自动通过已注册的回调处理资源释放和缓存移除
            bool cancelled = _MemoryCache.CancelSource(cacheKey);

            if (cancelled)
            {
                // 记录日志或执行后续操作（如果需要）
                return true;
            }

            // 如果没有找到对应的取消令牌源，直接尝试从缓存中移除
            if (_MemoryCache.TryGetValue<Account>(cacheKey, out var account))
            {
                if (account?.Token.HasValue == true)
                {
                    Token2KeyDic.TryRemove(account.Token.Value, out _);
                }

                // 释放数据库上下文
                using var dbContext = account?.DbContext;

                // 直接从缓存中移除
                _MemoryCache.Remove(cacheKey);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 根据令牌使用户缓存失效。
        /// </summary>
        /// <param name="token">用户令牌</param>
        /// <returns>如果成功使缓存失效则返回true，否则返回false</returns>
        public bool InvalidateUserCacheByToken(Guid token)
        {
            if (Token2KeyDic.TryGetValue(token, out var key) && Guid.TryParse(key, out var userId))
            {
                return InvalidateUserCache(userId);
            }
            return false;
        }

        /// <summary>
        /// 获取用户缓存的取消令牌源。
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <returns>取消令牌源，如果不存在则返回null</returns>
        public CancellationTokenSource GetUserCacheTokenSource(Guid userId)
        {
            string cacheKey = OwMemoryCacheExtensions.GetCacheKeyFromId<Account>(userId);
            return _MemoryCache.GetCancellationTokenSource(cacheKey);
        }
    }
}
