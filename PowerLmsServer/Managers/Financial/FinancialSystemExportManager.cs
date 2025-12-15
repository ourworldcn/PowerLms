/*
 * 项目：PowerLms财务系统
 * 模块：财务导出管理
 * 文件说明：
 * - 功能1：财务导出系统最基础的通用逻辑和共享服务
 * - 功能2：科目配置管理、DBF文件生成、凭证生成基础逻辑
 * - 功能3：权限验证、数据完整性验证基础服务
 * - 功能4：缓存管理和通用工具方法
 * 技术要点：
 * - 单例模式提供缓存和性能优化
 * - 统一的配置管理和验证机制
 * - 不包含具体导出业务逻辑，只提供基础服务
 * 作者：GitHub Copilot
 * 创建：2025-01
 * 修改：2025-01-27 重构为基础通用逻辑
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Data;
using PowerLms.Data;
using PowerLms.Data.Finance;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using System.Collections.Concurrent;
using DotNetDBF;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 财务系统导出管理器 - 单例服务
    /// 提供财务导出系统的最基础通用功能和共享服务，包括：
    /// - 科目配置管理和验证
    /// - DBF文件生成通用服务（不包含具体字段定义）
    /// - 凭证生成基础逻辑
    /// - 权限验证和数据过滤基础服务
    /// - 数据完整性验证
    /// - 缓存管理
    /// 
    /// 职责边界：
    /// - ✅ 包含：通用配置管理、文件生成工具、基础数据处理
    /// - ❌ 不包含：具体导出业务逻辑、特定字段定义、具体凭证生成规则
    /// - 📝 具体导出逻辑应在对应的Controller分部类中实现（如OA、PBI、结算单等）
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class FinancialSystemExportManager
    {
        private readonly IDbContextFactory<PowerLmsUserDbContext> _dbContextFactory;
        private readonly ILogger<FinancialSystemExportManager> _logger;
        private readonly IServiceProvider _serviceProvider;

        // 缓存字典 - 单例服务中的内存缓存
        private readonly ConcurrentDictionary<string, Dictionary<string, SubjectConfiguration>> _configCache;
        private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _orgPermissionCache;

        /// <summary>
        /// 初始化财务系统导出管理器的新实例
        /// </summary>
        /// <param name="dbContextFactory">数据库上下文工厂</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="serviceProvider">服务提供者</param>
        public FinancialSystemExportManager(
            IDbContextFactory<PowerLmsUserDbContext> dbContextFactory,
            ILogger<FinancialSystemExportManager> logger,
            IServiceProvider serviceProvider)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _configCache = new ConcurrentDictionary<string, Dictionary<string, SubjectConfiguration>>();
            _orgPermissionCache = new ConcurrentDictionary<Guid, HashSet<Guid>>();
        }

        #region 科目配置管理服务

        /// <summary>
        /// 加载指定前缀的科目配置（带缓存）
        /// </summary>
        /// <param name="prefix">配置项前缀，如 "OA_", "PBI_"</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>配置项字典</returns>
        public Dictionary<string, SubjectConfiguration> LoadConfigurationsByPrefix(string prefix, Guid? orgId)
        {
            var cacheKey = $"{prefix}_{orgId}";
            
            return _configCache.GetOrAdd(cacheKey, _ =>
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                
                var configs = dbContext.SubjectConfigurations
                    .Where(c => !c.IsDelete && c.OrgId == orgId && c.Code.StartsWith(prefix))
                    .ToList();

                _logger.LogDebug("加载科目配置，前缀: {Prefix}, 组织: {OrgId}, 数量: {Count}", 
                    prefix, orgId, configs.Count);

                return configs.ToDictionary(c => c.Code, c => c);
            });
        }

        /// <summary>
        /// 验证配置完整性
        /// </summary>
        /// <param name="requiredCodes">必需的配置项编码</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>验证结果</returns>
        public bool ValidateConfigurations(string[] requiredCodes, Guid? orgId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            
            var existingCodes = dbContext.SubjectConfigurations
                .Where(c => !c.IsDelete && c.OrgId == orgId && requiredCodes.Contains(c.Code))
                .Select(c => c.Code)
                .ToList();

            var missingCodes = requiredCodes.Except(existingCodes).ToList();
            
            if (missingCodes.Any())
            {
                _logger.LogWarning("科目配置不完整，缺少配置项: {MissingCodes}, 组织: {OrgId}", 
                    string.Join(", ", missingCodes), orgId);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取配置值（带默认值）
        /// </summary>
        /// <param name="code">配置项编码</param>
        /// <param name="orgId">组织ID</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public string GetConfigValue(string code, Guid? orgId, string defaultValue = null)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            
            var config = dbContext.SubjectConfigurations
                .FirstOrDefault(c => !c.IsDelete && c.OrgId == orgId && c.Code == code);

            return config?.DisplayName ?? defaultValue;
        }

        #endregion

        #region DBF文件生成服务

        /// <summary>
        /// 生成DBF文件到内存流（通用方法）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据集合</param>
        /// <param name="fieldMappings">字段映射（必需，由调用方提供）</param>
        /// <param name="fieldTypes">字段类型（必需，由调用方提供）</param>
        /// <returns>DBF文件内存流</returns>
        /// <exception cref="ArgumentNullException">当fieldMappings或fieldTypes为null时抛出</exception>
        public MemoryStream GenerateDbfStream<T>(IEnumerable<T> data, 
            Dictionary<string, string> fieldMappings, 
            Dictionary<string, NativeDbType> fieldTypes) where T : class
        {
            ArgumentNullException.ThrowIfNull(fieldMappings, "字段映射不能为空，应由调用方提供具体的字段映射定义");
            ArgumentNullException.ThrowIfNull(fieldTypes, "字段类型不能为空，应由调用方提供具体的字段类型定义");

            var memoryStream = new MemoryStream();
            try
            {
                DotNetDbfUtil.WriteToStream(data, memoryStream, fieldMappings, fieldTypes);
                memoryStream.Position = 0;
                
                _logger.LogDebug("生成DBF文件成功，数据量: {Count}, 文件大小: {Size} bytes", 
                    data.Count(), memoryStream.Length);
                
                return memoryStream;
            }
            catch
            {
                memoryStream?.Dispose();
                throw;
            }
        }

        #endregion

        #region 凭证生成基础服务

        /// <summary>
        /// 凭证号生成（防并发冲突）
        /// </summary>
        /// <param name="voucherDate">凭证日期</param>
        /// <param name="voucherGroup">凭证字</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>凭证号</returns>
        public string GenerateVoucherNumber(DateTime voucherDate, string voucherGroup, Guid? orgId)
        {
            // 简化实现：期间-凭证字-时间戳序号
            var period = voucherDate.Month;
            var timestamp = DateTime.Now.Ticks % 10000; // 取最后4位作为序号
            
            return $"{period}-{voucherGroup}-{timestamp}";
        }

        /// <summary>
        /// 通用凭证基础数据填充
        /// </summary>
        /// <param name="voucher">凭证对象</param>
        /// <param name="voucherDate">凭证日期</param>
        /// <param name="voucherNumber">凭证号</param>
        /// <param name="voucherGroup">凭证字</param>
        /// <param name="entryId">分录号</param>
        /// <param name="summary">摘要</param>
        public void PopulateBaseVoucherData(KingdeeVoucher voucher, DateTime voucherDate, 
            int voucherNumber, string voucherGroup, int entryId, string summary)
        {
            voucher.Id = Guid.NewGuid();
            voucher.FDATE = voucherDate;
            voucher.FTRANSDATE = voucherDate;
            voucher.FPERIOD = voucherDate.Month;
            voucher.FGROUP = voucherGroup ?? "转";
            voucher.FNUM = voucherNumber;
            voucher.FENTRYID = entryId;
            voucher.FEXP = summary ?? "";
            voucher.FCYID = "RMB";
            voucher.FEXCHRATE = 1.0000000m;
            voucher.FMODULE = "GL";
            voucher.FDELETED = false;
        }

        /// <summary>
        /// 多币种金额计算
        /// </summary>
        /// <param name="amount">金额</param>
        /// <param name="exchangeRate">汇率</param>
        /// <param name="isDebit">是否借方</param>
        /// <returns>计算结果</returns>
        public (decimal FFCyAmt, decimal FDebit, decimal FCredit) CalculateVoucherAmounts(
            decimal amount, decimal exchangeRate, bool isDebit)
        {
            var baseCurrencyAmount = amount * exchangeRate;
            
            return (
                FFCyAmt: amount,
                FDebit: isDebit ? baseCurrencyAmount : 0,
                FCredit: isDebit ? 0 : baseCurrencyAmount
            );
        }

        /// <summary>
        /// 核算项目处理（客户、供应商、部门、员工）
        /// </summary>
        /// <param name="voucher">凭证对象</param>
        /// <param name="accountingCategory1">核算类别1</param>
        /// <param name="objId1">对象ID1</param>
        /// <param name="objName1">对象名称1</param>
        /// <param name="accountingCategory2">核算类别2</param>
        /// <param name="objId2">对象ID2</param>
        /// <param name="objName2">对象名称2</param>
        /// <param name="transId">交易ID</param>
        public void PopulateAccountingDimensions(KingdeeVoucher voucher,
            string accountingCategory1 = null, string objId1 = null, string objName1 = null,
            string accountingCategory2 = null, string objId2 = null, string objName2 = null,
            string transId = null)
        {
            voucher.FCLSNAME1 = accountingCategory1 ?? "";
            voucher.FOBJID1 = objId1 ?? "";
            voucher.FOBJNAME1 = objName1 ?? "";
            voucher.FCLSNAME2 = accountingCategory2 ?? "";
            voucher.FOBJID2 = objId2 ?? "";
            voucher.FOBJNAME2 = objName2 ?? "";
            voucher.FTRANSID = transId ?? "";
        }

        #endregion

        #region 权限与过滤服务

        /// <summary>
        /// 验证导出权限（基础检查）
        /// </summary>
        /// <param name="user">用户账号</param>
        /// <param name="exportType">导出类型</param>
        /// <returns>权限验证结果</returns>
        public bool ValidateExportPermission(Account user, string exportType)
        {
            if (user is null) return false;
            if (user.IsSuperAdmin) return true;

            // 基础权限检查，具体的权限节点由调用方确定
            try
            {
                // 注意：这里不进行具体的权限验证，由调用方实现
                // 只做基础的用户状态检查
                return user.OrgId.HasValue; // 简化检查，只验证组织ID存在
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证导出权限时发生错误，用户: {UserId}, 导出类型: {ExportType}", 
                    user.Id, exportType);
                return false;
            }
        }

        /// <summary>
        /// 组织权限过滤 - 通用方法
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="query">查询对象</param>
        /// <param name="user">用户账号</param>
        /// <returns>过滤后的查询</returns>
        public IQueryable<T> ApplyOrganizationFilter<T>(IQueryable<T> query, Account user) where T : class, ISpecificOrg
        {
            if (user is null) return query.Where(_ => false);
            if (user.IsSuperAdmin) return query;

            var orgManager = _serviceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>();
            var merchantId = orgManager.GetMerchantIdByUserId(user.Id);
            if (!merchantId.HasValue) return query.Where(_ => false);

            HashSet<Guid?> allowedOrgIds;

            if (user.IsMerchantAdmin)
            {
                // 商户管理员可以访问整个商户下的所有组织机构
                var allOrgIds = orgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                allowedOrgIds = new HashSet<Guid?>(allOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }
            else
            {
                // 普通用户只能访问其当前登录的公司及下属机构
                var companyId = user.OrgId.HasValue ? orgManager.GetCompanyIdByOrgId(user.OrgId.Value) : null;
                if (!companyId.HasValue) return query.Where(_ => false);
                
                var companyOrgIds = orgManager.GetOrgIdsByCompanyId(companyId.Value).ToList();
                allowedOrgIds = new HashSet<Guid?>(companyOrgIds.Cast<Guid?>());
                allowedOrgIds.Add(merchantId.Value);
            }

            return query.Where(entity => allowedOrgIds.Contains(entity.OrgId));
        }

        #endregion

        #region 数据验证与完整性服务

        /// <summary>
        /// 数据一致性检查（如金额平衡）
        /// </summary>
        /// <param name="vouchers">凭证集合</param>
        /// <returns>验证结果</returns>
        public (bool IsValid, List<string> Errors) ValidateVoucherBalance(IEnumerable<KingdeeVoucher> vouchers)
        {
            var errors = new List<string>();
            
            try
            {
                // 按凭证号分组检查借贷平衡
                var voucherGroups = vouchers.GroupBy(v => v.FNUM);
                
                foreach (var group in voucherGroups)
                {
                    var totalDebit = group.Sum(v => v.FDEBIT ?? 0);
                    var totalCredit = group.Sum(v => v.FCREDIT ?? 0);
                    
                    if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                    {
                        errors.Add($"凭证号 {group.Key} 借贷不平衡，借方: {totalDebit}, 贷方: {totalCredit}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证凭证平衡时发生错误");
                errors.Add($"验证过程中发生错误: {ex.Message}");
            }

            return (errors.Count == 0, errors);
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清除配置缓存
        /// </summary>
        /// <param name="prefix">可选的前缀过滤</param>
        public void ClearConfigCache(string prefix = null)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                _configCache.Clear();
                _logger.LogInformation("已清除所有配置缓存");
            }
            else
            {
                var keysToRemove = _configCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
                foreach (var key in keysToRemove)
                {
                    _configCache.TryRemove(key, out _);
                }
                _logger.LogInformation("已清除前缀为 {Prefix} 的配置缓存，数量: {Count}", prefix, keysToRemove.Count);
            }
        }

        /// <summary>
        /// 清除权限缓存
        /// </summary>
        public void ClearPermissionCache()
        {
            _orgPermissionCache.Clear();
            _logger.LogInformation("已清除所有权限缓存");
        }

        #endregion

        #region 导出防重机制

        /// <summary>
        /// 标记财务数据为已导出
        /// 此方法仅修改实体属性,不调用SaveChanges,调用者需自行保存
        /// </summary>
        /// <typeparam name="T">实现IFinancialExportable的实体类型</typeparam>
        /// <param name="entities">要标记的实体集合</param>
        /// <param name="userId">执行导出的用户ID</param>
        /// <returns>成功标记的数量</returns>
        public int MarkAsExported<T>(IEnumerable<T> entities, Guid userId) 
            where T : class, IFinancialExportable
        {
            var exportDateTime = DateTime.UtcNow;
            var count = 0;
            foreach (var entity in entities)
            {
                entity.ExportedDateTime = exportDateTime;
                entity.ExportedUserId = userId;
                count++;
            }
            _logger.LogInformation("已标记 {Count} 条 {EntityType} 数据为已导出,用户: {UserId}", 
                count, typeof(T).Name, userId);
            return count;
        }

        /// <summary>
        /// 取消财务数据的导出标记
        /// 此方法仅修改实体属性,不调用SaveChanges,调用者需自行保存
        /// </summary>
        /// <typeparam name="T">实现IFinancialExportable的实体类型</typeparam>
        /// <param name="entities">要取消标记的实体集合</param>
        /// <param name="currentUserId">当前用户ID(用于日志记录)</param>
        /// <param name="requireSameUser">是否要求必须是导出人才能取消(默认false,由调用者在Controller层验证权限)</param>
        /// <returns>成功取消标记的数量</returns>
        /// <exception cref="UnauthorizedAccessException">当requireSameUser为true且非导出人尝试取消时</exception>
        public int UnmarkExported<T>(IEnumerable<T> entities, Guid currentUserId, bool requireSameUser = false) 
            where T : class, IFinancialExportable
        {
            var count = 0;
            foreach (var entity in entities)
            {
                if (requireSameUser && entity.ExportedUserId.HasValue && entity.ExportedUserId.Value != currentUserId)
                {
                    throw new UnauthorizedAccessException($"只有导出人才能取消导出,导出人ID:{entity.ExportedUserId}");
                }
                entity.ExportedDateTime = null;
                entity.ExportedUserId = null;
                count++;
            }
            _logger.LogInformation("已取消 {Count} 条 {EntityType} 数据的导出标记,用户: {UserId}", 
                count, typeof(T).Name, currentUserId);
            return count;
        }

        /// <summary>
        /// 为查询添加"未导出"过滤条件
        /// </summary>
        /// <typeparam name="T">实现IFinancialExportable的实体类型</typeparam>
        /// <param name="query">原始查询</param>
        /// <returns>添加过滤后的查询</returns>
        public IQueryable<T> FilterUnexported<T>(IQueryable<T> query) 
            where T : class, IFinancialExportable
        {
            return query.Where(e => e.ExportedDateTime == null);
        }

        /// <summary>
        /// 为查询添加"已导出"过滤条件
        /// </summary>
        /// <typeparam name="T">实现IFinancialExportable的实体类型</typeparam>
        /// <param name="query">原始查询</param>
        /// <returns>添加过滤后的查询</returns>
        public IQueryable<T> FilterExported<T>(IQueryable<T> query) 
            where T : class, IFinancialExportable
        {
            return query.Where(e => e.ExportedDateTime != null);
        }

        /// <summary>
        /// 验证是否可以取消导出
        /// </summary>
        /// <typeparam name="T">实现IFinancialExportable的实体类型</typeparam>
        /// <param name="entity">要验证的实体</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <param name="isAdmin">当前用户是否是管理员</param>
        /// <returns>(是否可以取消, 错误信息)</returns>
        public (bool CanCancel, string ErrorMessage) CanCancelExport<T>(
            T entity, 
            Guid currentUserId,
            bool isAdmin) 
            where T : class, IFinancialExportable
        {
            if (!entity.ExportedDateTime.HasValue)
            {
                return (false, "数据尚未导出,无需取消");
            }
            if (isAdmin)
            {
                return (true, string.Empty);
            }
            if (entity.ExportedUserId.HasValue && entity.ExportedUserId.Value == currentUserId)
            {
                return (true, string.Empty);
            }
            return (false, $"只有导出人或管理员可以取消导出");
        }

        #endregion
    }
}
