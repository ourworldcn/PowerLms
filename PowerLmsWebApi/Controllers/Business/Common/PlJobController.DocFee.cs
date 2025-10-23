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
    public partial class PlJobController : PlControllerBase
    {
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
            if (!(model.IsAudit && _AuthorizationManager.Demand(out string err, "F.2.4.5") || !model.IsAudit && _AuthorizationManager.Demand(out string err2, "F.2.4.6")))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out string err3, "D0.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err3);
                }
                else if (job.JobTypeId == ProjectContent.AiId)
                {
                    if (!_AuthorizationManager.Demand(out string err4, "D1.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err4);
                }
                else if (job.JobTypeId == ProjectContent.SeId)
                {
                    if (!_AuthorizationManager.Demand(out string err5, "D2.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err5);
                }
                else if (job.JobTypeId == ProjectContent.SiId)
                {
                    if (!_AuthorizationManager.Demand(out string err6, "D3.6.7")) return StatusCode((int)HttpStatusCode.Forbidden, err6);
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
            var r = coll.AsEnumerable();
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.2"))  //若无通用查看权限
            {
                var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                if (currentCompany == null)
                {
                    return result;
                }

                var orgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id).ToArray();
                var userIds = _DbContext.AccountPlOrganizations.Where(c => orgIds.Contains(c.OrgId)).Select(c => c.UserId).Distinct().ToHashSet();   //所有相关人Id集合
                var jobIds = r.Select(c => c.JobId).Distinct().ToHashSet();
                var jobDic = _DbContext.PlJobs
                    .Where(c => jobIds.Contains(c.Id))
                    .ToList() // 修复：先ToList()再ToDictionary()
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
                bool hasGeneralPermission = _AuthorizationManager.Demand(out string err, "F.2.4.2");

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
            var r = collBase.AsEnumerable();
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.2"))  //若无通用查看权限
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
            if (!_AuthorizationManager.Demand(out string err, "F.2.4.1"))
            {
                if (job.JobTypeId == ProjectContent.AeId)
                {
                    if (!_AuthorizationManager.Demand(out string err2, "D0.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err2);
                }
                else if (job.JobTypeId == ProjectContent.AiId)    //若是空运进口业务
                {
                    if (!_AuthorizationManager.Demand(out string err3, "D1.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err3);
                }
                else if (job.JobTypeId == ProjectContent.SeId)    //若是海运出口业务
                {
                    if (!_AuthorizationManager.Demand(out string err4, "D2.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err4);
                }
                else if (job.JobTypeId == ProjectContent.SiId)    //若是海运进口业务
                {
                    if (!_AuthorizationManager.Demand(out string err5, "D3.6.1")) return StatusCode((int)HttpStatusCode.Forbidden, err5);
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

        #endregion 业务单的费用单
    }
}
