using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;

namespace PowerLmsServer.Managers
{
    /// <summary>提供业务逻辑服务的类。</summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class BusinessLogicManager
    {
        private OrganizationManager _OrganizationManager => _ServiceProvider.GetRequiredService<OrganizationManager>();
        private DbContext DbContext => _DbContext ??= _ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();
        private DbContext _DbContext;
        private readonly IServiceProvider _ServiceProvider;

        /// <summary>构造函数，注入所需的服务。</summary>
        /// <param name="serviceProvider">服务提供者。</param>
        public BusinessLogicManager(IServiceProvider serviceProvider)
        {
            _ServiceProvider = serviceProvider;
        }

        /// <summary>获取关系。</summary>
        /// <typeparam name="TSrc">源类型</typeparam>
        /// <typeparam name="TDest">目标类型</typeparam>
        /// <param name="src">源对象</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>目标对象</returns>
        public TDest GetRelation<TSrc, TDest>(TSrc src, DbContext db = null) where TSrc : class where TDest : class
        {
            db ??= DbContext;
            var pv = (typeof(TSrc), typeof(TDest));
            object result = pv switch
            {
                _ when typeof(TSrc) == typeof(PlInvoicesItem) && typeof(TDest) == typeof(PlInvoices) => (src as PlInvoicesItem)?.GetParent(db),
                _ => null
            };
            return (TDest)result;
        }

        /// <summary>获取实体对象相关的组织机构Id。</summary>
        /// <param name="obj">实体对象</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>组织机构Id</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:删除未使用的参数", Justification = "<挂起>")]  //未来可能会用到
        public Guid? GetOrgId(object obj, DbContext db = null)
        {
            if (_ServiceProvider.GetService<OwContext>()?.User?.OrgId is not Guid orgId) return null;
            return orgId;
        }

        #region 本币相关代码

        /// <summary>获取实体对象的本币码。</summary>
        /// <param name="entityId">实体Id</param>
        /// <param name="entityType">实体类型</param>
        /// <returns>本币码</returns>
        public string GetEntityBaseCurrencyCode(Guid entityId, Type entityType)
        {
            var baseType = entityType.BaseType;
            if (baseType != null && baseType.Namespace == "Castle.Proxies") entityType = baseType;
            return entityType.Name switch
            {
                nameof(PlOrganization) => GetOrganizationBaseCurrencyCode(entityId),
                nameof(PlJob) => GetJobBaseCurrencyCode(entityId),
                nameof(DocFee) => GetFeeBaseCurrencyCode(entityId),
                nameof(DocBill) => GetBillBaseCurrencyCode(entityId),
                _ => throw new ArgumentException("不支持的实体类型", nameof(entityType)),
            };
        }

        /// <summary>获取组织机构的本币码。</summary>
        /// <param name="organizationId">组织机构Id</param>
        /// <returns>本币码</returns>
        private string GetOrganizationBaseCurrencyCode(Guid organizationId)
        {
            var organization = DbContext.Set<PlOrganization>().Find(organizationId);
            if (organization == null) throw new InvalidOperationException($"未找到 Id 为 {organizationId} 的组织机构。");
            return GetCurrencyCode(organization.Id);
        }

        /// <summary>获取一个实体对象的本币码。</summary>
        /// <param name="obj">实体对象</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>本币码</returns>
        public string GetBaseCurrencyCode(object obj, DbContext db = null)
        {
            if (GetOrgId(obj, db) is not Guid orgId) return null;
            db ??= DbContext;
            if (db.Set<PlOrganization>().Find(orgId) is not PlOrganization org) return null;
            return org.BaseCurrencyCode;
        }

        /// <summary>获取工作的本币码。</summary>
        /// <param name="jobId">工作Id</param>
        /// <returns>本币码</returns>
        private string GetJobBaseCurrencyCode(Guid jobId)
        {
            var job = DbContext.Set<PlJob>().Find(jobId);
            if (job == null) throw new InvalidOperationException($"未找到 Id 为 {jobId} 的工作。");
            return GetCurrencyCode(job.OrgId.Value);
        }

        /// <summary>获取费用的本币码。</summary>
        /// <param name="feeId">费用Id</param>
        /// <returns>本币码</returns>
        private string GetFeeBaseCurrencyCode(Guid feeId)
        {
            var fee = DbContext.Set<DocFee>().Find(feeId);
            if (fee == null) throw new InvalidOperationException($"未找到 Id 为 {feeId} 的费用。");
            return GetJobBaseCurrencyCode(fee.JobId.Value);
        }

        /// <summary>获取账单的本币码。</summary>
        /// <param name="billId">账单Id</param>
        /// <returns>本币码</returns>
        private string GetBillBaseCurrencyCode(Guid billId)
        {
            var bill = DbContext.Set<DocBill>().Find(billId);
            if (bill == null) throw new InvalidOperationException($"未找到 Id 为 {billId} 的账单。");
            var fee = DbContext.Set<DocFee>().FirstOrDefault(f => f.BillId == bill.Id);
            if (fee == null) throw new InvalidOperationException($"未找到与账单 Id 为 {bill.Id} 关联的费用。");
            return GetJobBaseCurrencyCode(fee.JobId.Value);
        }

        /// <summary>递归查找本币编码</summary>
        /// <param name="orgId">组织机构Id</param>
        /// <returns>本币编码</returns>
        private string GetCurrencyCode(Guid orgId)
        {
            var orgsCacheItem = _OrganizationManager.GetOrLoadByOrgId(orgId);
            if (orgsCacheItem == null || !orgsCacheItem.Data.TryGetValue(orgId, out var org)) throw new InvalidOperationException($"未找到 Id 为 {orgId} 的组织机构。");
            if (!string.IsNullOrEmpty(org.BaseCurrencyCode)) return org.BaseCurrencyCode;
            if (org.ParentId.HasValue) return GetCurrencyCode(org.ParentId.Value);
            throw new InvalidOperationException($"未找到 Id 为 {orgId} 的组织机构的本币码。");
        }

        /// <summary>获取所有汇率。</summary>
        /// <param name="orgId">组织机构Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="dateTime">日期时间</param>
        /// <returns>汇率字典</returns>
        public Dictionary<(string, string), decimal> GetAllRate(Guid orgId, DbContext dbContext = null, DateTime? dateTime = null)
        {
            var baseColl = GetRateQuery(orgId, dbContext, dateTime);
            var coll = baseColl.AsEnumerable().GroupBy(c => (c.SCurrency, c.DCurrency)).Select(c => c.OrderByDescending(d => d.EndData).First());
            var result = coll.ToDictionary(c => (c.SCurrency, c.DCurrency), c => c.Exchange);
            return result;
        }

        #endregion 本币相关代码

        #region 汇率相关代码

        /// <summary>返回汇率查询。</summary>
        /// <param name="orgId">组织机构Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="dateTime">日期时间</param>
        /// <returns>汇率查询</returns>
        public IQueryable<PlExchangeRate> GetRateQuery(Guid orgId, DbContext dbContext = null, DateTime? dateTime = null)
        {
            dbContext ??= DbContext;
            dateTime ??= OwHelper.WorldNow;
            var baseColl = dbContext.Set<PlExchangeRate>().Where(c => c.OrgId == orgId && c.BeginDate <= dateTime && c.EndData >= dateTime);
            return baseColl;
        }

        /// <summary>获取指定源币种到目标币种的汇率。</summary>
        /// <param name="orgId">组织机构Id</param>
        /// <param name="sCurrency">源币种</param>
        /// <param name="dCurrency">目标币种</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="dateTime">日期时间</param>
        /// <returns>汇率</returns>
        public decimal GetExchageRate(Guid orgId, string sCurrency, string dCurrency, DbContext dbContext = null, DateTime? dateTime = null)
        {
            var baseColl = GetRateQuery(orgId, dbContext, dateTime).Where(c => c.SCurrency == sCurrency && c.DCurrency == dCurrency);
            var rate = baseColl.AsNoTracking().AsEnumerable().OrderByDescending(c => c.EndData).FirstOrDefault();
            return rate?.Exchange ?? 1;
        }

        /// <summary>获取指定申请单的汇率。</summary>
        /// <param name="requisitionItem">申请单明细项</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>汇率</returns>
        public decimal GetExchageRate(DocFeeRequisitionItem requisitionItem, DbContext db = null)
        {
            db ??= DbContext;
            if (requisitionItem.GetParent(db) is not DocFeeRequisition requisition) return 0;
            var sCurrency = requisitionItem.GetDocFee(db)?.Currency;
            if (string.IsNullOrEmpty(sCurrency)) goto lbErr;
            var dCurrency = requisitionItem.GetParent(db)?.Currency;
            var baseCurrency = GetBaseCurrencyCode(requisitionItem);
            if (baseCurrency == dCurrency) return requisitionItem.GetDocFee(db).ExchangeRate;
            else
            {
                if (GetOrgId(requisitionItem, db) is not Guid orgId) goto lbErr;
                return GetExchageRate(orgId, sCurrency, dCurrency, db);
            }
        lbErr:
            return 0;
        }

        /// <summary>获取指定账单的汇率。</summary>
        /// <param name="fee">费用</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>汇率</returns>
        public decimal GetExchageRate(DocFee fee, DbContext db = null)
        {
            db ??= DbContext;
            if (fee.GetBill(db) is not DocBill bill) return 0;
            var sCurrency = fee?.Currency;
            if (string.IsNullOrEmpty(sCurrency)) goto lbErr;
            var dCurrency = bill?.CurrTypeId;
            if (string.IsNullOrEmpty(dCurrency)) goto lbErr;
            var baseCurrency = GetBaseCurrencyCode(fee);
            if (baseCurrency == dCurrency) return fee.ExchangeRate;
            else
            {
                if (GetOrgId(fee, db) is not Guid orgId) goto lbErr;
                return GetExchageRate(orgId, sCurrency, dCurrency, db);
            }
        lbErr:
            return 0;
        }

        #endregion 汇率相关代码

        #region 账单相关代码

        /// <summary>获取账单的合计金额和借贷方向。</summary>
        /// <param name="items">账单明细项</param>
        /// <param name="amount">金额</param>
        /// <param name="isOut">是否支出</param>
        /// <param name="db">数据库上下文</param>
        /// <returns>是否成功</returns>
        public bool GetBiillAmountAndIO(IEnumerable<DocFee> items, out decimal amount, out bool isOut, DbContext db = null)
        {
            var inner = items.AsEnumerable();
            if (!inner.Any()) { amount = 0; isOut = false; return true; }
            db ??= DbContext;
            if (inner.FirstOrDefault()?.GetBill(db) is not DocBill bill) goto lblErr;
            var baseCurrency = GetBaseCurrencyCode(bill, db);
            var debit = inner.Where(c => c.IO).Sum(c => Math.Round(c.Amount * GetExchageRate(c, db), 4, MidpointRounding.AwayFromZero));
            var credit = inner.Where(c => !c.IO).Sum(c => Math.Round(c.Amount * GetExchageRate(c, db), 4, MidpointRounding.AwayFromZero));
            amount = Math.Abs(debit - credit);
            isOut = debit > credit;
            return true;
        lblErr:
            amount = 0;
            isOut = false;
            return false;
        }

        #endregion 账单相关代码

        #region 工作任务相关代码

        #region 工作号删除功能

        /// <summary>
        /// 检查工作号是否可以删除。
        /// </summary>
        /// <param name="jobId">工作号Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>如果可以删除返回true，否则返回false并设置错误信息</returns>
        public bool CanDeleteJob(Guid jobId, PowerLmsUserDbContext dbContext = null)
        {
            dbContext ??= (PowerLmsUserDbContext)DbContext;

            // 检查工作号是否存在
            var job = dbContext.PlJobs.Find(jobId);
            if (job == null)
            {
                OwHelper.SetLastErrorAndMessage(404, $"未找到Id为{jobId}的工作号");
                return false;
            }

            // 检查工作号状态
            if (job.JobState > 2)
            {
                OwHelper.SetLastErrorAndMessage(400, $"工作号状态已超过操作阶段(JobState={job.JobState})，无法删除");
                return false;
            }

            // 检查是否存在已审核的费用
            var hasAuditedFees = dbContext.DocFees
                .Any(c => c.JobId == jobId && c.AuditOperatorId != null);
            if (hasAuditedFees)
            {
                OwHelper.SetLastErrorAndMessage(400, "工作号下存在已审核的费用，无法删除");
                return false;
            }

            // 检查是否存在关联账单的费用
            var hasBilledFees = dbContext.DocFees
                .Any(c => c.JobId == jobId && c.BillId != null);
            if (hasBilledFees)
            {
                OwHelper.SetLastErrorAndMessage(400, "工作号下存在已关联账单的费用，无法删除");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 删除工作号及其所有关联数据。
        /// </summary>
        /// <remarks>如果工作号下存在已审核的费用或已关联账单的费用，则无法删除。</remarks>
        /// <param name="jobId">工作号Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>如果删除成功返回true，否则返回false</returns>
        public bool DeleteJob(Guid jobId, PowerLmsUserDbContext dbContext = null)
        {
            dbContext ??= (PowerLmsUserDbContext)DbContext;

            try
            {
                // 首先验证是否可以删除
                if (!CanDeleteJob(jobId, dbContext))
                {
                    // CanDeleteJob 方法已设置适当的错误信息，这里不需要再设置
                    return false;
                }

                // 获取工作号
                var job = dbContext.PlJobs.Find(jobId);

                // 删除空运出口单
                var eaDocs = dbContext.PlEaDocs.Where(c => c.JobId == jobId).ToList();
                if (eaDocs.Any())
                    dbContext.PlEaDocs.RemoveRange(eaDocs);

                // 删除空运进口单
                var iaDocs = dbContext.PlIaDocs.Where(c => c.JobId == jobId).ToList();
                if (iaDocs.Any())
                    dbContext.PlIaDocs.RemoveRange(iaDocs);

                // 删除海运出口单
                var esDocs = dbContext.PlEsDocs.Where(c => c.JobId == jobId).ToList();
                if (esDocs.Any())
                    dbContext.PlEsDocs.RemoveRange(esDocs);

                // 删除海运进口单
                var isDocs = dbContext.PlIsDocs.Where(c => c.JobId == jobId).ToList();
                if (isDocs.Any())
                    dbContext.PlIsDocs.RemoveRange(isDocs);

                // 删除费用明细
                var fees = dbContext.DocFees.Where(c => c.JobId == jobId).ToList();
                if (fees.Any())
                    dbContext.DocFees.RemoveRange(fees);

                // 最后删除工作号本身
                dbContext.PlJobs.Remove(job);

                return true;
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(500, $"DELETE_JOB_EXCEPTION:{ex.Message}");
                return false;
            }
        }

        #endregion 工作号删除功能

        /// <summary>根据账单Id获取工作任务Id。</summary>
        /// <param name="billId">账单Id</param>
        /// <returns>工作任务Id</returns>
        public Guid? GetJobIdByBillId(Guid billId)
        {
            var fee = DbContext.Set<DocFee>().FirstOrDefault(c => c.BillId == billId && c.JobId != null);
            return (fee?.JobId.HasValue ?? false) ? DbContext.Set<PlJob>().Find(fee.JobId.Value).Id : null;
        }

        /// <summary>根据结算单Id获取工作任务Id。</summary>
        /// <param name="invoiceItemId">结算单明细项Id</param>
        /// <returns>工作任务Id</returns>
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
