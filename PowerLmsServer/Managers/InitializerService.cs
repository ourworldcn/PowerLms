/*
 * PowerLms - 货运物流业务管理系统
 * 系统初始化服务 - 数据库和种子数据初始化管理
 * 
 * 功能说明：
 * - 基于JSON流的高性能Excel数据初始化
 * - 权限表和系统资源的种子数据管理
 * - 超级管理员账户自动创建
 * - 数据关系完整性验证和清理
 * - 支持增量插入模式，避免数据重复
 * 
 * 技术特点：
 * - 复用OwDataUnit + OwNpoiUnit基础设施组件
 * - JSON流处理降低内存分配和GC压力
 * - PooledList内存优化，提升大数据集处理性能
 * - 统一错误处理和详细日志记录
 * - 支持多租户数据隔离和权限验证
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年12月 - JSON流架构重构，简化代码结构
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI;
using OW;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 系统初始化服务 - 负责数据库初始化和种子数据管理
    /// </summary>
    public partial class InitializerService : BackgroundService
    {
        #region 字段和构造函数

        /// <summary>超级管理员登录名</summary>
        private const string SuperAdminLoginName = "868d61ae-3a86-42a8-8a8c-1ed6cfa90817";
        
        private readonly ILogger<InitializerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="serviceScopeFactory">服务范围工厂</param>
        /// <param name="serviceProvider">服务提供者</param>
        public InitializerService(ILogger<InitializerService> logger, IServiceScopeFactory serviceScopeFactory, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        #endregion

        #region 主要初始化流程

        /// <summary>
        /// 执行系统初始化任务
        /// </summary>
        /// <param name="stoppingToken">取消令牌</param>
        /// <returns>异步任务</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var task = Task.Run(() =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var services = scope.ServiceProvider;
                InitializeDatabase(services); // 初始化数据库结构
                InitializeSeedData(services); // 初始化种子数据
                CreateSuperAdministrator(services); // 创建超级管理员
                CleanupInvalidRelationships(services); // 清理无效关系数据
                RunDiagnosticTests(services); // 运行诊断测试
            }, CancellationToken.None);
            _logger.LogInformation("PowerLms系统初始化完成，服务已上线");
            return task;
        }

        /// <summary>
        /// 初始化数据库结构
        /// </summary>
        /// <param name="services">服务提供者</param>
        private void InitializeDatabase(IServiceProvider services)
        {
            _logger.LogInformation("开始初始化数据库结构");
            // 这里可以添加数据库结构验证和基础表创建逻辑
            _logger.LogInformation("数据库结构初始化完成");
        }

        /// <summary>
        /// 初始化种子数据 - 基于Excel文件的统一数据处理
        /// </summary>
        /// <param name="services">服务提供者</param>
        private void InitializeSeedData(IServiceProvider services)
        {
            try
            {
                var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();
                _logger.LogInformation("开始初始化系统种子数据");
                dbContext.TruncateTable("PlPermissions"); // 权限表特殊处理：清空重建确保数据完整性
                var success = ProcessExcelSeedData(dbContext);
                if (success)
                {
                    _logger.LogInformation("系统种子数据初始化成功");
                }
                else
                {
                    _logger.LogWarning("系统种子数据初始化部分失败或跳过");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "系统种子数据初始化过程中发生错误");
            }
        }

        /// <summary>
        /// 处理Excel种子数据文件
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否成功处理</returns>
        private bool ProcessExcelSeedData(PowerLmsUserDbContext dbContext)
        {
            try
            {
                var excelFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "系统资源", "预初始化数据.xlsx");
                if (!File.Exists(excelFilePath))
                {
                    _logger.LogWarning("Excel种子数据文件不存在: {FilePath}", excelFilePath);
                    return false;
                }
                using var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read);
                using var workbook = new XSSFWorkbook(fileStream);
                ProcessWorkbookViaJsonStream(workbook, dbContext);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理Excel种子数据时发生错误");
                return false;
            }
        }

        #endregion

        #region JSON流数据处理

        /// <summary>
        /// 通过JSON流处理Excel工作簿数据 - 高性能版本
        /// </summary>
        /// <param name="workbook">Excel工作簿</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <remarks>
        /// 优化处理流程：
        /// 1. Excel工作表转换为JSON流
        /// 2. JSON反序列化为实体集合
        /// 3. 调用基础库批量插入数据库
        /// 4. 复用内存流降低GC压力
        /// </remarks>
        private void ProcessWorkbookViaJsonStream(IWorkbook workbook, PowerLmsUserDbContext dbContext)
        {
            var dbType = typeof(PowerLmsUserDbContext);
            int processedSheets = 0;
            int totalInserted = 0;
            _logger.LogInformation("开始处理Excel工作簿，共{totalSheets}个工作表", workbook.NumberOfSheets);
            using var processedResults = new PooledList<string>(workbook.NumberOfSheets);
            using var errorMessages = new PooledList<string>(workbook.NumberOfSheets);
            using var reusableJsonStream = new MemoryStream(1024 * 1024); // 预分配1MB复用流
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                var sheetName = sheet.SheetName;
                try
                {
                    var dbSetProperty = dbType.GetProperty(sheetName);
                    if (!IsValidDbSetProperty(dbSetProperty, dbContext))
                    {
                        var warningMsg = $"跳过工作表：{sheetName}，未找到对应的DbSet";
                        errorMessages.Add(warningMsg);
                        _logger.LogWarning(warningMsg);
                        continue;
                    }
                    var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
                    var insertedCount = ProcessSheetToDatabase(sheet, dbContext, entityType, reusableJsonStream);
                    totalInserted += insertedCount;
                    processedSheets++;
                    processedResults.Add($"{sheetName}({insertedCount}条记录)");
                    _logger.LogInformation("成功处理工作表：{sheetName}，插入记录：{insertedCount}", sheetName, insertedCount);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"处理工作表失败：{sheetName} - {ex.Message}";
                    errorMessages.Add(errorMsg);
                    _logger.LogError(ex, "处理工作表失败：{sheetName}", sheetName);
                }
            }
            SaveChangesAndLogResults(dbContext, processedSheets, totalInserted, workbook.NumberOfSheets, processedResults, errorMessages);
        }

        /// <summary>
        /// 处理单个工作表到数据库 - JSON流转换方式
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="reusableStream">可复用内存流</param>
        /// <returns>插入的记录数</returns>
        private int ProcessSheetToDatabase(ISheet sheet, DbContext dbContext, Type entityType, MemoryStream reusableStream)
        {
            reusableStream.SetLength(0); // 清空流但保持容量
            reusableStream.Position = 0;
            OwNpoiUnit.WriteJsonToStream(sheet, 0, reusableStream); // 步骤1：Excel转JSON流
            if (reusableStream.Length == 0) return 0;
            reusableStream.Position = 0;
            var jsonBytes = reusableStream.ToArray();
            var jsonString = Encoding.UTF8.GetString(jsonBytes);
            if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "[]") return 0;
            var collectionType = typeof(IEnumerable<>).MakeGenericType(entityType);
            var entities = JsonSerializer.Deserialize(jsonString, collectionType) as System.Collections.IEnumerable; // 步骤2：JSON反序列化
            if (entities == null) return 0;
            return OwDataUnit.BulkInsert(entities, dbContext, entityType, ignoreExisting: true); // 步骤3：批量插入数据库
        }

        /// <summary>
        /// 验证DbSet属性是否有效
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否有效</returns>
        private static bool IsValidDbSetProperty(System.Reflection.PropertyInfo property, DbContext dbContext)
        {
            return property?.PropertyType?.IsGenericType == true &&
                   property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                   property.GetValue(dbContext) != null;
        }

        /// <summary>
        /// 保存更改并记录处理结果
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="processedSheets">处理的工作表数量</param>
        /// <param name="totalInserted">插入的总记录数</param>
        /// <param name="totalSheets">总工作表数量</param>
        /// <param name="processedResults">处理结果列表</param>
        /// <param name="errorMessages">错误消息列表</param>
        private void SaveChangesAndLogResults(DbContext dbContext, int processedSheets, int totalInserted, int totalSheets,
            PooledList<string> processedResults, PooledList<string> errorMessages)
        {
            try
            {
                var affectedRows = dbContext.SaveChanges();
                _logger.LogInformation("数据初始化完成：处理{processedSheets}/{totalSheets}个工作表，插入{totalInserted}条记录，影响{affectedRows}行数据",
                    processedSheets, totalSheets, totalInserted, affectedRows);
                if (_logger.IsEnabled(LogLevel.Debug) && processedResults.Count > 0)
                {
                    _logger.LogDebug("处理详情：{details}", string.Join(", ", processedResults));
                }
                if (errorMessages.Count > 0)
                {
                    _logger.LogWarning("处理过程中遇到{errorCount}个问题", errorMessages.Count);
                    foreach (var error in errorMessages)
                    {
                        _logger.LogWarning("问题详情：{error}", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存数据库更改时发生错误");
                throw;
            }
        }

        #endregion

        #region 管理员和数据清理

        /// <summary>
        /// 创建超级管理员账户
        /// </summary>
        /// <param name="services">服务提供者</param>
        private void CreateSuperAdministrator(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();
            var admin = dbContext.Accounts.FirstOrDefault(c => c.LoginName == SuperAdminLoginName);
            if (admin == null)
            {
                admin = new Account
                {
                    LoginName = SuperAdminLoginName,
                    CurrentLanguageTag = "zh-CN",
                    LastModifyDateTimeUtc = OwHelper.WorldNow,
                    State = 4,
                };
                dbContext.Accounts.Add(admin);
                _logger.LogInformation("创建超级管理员账户");
            }
            else
            {
                admin.State = 4;
                _logger.LogInformation("更新超级管理员账户状态");
            }
            admin.SetPwd("1D381427-86BB-4D88-8CB0-5D92F8E1BADF");
            dbContext.SaveChanges();
        }

        /// <summary>
        /// 清理无效的关联关系数据
        /// </summary>
        /// <param name="services">服务提供者</param>
        private void CleanupInvalidRelationships(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("开始清理无效的关联关系数据");
            try
            {
                using var invalidUserRoleIds = new PooledList<Guid>(1000);
                using var invalidRolePermissionIds = new PooledList<Guid>(1000);
                using var invalidUserOrgIds = new PooledList<Guid>(1000);
                CollectInvalidUserRoles(dbContext, invalidUserRoleIds); // 收集无效用户-角色关系
                CollectInvalidRolePermissions(dbContext, invalidRolePermissionIds); // 收集无效角色-权限关系
                CollectInvalidUserOrganizations(dbContext, invalidUserOrgIds); // 收集无效用户-机构关系
                var totalRemoved = RemoveInvalidRelationships(dbContext, invalidUserRoleIds, invalidRolePermissionIds, invalidUserOrgIds);
                var actualRemoved = dbContext.SaveChanges();
                stopwatch.Stop();
                _logger.LogInformation("关系清理完成：删除{total}条无效数据，实际影响{actual}行，耗时{elapsed}ms",
                    totalRemoved, actualRemoved, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理无效关联关系时发生错误");
            }
        }

        /// <summary>
        /// 运行诊断测试
        /// </summary>
        /// <param name="services">服务提供者</param>
        [Conditional("DEBUG")]
        private void RunDiagnosticTests(IServiceProvider services)
        {
            _logger.LogDebug("运行系统诊断测试");
            // 这里可以添加各种系统诊断逻辑
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 收集无效的用户-角色关系
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="invalidIds">无效ID集合</param>
        private static void CollectInvalidUserRoles(PowerLmsUserDbContext dbContext, PooledList<Guid> invalidIds)
        {
            var userRoleQuery = dbContext.PlAccountRoles
                .Where(ur => !dbContext.Accounts.Any(u => u.Id == ur.UserId) ||
                             !dbContext.PlRoles.Any(r => r.Id == ur.RoleId))
                .Select(ur => ur.UserId);
            foreach (var id in userRoleQuery)
            {
                invalidIds.Add(id);
            }
        }

        /// <summary>
        /// 收集无效的角色-权限关系
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="invalidIds">无效ID集合</param>
        private static void CollectInvalidRolePermissions(PowerLmsUserDbContext dbContext, PooledList<Guid> invalidIds)
        {
            var rolePermissionQuery = dbContext.PlRolePermissions
                .Where(rp => !dbContext.PlRoles.Any(r => r.Id == rp.RoleId) ||
                             !dbContext.PlPermissions.Any(p => p.Name == rp.PermissionId))
                .Select(rp => rp.RoleId);
            foreach (var id in rolePermissionQuery)
            {
                invalidIds.Add(id);
            }
        }

        /// <summary>
        /// 收集无效的用户-机构关系
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="invalidIds">无效ID集合</param>
        private static void CollectInvalidUserOrganizations(PowerLmsUserDbContext dbContext, PooledList<Guid> invalidIds)
        {
            var userOrgQuery = dbContext.AccountPlOrganizations
                .Where(uo => !dbContext.Accounts.Any(u => u.Id == uo.UserId) ||
                            (!dbContext.PlOrganizations.Any(o => o.Id == uo.OrgId) && !dbContext.Merchants.Any(c => c.Id == uo.OrgId)))
                .Select(uo => uo.UserId);
            foreach (var id in userOrgQuery)
            {
                invalidIds.Add(id);
            }
        }

        /// <summary>
        /// 移除无效的关联关系
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="invalidUserRoleIds">无效用户-角色ID</param>
        /// <param name="invalidRolePermissionIds">无效角色-权限ID</param>
        /// <param name="invalidUserOrgIds">无效用户-机构ID</param>
        /// <returns>移除的记录总数</returns>
        private static int RemoveInvalidRelationships(PowerLmsUserDbContext dbContext,
            PooledList<Guid> invalidUserRoleIds, PooledList<Guid> invalidRolePermissionIds, PooledList<Guid> invalidUserOrgIds)
        {
            int totalRemoved = 0;
            if (invalidUserRoleIds.Count > 0)
            {
                var toRemove = dbContext.PlAccountRoles.Where(ur => invalidUserRoleIds.Contains(ur.UserId)).ToList();
                dbContext.PlAccountRoles.RemoveRange(toRemove);
                totalRemoved += toRemove.Count;
            }
            if (invalidRolePermissionIds.Count > 0)
            {
                var toRemove = dbContext.PlRolePermissions.Where(rp => invalidRolePermissionIds.Contains(rp.RoleId)).ToList();
                dbContext.PlRolePermissions.RemoveRange(toRemove);
                totalRemoved += toRemove.Count;
            }
            if (invalidUserOrgIds.Count > 0)
            {
                var toRemove = dbContext.AccountPlOrganizations.Where(uo => invalidUserOrgIds.Contains(uo.UserId)).ToList();
                dbContext.AccountPlOrganizations.RemoveRange(toRemove);
                totalRemoved += toRemove.Count;
            }
            return totalRemoved;
        }

        #endregion

        #region 公共工具方法

        /// <summary>
        /// 通用JSON流处理工具 - 处理Excel工作表到数据库
        /// </summary>
        /// <param name="sheet">Excel工作表</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="reusableStream">可复用内存流（可选）</param>
        /// <param name="ignoreExisting">是否忽略已存在数据</param>
        /// <returns>插入的记录数</returns>
        /// <exception cref="InvalidOperationException">处理失败时抛出</exception>
        /// <remarks>
        /// 高性能JSON流处理：
        /// 1. Excel工作表转JSON流（复用<paramref name="OwNpoiUnit.WriteJsonToStream"/>）
        /// 2. JSON反序列化为实体集合
        /// 3. 批量数据库插入（复用<paramref name="OwDataUnit.BulkInsert"/>）
        /// 4. 支持流复用降低GC压力
        /// </remarks>
        public static int ProcessExcelToDatabase(ISheet sheet, DbContext dbContext, Type entityType,
            MemoryStream reusableStream = null, bool ignoreExisting = true)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(entityType);
            bool shouldDisposeStream = reusableStream == null;
            var jsonStream = reusableStream ?? new MemoryStream(64 * 1024);
            try
            {
                if (reusableStream != null)
                {
                    jsonStream.SetLength(0);
                    jsonStream.Position = 0;
                }
                OwNpoiUnit.WriteJsonToStream(sheet, 0, jsonStream);
                if (jsonStream.Length == 0) return 0;
                jsonStream.Position = 0;
                var jsonBytes = jsonStream.ToArray();
                var jsonString = Encoding.UTF8.GetString(jsonBytes);
                if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "[]") return 0;
                var collectionType = typeof(IEnumerable<>).MakeGenericType(entityType);
                var entities = JsonSerializer.Deserialize(jsonString, collectionType) as System.Collections.IEnumerable;
                if (entities == null) return 0;
                return OwDataUnit.BulkInsert(entities, dbContext, entityType, ignoreExisting);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"处理Excel工作表'{sheet.SheetName}'到数据库失败：{ex.Message}", ex);
            }
            finally
            {
                if (shouldDisposeStream) jsonStream?.Dispose();
            }
        }

        #endregion
    }
}