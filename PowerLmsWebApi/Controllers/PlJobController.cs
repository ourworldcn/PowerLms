using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;
using static PowerLmsWebApi.Controllers.GetDocBillsByJobIdReturnDto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 业务相关功能控制器。
    /// </summary>
    public class PlJobController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlJobController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, OrganizationManager organizationManager, IMapper mapper, EntityManager entityManager, DataDicManager dataManager, ILogger<PlJobController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrganizationManager = organizationManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _DataManager = dataManager;
            _Logger = logger;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrganizationManager _OrganizationManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly DataDicManager _DataManager;
        readonly ILogger<PlJobController> _Logger;

        #region 业务总表

        /// <summary>
        /// 获取全部业务总表。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 JobNo(业务号)，Id。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlJobReturnDto> GetAllPlJob([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlJobReturnDto();

            var dbSet = _DbContext.PlJobs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "JobNo", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId && c.JobNo == item.Value);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新业务总表。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlJobReturnDto> AddPlJob(AddPlJobParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new AddPlJobReturnDto();
            var entity = model.PlJob;
            entity.GenerateNewId();
            _DbContext.PlJobs.Add(model.PlJob);
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            entity.JobState = 2;
            entity.OperateState = 0;
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
        [HttpPut]
        public ActionResult<ModifyPlJobReturnDto> ModifyPlJob(ModifyPlJobParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlJobReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlJob })) return NotFound();
            //忽略不可更改字段
            var entity = _DbContext.Entry(model.PlJob);
            entity.Property(c => c.OperateState).IsModified = false;
            entity.Property(c => c.JobState).IsModified = false;
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的业务总表。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务总表不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlJobReturnDto> RemovePlJob(RemovePlJobParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlJobReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlJobs;
            var item = dbSet.Find(id);
            if (item.JobState > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 切换业务/单据状态功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="404">未找到指定的业务对象。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">切换业务状态非法。</response>  
        [HttpPost]
        public ActionResult<ChangeStateReturnDto> ChangeState(ChangeStateParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }
            var result = new ChangeStateReturnDto();
            var job = _DbContext.PlJobs.Find(model.JobId);
            if (job is null) return NotFound();

            if (model.JobState.HasValue)
            {
                switch (model.JobState)
                {
                    case 2:
                        if (job.JobState != 4) return BadRequest();
                        break;
                    case 4:
                        if (job.JobState != 2 && job.JobState != 8) return BadRequest();
                        break;
                    case 8:
                        if (job.JobState != 4 && job.JobState != 16) return BadRequest();
                        break;
                    case 16:
                        if (job.JobState != 8) return BadRequest();
                        break;
                    default:
                        return BadRequest();
                }
                _Logger.LogInformation("用户{uid}改变任务状态，任务Id={Id}.JobState {ov} -> {nv}", context.User.Id, job.Id, job.JobState, model.JobState.Value);
                job.JobState = (byte)model.JobState.Value;
            }
            if (model.OperateState.HasValue)    //若改变操作状态
            {
                switch (model.OperateState)
                {
                    case 0:
                        if (job.JobState != 2 || job.OperateState != 2) return BadRequest();
                        _Logger.LogInformation("用户{uid}改变任务状态，任务Id={Id}.JobState {ov} -> {nv}", context.User.Id, job?.Id, job?.JobState, 2);
                        job.JobState = 2;
                        break;
                    case 2:
                        if (job.JobState > 2 || job.OperateState != 0 && job.OperateState != 4) return BadRequest();
                        _Logger.LogInformation("用户{uid}改变任务状态，任务Id={Id}.JobState {ov} -> {nv}", context.User.Id, job.Id, job.JobState, 2);
                        job.JobState = 2;
                        break;
                    case 4:
                        if (job.JobState > 2 || job.OperateState != 2 && job.OperateState != 8) return BadRequest();
                        break;
                    case 8:
                        if (job.JobState > 2 || job.OperateState != 4 && job.OperateState != 16) return BadRequest();
                        break;
                    case 16:
                        if (job.JobState > 2 || job.OperateState != 8 && job.OperateState != 32) return BadRequest();
                        break;
                    case 32:
                        if (job.JobState > 2 || job.OperateState != 16) return BadRequest();
                        _Logger.LogInformation("用户{uid}改变任务状态，任务Id={Id}.JobState {ov} -> {nv}", context.User.Id, job.Id, job.JobState, 4);
                        job.JobState = 4;
                        break;
                    default:
                        return BadRequest();
                }
                _Logger.LogInformation("用户{uid}改变任务状态，任务Id={Id}.OperateState {ov} -> {nv}", context.User.Id, job.Id, job.OperateState, model.OperateState.Value);
                job.OperateState = (byte)model.OperateState.Value;
            }
            else return BadRequest();
            _DbContext.SaveChanges();
            result.JobState = job.JobState;
            result.OperateState = job.OperateState;
            return result;

        }


        #endregion 业务总表

        #region 空运出口单

        /// <summary>
        /// 获取全部空运出口单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 JobNo(业务号)，Id，EaDocNo(单号)。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlEaDocReturnDto> GetAllPlEaDoc([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlEaDocReturnDto();

            var dbSet = _DbContext.PlEaDocs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "JobNo", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.JobNo == item.Value);
                }
                else if (string.Equals(item.Key, "EaDocNo", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DocNo == item.Value);
                }
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
        [HttpPost]
        public ActionResult<AddPlEaDocReturnDto> AddPlEaDoc(AddPlEaDocParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyPlEaDocReturnDto> ModifyPlEaDoc(ModifyPlEaDocParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlEaDocReturnDto();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// 审核单笔费用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">费用已被审核。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">找不到指定Id的费用。</response>  
        [HttpPost]
        public ActionResult<AuditDocFeeReturnDto> AuditDocFee(AuditDocFeeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AuditDocFeeReturnDto();
            if (_DbContext.DocFees.Find(model.FeeId) is not DocFee fee) return NotFound();
            if (fee.CheckDate is not null || fee.ChechManId is not null) return BadRequest();
            fee.CheckDate = OwHelper.WorldNow;
            fee.ChechManId = context.User.Id;
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取全部业务单的费用单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 Id，DocId(业务单Id),Io。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocFeeReturnDto> GetAllDocFee([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDocFeeReturnDto();

            var dbSet = _DbContext.DocFees;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(DocFee.JobId), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.JobId == id);
                }
                else if (string.Equals(item.Key, nameof(DocFee.IO), StringComparison.OrdinalIgnoreCase))
                {
                    if (bool.TryParse(item.Value, out var b))
                        coll = coll.Where(c => c.IO == b);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新业务单的费用单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddDocFeeReturnDto> AddDocFee(AddDocFeeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddDocFeeReturnDto();
            var entity = model.DocFee;
            entity.GenerateNewId();
            _DbContext.DocFees.Add(model.DocFee);
            model.DocFee.BillId = null;
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
        [HttpPut]
        public ActionResult<ModifyDocFeeReturnDto> ModifyDocFee(ModifyDocFeeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocFeeReturnDto();
            if (!_EntityManager.Modify(new[] { model.DocFee })) return NotFound();
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
        [HttpDelete]
        public ActionResult<RemoveDocFeeReturnDto> RemoveDocFee(RemoveDocFeeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <param name="conditional">查询的条件。支持 Id，DocNo(业务单Id)。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllDocBillReturnDto> GetAllDocBill([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPost]
        public ActionResult<AddDocBillReturnDto> AddDocBill(AddDocBillParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddDocBillReturnDto();
            var entity = model.DocBill;
            entity.GenerateNewId();
            if (entity is ICreatorInfo creatorInfo)
            {
                creatorInfo.CreateBy = context.User.Id;
                creatorInfo.CreateDateTime = OwHelper.WorldNow;
            }
            _DbContext.DocBills.Add(model.DocBill);

            //处理费用对象
            var collFees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id)).ToArray();
            if (collFees.Count() != model.FeeIds.Count)
            {
                return BadRequest("至少一个费用Id不存在。");
            }
            collFees.ForEach(c => c.BillId = model.DocBill.Id);
            _DbContext.SaveChanges();
            result.Id = model.DocBill.Id;
            return result;
        }

        /// <summary>
        /// 修改业务单的账单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">至少一个费用Id不存在。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务单的账单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyDocBillReturnDto> ModifyDocBill(ModifyDocBillParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyDocBillReturnDto();
            if (!_EntityManager.Modify(new[] { model.DocBill })) return NotFound();
            //处理费用对象
            var collFees = _DbContext.DocFees.Where(c => model.FeeIds.Contains(c.Id)).ToArray();
            if (collFees.Count() != model.FeeIds.Count)
            {
                return BadRequest("至少一个费用Id不存在。");
            }
            var oldFee = _DbContext.DocFees.Where(c => c.BillId == model.DocBill.Id).ToArray();    //旧费用对象
            oldFee.ForEach(c => c.BillId = null);

            collFees.ForEach(c => c.BillId = model.DocBill.Id);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的业务单的账单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的业务单的账单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveDocBillReturnDto> RemoveDocBill(RemoveDocBillParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveDocBillReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DocBills;
            var item = dbSet.Find(id);
            //if (item.JobState > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetDocBillsByJobIdReturnDto();
            var collJob = _DbContext.PlJobs.Where(c => model.Ids.Contains(c.Id));
            if (collJob.Count() != model.Ids.Count) return NotFound();

            var coll = from job in _DbContext.PlJobs
                       where model.Ids.Contains(job.Id)

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

    /// <summary>
    /// 审核单笔费用功能参数封装类。
    /// </summary>
    public class AuditDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要审核的费用Id。
        /// </summary>
        public Guid FeeId { get; set; }
    }

    /// <summary>
    /// 审核单笔费用功能返回值封装类。
    /// </summary>
    public class AuditDocFeeReturnDto : ReturnDtoBase
    {
    }

    #region 业务单的账单
    /// <summary>
    /// 标记删除业务单的账单功能的参数封装类。
    /// </summary>
    public class RemoveDocBillParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务单的账单功能的返回值封装类。
    /// </summary>
    public class RemoveDocBillReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 根据业务Id，获取相关账单对象功能的参数封装类。
    /// </summary>
    public class GetDocBillsByJobIdParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务Id的集合。
        /// </summary>
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 根据业务Id，获取相关账单对象功能的返回值封装类。
    /// </summary>
    public class GetDocBillsByJobIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 根据业务Id，获取相关账单对象功能的返回值内的元素类型。
        /// </summary>
        public class GetDocBillsByJobIdItemDto
        {
            /// <summary>
            /// 业务Id。
            /// </summary>
            public Guid JobId { get; set; }

            /// <summary>
            /// 相关的账单。
            /// </summary>
            public List<DocBill> Bills { get; set; } = new List<DocBill>();
        }

        /// <summary>
        /// 返回的账单。
        /// </summary>
        public List<GetDocBillsByJobIdItemDto> Result { get; set; } = new List<GetDocBillsByJobIdItemDto>();
    }

    /// <summary>
    /// 获取所有业务单的账单功能的返回值封装类。
    /// </summary>
    public class GetAllDocBillReturnDto : PagingReturnDtoBase<DocBill>
    {
    }

    /// <summary>
    /// 增加新业务单的账单功能参数封装类。
    /// </summary>
    public class AddDocBillParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务单的账单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocBill DocBill { get; set; }

        /// <summary>
        /// 绑定的费用Id集合。
        /// </summary>
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 增加新业务单的账单功能返回值封装类。
    /// </summary>
    public class AddDocBillReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务单的账单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务单的账单信息功能参数封装类。
    /// </summary>
    public class ModifyDocBillParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务单的账单数据。
        /// </summary>
        public DocBill DocBill { get; set; }

        /// <summary>
        /// 账单绑定的费用Id集合，不在该集合的费用对象将不再绑定到账单上。
        /// </summary>
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 修改业务单的账单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocBillReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务单的账单

    #region 业务单的费用单
    /// <summary>
    /// 标记删除业务单的费用单功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务单的费用单功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有业务单的费用单功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeReturnDto : PagingReturnDtoBase<DocFee>
    {
    }

    /// <summary>
    /// 增加新业务单的费用单功能参数封装类。
    /// </summary>
    public class AddDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务单的费用单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFee DocFee { get; set; }
    }

    /// <summary>
    /// 增加新业务单的费用单功能返回值封装类。
    /// </summary>
    public class AddDocFeeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务单的费用单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务单的费用单信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务单的费用单数据。
        /// </summary>
        public DocFee DocFee { get; set; }
    }

    /// <summary>
    /// 修改业务单的费用单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务单的费用单

    #region 货场出重单
    /// <summary>
    /// 标记删除货场出重单功能的参数封装类。
    /// </summary>
    public class RemoveHuochangChuchongParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除货场出重单功能的返回值封装类。
    /// </summary>
    public class RemoveHuochangChuchongReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有货场出重单功能的返回值封装类。
    /// </summary>
    public class GetAllHuochangChuchongReturnDto : PagingReturnDtoBase<HuochangChuchong>
    {
    }

    /// <summary>
    /// 增加新货场出重单功能参数封装类。
    /// </summary>
    public class AddHuochangChuchongParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新货场出重单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public HuochangChuchong HuochangChuchong { get; set; }
    }

    /// <summary>
    /// 增加新货场出重单功能返回值封装类。
    /// </summary>
    public class AddHuochangChuchongReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新货场出重单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改货场出重单信息功能参数封装类。
    /// </summary>
    public class ModifyHuochangChuchongParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 货场出重单数据。
        /// </summary>
        public HuochangChuchong HuochangChuchong { get; set; }
    }

    /// <summary>
    /// 修改货场出重单信息功能返回值封装类。
    /// </summary>
    public class ModifyHuochangChuchongReturnDto : ReturnDtoBase
    {
    }
    #endregion 货场出重单

    #region 空运出口单
    /// <summary>
    /// 标记删除空运出口单功能的参数封装类。
    /// </summary>
    public class RemovePlEaDocParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口单功能的返回值封装类。
    /// </summary>
    public class RemovePlEaDocReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口单功能的返回值封装类。
    /// </summary>
    public class GetAllPlEaDocReturnDto : PagingReturnDtoBase<PlEaDoc>
    {
    }

    /// <summary>
    /// 增加新空运出口单功能参数封装类。
    /// </summary>
    public class AddPlEaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlEaDoc PlEaDoc { get; set; }
    }

    /// <summary>
    /// 增加新空运出口单功能返回值封装类。
    /// </summary>
    public class AddPlEaDocReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口单信息功能参数封装类。
    /// </summary>
    public class ModifyPlEaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口单数据。
        /// </summary>
        public PlEaDoc PlEaDoc { get; set; }
    }

    /// <summary>
    /// 修改空运出口单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlEaDocReturnDto : ReturnDtoBase
    {
    }
    #endregion 空运出口单

    #region 业务总表
    /// <summary>
    /// 切换业务状态接口参数封装类。
    /// </summary>
    public class ChangeStateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改业务的唯一Id。
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// 新的业务状态。不改变则为空。
        /// NewJob初始=0，Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.
        /// </summary>
        public int? JobState { get; set; }

        /// <summary>
        /// 新的操作状态。不改变则为空。
        /// NewJob初始=0,Arrived 已到货=2,Declared 已申报=4,Delivered 已配送=8,Submitted 已交单=16,Notified 已通知=32
        /// </summary>
        public int? OperateState { get; set; }
    }

    /// <summary>
    /// 切换业务状态接口返回值封装类。
    /// </summary>
    public class ChangeStateReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功这里是切换后的业务状态。
        /// </summary>
        public int JobState { get; set; }

        /// <summary>
        /// 如果成功这里是切换后的操作状态。
        /// </summary>
        public int OperateState { get; set; }
    }

    /// <summary>
    /// 标记删除业务总表功能的参数封装类。
    /// </summary>
    public class RemovePlJobParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务总表功能的返回值封装类。
    /// </summary>
    public class RemovePlJobReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有业务总表功能的返回值封装类。
    /// </summary>
    public class GetAllPlJobReturnDto : PagingReturnDtoBase<PlJob>
    {
    }

    /// <summary>
    /// 增加新业务总表功能参数封装类。
    /// </summary>
    public class AddPlJobParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务总表信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlJob PlJob { get; set; }
    }

    /// <summary>
    /// 增加新业务总表功能返回值封装类。
    /// </summary>
    public class AddPlJobReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务总表的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务总表信息功能参数封装类。
    /// </summary>
    public class ModifyPlJobParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务总表数据。
        /// </summary>
        public PlJob PlJob { get; set; }
    }

    /// <summary>
    /// 修改业务总表信息功能返回值封装类。
    /// </summary>
    public class ModifyPlJobReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务总表


}
