﻿using AutoMapper;
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

        readonly ILogger<InitializerService> _Logger;
        readonly IServiceScopeFactory _ServiceScopeFactory;
        readonly NpoiManager _NpoiManager;

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
                Test(svc);
            }, CancellationToken.None);
            _Logger.LogInformation("Pl服务成功上线");
            return task;
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

            var sheet = workbook.GetSheet(nameof(db.PlPermissions));
            _NpoiManager.WriteToDb(sheet, db, db.PlPermissions);

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
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var dic = new Dictionary<string, string> {
                //{ "id", "B084BD4E-28BE-4052-A613-1C79B0AB268E" },
                {"CreateUtc","2024-1-19 ,2024-1-30 " },
                {"loginname","string" },
            };
            var query = EfHelper.GenerateWhereAnd(db.Accounts, dic);
            var tmp = typeof(Account).GetProperty("loginName", BindingFlags.IgnoreCase | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance|BindingFlags.FlattenHierarchy);

            var ary = query.ToArray();
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
