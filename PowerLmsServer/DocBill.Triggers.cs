/*
 * DocBill.Triggers.cs 文件
 * 这个文件包含 ExchangeRateService、BaseCurrencyService、DocFeeSavingTrigger、DocBillAmountUpdater 以及 DocTriggerConstants 类。
 * 作者: OW
 * 创建日期: 2025-02-10
 * 修改日期: 2025-02-10
 */
/*
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.Data;
using PowerLmsServer.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLms.Data
{
    /// <summary>
    /// 汇率和本币编码服务类，提供获取汇率和本币编码的功能。
    /// </summary>
    public static class CombinedServices
    {
        /// <summary>
        /// 获取汇率。
        /// </summary>
        /// <param name="dbContext">数据库上下文。</param>
        /// <param name="sourceCurrency">源货币代码。</param>
        /// <param name="targetCurrency">目标货币代码。</param>
        /// <returns>汇率。</returns>
        public static decimal GetExchangeRate(this DbContext dbContext, string sourceCurrency, string targetCurrency)
        {
            var currentTime = OwHelper.WorldNow;

            var exchangeRate = dbContext.Set<PlExchangeRate>()
                .Where(r => r.SCurrency == sourceCurrency
                            && r.DCurrency == targetCurrency
                            && r.BeginDate <= currentTime
                            && r.EndData >= currentTime)
                .OrderByDescending(r => r.EndData)
                .FirstOrDefault();

            return exchangeRate?.Exchange ?? 1;
        }

        /// <summary>
        /// 获取本币编码。
        /// </summary>
        /// <param name="dbContext">数据库上下文。</param>
        /// <param name="jobId">工作唯一标识。</param>
        /// <param name="merchantManager">商户管理器。</param>
        /// <param name="organizationManager">机构管理器。</param>
        /// <returns>本币编码。</returns>
        public static string GetBaseCurrencyCode(this DbContext dbContext, Guid jobId, MerchantManager merchantManager, OrganizationManager organizationManager)
        {
            var job = dbContext.Set<PlJob>().Find(jobId);
            if (job == null)
            {
                throw new Exception($"Job with Id {jobId} not found.");
            }

            if (merchantManager.TryGetIdByOrgOrMerchantId(job.OrgId.Value, out var merchantId))
            {
                var organization = organizationManager.LoadById(merchantId.Value);
                while (organization?.ParentId != null)
                {
                    organization = organizationManager.LoadById(organization.ParentId.Value);
                }

                if (organization == null)
                {
                    throw new Exception($"Root organization for JobId {jobId} not found.");
                }

                return organization.BaseCurrencyCode;
            }

            throw new Exception($"Merchant for OrgId {job.OrgId} not found.");
        }
    }

    /// <summary>
    /// 定义与文档触发器相关的常量。
    /// </summary>
    public static class DocTriggerConstants
    {
        /// <summary>
        /// 已更改文档明细的键。
        /// </summary>
        public const string ChangedDocFeeIdsKey = "ChangedDocFeeIds";
    }

    /// <summary>
    /// 在 DocFee 和 DocBill 添加/更改时触发相应处理的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFee>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocBill>))]
    public class DocFeeSavingTrigger : IDbContextSaving<DocFee>, IDbContextSaving<DocBill>
    {
        private readonly ILogger<DocFeeSavingTrigger> _logger;

        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public DocFeeSavingTrigger(ILogger<DocFeeSavingTrigger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 在 DocFee 和 DocBill 添加/更改时，将其 ParentId（如果不为空）放在 HashSet 中。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            if (!states.TryGetValue(DocTriggerConstants.ChangedDocFeeIdsKey, out var obj) || !(obj is HashSet<Guid> parentIds))
            {
                parentIds = new HashSet<Guid>();
                states[DocTriggerConstants.ChangedDocFeeIdsKey] = parentIds;
            }

            foreach (var entry in entities)
            {
                var id = entry.Entity switch
                {
                    DocFee df => df.ParentId,
                    DocBill db when entry.State == EntityState.Added || entry.State == EntityState.Modified => db.Id,
                    _ => null,
                };
                if (id.HasValue)
                {
                    parentIds.Add(id.Value);
                }
            }
        }
    }

    /// <summary>
    /// 在保存 DocFee 和 DocBill 后，更新父级结算单金额的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFee>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocBill>))]
    public class DocBillAmountUpdater : IAfterDbContextSaving<DocFee>, IAfterDbContextSaving<DocBill>
    {
        private readonly ILogger<DocBillAmountUpdater> _logger;

        /// <summary>
        /// 构造函数，初始化日志记录器和汇率服务。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public DocBillAmountUpdater(ILogger<DocBillAmountUpdater> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 在保存 DocFee 和 DocBill 后，从 HashSet 中获取父结算单的 ID，计算并更新其金额。
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            var merchantManager = serviceProvider.GetRequiredService<MerchantManager>();
            var organizationManager = serviceProvider.GetRequiredService<OrganizationManager>();

            if (states.TryGetValue(DocTriggerConstants.ChangedDocFeeIdsKey, out var obj) && obj is HashSet<Guid> parentIds)
            {
                var docBillUpdates = dbContext.Set<DocFee>()
                    .Where(item => parentIds.Contains(item.ParentId.Value))
                    .GroupBy(item => item.ParentId)
                    .Select(group => new
                    {
                        ParentId = group.Key,
                        TotalAmount = group.Sum(item =>
                        {
                            var docBill = dbContext.Set<DocBill>().Find(group.Key);
                            var baseCurrencyCode = dbContext.GetBaseCurrencyCode(docBill.JobId.Value, merchantManager, organizationManager);
                            var exchangeRate = dbContext.GetExchangeRate(item.Currency, baseCurrencyCode == docBill.Currency ? baseCurrencyCode : docBill.Currency);
                            return Math.Round(item.Amount * exchangeRate, 4, MidpointRounding.AwayFromZero);
                        })
                    })
                    .ToList();

                foreach (var update in docBillUpdates)
                {
                    var docBill = dbContext.Find<DocBill>(update.ParentId);
                    if (docBill != null)
                    {
                        docBill.Amount = update.TotalAmount;
                        dbContext.Update(docBill);
                    }
                }
            }
        }
    }
}
*/