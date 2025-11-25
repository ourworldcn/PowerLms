/*
 * 项目：PowerLms | 模块：OA费用申请单管理
 * 功能：OA日常费用申请单的业务逻辑管理，包括回退、状态管理、工作流回调等功能
 * 技术要点：依赖注入、服务层业务逻辑、工作流管理、工作流状态变更回调
 * 作者：zc | 创建：2025-01 | 修改：2025-02-06 集成工作流回调机制
 */

using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers.Workflow;
using System;
using System.Linq;

namespace PowerLmsServer.Managers.OA
{
    /// <summary>
    /// OA费用申请单管理器。
    /// 负责OA日常费用申请单的业务逻辑处理，包括状态管理、回退、工作流回调等功能。
    /// 
    /// 支持的业务类型（KindCode）：

    /// - OA_expense_reimb：OA费用报销
    /// - OA_expense_loan：OA费用借款
    /// - OA_exchange_income：OA外汇收入
    /// - OA_exchange_expense：OA外汇支出
    /// 
    /// 工作流回调状态同步规则：
    /// 1. 工作流创建时：Draft(0) → InApproval(1)
    /// 2. 工作流完成（State=1）：InApproval(1) → ApprovedPendingSettlement(2)
    /// 3. 工作流拒绝（State=2）：InApproval(1) → Rejected(32)
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OaExpenseManager : IWorkflowCallback
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OaExpenseManager(PowerLmsUserDbContext dbContext, OwSqlAppLogger sqlAppLogger)
        {
            _DbContext = dbContext;
            _SqlAppLogger = sqlAppLogger;
        }

        private readonly PowerLmsUserDbContext _DbContext;
        private readonly OwSqlAppLogger _SqlAppLogger;
        private readonly JobManager _JobManager;

