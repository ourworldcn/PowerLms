/*
 * 项目：PowerLms | 模块：OA费用申请单管理
 * 功能：OA日常费用申请单的业务逻辑管理，包括回退、状态管理等功能
 * 技术要点：依赖注入、服务层业务逻辑、工作流管理
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 实现OA费用申请单回退功能
 */

using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using System;

namespace PowerLmsServer.Managers.OA
{
    /// <summary>
    /// OA费用申请单管理器。
    /// 负责OA日常费用申请单的业务逻辑处理，包括状态管理、回退等功能。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OaExpenseManager
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
    }

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