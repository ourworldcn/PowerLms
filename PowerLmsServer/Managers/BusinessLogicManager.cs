using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 提供业务逻辑服务的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class BusinessLogicManager
    {
        private readonly OrganizationManager _OrganizationManager;
        private DbContext DbContext => _DbContext ??= _ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
        private DbContext _DbContext;
        IServiceProvider _ServiceProvider;
        /// <summary>
        /// 构造函数，注入所需的服务。
        /// </summary>
        /// <param name="organizationManager">机构管理器。</param>
        /// <param name="dbContextFactory"></param>
        /// <param name="dbContext"></param>
        public BusinessLogicManager(OrganizationManager organizationManager, IDbContextFactory<PowerLmsUserDbContext> dbContextFactory, DbContext dbContext, IServiceProvider serviceProvider)
        {
            _OrganizationManager = organizationManager;
            _ServiceProvider = serviceProvider;
        }

        #region 本币相关代码

        /// <summary>
        /// 获取实体对象的本币码。
        /// </summary>
        /// <param name="entityId">实体Id。</param>
        /// <param name="entityType">实体类型。</param>
        /// <returns>实体对象的本币码。</returns>
        /// <exception cref="ArgumentException">当实体类型不支持时抛出。</exception>
        /// <exception cref="InvalidOperationException">当找不到实体或本币码时抛出。</exception>
        public string GetEntityBaseCurrencyCode(Guid entityId, Type entityType)
        {
            var baseType = entityType.BaseType;
            if (baseType != null && baseType.Namespace == "Castle.Proxies")
            {
                entityType = baseType;
            }

            switch (entityType.Name)
            {
                case nameof(PlOrganization):
                    return GetOrganizationBaseCurrencyCode(entityId);
                case nameof(PlJob):
                    return GetJobBaseCurrencyCode(entityId);
                case nameof(DocFee):
                    return GetFeeBaseCurrencyCode(entityId);
                case nameof(DocBill):
                    return GetBillBaseCurrencyCode(entityId);
                default:
                    throw new ArgumentException("不支持的实体类型", nameof(entityType));
            }
        }

        /// <summary>
        /// 获取组织机构的本币码。
        /// </summary>
        /// <param name="organizationId">组织机构Id。</param>
        /// <returns>组织机构的本币码。</returns>
        /// <exception cref="InvalidOperationException">当找不到组织机构时抛出。</exception>
        private string GetOrganizationBaseCurrencyCode(Guid organizationId)
        {
            var organization = DbContext.Set<PlOrganization>().Find(organizationId);
            if (organization == null)
            {
                throw new InvalidOperationException($"未找到 Id 为 {organizationId} 的组织机构。");
            }
            return GetCurrencyCode(organization.Id);
        }

        /// <summary>
        /// 获取工作的本币码。
        /// </summary>
        /// <param name="jobId">工作Id。</param>
        /// <returns>工作的本币码。</returns>
        /// <exception cref="InvalidOperationException">当找不到工作时抛出。</exception>
        private string GetJobBaseCurrencyCode(Guid jobId)
        {
            var job = DbContext.Set<PlJob>().Find(jobId);
            if (job == null)
            {
                throw new InvalidOperationException($"未找到 Id 为 {jobId} 的工作。");
            }
            return GetCurrencyCode(job.OrgId.Value);
        }

        /// <summary>
        /// 获取费用的本币码。
        /// </summary>
        /// <param name="feeId">费用Id。</param>
        /// <returns>费用的本币码。</returns>
        /// <exception cref="InvalidOperationException">当找不到费用时抛出。</exception>
        private string GetFeeBaseCurrencyCode(Guid feeId)
        {
            var fee = DbContext.Set<DocFee>().Find(feeId);
            if (fee == null)
            {
                throw new InvalidOperationException($"未找到 Id 为 {feeId} 的费用。");
            }
            return GetJobBaseCurrencyCode(fee.JobId.Value);
        }

        /// <summary>
        /// 获取账单的本币码。
        /// </summary>
        /// <param name="billId">账单Id。</param>
        /// <returns>账单的本币码。</returns>
        /// <exception cref="InvalidOperationException">当找不到账单时抛出。</exception>
        private string GetBillBaseCurrencyCode(Guid billId)
        {
            var bill = DbContext.Set<DocBill>().Find(billId);
            if (bill == null)
            {
                throw new InvalidOperationException($"未找到 Id 为 {billId} 的账单。");
            }
            var fee = DbContext.Set<DocFee>().FirstOrDefault(f => f.BillId == bill.Id);
            if (fee == null)
            {
                throw new InvalidOperationException($"未找到与账单 Id 为 {bill.Id} 关联的费用。");
            }
            return GetJobBaseCurrencyCode(fee.JobId.Value);
        }

        /// <summary>
        /// 递归查找本币编码
        /// </summary>
        /// <param name="orgId">组织机构Id。</param>
        /// <returns>本币编码。</returns>
        /// <exception cref="InvalidOperationException">当找不到本币码时抛出异常。</exception>
        private string GetCurrencyCode(Guid orgId)
        {
            var orgsCacheItem = _OrganizationManager.GetOrLoadByOrgId(orgId);
            if (orgsCacheItem == null || !orgsCacheItem.Data.TryGetValue(orgId, out var org))
            {
                throw new InvalidOperationException($"未找到 Id 为 {orgId} 的组织机构。");
            }

            if (!string.IsNullOrEmpty(org.BaseCurrencyCode))
            {
                return org.BaseCurrencyCode;
            }

            if (org.ParentId.HasValue)
            {
                return GetCurrencyCode(org.ParentId.Value);
            }

            throw new InvalidOperationException($"未找到 Id 为 {orgId} 的组织机构的本币码。");
        }

        /// <summary>
        /// 返回汇率查询。
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="dbContext"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public IQueryable<PlExchangeRate> GetRateQuery(Guid orgId, DbContext dbContext = null, DateTime? dateTime = null)
        {
            dbContext ??= DbContext;
            dateTime ??= OwHelper.WorldNow;
            var baseColl = dbContext.Set<PlExchangeRate>().Where(c => c.OrgId == orgId && c.BeginDate <= dateTime && c.EndData >= dateTime);
            return baseColl;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="dbContext"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public Dictionary<(string, string), decimal> GetAllRate(Guid orgId, DbContext dbContext = null, DateTime? dateTime = null)
        {
            var baseColl = GetRateQuery(orgId, dbContext, dateTime);
            //汇率失效日期倒序排序，取第一个就行
            var coll = baseColl.AsEnumerable().GroupBy(c => (c.SCurrency, c.DCurrency)).Select(c => c.OrderByDescending(d => d.EndData).First());
            var result = coll.ToDictionary(c => (c.SCurrency, c.DCurrency), c => c.Exchange);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="sCurrency"></param>
        /// <param name="dCurrency"></param>
        /// <param name="dbContext"></param>
        /// <param name="dateTime"></param>
        /// <returns>未找到则返回默认值1.</returns>
        public decimal GetRate(Guid orgId, string sCurrency, string dCurrency, DbContext dbContext = null, DateTime? dateTime = null)
        {
            var baseColl = GetRateQuery(orgId, dbContext, dateTime).Where(c => c.SCurrency == sCurrency && c.DCurrency == dCurrency);
            var rate = baseColl.AsNoTracking().AsEnumerable().OrderByDescending(c => c.EndData).FirstOrDefault();
            return rate?.Exchange ?? 1;
        }
        #endregion 本币相关代码

        #region 工作任务相关代码
        /// <summary>
        /// 根据账单Id获取工作任务Id。
        /// </summary>
        /// <param name="billId"></param>
        /// <returns></returns>
        public Guid? GetJobIdByBillId(Guid billId)
        {
            var fee = DbContext.Set<DocFee>().FirstOrDefault(c => c.BillId == billId && c.JobId != null);
            return (fee?.JobId.HasValue ?? false) ? DbContext.Set<PlJob>().Find(fee.JobId.Value).Id : null;
        }

        /// <summary>
        /// 根据结算单Id获取工作任务Id。
        /// </summary>
        /// <param name="invoiceItemId"></param>
        /// <returns></returns>
        public Guid? GetJobIdByInvoiceItemId(Guid invoiceItemId)
        {
            var coll = from invoiceItem in DbContext.Set<PlInvoicesItem>().Where(c => c.Id == invoiceItemId)
                       join requisitionItem in DbContext.Set<DocFeeRequisitionItem>() on invoiceItem.RequisitionItemId equals requisitionItem.Id
                       join fee in DbContext.Set<DocFee>() on requisitionItem.FeeId equals fee.Id
                       where fee.JobId != null
                       select fee.JobId;
            return coll.FirstOrDefault();
        }
        #endregion 工作任务相关代码
    }
}
