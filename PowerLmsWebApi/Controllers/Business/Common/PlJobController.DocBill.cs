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

    }
}
