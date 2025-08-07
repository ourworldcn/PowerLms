﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using PowerLms.Data.OA;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PowerLmsWebApi.Controllers.OA
{
    /// <summary>
    /// OA费用申请单控制器。
    /// 处理OA日常费用申请单的增删改查操作。
    /// </summary>
    public partial class OaExpenseController : PlControllerBase
    {
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly IServiceProvider _ServiceProvider;
        private readonly AccountManager _AccountManager;
        private readonly ILogger<OaExpenseController> _Logger;
        private readonly EntityManager _EntityManager;
        private readonly OwWfManager _WfManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OaExpenseController(PowerLmsUserDbContext dbContext, 
            IServiceProvider serviceProvider, 
            AccountManager accountManager,
            ILogger<OaExpenseController> logger,
            EntityManager entityManager,
            OwWfManager wfManager,
            IMapper mapper,
            AuthorizationManager authorizationManager)
        {
            _DbContext = dbContext;
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _Logger = logger;
            _EntityManager = entityManager;
            _WfManager = wfManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
        }

        #region OA费用申请单主表操作

        /// <summary>
        /// 获取所有OA费用申请单。
        /// </summary>
        /// <param name="model">分页和查询参数</param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns>OA费用申请单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpGet]
        public ActionResult<GetAllOaExpenseRequisitionReturnDto> GetAllOaExpenseRequisition([FromQuery] GetAllOaExpenseRequisitionParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetAllOaExpenseRequisitionReturnDto();

            try
            {
                var dbSet = _DbContext.OaExpenseRequisitions.Where(c => c.OrgId == context.User.OrgId);

                // 非超管用户权限过滤
                if (!context.User.IsSuperAdmin)
                {
                    // 只能看自己创建/登记的申请单（CreateBy记录创建人/登记人/申请人）
                    dbSet = dbSet.Where(r => r.CreateBy == context.User.Id);
                }

                // 确保条件字典不区分大小写
                var normalizedConditional = conditional != null ?
                    new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                    null;

                // 应用通用条件查询
                var coll = EfHelper.GenerateWhereAnd(dbSet, normalizedConditional);

                // 移除了专用的搜索文本处理，改为完全依赖 conditional 参数
                // 文本搜索示例：
                // - conditional["RelatedCustomer"] = "*客户名称*"  
                // - conditional["Remark"] = "*备注关键词*"
                // - 复合条件可组合使用，更加灵活和强大

                // 排序应用在修改的查询
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                // 使用EntityManager进行分页
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                result.Total = prb.Total;
                result.Result.AddRange(prb.Result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取OA费用申请单列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取OA费用申请单列表时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 创建新的OA费用申请单。
        /// </summary>
        /// <param name="model">申请单信息</param>
        /// <returns>创建结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddOaExpenseRequisitionReturnDto> AddOaExpenseRequisition(AddOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AddOaExpenseRequisitionReturnDto();

            try
            {
                var entity = model.Item; // 更新为使用 Item 属性
                entity.GenerateNewId(); // 强制生成Id
                entity.OrgId = context.User.OrgId;
                entity.CreateBy = context.User.Id; // CreateBy就是登记人
                entity.CreateDateTime = OwHelper.WorldNow;

                // 注意：ApplicantId字段已废弃，统一使用CreateBy记录创建人/登记人/申请人
                // 处理申请模式：当前所有人员角色都通过CreateBy记录
                // 无论是代人登记还是自己申请，CreateBy都记录当前登录用户
                // 不再使用ApplicantId字段
                
                // 初始化审核字段
                entity.AuditDateTime = null;
                entity.AuditOperatorId = null;

                _DbContext.OaExpenseRequisitions.Add(entity);
                _DbContext.SaveChanges();

                result.Id = entity.Id;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建OA费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 修改OA费用申请单信息。
        /// </summary>
        /// <param name="model">申请单信息</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [HttpPut]
        public ActionResult<ModifyOaExpenseRequisitionReturnDto> ModifyOaExpenseRequisition(ModifyOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ModifyOaExpenseRequisitionReturnDto();

            try
            {
                // 检查所有实体是否存在和权限
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.OaExpenseRequisitions.Find(item.Id);
                    if (existing == null)
                    {
                        result.HasError = true;
                        result.ErrorCode = 404;
                        result.DebugMessage = $"指定的OA费用申请单 {item.Id} 不存在";
                        return result;
                    }

                    // 🔧 修正：OA日常费用申请单主单锁定规则
                    // 一旦不是草稿状态，整个主单都不能修改
                    if (!existing.CanEdit(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = GetEditRestrictionMessage(existing.Status);
                        return result;
                    }

                    // 检查用户权限：只能修改自己创建/登记的申请单（废弃ApplicantId，统一使用CreateBy）
                    if (existing.CreateBy.HasValue && existing.CreateBy.Value != context.User.Id && !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "权限不足，无法修改此申请单";
                        return result;
                    }
                }

                // 使用EntityManager进行修改
                if (!_EntityManager.Modify(model.Items))
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "修改失败，请检查数据";
                    return result;
                }

                // 确保核心字段不被修改
                foreach (var item in model.Items)
                {
                    var entry = _DbContext.Entry(item);
                    
                    // 始终保护的系统字段
                    entry.Property(e => e.OrgId).IsModified = false; // 机构Id创建时确定，不可修改
                    entry.Property(e => e.CreateBy).IsModified = false;
                    entry.Property(e => e.CreateDateTime).IsModified = false;
                    entry.Property(e => e.AuditDateTime).IsModified = false;
                    entry.Property(e => e.AuditOperatorId).IsModified = false;

                    // 状态驱动的字段保护：确认后只允许系统字段更新
                    var existing = entry.Entity as OaExpenseRequisition;
                    if (existing.IsCompletelyLocked())
                    {
                        // 确认后所有业务字段都不可修改，只允许系统字段更新
                        var allowedProperties = new[] { 
                            nameof(OaExpenseRequisition.Status),
                            nameof(OaExpenseRequisition.SettlementOperatorId),
                            nameof(OaExpenseRequisition.SettlementDateTime),
                            nameof(OaExpenseRequisition.SettlementMethod),
                            nameof(OaExpenseRequisition.SettlementRemark),
                            nameof(OaExpenseRequisition.ConfirmOperatorId),
                            nameof(OaExpenseRequisition.ConfirmDateTime),
                            nameof(OaExpenseRequisition.BankFlowNumber),
                            nameof(OaExpenseRequisition.ConfirmRemark)
                        };
                        
                        foreach (var property in entry.Properties)
                        {
                            if (!allowedProperties.Contains(property.Metadata.Name))
                            {
                                property.IsModified = false;
                            }
                        }
                    }
                }

                // 保存更改到数据库
                _DbContext.SaveChanges();

                _Logger.LogInformation("成功修改OA日常费用申请单主单，用户: {UserId}, 申请单数量: {Count}", 
                    context.User.Id, model.Items.Count());
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改OA日常费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改OA日常费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 删除OA费用申请单。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveOaExpenseRequisitionReturnDto> RemoveOaExpenseRequisition(RemoveOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new RemoveOaExpenseRequisitionReturnDto();

            try
            {
                var entities = _DbContext.OaExpenseRequisitions.Where(e => model.Ids.Contains(e.Id)).ToList();

                foreach (var entity in entities)
                {
                    // 检查权限和状态
                    if (!entity.CanEdit(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = $"申请单已审核，无法删除";
                        return result;
                    }

                    // 检查用户权限：只能删除自己创建/登记的申请单（废弃ApplicantId，统一使用CreateBy）
                    if (entity.CreateBy.HasValue && entity.CreateBy.Value != context.User.Id && !context.User.IsSuperAdmin)
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = $"权限不足，无法删除申请单";
                        return result;
                    }

                    // 删除关联的明细记录
                    var items = _DbContext.OaExpenseRequisitionItems.Where(i => i.ParentId == entity.Id);
                    _DbContext.OaExpenseRequisitionItems.RemoveRange(items);

                    // 使用EntityManager进行删除（支持级联删除）
                    _EntityManager.Remove(entity);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除OA费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 审核或取消审核OA费用申请单。
        /// 已废弃：请使用新的结算和确认流程
        /// </summary>
        /// <param name="model">审核参数</param>
        /// <returns>审核结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
        [Obsolete("已废弃原有审核接口，请使用SettleOaExpenseRequisition和ConfirmOaExpenseRequisition实现两步式处理")]
        [HttpPost]
        public ActionResult<AuditOaExpenseRequisitionReturnDto> AuditOaExpenseRequisition(AuditOaExpenseRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new AuditOaExpenseRequisitionReturnDto();

            try
            {
                var existing = _DbContext.OaExpenseRequisitions.Find(model.RequisitionId);
                if (existing == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "指定的OA费用申请单不存在";
                    return result;
                }

                // TODO: 这里需要添加更复杂的审核权限检查
                // 暂时只允许超管和申请单所属组织的用户审核
                if (!context.User.IsSuperAdmin && existing.OrgId != context.User.OrgId)
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "权限不足，无法审核此申请单";
                    return result;
                }

                if (model.IsAudit)
                {
                    // 审核通过前进行金额一致性校验
                    if (!existing.ValidateAmountConsistency(_DbContext))
                    {
                        var itemsSum = existing.GetItemsAmountSum(_DbContext);
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"明细金额合计({itemsSum:F2})与主单金额({existing.Amount:F2})不一致，请检查明细项后再提交审核";
                        _Logger.LogWarning("申请单{RequisitionId}金额校验失败: 主单金额={MainAmount:F2}, 明细合计={ItemsSum:F2}", 
                            model.RequisitionId, existing.Amount, itemsSum);
                        return result;
                    }

                    // 检查是否有明细项（如果需要的话）
                    var itemsCount = existing.GetItems(_DbContext).Count();
                    if (itemsCount == 0)
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = "申请单必须包含至少一个明细项才能审核";
                        _Logger.LogWarning("申请单{RequisitionId}审核失败: 没有明细项", model.RequisitionId);
                        return result;
                    }

                    // 审核通过
                    existing.AuditDateTime = OwHelper.WorldNow;
                    existing.AuditOperatorId = context.User.Id;
                    
                    _Logger.LogInformation("申请单审核通过，审核人: {UserId}, 申请单ID: {RequisitionId}, 主单金额: {Amount:F2}, 明细项数: {ItemsCount}", 
                        context.User.Id, model.RequisitionId, existing.Amount, itemsCount);
                }
                else
                {
                    // 取消审核
                    existing.AuditDateTime = null;
                    existing.AuditOperatorId = null;
                    _Logger.LogInformation("申请单取消审核，操作人: {UserId}, 申请单ID: {RequisitionId}", 
                        context.User.Id, model.RequisitionId);
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "审核OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"审核OA费用申请单时发生错误: {ex.Message}";
            }

            return result;
        }

        #region 私有辅助方法

        /// <summary>
        /// 检查工作流是否已完成。优先使用OwWfManager，数据库查询作为兜底方案。
        /// </summary>
        /// <param name="requisitionId">申请单Id</param>
        /// <returns>工作流已完成返回true，否则返回false</returns>
        private bool IsWorkflowCompleted(Guid requisitionId)
        {
            try
            {
                // 第一优先级：使用OwWfManager检查工作流状态
                var wfItems = _WfManager.GetWfNodeItemByOpertorId(Guid.Empty, 4); // 4=成功完成的流程
                var completedWf = wfItems.FirstOrDefault(item => 
                    item.Parent.Parent.DocId == requisitionId);

                if (completedWf != null)
                {
                    return true; // OwWfManager确认工作流已完成
                }

                // 第二优先级：直接查询数据库（兜底方案）
                var workflow = _DbContext.OwWfs
                    .Where(w => w.DocId == requisitionId)
                    .FirstOrDefault();

                return workflow?.State == 4; // 检查工作流是否成功完成
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "检查工作流状态时发生错误，申请单ID: {RequisitionId}，使用数据库查询作为兜底", requisitionId);

                // 异常情况下使用数据库查询
                try
                {
                    var workflow = _DbContext.OwWfs
                        .Where(w => w.DocId == requisitionId)
                        .FirstOrDefault();
                    return workflow?.State == 4;
                }
                catch (Exception dbEx)
                {
                    _Logger.LogError(dbEx, "数据库查询工作流状态也失败，申请单ID: {RequisitionId}", requisitionId);
                    return false; // 无法确定状态时保守返回false
                }
            }
        }

        /// <summary>
        /// 获取编辑限制的友好提示消息。
        /// </summary>
        /// <param name="status">申请单状态</param>
        /// <returns>状态对应的限制消息</returns>
        private static string GetEditRestrictionMessage(OaExpenseStatus status)
        {
            return status switch
            {
                OaExpenseStatus.InApproval => "申请单正在审批中，不能修改",
                OaExpenseStatus.ApprovedPendingSettlement => "申请单已审批完成，不能修改",
                OaExpenseStatus.SettledPendingConfirm => "申请单已结算，不能修改",
                OaExpenseStatus.ConfirmedReadyForExport => "申请单已确认，不能修改",
                OaExpenseStatus.ExportedToFinance => "申请单已导入财务，不能修改",
                _ => "申请单状态不允许修改"
            };
        }

        /// <summary>
        /// 检查修改是否包含主要字段变更。
        /// </summary>
        /// <param name="newItem">新的申请单数据</param>
        /// <param name="existing">现有申请单数据</param>
        /// <returns>包含主要字段变更返回true，否则返回false</returns>
        private static bool ItemContainsMainFieldChanges(OaExpenseRequisition newItem, OaExpenseRequisition existing)
        {
            return newItem.Amount != existing.Amount ||
                   newItem.ExchangeRate != existing.ExchangeRate ||
                   newItem.CurrencyCode != existing.CurrencyCode;
        }

        #endregion

        #endregion
    }
}