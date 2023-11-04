using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    public class InitializerService : BackgroundService
    {
        public InitializerService(ILogger<InitializerService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _Logger = logger;
            _ServiceScopeFactory = serviceScopeFactory;
        }

        ILogger<InitializerService> _Logger;
        PowerLmsUserDbContext _DbContext;
        IServiceScopeFactory _ServiceScopeFactory;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                CreateDb();
            });
        }

        private void CreateDb()
        {
            using var scope = _ServiceScopeFactory.CreateScope();
            var svc= scope.ServiceProvider;
            try
            {
                _DbContext=svc.GetRequiredService<PowerLmsUserDbContext>();
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
