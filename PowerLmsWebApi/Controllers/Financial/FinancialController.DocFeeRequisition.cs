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
    public partial class FinancialController : PlControllerBase
    {
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ModifyDocFeeRequisitionReturnDto();

            // 首先获取原始实体并保存需要保留的值
            var originalEntity = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisition.Id);
            if (originalEntity == null)
                return NotFound();

            var originalMakerId = originalEntity.MakerId;
            var originalMakeDateTime = originalEntity.MakeDateTime;

            // 使用_EntityManager.Modify更新实体
            if (!_EntityManager.Modify(new[] { model.DocFeeRequisition }))
                return NotFound();

            // 确保旧值的属性不被修改
            var entry = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisition.Id);

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
        /// <param name="conditional">条件使用 实体名.字段名 格式，值格式参见通用格式。
        /// 支持的实体名(不区分大小写)有：DocFeeRequisition,PlJob,DocFee,DocFeeRequisitionItem,DocBill</param>
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
                // 创建各实体的条件字典（不区分大小写）
                var itemConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var jobConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var feeConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var requisitionConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var billConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // 新增DocBill条件字典

                // 将conditional中的条件按实体类型分类
                if (conditional != null)
                {
                    foreach (var pair in conditional)
                    {
                        // 查找第一个点号
                        int dotIndex = pair.Key.IndexOf('.');

                        // 如果没有点号，则默认是DocFeeRequisitionItem的条件
                        if (dotIndex < 0)
                        {
                            itemConditions[pair.Key] = pair.Value;
                            continue;
                        }

                        // 提取实体名并标准化为小写
                        string entityName = pair.Key.Substring(0, dotIndex).ToLowerInvariant();
                        string propertyName = pair.Key.Substring(dotIndex + 1);

                        // 按实体名分发条件
                        switch (entityName)
                        {
                            case "docfeerequisitionitem":
                                itemConditions[propertyName] = pair.Value;
                                break;
                            case "pljob":
                                jobConditions[propertyName] = pair.Value;
                                break;
                            case "docfee":
                                feeConditions[propertyName] = pair.Value;
                                break;
                            case "docfeerequisition":
                                requisitionConditions[propertyName] = pair.Value;
                                break;
                            case "docbill": // 处理DocBill相关的条件
                                billConditions[propertyName] = pair.Value;
                                break;
                        }
                    }
                }

                // 向requisitionConditions添加组织ID限制
                requisitionConditions["OrgId"] = context.User.OrgId.ToString();

                // 应用各实体的筛选条件
                var itemsQuery = EfHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitionItems, itemConditions);
                var jobsQuery = EfHelper.GenerateWhereAnd(_DbContext.PlJobs, jobConditions);
                var feesQuery = EfHelper.GenerateWhereAnd(_DbContext.DocFees, feeConditions);
                var requisitionsQuery = EfHelper.GenerateWhereAnd(_DbContext.DocFeeRequisitions, requisitionConditions);
                var billsQuery = EfHelper.GenerateWhereAnd(_DbContext.DocBills, billConditions); // 应用DocBill的条件

                // 先对DocFeeRequisitionItem排序，然后再进行连接查询
                var orderedItemsQuery = itemsQuery.OrderBy(model.OrderFieldName, model.IsDesc);

                // 构建包含DocBill的复合查询
                var query = from item in orderedItemsQuery
                            join req in requisitionsQuery on item.ParentId equals req.Id
                            join fee in feesQuery on item.FeeId equals fee.Id
                            join job in jobsQuery on fee.JobId equals job.Id
                            join bill in billsQuery on fee.BillId equals bill.Id //into billGroup from bill in billGroup.DefaultIfEmpty()// 左连接DocBill（因为DocFee.BillId可能为空）
                            select new
                            {
                                item,
                                req,
                                fee,
                                job,
                                bill
                            };

                // 计算总数
                result.Total = query.Count();

                // 应用分页
                var pagedQuery = query.Skip(model.StartIndex);
                if (model.Count > 0)
                    pagedQuery = pagedQuery.Take(model.Count);

                // 先获取实体数据，避免在后续步骤中再次查询数据库
                var queryResults = pagedQuery.AsNoTracking().ToList();

                // 获取所有请求项ID，用于查询已结算金额
                var itemIds = queryResults.Select(x => x.item.Id).ToList();

                // 修正：先在数据库级别执行分组和求和，然后获取数据到内存
                var invoiceItems = _DbContext.PlInvoicesItems
                    .Where(x => x.RequisitionItemId.HasValue && itemIds.Contains(x.RequisitionItemId.Value))
                    .GroupBy(x => x.RequisitionItemId.Value)
                    .Select(g => new { RequisitionItemId = g.Key, InvoicedAmount = g.Sum(x => x.Amount) })
                    .AsNoTracking()
                    .ToList();

                // 在内存中创建字典
                var invoicedAmounts = invoiceItems.ToDictionary(
                    x => x.RequisitionItemId,
                    x => x.InvoicedAmount
                );

                // 组装结果
                foreach (var data in queryResults)
                {
                    var resultItem = new GetDocFeeRequisitionItemItem
                    {
                        DocFeeRequisitionItem = data.item,
                        DocFeeRequisition = data.req,
                        DocFee = data.fee,
                        PlJob = data.job,
                        DocBill = data.bill // 添加DocBill对象
                    };

                    // 计算余额：明细金额减去已结算金额
                    decimal invoicedAmount = 0;
                    if (invoicedAmounts.TryGetValue(data.item.Id, out var amount))
                        invoicedAmount = amount;

                    resultItem.Remainder = data.item.Amount - invoicedAmount;

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
        /// <response code="403">权限不足。当前暂无专用权限控制，未来将增加权限控制。</response>
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
                // 1. 权限验证（目前无专用权限，注释说明未来增加权限控制）
                // TODO: 未来需要增加专门的回退权限控制，如 "F.3.10" 或各业务类型特定权限
                string err;
                if (!_AuthorizationManager.Demand(out err, "F.3"))  // 暂时使用通用财务管理权限
                {
                    _Logger.LogWarning("权限不足，用户{UserId}尝试回退主营业务费用申请单{RequisitionId}", context.User.Id, model.RequisitionId);
                    return StatusCode((int)HttpStatusCode.Forbidden, "权限不足：当前暂无专用回退权限，未来将增加专门的权限控制");
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
    }
}
