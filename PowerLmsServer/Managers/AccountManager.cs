using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        public AccountManager(PasswordGenerator passwordGenerator, IMapper mapper, IMemoryCache memoryCache, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, IHostApplicationLifetime applicationLifetime)
        {
            _PasswordGenerator = passwordGenerator;
            _Mapper = mapper;
            _MemoryCache = memoryCache;
            _DbContextFactory = dbContextFactory;
            _ApplicationLifetime = applicationLifetime;
            TaskDispatcher = new TaskDispatcher(new TaskDispatcherOptions { CancellationToken = _ApplicationLifetime.ApplicationStopped });
        }

        readonly PasswordGenerator _PasswordGenerator;
        readonly IMapper _Mapper;
        readonly IMemoryCache _MemoryCache;
        readonly IDbContextFactory<PowerLmsUserDbContext> _DbContextFactory;
        IHostApplicationLifetime _ApplicationLifetime;

        /// <summary>
        /// 将令牌转换为用户Key的字典的缓存项的key。
        /// </summary>
        const string Token2KeyCacheKey = $"097E641E-03D5-45CE-A911-B4A1C7D2392B.Token2Key";

        /// <summary>
        /// TaskDispatcher 实例。延迟初始化。
        /// </summary>
        private TaskDispatcher TaskDispatcher;

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
        /// <param name="loginName">登录名</param>
        /// <param name="pwd">密码，为空时会自动生成</param>
        /// <param name="id">返回创建的账号ID</param>
        /// <param name="service">当前范围的服务容器。</param>
        /// <param name="template">账号模板</param>
        /// <returns>创建是否成功</returns>
        public bool CreateNew(string loginName, ref string pwd, out Guid id, IServiceProvider service, Account template)
        {
            // 参数验证：检查service是否为空
            if (service == null)
            {
                OwHelper.SetLastErrorAndMessage(400, "服务提供者不能为空");
                id = Guid.Empty;
                return false;
            }

            // 参数验证：检查loginName是否为空
            if (string.IsNullOrEmpty(loginName))
            {
                OwHelper.SetLastErrorAndMessage(400, "登录名不能为空");
                id = Guid.Empty;
                return false;
            }

            // 参数验证：检查template是否为空
            if (template == null)
            {
                OwHelper.SetLastErrorAndMessage(400, "账户模板不能为空");
                id = Guid.Empty;
                return false;
            }

            try
            {
                // 获取数据库上下文
                var db = service.GetRequiredService<PowerLmsUserDbContext>();

                // 检查登录名是否重复 - 修改为409错误并指出字段名
                if (db.Accounts.Any(c => c.LoginName == loginName))
                {
                    OwHelper.SetLastErrorAndMessage(409, nameof(Account.LoginName)); // 返回409错误码，消息为字段名
                    id = Guid.Empty;
                    return false;
                }

                if (string.IsNullOrEmpty(pwd)) pwd = _PasswordGenerator.Generate(6); // 若需要生成密码

                var user = _Mapper.Map<Account>(template);
                user.GenerateNewId();
                user.SetPwd(pwd);
                user.State = template.State; // 确保状态值被正确复制
                user.LastModifyDateTimeUtc = OwHelper.WorldNow; // 设置最后修改时间
                id = user.Id;
                db.Add(user);
                db.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("LoginName") == true)
            {
                // 捕获数据库级别的唯一约束违反异常，以防止并发情况下重复检查失效
                OwHelper.SetLastErrorAndMessage(409, nameof(Account.LoginName));
                id = Guid.Empty;
                return false;
            }
            catch (Exception ex)
            {
                // 异常处理：捕获并记录可能的异常
                OwHelper.SetLastErrorAndMessage(500, $"创建账户失败: {ex.Message}");
                id = Guid.Empty;
                return false;
            }
        }

        /// <summary>
        /// 获取缓存上下文。当前版本未实现缓存，未来将使用缓存加速。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="scope">范围服务容器。</param>
        /// <returns>上下文对象，可能是null如果出错。</returns>
        public OwContext GetOrLoadContextByToken(Guid token, IServiceProvider scope)
        {
            var account = GetOrLoadByToken(token);
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
        public Account GetByToken(Guid token)
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
        public Account GetOrLoadByToken(Guid token)
        {
            PowerLmsUserDbContext db = null;
            var id = GetOrLoadIdByToken(token, ref db);
            using var dw = db;
            if (id is null) return null;
            return GetOrLoadById(id.Value);
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
        public virtual Account GetOrLoadById(Guid id)
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
                    dbContext?.SaveChanges();

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
            if (_MemoryCache.CancelSource(cacheKey))
            {
                return true;
            }

            // 如果没有找到对应的取消令牌源，直接尝试从缓存中移除
            // 注意：正常情况下不应该走到这里，因为所有缓存条目都应该有关联的取消令牌
            if (_MemoryCache.TryGetValue(cacheKey, out _))
            {
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

        /// <summary>
        /// 更新用户令牌并确保相关缓存和映射正确更新。
        /// 在用户登录或令牌刷新时调用此方法。
        /// </summary>
        /// <param name="userId">要更新令牌的账号ID</param>
        /// <param name="newToken">新令牌，若为null则自动生成</param>
        /// <returns>新的令牌值，如果用户不存在则返回null</returns>
        public Guid? UpdateToken(Guid userId, Guid? newToken = null)
        {
            // 使用锁以确保原子性操作
            using var dw = Lock(userId.ToString(), Timeout.InfiniteTimeSpan);

            // 获取账号（这会自动处理缓存和数据库加载）
            var account = GetOrLoadById(userId);
            if (account == null) return null;

            // 记录旧令牌，以便从映射中移除
            var oldToken = account.Token;

            // 设置新令牌和更新时间
            account.Token = newToken ?? Guid.NewGuid();
            account.LastModifyDateTimeUtc = OwHelper.WorldNow;

            // 如果有旧令牌，从映射中移除
            if (oldToken.HasValue)
            {
                Token2KeyDic.TryRemove(oldToken.Value, out _);
            }

            // 添加新的令牌映射
            // 注意：这里直接添加映射而不依赖缓存项的回调
            Token2KeyDic[account.Token.Value] = account.IdString;
            SaveAccount(userId);
            return account.Token;
        }

        /// <summary>
        /// 将账号的更改保存到数据库。
        /// 该方法使用TaskDispatcher排队保存任务，确保对同一账号的操作按序执行。
        /// </summary>
        /// <param name="userId">要保存的账号ID</param>
        /// <returns>是否成功保存</returns>
        public bool SaveAccount(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                OwHelper.SetLastErrorAndMessage(400, "账号ID不能为空。");
                return false;
            }
            // 获取账号对象
            var account = GetAccountById(userId);
            if (account == null)
            {
                OwHelper.SetLastErrorAndMessage(404, "未找到指定的账号。");
                return false;
            }


            // 使用TaskDispatcher排队保存任务
            return TaskDispatcher.Enqueue(
                account.IdString,      // 任务类型标识
                parameter =>                  // 执行任务的函数
                {
                    if (parameter is not Account user)   //忽略不是Account类型的参数
                        return true;
                    try
                    {
                        // 保存更改
                        user.DbContext?.SaveChanges();
                        return true;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // 处理并发异常
                        OwHelper.SetLastErrorAndMessage(409, "保存账号时发生并发冲突。");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        // 记录异常
                        OwHelper.SetLastErrorAndMessage(500, $"保存账号时发生错误：{ex.Message}");
                        return false;
                    }
                },
                account,       // 任务参数
                true          // 需要锁定
            );
        }
    }
}
