using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using NuGet.Packaging;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Managers.Financial;
using PowerLmsWebApi.Dto;
using System.Linq;
using System.Net;
namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务相关功能控制器。
    /// </summary>
    public partial class FinancialController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。Debit 和credit。
        /// </summary>
        public FinancialController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<FinancialController> logger, IMapper mapper, OwWfManager wfManager, AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _WfManager = wfManager;
            _AuthorizationManager = authorizationManager;
            _SqlAppLogger = sqlAppLogger;
        }
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly ILogger<FinancialController> _Logger;
        readonly IMapper _Mapper;
        readonly OwWfManager _WfManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly OwSqlAppLogger _SqlAppLogger;
        #region 业务费用申请单
        /// <summary>
        /// 获取全部业务费用申请单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。
        /// 支持 PlJob.属性名 格式的键，用于关联到工作号表进行过滤。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeRequisitionReturnDto> GetAllDocFeeRequisition([FromQuery] GetAllDocFeeRequisitionParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeRequisitionReturnDto();
            try
            {
                // 获取DocFeeRequisitionManager服务
                var requisitionManager = _ServiceProvider.GetRequiredService<DocFeeRequisitionManager>();
                IQueryable<DocFeeRequisition> dbSet;
                // 判断是否有子表相关条件，如果没有使用简单查询
                if (conditional == null || !conditional.Any() ||
                    conditional.All(kv => kv.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase) || !kv.Key.Contains('.')))
                {
                    // 使用简单的父表查询
                    dbSet = requisitionManager.GetAllDocFeeRequisitionQuery(context.User.OrgId);
                    // 应用父表条件
                    if (conditional != null && conditional.Any())
                    {
                        var reqConditions = conditional.Where(kv => kv.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase) || !kv.Key.Contains('.'))
                            .ToDictionary(kv => kv.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase) ?
                                kv.Key[(nameof(DocFeeRequisition).Length + 1)..] : kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                        dbSet = EfHelper.GenerateWhereAnd(dbSet, reqConditions);
                    }
                }
                else
                {
                    // 使用核心查询函数进行复杂查询
                    var itemsQuery = requisitionManager.GetAllDocFeeRequisitionItemQuery(conditional, context.User.OrgId);
                    var parentIds = itemsQuery.Select(item => item.ParentId.Value).Distinct();
                    dbSet = _DbContext.DocFeeRequisitions.Where(req => parentIds.Contains(req.Id));
                }
                // 如果需要工作流状态过滤
                if (model.WfState.HasValue)
                {
                    var tmpColl = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, model.WfState.Value)
                        .Select(c => c.Parent.Parent.DocId);
                    dbSet = dbSet.Where(c => tmpColl.Contains(c.Id));
                }
                // 应用排序和分页
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取业务费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取业务费用申请单时发生错误: {ex.Message}";
            }
            return result;
        }
        /// <summary>
        /// 获取当前用户相关的业务费用申请单和审批流状态。
        /// </summary>
        /// <param name="model">分页和排序参数</param>
        /// <param name="conditional">查询条件字典。支持三种格式的条件：
        /// 1. 无前缀的条件：直接作为申请单(DocFeeRequisition)的筛选条件
        /// 2. "OwWf.字段名" 格式的条件：用于筛选关联的工作流(OwWf)对象
        /// 3. "PlJob.字段名" 格式的条件：用于筛选关联的工作号(PlJob)对象
        /// 所有键不区分大小写。其中，OwWf.State会特殊处理，与OwWfManager.GetWfNodeItemByOpertorId方法的state参数映射关系：
        /// 0(流转中)→3, 1(成功完成)→4, 2(已被终止)→8
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns>包含申请单和对应工作流信息的结果集</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeRequisitionWithWfReturnDto> GetAllDocFeeRequisitionWithWf([FromQuery] GetAllDocFeeRequisitionWithWfParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeRequisitionWithWfReturnDto();
            try
            {
                var orgManager = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>();
                bool hasE3Permission = _AuthorizationManager.Demand("E.3");
                // 从条件中分离出不同前缀的条件
                Dictionary<string, string> wfConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> otherConditions = conditional != null
                    ? new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                byte wfState = 15; // 默认值，意味着获取指定操作人相关的所有工作流节点项
                if (otherConditions.Count > 0)
                {
                    List<string> keysToRemove = new List<string>();
                    foreach (var pair in otherConditions)
                    {
                        // 处理工作流条件
                        if (pair.Key.StartsWith("OwWf.", StringComparison.OrdinalIgnoreCase))
                        {
                            string wfFieldName = pair.Key[5..]; // 去掉"OwWf."前缀
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
                    // 从原始条件中移除工作流前缀的条件
                    foreach (var key in keysToRemove)
                    {
                        otherConditions.Remove(key);
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
                // 获取DocFeeRequisitionManager服务
                var requisitionManager = _ServiceProvider.GetRequiredService<DocFeeRequisitionManager>();
                IQueryable<DocFeeRequisition> dbSet;
                // 判断是否有子表相关条件，如果没有使用简单查询
                if (otherConditions == null || !otherConditions.Any() ||
                    otherConditions.All(kv => kv.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase) || !kv.Key.Contains('.')))
                {
                    // 使用简单的父表查询并添加工作流ID限制
                    dbSet = requisitionManager.GetAllDocFeeRequisitionQuery(context.User.OrgId)
                        .Where(c => docIds.Contains(c.Id));
                    // 应用父表条件
                    if (otherConditions != null && otherConditions.Any())
                    {
                        var reqConditions = otherConditions.Where(kv => kv.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase) || !kv.Key.Contains('.'))
                            .ToDictionary(kv => kv.Key.StartsWith($"{nameof(DocFeeRequisition)}.", StringComparison.OrdinalIgnoreCase) ?
                                kv.Key[(nameof(DocFeeRequisition).Length + 1)..] : kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                        dbSet = EfHelper.GenerateWhereAnd(dbSet, reqConditions);
                    }
                }
                else
                {
                    // 使用核心查询函数进行复杂查询并添加工作流ID限制
                    var itemsQuery = requisitionManager.GetAllDocFeeRequisitionItemQuery(otherConditions, context.User.OrgId);
                    var parentIds = itemsQuery.Select(item => item.ParentId.Value).Distinct();
                    dbSet = _DbContext.DocFeeRequisitions.Where(req => parentIds.Contains(req.Id) && docIds.Contains(req.Id));
                }
                if (!hasE3Permission && !context.User.IsSuperAdmin)
                {
                    dbSet = dbSet.Where(r => r.MakerId == context.User.Id);
                    _Logger.LogDebug("用户 {UserId} 无E.3权限，仅显示本人申请单", context.User.Id);
                }
                else if (hasE3Permission && !context.User.IsSuperAdmin)
                {
                    var allowedOrgIds = orgManager.GetOrgIdsByCompanyId(context.User.OrgId.Value);
                    dbSet = dbSet.Where(r => allowedOrgIds.Contains(r.OrgId.Value));
                    _Logger.LogDebug("用户 {UserId} 拥有E.3权限，显示公司所有申请单", context.User.Id);
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
                    result.Result.Add(new GetAllDocFeeRequisitionWithWfItemDto()
                    {
                        Requisition = requisition,
                        Wf = _Mapper.Map<OwWfDto>(wf),
                    });
                }
                result.Total = prb.Total;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取业务费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取业务费用申请单时发生错误: {ex.Message}";
            }
            return result;
        }
        /// <summary>
        /// 增加新业务费用申请单。
        /// </summary>
        /// <param name="model">包含新业务费用申请单信息的参数对象</param>
        /// <returns>操作结果，包含新创建申请单的ID</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeRequisitionReturnDto> AddDocFeeRequisition(AddDocFeeRequisitionParamsDto model)
        {
            // 验证令牌和获取上下文
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("添加业务费用申请单时提供了无效的令牌: {token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocFeeRequisitionReturnDto();
            try
            {
                // 获取要保存的实体并进行基础设置
                var entity = model.DocFeeRequisition;
                entity.GenerateIdIfEmpty(); // 生成新的GUID
                // 设置创建信息
                entity.MakerId = context.User.Id; // 设置创建者ID
                entity.MakeDateTime = OwHelper.WorldNow; // 设置创建时间
                entity.OrgId = context.User.OrgId; // 设置组织ID
                // 添加实体到数据库上下文
                _DbContext.DocFeeRequisitions.Add(entity);
                // 应用审计日志(可选) - 修改为只传递一个参数
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了业务费用申请单ID:{entity.Id}，操作：AddDocFeeRequisition");
                // 保存更改到数据库
                _DbContext.SaveChanges();
                // 设置返回结果
                result.Id = entity.Id;
                _Logger.LogDebug("成功创建业务费用申请单: {id}", entity.Id);
            }
            catch (Exception ex)
            {
                // 记录错误并设置返回错误信息
                _Logger.LogError(ex, "创建业务费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建业务费用申请单时发生错误: {ex.Message}";
            }
            return result;
        }
        /// <summary>
        /// 修改业务费用申请单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyDocFeeRequisitionReturnDto> ModifyDocFeeRequisition(ModifyDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyDocFeeRequisitionReturnDto();
            var originalEntity = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisition.Id);
            if (originalEntity == null)
                return NotFound();
            var originalMakerId = originalEntity.MakerId;
            var originalMakeDateTime = originalEntity.MakeDateTime;
            var modifiedEntities = new List<DocFeeRequisition>();
            if (!_EntityManager.Modify(new[] { model.DocFeeRequisition }, modifiedEntities))
                return NotFound();
            var entry = modifiedEntities[0];
            entry.MakerId = originalMakerId;
            entry.MakeDateTime = originalMakeDateTime;
            _DbContext.SaveChanges();
            return result;
        }
        /// <summary>
        /// 删除指定Id的业务费用申请单。慎用！
        /// </summary>
        /// <param name="model">包含要删除申请单Id的参数</param>
        /// <returns>删除操作的结果</returns>
        /// <response code="200">操作成功，申请单及其明细已被删除。</response>  
        /// <response code="400">无法删除申请单，可能原因：申请单不存在或申请单明细已关联到结算单。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="500">服务器内部错误。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeRequisitionReturnDto> RemoveDocFeeRequisition(RemoveDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeRequisitionReturnDto();
            try
            {
                var id = model.Id;
                _Logger.LogInformation($"开始处理删除业务费用申请单请求，申请单ID：{id}");
                // 查找申请单
                var dbSet = _DbContext.DocFeeRequisitions;
                var item = dbSet.Find(id);
                if (item is null)
                {
                    _Logger.LogWarning($"未找到指定的申请单，ID：{id}");
                    return BadRequest("找不到指定的申请单");
                }
                // 获取所有关联的申请单明细项
                var items = _DbContext.DocFeeRequisitionItems.Where(c => c.ParentId == id).ToList();
                if (items.Count > 0)
                {
                    _Logger.LogInformation($"申请单 {id} 包含 {items.Count} 个明细项，正在检查是否可删除");
                    // 检查明细项是否已结算 - 仅通过直接查询数据库中是否存在关联的结算单明细
                    foreach (var detail in items)
                    {
                        // 直接查询数据库中是否存在关联的结算单明细
                        bool hasInvoiceItems = _DbContext.PlInvoicesItems.Any(invoiceItem =>
                            invoiceItem.RequisitionItemId == detail.Id);
                        if (hasInvoiceItems)
                        {
                            _Logger.LogWarning($"申请单明细(ID:{detail.Id})存在关联的结算单明细，无法删除申请单");
                            return BadRequest($"申请单明细(ID:{detail.Id})已被关联到结算单，无法删除申请单");
                        }
                    }
                }
                // 记录操作日志
                _DbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    OrgId = context.User.OrgId,
                    ActionId = $"Delete.{nameof(DocFeeRequisition)}.{item.Id}",
                    ExtraGuid = context.User.Id,
                    ExtraDecimal = items.Count,
                    WorldDateTime = OwHelper.WorldNow,
                });
                // 如果有关联的明细项，先删除它们
                if (items.Count > 0)
                {
                    _Logger.LogInformation($"删除业务费用申请单 {id} 的 {items.Count} 个明细项");
                    _DbContext.DocFeeRequisitionItems.RemoveRange(items);
                }
                // 删除申请单
                _EntityManager.Remove(item);
                // 保存所有更改
                _DbContext.SaveChanges();
                _Logger.LogInformation($"成功删除业务费用申请单 {id} 及其所有关联明细项");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除业务费用申请单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除业务费用申请单时发生错误: {ex.Message}";
            }
            return result;
        }
        /// <summary>
        /// 获取指定费用的剩余未申请金额。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用至少有一个不存在。</response>  
        [HttpGet]
        public ActionResult<GetFeeRemainingReturnDto> GetFeeRemaining([FromQuery] GetFeeRemainingParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetFeeRemainingReturnDto();
            var fees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id));
            if (fees.Count() != model.FeeIds.Count) return NotFound();
            var coll = from fee in fees
                       join job in _DbContext.PlJobs
                       on fee.JobId equals job.Id
                       //join fqi in _DbContext.DocFeeRequisitionItems
                       //on fee.Id equals fqi.FeeId
                       //join fq in _DbContext.DocFeeRequisitions
                       //on fqi.ParentId equals fq.Id
                       //group fee by fq.Id into g
                       select new { fee, job };
            var ary = coll.AsNoTracking().ToArray();
            var collRem = from fee in fees
                          join fqi in _DbContext.DocFeeRequisitionItems
                          on fee.Id equals fqi.FeeId
                          group fqi by fqi.FeeId into g
                          select new { FeeId = g.Key, Amount = g.Sum(d => d.Amount) };
            var dicRem = collRem.AsNoTracking().ToDictionary(c => c.FeeId, c => c.Amount);  //已申请的金额
            result.Result.AddRange(ary.Select(c =>
            {
                var r = new GetFeeRemainingItemReturnDto
                {
                    Fee = c.fee,
                    Job = c.job,
                    Remaining = c.fee.Amount - dicRem.GetValueOrDefault(c.fee.Id),
                };
                return r;
            }));
            return result;
        }
        #endregion 业务费用申请单
        #region 业务费用申请单明细
        /// <summary>
        /// 获取全部业务费用申请单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典,键格式: 实体名.字段名。
        /// 支持的实体名(不区分大小写)有：DocFeeRequisition,PlJob,DocFee,DocFeeRequisitionItem,DocBill</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeRequisitionItemReturnDto> GetAllDocFeeRequisitionItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeRequisitionItemReturnDto();
            try
            {
                // 获取DocFeeRequisitionManager服务
                var requisitionManager = _ServiceProvider.GetRequiredService<DocFeeRequisitionManager>();
                // 使用唯一基准函数获取已过滤的子表查询
                var coll = requisitionManager.GetAllDocFeeRequisitionItemQuery(conditional, context.User.OrgId);
                // 应用排序和分页
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取业务费用申请单明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取业务费用申请单明细时发生错误: {ex.Message}";
            }
            return result;
        }
        /// <summary>
        /// 获取申请单明细增强接口功能。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典,键格式: 实体名.字段名。
        /// 支持的实体名(不区分大小写)有：DocFeeRequisition,PlJob,DocFee,DocFeeRequisitionItem,DocBill
        /// 省略实体名的条件默认作为DocFeeRequisitionItem的属性条件</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetDocFeeRequisitionItemReturnDto> GetDocFeeRequisitionItem([FromQuery] GetDocFeeRequisitionItemParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            // 查询需要返回：申请单、job、费用实体、申请明细的余额（未结算）
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetDocFeeRequisitionItemReturnDto();
            try
            {
                // 获取DocFeeRequisitionManager服务
                var requisitionManager = _ServiceProvider.GetRequiredService<DocFeeRequisitionManager>();
                // 使用唯一基准函数获取已过滤的子表查询
                var filteredItemsQuery = requisitionManager.GetAllDocFeeRequisitionItemQuery(conditional, context.User.OrgId);
                // 应用排序
                var orderedQuery = filteredItemsQuery.OrderBy(model.OrderFieldName, model.IsDesc);
                // 计算总数
                result.Total = orderedQuery.Count();
                // 应用分页
                var pagedQuery = orderedQuery.Skip(model.StartIndex);
                if (model.Count > 0)
                    pagedQuery = pagedQuery.Take(model.Count);
                // 执行查询获取子表数据
                var items = pagedQuery.AsNoTracking().ToList();
                // 获取关联数据（父表、费用、工作任务、账单）
                var parentIds = items.Select(x => x.ParentId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
                var feeIds = items.Select(x => x.FeeId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
                // 批量加载关联数据
                var requisitions = parentIds.Any() ? _DbContext.DocFeeRequisitions
                    .Where(r => parentIds.Contains(r.Id))
                    .AsNoTracking()
                    .ToList()
                    .ToDictionary(r => r.Id) : new Dictionary<Guid, DocFeeRequisition>();
                var fees = feeIds.Any() ? _DbContext.DocFees
                    .Where(f => feeIds.Contains(f.Id))
                    .AsNoTracking()
                    .ToList()
                    .ToDictionary(f => f.Id) : new Dictionary<Guid, DocFee>();
                var jobIds = fees.Values.Select(f => f.JobId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
                var billIds = fees.Values.Select(f => f.BillId).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
                var jobs = jobIds.Any() ? _DbContext.PlJobs
                    .Where(j => jobIds.Contains(j.Id))
                    .AsNoTracking()
                    .ToList()
                    .ToDictionary(j => j.Id) : new Dictionary<Guid, PlJob>();
                var bills = billIds.Any() ? _DbContext.DocBills
                    .Where(b => billIds.Contains(b.Id))
                    .AsNoTracking()
                    .ToList()
                    .ToDictionary(b => b.Id) : new Dictionary<Guid, DocBill>();
                // 组装结果
                foreach (var item in items)
                {
                    var requisition = item.ParentId.HasValue ? requisitions.GetValueOrDefault(item.ParentId.Value) : null;
                    var fee = item.FeeId.HasValue ? fees.GetValueOrDefault(item.FeeId.Value) : null;
                    var job = fee?.JobId.HasValue == true ? jobs.GetValueOrDefault(fee.JobId.Value) : null;
                    var bill = fee?.BillId.HasValue == true ? bills.GetValueOrDefault(fee.BillId.Value) : null;
                    var resultItem = new GetDocFeeRequisitionItemItem
                    {
                        DocFeeRequisitionItem = item,
                        DocFeeRequisition = requisition,
                        DocFee = fee,
                        PlJob = job,
                        DocBill = bill
                    };
                    // 计算余额：直接使用实体字段，不再动态计算已结算金额
                    resultItem.Remainder = item.Amount - item.TotalSettledAmount;
                    result.Result.Add(resultItem);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取申请单明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取申请单明细时发生错误: {ex.Message}";
            }
            return result;
        }
        /// <summary>
        /// 增加新业务费用申请单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">超额申请或其他业务错误。</response>
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeRequisitionItemReturnDto> AddDocFeeRequisitionItem(AddDocFeeRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocFeeRequisitionItemReturnDto();
            var entity = model.DocFeeRequisitionItem;
            if (!entity.FeeId.HasValue)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "申请单明细必须关联费用（FeeId不能为空）";
                return BadRequest(result);
            }
            if (!DocFee.ValidateRequisitionItemAmount(
                entity.FeeId.Value,
                entity.Amount,
                null,
                _DbContext,
                out string errorMessage))
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = errorMessage;
                _Logger.LogWarning("申请单明细超额：FeeId={FeeId}, Amount={Amount}, 错误={Error}",
                    entity.FeeId.Value, entity.Amount, errorMessage);
                return BadRequest(result);
            }
            entity.GenerateNewId();
            _DbContext.DocFeeRequisitionItems.Add(model.DocFeeRequisitionItem);
            var req = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisitionItem.ParentId);
            var parent = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisitionItem.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
            // 已删除显式回写代码 - 由FeeTotalTriggerHandler触发器自动处理
            _DbContext.SaveChanges();
            result.Id = model.DocFeeRequisitionItem.Id;
            return result;
        }
        /// <summary>
        /// 修改业务费用申请单明细信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">超额申请或其他业务错误。</response>
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单明细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyDocFeeRequisitionItemReturnDto> ModifyDocFeeRequisitionItem(ModifyDocFeeRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeRequisitionItemReturnDto();
            var entity = model.DocFeeRequisitionItem;
            if (!entity.FeeId.HasValue)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "申请单明细必须关联费用（FeeId不能为空）";
                return BadRequest(result);
            }
            if (!DocFee.ValidateRequisitionItemAmount(
                entity.FeeId.Value,
                entity.Amount,
                entity.Id,
                _DbContext,
                out string errorMessage))
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = errorMessage;
                _Logger.LogWarning("修改申请单明细超额：Id={Id}, FeeId={FeeId}, Amount={Amount}, 错误={Error}",
                    entity.Id, entity.FeeId.Value, entity.Amount, errorMessage);
                return BadRequest(result);
            }
            var modifiedEntities = new List<DocFeeRequisitionItem>();
            if (!_EntityManager.Modify(new[] { model.DocFeeRequisitionItem }, modifiedEntities)) return NotFound();
            var entryEntity = _DbContext.Entry(modifiedEntities[0]);
            var parent = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisitionItem.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
            // 不需要显式回写 - 由FeeTotalTriggerHandler触发器自动处理
            _DbContext.SaveChanges();
            return result;
        }
        /// <summary>
        /// 删除指定Id的业务费用申请单明细。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单明细不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeRequisitionItemReturnDto> RemoveDocFeeRequisitionItem(RemoveDocFeeRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeRequisitionItemReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocFeeRequisitionItems;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            // 不需要保存FeeId和显式回写 - 由FeeTotalTriggerHandler触发器自动处理
            _EntityManager.Remove(item);
            //计算合计
            var parent = _DbContext.DocFeeRequisitions.Find(item.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
            // 已删除显式回写代码 - 由FeeTotalTriggerHandler触发器自动处理
            _DbContext.SaveChanges();
            return result;
        }
        /// <summary>
        /// 设置指定的申请单下所有明细。
        /// 指定存在id的明细则更新，Id全0或不存在的Id到自动添加，原有未指定的明细将被删除。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">超额申请或其他业务错误。</response>
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpPut]
        public ActionResult<SetDocFeeRequisitionItemReturnDto> SetDocFeeRequisitionItem(SetDocFeeRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetDocFeeRequisitionItemReturnDto();
            var fr = _DbContext.DocFeeRequisitions.Find(model.FrId);
            if (fr is null) return NotFound();
            var aryIds = model.Items.Select(c => c.Id).ToArray();
            var existsIds = _DbContext.DocFeeRequisitionItems.Where(c => c.ParentId == fr.Id).Select(c => c.Id).ToArray();
            var modifiesIds = aryIds.Intersect(existsIds).ToArray();
            var addIds = aryIds.Except(existsIds).ToArray();
            foreach (var item in model.Items.Where(c => addIds.Contains(c.Id) || modifiesIds.Contains(c.Id)))
            {
                if (!item.FeeId.HasValue)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "申请单明细必须关联费用（FeeId不能为空）";
                    return BadRequest(result);
                }
                var excludeItemId = modifiesIds.Contains(item.Id) ? item.Id : (Guid?)null;
                if (!DocFee.ValidateRequisitionItemAmount(
                    item.FeeId.Value,
                    item.Amount,
                    excludeItemId,
                    _DbContext,
                    out string errorMessage))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = $"明细项(Id={item.Id})超额：{errorMessage}";
                    _Logger.LogWarning("批量设置申请单明细超额：ItemId={Id}, FeeId={FeeId}, Amount={Amount}, 错误={Error}",
                        item.Id, item.FeeId.Value, item.Amount, errorMessage);
                    return BadRequest(result);
                }
            }
            var modifies = model.Items.Where(c => existsIds.Contains(c.Id));
            foreach (var item in modifies)
            {
                _DbContext.Entry(item).CurrentValues.SetValues(item);
                _DbContext.Entry(item).State = EntityState.Modified;
            }
            var adds = model.Items.Where(c => addIds.Contains(c.Id)).ToArray();
            Array.ForEach(adds, c => c.GenerateNewId());
            _DbContext.AddRange(adds);
            var removeIds = existsIds.Except(aryIds).ToArray();
            _DbContext.RemoveRange(_DbContext.DocFeeRequisitionItems.Where(c => removeIds.Contains(c.Id)));
            // 不需要显式回写 - 由FeeTotalTriggerHandler触发器自动处理
            _DbContext.SaveChanges();
            result.Result.AddRange(model.Items);
            return result;
        }
        #endregion 业务费用申请单明细
        #region 回退主营业务费用申请单
        /// <summary>
        /// 回退主营业务费用申请单到初始状态。
        /// 会清空相关工作流、重置申请单状态并释放被锁定的费用。
        /// 根据会议纪要，业务在任何状态下都可能被清空工作流并回退到工作流的初始状态。
        /// </summary>
        /// <param name="model">回退参数</param>
        /// <returns>回退操作结果</returns>
        /// <response code="200">回退成功。</response>
        /// <response code="400">回退失败，申请单不存在或其他业务错误。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。使用E.2（审批撤销）权限，专门用于一键在任何情况下撤销的接口。</response>
        /// <response code="404">指定ID的申请单不存在。</response>
        /// <response code="500">回退过程中发生系统错误。</response>
        [HttpPost]
        public ActionResult<RevertDocFeeRequisitionReturnDto> RevertDocFeeRequisition(RevertDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new RevertDocFeeRequisitionReturnDto();
            try
            {
                // 1. 权限验证（使用E.2审批撤销权限，专门用于一键在任何情况下撤销的接口）
                if (!_AuthorizationManager.Demand(out string err, "E.2"))  // 使用E.2审批撤销权限
                {
                    _Logger.LogWarning("权限不足，用户{UserId}尝试回退主营业务费用申请单{RequisitionId}", context.User.Id, model.RequisitionId);
                    return StatusCode((int)HttpStatusCode.Forbidden, "权限不足：需要审批撤销权限（E.2）");
                }
                // 2. 获取DocFeeRequisitionManager
                var requisitionManager = _ServiceProvider.GetRequiredService<DocFeeRequisitionManager>();
                // 3. 记录回退原因到审计日志
                if (!string.IsNullOrWhiteSpace(model.Reason))
                {
                    _SqlAppLogger.LogGeneralInfo($"主营业务费用申请单回退原因：RequisitionId={model.RequisitionId}, 操作人={context.User.Id}, 原因={model.Reason}");
                }
                // 4. 调用DocFeeRequisitionManager的回退服务方法
                var revertResult = requisitionManager.RevertRequisition(
                    model.RequisitionId,
                    context.User.Id,
                    _WfManager);
                // 5. 根据服务返回结果构造API响应
                if (revertResult.Success)
                {
                    result.RequisitionId = revertResult.RequisitionId;
                    result.ClearedWorkflowCount = revertResult.ClearedWorkflowCount;
                    result.Message = revertResult.Message;
                    _Logger.LogInformation("主营业务费用申请单回退成功：RequisitionId={RequisitionId}, 操作人={UserId}, 清空工作流{WorkflowCount}个",
                        model.RequisitionId, context.User.Id, revertResult.ClearedWorkflowCount);
                    return result;
                }
                else
                {
                    // 回退失败，根据错误信息确定HTTP状态码
                    _Logger.LogWarning("主营业务费用申请单回退失败：RequisitionId={RequisitionId}, 操作人={UserId}, 错误={Error}",
                        model.RequisitionId, context.User.Id, revertResult.Message);
                    if (revertResult.Message.Contains("未找到"))
                    {
                        return NotFound(revertResult.Message);
                    }
                    else
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = revertResult.Message;
                        return BadRequest(result);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                _Logger.LogError(ex, "回退主营业务费用申请单参数错误：RequisitionId={RequisitionId}, 操作人={UserId}", model.RequisitionId, context.User.Id);
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = $"参数错误：{ex.Message}";
                return BadRequest(result);
            }
            catch (InvalidOperationException ex)
            {
                _Logger.LogError(ex, "回退主营业务费用申请单操作错误：RequisitionId={RequisitionId}, 操作人={UserId}", model.RequisitionId, context.User.Id);
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = $"操作错误：{ex.Message}";
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "回退主营业务费用申请单时发生系统错误：RequisitionId={RequisitionId}, 操作人={UserId}", model.RequisitionId, context.User.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"系统错误：{ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        #endregion 回退主营业务费用申请单
        #region 费用方案
        /// <summary>
        /// 获取全部费用方案。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeTemplateReturnDto> GetAllDocFeeTemplate([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeTemplateReturnDto();
            var dbSet = _DbContext.DocFeeTemplates;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }
        /// <summary>
        /// 增加新费用方案。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeTemplateReturnDto> AddDocFeeTemplate(AddDocFeeTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌: {token}", model.Token);
                return Unauthorized();
            }
            #region 权限判定
            var docFeeTT = model.DocFeeTemplate;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            var result = new AddDocFeeTemplateReturnDto();
            var entity = model.DocFeeTemplate;
            entity.GenerateNewId();
            model.DocFeeTemplate.CreateBy = context.User.Id;
            model.DocFeeTemplate.CreateDateTime = OwHelper.WorldNow;
            _DbContext.DocFeeTemplates.Add(model.DocFeeTemplate);
            _DbContext.SaveChanges();
            result.Id = model.DocFeeTemplate.Id;
            return result;
        }
        /// <summary>
        /// 修改费用方案信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的费用方案不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyDocFeeTemplateReturnDto> ModifyDocFeeTemplate(ModifyDocFeeTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeTemplateReturnDto();
            #region 权限判定
            var docFeeTT = model.DocFeeTemplate;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            var modifiedEntities = new List<DocFeeTemplate>();
            if (!_EntityManager.Modify(new[] { model.DocFeeTemplate }, modifiedEntities)) return NotFound();
            var entity = _DbContext.Entry(modifiedEntities[0]);
            _DbContext.SaveChanges();
            return result;
        }
        /// <summary>
        /// 删除指定Id的费用方案。这会删除所有费用方案明细项。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的费用方案不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeTemplateReturnDto> RemoveDocFeeTemplate(RemoveDocFeeTemplateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeTemplateReturnDto();
            var id = model.Id;
            if (_DbContext.DocFeeTemplates.Find(id) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            var children = _DbContext.DocFeeTemplateItems.Where(c => c.ParentId == item.Id).ToArray();
            _EntityManager.Remove(item);
            if (children.Length > 0) _DbContext.RemoveRange(children);
            _DbContext.OwSystemLogs.Add(new OwSystemLog
            {
                OrgId = context.User.OrgId,
                ActionId = $"Delete.{nameof(DocFeeTemplate)}.{item.Id}",
                ExtraGuid = context.User.Id,
                ExtraDecimal = children.Length,
            });
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 费用方案
        #region 费用方案明细
        /// <summary>
        /// 获取全部费用方案明细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的费用方案不存在。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeTemplateItemReturnDto> GetAllDocFeeTemplateItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeTemplateItemReturnDto();
            var dbSet = _DbContext.DocFeeTemplateItems;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }
        /// <summary>
        /// 增加新费用方案明细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeTemplateItemReturnDto> AddDocFeeTemplateItem(AddDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌: {token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocFeeTemplateItemReturnDto();
            var entity = model.DocFeeTemplateItem;
            var id = model.DocFeeTemplateItem.ParentId;
            if (id is null) return BadRequest();
            if (_DbContext.DocFeeTemplates.Find(id.Value) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            entity.GenerateNewId();
            _DbContext.DocFeeTemplateItems.Add(model.DocFeeTemplateItem);
            _DbContext.SaveChanges();
            result.Id = model.DocFeeTemplateItem.Id;
            return result;
        }
        /// <summary>
        /// 修改费用方案明细信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的费用方案明细不存在。</response>
        [HttpPut]
        public ActionResult<ModifyDocFeeTemplateItemReturnDto> ModifyDocFeeTemplateItem(ModifyDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeTemplateItemReturnDto();
            var id = model.DocFeeTemplateItem.ParentId;
            if (id is null) return BadRequest();
            if (_DbContext.DocFeeTemplates.Find(id.Value) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            var modifiedEntities = new List<DocFeeTemplateItem>();
            if (!_EntityManager.Modify(new[] { model.DocFeeTemplateItem }, modifiedEntities)) return NotFound();
            var entity = _DbContext.Entry(modifiedEntities[0]);
            _DbContext.SaveChanges();
            return result;
        }
        /// <summary>
        /// 删除指定Id的费用方案明细。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的费用方案明细不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeTemplateItemReturnDto> RemoveDocFeeTemplateItem(RemoveDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeTemplateItemReturnDto();
            var id = model.Id;
            if (_DbContext.DocFeeTemplateItems.Find(id) is not DocFeeTemplateItem item) return BadRequest();
            var idTT = item.ParentId;
            if (idTT is null) return BadRequest();
            if (_DbContext.DocFeeTemplates.Find(idTT.Value) is not DocFeeTemplate tt) return BadRequest();
            #region 权限判定
            var docFeeTT = tt;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }
        /// <summary>
        /// 设置指定的费用方案下所有明细。
        /// 指定存在id的明细则更新，Id全0或不存在的Id到自动添加，原有未指定的明细将被删除。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpPut]
        public ActionResult<SetDocFeeTemplateItemReturnDto> SetDocFeeTemplateItem(SetDocFeeTemplateItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetDocFeeTemplateItemReturnDto();
            if (_DbContext.DocFeeRequisitions.Find(model.FrId) is not DocFeeRequisition fr) return NotFound();
            var idTT = model.FrId;
            if (_DbContext.DocFeeTemplates.Find(idTT) is not DocFeeTemplate tt) return BadRequest();
            #region 权限判定
            var docFeeTT = tt;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out string err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            var aryIds = model.Items.Select(c => c.Id).ToArray();   //指定的Id
            var existsIds = _DbContext.DocFeeTemplateItems.Where(c => c.ParentId == fr.Id).Select(c => c.Id).ToArray();    //已经存在的Id
            //更改
            var modifies = model.Items.Where(c => existsIds.Contains(c.Id));
            foreach (var item in modifies)
            {
                _DbContext.Entry(item).CurrentValues.SetValues(item);
                _DbContext.Entry(item).State = EntityState.Modified;
            }
            //增加
            var addIds = aryIds.Except(existsIds).ToArray();
            var adds = model.Items.Where(c => addIds.Contains(c.Id)).ToArray();
            Array.ForEach(adds, c => c.GenerateNewId());
            _DbContext.AddRange(adds);
            //删除
            var removeIds = existsIds.Except(aryIds).ToArray();
            _DbContext.RemoveRange(_DbContext.DocFeeTemplateItems.Where(c => removeIds.Contains(c.Id)));
            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }
        #endregion 费用方案明细
    }
}