        /// <summary>
        /// 回退OA费用申请单到初始状态。
        /// 会清空相关工作流、重置申请单状态并释放被锁定的费用。
        /// </summary>
        /// <param name="requisitionId">OA费用申请单ID</param>
        /// <param name="operatorId">操作人ID，用于记录审计信息</param>
        /// <param name="wfManager">工作流管理器</param>
        /// <returns>回退操作结果</returns>
        /// <exception cref="ArgumentException">当requisitionId为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当数据操作错误时抛出</exception>
        public OaExpenseRevertResult RevertRequisition(Guid requisitionId, Guid operatorId, OwWfManager wfManager)
        {
            // 1. 参数有效性验证
            if (requisitionId == Guid.Empty)
                throw new ArgumentException("申请单ID不能为空", nameof(requisitionId));

            if (operatorId == Guid.Empty)
                throw new ArgumentException("操作人ID不能为空", nameof(operatorId));

            if (wfManager == null)
                throw new ArgumentNullException(nameof(wfManager));

            try
            {
                // 2. 验证OA费用申请单是否存在（任何状态都可回退）
                var requisition = _DbContext.OaExpenseRequisitions.Find(requisitionId);
                if (requisition == null)
                {
                    return OaExpenseRevertResult.CreateFailure(requisitionId, $"未找到ID为 {requisitionId} 的OA费用申请单");
                }

                var originalStatus = requisition.Status;
                _SqlAppLogger.LogGeneralInfo($"开始回退OA费用申请单：申请单ID={requisitionId}, 当前状态={originalStatus}, 操作人={operatorId}");

                // 3. 调用工作流清理服务清空相关工作流
                var clearedWorkflows = wfManager.ClearWorkflowByDocId(requisitionId);

                // 4. 重置OA费用申请单状态为草稿状态（初始状态）
                requisition.Status = OaExpenseStatus.Draft;
                requisition.AuditDateTime = null;
                requisition.AuditOperatorId = null;
                requisition.SettlementDateTime = null;
                requisition.SettlementOperatorId = null;
                requisition.SettlementMethod = null;
                requisition.SettlementRemark = null;
                requisition.ConfirmDateTime = null;
                requisition.ConfirmOperatorId = null;
                requisition.BankFlowNumber = null;
                requisition.ConfirmRemark = null;

                // 5. 保存数据库更改
                _DbContext.SaveChanges();

                var message = $"成功回退OA费用申请单：申请单ID={requisitionId}, 状态从{originalStatus}回退为{requisition.Status}, 清空工作流{clearedWorkflows.Count}个";
                _SqlAppLogger.LogGeneralInfo($"OA费用申请单回退成功：{message}, 操作人={operatorId}");

                // 6. 返回操作结果摘要
                return OaExpenseRevertResult.CreateSuccess(requisitionId, clearedWorkflows.Count, message);
            }
            catch (Exception ex)
            {
                var errorMessage = $"回退OA费用申请单时发生错误：{ex.Message}";
                _SqlAppLogger.LogGeneralInfo($"OA费用申请单回退失败：申请单ID={requisitionId}, 操作人={operatorId}, 错误={ex.Message}");
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        /// <summary>
        /// 验证申请单是否可以进行回退操作。
        /// </summary>
        /// <param name="requisitionId">申请单ID</param>
        /// <returns>是否可以回退</returns>
        public bool CanRevert(Guid requisitionId)
        {
            try
            {
                var requisition = _DbContext.OaExpenseRequisitions.Find(requisitionId);
                if (requisition == null)
                    return false;

                // 根据会议纪要，业务在任何状态下都可能被清空工作流并回退到工作流的初始状态
                // 因此这里总是返回true，但可以根据具体业务需求添加限制条件
                return true;
            }
            catch (Exception ex)
            {
                _SqlAppLogger.LogGeneralInfo($"验证申请单回退权限时发生错误：申请单ID={requisitionId}, 错误={ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取申请单的当前状态信息。
        /// </summary>
        /// <param name="requisitionId">申请单ID</param>
        /// <returns>状态信息，如果申请单不存在则返回null</returns>
        public OaExpenseStatusInfo GetStatusInfo(Guid requisitionId)
        {
            try
            {
                var requisition = _DbContext.OaExpenseRequisitions.Find(requisitionId);
                if (requisition == null)
                    return null;

                return new OaExpenseStatusInfo
                {
                    RequisitionId = requisitionId,
                    Status = requisition.Status,
                    IsAudited = requisition.AuditDateTime.HasValue,
                    IsSettled = requisition.SettlementDateTime.HasValue,
                    IsConfirmed = requisition.ConfirmDateTime.HasValue,
                    AuditDateTime = requisition.AuditDateTime,
                    SettlementDateTime = requisition.SettlementDateTime,
                    ConfirmDateTime = requisition.ConfirmDateTime
                };
            }
            catch (Exception ex)
            {
                _SqlAppLogger.LogGeneralInfo($"获取申请单状态信息时发生错误：申请单ID={requisitionId}, 错误={ex.Message}");
                return null;
            }
        }

        #region 工作流回调

        /// <summary>
        /// 工作流状态变更回调入口。
        /// 从本地缓存读取数据，不进行数据库查询，不调用 SaveChanges()。
        /// 
        /// 回调触发时机：
        /// 1. 工作流创建时（首次发起审批）：需要更新申请单状态为 InApproval
        /// 2. 工作流完成时（State=1）：更新申请单状态为 ApprovedPendingSettlement
        /// 3. 工作流拒绝时（State=2）：更新申请单状态为 Rejected
        /// </summary>
        public void OnWorkflowStateChanged(OwWf workflow, PowerLmsUserDbContext dbContext)
        {
            if (workflow == null || !workflow.DocId.HasValue)
                return;
            try
            {
                var template = dbContext.ChangeTracker.Entries<OwWfTemplate>()
                    .Where(e => e.Entity.Id == workflow.TemplateId)
                    .Select(e => e.Entity)
                    .FirstOrDefault();
                if (template == null)
                {
                    template = dbContext.WfTemplates.Local.FirstOrDefault(t => t.Id == workflow.TemplateId);
                }
                if (template == null)
                {
                    _SqlAppLogger.LogGeneralInfo($"OA费用回调：未找到工作流模板（本地缓存），WorkflowId={workflow.Id}");
                    return;
                }
                switch (template.KindCode)
                {
                    case "OA_expense_reimb":
                    case "OA_expense_loan":
                    case "OA_exchange_income":
                    case "OA_exchange_expense":
                        SyncOaExpenseStatus(workflow.DocId.Value, workflow.State, dbContext);
                        break;
                    default:
                        return;
                }
            }
            catch (Exception ex)
            {
                _SqlAppLogger.LogGeneralInfo($"OA费用回调异常：WorkflowId={workflow.Id}, DocId={workflow.DocId}, 错误={ex.Message}");
            }
        }

        /// <summary>
        /// 同步OA申请单状态（从本地缓存读取，不调用 SaveChanges()）。
        /// 
        /// 状态同步规则：
        /// 1. State=0（流转中）：Draft(0) → InApproval(1)
        /// 2. State=1（成功完成）：InApproval(1) → ApprovedPendingSettlement(2)
        /// 3. State=2（已被终止）：InApproval(1) → Rejected(32)
        /// </summary>
        /// <param name="requisitionId">申请单ID</param>
        /// <param name="wfState">工作流状态：0=流转中，1=成功完成，2=已被终止</param>
        /// <param name="dbContext">数据库上下文</param>
        private void SyncOaExpenseStatus(Guid requisitionId, byte wfState, PowerLmsUserDbContext dbContext)
        {
            var requisition = dbContext.ChangeTracker.Entries<OaExpenseRequisition>()
                .Where(e => e.Entity.Id == requisitionId)
                .Select(e => e.Entity)
                .FirstOrDefault();
            if (requisition == null)
            {
                requisition = dbContext.OaExpenseRequisitions.Local.FirstOrDefault(r => r.Id == requisitionId);
            }
            if (requisition == null)
            {
                _SqlAppLogger.LogGeneralInfo($"OA费用回调：申请单不存在（本地缓存），RequisitionId={requisitionId}");
                return;
            }
            var oldStatus = requisition.Status;
            switch (wfState)
            {
                case 0:
                    if (requisition.Status == OaExpenseStatus.Draft)
                    {
                        requisition.Status = OaExpenseStatus.InApproval;
                        _SqlAppLogger.LogGeneralInfo($"✅ OA费用回调：工作流创建，状态同步成功。RequisitionId={requisitionId}, {oldStatus} → {requisition.Status}");
                    }
                    break;
                case 1:
                    if (requisition.Status == OaExpenseStatus.InApproval)
                    {
                        requisition.Status = OaExpenseStatus.ApprovedPendingSettlement;
                        requisition.AuditDateTime = OwHelper.WorldNow;
                        _SqlAppLogger.LogGeneralInfo($"✅ OA费用回调：审批通过，状态同步成功。RequisitionId={requisitionId}, {oldStatus} → {requisition.Status}");
                    }
                    break;
                case 2:
                    if (requisition.Status == OaExpenseStatus.InApproval)
                    {
                        requisition.Status = OaExpenseStatus.Rejected;
                        requisition.AuditDateTime = null;
                        requisition.AuditOperatorId = null;
                        _SqlAppLogger.LogGeneralInfo($"✅ OA费用回调：审批拒绝，状态同步成功。RequisitionId={requisitionId}, {oldStatus} → Rejected(32)");
                    }
                    break;
            }
        }

        #endregion 工作流回调

        /// <summary>
        /// OA费用申请单回退操作的结果类型。
        /// 专门用于OA费用申请单的回退操作结果封装。
        /// </summary>
        public class OaExpenseRevertResult
        {
            /// <summary>
            /// 操作是否成功的布尔值。
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// 申请单ID，用于确认操作目标。
            /// </summary>
            public Guid RequisitionId { get; set; }

            /// <summary>
            /// 清空的工作流数量，用于审计统计。
            /// </summary>
            public int ClearedWorkflowCount { get; set; }

            /// <summary>
            /// 操作结果描述信息。
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// 创建成功的回退结果。
            /// </summary>
            /// <param name="requisitionId">申请单ID</param>
            /// <param name="clearedWorkflowCount">清空的工作流数量</param>
            /// <param name="message">操作描述信息</param>
            /// <returns>成功的回退结果</returns>
            public static OaExpenseRevertResult CreateSuccess(Guid requisitionId, int clearedWorkflowCount, string message)
            {
                return new OaExpenseRevertResult
                {
                    Success = true,
                    RequisitionId = requisitionId,
                    ClearedWorkflowCount = clearedWorkflowCount,
                    Message = message
                };
            }

            /// <summary>
            /// 创建失败的回退结果。
            /// </summary>
            /// <param name="requisitionId">申请单ID</param>
            /// <param name="message">失败描述信息</param>
            /// <returns>失败的回退结果</returns>
            public static OaExpenseRevertResult CreateFailure(Guid requisitionId, string message)
            {
                return new OaExpenseRevertResult
                {
                    Success = false,
                    RequisitionId = requisitionId,
                    ClearedWorkflowCount = 0,
                    Message = message
                };
            }
        }

        /// <summary>
        /// OA费用申请单状态信息。
        /// 用于封装申请单的状态详情。
        /// </summary>
        public class OaExpenseStatusInfo
        {
            /// <summary>
            /// 申请单ID。
            /// </summary>
            public Guid RequisitionId { get; set; }

            /// <summary>
            /// 当前状态。
            /// </summary>
            public OaExpenseStatus Status { get; set; }

            /// <summary>
            /// 是否已审核。
            /// </summary>
            public bool IsAudited { get; set; }

            /// <summary>
            /// 是否已结算。
            /// </summary>
            public bool IsSettled { get; set; }

            /// <summary>
            /// 是否已确认。
            /// </summary>
            public bool IsConfirmed { get; set; }

            /// <summary>
            /// 审核时间。
            /// </summary>
            public DateTime? AuditDateTime { get; set; }

            /// <summary>
            /// 结算时间。
            /// </summary>
            public DateTime? SettlementDateTime { get; set; }

            /// <summary>
            /// 确认时间。
            /// </summary>
            public DateTime? ConfirmDateTime { get; set; }
        }
    }
}