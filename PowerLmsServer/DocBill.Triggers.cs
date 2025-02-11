using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.EntityFrameworkCore;
using PowerLmsServer;
using PowerLmsServer.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLms.Data
{
    /// <summary>
    /// 提供获取汇率和本币编码的服务类，以及定义与文档触发器相关的常量。
    /// </summary>
    public static class CombinedServices
    {
        /// <summary>
        /// 已更改文档明细的键。
        /// </summary>
        public const string ChangedDocFeeIdsKey = "ChangedDocFeeIds";

        /// <summary>
        /// 获取汇率。
        /// </summary>
        /// <param name="dbContext">数据库上下文。</param>
        /// <param name="billId">账单ID。</param>
        /// <param name="sourceCurrency">源货币代码。</param>
        /// <param name="targetCurrency">目标货币代码。</param>
        /// <param name="merchantManager">商户管理器。</param>
        /// <param name="organizationManager">组织机构管理器。</param>
        /// <returns>汇率。</returns>
        public static decimal GetExchangeRate(this DbContext dbContext, Guid billId, string sourceCurrency, string targetCurrency, MerchantManager merchantManager, OrganizationManager organizationManager)
        {
            // 获取 DocFee 对象
            var docFee = dbContext.Set<DocFee>().FirstOrDefault(df => df.BillId == billId);
            if (docFee == null)
            {
                throw new Exception($"DocFee with BillId {billId} not found.");
            }

            // 获取 PlJob 对象
            var job = dbContext.Set<PlJob>().Find(docFee.JobId);
            if (job == null)
            {
                throw new Exception($"Job with Id {docFee.JobId} not found.");
            }

            // 获取机构对象
            if (!merchantManager.TryGetIdByOrgOrMerchantId(job.OrgId.Value, out var organizationId))
            {
                throw new Exception($"Organization for JobId {job.Id} not found.");
            }

            var orgsCacheItem = organizationManager.GetOrgsCacheItemByMerchantId(organizationId.Value);
            if (orgsCacheItem == null || !orgsCacheItem.Data.TryGetValue(organizationId.Value, out var organization))
            {
                throw new Exception($"Organization with Id {organizationId.Value} not found.");
            }

            // 获取 baseCurrencyCode
            var baseCurrencyCode = organization.BaseCurrencyCode;

            // 获取汇率
            var currentTime = OwHelper.WorldNow;
            var exchangeRate = dbContext.Set<PlExchangeRate>()
                .Where(r => r.SCurrency == sourceCurrency
                            && r.DCurrency == targetCurrency
                            && r.BeginDate <= currentTime
                            && r.EndData >= currentTime) // 保留拼写错误
                .OrderByDescending(r => r.EndData) // 保留拼写错误
                .FirstOrDefault();

            return exchangeRate?.Exchange ?? 1;
        }
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
        /// 在 DocFee 和 DocBill 添加/更改时，将其 BillId（如果不为空）放在 HashSet 中。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            if (!states.TryGetValue(CombinedServices.ChangedDocFeeIdsKey, out var obj) || obj is not HashSet<Guid> billIds)
            {
                billIds = new HashSet<Guid>();
                states[CombinedServices.ChangedDocFeeIdsKey] = billIds;
            }

            foreach (var entry in entities)
            {
                var id = entry.Entity switch
                {
                    DocFee df => df.BillId,
                    DocBill db when entry.State == EntityState.Added || entry.State == EntityState.Modified => db.Id,
                    _ => null,
                };
                if (id.HasValue)
                {
                    billIds.Add(id.Value);
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
        /// 构造函数，初始化日志记录器。
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
            if (states.TryGetValue(CombinedServices.ChangedDocFeeIdsKey, out var obj) && obj is HashSet<Guid> billIds)
            {
                var dicBill = dbContext.Set<DocBill>().Where(c => billIds.Contains(c.Id)).AsEnumerable().ToDictionary(c => c.Id); // 加载所有用到的 DocBill 对象
                var lkupFee = dbContext.Set<DocFee>().Where(c => billIds.Contains(c.BillId.Value)).AsEnumerable().
                    ToLookup(c => c.BillId.Value); // 加载所有用到的 DocFee 对象

                foreach (var bill in dicBill.Values)
                {
                    bill.Amount = lkupFee[bill.Id].Sum(c => Math.Round(c.Amount * c.ExchangeRate, 4, MidpointRounding.AwayFromZero));
                    dbContext.Update(bill);
                }
            }
        }
    }
}
