using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    /// 初始化服务。
    /// </summary>
    public class InitializerService : BackgroundService
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serviceScopeFactory"></param>
        public InitializerService(ILogger<InitializerService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _Logger = logger;
            _ServiceScopeFactory = serviceScopeFactory;
        }

        ILogger<InitializerService> _Logger;
        PowerLmsUserDbContext _DbContext;
        IServiceScopeFactory _ServiceScopeFactory;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                CreateDb();
                CreateSystemResource();
                CreateAdmin();
                Test();
            });
        }

        /// <summary>
        /// 创建必要的系统资源。
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void CreateSystemResource()
        {
            using var scope = _ServiceScopeFactory.CreateScope();
            var svc = scope.ServiceProvider;
            _DbContext = svc.GetRequiredService<PowerLmsUserDbContext>();

            _DbContext.InsertOrUpdate(new SystemResource
            {
                Id = Guid.Parse("{BD7B4671-D11F-42FC-9818-8DA456BDA8BC}"),
                DisplayName = nameof(_DbContext.LanguageDataDics),
                Remark = "语言字典表",
            });

            _DbContext.InsertOrUpdate(new SystemResource
            {
                Id = Guid.Parse("{6AE3BBB3-BAC9-4509-BF82-C8578830CD24}"),
                DisplayName = nameof(_DbContext.Multilinguals),
                Remark = "多语言资源表",
            });


            _DbContext.SaveChanges();
        }

        /// <summary>
        /// 创建管理员。
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void CreateAdmin()
        {
            using var scope = _ServiceScopeFactory.CreateScope();
            var svc = scope.ServiceProvider;
            _DbContext = svc.GetRequiredService<PowerLmsUserDbContext>();
            var admin = _DbContext.Accounts.FirstOrDefault(c => c.LoginName == "868d61ae-3a86-42a8-8a8c-1ed6cfa90817");
            if (admin == null)  //若没有创建超管
            {
                admin = new Account
                {
                    LoginName = "868d61ae-3a86-42a8-8a8c-1ed6cfa90817",
                    LanguageTag = "zh-CN",
                    LastModifyDateTimeUtc = OwHelper.WorldNow,
                };
                admin.SetPwd("1D381427-86BB-4D88-8CB0-5D92F8E1BADF");
                _DbContext.Accounts.Add(admin);
                _DbContext.SaveChanges();
            }
        }

        private void Test()
        {
            var str = "sds";
            var b = str.StartsWith("");
        }

        private void CreateDb()
        {
            using var scope = _ServiceScopeFactory.CreateScope();
            var svc = scope.ServiceProvider;
            try
            {
                _DbContext = svc.GetRequiredService<PowerLmsUserDbContext>();
                MigrateDbInitializer.Initialize(_DbContext);
                _Logger.LogTrace("用户数据库已正常升级。");

                //var loggingDb = services.GetService<GameLoggingDbContext>();
                //if (loggingDb != null)
                //{
                //    GameLoggingMigrateDbInitializer.Initialize(loggingDb);
                //    logger.LogTrace("日志数据库已正常升级。");
                //}
            }
            catch (Exception err)
            {
                _Logger.LogError(err, "升级数据库出现错误。");
            }

        }
    }
}
