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
    /// ��ʼ������
    /// </summary>
    public partial class InitializerService : BackgroundService
    {
        /// <summary>
        /// ���캯����
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
        /// ��������Ա��¼����
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
            _Logger.LogInformation("Plms����ɹ�����");
            return task;
        }

        /// <summary>
        /// ������Ч���û�-��ɫ����ɫ-Ȩ�ޡ��û�-������ϵ����
        /// </summary>
        /// <param name="svc">�����ṩ��</param>
        private void CleanupInvalidRelationships(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _Logger.LogInformation("��ʼ������Ч�Ĺ�����ϵ����...");

            try
            {
                // ʹ��EF Coreɾ����Ч���û�-��ɫ��ϵ
                var invalidUserRoles = db.PlAccountRoles
                    .Where(ur => !db.Accounts.Any(u => u.Id == ur.UserId) ||
                                 !db.PlRoles.Any(r => r.Id == ur.RoleId))
                    .ToList();

                if (invalidUserRoles.Count > 0)
                {
                    db.PlAccountRoles.RemoveRange(invalidUserRoles);
                    _Logger.LogInformation("׼������ {count} ����Ч���û�-��ɫ��ϵ", invalidUserRoles.Count);
                }

                // ʹ��EF Coreɾ����Ч�Ľ�ɫ-Ȩ�޹�ϵ
                var invalidRolePermissions = db.PlRolePermissions
                    .Where(rp => !db.PlRoles.Any(r => r.Id == rp.RoleId) ||
                                 !db.PlPermissions.Any(p => p.Name == rp.PermissionId))
                    .ToList();

                if (invalidRolePermissions.Count > 0)
                {
                    db.PlRolePermissions.RemoveRange(invalidRolePermissions);
                    _Logger.LogInformation("׼������ {count} ����Ч�Ľ�ɫ-Ȩ�޹�ϵ", invalidRolePermissions.Count);
                }

                // ʹ��EF Coreɾ����Ч���û�-������ϵ
                var invalidUserOrgs = db.AccountPlOrganizations
                    .Where(uo => !db.Accounts.Any(u => u.Id == uo.UserId) ||
                                (!db.PlOrganizations.Any(o => o.Id == uo.OrgId) && !db.Merchants.Any(c => c.Id == uo.OrgId)))
                    .ToList();

                if (invalidUserOrgs.Count > 0)
                {
                    db.AccountPlOrganizations.RemoveRange(invalidUserOrgs);
                    _Logger.LogInformation("׼������ {count} ����Ч���û�-������ϵ", invalidUserOrgs.Count);
                }

                // �������и���
                var totalRemoved = db.SaveChanges();

                stopwatch.Stop();
                _Logger.LogInformation("��ϵ������ɣ���ɾ�� {total} ����Ч���ݣ���ʱ: {elapsed}ms",
                    totalRemoved, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "������Ч������ϵʱ��������");
            }
        }

        /// <summary>
        /// �����������ݡ�
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        [Conditional("DEBUG")]
        private void SeedData(IServiceProvider svc)
        {
            var db = svc.GetService<PowerLmsUserDbContext>();
            var merch = new PlMerchant
            {
                Id = Guid.Parse("{073E65D6-EA0F-4D13-9510-3973F5A47526}"),
                Name = new PlOwnedName { DisplayName = "�����̻�", Name = "�����̻�", },
            };
            db.AddOrUpdate(merch);
            var org = new PlOrganization
            {
                Id = Guid.Parse("{FB069576-3E3D-46DF-9F13-B7D5FBA84717}"),
                Name = new PlOwnedName() { DisplayName = "���ӻ���" },
                MerchantId = Guid.Parse("{073E65D6-EA0F-4D13-9510-3973F5A47526}"),
                Otc = 2,
            };
            db.AddOrUpdate(org);

            var user = new Account
            {
                Id = Guid.Parse("{61810FEA-7CE1-4458-BD2E-436BD22C894E}"),
                LoginName = "SeedUser",
                DisplayName = "�����û�",
                OrgId = org.Id,
                Token = Guid.Parse("{7B823D05-F7CD-4A0C-9EA8-5D2D8CA630EB}"),
            };
            user.SetPwd("!@#$");
            db.AddOrUpdate(user);

            var role = new PlRole
            {
                Id = Guid.Parse("{310319E1-39EE-4140-8100-1E598113E1FE}"),
                Name = new PlOwnedName() { DisplayName = "���ӽ�ɫ" },
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
            //���ý��㵥
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
        /// ������Ҫ��ϵͳ��Դ��
        /// </summary>
        /// <param name="svc"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CreateSystemResource(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();

            var filePath = Path.Combine(AppContext.BaseDirectory, "ϵͳ��Դ", "ϵͳ��Դ.xlsx");
            using var file = File.OpenRead(filePath);

            using var workbook = _NpoiManager.GetWorkbookFromStream(file);
            var sheet = workbook.GetSheetAt(0);

            _NpoiManager.WriteToDb(sheet, db, db.DD_SystemResources);


            db.SaveChanges();
        }

        /// <summary>
        /// ��ʼ�������ֵ䡣
        /// </summary>
        /// <param name="svc">��Χ�Է�������</param>
        private void InitializeDataDic(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var filePath = Path.Combine(AppContext.BaseDirectory, "ϵͳ��Դ", "Ԥ��ʼ�������ֵ�.xlsx");
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

            // �������и���
            db.SaveChanges();
        }

        /// <summary>
        /// ��������Ա��
        /// </summary>
        /// <param name="svc"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CreateAdmin(IServiceProvider svc)
        {
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var admin = db.Accounts.FirstOrDefault(c => c.LoginName == SuperAdminLoginName);
            if (admin == null)  //��û�д�������
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
                // д��һЩ����
            }
            // �������Ƿ񻹿���
            try
            {
                stream.Position = 0; // ������쳣��˵�������ر���
                Console.WriteLine("����Ȼ��");
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("���ѱ��ر�");
            }
        }

    }
}