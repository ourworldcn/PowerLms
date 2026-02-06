/*
 * 项目：PowerLms WebApi | 模块：财务管理
 * 功能：账单管理控制器
 * 技术要点：账单CRUD、权限验证、多租户隔离
 * 作者：zc | 创建：2026-02 | 修改：2026-02-06 从PlJobController重构独立
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 账单管理控制器。
    /// 提供账单的增删改查功能。
    /// </summary>
    public partial class DocBillController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DocBillController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<DocBillController> logger, IMapper mapper, AuthorizationManager authorizationManager,
            OwSqlAppLogger sqlAppLogger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _SqlAppLogger = sqlAppLogger;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly ILogger<DocBillController> _Logger;
        readonly IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;
        readonly OwSqlAppLogger _SqlAppLogger;

        #region 账单管理

        /// <summary>
        /// 获取全部业务单的账单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典。支持 Id，DocNo(业务单Id),JobId(间接属于指定的业务Id)。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllDocBillReturnDto> GetAllDocBill([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocBillReturnDto();
            var query = from bill in _DbContext.DocBills
                        join fee in _DbContext.DocFees on bill.Id equals fee.BillId
                        join job in _DbContext.PlJobs on fee.JobId equals job.Id
                        where job.OrgId == context.User.OrgId
                        select bill;
            var coll = query.Distinct().OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(DocBill.DocNo), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DocNo == item.Value);
                }
                else if (string.Equals(item.Key, "JobId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                    {
                        var collBillId = from job in _DbContext.PlJobs
                                         where job.Id == id && job.OrgId == context.User.OrgId
                                         join fee in _DbContext.DocFees
                                         on job.Id equals fee.JobId
                                         where fee.BillId != null
                                         select fee.BillId.Value;
                        coll = coll.Where(c => collBillId.Contains(c.Id));
                    }
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新业务单的账单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">至少一个费用Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddDocBillReturnDto> AddDocBill(AddDocBillParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocBillReturnDto();
            try
            {
                if (model.FeeIds == null || model.FeeIds.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "必须指定至少一个费用ID";
                    return result;
                }
                var entity = model.DocBill;
                entity.GenerateIdIfEmpty();
                var collFees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id)).ToArray();
                if (collFees.Length != model.FeeIds.Count)
                {
                    return BadRequest("至少一个费用ID不存在");
                }
                var feeJobIds = collFees.Where(f => f.JobId.HasValue).Select(f => f.JobId.Value).Distinct().ToArray();
                if (feeJobIds.Length == 0)
                {
                    return BadRequest("费用必须关联到有效的工作号");
                }
                var jobs = _DbContext.PlJobs.Where(j => feeJobIds.Contains(j.Id)).ToArray();
                if (jobs.Any(j => j.OrgId != context.User.OrgId))
                {
                    _Logger.LogWarning("尝试创建跨机构账单，用户机构：{UserOrgId}，费用关联机构：{FeeOrgIds}",
                        context.User.OrgId, string.Join(",", jobs.Select(j => j.OrgId)));
                    return BadRequest("不能将不同机构的费用添加到同一账单");
                }
                var collPerm = GetJobsFromFeeIds(model.FeeIds);
                if (collPerm.Any())
                {
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.AeId))
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D0.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.AiId))
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D1.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.SeId))
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D2.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.SiId))
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D3.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                }
                if (entity is ICreatorInfo creatorInfo)
                {
                    creatorInfo.CreateBy = context.User.Id;
                    creatorInfo.CreateDateTime = OwHelper.WorldNow;
                }
                var linkedFeeIds = collFees.Where(c => c.BillId.HasValue && c.BillId != entity.Id).Select(c => c.Id).ToArray();
                if (linkedFeeIds.Any())
                {
                    return BadRequest($"以下费用ID已关联到其他账单: {string.Join(", ", linkedFeeIds)}");
                }
                _DbContext.DocBills.Add(model.DocBill);
                foreach (var fee in collFees)
                {
                    fee.BillId = model.DocBill.Id;
                }
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了账单ID:{entity.Id}，机构ID:{context.User.OrgId}，操作：AddDocBill");
                _DbContext.SaveChanges();
                result.Id = model.DocBill.Id;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建账单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建账单时发生错误: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 修改业务单的账单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">至少一个费用Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的业务单的账单不存在。</response>
        [HttpPut]
        public ActionResult<ModifyDocBillReturnDto> ModifyDocBill(ModifyDocBillParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocBillReturnDto();
            var existingBill = _DbContext.DocBills.Find(model.DocBill.Id);
            if (existingBill == null)
                return NotFound("指定Id的账单不存在");
            var existingFee = _DbContext.DocFees.FirstOrDefault(f => f.BillId == existingBill.Id);
            if (existingFee != null && existingFee.JobId.HasValue)
            {
                var existingJob = _DbContext.PlJobs.Find(existingFee.JobId.Value);
                if (existingJob != null && existingJob.OrgId != context.User.OrgId)
                {
                    _Logger.LogWarning("尝试修改其他机构的账单，账单ID：{BillId}，用户机构：{UserOrgId}，账单机构：{BillOrgId}",
                        existingBill.Id, context.User.OrgId, existingJob.OrgId);
                    return StatusCode((int)HttpStatusCode.Forbidden, "无权修改其他机构的账单");
                }
            }
            var collFees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id)).ToList();
            if (collFees.Count != model.FeeIds.Count)
            {
                return BadRequest("至少一个费用Id不存在。");
            }
            var newFeeJobIds = collFees.Where(f => f.JobId.HasValue).Select(f => f.JobId.Value).Distinct().ToArray();
            if (newFeeJobIds.Length > 0)
            {
                var newJobs = _DbContext.PlJobs.Where(j => newFeeJobIds.Contains(j.Id)).ToArray();
                if (newJobs.Any(j => j.OrgId != context.User.OrgId))
                {
                    _Logger.LogWarning("尝试关联其他机构的费用到账单，账单ID：{BillId}，用户机构：{UserOrgId}",
                        existingBill.Id, context.User.OrgId);
                    return BadRequest("不能将其他机构的费用关联到账单");
                }
            }
            var oldFee = _DbContext.DocFees.Where(c => c.BillId == model.DocBill.Id).ToList();
            var jobs = GetJobsFromFeeIds(oldFee.Select(c => c.Id));
            if (jobs.Any())
            {
                if (jobs.Any(c => c.JobTypeId == ProjectContent.AeId))
                {
                    if (!_AuthorizationManager.Demand(out string err, "D0.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                if (jobs.Any(c => c.JobTypeId == ProjectContent.AiId))
                {
                    if (!_AuthorizationManager.Demand(out string err, "D1.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                if (jobs.Any(c => c.JobTypeId == ProjectContent.SeId))
                {
                    if (!_AuthorizationManager.Demand(out string err, "D2.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                if (jobs.Any(c => c.JobTypeId == ProjectContent.SiId))
                {
                    if (!_AuthorizationManager.Demand(out string err, "D3.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
            }
            try
            {
                _DbContext.Entry(existingBill).CurrentValues.SetValues(model.DocBill);
                foreach (var fee in oldFee)
                {
                    fee.BillId = null;
                    _DbContext.Entry(fee).Property(f => f.BillId).IsModified = true;
                }
                foreach (var fee in collFees)
                {
                    fee.BillId = model.DocBill.Id;
                    _DbContext.Entry(fee).Property(f => f.BillId).IsModified = true;
                }
                _DbContext.SaveChanges();
                _SqlAppLogger.LogGeneralInfo($"修改账单.{nameof(DocBill)}.{model.DocBill.Id}，机构ID:{context.User.OrgId}");
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改账单时发生错误，账单ID: {BillId}", model.DocBill.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改账单时发生错误: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 删除指定Id的业务单的账单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 Error Code 。</response>
        /// <response code="400">未找到指定的业务，或该账单关联的费用已被申请，不能删除。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的业务单的账单不存在。或已经被申请费用。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveDocBillReturnDto> RemoveDocBill(RemoveDocBillParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocBillReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocBills;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest("找不到指定的账单");
            var billFee = _DbContext.DocFees.FirstOrDefault(f => f.BillId == id);
            if (billFee != null && billFee.JobId.HasValue)
            {
                var billJob = _DbContext.PlJobs.Find(billFee.JobId.Value);
                if (billJob != null && billJob.OrgId != context.User.OrgId)
                {
                    _Logger.LogWarning("尝试删除其他机构的账单，账单ID：{BillId}，用户机构：{UserOrgId}，账单机构：{BillOrgId}",
                    id, context.User.OrgId, billJob.OrgId);
                    return StatusCode((int)HttpStatusCode.Forbidden, "无权删除其他机构的账单");
                }
            }
            var jobs = GetJobsFromBillIds(new Guid[] { model.Id });
            if (jobs.Any(c => c.JobTypeId == ProjectContent.AeId))
                if (!_AuthorizationManager.Demand(out string err, "D0.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (jobs.Any(c => c.JobTypeId == ProjectContent.AeId))
                if (!_AuthorizationManager.Demand(out string err, "D1.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (jobs.Any(c => c.JobTypeId == ProjectContent.SeId))
                if (!_AuthorizationManager.Demand(out string err, "D2.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (jobs.Any(c => c.JobTypeId == ProjectContent.SiId))
                if (!_AuthorizationManager.Demand(out string err, "D3.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var usedFeeIds = (from fee in _DbContext.DocFees
                              where fee.BillId == id
                              join requisitionItem in _DbContext.DocFeeRequisitionItems on fee.Id equals requisitionItem.FeeId
                              select fee.Id).ToList();
            if (usedFeeIds.Any())
            {
                return BadRequest($"账单(ID:{id})关联的费用已被申请，无法删除账单");
            }
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            _SqlAppLogger.LogGeneralInfo($"删除账单.{nameof(DocBill)}.{id}，机构ID:{context.User.OrgId}");
            return result;
        }

        /// <summary>
        /// 根据业务Id，获取相关账单对象。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">至少有一个指定Id的业务不存在。</response>
        [HttpGet]
        public ActionResult<GetDocBillsByJobIdReturnDto> GetDocBillsByJobIds([FromQuery] GetDocBillsByJobIdParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetDocBillsByJobIdReturnDto();
            var collJob = _DbContext.PlJobs.Where(c => model.Ids.Contains(c.Id) && c.OrgId == context.User.OrgId);
            if (collJob.Count() != model.Ids.Count) return NotFound("至少有一个指定Id的业务不存在或不属于当前机构");
            var allowAe = _AuthorizationManager.Demand(out string err, "D0.7.2");
            var allowAi = _AuthorizationManager.Demand(out string err2, "D1.7.2");
            var coll = from job in _DbContext.PlJobs
                       where model.Ids.Contains(job.Id) &&
                             job.OrgId == context.User.OrgId &&
                             (allowAe || job.JobTypeId != ProjectContent.AeId) &&
                             (allowAi || job.JobTypeId != ProjectContent.AiId)
                       join fee in _DbContext.DocFees
                       on job.Id equals fee.JobId
                       join bill in _DbContext.DocBills
                       on fee.BillId equals bill.Id
                       select new { job.Id, bill };
            var r = coll.ToArray().GroupBy(c => c.Id, c => c.bill);
            var collDto = r.Select(c =>
            {
                var r = new GetDocBillsByJobIdItemDto { JobId = c.Key };
                r.Bills.AddRange(c);
                return r;
            });
            result.Result.AddRange(collDto);
            return result;
        }

        /// <summary>
        /// 从费用生成账单（批量）。
        /// 自动为指定费用创建账单，按结算单位和收支方向自动分组，每组生成一个账单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">参数错误或业务规则验证失败。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定的费用不存在。</response>
        [HttpPost]
        public ActionResult<AddDocBillsFromFeesReturnDto> AddDocBillsFromFees(AddDocBillsFromFeesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new AddDocBillsFromFeesReturnDto();
            try
            {
                if (model.FeeIds == null || !model.FeeIds.Any())
                {
                    result.DebugMessage = "必须指定至少一个费用ID";
                    return result;
                }
                var allFees = _DbContext.DocFees
                    .Where(f => model.FeeIds.Contains(f.Id))
                    .ToList();
                if (allFees.Count != model.FeeIds.Count)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "至少有一个费用ID不存在";
                    return result;
                }
                var feeJobIds = allFees.Where(f => f.JobId.HasValue).Select(f => f.JobId.Value).Distinct().ToList();
                if (feeJobIds.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "费用必须关联到有效的工作号";
                    return result;
                }
                var jobs = _DbContext.PlJobs.Where(j => feeJobIds.Contains(j.Id)).ToList();
                if (jobs.Any(j => j.OrgId != context.User.OrgId))
                {
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = "不能操作其他机构的费用";
                    return result;
                }
                var jobTypeGroups = jobs.GroupBy(j => j.JobTypeId).ToList();
                foreach (var jobTypeGroup in jobTypeGroups)
                {
                    if (jobTypeGroup.Key == ProjectContent.AeId)
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D0.7.1"))
                        {
                            result.HasError = true;
                            result.ErrorCode = 403;
                            result.DebugMessage = $"空运出口业务权限不足：{err}";
                            return result;
                        }
                    }
                    else if (jobTypeGroup.Key == ProjectContent.AiId)
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D1.7.1"))
                        {
                            result.HasError = true;
                            result.ErrorCode = 403;
                            result.DebugMessage = $"空运进口业务权限不足：{err}";
                            return result;
                        }
                    }
                    else if (jobTypeGroup.Key == ProjectContent.SeId)
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D2.7.1"))
                        {
                            result.HasError = true;
                            result.ErrorCode = 403;
                            result.DebugMessage = $"海运出口业务权限不足：{err}";
                            return result;
                        }
                    }
                    else if (jobTypeGroup.Key == ProjectContent.SiId)
                    {
                        if (!_AuthorizationManager.Demand(out string err, "D3.7.1"))
                        {
                            result.HasError = true;
                            result.ErrorCode = 403;
                            result.DebugMessage = $"海运进口业务权限不足：{err}";
                            return result;
                        }
                    }
                }
                var eligibleFees = allFees
                    .Where(f => f.AuditDateTime != null)
                    .Where(f => f.BillId == null)
                    .Where(f => f.BalanceId != null)
                    .ToList();
                if (!eligibleFees.Any())
                {
                    result.DebugMessage = "所选费用中没有符合条件的（已审核、未建账单、有结算单位）";
                    return result;
                }
                var groupedFees = eligibleFees
                    .GroupBy(f => new { f.JobId, f.BalanceId, f.IO })
                    .ToList();
                var createdBills = new List<DocBill>();
                var createdTime = OwHelper.WorldNow;
                foreach (var group in groupedFees)
                {
                    var currencies = group.Select(f => f.Currency).Distinct().ToList();
                    if (currencies.Count > 1)
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"工作号 {jobs.First(j => j.Id == group.Key.JobId).JobNo} 的结算单位 {group.Key.BalanceId} {(group.Key.IO ? "收入" : "支出")} 费用存在多币种 ({string.Join(", ", currencies)})，无法自动生成账单";
                        return result;
                    }
                    var job = jobs.First(j => j.Id == group.Key.JobId);
                    var bill = new DocBill
                    {
                        Id = Guid.NewGuid(),
                        BillNo = GenerateBillNo(context.User.OrgId.Value, createdTime),
                        PayerId = group.Key.BalanceId.Value,
                        CurrTypeId = model.DefaultCurrency ?? "CNY",
                        IO = group.Key.IO,
                        Amount = group.Sum(f => f.Amount),
                        DocNo = job.JobNo,
                        MblNo = job.MblNo,
                        LoadingCode = job.LoadingCode,
                        DestinationCode = job.DestinationCode,
                        Etd = job.Etd ?? default,
                        Eta = job.ETA ?? default,
                        GoodsName = job.GoodsName,
                        PkgsCount = job.PkgsCount ?? 0,
                        Weight = job.Weight ?? 0,
                        ChargeWeight = job.ChargeWeight ?? 0,
                        MeasureMent = job.MeasureMent ?? 0,
                        Consignor = job.Consignor,
                        Consignee = job.Consignee,
                        Carrier = job.CarrierNo,
                        CreateBy = context.User.Id,
                        CreateDateTime = createdTime,
                        IsEnable = true
                    };
                    _DbContext.DocBills.Add(bill);
                    foreach (var fee in group)
                    {
                        fee.BillId = bill.Id;
                    }
                    createdBills.Add(bill);
                }
                _DbContext.SaveChanges();
                result.CreatedBillIds = createdBills.Select(b => b.Id).ToList();
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "从费用批量生成账单时发生错误 - 费用数量: {FeeCount}", model.FeeIds.Count);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"批量生成账单时发生错误: {ex.Message}";
                return result;
            }
        }

        #endregion 账单管理

        #region 辅助方法

        /// <summary>
        /// 生成账单号。
        /// TODO: 由开发人员后续实现具体的编号规则。
        /// </summary>
        /// <param name="orgId">机构ID</param>
        /// <param name="createTime">创建时间</param>
        /// <returns>账单号</returns>
        private string GenerateBillNo(Guid orgId, DateTime createTime)
        {
            return $"BILL-{createTime:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
        }

        private IEnumerable<PlJob> GetJobsFromFeeIds(IEnumerable<Guid> feeIds)
        {
            var jobIds = _DbContext.DocFees.Where(c => feeIds.Contains(c.Id)).Select(c => c.JobId).Distinct().ToArray();
            var jobs = _DbContext.PlJobs.Where(c => jobIds.Contains(c.Id));
            return jobs;
        }

        private IEnumerable<PlJob> GetJobsFromBillIds(IEnumerable<Guid> billIds)
        {
            var feeIds = _DbContext.DocFees.Where(c => billIds.Contains(c.BillId.Value)).Select(c => c.Id).Distinct().ToArray();
            return GetJobsFromFeeIds(feeIds);
        }

        #endregion 辅助方法
    }
}
