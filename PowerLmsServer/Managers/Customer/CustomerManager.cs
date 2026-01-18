using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 客户资料及相关管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class CustomerManager
    {
        private readonly PowerLmsUserDbContext _DbContext;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomerManager(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }
        /// <summary>
        /// 检查客户是否可以被删除。
        /// 遍历可能引用客户的业务表，检测是否存在外键引用。
        /// </summary>
        /// <param name="customerId">客户ID</param>
        /// <param name="errorMessage">如果不能删除，返回错误信息</param>
        /// <returns>true=可以删除，false=不可删除</returns>
        public bool CanDeleteCustomer(Guid customerId, out string errorMessage)
        {
            errorMessage = null;
            bool hasJobAsCustomer = _DbContext.PlJobs.Any(j => j.CustomId == customerId);
            if (hasJobAsCustomer)
            {
                errorMessage = "该客户资料已被工作号引用（作为客户），无法删除。请使用设为无效功能代替。";
                return false;
            }
            bool hasJobAsCarrier = _DbContext.PlJobs.Any(j => j.CarrieId == customerId);
            if (hasJobAsCarrier)
            {
                errorMessage = "该客户资料已被工作号引用（作为承运人），无法删除。请使用设为无效功能代替。";
                return false;
            }
            bool hasInvoiceAsJiesuan = _DbContext.PlInvoicess.Any(i => i.JiesuanDanweiId == customerId);
            if (hasInvoiceAsJiesuan)
            {
                errorMessage = "该客户资料已被结算单引用（作为结算单位），无法删除。请使用设为无效功能代替。";
                return false;
            }
            bool hasInvoiceAsRefund = _DbContext.PlInvoicess.Any(i => i.RefundUnitId == customerId);
            if (hasInvoiceAsRefund)
            {
                errorMessage = "该客户资料已被结算单引用（作为回款单位），无法删除。请使用设为无效功能代替。";
                return false;
            }
            return true;
        }
    }
}
