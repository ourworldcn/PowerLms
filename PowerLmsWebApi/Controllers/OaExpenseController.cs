using Microsoft.AspNetCore.Mvc;
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

namespace PowerLmsWebApi.Controllers
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

        /// <summary>
        /// 构造函数。
        /// </summary>
        public OaExpenseController(PowerLmsUserDbContext dbContext, 
            IServiceProvider serviceProvider, 
            AccountManager accountManager,
            ILogger<OaExpenseController> logger,
            EntityManager entityManager,
            OwWfManager wfManager,
            IMapper mapper)
        {
            _DbContext = dbContext;
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _Logger = logger;
            _EntityManager = entityManager;
            _WfManager = wfManager;
            _Mapper = mapper;
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
                    // 非超管只能看自己申请的或自己登记的
                    dbSet = dbSet.Where(r => r.ApplicantId == context.User.Id || r.CreateBy == context.User.Id);
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

                // 处理申请模式：代人登记
                if (model.IsRegisterForOthers)
                {
                    // 代为登记模式：登记人为当前用户（CreateBy），申请人由用户选择
                    // 申请人Id应由前端传入
                }
                else
                {
                    // 自己申请模式：申请人和登记人都是当前用户
                    entity.ApplicantId = context.User.Id;
                }

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

                    // 检查权限和状态
                    if (!existing.CanEdit(_DbContext))
                    {
                        result.HasError = true;
                        result.ErrorCode = 403;
                        result.DebugMessage = "申请单已审核，无法修改";
                        return result;
                    }

                    // 检查用户权限：只能修改自己申请的申请单或自己登记的申请单
                    if ((existing.ApplicantId.HasValue && existing.ApplicantId.Value != context.User.Id) && 
                        (existing.CreateBy.HasValue && existing.CreateBy.Value != context.User.Id) && 
                        !context.User.IsSuperAdmin)
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
                    entry.Property(e => e.OrgId).IsModified = false; // 机构Id创建时确定，不可修改
                    entry.Property(e => e.CreateBy).IsModified = false;
                    entry.Property(e => e.CreateDateTime).IsModified = false;
                    entry.Property(e => e.AuditDateTime).IsModified = false;
                    entry.Property(e => e.AuditOperatorId).IsModified = false;
                }

                _DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改OA费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改OA费用申请单时发生错误: {ex.Message}";
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

                    // 检查用户权限
                    if ((entity.ApplicantId.HasValue && entity.ApplicantId.Value != context.User.Id) && 
                        (entity.CreateBy.HasValue && entity.CreateBy.Value != context.User.Id) && 
                        !context.User.IsSuperAdmin)
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
        /// </summary>
        /// <param name="model">审核参数</param>
        /// <returns>审核结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的申请单不存在。</response>
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

        /// <summary>
        /// 获取当前用户相关的OA费用申请单和审批流状态。
        /// 跑完标准审批流程后可审核。
        /// </summary>
        /// <param name="model">分页和排序参数</param>
        /// <param name="conditional">查询的条件。支持三种格式的条件：
        /// 1. 无前缀的条件：直接作为申请单(OaExpenseRequisition)的筛选条件
        /// 2. "OwWf.字段名" 格式的条件：用于筛选关联的工作流(OwWf)对象
        /// 所有键不区分大小写。其中，OwWf.State会特殊处理，与OwWfManager.GetWfNodeItemByOpertorId方法的state参数映射关系：
        /// 0(流转中)→3, 1(成功完成)→4, 2(已被终止)→8
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns>包含申请单和对应工作流信息的结果集</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllOaExpenseRequisitionWithWfReturnDto> GetAllOaExpenseRequisitionWithWf([FromQuery] GetAllOaExpenseRequisitionWithWfParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            var result = new GetAllOaExpenseRequisitionWithWfReturnDto();

            try
            {
                // 从条件中分离出不同前缀的条件
                Dictionary<string, string> wfConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> reqConditions = conditional != null
                    ? new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                byte wfState = 15; // 默认值，意味着获取指定操作人相关的所有工作流节点项

                if (reqConditions.Count > 0)
                {
                    List<string> keysToRemove = new List<string>();

                    foreach (var pair in reqConditions)
                    {
                        // 处理工作流条件
                        if (pair.Key.StartsWith("OwWf.", StringComparison.OrdinalIgnoreCase))
                        {
                            string wfFieldName = pair.Key.Substring(5); // 去掉"OwWf."前缀

                            // 处理 State 的特殊情况
                            if (string.Equals(wfFieldName, "State", StringComparison.OrdinalIgnoreCase))
                            {
                                if (byte.TryParse(pair.Value, out var state))
                                {
                                    switch (state)
                                    {
                                        case 0: // 流转中 - 等价于旧的"3"（1|2）
                                            wfState = 3; // 使用OwWfManager中的值3：流转中的节点项
                                            break;
                                        case 1: // 成功完成 - 等价于旧的"4"
                                            wfState = 4; // 使用OwWfManager中的值4：成功结束的流程
                                            break;
                                        case 2: // 已被终止 - 等价于旧的"8"
                                            wfState = 8; // 使用OwWfManager中的值8：已失败结束的流程
                                            break;
                                        default:
                                            wfState = 15; // 使用默认值15：不限定状态
                                            break;
                                    }
                                }
                            }
                            else // 其他工作流条件
                            {
                                wfConditions[wfFieldName] = pair.Value;
                            }
                            keysToRemove.Add(pair.Key);
                        }
                    }

                    // 从原始条件中移除特殊前缀的条件
                    foreach (var key in keysToRemove)
                    {
                        reqConditions.Remove(key);
                    }
                }

                // 查询关联的工作流
                var docIdsQuery = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, wfState)
                    .Select(c => c.Parent.Parent);

                // 如果有其他工作流条件，先应用它们
                if (wfConditions.Count > 0)
                {
                    _Logger.LogDebug("应用工作流过滤条件: {conditions}",
                        string.Join(", ", wfConditions.Select(kv => $"{kv.Key}={kv.Value}")));

                    // 应用工作流筛选条件
                    docIdsQuery = EfHelper.GenerateWhereAnd(docIdsQuery, wfConditions);
                }

                // 获取符合条件的文档ID
                var docIds = docIdsQuery.Select(wf => wf.DocId.Value).Distinct();

                // 构建申请单查询
                var dbSet = _DbContext.OaExpenseRequisitions.Where(r => docIds.Contains(r.Id));

                // 🔥 修复Bug：工作流查询已经包含权限控制，只需保留组织隔离
                // 移除 (r.ApplicantId == context.User.Id || r.CreateBy == context.User.Id) 条件
                // 这样审批人就能看到分配给自己审批的申请单了
                if (!context.User.IsSuperAdmin)
                {
                    dbSet = dbSet.Where(r => r.OrgId == context.User.OrgId);
                }

                // 应用申请单条件
                if (reqConditions.Count > 0)
                {
                    dbSet = EfHelper.GenerateWhereAnd(dbSet, reqConditions);
                }

                // 应用分页和排序
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);

                // 获取结果ID集合
                var resultIds = prb.Result.Select(c => c.Id).ToList();

                // 只查询结果相关的工作流
                var wfsArray = _DbContext.OwWfs
                    .Where(c => resultIds.Contains(c.DocId.Value))
                    .ToArray();

                // 组装结果
                foreach (var requisition in prb.Result)
                {
                    var wf = wfsArray.FirstOrDefault(d => d.DocId == requisition.Id);
                    result.Result.Add(new GetAllOaExpenseRequisitionWithWfItemDto()
                    {
                        Requisition = requisition,
                        Wf = _Mapper.Map<OwWfDto>(wf),
                    });
                }

                result.Total = prb.Total;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取OA费用申请单审批流程列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取OA费用申请单审批流程列表时发生错误: {ex.Message}";
            }

            return result;
        }

        #endregion
    }
}