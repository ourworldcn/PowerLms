/*
 * PlInvoices.DocFeeRequisition.cs �ļ�
 * ����ļ��������� DocFeeRequisition �� DocFeeRequisitionItem ���/����ʱ������Ӧ������࣬�Լ�����������ݵ��ࡣ
 * ����: OW
 * ��������: 2025-02-10
 * �޸�����: 2025-02-10
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
    /// ���������뵥��������صĳ�����
    /// </summary>
    public static class DocFeeRequisitionTriggerConstants
    {
        /// <summary>
        /// �Ѹ������뵥��ϸ�ļ���
        /// </summary>
        public const string ChangedRequisitionItemIdsKey = "ChangedRequisitionItemIds";
    }

    /// <summary>
    /// �� DocFeeRequisition �� DocFeeRequisitionItem ���/����ʱ������Ӧ������࣬���ڱ��� DocFeeRequisition �� DocFeeRequisitionItem �󣬸���������ݵ��ࡣ
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeRequisition>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeRequisitionItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFeeRequisition>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFeeRequisitionItem>))]
    public class DocFeeRequisitionTriggerHandler : IDbContextSaving<DocFeeRequisition>, IDbContextSaving<DocFeeRequisitionItem>, IAfterDbContextSaving<DocFeeRequisition>, IAfterDbContextSaving<DocFeeRequisitionItem>
    {
        #region ˽���ֶ�
        private readonly ILogger<DocFeeRequisitionTriggerHandler> _Logger;
        private readonly IServiceProvider _ServiceProvider;
        #endregion ˽���ֶ�

        #region �ӳٻ�ȡ�ķ���
        private BusinessLogicManager _BusinessLogic => _ServiceProvider.GetRequiredService<BusinessLogicManager>();
        #endregion �ӳٻ�ȡ�ķ���

        #region ���캯��
        /// <summary>
        /// ���캯������ʼ����־��¼����
        /// </summary>
        /// <param name="logger">��־��¼����</param>
        /// <param name="serviceProvider">�����ṩ�ߡ�</param>
        public DocFeeRequisitionTriggerHandler(ILogger<DocFeeRequisitionTriggerHandler> logger, IServiceProvider serviceProvider)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        #endregion ���캯��

        #region Saving ����
        /// <summary>
        /// �� DocFeeRequisition �� DocFeeRequisitionItem ���/����ʱ������ ParentId�������Ϊ�գ����� HashSet �У������㸸���뵥�Ľ�
        /// </summary>
        /// <param name="entities">��ǰʵ����Ŀ���ϡ�</param>
        /// <param name="service">�����ṩ�ߡ�</param>
        /// <param name="states">״̬�ֵ䡣</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            var dbContext = entities.First().Context;
            var parentIds = new HashSet<Guid>();

            foreach (var entry in entities)
            {
                var id = entry.Entity switch
                {
                    DocFeeRequisitionItem item => item.ParentId,
                    DocFeeRequisition requisition when entry.State == EntityState.Added || entry.State == EntityState.Modified => requisition.Id,
                    _ => null,
                };
                if (id.HasValue)
                {
                    parentIds.Add(id.Value);
                }
            }

            // ���㲢���¸����뵥�Ľ��
            var requisitions = dbContext.Set<DocFeeRequisition>().Where(c => parentIds.Contains(c.Id)).ToArray(); // ���������õ��� DocFeeRequisition ����
            var lkupRequisitionItem = dbContext.Set<DocFeeRequisitionItem>().Where(c => parentIds.Contains(c.ParentId.Value)).AsEnumerable().ToLookup(c => c.ParentId.Value); // ���������õ��� DocFeeRequisitionItem ����

            var financialManager = service.GetRequiredService<FinancialManager>();

            foreach (var requisition in requisitions)
            {
                if(dbContext.Entry(requisition).State == EntityState.Deleted)
                {
                    continue;
                }
                if (financialManager.GetRequisitionAmountAndIO(lkupRequisitionItem[requisition.Id], out decimal amount, out bool isOut, dbContext))
                {
                    requisition.Amount = amount;
                    requisition.IO = isOut;
                }
            }
        }
        #endregion Saving ����

        #region AfterSaving ����
        /// <summary>
        /// �ڱ��� DocFeeRequisition �� DocFeeRequisitionItem �󣬴� HashSet �л�ȡ�����뵥�� ID�����㲢������������ݡ�
        /// </summary>
        /// <param name="dbContext">��ǰ DbContext ʵ����</param>
        /// <param name="serviceProvider">�����ṩ�ߡ�</param>
        /// <param name="states">״̬�ֵ䡣</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            // AfterSaving �������������� Saving �������˴��������ջ�ɾ��
        }
        #endregion AfterSaving ����
    }

    /// <summary>
    /// �������뵥�����뵥��ϸ�����
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<PlInvoicesItem>))]
    public class DocFeeRequisitionTotalSettledAmountTriggerHandler : IDbContextSaving<PlInvoicesItem>
    {
        /// <summary>
        /// �������뵥�����뵥��ϸ�����
        /// </summary>
        /// <param name="entity"><inheritdoc/></param>
        /// <param name="serviceProvider"></param>
        /// <param name="states"></param>
        public void Saving(IEnumerable<EntityEntry> entity, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            var db = entity.First().Context;
            var requisitionItemIds = new HashSet<Guid>(
                entity.Select(c => c.Entity).OfType<PlInvoicesItem>().Where(c => c.RequisitionItemId.HasValue).Select(c => c.RequisitionItemId.Value));
            var requisitionIds = new HashSet<Guid>();
            foreach (var id in requisitionItemIds)
            {
                if (db.Set<DocFeeRequisitionItem>().Find(id) is DocFeeRequisitionItem reqItem)
                {
                    if(db.Entry(reqItem).State == EntityState.Deleted)
                    {
                        continue;
                    }
                    reqItem.TotalSettledAmount = reqItem.GetInvoicesItems(db).AsEnumerable().Sum(c => c.GetDAmount());
                    if (reqItem.ParentId is Guid parentId)
                        requisitionIds.Add(parentId);
                }
            }

            foreach (var id in requisitionIds)
            {
                if (db.Set<DocFeeRequisition>().Find(id) is DocFeeRequisition req)
                {
                    if (db.Entry(req).State == EntityState.Deleted)
                    {
                        continue;
                    }
                    req.TotalSettledAmount = req.GetChildren(db).AsEnumerable().Sum(c => c.TotalSettledAmount);
                }
            }
        }
    }
}
