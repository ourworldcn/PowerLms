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
    /// 提供财务相关服务的类。
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
        /// 构造函数，注入所需的服务。
        /// </summary>
        /// <param name="serviceProvider">服务提供者。</param>
        public FinancialManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 获取结算单的金额和进出口。
        /// </summary>
        /// <param name="items">结算单明细项。</param>
        /// <param name="amount">金额。</param>
        /// <param name="isOut">是否支出。</param>
        /// <param name="db">数据库上下文。</param>
        /// <returns>是否成功。</returns>
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
        /// 获取申请单的合计金额和借贷方向。
        /// </summary>
        /// <param name="items">申请单明细项</param>
        /// <param name="amount">金额</param>
        /// <param name="isOut">是否支出</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>是否成功</returns>
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
