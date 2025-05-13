/*
 * ����: OW
 * ��������: 2023-10-20
 * �޸�����: 2023-10-20
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
    /// ��ɾ������ʱ������˵������á����롢���㼰���ǵ��Ӷ��󣬲�����������Ĺ���Id��Ϊnull��
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
        /// ���캯������ʼ����־��¼����
        /// </summary>
        /// <param name="logger">��־��¼����</param>
        public DeletedTriggerHandler(ILogger<DeletedTriggerHandler> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Priority => 10;

        /// <summary>
        /// ��ɾ������ʱ������˵������á����롢���㼰���ǵ��Ӷ��󣬲�����������Ĺ���Id��Ϊnull��
        /// </summary>
        /// <param name="entities">��ǰʵ����Ŀ���ϡ�</param>
        /// <param name="service">�����ṩ�ߡ�</param>
        /// <param name="states">״̬�ֵ䡣</param>
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
            var relatedFees = dbContext.Set<DocFee>().Where(f => f.BillId == deletedBill.Id).ToList();  // ��ȡ������ط���
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
            // ����������Ӷ�����Ҫ������������������߼�
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
