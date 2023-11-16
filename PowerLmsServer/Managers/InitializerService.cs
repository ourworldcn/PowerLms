using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <param name="npoiManager"></param>
        public InitializerService(ILogger<InitializerService> logger, IServiceScopeFactory serviceScopeFactory, NpoiManager npoiManager)
        {
            _Logger = logger;
            _ServiceScopeFactory = serviceScopeFactory;
            _NpoiManager = npoiManager;
        }

        ILogger<InitializerService> _Logger;
        PowerLmsUserDbContext _DbContext;
        IServiceScopeFactory _ServiceScopeFactory;
        NpoiManager _NpoiManager;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CreateDb();
            return Task.Run(() =>
            {
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

            var filePath = Path.Combine(AppContext.BaseDirectory, "系统资源", "系统资源.xlsx");
            using var file = File.OpenRead(filePath);

            using var workbook = _NpoiManager.GetWorkbookFromStream(file);
            var sheet = workbook.GetSheetAt(0);

            _NpoiManager.WriteToDb(sheet, _DbContext, _DbContext.SystemResources);


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
                    CurrentLanguageTag= "zh-CN",
                    LastModifyDateTimeUtc = OwHelper.WorldNow,
                };
                //admin.SetPwd("1D381427-86BB-4D88-8CB0-5D92F8E1BADF");
                _DbContext.Accounts.Add(admin);
                //_DbContext.SaveChanges();
            }
            admin.SetPwd("1D381427-86BB-4D88-8CB0-5D92F8E1BADF");
            _DbContext.SaveChanges();
        }

        private void Test()
        {
            string fileName = Path.ChangeExtension("sr", ".xls");
            var cul = new CultureInfo("zh-CN");
            var ss = cul.LCID;
        }

        private void CreateDb()
        {
            using var scope = _ServiceScopeFactory.CreateScope();
            var svc = scope.ServiceProvider;
            try
            {
                _DbContext = svc.GetRequiredService<PowerLmsUserDbContext>();
                MigrateDbInitializer.Initialize(_DbContext);
                _DbContext.SaveChanges();
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
