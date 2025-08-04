using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OW.Data;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// OA费用申请单控制器 - 其他操作部分。
    /// </summary>
    public partial class OaExpenseController
    {
        #region 凭证号生成功能

        /// <summary>
        /// 生成凭证号。
        /// 根据账期时间和结算账号生成符合财务要求的凭证号。
        /// </summary>
        /// <param name="model">凭证号生成参数</param>
        /// <returns>生成的凭证号信息</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="201">生成成功但存在重号警告。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定的结算账号不存在。</response>
        [HttpPost]
        public async Task<ActionResult<GenerateVoucherNumberReturnDto>> GenerateVoucherNumber(GenerateVoucherNumberParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GenerateVoucherNumberReturnDto();

            try
            {
                // 检查结算账号是否存在
                var settlementAccount = _DbContext.BankInfos.Find(model.SettlementAccountId);
                if (settlementAccount == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "指定的结算账号不存在";
                    return result;
                }

                // 检查凭证字是否配置
                if (string.IsNullOrEmpty(settlementAccount.VoucherCharacter))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "结算账号未配置凭证字，无法生成凭证号";
                    return result;
                }

                // 生成凭证号
                var period = model.AccountingPeriod.Month;
                var voucherCharacter = settlementAccount.VoucherCharacter;

                // 使用VoucherSequence表的乐观锁控制获取下一个序号
                var nextSequence = await GetNextVoucherSequenceAsync(context.User.OrgId.Value, period, voucherCharacter);

                // 生成凭证号：格式为"期间-凭证字-序号"
                var voucherNumber = $"{period}-{voucherCharacter}-{nextSequence}";

                // 检查是否存在重号（基于当前组织）
                var duplicateExists = CheckVoucherNumberDuplicateInOrg(voucherNumber, context.User.OrgId.Value);

                result.VoucherNumber = voucherNumber;
                result.VoucherCharacter = voucherCharacter;
                result.Period = period;
                result.SequenceNumber = nextSequence;
                result.HasDuplicateWarning = duplicateExists;

                if (duplicateExists)
                {
                    result.DuplicateWarningMessage = $"凭证号 {voucherNumber} 已存在，请核查是否重复";
                    _Logger.LogWarning("生成的凭证号存在重复: {VoucherNumber}", voucherNumber);

                    // 返回201状态码表示成功但有警告
                    return StatusCode(201, result);
                }

                _Logger.LogInformation("成功生成凭证号: {VoucherNumber}，账期: {Period}, 凭证字: {VoucherCharacter}",
                    voucherNumber, period, voucherCharacter);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "生成凭证号时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"生成凭证号时发生错误: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region 新增结算确认功能

        /// <summary>
        /// 执行OA费用申请单结算操作。
        /// 出纳权限：OA.1.2.1，工作流完成后将状态从"待结算"更新为"待确认"。
        /// </summary>
        /// <param name="model">结算参数</param>
        /// <returns>结算结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [HttpPost]
        public ActionResult<SettleOaExpenseRequisitionReturnDto> SettleOaExpenseRequisition(SettleOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new SettleOaExpenseRequisitionReturnDto();

            try
            {
                // 权限验证 - 需要OA.1.2.1权限
                string err;
                if (!_AuthorizationManager.Demand(out err, "OA.1.2.1"))
                    return StatusCode(403, err);

                var requisition = _DbContext.OaExpenseRequisitions.Find(model.RequisitionId);
                if (requisition == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "指定的OA费用申请单不存在";
                    return result;
                }

                // 多租户数据隔离检查
                if (!context.User.IsSuperAdmin && requisition.OrgId != context.User.OrgId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "权限不足，无法操作此申请单";
                    return result;
                }

                // 状态检查：必须是审批完成待结算状态
                if (requisition.Status != OaExpenseStatus.ApprovedPendingSettlement)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = $"申请单状态不正确，当前状态：{requisition.GetApprovalStatus()}，只能对待结算状态的申请单执行结算操作";
                    return result;
                }

                // 工作流状态检查：优先使用OwWfManager
                if (!IsWorkflowCompleted(requisition.Id))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "申请单工作流尚未完成，无法执行结算操作";
                    return result;
                }

                // 执行结算操作
                requisition.Status = OaExpenseStatus.SettledPendingConfirm;
                requisition.SettlementOperatorId = context.User.Id;
                requisition.SettlementDateTime = OwHelper.WorldNow;
                requisition.SettlementMethod = model.SettlementMethod;
                requisition.SettlementRemark = model.SettlementRemark;

                _DbContext.SaveChanges();

                result.SettlementDateTime = requisition.SettlementDateTime.Value;
                result.NewStatus = requisition.Status;

                _Logger.LogInformation("申请单结算完成 - 申请单ID: {RequisitionId}, 结算人: {OperatorId}, 结算方式: {Method}",
                    model.RequisitionId, context.User.Id, model.SettlementMethod);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "执行OA费用申请单结算操作时发生错误 - 申请单ID: {RequisitionId}", model.RequisitionId);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"执行结算操作时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 执行OA费用申请单确认操作。
        /// 会计权限：OA.1.2.2，结算完成后将状态从"待确认"更新为"可导入财务"。
        /// </summary>
        /// <param name="model">确认参数</param>
        /// <returns>确认结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [HttpPost]
        public ActionResult<ConfirmOaExpenseRequisitionReturnDto> ConfirmOaExpenseRequisition(ConfirmOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ConfirmOaExpenseRequisitionReturnDto();

            try
            {
                // 权限验证 - 需要OA.1.2.2权限
                string err;
                if (!_AuthorizationManager.Demand(out err, "OA.1.2.2"))
                    return StatusCode((int)HttpStatusCode.Forbidden, err);

                var requisition = _DbContext.OaExpenseRequisitions.Find(model.RequisitionId);
                if (requisition == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "指定的OA费用申请单不存在";
                    return result;
                }

                // 多租Tenant数据隔离检查
                if (!context.User.IsSuperAdmin && requisition.OrgId != context.User.OrgId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "权限不足，无法操作此申请单";
                    return result;
                }

                // 状态检查：必须是已结算待确认状态
                if (requisition.Status != OaExpenseStatus.SettledPendingConfirm)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = $"申请单状态不正确，当前状态：{requisition.GetApprovalStatus()}，只能对待确认状态的申请单执行确认操作";
                    return result;
                }

                // 职责分离检查：确认人不能是结算人
                if (context.User.Id == requisition.SettlementOperatorId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "职责分离限制：确认操作不能由结算人执行，请使用不同的账号进行确认";
                    return result;
                }

                // 执行确认操作
                requisition.Status = OaExpenseStatus.ConfirmedReadyForExport;
                requisition.ConfirmOperatorId = context.User.Id;
                requisition.ConfirmDateTime = OwHelper.WorldNow;
                requisition.BankFlowNumber = model.BankFlowNumber;
                requisition.ConfirmRemark = model.ConfirmRemark;

                _DbContext.SaveChanges();

                result.ConfirmDateTime = requisition.ConfirmDateTime.Value;
                result.NewStatus = requisition.Status;

                _Logger.LogInformation("申请单确认完成 - 申请单ID: {RequisitionId}, 确认人: {OperatorId}, 银行流水号: {BankFlowNumber}",
                    model.RequisitionId, context.User.Id, model.BankFlowNumber);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "执行OA费用申请单确认操作时发生错误 - 申请单ID: {RequisitionId}", model.RequisitionId);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"执行确认操作时发生错误: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region 凭证序号管理辅助方法

        /// <summary>
        /// 获取下一个凭证序号（基于VoucherSequence表，支持乐观锁）。
        /// </summary>
        /// <param name="orgId">组织ID</param>
        /// <param name="month">月份</param>
        /// <param name="voucherCharacter">凭证字</param>
        /// <returns>下一个序号</returns>
        private async Task<int> GetNextVoucherSequenceAsync(Guid orgId, int month, string voucherCharacter)
        {
            const int maxRetries = 3;
            var retryCount = 0;
            VoucherSequence manager = null;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    // 查找现有序号记录
                    manager = await _DbContext.VoucherSequences
                        .FirstOrDefaultAsync(x => x.OrgId == orgId && 
                                                 x.Month == month && 
                                                 x.VoucherCharacter == voucherCharacter);
                    
                    if (manager == null)
                    {
                        // 首次创建记录
                        manager = new VoucherSequence
                        {
                            OrgId = orgId,
                            Month = month,
                            VoucherCharacter = voucherCharacter,
                            MaxSequence = 1,
                            CreateDateTime = DateTime.Now,
                            LastUpdateDateTime = DateTime.Now
                        };
                        _DbContext.VoucherSequences.Add(manager);
                    }
                    else
                    {
                        // 递增序号
                        manager.MaxSequence++;
                        manager.LastUpdateDateTime = DateTime.Now;
                    }
                    
                    // 保存更改（乐观锁生效）
                    await _DbContext.SaveChangesAsync();
                    
                    return manager.MaxSequence;
                }
                catch (DbUpdateConcurrencyException)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw new InvalidOperationException("获取凭证序号失败，并发冲突次数过多");
                    
                    // 重新加载实体以获取最新状态
                    if (manager != null)
                        await _DbContext.Entry(manager).ReloadAsync();
                    
                    // 短暂延迟后重试
                    await Task.Delay(50);
                }
            }
            
            throw new InvalidOperationException("获取凭证序号失败");
        }

        /// <summary>
        /// 检查凭证号在指定组织内是否重复。
        /// </summary>
        /// <param name="voucherNumber">凭证号</param>
        /// <param name="orgId">组织ID</param>
        /// <returns>是否存在重复</returns>
        private bool CheckVoucherNumberDuplicateInOrg(string voucherNumber, Guid orgId)
        {
            try
            {
                return _DbContext.OaExpenseRequisitionItems
                    .Any(item => item.VoucherNumber == voucherNumber && 
                                _DbContext.OaExpenseRequisitions.Any(r => r.Id == item.ParentId && r.OrgId == orgId));
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "检查凭证号重复时发生错误");
                return false; // 出错时返回false，避免阻塞流程
            }
        }

        #endregion
    }
}
