/*
 * PlInvoices.Triggers.cs 文件
 * 这个文件包含了在 PlInvoicesItem 添加/更改时触发相应处理的类，以及更新父级结算单金额的类。
 * 作者: OW
 * 创建日期: 2025-02-10
 * 修改日期: 2025-02-10
 */

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PowerLms.Data;
using OW.EntityFrameworkCore;

namespace PowerLms.Data
{
    #region PlInvoicesItemSavingTrigger 类
    /// <summary>
    /// 在 PlInvoicesItem 和 PlInvoices 添加/更改时触发相应处理的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlInvoicesItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlInvoices>))]
    public class PlInvoicesItemSavingTrigger : IDbContextSaving<PlInvoicesItem>, IDbContextSaving<PlInvoices>
    {
        private readonly ILogger<PlInvoicesItemSavingTrigger> _Logger;

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public PlInvoicesItemSavingTrigger(ILogger<PlInvoicesItemSavingTrigger> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion 构造函数

        #region Saving 方法
        /// <summary>
        /// 在 PlInvoicesItem 和 PlInvoices 添加/更改时，将其 ParentId（如果不为空）放在 HashSet 中。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, Dictionary<object, object> states)
        {
            if (!states.TryGetValue(InvoiceTriggerConstants.ChangedInvoiceItemIdsKey, out var obj) || !(obj is HashSet<Guid> parentIds))
            {
                parentIds = new HashSet<Guid>();
                states[InvoiceTriggerConstants.ChangedInvoiceItemIdsKey] = parentIds;
            }

            foreach (var entry in entities)
            {
                var id = entry.Entity switch
                {
                    PlInvoicesItem pi => pi.ParentId,
                    PlInvoices pi when entry.State == EntityState.Added || entry.State == EntityState.Modified => pi.Id,
                    _ => null,
                };
                if (id.HasValue)
                {
                    parentIds.Add(id.Value);
                    _Logger.LogDebug("ParentId {ParentId} added to HashSet.", id.Value);
                }
            }
        }
        #endregion Saving 方法
    }
    #endregion PlInvoicesItemSavingTrigger 类

    #region InvoiceTriggerConstants 类
    /// <summary>
    /// 定义与结算单触发器相关的常量。
    /// </summary>
    public static class InvoiceTriggerConstants
    {
        /// <summary>
        /// 已更改结算单明细的键。
        /// </summary>
        public const string ChangedInvoiceItemIdsKey = "ChangedInvoiceItemIds";
    }
    #endregion InvoiceTriggerConstants 类

    #region PlInvoicesItemAmountUpdater 类
    /// <summary>
    /// 在保存 PlInvoicesItem 和 PlInvoices 后，更新父级结算单金额的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlInvoicesItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlInvoices>))]
    public class PlInvoicesItemAmountUpdater : IAfterDbContextSaving<PlInvoicesItem>, IAfterDbContextSaving<PlInvoices>
    {
        private readonly ILogger<PlInvoicesItemAmountUpdater> _Logger;

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public PlInvoicesItemAmountUpdater(ILogger<PlInvoicesItemAmountUpdater> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion 构造函数

        #region Saving 方法
        /// <summary>
        /// 在保存 PlInvoicesItem 和 PlInvoices 后，从 HashSet 中获取父结算单的 ID，计算并更新其金额。
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (states.TryGetValue(InvoiceTriggerConstants.ChangedInvoiceItemIdsKey, out var obj) && obj is HashSet<Guid> parentIds)
            {
                var invoiceUpdates = dbContext.Set<PlInvoicesItem>()
                    .Where(item => parentIds.Contains(item.ParentId.Value))
                    .Select(c => c).AsEnumerable()
                    .ToLookup(c => new
                    {
                        ParentId = c.ParentId.Value,
                        TotalAmount = c.Sum(item => Math.Round(item.ExchangeRate * item.Amount, 4, MidpointRounding.AwayFromZero))
                    })
                    .ToList();

                foreach (var update in invoiceUpdates)
                {
                    var invoice = dbContext.Find<PlInvoices>(update.ParentId);
                    if (invoice != null)
                    {
                        invoice.Amount = update.TotalAmount;
                        dbContext.Update(invoice);
                        _Logger.LogDebug("Updated invoice {InvoiceId} with total amount {TotalAmount}.", update.ParentId, update.TotalAmount);
                    }
                }
            }
        }
        #endregion Saving 方法
    }
    #endregion PlInvoicesItemAmountUpdater 类
}
