using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Linq.Expressions;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 业务相关功能控制器。
    /// </summary>
    public partial class PlJobController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlJobController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, OrgManager<PowerLmsUserDbContext> orgManager, IMapper mapper, EntityManager entityManager, DataDicManager dataManager, ILogger<PlJobController> logger, JobManager jobManager, AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger, BusinessLogicManager businessLogic)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrgManager = orgManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _DataManager = dataManager;
            _Logger = logger;
            _JobManager = jobManager;
            _AuthorizationManager = authorizationManager;
            _SqlAppLogger = sqlAppLogger;
            _BusinessLogic = businessLogic;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly DataDicManager _DataManager;
        readonly ILogger<PlJobController> _Logger;
        readonly JobManager _JobManager;
        readonly AuthorizationManager _AuthorizationManager;
        private readonly OwSqlAppLogger _SqlAppLogger;
        readonly BusinessLogicManager _BusinessLogic;

        #region 业务总表
        /// <summary>
        /// 获取全部业务总表。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlJobReturnDto> GetAllPlJob([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlJobReturnDto();

            try
            {
                var dbSet = _DbContext.PlJobs.Where(c => c.OrgId == context.User.OrgId);
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                coll = EfHelper.GenerateWhereAnd(coll, conditional);
                #region 业务表单关联过滤
                if (conditional != null)
                {
                    // 空运出口单条件过滤 - 使用范围运算符简化
                    var eaDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlEaDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key[(c.Key.IndexOf('.') + 1)..],
                            c => c.Value,
                            StringComparer.OrdinalIgnoreCase);

                    if (eaDocConditions.Any())
                    {
                        var eaDocQuery = _DbContext.PlEaDocs.AsNoTracking();
                        eaDocQuery = EfHelper.GenerateWhereAnd(eaDocQuery, eaDocConditions);
                        var eaDocJobIds = eaDocQuery.Select(doc => doc.JobId);
                        coll = coll.Where(job => eaDocJobIds.Contains(job.Id));
                    }

                    // 空运进口单条件过滤 - 使用范围运算符简化
                    var iaDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlIaDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key[(c.Key.IndexOf('.') + 1)..],
                            c => c.Value,
                            StringComparer.OrdinalIgnoreCase);

                    if (iaDocConditions.Any())
                    {
                        var iaDocQuery = _DbContext.PlIaDocs.AsNoTracking();
                        iaDocQuery = EfHelper.GenerateWhereAnd(iaDocQuery, iaDocConditions);
                        var iaDocJobIds = iaDocQuery.Select(doc => doc.JobId);
                        coll = coll.Where(job => iaDocJobIds.Contains(job.Id));
                    }

                    // 海运出口单条件过滤 - 使用范围运算符简化
                    var esDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlEsDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key[(c.Key.IndexOf('.') + 1)..],
                            c => c.Value,
                            StringComparer.OrdinalIgnoreCase);

                    if (esDocConditions.Any())
                    {
                        var esDocQuery = _DbContext.PlEsDocs.AsNoTracking();
                        esDocQuery = EfHelper.GenerateWhereAnd(esDocQuery, esDocConditions);
                        var esDocJobIds = esDocQuery.Select(doc => doc.JobId);
                        coll = coll.Where(job => esDocJobIds.Contains(job.Id));
                    }

                    // 海运进口单条件过滤 - 使用范围运算符简化
                    // 修复: 使用正确的类型名称 "PlIsDoc" 而不是 "PlIaDoc"
                    var isDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlIsDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key[(c.Key.IndexOf('.') + 1)..],
                            c => c.Value,
                            StringComparer.OrdinalIgnoreCase);

                    if (isDocConditions.Any())
                    {
                        var isDocQuery = _DbContext.PlIsDocs.AsNoTracking();
                        isDocQuery = EfHelper.GenerateWhereAnd(isDocQuery, isDocConditions);
                        var isDocJobIds = isDocQuery.Select(doc => doc.JobId);
                        coll = coll.Where(job => isDocJobIds.Contains(job.Id));
                    }
                }
                #endregion 业务表单关联过滤

                #region 权限判定
                string err;
                var r = coll.AsEnumerable();    //设计备注：如果结果集小则没问题；如果结果集大虽然这导致巨大内存消耗，但在此问题规模下，用内存替换cpu消耗是合理的置换代价
                if (!_AuthorizationManager.Demand(out err, "F.2"))  //若无通用查看权限
                {
                    var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                    if (currentCompany == null)
                    {
                        // 如果用户未登录任何公司，返回空结果
                        return result;
                    }
                    
                    var orgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                    var userIds = _DbContext.AccountPlOrganizations.Where(c => orgIds.Contains(c.OrgId)).Select(c => c.UserId).Distinct().ToHashSet();   //所有相关人Id集合
                    var d0Func = GetFunc("D0.1.1.1", ProjectContent.AeId);
                    var d1Func = GetFunc("D1.1.1.1", ProjectContent.AiId);
                    var d2Func = GetFunc("D2.1.1.1", ProjectContent.SeId);
                    var d3Func = GetFunc("D3.1.1.1", ProjectContent.SiId);
                    var d4Func = GetFunc("D4.1.1.1", ProjectContent.JeId);
                    var d5Func = GetFunc("D5.1.1.1", ProjectContent.JiId);
                    var d6Func = GetFunc("D6.1.1.1", ProjectContent.ReId);
                    var d7Func = GetFunc("D7.1.1.1", ProjectContent.RiId);
                    var d8Func = GetFunc("D8.1.1.1", ProjectContent.OtId);
                    var d9Func = GetFunc("D9.1.1.1", ProjectContent.WhId);
                    r = r.Where(c => d0Func(c) || d1Func(c) || d2Func(c) || d3Func(c) || d4Func(c)
                       || d5Func(c) || d6Func(c) || d7Func(c) || d8Func(c) || d9Func(c));
                    #region 获取判断函数的本地函数。
                    Func<PlJob, bool> GetFunc(string prefix, Guid typeId)
                    {
                        Func<PlJob, bool> result;
                        if (_AuthorizationManager.Demand(out err, $"{prefix}.3"))    //公司级别权限
                        {
                            result = c => c.JobTypeId == typeId;
                        }
                        else if (_AuthorizationManager.Demand(out err, $"{prefix}.2"))   //同组级别权限
                        {
                            result = c => c.JobTypeId == typeId && c.OperatorId != null && userIds.Contains(c.OperatorId.Value);
                        }
                        else if (_AuthorizationManager.Demand(out err, $"{prefix}.1"))   //本人级别权限
                        {
                            result = c => c.JobTypeId == typeId && c.OperatorId == context.User.Id;
                        }
                        else //此类无权限
                            result = c => false;
                        return result;
                    }
                    #endregion 获取判断函数的本地函数。
                }
                #endregion 权限判定

                // 从数据库中获取数据
                var prb = _EntityManager.GetAll(r.AsQueryable(), model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取业务总表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取业务总表时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 增加新业务总表。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="409">工作号重复。当手动指定工作号时，如果该工作号在同一机构内已存在。</response>  
        [HttpPost]
        public ActionResult<AddPlJobReturnDto> AddPlJob(AddPlJobParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            string err;
            if (model.PlJob.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.PlJob.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.PlJob.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D2.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.PlJob.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D3.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }

            var result = new AddPlJobReturnDto();
            var entity = model.PlJob;
            entity.GenerateNewId();
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            entity.JobState = 2;
            entity.OperatingDateTime = OwHelper.WorldNow;
            entity.OperatorId = context.User.Id;
            entity.OrgId = context.User.OrgId; // 确保设置机构ID

            // 🆕 工作号处理逻辑：支持手动录入 + 唯一性校验
            if (!string.IsNullOrWhiteSpace(entity.JobNo))
            {
                // 手动指定了工作号，需要验证唯一性
                var existingJob = _DbContext.PlJobs
                    .Where(j => j.OrgId == context.User.OrgId && j.JobNo == entity.JobNo)
                    .FirstOrDefault();

                if (existingJob != null)
                {
                    _Logger.LogWarning("工作号重复，机构ID: {OrgId}, 工作号: {JobNo}, 用户: {UserId}", 
                        context.User.OrgId, entity.JobNo, context.User.Id);
                    
                    result.HasError = true;
                    result.ErrorCode = 1001;
                    result.DebugMessage = $"工作号 '{entity.JobNo}' 在当前机构内已存在，请使用其他工作号";
                    
                    return Conflict(result);
                }

                _Logger.LogInformation("使用手动指定的工作号：{JobNo}，机构ID：{OrgId}，用户：{UserId}", 
                    entity.JobNo, context.User.OrgId, context.User.Id);
            }
            else
            {
                // 未指定工作号，使用自动生成（保持原有逻辑）
                try
                {
                    // 根据业务类型获取对应的工作号规则并生成工作号
                    var rules = _DbContext.DD_JobNumberRules
                        .Where(r => r.OrgId == context.User.OrgId && r.BusinessTypeId == entity.JobTypeId)
                        .ToList();

                    if (rules.Any())
                    {
                        var rule = rules.First(); // 取第一个匹配的规则
                        using var dw = DisposeHelper.Create(
                            (key, timeout) => SingletonLocker.TryEnter(key, timeout), 
                            key => SingletonLocker.Exit(key), 
                            rule.Id.ToString(), 
                            TimeSpan.FromSeconds(2)
                        );
                        
                        entity.JobNo = _JobManager.Generated(rule, context.User, OwHelper.WorldNow);
                        _Logger.LogInformation("自动生成工作号：{JobNo}，规则ID：{RuleId}，用户：{UserId}", 
                            entity.JobNo, rule.Id, context.User.Id);
                    }
                    else
                    {
                        // 如果没有找到规则，生成一个简单的工作号
                        entity.JobNo = $"JOB{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
                        _Logger.LogWarning("未找到工作号生成规则，使用默认格式：{JobNo}，机构ID：{OrgId}", 
                            entity.JobNo, context.User.OrgId);
                    }
                }
                catch (Exception ex)
                {
                    _Logger.LogError(ex, "自动生成工作号时发生错误，机构ID: {OrgId}", context.User.OrgId);
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = $"生成工作号时发生错误: {ex.Message}";
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }
            }

            try
            {
                _DbContext.PlJobs.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;
                
                _Logger.LogInformation("工作号创建成功：ID={JobId}, 工作号={JobNo}, 用户={UserId}", 
                    entity.Id, entity.JobNo, context.User.Id);
                
                return result;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_PlJobs_OrgId_JobNo") == true)
            {
                // 捕获数据库唯一性约束冲突（双重保险）
                _Logger.LogError(ex, "数据库唯一性约束冲突，工作号: {JobNo}", entity.JobNo);
                result.HasError = true;
                result.ErrorCode = 1001;
                result.DebugMessage = $"工作号 '{entity.JobNo}' 重复，请使用其他工作号";
                return Conflict(result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "保存工作号时发生错误，工作号: {JobNo}", entity.JobNo);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"保存工作号时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改业务总表信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务总表不存在。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="409">工作号重复。当修改工作号时，如果该工作号在同一机构内已被其他工作号使用。</response>  
        [HttpPut]
        public ActionResult<ModifyPlJobReturnDto> ModifyPlJob(ModifyPlJobParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlJobReturnDto();
            if (_DbContext.PlJobs.Find(model.PlJob.Id) is not PlJob ov) return NotFound();
            
            string err; //权限报错字符串
            if (model.PlJob.JobTypeId == ProjectContent.AeId)    //若是空运出口业务
            {
                if (model.PlJob.OperatorId != ov.OperatorId)    //若试图更换操作员
                    if (!_AuthorizationManager.Demand(out err, "D0.1.1.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);

                if (!_AuthorizationManager.Demand(out err, "D0.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.PlJob.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
            {
                if (model.PlJob.OperatorId != ov.OperatorId)    //若试图更换操作员
                    if (!_AuthorizationManager.Demand(out err, "D1.1.1.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);

                if (!_AuthorizationManager.Demand(out err, "D1.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.PlJob.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D2.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (model.PlJob.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D3.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }

            // 🆕 工作号唯一性校验（仅当工作号发生变更时）
            if (!string.IsNullOrWhiteSpace(model.PlJob.JobNo) && model.PlJob.JobNo != ov.JobNo)
            {
                var existingJob = _DbContext.PlJobs
                    .Where(j => j.OrgId == context.User.OrgId && 
                               j.JobNo == model.PlJob.JobNo && 
                               j.Id != model.PlJob.Id) // 排除当前工作号自身
                    .FirstOrDefault();

                if (existingJob != null)
                {
                    _Logger.LogWarning("尝试修改为重复的工作号，机构ID: {OrgId}, 工作号: {JobNo}, 当前工作ID: {JobId}, 冲突工作ID: {ConflictJobId}", 
                        context.User.OrgId, model.PlJob.JobNo, model.PlJob.Id, existingJob.Id);
                    
                    result.HasError = true;
                    result.ErrorCode = 1001;
                    result.DebugMessage = $"工作号 '{model.PlJob.JobNo}' 在当前机构内已存在，请使用其他工作号";
                    
                    return Conflict(result);
                }

                _Logger.LogInformation("工作号变更：从 '{OldJobNo}' 修改为 '{NewJobNo}'，工作ID：{JobId}", 
                    ov.JobNo, model.PlJob.JobNo, model.PlJob.Id);
            }

            if (ov.SalesId != model.PlJob.SalesId)
            {

            }
            try
            {
                if (!_EntityManager.Modify(new[] { model.PlJob })) return NotFound();
                
                //忽略不可更改字段
                var entity = _DbContext.Entry(model.PlJob);
                entity.Property(c => c.JobState).IsModified = false;
                entity.Property(c => c.AuditOperatorId).IsModified = false;
                entity.Property(c => c.AuditDateTime).IsModified = false;
                
                _DbContext.SaveChanges();

                _Logger.LogInformation("工作号修改成功：ID={JobId}, 工作号={JobNo}, 用户={UserId}", 
                    model.PlJob.Id, model.PlJob.JobNo, context.User.Id);

                return result;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_PlJobs_OrgId_JobNo") == true)
            {
                // 捕获数据库唯一性约束冲突（双重保险）
                _Logger.LogError(ex, "数据库唯一性约束冲突，工作号: {JobNo}, 工作ID: {JobId}", 
                    model.PlJob.JobNo, model.PlJob.Id);
                result.HasError = true;
                result.ErrorCode = 1001;
                result.DebugMessage = $"工作号 '{model.PlJob.JobNo}' 重复，请使用其他工作号";
                return Conflict(result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改工作号时发生错误，工作号: {JobNo}, 工作ID: {JobId}", 
                    model.PlJob.JobNo, model.PlJob.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改工作号时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的业务总表。
        /// </summary>
        /// <param name="model">包含要删除的业务ID</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">删除失败，可能原因：
        /// 工作号状态已超过操作阶段(JobState>2)
        /// 存在已审核费用
        /// 存在关联账单的费用</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的业务总表不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlJobReturnDto> RemovePlJob(RemovePlJobParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlJobReturnDto();
            var id = model.Id;

            // 检查是否存在该工作号
            var item = _DbContext.PlJobs.Find(id);
            if (item is null) return NotFound($"未找到ID为{id}的工作号");

            // 验证权限
            string err;
            if (item.JobTypeId == ProjectContent.AeId)    // 空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (item.JobTypeId == ProjectContent.AiId)    // 空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (item.JobTypeId == ProjectContent.SeId)    // 海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D2.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (item.JobTypeId == ProjectContent.SiId)    // 海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D3.1.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }

            // 获取BusinessLogicManager实例进行删除操作
            var businessLogicManager = _ServiceProvider.GetRequiredService<BusinessLogicManager>();

            try
            {
                // 执行删除操作
                if (!businessLogicManager.DeleteJob(id, _DbContext))
                {
                    // 删除失败，返回错误信息
                    return BadRequest(OwHelper.GetLastErrorMessage());
                }

                // 保存更改到数据库
                _DbContext.SaveChanges();

                // 删除成功
                return result;
            }
            catch (Exception ex)
            {
                // 捕获并记录任何可能的异常
                _Logger.LogError(ex, "删除工作号 {JobId} 时发生异常", id);
                return BadRequest($"删除工作号时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换业务/单据状态功能。支持业务主体状态和操作状态的双向切换。
        /// </summary>
        /// <param name="model">包含业务ID和要修改的状态参数</param>
        /// <returns>更新后的业务和操作状态信息</returns>
        /// <response code="200">操作成功。返回更新后的业务状态和操作状态。</response>
        /// <response code="400">请求无效。找不到业务单据对象或请求参数不符合规范。</response>
        /// <response code="401">未授权。提供的Token无效或已过期。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">未找到。指定ID的业务对象不存在。</response>
        /// <response code="500">服务器错误。执行状态变更过程中发生异常。</response>
        [HttpPost]
        public ActionResult<ChangeStateReturnDto> ChangeState(ChangeStateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }

            var result = new ChangeStateReturnDto();

            var changeResult = _BusinessLogic.ChangeJobAndDocState(
                model.JobId,
                model.JobState,
                model.OperateState,
                context.User.Id);

            if (changeResult == null)
            {
                return BadRequest(OwHelper.GetLastErrorMessage());
            }

            result.JobState = changeResult.Value.JobState;
            result.OperateState = changeResult.Value.OperateState;

            return result;
        }

        /// <summary>
        /// 审核任务及下属所有费用或取消审核工作任务并取消审核所有下属费用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">任务状态非法。要审核任务的 JobStata 必须是4时才能调用，成功后 JobStata 自动切换为8。
        /// 要取消审核任务的 JobStata 必须是8才能调用，成功后 JobStata 自动切换为4, 此时会取消下属费用的已审核状态。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">未找到指定的业务对象(Job) -或- 没有找到对应的业务单据。</response>  
        [HttpPost]
        public ActionResult<AuditJobAndDocFeeReturnDto> AuditJobAndDocFee(AuditJobAndDocFeeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AuditJobAndDocFeeReturnDto();
            if (_DbContext.PlJobs.Find(model.JobId) is not PlJob job) return NotFound($"未找到指定的任务 ，Id={model.JobId}");

            #region 验证权限
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.2.8"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (model.IsAudit && !_AuthorizationManager.Demand(out err, "D0.6.6")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    if (!model.IsAudit && !_AuthorizationManager.Demand(out err, "D0.6.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (model.IsAudit && !_AuthorizationManager.Demand(out err, "D1.6.6")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    if (!model.IsAudit && !_AuthorizationManager.Demand(out err, "D1.6.10")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (model.IsAudit && !_AuthorizationManager.Demand(out err, "D2.6.6")) return StatusCode((int)HttpStatusCode.Forbidden);
                    if (!model.IsAudit && !_AuthorizationManager.Demand(out err, "D2.6.10")) return StatusCode((int)HttpStatusCode.Forbidden);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (model.IsAudit && !_AuthorizationManager.Demand(out err, "D3.6.6")) return StatusCode((int)HttpStatusCode.Forbidden);
                    if (!model.IsAudit && !_AuthorizationManager.Demand(out err, "D3.6.10")) return StatusCode((int)HttpStatusCode.Forbidden);
                }
            }
            #endregion 验证权限
            var now = OwHelper.WorldNow;
            if (model.IsAudit)   //若审核
            {
                if (!_JobManager.Audit(job, context))
                    return BadRequest(OwHelper.GetLastErrorMessage());
            }
            else //取消审核
            {
                if (!_JobManager.UnAudit(job, context))
                    return BadRequest(OwHelper.GetLastErrorMessage());
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 复制任务对象。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<CopyJobReturnDto> CopyJob(CopyJobParamsDto model)
        {
            // 验证Token和获取上下文
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }

            var result = new CopyJobReturnDto();

            // 处理任务对象本体
            var srcJob = _DbContext.PlJobs.Find(model.SourceJobId);
            if (srcJob is null) return NotFound($"没找到指定任务对象，Id={model.SourceJobId}");

            // 权限验证 - 基于源工作号的业务类型进行权限检查
            string err;
            if (srcJob.JobTypeId == ProjectContent.AeId)    // 空运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D0.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (srcJob.JobTypeId == ProjectContent.AiId)    // 空运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D1.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (srcJob.JobTypeId == ProjectContent.SeId)    // 海运出口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D2.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }
            else if (srcJob.JobTypeId == ProjectContent.SiId)    // 海运进口业务
            {
                if (!_AuthorizationManager.Demand(out err, "D3.1.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            }

            var destJob = new PlJob();

            // 确保NewValues是不区分大小写的，并且将所有键转为标准格式
            var normalizedNewValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in model.NewValues)
            {
                // 规范化键名，确保符合命名规范
                var normalizedKey = pair.Key.Trim();
                normalizedNewValues[normalizedKey] = pair.Value;
            }

            // 获取Job对象的属性
            var jobProperties = normalizedNewValues
                .Where(c => !c.Key.Contains('.'))
                .ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

            if (!_EntityManager.CopyIgnoreCase(srcJob, destJob, jobProperties,
                model.IgnorePropertyNames.Where(c => !c.Contains('.'))))
            {
                return BadRequest($"无法复制新任务对象，Id={model.SourceJobId},错误：{OwHelper.GetLastErrorMessage()}");
            }
            
            // 强制设置新工作号的系统管理字段
            destJob.GenerateNewId();    // 强制Id不可重
            destJob.CreateBy = context.User.Id;    // 设置创建者
            destJob.CreateDateTime = OwHelper.WorldNow;    // 设置创建时间
            destJob.JobState = 2;    // 强制设置为初始状态（Operating正操作）
            destJob.OperatingDateTime = OwHelper.WorldNow;    // 设置操作时间
            destJob.OperatorId = context.User.Id;    // 设置操作人
            destJob.OrgId = context.User.OrgId;    // 设置所属机构
            destJob.AuditDateTime = null;    // 清空审核时间
            destJob.AuditOperatorId = null;    // 清空审核人

            // 处理业务单对象
            var tmpDoc = _JobManager.GetBusinessDoc(srcJob.Id, _DbContext);
            if (tmpDoc is null) return NotFound($"没找到业务单据，DocId={srcJob.Id}");

            switch (tmpDoc)
            {
                case PlEaDoc srcDoc:
                    {
                        var destDoc = new PlEaDoc();
                        var typeName = destDoc.GetType().Name;  // 类型名

                        var nVals = normalizedNewValues
                            .Where(c => c.Key.StartsWith($"{typeName}.", StringComparison.OrdinalIgnoreCase))
                            .ToDictionary(
                                c => c.Key[(typeName.Length + 1)..],
                                c => c.Value,
                                StringComparer.OrdinalIgnoreCase);

                        var ignorePNames = model.IgnorePropertyNames
                            .Where(c => c.IndexOf($"{typeName}.", StringComparison.OrdinalIgnoreCase) == 0)
                            .Select(c => c[(typeName.Length + 1)..]);

                        if (!_EntityManager.CopyIgnoreCase(srcDoc, destDoc, nVals, ignorePNames))
                        {
                            return BadRequest($"无法复制新任务对象，Id={model.SourceJobId},错误：{OwHelper.GetLastErrorMessage()}");
                        }
                        destDoc.GenerateNewId();
                        destDoc.JobId = destJob.Id;
                        destDoc.CreateBy = context.User.Id;    // 设置创建者
                        destDoc.CreateDateTime = OwHelper.WorldNow;    // 设置创建时间
                        destDoc.Status = 0;    // 重置为初始状态
                        _DbContext.Add(destDoc);
                    }
                    break;
                case PlIaDoc srcDoc:
                    {
                        var destDoc = new PlIaDoc();
                        var typeName = destDoc.GetType().Name;  // 类型名

                        var nVals = normalizedNewValues
                            .Where(c => c.Key.StartsWith($"{typeName}.", StringComparison.OrdinalIgnoreCase))
                            .ToDictionary(
                                c => c.Key[(typeName.Length + 1)..],
                                c => c.Value,
                                StringComparer.OrdinalIgnoreCase);

                        var ignorePNames = model.IgnorePropertyNames
                            .Where(c => c.IndexOf($"{typeName}.", StringComparison.OrdinalIgnoreCase) == 0)
                            .Select(c => c[(typeName.Length + 1)..]);

                        if (!_EntityManager.CopyIgnoreCase(srcDoc, destDoc, nVals, ignorePNames))
                        {
                            return BadRequest($"无法复制新任务对象，Id={model.SourceJobId},错误：{OwHelper.GetLastErrorMessage()}");
                        }
                        destDoc.GenerateNewId();
                        destDoc.JobId = destJob.Id;
                        destDoc.CreateBy = context.User.Id;    // 设置创建者
                        destDoc.CreateDateTime = OwHelper.WorldNow;    // 设置创建时间
                        destDoc.Status = 0;    // 重置为初始状态
                        _DbContext.Add(destDoc);
                    }
                    break;
                case PlEsDoc srcDoc:
                    {
                        var destDoc = new PlEsDoc();
                        var typeName = destDoc.GetType().Name;  // 类型名

                        var nVals = normalizedNewValues
                            .Where(c => c.Key.StartsWith($"{typeName}.", StringComparison.OrdinalIgnoreCase))
                            .ToDictionary(
                                c => c.Key[(typeName.Length + 1)..],
                                c => c.Value,
                                StringComparer.OrdinalIgnoreCase);

                        var ignorePNames = model.IgnorePropertyNames
                            .Where(c => c.IndexOf($"{typeName}.", StringComparison.OrdinalIgnoreCase) == 0)
                            .Select(c => c[(typeName.Length + 1)..]);

                        if (!_EntityManager.CopyIgnoreCase(srcDoc, destDoc, nVals, ignorePNames))
                        {
                            return BadRequest($"无法复制新任务对象，Id={model.SourceJobId},错误：{OwHelper.GetLastErrorMessage()}");
                        }
                        destDoc.GenerateNewId();
                        destDoc.JobId = destJob.Id;
                        destDoc.CreateBy = context.User.Id;    // 设置创建者
                        destDoc.CreateDateTime = OwHelper.WorldNow;    // 设置创建时间
                        destDoc.Status = 0;    // 重置为初始状态
                        _DbContext.Add(destDoc);
                    }
                    break;
                case PlIsDoc srcDoc:
                    {
                        var destDoc = new PlIsDoc();
                        var typeName = destDoc.GetType().Name;  // 类型名

                        var nVals = normalizedNewValues
                            .Where(c => c.Key.StartsWith($"{typeName}.", StringComparison.OrdinalIgnoreCase))
                            .ToDictionary(
                                c => c.Key[(typeName.Length + 1)..],
                                c => c.Value,
                                StringComparer.OrdinalIgnoreCase);

                        var ignorePNames = model.IgnorePropertyNames
                            .Where(c => c.IndexOf($"{typeName}.", StringComparison.OrdinalIgnoreCase) == 0)
                            .Select(c => c[(typeName.Length + 1)..]);

                        if (!_EntityManager.CopyIgnoreCase(srcDoc, destDoc, nVals, ignorePNames))
                        {
                            return BadRequest($"无法复制新任务对象，Id={model.SourceJobId},错误：{OwHelper.GetLastErrorMessage()}");
                        }
                        destDoc.GenerateNewId();
                        destDoc.JobId = destJob.Id;
                        destDoc.CreateBy = context.User.Id;    // 设置创建者
                        destDoc.CreateDateTime = OwHelper.WorldNow;    // 设置创建时间
                        destDoc.Status = 0;    // 重置为初始状态
                        _DbContext.Add(destDoc);
                    }
                    break;
                default:
                    return BadRequest($"不认识的业务单类型，Type={tmpDoc.GetType()}");
            }

            // 处理附属费用对象
            var fees = _DbContext.DocFees.Where(c => c.JobId == srcJob.Id);
            var ignFeeName = nameof(DocFee);

            // 创建不区分大小写的忽略属性集合
            var ignFees = new HashSet<string>(
                model.IgnorePropertyNames
                    .Where(c => c.StartsWith($"{ignFeeName}.", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c[(ignFeeName.Length + 1)..]),
                StringComparer.OrdinalIgnoreCase);

            try
            {
                // 提取与DocFee相关的新值，使用不区分大小写的比较
                var feeNewValues = normalizedNewValues
                    .Where(c => c.Key.StartsWith($"{ignFeeName}.", StringComparison.OrdinalIgnoreCase) ||
                                c.Key.StartsWith("docFee.", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        c => c.Key[(c.Key.IndexOf('.') + 1)..],
                        c => c.Value,
                        StringComparer.OrdinalIgnoreCase);

                foreach (var item in fees)
                {
                    var fee = new DocFee();

                    // 使用CopyIgnoreCase传递ignFees和feeNewValues
                    if (!_EntityManager.CopyIgnoreCase(item, fee, feeNewValues, ignFees))
                    {
                        _Logger.LogWarning("复制费用时遇到问题: {FeeId}", item.Id);
                    }
                    fee.GenerateNewId();
                    fee.JobId = destJob.Id;
                    fee.CreateBy = context.User.Id;    // 设置创建者
                    fee.CreateDateTime = OwHelper.WorldNow;    // 设置创建时间
                    fee.AuditDateTime = null;    // 清空审核时间
                    fee.AuditOperatorId = null;    // 清空审核人

                    _DbContext.Add(fee);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "复制费用时发生异常");
                return BadRequest($"复制费用时发生异常: {ex.Message}");
            }

            // 后处理
            _DbContext.Add(destJob);
            _DbContext.SaveChanges();
            result.Result = destJob.Id; // 设置返回结果的Id
            return result;
        }

        /// <summary>
        /// 验证工作号是否在当前机构内唯一。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">验证完成。通过Result属性查看是否唯一。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<ValidateJobNoReturnDto> ValidateJobNo([FromQuery] ValidateJobNoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("工作号验证：无效的令牌{token}", model.Token);
                return Unauthorized();
            }

            var result = new ValidateJobNoReturnDto();

            // 检查参数有效性
            if (string.IsNullOrWhiteSpace(model.JobNo))
            {
                result.IsUnique = true; // 空工作号视为有效（将使用自动生成）
                result.Message = "工作号为空，将使用自动生成";
                return result;
            }

            try
            {
                // 查询是否存在重复的工作号
                var query = _DbContext.PlJobs
                    .Where(j => j.OrgId == context.User.OrgId && j.JobNo == model.JobNo);

                // 如果是编辑现有工作号，排除自身
                if (model.ExcludeJobId.HasValue)
                {
                    query = query.Where(j => j.Id != model.ExcludeJobId.Value);
                }

                var existingJob = query.FirstOrDefault();

                if (existingJob != null)
                {
                    result.IsUnique = false;
                    result.Message = $"工作号 '{model.JobNo}' 已存在";
                    result.ConflictJobId = existingJob.Id;
                    
                    _Logger.LogDebug("工作号重复检测：'{JobNo}' 已被工作ID {ConflictJobId} 使用", 
                        model.JobNo, existingJob.Id);
                }
                else
                {
                    result.IsUnique = true;
                    result.Message = $"工作号 '{model.JobNo}' 可以使用";
                    
                    _Logger.LogDebug("工作号唯一性验证通过：'{JobNo}'", model.JobNo);
                }

                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "验证工作号唯一性时发生错误，工作号: {JobNo}", model.JobNo);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"验证工作号时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 业务总表

    }

}
