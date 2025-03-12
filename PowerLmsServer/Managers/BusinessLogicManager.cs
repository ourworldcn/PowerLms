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
        private readonly IServiceProvider _ServiceProvider;
        private OrganizationManager _OrganizationManager => _ServiceProvider.GetRequiredService<OrganizationManager>();
        // 使用后期初始化避免重复解析
        private DbContext _DbContext;
        /// <summary>数据库上下文。</summary>
        private DbContext DbContext => _DbContext ??= _ServiceProvider.GetRequiredService<PowerLmsUserDbContext>();

        AuthorizationManager _AuthorizationManager;
        /// <summary>权限管理器。</summary>
        AuthorizationManager AuthorizationManager => _AuthorizationManager ??= _ServiceProvider.GetRequiredService<AuthorizationManager>();

        OwSqlAppLogger _SqlAppLogger;
        /// <summary>SQL 应用日志服务。</summary>
        OwSqlAppLogger SqlAppLogger => _SqlAppLogger ??= _ServiceProvider.GetRequiredService<OwSqlAppLogger>();

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:删除未使用的参数", Justification = "<挂起>")]
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
            if (organization == null)
                throw new InvalidOperationException($"未找到 Id 为 {organizationId} 的组织机构。");
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

        /// <summary>递归查找本币编码。</summary>
        /// <param name="orgId">组织机构Id</param>
        /// <returns>本币编码</returns>
        private string GetCurrencyCode(Guid orgId)
        {
            var orgsCacheItem = _OrganizationManager.GetOrLoadByOrgId(orgId);
            if (orgsCacheItem == null || !orgsCacheItem.Data.TryGetValue(orgId, out var org))
                throw new InvalidOperationException($"未找到 Id 为 {orgId} 的组织机构。");
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

        #endregion

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

        #endregion

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

        #endregion

        #region 工作任务相关代码

        #region 工作号删除功能

        /// <summary>检查工作号是否可以删除。</summary>
        /// <param name="jobId">工作号Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>可以删除返回true，否则返回false并设置错误信息。</returns>
        public bool CanDeleteJob(Guid jobId, PowerLmsUserDbContext dbContext = null)
        {
            dbContext ??= (PowerLmsUserDbContext)DbContext;
            var job = dbContext.PlJobs.Find(jobId);
            if (job == null)
            {
                OwHelper.SetLastErrorAndMessage(404, $"未找到Id为{jobId}的工作号");
                return false;
            }
            if (job.JobState > 2)
            {
                OwHelper.SetLastErrorAndMessage(400, $"工作号状态已超过操作阶段(JobState={job.JobState})，无法删除");
                return false;
            }
            var hasAuditedFees = dbContext.DocFees.Any(c => c.JobId == jobId && c.AuditOperatorId != null);
            if (hasAuditedFees)
            {
                OwHelper.SetLastErrorAndMessage(400, "工作号下存在已审核的费用，无法删除");
                return false;
            }
            var hasBilledFees = dbContext.DocFees.Any(c => c.JobId == jobId && c.BillId != null);
            if (hasBilledFees)
            {
                OwHelper.SetLastErrorAndMessage(400, "工作号下存在已关联账单的费用，无法删除");
                return false;
            }
            return true;
        }

        /// <summary>删除工作号及其所有关联数据。</summary>
        /// <remarks>如果工作号下存在已审核或已开票费用，则无法删除。</remarks>
        /// <param name="jobId">工作号Id</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>删除成功返回true，否则返回false。</returns>
        public bool DeleteJob(Guid jobId, PowerLmsUserDbContext dbContext = null)
        {
            dbContext ??= (PowerLmsUserDbContext)DbContext;
            try
            {
                if (!CanDeleteJob(jobId, dbContext)) return false;
                var job = dbContext.PlJobs.Find(jobId);
                var eaDocs = dbContext.PlEaDocs.Where(c => c.JobId == jobId).ToList();
                if (eaDocs.Any()) dbContext.PlEaDocs.RemoveRange(eaDocs);
                var iaDocs = dbContext.PlIaDocs.Where(c => c.JobId == jobId).ToList();
                if (iaDocs.Any()) dbContext.PlIaDocs.RemoveRange(iaDocs);
                var esDocs = dbContext.PlEsDocs.Where(c => c.JobId == jobId).ToList();
                if (esDocs.Any()) dbContext.PlEsDocs.RemoveRange(esDocs);
                var isDocs = dbContext.PlIsDocs.Where(c => c.JobId == jobId).ToList();
                if (isDocs.Any()) dbContext.PlIsDocs.RemoveRange(isDocs);
                var fees = dbContext.DocFees.Where(c => c.JobId == jobId).ToList();
                if (fees.Any()) dbContext.DocFees.RemoveRange(fees);
                dbContext.PlJobs.Remove(job);
                return true;
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(500, $"DELETE_JOB_EXCEPTION:{ex.Message}");
                return false;
            }
        }

        #endregion

        /// <summary>变更业务状态和操作状态。</summary>
        /// <param name="jobId">工作号ID</param>
        /// <param name="jobState">要变更的业务状态(可为null)</param>
        /// <param name="operateState">要变更的操作状态(可为null)</param>
        /// <param name="userId">当前用户Id</param>
        /// <returns>变更结果,包含最新状态;失败返回null。</returns>
        public (byte JobState, byte OperateState)? ChangeJobAndDocState(Guid jobId, int? jobState, byte? operateState, Guid userId)
        {
            var job = DbContext.Set<PlJob>().Find(jobId);
            if (job is null)
            {
                OwHelper.SetLastErrorAndMessage(404, $"找不到指定的业务对象，Id={jobId}");
                return null;
            }
            byte oldJobState = job.JobState;
            var now = OwHelper.WorldNow;
            string error;
            var plBusinessDoc = FindAndChangeBusinessDoc(jobId, job, operateState, jobState, out error);
            if (plBusinessDoc == null)
            {
                OwHelper.SetLastErrorAndMessage(400, error);
                return null;
            }
            byte oldOperateState = plBusinessDoc.Status;
            try
            {
                if (jobState.HasValue)
                {
                    var transition = (job.JobState, jobState.Value);
                    switch (transition)
                    {
                        case (4, 8): // 从“操作完成”到“已审核”
                            if (!AuthorizationManager.Demand(out error, "F.2.8"))
                            {
                                OwHelper.SetLastErrorAndMessage(403, error);
                                return null;
                            }
                            job.AuditDateTime = now; job.AuditOperatorId = userId;
                            var auditFees = DbContext.Set<DocFee>().Where(c => c.JobId == job.Id && !c.AuditDateTime.HasValue).ToList();
                            foreach (var fee in auditFees)
                            {
                                fee.AuditDateTime = now;
                                fee.AuditOperatorId = userId;
                            }
                            SqlAppLogger.LogGeneralInfo($"审核工作号:{job.JobNo}, 状态从{oldJobState}变更为{jobState.Value}, 审核费用数量:{auditFees.Count}");
                            break;
                        case (8, 4): // 从“已审核”回到“操作完成”
                            if (!AuthorizationManager.Demand(out error, "F.2.8"))
                            {
                                OwHelper.SetLastErrorAndMessage(403, error);
                                return null;
                            }
                            job.AuditDateTime = null; job.AuditOperatorId = null;
                            var unauditFees = DbContext.Set<DocFee>().Where(c => c.JobId == job.Id && c.AuditDateTime.HasValue).ToList();
                            foreach (var fee in unauditFees)
                            {
                                fee.AuditDateTime = null;
                                fee.AuditOperatorId = null;
                            }
                            SqlAppLogger.LogGeneralInfo($"取消审核工作号:{job.JobNo}, 状态从{oldJobState}变更为{jobState.Value}, 取消审核费用数量:{unauditFees.Count}");
                            break;
                        case (8, 16): // 从“已审核”到“已关闭”
                            if (!AuthorizationManager.Demand(out error, "F.2.9"))
                            {
                                OwHelper.SetLastErrorAndMessage(403, error);
                                return null;
                            }
                            job.CloseDate = now;
                            SqlAppLogger.LogGeneralInfo($"关闭工作号:{job.JobNo}, 状态从{oldJobState}变更为{jobState.Value}");
                            break;
                        case (16, 8): // 从“已关闭”回到“已审核”
                            if (!AuthorizationManager.Demand(out error, "F.2.9"))
                            {
                                OwHelper.SetLastErrorAndMessage(403, error);
                                return null;
                            }
                            job.CloseDate = null;
                            SqlAppLogger.LogGeneralInfo($"取消关闭工作号:{job.JobNo}, 状态从{oldJobState}变更为{jobState.Value}");
                            break;
                        default:
                            SqlAppLogger.LogGeneralInfo($"变更工作号状态:{job.JobNo}, 状态从{oldJobState}变更为{jobState.Value}");
                            break;
                    }
                    job.JobState = (byte)jobState.Value;
                }
                if (operateState.HasValue)
                {
                    plBusinessDoc.Status = operateState.Value;
                    switch (true)
                    {
                        case var _ when (operateState.Value & 128) != 0 && job.JobState <= 2:
                            job.JobState = 4;
                            SqlAppLogger.LogGeneralInfo($"完成操作:{job.JobNo}, 操作状态从{oldOperateState}变更为{operateState.Value}, 业务状态自动更新为4");
                            break;
                        case var _ when operateState.Value < 128 && job.JobState == 4:
                            job.JobState = 2;
                            SqlAppLogger.LogGeneralInfo($"取消完成操作:{job.JobNo}, 操作状态从{oldOperateState}变更为{operateState.Value}, 业务状态自动回退为2");
                            break;
                        default:
                            SqlAppLogger.LogGeneralInfo($"变更操作状态:{job.JobNo}, 操作状态从{oldOperateState}变更为{operateState.Value}");
                            break;
                    }
                }
                DbContext.SaveChanges();
                return (job.JobState, plBusinessDoc.Status);
            }
            catch (Exception ex)
            {
                OwHelper.SetLastErrorAndMessage(500, $"执行状态变更时发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>查找指定工作号的业务单据并验证权限。</summary>
        /// <param name="jobId">工作号ID</param>
        /// <param name="job">工作号对象</param>
        /// <param name="operateState">操作状态(可选)</param>
        /// <param name="jobState">业务状态(可选)</param>
        /// <param name="error">错误信息</param>
        /// <returns>业务单据对象,验证失败返回null</returns>
        public IPlBusinessDoc FindAndChangeBusinessDoc(Guid jobId, PlJob job, byte? operateState, int? jobState, out string error)
        {
            error = null;
            IPlBusinessDoc businessDoc = null;
            if (DbContext.Set<PlIaDoc>().FirstOrDefault(c => c.JobId == jobId) is PlIaDoc iaDoc) businessDoc = iaDoc;
            else if (DbContext.Set<PlEaDoc>().FirstOrDefault(c => c.JobId == jobId) is PlEaDoc eaDoc) businessDoc = eaDoc;
            else if (DbContext.Set<PlIsDoc>().FirstOrDefault(c => c.JobId == jobId) is PlIsDoc isDoc) businessDoc = isDoc;
            else if (DbContext.Set<PlEsDoc>().FirstOrDefault(c => c.JobId == jobId) is PlEsDoc esDoc) businessDoc = esDoc;
            if (businessDoc == null)
            {
                error = $"找不到业务单据对象，Id={jobId}";
                return null;
            }
            // 根据业务单据类型确定后续权限前缀
            if (jobState.HasValue)
            {
                var (docType, prefix) = businessDoc switch
                {
                    PlIaDoc => ("空运进口单", "D1"),
                    PlEaDoc => ("空运出口单", "D0"),
                    PlIsDoc => ("海运进口单", "D3"),
                    PlEsDoc => ("海运出口单", "D2"),
                    _ => ("未知单据类型", string.Empty)
                };
                var stateTransition = (job.JobState, jobState.Value);
                switch (stateTransition)
                {
                    case var _ when jobState.Value == 16:
                        if (!AuthorizationManager.Demand(out error, "F.2.9"))
                        {
                            if (!AuthorizationManager.Demand(out error, $"{prefix}.1.1.7")) return null;
                        }
                        break;
                    case (16, _):
                        if (!AuthorizationManager.Demand(out error, $"{prefix}.1.1.11")) return null;
                        break;
                }
            }
            if (operateState.HasValue)
            {
                var prefix = businessDoc switch
                {
                    PlIaDoc => "D1",
                    PlEaDoc => "D0",
                    PlIsDoc => "D3",
                    PlEsDoc => "D2",
                    _ => string.Empty
                };
                var currentStatus = businessDoc.Status;
                switch (true)
                {
                    case var _ when operateState.Value == 128:
                        if (!AuthorizationManager.Demand(out error, $"{prefix}.1.1.8")) return null;
                        break;
                    case var _ when currentStatus == 128 && operateState.Value != 128:
                        if (!AuthorizationManager.Demand(out error, $"{prefix}.1.1.12")) return null;
                        break;
                }
            }
            return businessDoc;
        }

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
        #endregion
    }
}
