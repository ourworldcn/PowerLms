using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLmsServer.Triggers
{
    /// <summary>
    /// 提供获取汇率和本币编码的服务类，以及定义与文档触发器相关的常量。
    /// </summary>
    public static class CombinedServices
    {
        /// <summary>
        /// 已更改文档明细的键。
        /// </summary>
        public const string ChangedDocFeeIdsKey = "ChangedDocFeeIds";

    }

    /// <summary>
    /// 在 DocFee 和 DocBill 添加/更改时触发相应处理的类，并在保存 DocFee 和 DocBill 后，更新父级结算单金额的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFee>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocBill>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFee>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocBill>))]
    public class DocFeeAndBillTriggerHandler : IDbContextSaving<DocFee>, IDbContextSaving<DocBill>, IAfterDbContextSaving<DocFee>, IAfterDbContextSaving<DocBill>
    {
        private readonly ILogger<DocFeeAndBillTriggerHandler> _Logger;
        BusinessLogicManager _BusinessLogic;

        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="businessLogic"></param>
        public DocFeeAndBillTriggerHandler(ILogger<DocFeeAndBillTriggerHandler> logger, BusinessLogicManager businessLogic)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _BusinessLogic = businessLogic;
        }

        /// <summary>
        /// 在 DocFee 和 DocBill 添加/更改时，将其 BillId（如果不为空）放在 HashSet 中。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="service">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            if (!states.TryGetValue(CombinedServices.ChangedDocFeeIdsKey, out var obj) || obj is not HashSet<Guid> billIds)
            {
                billIds = new HashSet<Guid>();
                states[CombinedServices.ChangedDocFeeIdsKey] = billIds;
            }
            foreach (var entry in entities)
            {
                var id = entry.Entity switch
                {
                    DocFee df => df.BillId,
                    DocBill docBill when entry.State == EntityState.Added || entry.State == EntityState.Modified => docBill.Id,
                    _ => null,
                };
                if (id.HasValue)
                {
                    billIds.Add(id.Value);
                }
            }
        }

        /// <summary>
        /// 在保存 DocFee 和 DocBill 后，从 HashSet 中获取父结算单的 ID，计算并更新其金额。
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (states.TryGetValue(CombinedServices.ChangedDocFeeIdsKey, out var obj) && obj is HashSet<Guid> billIds)
            {
                var bills = dbContext.Set<DocBill>().Where(c => billIds.Contains(c.Id)).ToArray(); // 加载所有用到的 DocBill 对象
                var lkupFee = dbContext.Set<DocFee>().Where(c => billIds.Contains(c.BillId.Value)).AsEnumerable().ToLookup(c => c.BillId.Value); // 加载所有用到的 DocFee 对象

                foreach (var bill in bills)
                {
                    var bcCode = _BusinessLogic.GetEntityBaseCurrencyCode(bill.Id, typeof(DocBill));
                    if (bcCode == bill.CurrTypeId)  // 如果本币与账单的币种相同，则不需要转换
                        bill.Amount = lkupFee[bill.Id].Sum(c => Math.Round(c.Amount * c.ExchangeRate, 4, MidpointRounding.AwayFromZero));
                    else
                    {
                        var jobId = _BusinessLogic.GetJobIdByBillId(bill.Id);
                        if (jobId is not null)  //若账单关联了工作，则使用工作的组织机构Id，否则忽略
                        {
                            var job = dbContext.Set<PlJob>().Find(jobId);
                            var orgId = job.OrgId.Value;
                            bill.Amount = lkupFee[bill.Id].Sum(c =>
                            {
                                var rate = _BusinessLogic.GetExchageRate(job.OrgId.Value, c.Currency, bill.CurrTypeId);
                                return Math.Round(c.Amount * rate, 4, MidpointRounding.AwayFromZero);
                            });
                        }
                    }

                    // 更新 DocFee 的 TotalRequestedAmount 和 TotalSettledAmount 字段
                    foreach (var fee in lkupFee[bill.Id])
                    {
                        fee.TotalRequestedAmount = dbContext.Set<DocFeeRequisitionItem>().Where(r => r.FeeId == fee.Id).
                            Sum(r => r.Amount);

                        // 计算 TotalSettledAmount
                        var settledAmount = dbContext.Set<PlInvoicesItem>()
                            .Where(i => i.RequisitionItemId == fee.Id)
                            .Sum(i => i.Amount * i.ExchangeRate);

                        fee.TotalSettledAmount = settledAmount;
                        dbContext.Update(fee);
                    }

                    dbContext.Update(bill);
                }
            }
        }
    }
}
