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
        #region 私有字段
        private readonly IServiceProvider _ServiceProvider;
        private DbContext _DbContext;
        private BusinessLogicManager _BusinessLogicManager;
        #endregion

        #region 属性
        private DbContext DbContext => _DbContext ??= _ServiceProvider.GetRequiredService<PowerLmsUserDbContext>(); // 延迟加载数据库上下文
        private BusinessLogicManager BusinessLogicManager => _BusinessLogicManager ??= _ServiceProvider.GetRequiredService<BusinessLogicManager>(); // 延迟加载业务逻辑管理器
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数，注入所需的服务。
        /// </summary>
        /// <param name="serviceProvider">服务提供者。</param>
        public FinancialManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider; // 保存服务提供者以便后续使用
        }
        #endregion

        #region 公共方法
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
            db ??= DbContext; // 如果没有提供数据库上下文，则使用默认的
            var inner = items.AsEnumerable(); // 转换为可枚举集合
            if (!inner.Any()) { amount = 0; isOut = false; return true; } // 无明细项时直接返回
            if (inner.FirstOrDefault()?.GetParent(db) is not PlInvoices invoices) goto lblErr; // 获取父结算单失败时跳转到错误处理
            var debit = inner.Where(c => c.GetRequisitionItem(db)?.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * c.ExchangeRate, 4, MidpointRounding.AwayFromZero)); // 计算借方金额
            var credit = inner.Where(c => !c.GetRequisitionItem(db)?.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * c.ExchangeRate, 4, MidpointRounding.AwayFromZero)); // 计算贷方金额
            amount = Math.Abs(debit - credit); // 计算净额
            isOut = debit > credit; // 借方大于贷方表示支出
            return true;
        lblErr:
            amount = 0;
            isOut = false;
            return false; // 出错时返回失败
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
            var inner = items.AsEnumerable(); // 转换为可枚举集合
            if (!inner.Any()) { amount = 0; isOut = false; return true; } // 无明细项时直接返回
            db ??= DbContext; // 如果没有提供数据库上下文，则使用默认的
            if (inner.FirstOrDefault()?.GetParent(db) is not DocFeeRequisition requisition) goto lblErr; // 获取父申请单失败时跳转到错误处理
            var baseCurrency = BusinessLogicManager.GetBaseCurrencyCode(requisition, db); // 获取基础货币代码
            var debit = inner.Where(c => c.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * BusinessLogicManager.GetExchageRate(c, db), 4, MidpointRounding.AwayFromZero)); // 计算借方金额
            var credit = inner.Where(c => !c.GetDocFee(db)?.IO ?? false).Sum(c => Math.Round(c.Amount * BusinessLogicManager.GetExchageRate(c, db), 4, MidpointRounding.AwayFromZero)); // 计算贷方金额
            amount = Math.Abs(debit - credit); // 计算净额
            isOut = debit > credit; // 借方大于贷方表示支出
            return true;
        lblErr:
            amount = 0;
            isOut = false;
            return false; // 出错时返回失败
        }
        #endregion
    }
}
