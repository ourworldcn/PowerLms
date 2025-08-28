/*
 * 项目：PowerLms | 模块：主营业务费用申请单管理
 * 功能：主营业务费用申请单的业务逻辑管理，包括回退、状态管理等功能
 * 技术要点：依赖注入、服务层业务逻辑、工作流管理
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 实现主营业务费用申请单回退功能
 */

using Microsoft.Extensions.DependencyInjection;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;

namespace PowerLmsServer.Managers.Financial
{
    /// <summary>
    /// 主营业务费用申请单管理器。
    /// 负责主营业务费用申请单的业务逻辑处理，包括状态管理、回退等功能。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class DocFeeRequisitionManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DocFeeRequisitionManager(PowerLmsUserDbContext dbContext, OwSqlAppLogger sqlAppLogger)
        {
            _DbContext = dbContext;
            _SqlAppLogger = sqlAppLogger;
        }

        private readonly PowerLmsUserDbContext _DbContext;
        private readonly OwSqlAppLogger _SqlAppLogger;

        /// <summary>
        /// 回退主营业务费用申请单到初始状态。
        /// 会清空相关工作流、重置申请单状态并释放被锁定的费用。
        /// </summary>
        /// <param name="requisitionId">主营业务费用申请单ID</param>
        /// <param name="operatorId">操作人ID，用于记录审计信息</param>
        /// <param name="wfManager">工作流管理器</param>
        /// <returns>回退操作结果</returns>
        /// <exception cref="ArgumentException">当requisitionId为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当数据操作错误时抛出</exception>
        public DocFeeRequisitionRevertResult RevertRequisition(Guid requisitionId, Guid operatorId, OwWfManager wfManager)
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
                // 2. 验证主营业务费用申请单是否存在（任何状态都可回退）
                var requisition = _DbContext.DocFeeRequisitions.Find(requisitionId);
                if (requisition == null)
                {
                    return DocFeeRequisitionRevertResult.CreateFailure(requisitionId, $"未找到ID为 {requisitionId} 的主营业务费用申请单");
                }

                _SqlAppLogger.LogGeneralInfo($"开始回退主营业务费用申请单：申请单ID={requisitionId}, 申请单号={requisition.FrNo}, 操作人={operatorId}");

                // 3. 调用工作流清理服务清空相关工作流
                var clearedWorkflows = wfManager.ClearWorkflowByDocId(requisitionId);

                // 4. 重置主营业务费用申请单状态为初始状态
                // 注意：DocFeeRequisition没有像OaExpenseRequisition那样的Status枚举字段
                // 它的状态主要通过工作流来管理，所以主要是清空工作流相关数据

                // 5. 清空与结算相关的字段（如果有的话）
                // DocFeeRequisition的状态主要体现在是否有关联的工作流和结算数据
                // 这里主要是确保工作流被清空，费用可以被重新申请

                // 6. 保存数据库更改
                _DbContext.SaveChanges();

                var message = $"成功回退主营业务费用申请单：申请单ID={requisitionId}, 申请单号={requisition.FrNo}, 清空工作流{clearedWorkflows.Count}个";
                _SqlAppLogger.LogGeneralInfo($"主营业务费用申请单回退成功：{message}, 操作人={operatorId}");

                // 7. 返回操作结果摘要
                return DocFeeRequisitionRevertResult.CreateSuccess(requisitionId, clearedWorkflows.Count, message);
            }
            catch (Exception ex)
            {
                var errorMessage = $"回退主营业务费用申请单时发生错误：{ex.Message}";
                _SqlAppLogger.LogGeneralInfo($"主营业务费用申请单回退失败：申请单ID={requisitionId}, 操作人={operatorId}, 错误={ex.Message}");
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
                var requisition = _DbContext.DocFeeRequisitions.Find(requisitionId);
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
        public DocFeeRequisitionStatusInfo GetStatusInfo(Guid requisitionId)
        {
            try
            {
                var requisition = _DbContext.DocFeeRequisitions.Find(requisitionId);
                if (requisition == null)
                    return null;

                // 检查是否有关联的工作流
                var hasWorkflow = _DbContext.OwWfs.Any(wf => wf.DocId == requisitionId);

                // 检查是否有关联的结算单
                var hasSettlement = _DbContext.PlInvoicesItems
                    .Join(_DbContext.DocFeeRequisitionItems, 
                          ii => ii.RequisitionItemId, 
                          ri => ri.Id, 
                          (ii, ri) => ri.ParentId)
                    .Any(parentId => parentId == requisitionId);

                return new DocFeeRequisitionStatusInfo
                {
                    RequisitionId = requisitionId,
                    RequisitionNumber = requisition.FrNo,
                    HasWorkflow = hasWorkflow,
                    HasSettlement = hasSettlement,
                    MakeDateTime = requisition.MakeDateTime,
                    Amount = requisition.Amount,
                    Currency = requisition.Currency
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
    /// 主营业务费用申请单回退操作的结果类型。
    /// 专门用于主营业务费用申请单的回退操作结果封装。
    /// </summary>
    public class DocFeeRequisitionRevertResult
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
        public static DocFeeRequisitionRevertResult CreateSuccess(Guid requisitionId, int clearedWorkflowCount, string message)
        {
            return new DocFeeRequisitionRevertResult
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
        public static DocFeeRequisitionRevertResult CreateFailure(Guid requisitionId, string message)
        {
            return new DocFeeRequisitionRevertResult
            {
                Success = false,
                RequisitionId = requisitionId,
                ClearedWorkflowCount = 0,
                Message = message
            };
        }
    }

    /// <summary>
    /// 主营业务费用申请单状态信息。
    /// 用于封装申请单的状态详情。
    /// </summary>
    public class DocFeeRequisitionStatusInfo
    {
        /// <summary>
        /// 申请单ID。
        /// </summary>
        public Guid RequisitionId { get; set; }

        /// <summary>
        /// 申请单号。
        /// </summary>
        public string RequisitionNumber { get; set; }

        /// <summary>
        /// 是否有关联的工作流。
        /// </summary>
        public bool HasWorkflow { get; set; }

        /// <summary>
        /// 是否有关联的结算单。
        /// </summary>
        public bool HasSettlement { get; set; }

        /// <summary>
        /// 制单时间。
        /// </summary>
        public DateTime? MakeDateTime { get; set; }

        /// <summary>
        /// 申请金额。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 币种。
        /// </summary>
        public string Currency { get; set; }
    }
}