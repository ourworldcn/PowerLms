/*
 * PlInvoices.DocFeeRequisition.cs 文件
 * 这个文件包含了在 DocFeeRequisition 和 DocFeeRequisitionItem 添加/更改时触发相应处理的类，以及更新相关数据的类。
 * 作者: OW
 * 创建日期: 2025-02-10
 * 修改日期: 2025-02-10
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
    /// 定义与申请单触发器相关的常量。
    /// </summary>
    public static class DocFeeRequisitionTriggerConstants
    {
        /// <summary>
        /// 已更改申请单明细的键。
        /// </summary>
        public const string ChangedRequisitionItemIdsKey = "ChangedRequisitionItemIds";
    }

    /// <summary>
    /// 在 DocFeeRequisition 和 DocFeeRequisitionItem 添加/更改时触发相应处理的类，并在保存 DocFeeRequisition 和 DocFeeRequisitionItem 后，更新相关数据的类。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeRequisition>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IDbContextSaving<DocFeeRequisitionItem>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFeeRequisition>))]
    [OwAutoInjection(ServiceLifetime.Scoped, ServiceType = typeof(IAfterDbContextSaving<DocFeeRequisitionItem>))]
    public class DocFeeRequisitionTriggerHandler : IDbContextSaving<DocFeeRequisition>, IDbContextSaving<DocFeeRequisitionItem>, IAfterDbContextSaving<DocFeeRequisition>, IAfterDbContextSaving<DocFeeRequisitionItem>
    {
        #region 私有字段
        private readonly ILogger<DocFeeRequisitionTriggerHandler> _Logger;
        private readonly IServiceProvider _ServiceProvider;
        #endregion 私有字段

        #region 延迟获取的服务
        private BusinessLogicManager _BusinessLogic => _ServiceProvider.GetRequiredService<BusinessLogicManager>();
        #endregion 延迟获取的服务

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        public DocFeeRequisitionTriggerHandler(ILogger<DocFeeRequisitionTriggerHandler> logger, IServiceProvider serviceProvider)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        #endregion 构造函数

        #region Saving 方法
        /// <summary>
        /// 在 DocFeeRequisition 和 DocFeeRequisitionItem 添加/更改时，将其 ParentId（如果不为空）放在 HashSet 中。
        /// </summary>
        /// <param name="entities">当前实体条目集合。</param>
        /// <param name="service">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            if (!states.TryGetValue(DocFeeRequisitionTriggerConstants.ChangedRequisitionItemIdsKey, out var obj) || obj is not HashSet<Guid> parentIds)
            {
                parentIds = new HashSet<Guid>();
                states[DocFeeRequisitionTriggerConstants.ChangedRequisitionItemIdsKey] = parentIds;
            }

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
                    _Logger.LogDebug("ParentId {0} added to HashSet.", id.Value);
                }
            }
        }
        #endregion Saving 方法

        #region AfterSaving 方法
        /// <summary>
        /// 在保存 DocFeeRequisition 和 DocFeeRequisitionItem 后，从 HashSet 中获取父申请单的 ID，计算并更新其相关数据。
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例。</param>
        /// <param name="serviceProvider">服务提供者。</param>
        /// <param name="states">状态字典。</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            if (states.TryGetValue(DocFeeRequisitionTriggerConstants.ChangedRequisitionItemIdsKey, out var obj) && obj is HashSet<Guid> parentIds)
            {
                var requisitions = dbContext.Set<DocFeeRequisition>().Where(c => parentIds.Contains(c.Id)).ToArray(); // 加载所有用到的 DocFeeRequisition 对象
                var lkupRequisitionItem = dbContext.Set<DocFeeRequisitionItem>().Where(c => parentIds.Contains(c.ParentId.Value)).AsEnumerable().ToLookup(c => c.ParentId.Value); // 加载所有用到的 DocFeeRequisitionItem 对象

                var financialManager = serviceProvider.GetRequiredService<FinancialManager>();

                foreach (var requisition in requisitions)
                {
                    if (financialManager.GetRequisitionAmountAndIO(lkupRequisitionItem[requisition.Id], out decimal amount, out bool isOut, dbContext))
                    {
                        requisition.Amount = amount;
                        requisition.IO = isOut;
                        dbContext.Update(requisition);
                    }
                }
            }
        }
        #endregion AfterSaving 方法
    }
}
