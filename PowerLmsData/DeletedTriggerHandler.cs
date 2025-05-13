/*
 * 作者: OW
 * 创建日期: 2023-10-20
 * 修改日期: 2023-10-20
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLmsServer.Triggers
{
    /// <summary>
    /// 在删除操作时，监控账单、费用、申请、结算及它们的子对象，并将关联对象的关联Id置为null。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocBill>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFee>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeRequisitionItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlInvoicesItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeRequisition>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlInvoices>))]
    public class DeletedTriggerHandler : IDbContextSaving<DocBill>, IDbContextSaving<DocFee>, IDbContextSaving<DocFeeRequisitionItem>, IDbContextSaving<PlInvoicesItem>, IDbContextSaving<DocFeeRequisition>, IDbContextSaving<PlInvoices>
    {
        private readonly ILogger<DeletedTriggerHandler> _Logger;

        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public DeletedTriggerHandler(ILogger<DeletedTriggerHandler> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Priority => 10;

        /// <summary>
        /// 在删除操作时，监控账单、费用、申请、结算及它们的子对象，并将关联对象的关联Id置为null。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="service">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            var dbContext = entities.First().Context;
            foreach (var entry in entities)
            {
                if (entry.State == EntityState.Deleted)
                {
                    switch (entry.Entity)
                    {
                        case DocBill deletedBill:
                            HandleDocBillDeletion(dbContext, deletedBill);
                            break;
                        case DocFee deletedFee:
                            HandleDocFeeDeletion(dbContext, deletedFee);
                            break;
                        case DocFeeRequisitionItem deletedRequisitionItem:
                            HandleDocFeeRequisitionItemDeletion(dbContext, deletedRequisitionItem);
                            break;
                        case PlInvoicesItem deletedInvoicesItem:
                            HandlePlInvoicesItemDeletion(dbContext, deletedInvoicesItem);
                            break;
                        case DocFeeRequisition deletedRequisition:
                            HandleDocFeeRequisitionDeletion(dbContext, deletedRequisition);
                            break;
                        case PlInvoices deletedInvoices:
                            HandlePlInvoicesDeletion(dbContext, deletedInvoices);
                            break;
                    }
                }
            }
        }

        private void HandleDocBillDeletion(DbContext dbContext, DocBill deletedBill)
        {
            var relatedFees = dbContext.Set<DocFee>().Where(f => f.BillId == deletedBill.Id).ToList();  // 获取所有相关费用
            foreach (var fee in relatedFees)
            {
                if (dbContext.Entry(fee).State != EntityState.Deleted)
                {
                    fee.BillId = null;
                }
            }
        }

        private void HandleDocFeeDeletion(DbContext dbContext, DocFee deletedFee)
        {
            var relatedRequisitionItems = dbContext.Set<DocFeeRequisitionItem>().Where(ri => ri.FeeId == deletedFee.Id).ToList();
            foreach (var requisitionItem in relatedRequisitionItems)
            {
                if (dbContext.Entry(requisitionItem).State != EntityState.Deleted)
                {
                    requisitionItem.FeeId = null;
                }
            }
        }

        private void HandleDocFeeRequisitionItemDeletion(DbContext dbContext, DocFeeRequisitionItem deletedRequisitionItem)
        {
            var relatedInvoicesItems = dbContext.Set<PlInvoicesItem>().Where(ii => ii.RequisitionItemId == deletedRequisitionItem.Id).ToList();
            foreach (var invoicesItem in relatedInvoicesItems)
            {
                if (dbContext.Entry(invoicesItem).State != EntityState.Deleted)
                {
                    invoicesItem.RequisitionItemId = null;
                }
            }
        }

        private void HandlePlInvoicesItemDeletion(DbContext dbContext, PlInvoicesItem deletedInvoicesItem)
        {
            // 如果有其他子对象需要处理，可以在这里添加逻辑
        }

        private void HandleDocFeeRequisitionDeletion(DbContext dbContext, DocFeeRequisition deletedRequisition)
        {
            var relatedItems = dbContext.Set<DocFeeRequisitionItem>().Where(ri => ri.ParentId == deletedRequisition.Id).ToList();
            foreach (var item in relatedItems)
            {
                if (dbContext.Entry(item).State != EntityState.Deleted)
                {
                    dbContext.Remove(item);
                }
            }
        }

        private void HandlePlInvoicesDeletion(DbContext dbContext, PlInvoices deletedInvoices)
        {
            var relatedItems = dbContext.Set<PlInvoicesItem>().Where(ii => ii.ParentId == deletedInvoices.Id).ToList();
            foreach (var item in relatedItems)
            {
                if (dbContext.Entry(item).State != EntityState.Deleted)
                {
                    dbContext.Remove(item);
                }
            }
        }
    }
}
