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
    public class DocFeeAndBillTriggerHandler : IDbContextSaving<DocFee>, IDbContextSaving<DocBill>
    {
        private readonly ILogger<DocFeeAndBillTriggerHandler> _Logger;
        private readonly BusinessLogicManager _BusinessLogic;

        /// <summary>
        /// 构造函数，初始化日志记录器和业务逻辑管理器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="businessLogic">业务逻辑管理器。</param>
        public DocFeeAndBillTriggerHandler(ILogger<DocFeeAndBillTriggerHandler> logger, BusinessLogicManager businessLogic)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _BusinessLogic = businessLogic;
        }

        /// <summary>
        /// 在 DocFee 和 DocBill 添加/更改时，将其 BillId（如果不为空）放在 HashSet 中，并计算父结算单的金额。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="service">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            var dbContext = entities.First().Context;
            var billIds = new HashSet<Guid>();

            foreach (var entry in entities)
            {
                if (entry.Entity is DocFee df && df.BillId.HasValue)
                {
                    billIds.Add(df.BillId.Value);
                }
                else if (entry.Entity is DocBill docBill && (entry.State == EntityState.Added || entry.State == EntityState.Modified))
                {
                    billIds.Add(docBill.Id);
                }
            }

            // 计算并更新父结算单的金额
            var bills = dbContext.Set<DocBill>().WhereWithLocalSafe(c => billIds.Contains(c.Id));  //获取数据库和缓存中的合集
            var lkupFee = dbContext.Set<DocFee>().WhereWithLocal(c => c.BillId.HasValue && billIds.Contains(c.BillId.Value)).ToLookup(c => c.BillId.Value); // 加载所有用到的 DocFee 对象

            foreach (var bill in bills)
            {
                if (dbContext.Entry(bill).State == EntityState.Deleted)  // 如果账单已被删除，则忽略
                {
                    continue;
                }
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
            }
        }
    }

    /// <summary>
    /// 在 DocFeeRequisitionItem 添加/更改时触发相应处理的类，并在保存 DocFeeRequisitionItem 后，更新相关费用的合计金额的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeRequisitionItem>))]
    public class FeeTotalTriggerHandler : IDbContextSaving<DocFeeRequisitionItem>
    {
        private readonly ILogger<FeeTotalTriggerHandler> _Logger;

        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public FeeTotalTriggerHandler(ILogger<FeeTotalTriggerHandler> logger)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 在 DocFeeRequisitionItem 添加/更改时，更新相关费用的合计金额。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="service">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            var dbContext = entities.First().Context;
            var feeIds = new HashSet<Guid>(entities.Select(c => c.Entity).OfType<DocFeeRequisitionItem>().Where(c => c.FeeId.HasValue).Select(c => c.FeeId.Value));
            foreach (var id in feeIds)
            {
                if (dbContext.Set<DocFee>().Find(id) is DocFee fee)
                {
                    if (dbContext.Entry(fee).State == EntityState.Deleted)
                    {
                        continue;
                    }
                    var rItems = fee.GetRequisitionItems(dbContext).ToArray();
                    fee.TotalSettledAmount = rItems.Sum(c => c.TotalSettledAmount);
                    fee.TotalRequestedAmount = rItems.Sum(c => c.Amount);
                }
            }
        }
    }
}