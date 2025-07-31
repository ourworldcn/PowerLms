using AutoMapper;
using DotNetDBF;
using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using OW;
using OW.Data;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 初始化服务。
    /// </summary>
    public partial class InitializerService : BackgroundService
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

        /// <summary>
        /// 超级管理员登录名。
        /// </summary>
        private const string SuperAdminLoginName = "868d61ae-3a86-42a8-8a8c-1ed6cfa90817";
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
            var task = Task.Run(() =>
            {
                using var scope = _ServiceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider;
                InitDb(svc);
                CreateSystemResource(svc);
                InitializeDataDic(svc);
                CreateAdmin(svc);
                SeedData(svc);
                CleanupInvalidRelationships(svc);
                Test(svc);
            }, CancellationToken.None);
            _Logger.LogInformation("Plms服务成功上线");
            return task;
        }

        /// <summary>
        /// 清理无效的用户-角色、角色-权限、用户-机构关系数据
        /// </summary>
        /// <param name="svc">服务提供者</param>
        private void CleanupInvalidRelationships(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _Logger.LogInformation("开始清理无效的关联关系数据...");

            try
            {
                // 使用EF Core删除无效的用户-角色关系
                var invalidUserRoles = db.PlAccountRoles
                    .Where(ur => !db.Accounts.Any(u => u.Id == ur.UserId) ||
                                 !db.PlRoles.Any(r => r.Id == ur.RoleId))
                    .ToList();

                if (invalidUserRoles.Count > 0)
                {
                    db.PlAccountRoles.RemoveRange(invalidUserRoles);
                    _Logger.LogInformation("准备清理 {count} 条无效的用户-角色关系", invalidUserRoles.Count);
                }

                // 使用EF Core删除无效的角色-权限关系
                var invalidRolePermissions = db.PlRolePermissions
                    .Where(rp => !db.PlRoles.Any(r => r.Id == rp.RoleId) ||
                                 !db.PlPermissions.Any(p => p.Name == rp.PermissionId))
                    .ToList();

                if (invalidRolePermissions.Count > 0)
                {
                    db.PlRolePermissions.RemoveRange(invalidRolePermissions);
                    _Logger.LogInformation("准备清理 {count} 条无效的角色-权限关系", invalidRolePermissions.Count);
                }

                // 使用EF Core删除无效的用户-机构关系
                var invalidUserOrgs = db.AccountPlOrganizations
                    .Where(uo => !db.Accounts.Any(u => u.Id == uo.UserId) ||
                                (!db.PlOrganizations.Any(o => o.Id == uo.OrgId) && !db.Merchants.Any(c => c.Id == uo.OrgId)))
                    .ToList();

                if (invalidUserOrgs.Count > 0)
                {
                    db.AccountPlOrganizations.RemoveRange(invalidUserOrgs);
                    _Logger.LogInformation("准备清理 {count} 条无效的用户-机构关系", invalidUserOrgs.Count);
                }

                // 保存所有更改
                var totalRemoved = db.SaveChanges();

                stopwatch.Stop();
                _Logger.LogInformation("关系清理完成，共删除 {total} 条无效数据，耗时: {elapsed}ms",
                    totalRemoved, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "清理无效关联关系时发生错误");
            }
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
                Name_DisplayName = "种子机构",
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
                Name_DisplayName =  "种子角色",
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
            var admin = db.Accounts.FirstOrDefault(c => c.LoginName == SuperAdminLoginName);
            if (admin == null)  //若没有创建超管
            {
                admin = new Account
                {
                    LoginName = SuperAdminLoginName,
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
            var stream = new MemoryStream();
            using (var writer = new DBFWriter(stream))
            {
                writer.Fields = new[]
                {
                    new DBFField("Name", NativeDbType.Char, 50, 0),
                };
                // 写入一些数据
            }
            // 测试流是否还可用
            try
            {
                stream.Position = 0; // 如果抛异常，说明流被关闭了
                Console.WriteLine("流仍然打开");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("流已被关闭");
            }
        }

    }
}