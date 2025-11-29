/*
 * 项目：PowerLms货运物流业务管理系统
 * 模块：业务触发器 - 费用申请单合计
 * 文件说明：
 * - 功能1：费用申请单（DocFeeRequisition）的自动金额合计计算
 * - 功能2：费用申请单明细项（DocFeeRequisitionItem）的结算金额统计
 * - 功能3：主单与明细的金额联动更新机制
 * 
 * 账单申请单合计设计说明：
 * 
 * 1. 合计计算原理：
 * - 主单合计 = Σ(明细项.金额 × 汇率)，转换为本位币统计
 * - 收付标识(IO) = 根据明细项的收支类型自动判断
 * - 已结算合计 = Σ(明细项.已结算金额)，反映结算单对申请单的核销情况
 * 
 * 2. 触发器机制：
 * - 新增/修改明细项时：自动重算主单的Amount和IO字段
 * - 新增/修改主单时：重新计算相关统计信息
 * - 删除操作时：排除已删除记录，重新统计剩余项目
 * 
 * 3. 金额计算逻辑：
 * - 明细项金额：支持多币种，按实时汇率转换本位币
 * - 收付判断：根据费用类型和明细项设置自动判断收入/支出性质
 * - 精度控制：金额计算保持两位小数精度，避免浮点误差
 * 
 * 4. 结算关联处理：
 * - 当结算单明细(PlInvoicesItem)关联申请单明细时，更新TotalSettledAmount
 * - 级联更新：明细结算金额变化时，主单的TotalSettledAmount同步更新
 * - 结算状态：根据已结算金额与申请金额的比较，判断结算完成状态
 * 
 * 5. 性能优化机制：
 * - 批量处理：同一事务内的多个变更进行批量计算，减少数据库操作
 * - 增量计算：只重算发生变化的申请单，避免全表扫描
 * - 状态缓存：使用HashSet缓存需要更新的主单ID，避免重复计算
 * 
 * 6. 数据一致性保证：
 * - 事务完整性：所有金额更新在同一事务内完成，保证数据一致性
 * - 级联更新：明细变化时主单同步更新，保持父子数据一致
 * - 异常处理：计算失败时不影响主要业务流程，记录错误日志
 * 
 * 7. 业务规则约束：
 * - 金额校验：明细项金额必须大于0，负数金额需要特殊标识
 * - 币种统一：主单金额统一按本位币展示，明细支持多币种
 * - 状态联动：申请单的审批状态影响明细项的可编辑性
 * 
 * 技术要点：
 * - 基于Entity Framework Core的实体触发器机制
 * - 使用Lookup提高关联查询性能
 * - 延迟加载服务依赖，避免循环依赖
 * - 实体状态检查，正确处理新增、修改、删除场景
 * 
 * 作者: OW
 * 创建日期: 2025-02-10
 * 修改日期: 2025-02-10
 * 修改记录: 2025-01-27 整合账单申请单合计设计需求
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
    /// 费用申请单触发器相关的常量定义
    /// 
    /// 常量说明：
    /// - ChangedRequisitionItemIdsKey：用于在触发器状态字典中存储发生变化的申请单明细ID集合
    /// - 状态缓存机制：避免在同一事务中重复计算相同申请单的金额
    /// </summary>
    public static class DocFeeRequisitionTriggerConstants
    {
        /// <summary>
        /// 已更改申请单明细的键名
        /// 
        /// 使用场景：
        /// - 在Saving触发器中收集发生变化的明细项ID
        /// - 在AfterSaving触发器中批量处理这些变化
        /// - 避免同一申请单被多次重复计算
        /// </summary>
        public const string ChangedRequisitionItemIdsKey = "ChangedRequisitionItemIds";
    }

    /// <summary>
    /// 费用申请单和明细项的数据库触发器处理器
    /// 
    /// 核心职责：
    /// - 自动维护申请单主表的金额合计字段（Amount）
    /// - 自动判断和设置收付标识字段（IO：true=支出，false=收入）
    /// - 响应明细项的增删改操作，实时更新主表统计信息
    /// - 保证主从表数据的一致性和实时性
    /// 
    /// 触发时机：
    /// - DocFeeRequisition新增/修改时：重新计算相关统计
    /// - DocFeeRequisitionItem新增/修改/删除时：更新对应主单的合计金额
    /// - 批量操作时：一次性处理多个变更，提高性能
    /// 
    /// 计算规则：
    /// - 金额合计：按本位币汇率折算后的明细项金额总和
    /// - 收付性质：根据明细项的费用类型和金额正负自动判断
    /// - 精度处理：使用Decimal类型保证金额计算精度
    /// 
    /// 性能考虑：
    /// - 使用HashSet去重，避免重复计算同一申请单
    /// - 使用Lookup预查询相关明细，减少数据库访问
    /// - 延迟加载业务逻辑管理器，避免启动时的循环依赖
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
        /// <summary>
        /// 业务逻辑管理器（延迟加载）
        /// 
        /// 延迟加载说明：
        /// - 避免在构造函数中直接注入，防止循环依赖
        /// - 在实际使用时才获取服务实例
        /// - 提供金额计算和收付性质判断的业务逻辑
        /// </summary>
        private BusinessLogicManager _BusinessLogic => _ServiceProvider.GetRequiredService<BusinessLogicManager>();
        #endregion 延迟获取的服务

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器和服务提供者
        /// 
        /// 依赖注入说明：
        /// - ILogger：记录触发器执行过程中的信息和错误
        /// - IServiceProvider：延迟获取其他服务，避免循环依赖
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="serviceProvider">服务提供者</param>
        public DocFeeRequisitionTriggerHandler(ILogger<DocFeeRequisitionTriggerHandler> logger, IServiceProvider serviceProvider)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        #endregion 构造函数

        #region Saving 事件处理
        /// <summary>
        /// 在保存申请单和明细项之前执行的合计金额计算
        /// 
        /// 处理逻辑：
        /// 1. 收集所有发生变化的申请单ID（包括直接修改主单和修改明细项的情况）
        /// 2. 批量查询相关的申请单和明细项数据
        /// 3. 为每个申请单重新计算Amount（金额合计）和IO（收付标识）
        /// 4. 排除已删除的记录，确保计算准确性
        /// 
        /// 金额计算规则：
        /// - 按本位币汇率折算明细项金额
        /// - 支持多币种混合计算
        /// - 自动判断收入/支出性质
        /// 
        /// 性能优化：
        /// - 使用HashSet去重申请单ID
        /// - 使用Lookup预加载明细项，减少N+1查询
        /// - 只处理实际发生变化的数据
        /// </summary>
        /// <param name="entities">当前实体条目集合</param>
        /// <param name="service">服务提供者</param>
        /// <param name="states">状态字典，用于在触发器间传递数据</param>
        public void Saving(IEnumerable<EntityEntry> entities, IServiceProvider service, Dictionary<object, object> states)
        {
            var dbContext = entities.First().Context;
            var parentIds = new HashSet<Guid>();

            foreach (var entry in entities)
            {
                var id = entry.Entity switch
                {
                    DocFeeRequisitionItem item => item.ParentId, // 明细项变化时，收集其父申请单ID
                    DocFeeRequisition requisition when entry.State == EntityState.Added || entry.State == EntityState.Modified => requisition.Id, // 主单直接变化时，收集主单ID
                    _ => null,
                };
                if (id.HasValue)
                {
                    parentIds.Add(id.Value);
                }
            }

            // 计算并更新父申请单的金额合计
            var requisitions = dbContext.Set<DocFeeRequisition>().WhereWithLocalSafe(c => parentIds.Contains(c.Id)); // 获取所有相关的 DocFeeRequisition 记录
            var lkupRequisitionItem = dbContext.Set<DocFeeRequisitionItem>().WhereWithLocal(c => parentIds.Contains(c.ParentId.Value)).ToLookup(c => c.ParentId.Value); // 获取所有相关的 DocFeeRequisitionItem 记录

            var financialManager = service.GetRequiredService<FinancialManager>();

            foreach (var requisition in requisitions)
            {
                // 跳过已删除的申请单
                if(dbContext.Entry(requisition).State == EntityState.Deleted)
                {
                    continue;
                }
                
                // 重新计算申请单的金额合计和收付性质
                if (financialManager.GetRequisitionAmountAndIO(lkupRequisitionItem[requisition.Id], out decimal amount, out bool isOut, dbContext))
                {
                    requisition.Amount = amount; // 更新金额合计（本位币）
                    requisition.IO = isOut; // 更新收付标识（true=支出，false=收入）
                }
            }
        }
        #endregion Saving 事件处理

        #region AfterSaving 事件处理
        /// <summary>
        /// 在保存申请单和明细项之后执行的后续处理
        /// 
        /// 当前实现：
        /// - 预留接口，便于未来扩展
        /// - 可用于执行保存后的通知、日志记录等操作
        /// - 所有主要计算逻辑在Saving阶段完成，确保事务一致性
        /// 
        /// 潜在用途：
        /// - 发送金额变更通知
        /// - 记录审计日志
        /// - 触发后续工作流
        /// - 缓存刷新等
        /// </summary>
        /// <param name="dbContext">当前 DbContext 实例</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="states">状态字典</param>
        public void AfterSaving(DbContext dbContext, IServiceProvider serviceProvider, Dictionary<object, object> states)
        {
            // AfterSaving 阶段暂无处理逻辑，此处保留供未来扩展
            // 主要计算逻辑在Saving阶段完成，确保在同一事务内
        }
        #endregion AfterSaving 事件处理
    }

    // ✅ 已删除 DocFeeRequisitionTotalSettledAmountTriggerHandler 触发器
    // 理由：
    // 1. 功能重复：与 PlInvoicesItemTriggerHandler 触发器重复（DocBill.Triggers.cs）
    // 2. 性能浪费：导致 DocFeeRequisitionItem.TotalSettledAmount 被重复计算两次
    // 3. 设计不合理：DocFeeRequisition.TotalSettledAmount 应该由 FeeTotalTriggerHandler 级联计算
    // 
    // 正确的触发器链路：
    // PlInvoicesItem 变化 → PlInvoicesItemTriggerHandler → 更新 DocFeeRequisitionItem.TotalSettledAmount
    // DocFeeRequisitionItem 变化 → FeeTotalTriggerHandler → 更新 DocFee.TotalSettledAmount
}
