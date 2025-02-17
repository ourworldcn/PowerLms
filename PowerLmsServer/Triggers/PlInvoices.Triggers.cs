﻿/*
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
using PowerLmsServer.Managers;

namespace PowerLmsServer.Triggers
{
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

    /// <summary>
    /// 在 PlInvoicesItem 和 PlInvoices 添加/更改时触发相应处理的类，并在保存 PlInvoicesItem 和 PlInvoices 后，更新父级结算单金额的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlInvoicesItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlInvoices>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlInvoicesItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<PlInvoices>))]
    public class PlInvoicesItemTriggerHandler : IDbContextSaving<PlInvoicesItem>, IDbContextSaving<PlInvoices>, IAfterDbContextSaving<PlInvoicesItem>, IAfterDbContextSaving<PlInvoices>
    {
        private readonly ILogger<PlInvoicesItemTriggerHandler> _Logger;
        private readonly FinancialManager _FinancialManager;
        BusinessLogicManager _BusinessLogic;

        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="businessLogic"></param>
        /// <param name="invoiceManager">结算单管理器。</param>
        public PlInvoicesItemTriggerHandler(ILogger<PlInvoicesItemTriggerHandler> logger, BusinessLogicManager businessLogic, FinancialManager invoiceManager)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _BusinessLogic = businessLogic;
            _FinancialManager = invoiceManager;
        }

        #region Saving 方法
        /// <summary>
        /// 在 PlInvoicesItem 和 PlInvoices 添加/更改时，将其 ParentId（如果不为空）放在 HashSet 中。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="service">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            if (!states.TryGetValue(InvoiceTriggerConstants.ChangedInvoiceItemIdsKey, out var obj) || obj is not HashSet<Guid> parentIds)
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

        #region AfterSaving 方法
        /// <summary>
        /// 在保存 PlInvoicesItem 和 PlInvoices 后，从 HashSet 中获取父结算单的 ID，计算并更新其金额。
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (states.TryGetValue(InvoiceTriggerConstants.ChangedInvoiceItemIdsKey, out var obj) && obj is HashSet<Guid> parentIds)
            {
                var invoices = dbContext.Set<PlInvoices>().Where(c => parentIds.Contains(c.Id)).ToArray(); // 加载所有用到的 PlInvoices 对象
                var lkupInvoiceItem = dbContext.Set<PlInvoicesItem>().Where(c => parentIds.Contains(c.ParentId.Value)).AsEnumerable().
                    ToLookup(c => c.ParentId.Value); // 加载所有用到的 PlInvoicesItem 对象

                foreach (var invoice in invoices)
                {
                    var items = lkupInvoiceItem[invoice.Id];
                    if (_FinancialManager.GetInvoiceAmountAndIO(items, out decimal amount, out bool isOut, dbContext))
                    {
                        invoice.Amount = amount;
                        invoice.IO = isOut;
                        dbContext.Update(invoice);
                    }
                }
            }
        }
        #endregion AfterSaving 方法
    }
}
