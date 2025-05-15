using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// �ṩ������ط�����ࡣ
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class FinancialManager
    {
        private readonly IServiceProvider _ServiceProvider;

        private DbContext _DbContext;
        private DbContext DbContext => _DbContext ??= _ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();

        private BusinessLogicManager _BusinessLogicManager;
        private BusinessLogicManager BusinessLogicManager => _BusinessLogicManager ??= _ServiceProvider.GetRequiredService<BusinessLogicManager>();

        /// <summary>
        /// ���캯����ע������ķ���
        /// </summary>
        /// <param name="serviceProvider">�����ṩ�ߡ�</param>
        public FinancialManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// ��ȡ���㵥�Ľ��ͽ����ڡ�
        /// </summary>
        /// <param name="items">���㵥��ϸ�</param>
        /// <param name="amount">��</param>
        /// <param name="isOut">�Ƿ�֧����</param>
        /// <param name="db">���ݿ������ġ�</param>
        /// <returns>�Ƿ�ɹ���</returns>
        public bool GetInvoiceAmountAndIO(IEnumerable<PlInvoicesItem> items, out decimal amount, out bool isOut, DbContext db = null)
        {
            db ??= DbContext;
            var inner = items.AsEnumerable();
            if (!inner.Any()) { amount = 0; isOut = false; return true; }
            if (inner.FirstOrDefault()?.GetParent(db) is not PlInvoices invoices) goto lblErr;
            var debit = inner.Where(c => c.GetRequisitionItem(db)?.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * c.ExchangeRate, 4, MidpointRounding.AwayFromZero));
            var credit = inner.Where(c => !c.GetRequisitionItem(db)?.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * c.ExchangeRate, 4, MidpointRounding.AwayFromZero));
            amount = Math.Abs(debit - credit);
            isOut = debit > credit;
            return true;
        lblErr:
            amount = 0;
            isOut = false;
            return false;
        }
        
        /// <summary>
        /// ��ȡ���뵥�ĺϼƽ��ͽ������
        /// </summary>
        /// <param name="items">���뵥��ϸ��</param>
        /// <param name="amount">���</param>
        /// <param name="isOut">�Ƿ�֧��</param>
        /// <param name="db">���ݿ�������</param>
        /// <returns>�Ƿ�ɹ�</returns>
        public bool GetRequisitionAmountAndIO(IEnumerable<DocFeeRequisitionItem> items, out decimal amount, out bool isOut, DbContext db = null)
        {
            var inner = items.AsEnumerable();
            if (!inner.Any()) { amount = 0; isOut = false; return true; }
            db ??= DbContext;
            if (inner.FirstOrDefault()?.GetParent(db) is not DocFeeRequisition requisition) goto lblErr;
            var baseCurrency = BusinessLogicManager.GetBaseCurrencyCode(requisition, db);
            var debit = inner.Where(c => c.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * BusinessLogicManager.GetExchageRate(c, db), 4, MidpointRounding.AwayFromZero));
            var credit = inner.Where(c => !c.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * BusinessLogicManager.GetExchageRate(c, db), 4, MidpointRounding.AwayFromZero));
            amount = Math.Abs(debit - credit);
            isOut = debit > credit;
            return true;
        lblErr:
            amount = 0;
            isOut = false;
            return false;
        }
    }
}
