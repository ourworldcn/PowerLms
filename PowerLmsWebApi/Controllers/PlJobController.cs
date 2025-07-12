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
        JobManager _JobManager;
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
                    // 空运出口单条件过滤 - 使用不区分大小写的比较
                    var eaDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlEaDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key.Substring(c.Key.IndexOf('.') + 1),
                            c => c.Value,
                            StringComparer.OrdinalIgnoreCase);

                    if (eaDocConditions.Any())
                    {
                        var eaDocQuery = _DbContext.PlEaDocs.AsNoTracking();
                        eaDocQuery = EfHelper.GenerateWhereAnd(eaDocQuery, eaDocConditions);
                        var eaDocJobIds = eaDocQuery.Select(doc => doc.JobId);
                        coll = coll.Where(job => eaDocJobIds.Contains(job.Id));
                    }

                    // 空运进口单条件过滤 - 使用不区分大小写的比较
                    var iaDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlIaDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key.Substring(c.Key.IndexOf('.') + 1),
                            c => c.Value,
                            StringComparer.OrdinalIgnoreCase);

                    if (iaDocConditions.Any())
                    {
                        var iaDocQuery = _DbContext.PlIaDocs.AsNoTracking();
                        iaDocQuery = EfHelper.GenerateWhereAnd(iaDocQuery, iaDocConditions);
                        var iaDocJobIds = iaDocQuery.Select(doc => doc.JobId);
                        coll = coll.Where(job => iaDocJobIds.Contains(job.Id));
                    }

                    // 海运出口单条件过滤 - 使用不区分大小写的比较
                    var esDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlEsDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key.Substring(c.Key.IndexOf('.') + 1),
                            c => c.Value,
                            StringComparer.OrdinalIgnoreCase);

                    if (esDocConditions.Any())
                    {
                        var esDocQuery = _DbContext.PlEsDocs.AsNoTracking();
                        esDocQuery = EfHelper.GenerateWhereAnd(esDocQuery, esDocConditions);
                        var esDocJobIds = esDocQuery.Select(doc => doc.JobId);
                        coll = coll.Where(job => esDocJobIds.Contains(job.Id));
                    }

                    // 海运进口单条件过滤 - 使用不区分大小写的比较
                    // 修复: 使用正确的类型名称 "PlIsDoc" 而不是 "PlIaDoc"
                    var isDocConditions = conditional
                        .Where(c => c.Key.StartsWith("PlIsDoc.", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(
                            c => c.Key.Substring(c.Key.IndexOf('.') + 1),
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
            _DbContext.PlJobs.Add(model.PlJob);
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            entity.JobState = 2;
            entity.OperatingDateTime = OwHelper.WorldNow;
            entity.OperatorId = context.User.Id;
            _DbContext.SaveChanges();
            result.Id = model.PlJob.Id;
            return result;
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
            if (ov.SalesId != model.PlJob.SalesId)
            {

            }
            if (!_EntityManager.Modify(new[] { model.PlJob })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.PlJob);
            entity.Property(c => c.JobState).IsModified = false;
            entity.Property(c => c.AuditOperatorId).IsModified = false;
            entity.Property(c => c.AuditDateTime).IsModified = false;
            //model.PlJob.OperatingDateTime = OwHelper.WorldNow;
            //model.PlJob.OperatorId = context.User.Id;
            _DbContext.SaveChanges();

            return result;
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
        /// 要取消审核任务的 JobStata 必须是8才能调用，成功后 JobStata 自动切换为4,此时会取消下属费用的已审核状态。</response>  
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
            var result = new CopyJobReturnDto();

            //处理任务对象本体
            var srcJob = _DbContext.PlJobs.Find(model.SourceJobId);
            if (srcJob is null) return NotFound($"没找到指定任务对象，Id={model.SourceJobId}");
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
            destJob.GenerateNewId();    //强制Id不可重

            //处理业务单对象
            var tmpDoc = _JobManager.GetBusinessDoc(srcJob.Id, _DbContext);
            if (tmpDoc is null) return NotFound($"没找到业务单据，DocId={srcJob.Id}");

            switch (tmpDoc)
            {
                case PlEaDoc srcDoc:
                    {
                        var destDoc = new PlEaDoc();
                        var typeName = destDoc.GetType().Name;  //类型名

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
                        _DbContext.Add(destDoc);
                    }
                    break;
                case PlIaDoc srcDoc:
                    {
                        var destDoc = new PlIaDoc();
                        var typeName = destDoc.GetType().Name;  //类型名

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
                        _DbContext.Add(destDoc);
                    }
                    break;
                case PlEsDoc srcDoc:
                    {
                        var destDoc = new PlEsDoc();
                        var typeName = destDoc.GetType().Name;  //类型名

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
                        _DbContext.Add(destDoc);
                    }
                    break;
                case PlIsDoc srcDoc:
                    {
                        var destDoc = new PlIsDoc();
                        var typeName = destDoc.GetType().Name;  //类型名

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
                        _DbContext.Add(destDoc);
                    }
                    break;
                default:
                    return BadRequest($"不认识的业务单类型，Type={tmpDoc.GetType()}");
            }

            //处理附属费用对象
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
                        c => c.Key.Substring(c.Key.IndexOf('.') + 1),
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

                    _DbContext.Add(fee);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "复制费用时发生异常");
                return BadRequest($"复制费用时发生异常: {ex.Message}");
            }

            //后处理
            _DbContext.Add(destJob);
            _DbContext.SaveChanges();
            result.Result = destJob.Id; // 设置返回结果的Id
            return result;
        }

        #endregion 业务总表

        #region 空运出口单

        /// <summary>
        /// 获取全部空运出口单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlEaDocReturnDto> GetAllPlEaDoc([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlEaDocReturnDto();

            var dbSet = _DbContext.PlEaDocs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新空运出口单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlEaDocReturnDto> AddPlEaDoc(AddPlEaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "D0.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlEaDocReturnDto();
            var entity = model.PlEaDoc;
            entity.GenerateNewId();
            _DbContext.PlEaDocs.Add(model.PlEaDoc);
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            _DbContext.SaveChanges();
            result.Id = model.PlEaDoc.Id;
            return result;
        }

        /// <summary>
        /// 修改空运出口单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的空运出口单不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlEaDocReturnDto> ModifyPlEaDoc(ModifyPlEaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlEaDocReturnDto();
            var doc = _DbContext.PlEaDocs.Find(model.PlEaDoc.Id);
            if (doc == null) return NotFound("指定Id的空运出口单不存在");
            string err;
            if (!_AuthorizationManager.Demand(out err, "D0.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            if (!_EntityManager.Modify(new[] { model.PlEaDoc })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的空运出口单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的空运出口单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlEaDocReturnDto> RemovePlEaDoc(RemovePlEaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlEaDocReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlEaDocs;
            var item = dbSet.Find(id);
            //if (item.JobState > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 空运出口单

        #region 货场出重单

        /// <summary>
        /// 获取全部货场出重单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 Id，EaDocId(EA单Id)。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllHuochangChuchongReturnDto> GetAllHuochangChuchong([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllHuochangChuchongReturnDto();

            var dbSet = _DbContext.HuochangChuchongs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "EaDocId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.EaDocId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新货场出重单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddHuochangChuchongReturnDto> AddHuochangChuchong(AddHuochangChuchongParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddHuochangChuchongReturnDto();
            var entity = model.HuochangChuchong;
            entity.GenerateNewId();
            _DbContext.HuochangChuchongs.Add(model.HuochangChuchong);
            _DbContext.SaveChanges();
            result.Id = model.HuochangChuchong.Id;
            return result;
        }

        /// <summary>
        /// 修改货场出重单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的货场出重单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyHuochangChuchongReturnDto> ModifyHuochangChuchong(ModifyHuochangChuchongParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyHuochangChuchongReturnDto();
            if (!_EntityManager.Modify(new[] { model.HuochangChuchong })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的货场出重单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的货场出重单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveHuochangChuchongReturnDto> RemoveHuochangChuchong(RemoveHuochangChuchongParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveHuochangChuchongReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.HuochangChuchongs;
            var item = dbSet.Find(id);
            //if (item.JobState > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 货场出重单

        #region 业务单的费用单

        /// <summary>
        /// 审核或取消审核单笔费用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">费用已被审核 -或- 所属任务已不可更改。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">找不到指定Id的费用。</response>  
        [HttpPost]
        public ActionResult<AuditDocFeeReturnDto> AuditDocFee(AuditDocFeeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            var result = new AuditDocFeeReturnDto();
            if (_DbContext.DocFees.Find(model.FeeId) is not DocFee fee) return NotFound();
            if (_DbContext.PlJobs.Find(fee.JobId) is not PlJob job) return NotFound();

            #region 验证权限
            string err;
            if (!(model.IsAudit && _AuthorizationManager.Demand(out err, "F.2.4.5") || !model.IsAudit && _AuthorizationManager.Demand(out err, "F.2.4.6")))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D0.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D1.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D2.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D3.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
            }
            #endregion 验证权限

            if (job.JobState > 4) return BadRequest("所属任务已经不可更改。");
            if (model.IsAudit)
            {
                fee.AuditDateTime = OwHelper.WorldNow;
                fee.AuditOperatorId = context.User.Id;
            }
            else
            {
                fee.AuditDateTime = null;
                fee.AuditOperatorId = null;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取全部业务单的费用单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeReturnDto> GetAllDocFee([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            //if (!_AuthorizationManager.HasPermission(context.User, "D0.6.2")) return StatusCode((int)HttpStatusCode.Forbidden);
            var result = new GetAllDocFeeReturnDto();

            var dbSet = _DbContext.DocFees;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // 确保条件字典不区分大小写
            var normalizedConditional = conditional != null ?
                new Dictionary<string, string>(conditional.Where(c => !c.Key.Contains(".")), StringComparer.OrdinalIgnoreCase) :
                null;

            // 应用其他条件
            coll = EfHelper.GenerateWhereAnd(coll, normalizedConditional);

            #region 验证权限
            string err;
            var r = coll.AsEnumerable();
            if (!_AuthorizationManager.Demand(out err, "F.2.4.2"))  //若无通用查看权限
            {
                var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                if (currentCompany == null)
                {
                    return result;
                }
                
                var orgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                var userIds = _DbContext.AccountPlOrganizations.Where(c => orgIds.Contains(c.OrgId)).Select(c => c.UserId).Distinct().ToHashSet();   //所有相关人Id集合
                var jobIds = r.Select(c => c.JobId).Distinct().ToHashSet();
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
                    if (_AuthorizationManager.Demand(out err, $"{prefix}.3"))    //公司级别权限
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId;
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.2"))   //同组级别权限
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId != null
                            && userIds.Contains(jobDic[c.JobId.Value].OperatorId.Value);
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.1"))   //本人级别权限
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId == context.User.Id;
                    }
                    else //此类无权限
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
        /// <param name="conditional">通用查询条件字典。键是实体名.实体字段名，值是条件值。省略实体名则认为是 DocFee的实体属性。
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
                // 条件字典初始化（不区分大小写）
                var docFeeConditional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var jobConditional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var billConditional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // 一次遍历完成所有条件解析
                if (conditional != null)
                {
                    foreach (var pair in conditional)
                    {
                        string key = pair.Key;
                        int dotIndex = key.IndexOf('.');

                        if (dotIndex > 0)
                        {
                            // 有实体前缀的条件
                            string entityName = key.Substring(0, dotIndex).ToLowerInvariant();
                            string propertyName = key.Substring(dotIndex + 1);

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
                                    // 未知实体前缀，作为DocFee条件处理
                                    docFeeConditional[key] = pair.Value;
                                    break;
                            }
                        }
                        else
                        {
                            // 无实体前缀的条件默认为DocFee属性
                            docFeeConditional[key] = pair.Value;
                        }
                    }
                }

                // 添加组织ID限制（安全检查）
                jobConditional["OrgId"] = context.User.OrgId.ToString();

                // 构建主查询（最初不执行，仅构建表达式）
                IQueryable<DocFee> feeQuery = _DbContext.DocFees.AsQueryable();

                // 如果有工作任务条件，应用关联
                if (jobConditional.Count > 0)
                {
                    var jobQuery = _DbContext.PlJobs.AsQueryable();
                    jobQuery = EfHelper.GenerateWhereAnd(jobQuery, jobConditional);

                    // 获取符合条件的工作ID
                    var filteredJobIds = jobQuery.Select(j => j.Id).ToList();
                    if (filteredJobIds.Any())
                    {
                        feeQuery = feeQuery.Where(f => f.JobId.HasValue && filteredJobIds.Contains(f.JobId.Value));
                    }
                    else
                    {
                        // 没有符合条件的工作，返回空结果
                        feeQuery = feeQuery.Where(f => false);
                        result.Total = 0;
                        return result;
                    }
                }

                // 如果有账单条件，应用关联
                if (billConditional.Count > 0)
                {
                    var billQuery = _DbContext.DocBills.AsQueryable();
                    billQuery = EfHelper.GenerateWhereAnd(billQuery, billConditional);

                    // 获取符合条件的账单ID
                    var filteredBillIds = billQuery.Select(b => b.Id).ToList();
                    if (filteredBillIds.Any())
                    {
                        feeQuery = feeQuery.Where(f => f.BillId.HasValue && filteredBillIds.Contains(f.BillId.Value));
                    }
                    else
                    {
                        // 没有符合条件的账单，返回空结果
                        feeQuery = feeQuery.Where(f => false);
                        result.Total = 0;
                        return result;
                    }
                }

                // 应用DocFee自身的条件过滤
                feeQuery = EfHelper.GenerateWhereAnd(feeQuery, docFeeConditional);

                // 应用排序
                feeQuery = feeQuery.OrderBy(model.OrderFieldName, model.IsDesc);

                // 权限检查
                string err;
                bool hasGeneralPermission = _AuthorizationManager.Demand(out err, "F.2.4.2");

                if (!hasGeneralPermission)
                {
                    // 获取用户所在组织的所有用户ID
                    var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                    if (currentCompany == null)
                    {
                        feeQuery = feeQuery.Where(f => false);
                        result.Total = 0;
                        return result;
                    }
                    
                    var orgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                    var userIds = _DbContext.AccountPlOrganizations
                        .Where(c => orgIds.Contains(c.OrgId))
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToList();

                    // 预先加载相关的工作任务
                    var accessibleJobIds = new HashSet<Guid>();
                    var relatedJobInfo = _DbContext.PlJobs
                        .Where(j => feeQuery.Any(f => f.JobId == j.Id))
                        .Select(j => new { j.Id, j.JobTypeId, j.OperatorId })
                        .ToList();

                    if (relatedJobInfo.Any())
                    {
                        // 检查各业务类型的权限
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

                        // 本地函数：检查权限
                        void CheckPermissions(string prefix, Guid typeId)
                        {
                            var typeJobs = relatedJobInfo.Where(j => j.JobTypeId == typeId).ToList();
                            if (!typeJobs.Any()) return;

                            if (_AuthorizationManager.Demand(out err, $"{prefix}.3")) // 公司级别权限
                            {
                                foreach (var job in typeJobs)
                                {
                                    accessibleJobIds.Add(job.Id);
                                }
                            }
                            else if (_AuthorizationManager.Demand(out err, $"{prefix}.2")) // 同组级别权限
                            {
                                foreach (var job in typeJobs)
                                {
                                    if (job.OperatorId.HasValue && userIds.Contains(job.OperatorId.Value))
                                    {
                                        accessibleJobIds.Add(job.Id);
                                    }
                                }
                            }
                            else if (_AuthorizationManager.Demand(out err, $"{prefix}.1")) // 本人级别权限
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

                        // 应用权限过滤
                        if (accessibleJobIds.Any())
                        {
                            feeQuery = feeQuery.Where(f => f.JobId.HasValue && accessibleJobIds.Contains(f.JobId.Value));
                        }
                        else
                        {
                            // 没有权限访问任何记录
                            feeQuery = feeQuery.Where(f => false);
                        }
                    }
                    else
                    {
                        // 没有相关工作任务，返回空结果
                        feeQuery = feeQuery.Where(f => false);
                    }
                }

                // 获取总数（必须在应用分页前进行）
                result.Total = feeQuery.Count();

                // 应用分页并执行查询
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

        // 参数替换类（用于表达式树合并）
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return ReferenceEquals(node, _oldParameter) ? _newParameter : base.VisitParameter(node);
            }
        }

        /// <summary>
        /// 按复杂的多表条件返回费用。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持的查询条件字典，键的写法须在前加实体名用.分隔，如 PlJob.JobNo 表示工作对象的工作号；目前支持的实体有DocFee,DocBill,PlJob。
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

            // 将条件字典转换为不区分大小写的字典
            var insensitiveConditional = conditional != null ?
                new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase) :
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var keyJob = insensitiveConditional.Where(c => c.Key.StartsWith(nameof(PlJob) + ".", StringComparison.OrdinalIgnoreCase));
            var dicJob = new Dictionary<string, string>(keyJob.Select(c => new KeyValuePair<string, string>(c.Key.Replace(nameof(PlJob) + ".", string.Empty, StringComparison.OrdinalIgnoreCase), c.Value)));
            var collJob = EfHelper.GenerateWhereAnd(_DbContext.PlJobs.Where(c => c.OrgId == context.User.OrgId), dicJob);

            var keyBill = insensitiveConditional.Where(c => c.Key.StartsWith(nameof(DocBill) + ".", StringComparison.OrdinalIgnoreCase));
            var dicBill = new Dictionary<string, string>(keyBill.Select(c => new KeyValuePair<string, string>(c.Key.Replace(nameof(DocBill) + ".", string.Empty, StringComparison.OrdinalIgnoreCase), c.Value)));
            var collBill = EfHelper.GenerateWhereAnd(_DbContext.DocBills, dicBill);

            var jobIds = collJob.Select(c => c.Id).ToArray();
            var keyDocFee = insensitiveConditional.Where(c => c.Key.StartsWith(nameof(DocFee) + ".", StringComparison.OrdinalIgnoreCase));
            var dicDocFee = new Dictionary<string, string>(keyDocFee.Select(c => new KeyValuePair<string, string>(c.Key.Replace(nameof(DocFee) + ".", string.Empty, StringComparison.OrdinalIgnoreCase), c.Value)));

            // 处理特殊情况: balanceId 参数
            var docFeeQuery = _DbContext.DocFees.Where(c => jobIds.Contains(c.JobId.Value));

            // 检查是否有 balanceId 直接条件 (不带前缀的)
            if (insensitiveConditional.TryGetValue("balanceId", out var directBalanceIdStr) &&
                Guid.TryParse(directBalanceIdStr, out var directBalanceId))
            {
                // 应用 balanceId 过滤
                docFeeQuery = docFeeQuery.Where(c => c.BalanceId == directBalanceId);
            }

            // 检查是否有 DocFee.balanceId 前缀形式的条件
            if (dicDocFee.TryGetValue("balanceId", out var prefixedBalanceIdStr) &&
                Guid.TryParse(prefixedBalanceIdStr, out var prefixedBalanceId))
            {
                // 应用 DocFee.balanceId 过滤
                docFeeQuery = docFeeQuery.Where(c => c.BalanceId == prefixedBalanceId);
                // 从条件中移除已处理的条件
                dicDocFee.Remove("balanceId");
            }

            // 应用其他 DocFee 条件
            var collDocFee = EfHelper.GenerateWhereAnd(docFeeQuery, dicDocFee);

            var collBase =
                from fee in collDocFee
                join bill in collBill on fee.BillId equals bill.Id
                select fee;

            collBase = collBase.OrderBy(model.OrderFieldName, model.IsDesc);
            collBase = collBase.Skip(model.StartIndex);
            if (model.Count > -1)
                collBase = collBase.Take(model.Count);
            #region 验证权限
            string err;
            var r = collBase.AsEnumerable();
            if (!_AuthorizationManager.Demand(out err, "F.2.4.2"))  //若无通用查看权限
            {
                var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                if (currentCompany == null)
                {
                    return result;
                }
                
                var orgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                var userIds = _DbContext.AccountPlOrganizations.Where(c => orgIds.Contains(c.OrgId)).Select(c => c.UserId).Distinct().ToHashSet();   //所有相关人Id集合
                                                                                                                                                     //var jobIds = r.Select(c => c.JobId).Distinct().ToHashSet();
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
                    if (_AuthorizationManager.Demand(out err, $"{prefix}.3"))    //公司级别权限
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId;
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.2"))   //同组级别权限
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId != null
                            && userIds.Contains(jobDic[c.JobId.Value].OperatorId.Value);
                    }
                    else if (_AuthorizationManager.Demand(out err, $"{prefix}.1"))   //本人级别权限
                    {
                        result = c => jobDic[c.JobId.Value].JobTypeId == typeId && jobDic[c.JobId.Value].OperatorId == context.User.Id;
                    }
                    else //此类无权限
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
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.2.4.1"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D0.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
                {
                    if (!_AuthorizationManager.Demand(out err, "D1.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
                {
                    if (!_AuthorizationManager.Demand(out err, "D2.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
                {
                    if (!_AuthorizationManager.Demand(out err, "D3.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
            }
            #endregion 验证权限

            var result = new AddDocFeeReturnDto();
            var entity = model.DocFee;
            entity.GenerateNewId();
            _DbContext.DocFees.Add(model.DocFee);
            //model.DocFee.BillId = null;
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
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.2.4.3"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D0.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D1.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D2.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D3.6.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
            }
            #endregion 权限验证

            var result = new ModifyDocFeeReturnDto();
            if (!_EntityManager.Modify(new[] { model.DocFee })) return NotFound();
            var entity = _DbContext.Entry(model.DocFee);
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
            string err;
            if (!_AuthorizationManager.Demand(out err, "F.2.4.4"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D0.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D1.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D2.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out err, "D3.6.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
            }
            #endregion 权限验证
            var result = new RemoveDocFeeReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocFees;
            var item = dbSet.Find(id);
            //if (item.JobState > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 业务单的费用单

        #region 业务单的账单

        /// <summary>
        /// 获取全部业务单的账单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 Id，DocNo(业务单Id),JobId(间接属于指定的业务Id)。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocBillReturnDto> GetAllDocBill([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocBillReturnDto();

            var dbSet = _DbContext.DocBills;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
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
                                         where job.Id == id
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
                // 参数验证
                if (model.FeeIds == null || model.FeeIds.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "必须指定至少一个费用ID";
                    return result;
                }

                var entity = model.DocBill;
                entity.GenerateIdIfEmpty();

                // 权限检查
                string err;
                var collPerm = GetJobsFromFeeIds(model.FeeIds);
                if (collPerm.Any())
                {
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.AeId))
                    {
                        if (!_AuthorizationManager.Demand(out err, "D0.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.AiId))
                    {
                        if (!_AuthorizationManager.Demand(out err, "D1.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.SeId))
                    {
                        if (!_AuthorizationManager.Demand(out err, "D2.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                    if (collPerm.Any(c => c.JobTypeId == ProjectContent.SiId))
                    {
                        if (!_AuthorizationManager.Demand(out err, "D3.7.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                    }
                }

                if (entity is ICreatorInfo creatorInfo)
                {
                    creatorInfo.CreateBy = context.User.Id;
                    creatorInfo.CreateDateTime = OwHelper.WorldNow;
                }

                // 处理费用对象
                var collFees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id)).ToArray();
                if (collFees.Length != model.FeeIds.Count)
                {
                    return BadRequest("至少一个费用ID不存在");
                }

                // 检查费用是否已关联其他账单
                var linkedFeeIds = collFees.Where(c => c.BillId.HasValue && c.BillId != entity.Id).Select(c => c.Id).ToArray();
                if (linkedFeeIds.Any())
                {
                    return BadRequest($"以下费用ID已关联到其他账单: {string.Join(", ", linkedFeeIds)}");
                }

                // 添加账单
                _DbContext.DocBills.Add(model.DocBill);

                // 关联费用
                foreach (var fee in collFees)
                {
                    fee.BillId = model.DocBill.Id;
                }

                // 记录审计日志
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了账单ID:{entity.Id}，操作：AddDocBill");

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

            // 确保账单存在
            var existingBill = _DbContext.DocBills.Find(model.DocBill.Id);
            if (existingBill == null)
                return NotFound("指定Id的账单不存在");

            // 处理费用对象 - 检查它们是否存在
            var collFees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id)).ToList();
            if (collFees.Count != model.FeeIds.Count)
            {
                return BadRequest("至少一个费用Id不存在。");
            }

            // 获取当前关联到此账单的所有费用
            var oldFee = _DbContext.DocFees.Where(c => c.BillId == model.DocBill.Id).ToList();
            var jobs = GetJobsFromFeeIds(oldFee.Select(c => c.Id)); // 相关业务对象

            // 权限检查
            string err;
            if (jobs.Any())
            {
                if (jobs.Any(c => c.JobTypeId == ProjectContent.AeId))   //若有空运出口业务
                {
                    if (!_AuthorizationManager.Demand(out err, "D0.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                if (jobs.Any(c => c.JobTypeId == ProjectContent.AiId))   //若有空运进口业务
                {
                    if (!_AuthorizationManager.Demand(out err, "D1.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                if (jobs.Any(c => c.JobTypeId == ProjectContent.SeId))   //若有海运出口业务
                {
                    if (!_AuthorizationManager.Demand(out err, "D2.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
                if (jobs.Any(c => c.JobTypeId == ProjectContent.SiId))   //若有海运进口业务
                {
                    if (!_AuthorizationManager.Demand(out err, "D3.7.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                }
            }

            // 开始显式事务
            try
            {
                // 修改账单实体
                _DbContext.Entry(existingBill).CurrentValues.SetValues(model.DocBill);

                // 解除旧费用关联
                foreach (var fee in oldFee)
                {
                    fee.BillId = null;
                    _DbContext.Entry(fee).Property(f => f.BillId).IsModified = true;
                }

                // 建立新费用关联
                foreach (var fee in collFees)
                {
                    fee.BillId = model.DocBill.Id;
                    _DbContext.Entry(fee).Property(f => f.BillId).IsModified = true;
                }

                // 保存所有更改
                _DbContext.SaveChanges();

                // 记录日志
                _SqlAppLogger.LogGeneralInfo($"修改账单.{nameof(DocBill)}.{model.DocBill.Id}");

                return result;
            }
            catch (Exception ex)
            {
                // 记录错误日志
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
            var jobs = GetJobsFromBillIds(new Guid[] { model.Id });
            string err;
            if (jobs.Any(c => c.JobTypeId == ProjectContent.AeId))   //若有空运出口业务
                if (!_AuthorizationManager.Demand(out err, "D0.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (jobs.Any(c => c.JobTypeId == ProjectContent.AeId))   //若有空运进口业务
                if (!_AuthorizationManager.Demand(out err, "D1.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (jobs.Any(c => c.JobTypeId == ProjectContent.SeId))   //若有海运出口业务
                if (!_AuthorizationManager.Demand(out err, "D2.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (jobs.Any(c => c.JobTypeId == ProjectContent.SiId))   //若有海运进口业务
                if (!_AuthorizationManager.Demand(out err, "D3.7.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var id = model.Id;
            var dbSet = _DbContext.DocBills;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest("找不到指定的账单");

            // 检查关联费用是否已被申请
            var relatedFees = _DbContext.DocFees.Any(f => f.BillId == id);
            if (relatedFees)
            {
                return BadRequest($"账单(ID:{id})关联的费用已被申请，无法删除账单");
            }

            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
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
            var collJob = _DbContext.PlJobs.Where(c => model.Ids.Contains(c.Id));
            if (collJob.Count() != model.Ids.Count) return NotFound();
            string err;
            var allowAe = _AuthorizationManager.Demand(out err, "D0.7.2");
            var allowAi = _AuthorizationManager.Demand(out err, "D1.7.2");

            var coll = from job in _DbContext.PlJobs
                       where model.Ids.Contains(job.Id) && (allowAe || job.JobTypeId != ProjectContent.AeId) && (allowAi || job.JobTypeId != ProjectContent.AiId)

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

        #endregion 业务单的账单

        #region 空运进口单相关

        /// <summary>
        /// 获取全部空运进口单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlIaDocReturnDto> GetAllPlIaDoc([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlIaDocReturnDto();

            var dbSet = _DbContext.PlIaDocs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新空运进口单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlIaDocReturnDto> AddPlIaDoc(AddPlIaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "D1.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlIaDocReturnDto();
            var entity = model.PlIaDoc;
            entity.GenerateNewId();
            _DbContext.PlIaDocs.Add(model.PlIaDoc);
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            _DbContext.SaveChanges();
            result.Id = model.PlIaDoc.Id;
            return result;
        }

        /// <summary>
        /// 修改空运进口单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的空运进口单不存在。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlIaDocReturnDto> ModifyPlIaDoc(ModifyPlIaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlIaDocReturnDto();
            var doc = _DbContext.PlIaDocs.Find(model.PlIaDoc.Id);
            if (doc == null) return NotFound("指定Id的空运进口单不存在");
            string err;
            if (!_AuthorizationManager.Demand(out err, "D1.1.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            if (!_EntityManager.Modify(new[] { model.PlIaDoc })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的空运进口单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的空运进口单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlIaDocReturnDto> RemovePlIaDoc(RemovePlIaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlIaDocReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlIaDocs;
            var item = dbSet.Find(id);
            if (item.Status > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion  空运进口单相关

    }

}
