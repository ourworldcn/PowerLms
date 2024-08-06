using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.Formula.Functions;
using NuGet.Packaging;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

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
            PowerLmsUserDbContext dbContext, ILogger<FinancialController> logger, IMapper mapper, OwWfManager wfManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _WfManager = wfManager;
        }

        private AccountManager _AccountManager;
        private IServiceProvider _ServiceProvider;
        private EntityManager _EntityManager;
        private PowerLmsUserDbContext _DbContext;
        readonly ILogger<FinancialController> _Logger;
        readonly IMapper _Mapper;
        readonly OwWfManager _WfManager;

        #region 业务费用申请单

        /// <summary>
        /// 获取全部业务费用申请单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeRequisitionReturnDto> GetAllDocFeeRequisition([FromQuery] GetAllDocFeeRequisitionParamsDto model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeRequisitionReturnDto();
            var dbSet = _DbContext.DocFeeRequisitions.Where(c => c.OrgId == context.User.OrgId);
            if (model.WfState.HasValue)  //须限定审批流程状态
            {
                var tmpColl = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, model.WfState.Value).Select(c => c.Parent.Parent.DocId);
                dbSet = dbSet.Where(c => tmpColl.Contains(c.Id));
            }
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 获取当前用户相关的业务费用申请单和审批流状态。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeRequisitionWithWfReturnDto> GetAllDocFeeRequisitionWithWf([FromQuery] GetAllDocFeeRequisitionWithWfParamsDto model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeRequisitionWithWfReturnDto();
            var dbSet = _DbContext.DocFeeRequisitions.Where(c => c.OrgId == context.User.OrgId);
            if (model.WfState.HasValue)  //须限定审批流程状态
            {
                var tmpColl = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, model.WfState.Value).Select(c => c.Parent.Parent.DocId);
                dbSet = dbSet.Where(c => tmpColl.Contains(c.Id));
            }
            else
            {
                var tmpColl = _WfManager.GetWfNodeItemByOpertorId(context.User.Id, 15).Select(c => c.Parent.Parent.DocId);
                dbSet = dbSet.Where(c => tmpColl.Contains(c.Id));
            }
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);

            //后处理
            var ids = prb.Result.Select(c => c.Id).ToList();
            var wfs = _DbContext.OwWfs.Where(c => ids.Contains(c.DocId.Value)).ToArray();

            prb.Result.ForEach(c =>
            {
                result.Result.Add(new GetAllDocFeeRequisitionWithWfItemDto()
                {
                    Requisition = c,
                    Wf = _Mapper.Map<OwWfDto>(wfs.FirstOrDefault(d => d.DocId == c.Id)),
                });
            });
            result.Total = prb.Total;
            //_Mapper.Map(prb, result);

            return result;
        }

        /// <summary>
        /// 增加新业务费用申请单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeRequisitionReturnDto> AddDocFeeRequisition(AddDocFeeRequisitionParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AddDocFeeRequisitionReturnDto();
            var entity = model.DocFeeRequisition;
            entity.GenerateNewId();
            _DbContext.DocFeeRequisitions.Add(model.DocFeeRequisition);
            entity.MakerId = context.User.Id;
            entity.MakeDateTime = OwHelper.WorldNow;
            entity.OrgId = context.User.OrgId;

            _DbContext.SaveChanges();
            result.Id = model.DocFeeRequisition.Id;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeRequisitionReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocFeeRequisitions;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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

            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPost]
        public ActionResult<AddDocFeeRequisitionItemReturnDto> AddDocFeeRequisitionItem(AddDocFeeRequisitionItemParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context)
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
            var collSum = _DbContext.DocFeeRequisitionItems.Where(c => c.ParentId == model.DocFeeRequisitionItem.ParentId && c.Id != model.DocFeeRequisitionItem.Id);
            var amount = collSum.Sum(c => c.Amount) + model.DocFeeRequisitionItem.Amount;
            parent.Amount = amount;

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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeRequisitionItemReturnDto();
            if (!_EntityManager.Modify(new[] { model.DocFeeRequisitionItem })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.DocFeeRequisitionItem);
            //计算合计
            var parent = _DbContext.DocFeeRequisitions.Find(model.DocFeeRequisitionItem.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
            var collSum = _DbContext.DocFeeRequisitionItems.Where(c => c.ParentId == model.DocFeeRequisitionItem.ParentId && c.Id != model.DocFeeRequisitionItem.Id);
            var amount = collSum.Sum(c => c.Amount) + model.DocFeeRequisitionItem.Amount;
            parent.Amount = amount;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocFeeRequisitionItemReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocFeeRequisitionItems;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            //计算合计
            var parent = _DbContext.DocFeeRequisitions.Find(item.ParentId);
            if (parent is null) return BadRequest("没有找到 指定的 ParentId 实体");
            var collSum = _DbContext.DocFeeRequisitionItems.Where(c => c.ParentId == item.ParentId && c.Id != item.Id);
            var amount = collSum.Sum(c => c.Amount);
            parent.Amount = amount;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlInvoicesReturnDto> GetAllPlInvoices([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {

            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlInvoicesReturnDto();
            var dbSet = _DbContext.PlInvoicess;

            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新结算单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlInvoicesReturnDto> AddPlInvoices(AddPlInvoicesParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AddPlInvoicesReturnDto();
            var entity = model.PlInvoices;
            entity.GenerateNewId();
            model.PlInvoices.CreateBy = context.User.Id;
            model.PlInvoices.CreateDateTime = OwHelper.WorldNow;
            _DbContext.PlInvoicess.Add(model.PlInvoices);

            _DbContext.SaveChanges();

            result.Id = model.PlInvoices.Id;
            return result;
        }

        /// <summary>
        /// 修改结算单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlInvoicesReturnDto> ModifyPlInvoices(ModifyPlInvoicesParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlInvoicesReturnDto> RemovePlInvoices(RemovePlInvoicesParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlInvoicesReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlInvoicess;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            var children = _DbContext.PlInvoicesItems.Where(c => c.ParentId == item.Id).ToArray();
            _EntityManager.Remove(item);
            if (children.Length > 0) _DbContext.RemoveRange(children);
            _DbContext.OwSystemLogs.Add(new OwSystemLog
            {
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
        /// <response code="404">指定Id的结算单不存在。</response>  
        [HttpPost]
        public ActionResult<ConfirmPlInvoicesReturnDto> ConfirmPlInvoices(ConfirmPlInvoicesParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ConfirmPlInvoicesReturnDto();
            var coll = _DbContext.PlInvoicess.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (coll.Length != model.Ids.Count) return BadRequest("至少有一个id不存在对应的结算单");
            //if (coll.Any(c => c.ConfirmDateTime is not null)) return BadRequest("至少有一个结算单已被确认");
            //if (coll.Any(c => c.CreateBy == context.User.Id)) return BadRequest("至少有一个结算单是自己创建的");
            var now = OwHelper.WorldNow;
            coll.ForEach(c =>
            {
                c.ConfirmDateTime = now;
                c.ConfirmId = context.User.Id;
            });
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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

            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPost]
        public ActionResult<AddPlInvoicesItemReturnDto> AddPlInvoicesItem(AddPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
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
        /// <response code="404">指定Id的结算单明细不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlInvoicesItemReturnDto> ModifyPlInvoicesItem(ModifyPlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlInvoicesItemReturnDto();
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
        /// <response code="404">指定Id的结算单明细不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlInvoicesItemReturnDto> RemovePlInvoicesItem(RemovePlInvoicesItemParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
    }

    #region 结算单明细

    /// <summary>
    /// 获取结算单明细增强接口功能参数封装类。
    /// </summary>
    public class GetPlInvoicesItemParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取结算单明细增强接口功能返回值封装类。
    /// </summary>
    public class GetPlInvoicesItemReturnDto : PagingReturnDtoBase<GetPlInvoicesItemItem>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetPlInvoicesItemItem
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetPlInvoicesItemItem()
        {
            //申请单 job 费用实体 申请明细的余额（未结算）
        }

        /// <summary>
        /// 结算单详细项
        /// </summary>
        public PlInvoicesItem InvoicesItem { get; set; }

        /// <summary>
        /// 结算单。
        /// </summary>
        public PlInvoices Invoices { get; set; }

        /// <summary>
        /// 相关的任务对象。
        /// </summary>
        public PlJob PlJob { get; set; }

        /// <summary>
        /// 相关的申请单对象。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }

        /// <summary>
        /// 申请单明细对象。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }

        /// <summary>
        /// 相关的结算单对象。
        /// </summary>
        public PlInvoices Parent { get; set; }
    }

    /// <summary>
    /// 结算单确认功能参数封装类。
    /// </summary>
    public class ConfirmPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 结算单的Id集合。
        /// </summary>
        public List<Guid> Ids { get; set; }
    }

    /// <summary>
    /// 结算单确认功能返回值封装类。
    /// </summary>
    public class ConfirmPlInvoicesReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetPlInvoicesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<PlInvoicesItem> Items { get; set; } = new List<PlInvoicesItem>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetPlInvoicesItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<PlInvoicesItem> Result { get; set; } = new List<PlInvoicesItem>();
    }

    /// <summary>
    /// 标记删除结算单明细功能的参数封装类。
    /// </summary>
    public class RemovePlInvoicesItemParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除结算单明细功能的返回值封装类。
    /// </summary>
    public class RemovePlInvoicesItemReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有结算单明细功能的返回值封装类。
    /// </summary>
    public class GetAllPlInvoicesItemReturnDto : PagingReturnDtoBase<PlInvoicesItem>
    {
    }

    /// <summary>
    /// 获取申请单明细增强接口功能参数封装类。
    /// </summary>
    public class GetDocFeeRequisitionItemParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 获取申请单明细增强接口功能返回值封装类。
    /// </summary>
    public class GetDocFeeRequisitionItemReturnDto : PagingReturnDtoBase<GetDocFeeRequisitionItemItem>
    {

    }

    /// <summary>
    /// 获取申请单明细增强接口功能的返回值中的元素类型。
    /// </summary>
    public class GetDocFeeRequisitionItemItem
    {
        /// <summary>
        /// 申请单明细对象。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }

        /// <summary>
        /// 相关的任务对象。
        /// </summary>
        public PlJob PlJob { get; set; }

        /// <summary>
        /// 相关的申请单对象。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }

        /// <summary>
        /// 相关的费用对象。
        /// </summary>
        public DocFee DocFee { get; set; }

        /// <summary>
        /// 申请单明细对象未结算的剩余费用。
        /// </summary>
        public decimal Remainder { get; set; }
    }


    /// <summary>
    /// 增加新结算单明细功能参数封装类。
    /// </summary>
    public class AddPlInvoicesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新结算单明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlInvoicesItem PlInvoicesItem { get; set; }
    }

    /// <summary>
    /// 增加新结算单明细功能返回值封装类。
    /// </summary>
    public class AddPlInvoicesItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新结算单明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改结算单明细信息功能参数封装类。
    /// </summary>
    public class ModifyPlInvoicesItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 结算单明细数据。
        /// </summary>
        public PlInvoicesItem PlInvoicesItem { get; set; }
    }

    /// <summary>
    /// 修改结算单明细信息功能返回值封装类。
    /// </summary>
    public class ModifyPlInvoicesItemReturnDto : ReturnDtoBase
    {
    }
    #endregion 结算单明细

    #region 结算单

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<PlInvoices> Items { get; set; } = new List<PlInvoices>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetPlInvoicesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<PlInvoices> Result { get; set; } = new List<PlInvoices>();
    }

    /// <summary>
    /// 标记删除结算单功能的参数封装类。
    /// </summary>
    public class RemovePlInvoicesParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除结算单功能的返回值封装类。
    /// </summary>
    public class RemovePlInvoicesReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有结算单功能的返回值封装类。
    /// </summary>
    public class GetAllPlInvoicesReturnDto : PagingReturnDtoBase<PlInvoices>
    {
    }

    /// <summary>
    /// 增加新结算单功能参数封装类。
    /// </summary>
    public class AddPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新结算单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlInvoices PlInvoices { get; set; }
    }

    /// <summary>
    /// 增加新结算单功能返回值封装类。
    /// </summary>
    public class AddPlInvoicesReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新结算单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改结算单信息功能参数封装类。
    /// </summary>
    public class ModifyPlInvoicesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 结算单数据。
        /// </summary>
        public PlInvoices PlInvoices { get; set; }
    }

    /// <summary>
    /// 修改结算单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlInvoicesReturnDto : ReturnDtoBase
    {
    }
    #endregion 结算单

    #region 业务费用申请单明细

    /// <summary>
    /// 设置指定的申请单下所有明细功能的参数封装类。
    /// </summary>
    public class SetDocFeeRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 申请单的Id。
        /// </summary>
        public Guid FrId { get; set; }

        /// <summary>
        /// 申请单明细表的集合。
        /// 指定存在id的明细则更新，Id全0或不存在的Id自动添加，原有未指定的明细将被删除。
        /// </summary>
        public List<DocFeeRequisitionItem> Items { get; set; } = new List<DocFeeRequisitionItem>();
    }

    /// <summary>
    /// 设置指定的申请单下所有明细功能的返回值封装类。
    /// </summary>
    public class SetDocFeeRequisitionItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 指定申请单下，所有明细的对象。
        /// </summary>
        public List<DocFeeRequisitionItem> Result { get; set; } = new List<DocFeeRequisitionItem>();
    }

    /// <summary>
    /// 标记删除业务费用申请单明细功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionItemParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务费用申请单明细功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionItemReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有业务费用申请单明细功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionItemReturnDto : PagingReturnDtoBase<DocFeeRequisitionItem>
    {
    }

    /// <summary>
    /// 增加新业务费用申请单明细功能参数封装类。
    /// </summary>
    public class AddDocFeeRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务费用申请单明细信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }
    }

    /// <summary>
    /// 增加新业务费用申请单明细功能返回值封装类。
    /// </summary>
    public class AddDocFeeRequisitionItemReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务费用申请单明细的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单明细信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionItemParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务费用申请单明细数据。
        /// </summary>
        public DocFeeRequisitionItem DocFeeRequisitionItem { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单明细信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionItemReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务费用申请单明细

    #region 业务费用申请单

    /// <summary>
    /// 获取指定费用的剩余未申请金额参数封装类。
    /// </summary>
    public class GetFeeRemainingParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 费用的Id集合。
        /// </summary>
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 获取指定费用的剩余未申请金额功能返回值封装类。
    /// </summary>
    public class GetFeeRemainingItemReturnDto
    {
        /// <summary>
        /// 关联的费用的对象。
        /// </summary>
        public DocFee Fee { get; set; }

        /// <summary>
        /// 剩余未申请的费用。
        /// </summary>
        public decimal Remaining { get; set; }

        /// <summary>
        /// 费用关联的任务对象。
        /// </summary>
        public PlJob Job { get; set; }

    }

    /// <summary>
    /// 获取指定费用的剩余未申请金额功能返回值封装类。
    /// </summary>
    public class GetFeeRemainingReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 费用单据的额外信息。
        /// </summary>
        public List<GetFeeRemainingItemReturnDto> Result { get; set; } = new List<GetFeeRemainingItemReturnDto>();
    }

    /// <summary>
    /// 标记删除业务费用申请单功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务费用申请单功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeRequisitionReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取当前用户相关的业务费用申请单和审批流状态功能的参数封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionWithWfParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 限定流程状态。省略或为null则不限定。若限定流程状态，则操作人默认去当前登录用户。
        /// 1=正等待指定操作者审批，2=指定操作者已审批但仍在流转中，4=指定操作者参与的且已成功结束的流程,8=指定操作者参与的且已失败结束的流程。
        /// 12=指定操作者参与的且已结束的流程（包括成功/失败）
        /// </summary>
        public byte? WfState { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetAllDocFeeRequisitionWithWfItemDto
    {
        /// <summary>
        /// 申请单对象。
        /// </summary>
        public DocFeeRequisition Requisition { get; set; }

        /// <summary>
        /// 相关流程对象。
        /// </summary>
        public OwWfDto Wf { get; set; }
    }

    /// <summary>
    /// 获取当前用户相关的业务费用申请单和审批流状态的返回值封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionWithWfReturnDto
    {
        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<GetAllDocFeeRequisitionWithWfItemDto> Result { get; set; } = new List<GetAllDocFeeRequisitionWithWfItemDto>();
    }

    /// <summary>
    /// 获取全部业务费用申请单功能的参数封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionParamsDto : PagingParamsDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GetAllDocFeeRequisitionParamsDto()
        {

        }

        /// <summary>
        /// 限定流程状态。省略或为null则不限定。若限定流程状态，则操作人默认去当前登录用户。
        /// 1=正等待指定操作者审批，2=指定操作者已审批但仍在流转中，4=指定操作者参与的且已成功结束的流程,8=指定操作者参与的且已失败结束的流程。
        /// 12=指定操作者参与的且已结束的流程（包括成功/失败）
        /// </summary>
        public byte? WfState { get; set; }
    }


    /// <summary>
    /// 获取所有业务费用申请单功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeRequisitionReturnDto : PagingReturnDtoBase<DocFeeRequisition>
    {
    }

    /// <summary>
    /// 增加新业务费用申请单功能参数封装类。
    /// </summary>
    public class AddDocFeeRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务费用申请单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }
    }

    /// <summary>
    /// 增加新业务费用申请单功能返回值封装类。
    /// </summary>
    public class AddDocFeeRequisitionReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务费用申请单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务费用申请单数据。
        /// </summary>
        public DocFeeRequisition DocFeeRequisition { get; set; }
    }

    /// <summary>
    /// 修改业务费用申请单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeRequisitionReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务费用申请单


}
