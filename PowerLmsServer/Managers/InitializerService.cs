using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NPOI.SS.UserModel;
using OwDbBase;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using OW.Data;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Primitives;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using NPOI.SS.Formula.Functions;
using OW.EntityFrameworkCore;
using System.Reflection.Emit;

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
        /// <param name="serviceProvider"></param>
        public InitializerService(ILogger<InitializerService> logger, IServiceScopeFactory serviceScopeFactory, NpoiManager npoiManager, IServiceProvider serviceProvider) : base()
        {
            _Logger = logger;
            _ServiceScopeFactory = serviceScopeFactory;
            _NpoiManager = npoiManager;
            _ServiceProvider = serviceProvider;
        }

        readonly ILogger<InitializerService> _Logger;
        readonly IServiceScopeFactory _ServiceScopeFactory;
        readonly NpoiManager _NpoiManager;
        IServiceProvider _ServiceProvider;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            CreateDb();
            var task = Task.Run(() =>
            {
                using var scope = _ServiceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider;
                CreateSystemResource(svc);
                InitializeDataDic(svc);
                CreateAdmin(svc);
                SeedData(svc);
                Test(svc);
            }, CancellationToken.None);
            _Logger.LogInformation("Plms服务成功上线");
            return task;
        }

        /// <summary>
        /// 生成种子数据。
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [Conditional("DEBUG")]
        private void SeedData(IServiceProvider svc)
        {
            var db = svc.GetService<PowerLmsUserDbContext>();
            var merch = new PlMerchant
            {
                Id = Guid.Parse("{073E65D6-EA0F-4D13-9510-3973F5A47526}"),
                Name = new PlOwnedName { DisplayName = "种子商户", Name = "种子商户", },
            };
            db.AddOrUpdate(merch);
            var org = new PlOrganization
            {
                Id = Guid.Parse("{FB069576-3E3D-46DF-9F13-B7D5FBA84717}"),
                Name = new PlOwnedName() { DisplayName = "种子机构" },
                MerchantId = Guid.Parse("{073E65D6-EA0F-4D13-9510-3973F5A47526}"),
                Otc = 2,
            };
            db.AddOrUpdate(org);

            var user = new Account
            {
                Id = Guid.Parse("{61810FEA-7CE1-4458-BD2E-436BD22C894E}"),
                LoginName = "SeedUser",
                DisplayName = "种子用户",
                OrgId = org.Id,
                Token = Guid.Parse("{7B823D05-F7CD-4A0C-9EA8-5D2D8CA630EB}"),
            };
            user.SetPwd("!@#$");
            db.AddOrUpdate(user);

            var role = new PlRole
            {
                Id = Guid.Parse("{310319E1-39EE-4140-8100-1E598113E1FE}"),
                Name = new PlOwnedName() { DisplayName = "种子角色" },
                OrgId = org.Id,
            };
            db.AddOrUpdate(role);

            if (db.AccountPlOrganizations.FirstOrDefault(c => c.UserId == user.Id && c.OrgId == org.Id) is not AccountPlOrganization accountOrg)
            {
                accountOrg = new AccountPlOrganization { OrgId = org.Id, UserId = user.Id };
                db.Add(accountOrg);
            }
            if (db.PlAccountRoles.FirstOrDefault(c => c.UserId == user.Id && c.RoleId == role.Id) is not AccountRole accountRole)
            {
                accountRole = new AccountRole { RoleId = role.Id, UserId = user.Id };
                db.Add(accountRole);
            }
            if (db.PlRolePermissions.FirstOrDefault(c => c.PermissionId == "D0.1.1.10" && c.RoleId == role.Id) is not RolePermission rolePermission)
            {
                rolePermission = new RolePermission { RoleId = role.Id, PermissionId = "D0.1.1.10" };
                db.Add(rolePermission);
            }
            //费用结算单
            var inv = new PlInvoices
            {
                Id = Guid.Parse("{AAE637AE-88B9-45F6-8925-4A9EF1B75F88}")
            };
            var tmp = db.PlInvoicess.Find(inv.Id);
            if (tmp != null)
            {
                tmp.Currency = "CNY";
                var ss = db.PlInvoicess.Where(c => c.Id == inv.Id).FirstOrDefault();
                db.AddOrUpdate(inv);
                db.AddOrUpdate(new PlInvoicesItem
                {
                    Id = Guid.Parse("{916FD192-EE2A-4557-BFA1-C66A91C74118}"),
                    ParentId = inv.Id,
                });
                db.AddOrUpdate(new PlInvoicesItem
                {
                    Id = Guid.Parse("{AD6339C7-015E-482F-A8A1-29BB9E595750}"),
                    ParentId = inv.Id,
                });
            }
            #region 税务发票通道初始数据
            db.AddOrUpdate(
                new TaxInvoiceChannel
                {
                    Id = typeof(NuoNuoManager).GUID,
                    DisplayName = "诺诺发票",
                    InvoiceChannel = nameof(NuoNuoManager),
                    InvoiceChannelParams = "{}",
                });
            db.AddOrUpdate(
                new TaxInvoiceChannel
                {
                    Id = typeof(ManualInvoicingManager).GUID,
                    DisplayName = "手工开票",
                    InvoiceChannel = nameof(ManualInvoicingManager),
                    InvoiceChannelParams = "{}",
                }
            );
            #endregion 税务发票通道初始数据

            db.SaveChanges();
        }

        /// <summary>
        /// 创建必要的系统资源。
        /// </summary>
        /// <param name="svc"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CreateSystemResource(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();

            var filePath = Path.Combine(AppContext.BaseDirectory, "系统资源", "系统资源.xlsx");
            using var file = File.OpenRead(filePath);

            using var workbook = _NpoiManager.GetWorkbookFromStream(file);
            var sheet = workbook.GetSheetAt(0);

            _NpoiManager.WriteToDb(sheet, db, db.DD_SystemResources);


            db.SaveChanges();
        }

        /// <summary>
        /// 初始化数据字典。
        /// </summary>
        /// <param name="svc">范围性服务容器</param>
        private void InitializeDataDic(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var filePath = Path.Combine(AppContext.BaseDirectory, "系统资源", "预初始化数据字典.xlsx");
            using var file = File.OpenRead(filePath);
            using var workbook = _NpoiManager.GetWorkbookFromStream(file);

            db.TruncateTable("PlPermissions");
            var sheet = workbook.GetSheet(nameof(db.PlPermissions));
            _NpoiManager.WriteToDb(sheet, db, db.PlPermissions);

            using var scope = _ServiceScopeFactory.CreateScope();
            var mng = scope.ServiceProvider.GetService<AuthorizationManager>();

            //var sheet = workbook.GetSheet(nameof(db.DD_DataDicCatalogs));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_DataDicCatalogs);

            //sheet = workbook.GetSheet(nameof(db.DD_SimpleDataDics));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_SimpleDataDics);

            //sheet = workbook.GetSheet(nameof(db.DD_BusinessTypeDataDics));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_BusinessTypeDataDics);

            //sheet = workbook.GetSheet(nameof(db.DD_PlPorts));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_PlPorts);

            //sheet = workbook.GetSheet(nameof(db.DD_PlCargoRoutes));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_PlCargoRoutes);

            //sheet = workbook.GetSheet(nameof(db.DD_PlCountrys));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_PlCountrys);

            //sheet = workbook.GetSheet(nameof(db.DD_PlCurrencys));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_PlCurrencys);

            //sheet = workbook.GetSheet(nameof(db.DD_JobNumberRules));
            //_NpoiManager.WriteToDb(sheet, db, db.DD_JobNumberRules);

            // 保存所有更改
            db.SaveChanges();
        }

        /// <summary>
        /// 创建管理员。
        /// </summary>
        /// <param name="svc"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CreateAdmin(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var admin = db.Accounts.FirstOrDefault(c => c.LoginName == "868d61ae-3a86-42a8-8a8c-1ed6cfa90817");
            if (admin == null)  //若没有创建超管
            {
                admin = new Account
                {
                    LoginName = "868d61ae-3a86-42a8-8a8c-1ed6cfa90817",
                    CurrentLanguageTag = "zh-CN",
                    LastModifyDateTimeUtc = OwHelper.WorldNow,
                    State = 4,
                };
                //admin.SetPwd("1D381427-86BB-4D88-8CB0-5D92F8E1BADF");
                db.Accounts.Add(admin);
                //_DbContext.SaveChanges();
            }
            else
                admin.State = 4;
            admin.SetPwd("1D381427-86BB-4D88-8CB0-5D92F8E1BADF");
            db.SaveChanges();
        }

        [Conditional("DEBUG")]
        private void Test(IServiceProvider svc)
        {
            var nn = _ServiceProvider.GetRequiredService<NuoNuoManager>();
            nn.IssueInvoice(Guid.Parse("29DC0DA1-C1AB-4EB1-98D8-6B7F5339381E"));
        }

        private void CreateDb()
        {
            using var scope = _ServiceScopeFactory.CreateScope();
            var svc = scope.ServiceProvider;
            try
            {
                var db = svc.GetRequiredService<PowerLmsUserDbContext>();
                MigrateDbInitializer.Initialize(db);
                db.SaveChanges();
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
