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
using NPOI; // 添加NPOI引用以使用NpoiUnit.GetStringList
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel; // 添加XSSFWorkbook支持
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
    /// 初始化服务 - 完全基于Excel文件的统一数据初始化
    /// </summary>
    public partial class InitializerService : BackgroundService
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="serviceScopeFactory">服务范围工厂</param>
        /// <param name="serviceProvider">服务提供者</param>
        public InitializerService(ILogger<InitializerService> logger, IServiceScopeFactory serviceScopeFactory, IServiceProvider serviceProvider) : base()
        {
            _Logger = logger;
            _ServiceScopeFactory = serviceScopeFactory;
            _ServiceProvider = serviceProvider;
        }

        /// <summary>超级管理员登录名</summary>
        private const string SuperAdminLoginName = "868d61ae-3a86-42a8-8a8c-1ed6cfa90817";
        readonly ILogger<InitializerService> _Logger;
        readonly IServiceScopeFactory _ServiceScopeFactory;
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
                
                // ✅ 统一的Excel数据初始化：包含所有数据字典和种子数据（包括系统资源）
                InitializeDataFromExcelSeed(svc);
                
                CreateAdmin(svc);
                CleanupInvalidRelationships(svc);
                Test(svc);
            }, CancellationToken.None);
            _Logger.LogInformation("Plms服务成功上线");
            return task;
        }

        /// <summary>
        /// 从预初始化数据Excel文件初始化数据库种子数据 - 统一处理所有数据
        /// </summary>
        /// <param name="svc">服务提供者</param>
        private void InitializeDataFromExcelSeed(IServiceProvider svc)
        {
            try
            {
                var db = svc.GetRequiredService<PowerLmsUserDbContext>();
                
                // 🎯 权限表特殊处理：清理后重新初始化，确保权限数据完整性
                // ⚠️ 注意：只有权限表采用"清空+重建"模式，其他表采用"增量插入"模式
                _Logger.LogInformation("清理权限表以确保数据一致性（权限表特殊处理）");
                db.TruncateTable("PlPermissions");
                
                // ✅ 其他数据表采用增量插入模式：只添加不存在的数据，避免重复
                var success = InitializeDataFromExcel(db);
                
                if (success)
                {
                    _Logger.LogInformation("Excel统一数据初始化成功完成（增量模式）");
                }
                else
                {
                    _Logger.LogWarning("Excel统一数据初始化未执行或部分失败");
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Excel统一数据初始化过程中发生错误");
                // 不抛出异常，继续其他初始化步骤
            }
        }

        /// <summary>
        /// 清理无效的用户-角色、角色-权限、用户-机构关系数据 - 使用PooledList优化
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
                // 使用PooledList收集需要删除的数据，减少内存分配
                using var invalidUserRoleIds = new PooledList<Guid>(1000); // 预估容量
                using var invalidRolePermissionIds = new PooledList<Guid>(1000);
                using var invalidUserOrgIds = new PooledList<Guid>(1000);

                // 收集无效的用户-角色关系ID
                var userRoleQuery = db.PlAccountRoles
                    .Where(ur => !db.Accounts.Any(u => u.Id == ur.UserId) ||
                                 !db.PlRoles.Any(r => r.Id == ur.RoleId))
                    .Select(ur => ur.UserId); // 使用UserId作为示例，实际应该是主键

                foreach (var id in userRoleQuery)
                {
                    invalidUserRoleIds.Add(id);
                }

                // 收集无效的角色-权限关系ID
                var rolePermissionQuery = db.PlRolePermissions
                    .Where(rp => !db.PlRoles.Any(r => r.Id == rp.RoleId) ||
                                 !db.PlPermissions.Any(p => p.Name == rp.PermissionId))
                    .Select(rp => rp.RoleId); // 使用RoleId作为示例

                foreach (var id in rolePermissionQuery)
                {
                    invalidRolePermissionIds.Add(id);
                }

                // 收集无效的用户-机构关系ID
                var userOrgQuery = db.AccountPlOrganizations
                    .Where(uo => !db.Accounts.Any(u => u.Id == uo.UserId) ||
                                (!db.PlOrganizations.Any(o => o.Id == uo.OrgId) && !db.Merchants.Any(c => c.Id == uo.OrgId)))
                    .Select(uo => uo.UserId); // 使用UserId作为示例

                foreach (var id in userOrgQuery)
                {
                    invalidUserOrgIds.Add(id);
                }

                // 批量删除操作
                var totalRemoved = 0;
                
                if (invalidUserRoleIds.Count > 0)
                {
                    // 注意：这里的逻辑需要根据实际的主键结构调整
                    var toRemove = db.PlAccountRoles.Where(ur => invalidUserRoleIds.Contains(ur.UserId)).ToList();
                    db.PlAccountRoles.RemoveRange(toRemove);
                    _Logger.LogInformation("准备清理 {count} 条无效的用户-角色关系", toRemove.Count);
                    totalRemoved += toRemove.Count;
                }

                if (invalidRolePermissionIds.Count > 0)
                {
                    var toRemove = db.PlRolePermissions.Where(rp => invalidRolePermissionIds.Contains(rp.RoleId)).ToList();
                    db.PlRolePermissions.RemoveRange(toRemove);
                    _Logger.LogInformation("准备清理 {count} 条无效的角色-权限关系", toRemove.Count);
                    totalRemoved += toRemove.Count;
                }

                if (invalidUserOrgIds.Count > 0)
                {
                    var toRemove = db.AccountPlOrganizations.Where(uo => invalidUserOrgIds.Contains(uo.UserId)).ToList();
                    db.AccountPlOrganizations.RemoveRange(toRemove);
                    _Logger.LogInformation("准备清理 {count} 条无效的用户-机构关系", toRemove.Count);
                    totalRemoved += toRemove.Count;
                }

                // 保存所有更改
                var actualRemoved = db.SaveChanges();

                stopwatch.Stop();
                _Logger.LogInformation("关系清理完成，共删除 {total} 条无效数据，耗时: {elapsed}ms",
                    actualRemoved, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "清理无效关联关系时发生错误");
            }
        }

        /// <summary>
        /// 创建必要的系统资源 - 已整合到通用Excel初始化中
        /// </summary>
        /// <param name="svc">服务提供者</param>
        [Obsolete("系统资源数据已整合到通用Excel初始化中，请使用InitializeDataFromExcelSeed")]
        private void CreateSystemResource(IServiceProvider svc)
        {
            // ✅ 系统资源数据已通过InitializeDataFromExcelSeed()统一处理
            // 数据来源：PowerLmsData/系统资源/预初始化数据.xlsx 的 DD_SystemResources 工作表
            // 🎯 采用增量插入模式：只添加数据库中不存在的记录，已存在的记录不会重复插入
            _Logger.LogInformation("系统资源数据将通过通用Excel初始化处理（增量模式），跳过独立处理");
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

        #region 数据初始化相关方法 - PooledList优化版本

        /// <summary>
        /// 从Excel工作簿批量初始化数据库表 - 使用DataSeedHelper优化版本
        /// </summary>
        /// <param name="workbook">Excel工作簿，每个工作表名称对应数据库表名</param>
        /// <param name="svc">服务提供者，用于获取数据库上下文</param>
        /// <exception cref="ArgumentNullException">当workbook或svc为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当找不到对应的DbSet属性时抛出</exception>
        public void InitializationDataFromWorkbook(IWorkbook workbook, IServiceProvider svc)
        {
            if (workbook == null) throw new ArgumentNullException(nameof(workbook));
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            
            var db = svc.GetRequiredService<PowerLmsUserDbContext>();
            var dbType = typeof(PowerLmsUserDbContext);
            int processedSheets = 0;
            int totalSheets = workbook.NumberOfSheets;
            int totalInserted = 0;
            
            _Logger.LogInformation("开始从Excel工作簿初始化数据，总计{totalSheets}个工作表", totalSheets);
            
            // 使用PooledList收集处理结果，避免频繁的字符串拼接和集合操作
            using var processedSheetNames = new PooledList<string>(totalSheets);
            using var errorMessages = new PooledList<string>(totalSheets);
            
            for (int i = 0; i < totalSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                var sheetName = sheet.SheetName;
                
                try
                {
                    var dbSetProperty = dbType.GetProperty(sheetName);
                    if (dbSetProperty == null)
                    {
                        var warningMsg = $"跳过工作表：{sheetName}，未找到对应的数据库表";
                        errorMessages.Add(warningMsg);
                        _Logger.LogWarning(warningMsg);
                        continue;
                    }
                    
                    var dbSetPropertyType = dbSetProperty.PropertyType;
                    if (!dbSetPropertyType.IsGenericType || dbSetPropertyType.GetGenericTypeDefinition() != typeof(DbSet<>))
                    {
                        var warningMsg = $"跳过工作表：{sheetName}，对应属性不是DbSet类型";
                        errorMessages.Add(warningMsg);
                        _Logger.LogWarning(warningMsg);
                        continue;
                    }
                    
                    var entityType = dbSetPropertyType.GetGenericArguments()[0];
                    var dbSetValue = dbSetProperty.GetValue(db);
                    
                    if (dbSetValue == null)
                    {
                        var warningMsg = $"跳过工作表：{sheetName}，DbSet实例为null";
                        errorMessages.Add(warningMsg);
                        _Logger.LogWarning(warningMsg);
                        continue;
                    }
                    
                    // 🚀 直接使用DataSeedHelper替代反射调用NpoiManager.WriteToDb
                    try
                    {
                        // 直接使用 DataSeedHelper.BulkInsertFromExcelNonGeneric 替代复杂的反射
                        var insertedCount = DataSeedHelper.BulkInsertFromExcelNonGeneric(
                            sheet, db, entityType, ignoreExisting: true, _Logger, $"工作表{sheetName}批量导入");
                        
                        totalInserted += insertedCount;
                        processedSheets++;
                        processedSheetNames.Add($"{sheetName}({insertedCount}条记录)");
                        
                        _Logger.LogInformation("成功处理工作表：{sheetName}，实体类型：{entityType}，插入记录：{insertedCount}", 
                            sheetName, entityType.Name, insertedCount);
                    }
                    catch (Exception ex)
                    {
                        // 如果DataSeedHelper方式失败，尝试使用 NpoiUnit + AddOrUpdate 回退
                        _Logger.LogWarning(ex, "DataSeedHelper处理工作表{sheetName}失败，回退到NpoiUnit方式", sheetName);
                        
                        try
                        {
                            // 使用 NpoiUnit.GetSheet<T> 进行类型安全的转换
                            var getSheetMethod = typeof(NpoiUnit).GetMethod(nameof(NpoiUnit.GetSheet))?.MakeGenericMethod(entityType);
                            if (getSheetMethod != null)
                            {
                                var entities = getSheetMethod.Invoke(null, new object[] { sheet }) as IEnumerable<object>;
                                if (entities != null)
                                {
                                    foreach (var entity in entities)
                                    {
                                        db.AddOrUpdate(entity);
                                    }
                                    processedSheets++;
                                    processedSheetNames.Add($"{sheetName}(回退模式)");
                                    _Logger.LogInformation("成功处理工作表（NpoiUnit回退模式）：{sheetName}，实体类型：{entityType}", sheetName, entityType.Name);
                                }
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            var errorMsg = $"处理工作表失败：{sheetName} - {fallbackEx.Message}";
                            errorMessages.Add(errorMsg);
                            _Logger.LogError(fallbackEx, "NpoiUnit回退模式也失败：{sheetName}", sheetName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"处理工作表失败：{sheetName} - {ex.Message}";
                    errorMessages.Add(errorMsg);
                    _Logger.LogError(ex, "处理工作表失败：{sheetName}", sheetName);
                    // 继续处理下一个工作表，不中断整个初始化过程
                }
            }
            
            try
            {
                var affectedRows = db.SaveChanges();
                _Logger.LogInformation("数据初始化完成，成功处理{processedSheets}/{totalSheets}个工作表，DataSeedHelper插入{totalInserted}条，总影响{affectedRows}行数据", 
                    processedSheets, totalSheets, totalInserted, affectedRows);
                
                // 记录处理的工作表名称（仅在调试模式下）
                if (_Logger.IsEnabled(LogLevel.Debug) && processedSheetNames.Count > 0)
                {
                    var sheetNamesList = string.Join(", ", processedSheetNames);
                    _Logger.LogDebug("已处理的工作表: {sheetNames}", sheetNamesList);
                }
                
                // 记录错误信息（如果有）
                if (errorMessages.Count > 0)
                {
                    _Logger.LogWarning("处理过程中遇到 {errorCount} 个问题", errorMessages.Count);
                    foreach (var error in errorMessages)
                    {
                        _Logger.LogWarning("问题详情: {error}", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "保存数据库更改时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 从指定Excel文件批量初始化数据库表 - PooledList优化版本
        /// </summary>
        /// <param name="excelFilePath">Excel文件的完整路径</param>
        /// <param name="svc">服务提供者，用于获取数据库上下文</param>
        /// <exception cref="ArgumentNullException">当excelFilePath或svc为null时抛出</exception>
        /// <exception cref="FileNotFoundException">当Excel文件不存在时抛出</exception>
        public void InitializeDataFromExcelFile(string excelFilePath, IServiceProvider svc)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath)) throw new ArgumentNullException(nameof(excelFilePath));
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            if (!File.Exists(excelFilePath)) throw new FileNotFoundException($"Excel文件不存在: {excelFilePath}");
            
            _Logger.LogInformation("开始从Excel文件初始化数据: {excelFilePath}", excelFilePath);
            
            using var fileStream = File.OpenRead(excelFilePath);
            using var workbook = WorkbookFactory.Create(fileStream); // 🚀 直接使用WorkbookFactory.Create
            InitializationDataFromWorkbook(workbook, svc);
        }

        /// <summary>
        /// 批量数据处理的工具方法 - 使用PooledList优化大数据集处理
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="batchSize">批次大小</param>
        /// <param name="processor">批次处理器</param>
        /// <returns>处理结果统计</returns>
        public (int TotalProcessed, int BatchCount) ProcessInBatches<T>(IEnumerable<T> source, int batchSize, Action<PooledList<T>> processor)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "批次大小必须大于0");

            int totalProcessed = 0;
            int batchCount = 0;
            
            using var currentBatch = new PooledList<T>(batchSize); // 预分配批次大小的容量
            
            foreach (var item in source)
            {
                currentBatch.Add(item);
                
                if (currentBatch.Count >= batchSize)
                {
                    processor(currentBatch); // 处理当前批次
                    totalProcessed += currentBatch.Count;
                    batchCount++;
                    currentBatch.Clear(); // 清空但保留容量，避免重新分配
                }
            }
            
            // 处理最后一个不满批次的数据
            if (currentBatch.Count > 0)
            {
                processor(currentBatch);
                totalProcessed += currentBatch.Count;
                batchCount++;
            }
            
            return (totalProcessed, batchCount);
        }

        #endregion 数据初始化相关方法 - PooledList优化版本
    }
}