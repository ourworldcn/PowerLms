using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using NuGet.Packaging;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 财务相关功能控制器。
    /// </summary>
    public class FinancialController : PlControllerBase
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

        private AccountManager _AccountManager;
        private IServiceProvider _ServiceProvider;
        private EntityManager _EntityManager;
        private PowerLmsUserDbContext _DbContext;
        readonly ILogger<FinancialController> _Logger;
        readonly IMapper _Mapper;
        readonly OwWfManager _WfManager;
        readonly AuthorizationManager _AuthorizationManager;
        OwSqlAppLogger _SqlAppLogger;

        #region 业务费用申请单

        /// <summary>
        /// 获取全部业务费用申请单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
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
                // 从条件中分离出PlJob开头的条件
                Dictionary<string, string> plJobConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> reqConditions = conditional != null
                    ? new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (reqConditions.Count > 0)
                {
                    List<string> keysToRemove = new List<string>();

                    foreach (var pair in reqConditions)
                    {
                        if (pair.Key.StartsWith("PlJob.", StringComparison.OrdinalIgnoreCase))
                        {
                            string jobFieldName = pair.Key.Substring(6); // 去掉"PlJob."前缀
                            plJobConditions[jobFieldName] = pair.Value;
                            keysToRemove.Add(pair.Key);
                        }
                    }

                    // 从原始条件中移除PlJob开头的条件
                    foreach (var key in keysToRemove)
                    {
                        reqConditions.Remove(key);
                    }
                }

                IQueryable<DocFeeRequisition> dbSet;

                // 如果有PlJob相关的条件，则需要联合查询
                if (plJobConditions.Count > 0)
                {
                    _Logger.LogDebug("应用工作号过滤条件: {conditions}",
                        string.Join(", ", plJobConditions.Select(kv => $"{kv.Key}={kv.Value}")));

                    // 先获取符合PlJob条件的工作号
                    var jobIds = EfHelper.GenerateWhereAnd(_DbContext.PlJobs.AsNoTracking(), plJobConditions)
                        .Select(job => job.Id);

                    // 按ID链接查询相关申请单
                    dbSet = (from requisition in _DbContext.DocFeeRequisitions
                             join item in _DbContext.DocFeeRequisitionItems on requisition.Id equals item.ParentId
                             join fee in _DbContext.DocFees on item.FeeId equals fee.Id
                             where requisition.OrgId == context.User.OrgId
                                   && fee.JobId.HasValue
                                   && jobIds.Contains(fee.JobId.Value)
                             select requisition).Distinct();

                    if (model.WfState.HasValue)  //须限定审批流程状态
                    {
                        var tmpColl = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, model.WfState.Value)
                            .Select(c => c.Parent.Parent.DocId);
                        dbSet = dbSet.Where(c => tmpColl.Contains(c.Id));
                    }
                }
                else
                {
                    // 原始查询逻辑
                    dbSet = _DbContext.DocFeeRequisitions.Where(c => c.OrgId == context.User.OrgId);

                    if (model.WfState.HasValue)  //须限定审批流程状态
                    {
                        var tmpColl = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, model.WfState.Value)
                            .Select(c => c.Parent.Parent.DocId);
                        dbSet = dbSet.Where(c => tmpColl.Contains(c.Id));
                    }
                }

                // 应用申请单条件
                var coll = EfHelper.GenerateWhereAnd(dbSet, reqConditions);

                // 应用排序和分页
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
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
        /// <param name="conditional">查询的条件。支持三种格式的条件：
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
                // 从条件中分离出不同前缀的条件
                Dictionary<string, string> wfConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> plJobConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
                        // 处理PlJob条件
                        else if (pair.Key.StartsWith("PlJob.", StringComparison.OrdinalIgnoreCase))
                        {
                            string jobFieldName = pair.Key.Substring(6); // 去掉"PlJob."前缀
                            plJobConditions[jobFieldName] = pair.Value;
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

                // 构建申请单查询的初始部分
                var dbSet = _DbContext.DocFeeRequisitions
                    .Where(c => c.OrgId == context.User.OrgId && docIds.Contains(c.Id));

                // 如果有PlJob条件，需要联合查询
                if (plJobConditions.Count > 0)
                {
                    _Logger.LogDebug("应用工作号过滤条件: {conditions}",
                        string.Join(", ", plJobConditions.Select(kv => $"{kv.Key}={kv.Value}")));

                    // 先获取符合PlJob条件的工作号
                    var jobIds = EfHelper.GenerateWhereAnd(_DbContext.PlJobs.AsNoTracking(), plJobConditions)
                        .Select(job => job.Id);

                    // 按ID链接查询相关申请单 - 注意保留与docIds的交集条件
                    dbSet = (from requisition in _DbContext.DocFeeRequisitions
                             where requisition.OrgId == context.User.OrgId && docIds.Contains(requisition.Id)
                             join item in _DbContext.DocFeeRequisitionItems on requisition.Id equals item.ParentId
                             join fee in _DbContext.DocFees on item.FeeId equals fee.Id
                             where fee.JobId.HasValue && jobIds.Contains(fee.JobId.Value)
                             select requisition).Distinct();
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeRequisitionReturnDto();
            if (!_EntityManager.Modify(new[] { model.DocFeeRequisition })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeRequisition);
            entity.Property(c => c.MakeDateTime).IsModified = false;
            entity.Property(c => c.MakerId).IsModified = false;
            entity.Property(c => c.OrgId).IsModified = false;
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的业务费用申请单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocFeeRequisitionReturnDto> RemoveDocFeeRequisition(RemoveDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeRequisitionReturnDto();
            
            try
            {
                var id = model.Id;
                var dbSet = _DbContext.DocFeeRequisitions;
                var item = dbSet.Find(id);
                if (item is null) return BadRequest();
                
                // 查找并获取所有关联的申请单明细项
                var items = _DbContext.DocFeeRequisitionItems.Where(c => c.ParentId == id).ToList();
                
                // 记录操作日志
                _DbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    OrgId = context.User.OrgId,
                    ActionId = $"Delete.{nameof(DocFeeRequisition)}.{item.Id}",
                    ExtraGuid = context.User.Id,
                    ExtraDecimal = items.Count,
                    WorldDateTime = OwHelper.WorldNow
                });
                
                // 如果有关联的明细项，先处理它们
                if (items.Count > 0)
                {
                    _Logger.LogInformation($"删除业务费用申请单 {id} 的 {items.Count} 个明细项");
                    
                    // 先恢复相关的费用状态
                    foreach (var detail in items)
                    {
                        // 当明细项有关联费用时，需要处理该费用
                        if (detail.FeeId.HasValue)
                        {
                            var fee = _DbContext.DocFees.Find(detail.FeeId.Value);
                            if (fee != null)
                            {
                                // 这里可以添加恢复费用状态的逻辑，例如更新某些标志字段
                                _Logger.LogDebug($"恢复费用 {fee.Id} 的状态");
                            }
                        }
                    }
                    
                    // 删除所有关联的明细项
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
        /// <param name="conditional">查询的条件。支持 Id 和 ParentId。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeRequisitionItemReturnDto> GetAllDocFeeRequisitionItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeRequisitionItemReturnDto();

            var dbSet = _DbContext.DocFeeRequisitionItems;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "ParentId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.ParentId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 获取申请单明细增强接口功能。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">条件使用 [实体名.字段名] (带实体名前缀的需要方括号括住)格式,值格式参见通用格式。
        /// 支持的实体名有：DocFeeRequisition,PlJob,DocFee,DocFeeRequisitionItem</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetDocFeeRequisitionItemReturnDto> GetDocFeeRequisitionItem([FromQuery] GetDocFeeRequisitionItemParamsDto model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            //查询 需要返回 申请单 job 费用实体 申请明细的余额（未结算）
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetDocFeeRequisitionItemReturnDto();
            var dbSet = _DbContext.DocFeeRequisitionItems;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            var collDocFeeRequisitionItem = EfHelper.GenerateWhereAndWithEntityName(coll, conditional);

            var collPlJob = EfHelper.GenerateWhereAndWithEntityName(_DbContext.PlJobs, conditional);

            var collDocFee = EfHelper.GenerateWhereAndWithEntityName(_DbContext.DocFees, conditional);

            var collDocFeeRequisitions = EfHelper.GenerateWhereAndWithEntityName(_DbContext.DocFeeRequisitions, conditional).Where(c => c.OrgId == context.User.OrgId);

            var collTmp = from item in collDocFeeRequisitionItem
                          join fee in collDocFee on item.FeeId equals fee.Id
                          join job in collPlJob on fee.JobId equals job.Id
                          join ri in collDocFeeRequisitions on item.ParentId equals ri.Id
                          select item;

            collTmp = collTmp.Distinct();
            result.Total = collTmp.Count();
            collTmp = collTmp.Skip(model.StartIndex);
            if (model.Count > 0)
                collTmp = collTmp.Take(model.Count);

            var aryResult = collTmp.ToArray();

            foreach (var item in aryResult)
            {
                var tmp = new GetDocFeeRequisitionItemItem
                {
                    DocFeeRequisitionItem = item,
                    DocFeeRequisition = _DbContext.DocFeeRequisitions.First(c => c.Id == item.ParentId),
                    DocFee = _DbContext.DocFees.First(c => c.Id == item.FeeId)
                };
                tmp.PlJob = _DbContext.PlJobs.First(c => c.Id == tmp.DocFee.JobId);
                tmp.Remainder = item.Amount - _DbContext.PlInvoicesItems.Where(c => c.RequisitionItemId == item.Id).Sum(c => c.Amount);
                result.Result.Add(tmp);
            }
            return result;
        }

        /// <summary>
        /// 增加新业务费用申请单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
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
            entity.GenerateNewId();
            _DbContext.DocFeeRequisitionItems.Add(model.DocFeeRequisitionItem);
            var req = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisitionItem.ParentId);
            //计算合计
            var parent = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisitionItem.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
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
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单明细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyDocFeeRequisitionItemReturnDto> ModifyDocFeeRequisitionItem(ModifyDocFeeRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeRequisitionItemReturnDto();
            if (!_EntityManager.Modify(new[] { model.DocFeeRequisitionItem })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeRequisitionItem);
            //计算合计
            var parent = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisitionItem.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
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
            _EntityManager.Remove(item);
            //计算合计
            var parent = _DbContext.DocFeeRequisitions.Find(item.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
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
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpPut]
        public ActionResult<SetDocFeeRequisitionItemReturnDto> SetDocFeeRequisitionItem(SetDocFeeRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetDocFeeRequisitionItemReturnDto();
            var fr = _DbContext.DocFeeRequisitions.Find(model.FrId);
            if (fr is null) return NotFound();
            var aryIds = model.Items.Select(c => c.Id).ToArray();   //指定的Id
            var existsIds = _DbContext.DocFeeRequisitionItems.Where(c => c.ParentId == fr.Id).Select(c => c.Id).ToArray();    //已经存在的Id
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
            _DbContext.RemoveRange(_DbContext.DocFeeRequisitionItems.Where(c => removeIds.Contains(c.Id)));

            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }

        #endregion 业务费用申请单明细

        #region 结算单

        /// <summary>
        /// 获取全部结算单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。
        /// 支持 DocFeeRequisition.属性名 格式的键，用于关联到申请单表进行过滤。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlInvoicesReturnDto> GetAllPlInvoices([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlInvoicesReturnDto();

            try
            {
                conditional = conditional ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // 使用nameof表达式获取DocFeeRequisition类名，并创建前缀
                string docFeeRequisitionPrefix = $"{nameof(DocFeeRequisition)}.";

                // 分离DocFeeRequisition相关条件和普通条件
                var reqConditions = conditional
                    .Where(pair => pair.Key.StartsWith(docFeeRequisitionPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        pair => pair.Key.Substring(docFeeRequisitionPrefix.Length), // 去掉前缀
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );

                // 收集所有不包含点号的条件
                var invoiceConditions = conditional
                    .Where(pair => !pair.Key.Contains("."))
                    .ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase
                    );

                IQueryable<PlInvoices> dbSet = _DbContext.PlInvoicess;

                // 如果有DocFeeRequisition相关的条件，则需要联合查询
                if (reqConditions.Count > 0)
                {
                    _Logger.LogDebug("应用申请单过滤条件: {conditions}",
                        string.Join(", ", reqConditions.Select(kv => $"{kv.Key}={kv.Value}")));

                    // 先获取符合DocFeeRequisition条件的申请单
                    var requisitions = EfHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions.AsNoTracking(), reqConditions);

                    // 通过申请单明细和结算单明细的关联，找到相关的结算单
                    dbSet = (from invoice in _DbContext.PlInvoicess
                             join item in _DbContext.PlInvoicesItems on invoice.Id equals item.ParentId
                             join reqItem in _DbContext.DocFeeRequisitionItems on item.RequisitionItemId equals reqItem.Id
                             join req in requisitions on reqItem.ParentId equals req.Id
                             select invoice).Distinct();
                }

                // 应用结算单自身的过滤条件
                var coll = EfHelper.GenerateWhereAnd(dbSet, invoiceConditions);

                // 应用排序和分页
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取结算单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取结算单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 增加新结算单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlInvoiceReturnDto> AddPlInvoice(AddPlInvoiceParamsDto model)
        {
            // 验证令牌和获取上下文
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("添加结算单时提供了无效的令牌: {token}", model.Token);
                return Unauthorized();
            }

            var result = new AddPlInvoiceReturnDto();

            try
            {
                // 验证权限
                string err;
                if (!_AuthorizationManager.Demand(out err, "F.3.1"))
                    return StatusCode((int)HttpStatusCode.Forbidden, err);

                // 验证输入参数
                if (model.PlInvoices == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "结算单数据不能为空";
                    return result;
                }

                // 获取要保存的实体并进行基础设置
                var entity = model.PlInvoices;
                entity.GenerateIdIfEmpty(); // 生成新的GUID

                // 设置创建信息
                entity.CreateBy = context.User.Id;
                entity.CreateDateTime = OwHelper.WorldNow;

                // 添加实体到数据库上下文
                _DbContext.PlInvoicess.Add(entity);

                // 应用审计日志(可选)
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了结算单ID:{entity.Id}，操作：AddPlInvoices");

                // 保存更改到数据库
                _DbContext.SaveChanges();

                // 设置返回结果
                result.Id = entity.Id;
                result.HasError = false;

                _Logger.LogDebug("成功创建结算单: {id}", entity.Id);
            }
            catch (Exception ex)
            {
                // 记录错误并设置返回错误信息
                _Logger.LogError(ex, "创建结算单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建结算单时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 修改结算单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlInvoicesReturnDto> ModifyPlInvoices(ModifyPlInvoicesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlInvoicesReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlInvoices })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.PlInvoices);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的结算单。这会删除所有结算单明细项。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlInvoicesReturnDto> RemovePlInvoices(RemovePlInvoicesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlInvoicesReturnDto();
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.3.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var id = model.Id;
            var dbSet = _DbContext.PlInvoicess;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            var children = _DbContext.PlInvoicesItems.Where(c => c.ParentId == item.Id).ToArray();
            _EntityManager.Remove(item);
            if (children.Length > 0) _DbContext.RemoveRange(children);
            _DbContext.OwSystemLogs.Add(new OwSystemLog
            {
                OrgId = context.User.OrgId,
                ActionId = $"Delete.{nameof(PlInvoices)}.{item.Id}",
                ExtraGuid = context.User.Id,
                ExtraDecimal = children.Length,
            });
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 结算单确认.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">至少有一个结算单已被确认或至少有一个结算单是自己创建的。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpPost]
        public ActionResult<ConfirmPlInvoicesReturnDto> ConfirmPlInvoices(ConfirmPlInvoicesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ConfirmPlInvoicesReturnDto();
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.3.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var coll = _DbContext.PlInvoicess.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (coll.Length != model.Ids.Count) return BadRequest("至少有一个id不存在对应的结算单");

            var now = OwHelper.WorldNow;
            foreach (var invoice in coll)
            {
                if (model.IsConfirm)
                {
                    // 确认结算单
                    invoice.ConfirmDateTime = now;
                    invoice.ConfirmId = context.User.Id;
                }
                else
                {
                    // 取消确认结算单
                    invoice.ConfirmDateTime = null;
                    invoice.ConfirmId = null;
                }
            }

            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取结算单明细增强接口功能。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">条件使用 [实体名.字段名] (带实体名前缀的需要方括号括住)格式,值格式参见通用格式。
        /// 支持的实体名有：PlJob,DocFeeRequisition,DocFeeRequisitionItem，PlInvoices ,PlInvoicesItem</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetPlInvoicesItemReturnDto> GetDocInvoicesItem([FromQuery] GetPlInvoicesItemParamsDto model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            //查询 需要返回 申请单 job 费用实体 申请明细的余额（未结算）
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetPlInvoicesItemReturnDto();
            var dbSet = _DbContext.PlInvoicesItems;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            var collInvoicesItem = EfHelper.GenerateWhereAndWithEntityName(coll, conditional);

            var collPlJob = EfHelper.GenerateWhereAndWithEntityName(_DbContext.PlJobs, conditional);

            var collInvoice = EfHelper.GenerateWhereAndWithEntityName(_DbContext.PlInvoicess, conditional);

            var collDocFeeRequisition = EfHelper.GenerateWhereAndWithEntityName(_DbContext.DocFeeRequisitions, conditional).Where(c => c.OrgId == context.User.OrgId);

            var collDocFeeRequisitionItem = EfHelper.GenerateWhereAndWithEntityName(_DbContext.DocFeeRequisitionItems, conditional);

            var collBase = from fii in collInvoicesItem
                           join fi in collInvoice on fii.ParentId equals fi.Id
                           join fri in collDocFeeRequisitionItem on fii.RequisitionItemId equals fri.Id
                           join fr in collDocFeeRequisition on fri.ParentId equals fr.Id
                           join fee in _DbContext.DocFees on fri.FeeId equals fee.Id
                           join job in collPlJob on fee.JobId equals job.Id
                           select new { fii, fi, job, fr, fri };
            //获取总数
            var collCount = collBase.Distinct();
            result.Total = collCount.Count();
            //获取集合
            collBase = collBase.Skip(model.StartIndex);
            if (model.Count > 0)
                collBase = collBase.Take(model.Count);
            var aryResult = collBase.ToArray();

            foreach (var item in aryResult)
            {
                var tmp = new GetPlInvoicesItemItem
                {
                    InvoicesItem = item.fii,
                    Invoices = item.fi,
                    PlJob = item.job,
                    DocFeeRequisitionItem = item.fri,
                    DocFeeRequisition = item.fr,
                    Parent = item.fi,
                };
                //tmp.Remainder = item.Amount - _DbContext.PlInvoicesItems.Where(c => c.RequisitionItemId == item.Id).Sum(c => c.Amount);
                result.Result.Add(tmp);
            }
            return result;
        }

        #endregion 结算单

        #region 结算单明细

        /// <summary>
        /// 获取全部结算单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlInvoicesItemReturnDto> GetAllPlInvoicesItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlInvoicesItemReturnDto();
            var dbSet = _DbContext.PlInvoicesItems;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新结算单明细。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlInvoicesItemReturnDto> AddPlInvoicesItem(AddPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.3.1") && !_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlInvoicesItemReturnDto();
            var entity = model.PlInvoicesItem;
            entity.GenerateNewId();
            _DbContext.PlInvoicesItems.Add(model.PlInvoicesItem);

            _DbContext.SaveChanges();

            result.Id = model.PlInvoicesItem.Id;
            return result;
        }

        /// <summary>
        /// 修改结算单明细信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单明细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlInvoicesItemReturnDto> ModifyPlInvoicesItem(ModifyPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlInvoicesItemReturnDto();
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.3.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (!_EntityManager.Modify(new[] { model.PlInvoicesItem })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.PlInvoicesItem);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的结算单明细。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的结算单明细不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlInvoicesItemReturnDto> RemovePlInvoicesItem(RemovePlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.3.2") && !_AuthorizationManager.Demand(out err, "F.3.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlInvoicesItemReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlInvoicesItems;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
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
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务费用申请单不存在。</response>  
        [HttpPut]
        public ActionResult<SetPlInvoicesItemReturnDto> SetPlInvoicesItem(SetPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetPlInvoicesItemReturnDto();
            var fr = _DbContext.DocFeeRequisitions.Find(model.FrId);
            if (fr is null) return NotFound();
            var aryIds = model.Items.Select(c => c.Id).ToArray();   //指定的Id
            var existsIds = _DbContext.PlInvoicesItems.Where(c => c.ParentId == fr.Id).Select(c => c.Id).ToArray();    //已经存在的Id
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
            _DbContext.RemoveRange(_DbContext.PlInvoicesItems.Where(c => removeIds.Contains(c.Id)));

            _DbContext.SaveChanges();
            //后处理
            result.Result.AddRange(model.Items);
            return result;
        }

        #endregion 结算单明细

        #region 费用方案

        /// <summary>
        /// 获取全部费用方案。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeTemplateReturnDto> GetAllDocFeeTemplate([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
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
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }

            #region 权限判定
            string err;
            var docFeeTT = model.DocFeeTemplate;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
            string err;
            var docFeeTT = model.DocFeeTemplate;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定
            if (!_EntityManager.Modify(new[] { model.DocFeeTemplate })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeTemplate);
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
            var dbSet = _DbContext.DocFeeTemplates;
            if (dbSet.Find(id) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            string err;
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
        /// <param name="conditional">查询的条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的费用方案不存在。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeTemplateItemReturnDto> GetAllDocFeeTemplateItem([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
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
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocFeeTemplateItemReturnDto();
            var entity = model.DocFeeTemplateItem;

            var id = model.DocFeeTemplateItem.ParentId;
            if (id is null) return BadRequest();
            var dbSet = _DbContext.DocFeeTemplates;
            if (dbSet.Find(id.Value) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            string err;
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
            var dbSet = _DbContext.DocFeeTemplates;
            if (dbSet.Find(id.Value) is not DocFeeTemplate item) return BadRequest();
            #region 权限判定
            string err;
            var docFeeTT = item;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            #endregion 权限判定

            if (!_EntityManager.Modify(new[] { model.DocFeeTemplateItem })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeTemplateItem);
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
            var dbSet = _DbContext.DocFeeTemplateItems;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();

            var idTT = item.ParentId;
            if (idTT is null) return BadRequest();

            if (_DbContext.DocFeeTemplates.Find(idTT.Value) is not DocFeeTemplate tt) return BadRequest();
            #region 权限判定
            string err;
            var docFeeTT = tt;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
            var fr = _DbContext.DocFeeRequisitions.Find(model.FrId);
            if (fr is null) return NotFound();

            var idTT = model.FrId;
            if (_DbContext.DocFeeTemplates.Find(idTT) is not DocFeeTemplate tt) return BadRequest();
            #region 权限判定
            string err;
            var docFeeTT = tt;
            if (docFeeTT.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (docFeeTT.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D20.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
