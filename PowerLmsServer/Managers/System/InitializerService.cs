/*
 * PowerLms - 货运物流业务管理系统
 * 系统初始化服务 - 负责数据库初始化和种子数据管理
 * 
 * 功能说明：
 * - 数据库连接验证和自动迁移（智能检测SQL Server状态）
 * - 系统种子数据的批量导入（基于Excel文件的高性能处理）
 * - 超级管理员账户创建和管理
 * - 无效关联关系数据清理和维护
 * - 基础配置数据初始化
 * 
 * 技术特点：
 * - 高性能JSON流处理Excel数据，使用OwDataUnit + OwNpoiUnit基础设施
 * - 智能数据库连接检查（支持数据库不存在的情况，连接master数据库验证）
 * - 完整的错误处理和结构化日志记录
 * - 复用基础设施组件，避免重复开发
 * - 内存优化的批量数据操作，降低GC压力
 * - 统一使用范围服务中的DbContext，避免工厂模式反复创建
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年12月
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
using System.Collections;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OwExtensions.NPOI;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 系统初始化服务 - 负责数据库初始化和种子数据管理
    /// 作为BackgroundService在应用启动时自动执行，确保系统各项基础设施就绪
    /// </summary>
    public partial class InitializerService : BackgroundService
    {
        #region 常量和字段

        /// <summary>超级管理员登录名 - 使用GUID格式确保全局唯一性</summary>
        private const string SuperAdminLoginName = "868d61ae-3a86-42a8-8a8c-1ed6cfa90817";

        /// <summary>数据库连接超时时间（秒） - 等待SQL Server就绪的最大时间</summary>
        private const int DatabaseConnectionTimeout = 60;

        private readonly ILogger<InitializerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化系统初始化服务
        /// </summary>
        /// <param name="logger">日志服务，用于记录初始化过程的详细信息</param>
        /// <param name="serviceScopeFactory">服务范围工厂，用于创建作用域内的服务实例</param>
        public InitializerService(ILogger<InitializerService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        #endregion

        #region BackgroundService实现

        /// <summary>
        /// 执行系统初始化任务的主入口点
        /// 该方法在应用启动时由BackgroundService框架自动调用
        /// </summary>
        /// <param name="stoppingToken">取消令牌，用于优雅停止服务</param>
        /// <returns>表示异步操作的任务</returns>
        /// <remarks>
        /// 初始化流程按照以下顺序执行：
        /// 1. 确保数据库连接就绪并执行迁移
        /// 2. 创建或更新超级管理员账户
        /// 3. 初始化系统种子数据（Excel文件导入）
        /// 4. 初始化基础配置数据（科目配置、发票通道等）
        /// 5. 清理无效的关联关系数据
        /// 所有步骤都有详细的日志记录和异常处理
        /// 所有方法共享同一个范围服务中的DbContext实例
        /// </remarks>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("系统初始化服务启动");

                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("系统初始化服务收到取消请求，停止初始化");
                    return;
                }

                // 使用Task.Run避免阻塞主线程
                await Task.Run(async () =>
                {
                    // 创建服务范围，确保所有操作在同一个作用域内
                    using var scope = _serviceScopeFactory.CreateScope();
                    var services = scope.ServiceProvider;
                    
                    try
                    {
                        _logger.LogInformation("开始PowerLms系统初始化流程");

                        // 第一步：确保数据库连接就绪并执行迁移
                        await EnsureDatabaseReadyAsync(services, stoppingToken);

                        if (stoppingToken.IsCancellationRequested) return;

                        // 第二步：创建超级管理员账户（确保系统有可管理账户）
                        CreateSuperAdministrator(services);

                        if (stoppingToken.IsCancellationRequested) return;

                        // 第三步：初始化系统种子数据（Excel文件中的基础数据）
                        InitializeSeedData(services);

                        if (stoppingToken.IsCancellationRequested) return;

                        // 第四步：初始化基础配置数据（科目配置、发票通道等）
                        InitDb(services);

                        if (stoppingToken.IsCancellationRequested) return;

                        // 第五步：清理无效的关联关系数据（数据完整性维护）
                        CleanupInvalidRelationships(services);

                        _logger.LogInformation("PowerLms系统初始化完成，服务已上线");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("系统初始化被取消");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "系统初始化失败，服务无法正常启动");
                        throw;
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("系统初始化服务被取消");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "系统初始化服务启动失败");
                throw;
            }
        }

        #endregion

        #region 数据库连接和迁移

        /// <summary>
        /// 异步确保数据库连接就绪并执行迁移
        /// 该方法首先检查SQL Server的连通性，然后执行Entity Framework的数据库迁移
        /// </summary>
        /// <param name="services">服务提供者，用于获取数据库上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <remarks>
        /// 连接检查策略：
        /// 1. 直接从数据库上下文获取连接字符串（无需配置文件）
        /// 2. 先连接到master数据库验证SQL Server是否可用（避免目标数据库不存在导致的连接失败）
        /// 3. 使用异步重试机制，最大等待60秒
        /// 4. 连接成功后使用Entity Framework执行数据库迁移
        /// 5. 迁移过程包括创建数据库（如果不存在）和应用所有挂起的迁移
        /// </remarks>
        private async Task EnsureDatabaseReadyAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始检查数据库连接和执行迁移");
            var startTime = DateTime.Now;

            try
            {
                // 直接从服务容器获取数据库上下文（让DI容器管理生命周期）
                var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();

                // 从数据库上下文获取连接字符串
                var connectionString = dbContext.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("无法从数据库上下文获取连接字符串");
                }

                _logger.LogInformation("从数据库上下文获取连接字符串成功");

                // 使用ConnectionStringBuilder修改连接字符串，连接到master数据库验证服务器状态
                var builder = new SqlConnectionStringBuilder(connectionString);
                var targetDatabase = builder.InitialCatalog; // 保存目标数据库名
                builder.InitialCatalog = "master"; // 连接master数据库避免目标数据库不存在的问题
                builder.ConnectTimeout = 5; // 设置较短的连接超时避免长时间阻塞

                _logger.LogInformation("等待SQL Server连接就绪，目标数据库：{TargetDatabase}", targetDatabase);

                // 异步重试连接直到SQL Server可用
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using var connection = new SqlConnection(builder.ConnectionString);
                        await connection.OpenAsync(cancellationToken);
                        connection.Close();
                        _logger.LogInformation("SQL Server连接验证成功，开始执行数据库迁移到：{TargetDatabase}", targetDatabase);
                        break; // 连接成功，跳出重试循环
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        if (DateTime.Now - startTime > TimeSpan.FromSeconds(DatabaseConnectionTimeout))
                        {
                            throw new TimeoutException($"启动时等待{DatabaseConnectionTimeout}秒仍无法连接到SQL Server，请检查数据库连接配置或机器性能。目标数据库：{targetDatabase}");
                        }
                        _logger.LogWarning("SQL Server连接失败，1秒后重试: {Message}", ex.Message);
                        await Task.Delay(1000, cancellationToken); // 每秒重试一次，避免过于频繁的重试
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("数据库连接检查被取消");
                    return;
                }

                // SQL Server可连接后，执行数据库迁移（这会自动创建数据库如果不存在）
                _logger.LogInformation("开始执行Entity Framework数据库迁移到：{TargetDatabase}", targetDatabase);
                
                // 使用异步迁移方法
                await dbContext.Database.MigrateAsync(cancellationToken);
                
                _logger.LogInformation("数据库迁移执行完成，数据库结构已是最新版本：{TargetDatabase}", targetDatabase);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("数据库连接检查被取消");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接或迁移失败，系统无法正常启动");
                throw;
            }
        }

        #endregion

        #region 种子数据初始化

        /// <summary>
        /// 初始化系统种子数据
        /// 该方法处理Excel种子数据文件，批量导入系统基础数据
        /// </summary>
        /// <param name="services">服务提供者，用于获取数据库上下文</param>
        /// <remarks>
        /// 种子数据处理流程：
        /// 1. 清空权限表以确保权限数据的完整性（PlPermissions表特殊处理）
        /// 2. 读取系统资源目录下的Excel文件（系统资源\预初始化数据.xlsx中已存在的表）
        /// 3. 使用高性能JSON流处理Excel数据
        /// 4. 批量插入数据库，忽略已存在的数据避免重复
        /// 所有处理过程都有详细的日志记录和异常处理
        /// </remarks>
        private void InitializeSeedData(IServiceProvider services)
        {
            try
            {
                // 直接使用范围服务中的数据库上下文，由DI容器管理生命周期
                var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();
                
                _logger.LogInformation("开始初始化系统种子数据");

                // 权限表特殊处理：清空重建确保数据完整性
                try
                {
                    dbContext.TruncateTable("PlPermissions");
                    _logger.LogInformation("已清空权限表，准备重新导入权限数据");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "清空权限表时发生错误，继续执行其他初始化");
                }

                // 读取系统资源目录，处理所有Excel种子数据文件
                var seedDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "数据库初始化数据");
                if (!Directory.Exists(seedDataDirectory))
                {
                    _logger.LogWarning("系统资源目录不存在：{Directory}，跳过种子数据初始化", seedDataDirectory);
                    return;
                }

                var excelFiles = Directory.GetFiles(seedDataDirectory, "*.xlsx", SearchOption.TopDirectoryOnly);

                using var jsonStream = new MemoryStream(1024 * 1024);   // 使用JSON流方式处理Excel数据，性能优异且内存占用低，且应在多个工作表中使用

                _logger.LogInformation("发现{fileCount}个Excel种子数据文件，开始逐个处理", excelFiles.Length);
                foreach (var excelFilePath in excelFiles)
                {
                    try
                    {
                        _logger.LogInformation("开始处理Excel种子数据文件: {FilePath}", excelFilePath);
                        using var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using var workbook = new XSSFWorkbook(fileStream);

                        var dbType = typeof(PowerLmsUserDbContext);
                        int processedSheets = 0;
                        int totalInserted = 0;

                        _logger.LogInformation("Excel文件包含{totalSheets}个工作表，开始逐个处理", workbook.NumberOfSheets);

                        // 遍历所有工作表，每个工作表对应一个数据库实体
                        for (int i = 0; i < workbook.NumberOfSheets; i++)
                        {
                            var sheet = workbook.GetSheetAt(i);
                            var sheetName = sheet.SheetName;

                            try
                            {
                                // 根据工作表名查找对应的DbSet属性
                                var dbSetProperty = dbType.GetProperty(sheetName);
                                if (dbSetProperty?.PropertyType?.IsGenericType != true ||
                                    dbSetProperty.PropertyType.GetGenericTypeDefinition() != typeof(DbSet<>) ||
                                    dbSetProperty.GetValue(dbContext) == null)
                                {
                                    _logger.LogWarning("跳过工作表：{sheetName}，未找到对应的DbSet或DbSet无效", sheetName);
                                    continue;
                                }

                                // 获取实体类型并处理数据
                                var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
                                jsonStream.SetLength(0); // 清空JSON流以准备写入新数据
                                OwNpoiUnit.WriteJsonToStream(sheet, 0, jsonStream);

                                if (jsonStream.Length == 0)
                                {
                                    _logger.LogInformation("工作表{sheetName}无数据，跳过处理", sheetName);
                                    continue;
                                }

                                // JSON反序列化为实体集合，使用企业级配置处理Excel数据类型转换
                                jsonStream.Position = 0;
                                var jsonOptions = CreateExcelDataJsonOptions();
                                var collectionType = typeof(IEnumerable<>).MakeGenericType(entityType);
                                var entities = JsonSerializer.Deserialize(jsonStream, collectionType, jsonOptions) as IEnumerable;

                                if (entities == null)
                                {
                                    _logger.LogWarning("工作表{sheetName}数据反序列化失败，跳过处理", sheetName);
                                    continue;
                                }

                                // 批量插入数据库，忽略已存在的数据
                                var insertedCount = OwDataUnit.BulkInsert(entities, dbContext, entityType, ignoreExisting: true);
                                totalInserted += insertedCount;
                                processedSheets++;

                                _logger.LogInformation("成功处理工作表：{sheetName}，插入{insertedCount}条记录", sheetName, insertedCount);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "处理工作表{sheetName}时发生错误，继续处理其他工作表", sheetName);
                                // 不抛出异常，继续处理其他工作表
                            }
                        }

                        // 保存所有更改并记录结果
                        var affectedRows = dbContext.SaveChanges();
                        _logger.LogInformation("Excel种子数据处理完成：文件{FilePath}，成功处理{processedSheets}/{totalSheets}个工作表，插入{totalInserted}条记录，数据库影响{affectedRows}行",
                            excelFilePath, processedSheets, workbook.NumberOfSheets, totalInserted, affectedRows);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理Excel种子数据文件{FilePath}时发生严重错误", excelFilePath);
                        throw;
                    }
                }

                _logger.LogInformation("所有Excel种子数据文件处理完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "系统种子数据初始化过程中发生错误");
                throw; // 种子数据初始化失败应该阻止系统启动
            }
        }

        #endregion

        #region 数据库基础配置初始化

        /// <summary>
        /// 初始化数据库基础配置数据
        /// </summary>
        /// <param name="services">服务提供者</param>
        /// <remarks>
        /// 重构说明：
        /// - 所有基础配置数据（TaxInvoiceChannels、SubjectConfigurations等）已迁移到Excel种子数据文件
        /// - Excel文件路径：系统资源\预初始化数据.xlsx
        /// - 对应工作表：TaxInvoiceChannels、SubjectConfigurations、PlPermissions等
        /// - 系统启动时会自动从Excel文件导入这些数据，无需硬编码初始化
        /// - 此方法保留用于将来可能需要的特殊初始化逻辑（如动态生成的配置）
        /// </remarks>
        private void InitDb(IServiceProvider services)
        {
            try
            {
                // 直接使用范围服务中的数据库上下文，由DI容器管理生命周期
                var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();

                _logger.LogInformation("基础配置数据已通过Excel种子数据文件管理，无需硬编码初始化");
                _logger.LogInformation("如需修改基础配置数据，请编辑：系统资源\\预初始化数据.xlsx");

                // 检查关键配置表是否已有数据（作为验证）
                var hasTaxChannels = dbContext.TaxInvoiceChannels.Any();
                var hasSubjectConfigs = dbContext.SubjectConfigurations.Any();
                var hasPermissions = dbContext.PlPermissions.Any();

                if (hasTaxChannels && hasSubjectConfigs && hasPermissions)
                {
                    _logger.LogInformation("验证成功：基础配置数据已正确从Excel种子数据导入");
                }
                else
                {
                    _logger.LogWarning("基础配置数据可能未完整导入，请检查Excel种子数据文件：" +
                        "TaxInvoiceChannels={HasTaxChannels}, SubjectConfigurations={HasSubjectConfigs}, PlPermissions={HasPermissions}",
                        hasTaxChannels, hasSubjectConfigs, hasPermissions);
                }

                // 此处可以添加将来需要的动态初始化逻辑
                // 例如：基于运行时环境的特殊配置、依赖外部服务的动态数据等
                // 注意：常规的基础数据应该继续通过Excel文件管理，而不是在此处硬编码
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "基础配置数据初始化时发生错误");
                // 基础配置错误通常不应该阻止系统启动，仅记录警告
            }
        }

        #endregion

        #region 系统管理

        /// <summary>
        /// 创建或更新超级管理员账户
        /// 确保系统始终有一个可用的超级管理员账户用于系统管理
        /// </summary>
        /// <param name="services">服务提供者，用于获取数据库上下文</param>
        /// <remarks>
        /// 超级管理员账户说明：
        /// 1. 登录名使用固定的GUID，确保全局唯一性
        /// 2. 如果账户不存在则创建新账户，如果存在则更新状态
        /// 3. 默认语言设置为中文（zh-CN）
        /// 4. 账户状态设置为4（激活状态）
        /// 5. 密码使用固定的GUID进行加密存储
        /// </remarks>
        private void CreateSuperAdministrator(IServiceProvider services)
        {
            try
            {
                // 直接使用范围服务中的数据库上下文，由DI容器管理生命周期
                var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();

                var admin = dbContext.Accounts.FirstOrDefault(c => c.LoginName == SuperAdminLoginName);

                if (admin == null)
                {
                    // 创建新的超级管理员账户
                    admin = new Account
                    {
                        LoginName = SuperAdminLoginName,
                        CurrentLanguageTag = "zh-CN", // 默认中文环境
                        LastModifyDateTimeUtc = OwHelper.WorldNow,
                        State = 4, // 激活状态
                    };
                    dbContext.Accounts.Add(admin);
                    _logger.LogInformation("创建新的超级管理员账户，登录名：{LoginName}", SuperAdminLoginName);
                }
                else
                {
                    // 更新现有账户状态
                    admin.State = 4;
                    admin.LastModifyDateTimeUtc = OwHelper.WorldNow;
                    _logger.LogInformation("更新现有超级管理员账户状态，确保账户处于激活状态");
                }

                // 设置密码（使用固定GUID进行加密）
                admin.SetPwd("1D381427-86BB-4D88-8CB0-5D92F8E1BADF");

                dbContext.SaveChanges();
                _logger.LogInformation("超级管理员账户配置完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建或更新超级管理员账户时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 清理数据库中的无效关联关系数据
        /// 定期维护数据完整性，删除因为主表记录删除而变成孤儿的关联关系记录
        /// </summary>
        /// <param name="services">服务提供者，用于获取数据库上下文</param>
        /// <remarks>
        /// 清理的关联关系包括：
        /// 1. 用户-角色关系（PlAccountRoles）：清理引用不存在用户或角色的记录
        /// 2. 角色-权限关系（PlRolePermissions）：清理引用不存在角色或权限的记录
        /// 3. 用户-机构关系（AccountPlOrganizations）：清理引用不存在用户或机构的记录
        /// 清理过程使用事务确保数据一致性，并记录详细的操作日志
        /// </remarks>
        private void CleanupInvalidRelationships(IServiceProvider services)
        {
            try
            {
                // 直接使用范围服务中的数据库上下文，由DI容器管理生命周期
                var dbContext = services.GetRequiredService<PowerLmsUserDbContext>();
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                _logger.LogInformation("开始清理数据库中的无效关联关系数据");

                var totalRemoved = 0;

                // 清理无效的用户-角色关系（用户或角色不存在的关联记录）
                var invalidUserRoles = dbContext.PlAccountRoles
                    .Where(ur => !dbContext.Accounts.Any(u => u.Id == ur.UserId) ||
                                 !dbContext.PlRoles.Any(r => r.Id == ur.RoleId))
                    .ToList();
                if (invalidUserRoles.Count > 0)
                {
                    dbContext.PlAccountRoles.RemoveRange(invalidUserRoles);
                    totalRemoved += invalidUserRoles.Count;
                    _logger.LogInformation("清理无效用户-角色关系：{count}条记录", invalidUserRoles.Count);
                }

                // 清理无效的角色-权限关系（角色或权限不存在的关联记录）
                var invalidRolePermissions = dbContext.PlRolePermissions
                    .Where(rp => !dbContext.PlRoles.Any(r => r.Id == rp.RoleId) ||
                                 !dbContext.PlPermissions.Any(p => p.Name == rp.PermissionId))
                    .ToList();
                if (invalidRolePermissions.Count > 0)
                {
                    dbContext.PlRolePermissions.RemoveRange(invalidRolePermissions);
                    totalRemoved += invalidRolePermissions.Count;
                    _logger.LogInformation("清理无效角色-权限关系：{count}条记录", invalidRolePermissions.Count);
                }

                // 清理无效的用户-机构关系（用户或机构不存在的关联记录）
                var invalidUserOrgs = dbContext.AccountPlOrganizations
                    .Where(uo => !dbContext.Accounts.Any(u => u.Id == uo.UserId) ||
                                (!dbContext.PlOrganizations.Any(o => o.Id == uo.OrgId) &&
                                 !dbContext.Merchants.Any(c => c.Id == uo.OrgId)))
                    .ToList();
                if (invalidUserOrgs.Count > 0)
                {
                    dbContext.AccountPlOrganizations.RemoveRange(invalidUserOrgs);
                    totalRemoved += invalidUserOrgs.Count;
                    _logger.LogInformation("清理无效用户-机构关系：{count}条记录", invalidUserOrgs.Count);
                }

                // 提交所有更改并记录结果
                var actualRemoved = dbContext.SaveChanges();
                stopwatch.Stop();

                _logger.LogInformation("无效关系数据清理完成：计划删除{totalRemoved}条记录，实际影响数据库{actualRemoved}行，总耗时{elapsed}毫秒",
                    totalRemoved, actualRemoved, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理无效关联关系时发生错误");
                // 清理操作失败不应该阻止系统启动，仅记录错误
            }
        }

        #endregion

        #region Excel数据类型转换支持

        /// <summary>
        /// 创建专用于Excel数据反序列化的JSON配置选项
        /// 使用.NET 6内置功能处理Excel中数字类型与实体字符串属性的类型转换问题
        /// </summary>
        /// <returns>JSON序列化选项</returns>
        private static JsonSerializerOptions CreateExcelDataJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                // 核心配置：允许从字符串读取数字，处理Excel数据类型混合的问题
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                // 编码器配置：处理中文字符等特殊字符
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        #endregion
    }
}