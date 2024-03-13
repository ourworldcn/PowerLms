using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

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
        public PlJobController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, OrganizationManager organizationManager, IMapper mapper, EntityManager entityManager, DataDicManager dataManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrganizationManager = organizationManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _DataManager = dataManager;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrganizationManager _OrganizationManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly DataDicManager _DataManager;

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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlJobReturnDto();
            var entity = model.PlJob;
            entity.GenerateNewId();
            _DbContext.PlJobs.Add(model.PlJob);
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
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
        /// 获取全部业务单的费用单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 Id，DocId(业务单Id)。不区分大小写。</param>
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
                else if (string.Equals(item.Key, nameof(DocFee.DocId), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.DocId == id);
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
    }

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
