/*
 * 项目：PowerLms WebApi | 模块：财务管理
 * 功能：费用管理控制器
 * 技术要点：费用增删改查、批量审核
 * 作者：zc | 创建：2026-02 | 修改：2026-02-06 从PlJobController重构独立
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System.Linq.Expressions;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 费用管理控制器。
    /// 提供费用的增删改查和审核功能。
    /// </summary>
    public partial class DocFeeController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="accountManager">账户管理器</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="entityManager">实体管理器</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="mapper">对象映射器</param>
        /// <param name="authorizationManager">权限管理器</param>
        public DocFeeController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<DocFeeController> logger, IMapper mapper, AuthorizationManager authorizationManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly EntityManager _EntityManager;
        readonly PowerLmsUserDbContext _DbContext;
        readonly ILogger<DocFeeController> _Logger;
        readonly IMapper _Mapper;
        readonly AuthorizationManager _AuthorizationManager;

        #region 费用管理

        /// <summary>
        /// 审核或取消审核费用（支持单笔和批量）。
        /// 批量操作采用原子化策略：全部成功或全部失败。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">参数验证失败或部分费用不满足审核条件。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AuditDocFeeReturnDto> AuditDocFee(AuditDocFeeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AuditDocFeeReturnDto();
            if (model.FeeIds == null || !model.FeeIds.Any())
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = "费用Id列表不能为空";
                return BadRequest("费用Id列表不能为空");
            }
            var auditDateTime = OwHelper.WorldNow;
            var feesToAudit = new List<DocFee>();
            var jobsToCheck = new HashSet<PlJob>();
            foreach (var feeId in model.FeeIds)
            {
                var itemResult = new AuditDocFeeResultItem { FeeId = feeId };
                var fee = _DbContext.DocFees.Find(feeId);
                if (fee == null)
                {
                    itemResult.Success = false;
                    itemResult.ErrorMessage = $"未找到费用Id={feeId}";
                    result.Results.Add(itemResult);
                    result.FailureCount++;
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = itemResult.ErrorMessage;
                    return BadRequest(itemResult.ErrorMessage);
                }
                var job = _DbContext.PlJobs.Find(fee.JobId);
                if (job == null)
                {
                    itemResult.Success = false;
                    itemResult.ErrorMessage = $"未找到关联的工作号，JobId={fee.JobId}";
                    result.Results.Add(itemResult);
                    result.FailureCount++;
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = itemResult.ErrorMessage;
                    return BadRequest(itemResult.ErrorMessage);
                }
                bool hasPermission = false;
                string permissionError = null;
                if (model.IsAudit && _AuthorizationManager.Demand(out string err, "F.2.4.5") ||
                    !model.IsAudit && _AuthorizationManager.Demand(out string err2, "F.2.4.6"))
                {
                    hasPermission = true;
                }
                else
                {
                    if (job.JobTypeId == ProjectContent.AeId)
                    {
                        hasPermission = _AuthorizationManager.Demand(out permissionError, "D0.6.7");
                    }
                    else if (job.JobTypeId == ProjectContent.AiId)
                    {
                        hasPermission = _AuthorizationManager.Demand(out permissionError, "D1.6.7");
                    }
                    else if (job.JobTypeId == ProjectContent.SeId)
                    {
                        hasPermission = _AuthorizationManager.Demand(out permissionError, "D2.6.7");
                    }
                    else if (job.JobTypeId == ProjectContent.SiId)
                    {
                        hasPermission = _AuthorizationManager.Demand(out permissionError, "D3.6.7");
                    }
                }
                if (!hasPermission)
                {
                    itemResult.Success = false;
                    itemResult.ErrorMessage = $"权限不足：{permissionError}";
                    result.Results.Add(itemResult);
                    result.FailureCount++;
                    result.HasError = true;
                    result.ErrorCode = 403;
                    result.DebugMessage = itemResult.ErrorMessage;
                    return StatusCode((int)HttpStatusCode.Forbidden, itemResult.ErrorMessage);
                }
                if (job.JobState > 4)
                {
                    itemResult.Success = false;
                    itemResult.ErrorMessage = $"所属任务已经不可更改（JobState={job.JobState}），工作号={job.JobNo}";
                    result.Results.Add(itemResult);
                    result.FailureCount++;
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = itemResult.ErrorMessage;
                    return BadRequest(itemResult.ErrorMessage);
                }
                feesToAudit.Add(fee);
                jobsToCheck.Add(job);
                itemResult.Success = true;
                result.Results.Add(itemResult);
            }
            try
            {
                foreach (var fee in feesToAudit)
                {
                    if (model.IsAudit)
                    {
                        fee.AuditDateTime = auditDateTime;
                        fee.AuditOperatorId = context.User.Id;
                    }
                    else
                    {
                        fee.AuditDateTime = null;
                        fee.AuditOperatorId = null;
                    }
                }
                _DbContext.SaveChanges();
                result.SuccessCount = feesToAudit.Count;
                _Logger.LogInformation(
                    "批量{Action}费用成功，数量={Count}，操作人={Operator}",
                    model.IsAudit ? "审核" : "取消审核",
                    result.SuccessCount,
                    context.User.DisplayName);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量{Action}费用时发生异常", model.IsAudit ? "审核" : "取消审核");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"保存失败：{ex.Message}";
                result.Results.ForEach(r =>
                {
                    r.Success = false;
                    r.ErrorMessage = "数据库保存失败，所有操作已回滚";
                });
                result.FailureCount = result.Results.Count;
                result.SuccessCount = 0;
                return StatusCode(500, result.DebugMessage);
            }
            return result;
        }

        /// <summary>
        /// 获取全部业务单的费用单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典。支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpGet]
        public ActionResult<GetAllDocFeeReturnDto> GetAllDocFee([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeReturnDto();
            var dbSet = _DbContext.DocFees;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            var normalizedConditional = conditional != null ?
                new Dictionary<string, string>(conditional.Where(c => !c.Key.Contains(".")), StringComparer.OrdinalIgnoreCase) :
                null;
            coll = QueryHelper.GenerateWhereAnd(coll, normalizedConditional);
            #region 验证权限
            var r = coll.AsEnumerable();
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.2"))
            {
                var currentCompany = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>().GetCurrentCompanyByUser(context.User);
                if (currentCompany == null)
                {
                    return result;
                }
                var orgIds = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>().GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                var userIds = _DbContext.AccountPlOrganizations.Where(c => orgIds.Contains(c.OrgId)).Select(c => c.UserId).Distinct().ToHashSet();
                var jobIds = r.Select(c => c.JobId).Distinct().ToHashSet();
                var jobDic = _DbContext.PlJobs
                    .Where(c => jobIds.Contains(c.Id))
                    .ToList()
                    .ToDictionary(c => c.Id);
                var d0Func = GetFunc("D0.6.2", ProjectContent.AeId);
                var d1Func = GetFunc("D1.6.2", ProjectContent.AiId);
                var d2Func = GetFunc("D2.6.2", ProjectContent.SeId);
                var d3Func = GetFunc("D3.6.2", ProjectContent.SiId);
                var d4Func = GetFunc("D4.6.2", ProjectContent.JeId);
                var d5Func = GetFunc("D5.6.2", ProjectContent.JiId);
                var d6Func = GetFunc("D6.6.2", ProjectContent.ReId);
                var d7Func = GetFunc("D7.6.2", ProjectContent.RiId);
                var d8Func = GetFunc("D8.6.2", ProjectContent.OtId);
                var d9Func = GetFunc("D9.6.2", ProjectContent.WhId);
                r = r.Where(c => d0Func(c) || d1Func(c) || d2Func(c) || d3Func(c) || d4Func(c)
                   || d5Func(c) || d6Func(c) || d7Func(c) || d8Func(c) || d9Func(c));
                #region 获取判断函数的本地函数
                Func<DocFee, bool> GetFunc(string prefix, Guid typeId)
                {
                    Func<DocFee, bool> result;
                    if (_AuthorizationManager.Demand(out err, $"{prefix}.3"))
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId;
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.2"))
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId != null
                            && userIds.Contains(jobDic[c.JobId.Value].OperatorId.Value);
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.1"))
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId == context.User.Id;
                    }
                    else
                        result = c => false;
                    return result;
                }
                #endregion 获取判断函数的本地函数
            }
            #endregion 验证权限
            var prb = _EntityManager.GetAll(r.AsQueryable(), model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 批量查询费用接口V2，支持多实体复合条件查询。注意设置权限。
        /// </summary>
        /// <param name="model">分页和排序参数</param>
        /// <param name="conditional">查询条件字典,键格式: 实体名.字段名。省略实体名则认为是DocFee的实体属性。
        /// </param>
        /// <returns>符合条件的费用实体列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpGet]
        public ActionResult<GetAllDocFeeV2ReturnDto> GetAllDocFeeV2(
            [FromQuery] GetAllDocFeeV2ParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllDocFeeV2ReturnDto();
            try
            {
                var docFeeConditional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var jobConditional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var billConditional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (conditional != null)
                {
                    foreach (var pair in conditional)
                    {
                        string key = pair.Key;
                        int dotIndex = key.IndexOf('.');
                        if (dotIndex > 0)
                        {
                            string entityName = key[..dotIndex].ToLowerInvariant();
                            string propertyName = key[(dotIndex + 1)..];
                            switch (entityName)
                            {
                                case "pljob":
                                    jobConditional[propertyName] = pair.Value;
                                    break;
                                case "docbill":
                                    billConditional[propertyName] = pair.Value;
                                    break;
                                case "docfee":
                                    docFeeConditional[propertyName] = pair.Value;
                                    break;
                                default:
                                    docFeeConditional[key] = pair.Value;
                                    break;
                            }
                        }
                        else
                        {
                            docFeeConditional[key] = pair.Value;
                        }
                    }
                }
                jobConditional["OrgId"] = context.User.OrgId.ToString();
                IQueryable<DocFee> feeQuery = _DbContext.DocFees.AsQueryable();
                if (jobConditional.Count > 0)
                {
                    var jobQuery = _DbContext.PlJobs.AsQueryable();
                    jobQuery = QueryHelper.GenerateWhereAnd(jobQuery, jobConditional);
                    var filteredJobIds = jobQuery.Select(j => j.Id).ToList();
                    if (filteredJobIds.Any())
                    {
                        feeQuery = feeQuery.Where(f => f.JobId.HasValue && filteredJobIds.Contains(f.JobId.Value));
                    }
                    else
                    {
                        feeQuery = feeQuery.Where(f => false);
                        result.Total = 0;
                        return result;
                    }
                }
                if (billConditional.Count > 0)
                {
                    var billQuery = _DbContext.DocBills.AsQueryable();
                    billQuery = QueryHelper.GenerateWhereAnd(billQuery, billConditional);
                    var filteredBillIds = billQuery.Select(b => b.Id).ToList();
                    if (filteredBillIds.Any())
                    {
                        feeQuery = feeQuery.Where(f => f.BillId.HasValue && filteredBillIds.Contains(f.BillId.Value));
                    }
                    else
                    {
                        feeQuery = feeQuery.Where(f => false);
                        result.Total = 0;
                        return result;
                    }
                }
                feeQuery = QueryHelper.GenerateWhereAnd(feeQuery, docFeeConditional);
                feeQuery = feeQuery.OrderBy(model.OrderFieldName, model.IsDesc);
                bool hasGeneralPermission = _AuthorizationManager.Demand(out string err, "F.2.4.2");
                if (!hasGeneralPermission)
                {
                    var currentCompany = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>().GetCurrentCompanyByUser(context.User);
                    if (currentCompany == null)
                    {
                        feeQuery = feeQuery.Where(f => false);
                        result.Total = 0;
                        return result;
                    }
                    var orgIds = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>().GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                    var userIds = _DbContext.AccountPlOrganizations
                        .Where(c => orgIds.Contains(c.OrgId))
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToList();
                    var accessibleJobIds = new HashSet<Guid>();
                    var relatedJobInfo = _DbContext.PlJobs
                        .Where(j => feeQuery.Any(f => f.JobId == j.Id))
                        .Select(j => new { j.Id, j.JobTypeId, j.OperatorId })
                        .ToList();
                    if (relatedJobInfo.Any())
                    {
                        CheckPermissions("D0.6.2", ProjectContent.AeId);
                        CheckPermissions("D1.6.2", ProjectContent.AiId);
                        CheckPermissions("D2.6.2", ProjectContent.SeId);
                        CheckPermissions("D3.6.2", ProjectContent.SiId);
                        CheckPermissions("D4.6.2", ProjectContent.JeId);
                        CheckPermissions("D5.6.2", ProjectContent.JiId);
                        CheckPermissions("D6.6.2", ProjectContent.ReId);
                        CheckPermissions("D7.6.2", ProjectContent.RiId);
                        CheckPermissions("D8.6.2", ProjectContent.OtId);
                        CheckPermissions("D9.6.2", ProjectContent.WhId);
                        void CheckPermissions(string prefix, Guid typeId)
                        {
                            var typeJobs = relatedJobInfo.Where(j => j.JobTypeId == typeId).ToList();
                            if (!typeJobs.Any()) return;
                            if (_AuthorizationManager.Demand(out err, $"{prefix}.3"))
                            {
                                foreach (var job in typeJobs)
                                {
                                    accessibleJobIds.Add(job.Id);
                                }
                            }
                            else if (_AuthorizationManager.Demand(out err, $"{prefix}.2"))
                            {
                                foreach (var job in typeJobs)
                                {
                                    if (job.OperatorId.HasValue && userIds.Contains(job.OperatorId.Value))
                                    {
                                        accessibleJobIds.Add(job.Id);
                                    }
                                }
                            }
                            else if (_AuthorizationManager.Demand(out err, $"{prefix}.1"))
                            {
                                foreach (var job in typeJobs)
                                {
                                    if (job.OperatorId == context.User.Id)
                                    {
                                        accessibleJobIds.Add(job.Id);
                                    }
                                }
                            }
                        }
                        if (accessibleJobIds.Any())
                        {
                            feeQuery = feeQuery.Where(f => f.JobId.HasValue && accessibleJobIds.Contains(f.JobId.Value));
                        }
                        else
                        {
                            feeQuery = feeQuery.Where(f => false);
                        }
                    }
                    else
                    {
                        feeQuery = feeQuery.Where(f => false);
                    }
                }
                result.Total = feeQuery.Count();
                result.Result = feeQuery
                    .Skip(model.StartIndex)
                    .Take(model.Count > 0 ? model.Count : int.MaxValue)
                    .ToList();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量查询费用V2时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"批量查询费用V2时发生错误: {ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// 按复杂的多表条件返回费用。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询条件字典,键格式: 实体名.字段名,如PlJob.JobNo表示工作对象的工作号。目前支持的实体有DocFee,DocBill,PlJob。
        /// 值的写法和一般条件一致。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetDocFeeReturnDto> GetDocFee([FromQuery] GetDocFeeParamsDto model, [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string[] entityNames = new string[] { nameof(DocFee), nameof(DocBill), nameof(PlJob) };
            var result = new GetDocFeeReturnDto();
            var insensitiveConditional = conditional != null ?
                new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var keyJob = insensitiveConditional.Where(c => c.Key.StartsWith(nameof(PlJob) + ".", StringComparison.OrdinalIgnoreCase));
            var dicJob = new Dictionary<string, string>(keyJob.Select(c => new KeyValuePair<string, string>(c.Key.Replace(nameof(PlJob) + ".", string.Empty, StringComparison.OrdinalIgnoreCase), c.Value)));
            var collJob = QueryHelper.GenerateWhereAnd(_DbContext.PlJobs.Where(c => c.OrgId == context.User.OrgId), dicJob);
            var keyBill = insensitiveConditional.Where(c => c.Key.StartsWith(nameof(DocBill) + ".", StringComparison.OrdinalIgnoreCase));
            var dicBill = new Dictionary<string, string>(keyBill.Select(c => new KeyValuePair<string, string>(c.Key.Replace(nameof(DocBill) + ".", string.Empty, StringComparison.OrdinalIgnoreCase), c.Value)));
            var collBill = QueryHelper.GenerateWhereAnd(_DbContext.DocBills, dicBill);
            var jobIds = collJob.Select(c => c.Id).ToArray();
            var keyDocFee = insensitiveConditional.Where(c => c.Key.StartsWith(nameof(DocFee) + ".", StringComparison.OrdinalIgnoreCase));
            var dicDocFee = new Dictionary<string, string>(keyDocFee.Select(c => new KeyValuePair<string, string>(c.Key.Replace(nameof(DocFee) + ".", string.Empty, StringComparison.OrdinalIgnoreCase), c.Value)));
            var docFeeQuery = _DbContext.DocFees.Where(c => jobIds.Contains(c.JobId.Value));
            if (insensitiveConditional.TryGetValue("balanceId", out var directBalanceIdStr) &&
                Guid.TryParse(directBalanceIdStr, out var directBalanceId))
            {
                docFeeQuery = docFeeQuery.Where(c => c.BalanceId == directBalanceId);
            }
            if (dicDocFee.TryGetValue("balanceId", out var prefixedBalanceIdStr) &&
                Guid.TryParse(prefixedBalanceIdStr, out var prefixedBalanceId))
            {
                docFeeQuery = docFeeQuery.Where(c => c.BalanceId == prefixedBalanceId);
                dicDocFee.Remove("balanceId");
            }
            var collDocFee = QueryHelper.GenerateWhereAnd(docFeeQuery, dicDocFee);
            var collBase =
                from fee in collDocFee
                join bill in collBill on fee.BillId equals bill.Id
                select fee;
            collBase = collBase.OrderBy(model.OrderFieldName, model.IsDesc);
            collBase = collBase.Skip(model.StartIndex);
            if (model.Count > -1)
                collBase = collBase.Take(model.Count);
            #region 验证权限
            var r = collBase.AsEnumerable();
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.2"))
            {
                var currentCompany = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>().GetCurrentCompanyByUser(context.User);
                if (currentCompany == null)
                {
                    return result;
                }
                var orgIds = _ServiceProvider.GetRequiredService<OrgManager<PowerLmsUserDbContext>>().GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                var userIds = _DbContext.AccountPlOrganizations.Where(c => orgIds.Contains(c.OrgId)).Select(c => c.UserId).Distinct().ToHashSet();
                var jobDic = _DbContext.PlJobs.Where(c => jobIds.Contains(c.Id)).AsEnumerable().ToDictionary(c => c.Id);
                var d0Func = GetFunc("D0.6.2", ProjectContent.AeId);
                var d1Func = GetFunc("D1.6.2", ProjectContent.AiId);
                var d2Func = GetFunc("D2.6.2", ProjectContent.SeId);
                var d3Func = GetFunc("D3.6.2", ProjectContent.SiId);
                var d4Func = GetFunc("D4.6.2", ProjectContent.JeId);
                var d5Func = GetFunc("D5.6.2", ProjectContent.JiId);
                var d6Func = GetFunc("D6.6.2", ProjectContent.ReId);
                var d7Func = GetFunc("D7.6.2", ProjectContent.RiId);
                var d8Func = GetFunc("D8.6.2", ProjectContent.OtId);
                var d9Func = GetFunc("D9.6.2", ProjectContent.WhId);
                r = r.Where(c => d0Func(c) || d1Func(c) || d2Func(c) || d3Func(c) || d4Func(c)
                   || d5Func(c) || d6Func(c) || d7Func(c) || d8Func(c) || d9Func(c));
                #region 获取判断函数的本地函数
                Func<DocFee, bool> GetFunc(string prefix, Guid typeId)
                {
                    Func<DocFee, bool> result;
                    if (_AuthorizationManager.Demand(out err, $"{prefix}.3"))
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId;
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.2"))
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId != null
                            && userIds.Contains(jobDic[c.JobId.Value].OperatorId.Value);
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.1"))
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId == context.User.Id;
                    }
                    else
                        result = c => false;
                    return result;
                }
                #endregion 获取判断函数的本地函数
            }
            #endregion 验证权限
            var ary = r.ToArray();
            result.Result.AddRange(ary);
            result.Total = ary.Length;
            return result;
        }

        /// <summary>
        /// 增加新业务单的费用单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddDocFeeReturnDto> AddDocFee(AddDocFeeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (_DbContext.PlJobs.Find(model.DocFee.JobId) is not PlJob job) return NotFound($"没有找到业务Id={model.DocFee.JobId}");
            #region 验证权限
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.1"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out string err2, "D0.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err2);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out string err3, "D1.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err3);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out string err4, "D2.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err4);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out string err5, "D3.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err5);
                }
            }
            #endregion 验证权限
            var result = new AddDocFeeReturnDto();
            var entity = model.DocFee;
            entity.GenerateNewId();
            _DbContext.DocFees.Add(model.DocFee);
            _DbContext.SaveChanges();
            result.Id = model.DocFee.Id;
            return result;
        }

        /// <summary>
        /// 修改业务单的费用单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的业务单的费用单不存在。</response>
        /// <response code="403">权限不足。</response>
        [HttpPut]
        public ActionResult<ModifyDocFeeReturnDto> ModifyDocFee(ModifyDocFeeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (_DbContext.PlJobs.Find(model.DocFee.JobId) is not PlJob job) return NotFound($"没有找到业务Id={model.DocFee.JobId}");
            #region 权限验证
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.3"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out string err2, "D0.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err2);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out string err3, "D1.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err3);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out string err4, "D2.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err4);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out string err5, "D3.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err5);
                }
            }
            #endregion 权限验证
            var result = new ModifyDocFeeReturnDto();
            var modifiedEntities = new List<DocFee>();
            if (!_EntityManager.Modify(new[] { model.DocFee }, modifiedEntities)) return NotFound();
            var entity = _DbContext.Entry(modifiedEntities[0]);
            entity.Property(c => c.AuditOperatorId).IsModified = false;
            entity.Property(c => c.AuditDateTime).IsModified = false;
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的业务单的费用单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的业务单的费用单不存在。</response>
        /// <response code="403">权限不足。</response>
        [HttpDelete]
        public ActionResult<RemoveDocFeeReturnDto> RemoveDocFee(RemoveDocFeeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (_DbContext.DocFees.Find(model.Id) is not DocFee docFee) return NotFound($"没有找到费用Id={model.Id}");
            if (_DbContext.PlJobs.Find(docFee.JobId) is not PlJob job) return NotFound($"没有找到业务Id={docFee.JobId}");
            #region 权限验证
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.4"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out string err2, "D0.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err2);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out string err3, "D1.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err3);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out string err4, "D2.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err4);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out string err5, "D3.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err5);
                }
            }
            #endregion 权限验证
            var result = new RemoveDocFeeReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocFees;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 费用管理
    }
}
