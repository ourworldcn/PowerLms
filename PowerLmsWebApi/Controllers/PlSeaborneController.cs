using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 海运业务相关控制器。
    /// </summary>
    public class PlSeaborneController : PlControllerBase
    {
        private AccountManager _AccountManager;
        private IServiceProvider _ServiceProvider;
        private PowerLmsUserDbContext _DbContext;
        private EntityManager _EntityManager;
        private IMapper _Mapper;

        /// <summary>
        /// 狗构造函数。
        /// </summary>
        /// <param name="accountManager"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="dbContext"></param>
        /// <param name="entityManager"></param>
        /// <param name="mapper"></param>
        public PlSeaborneController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext,
            EntityManager entityManager, IMapper mapper)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }

        #region 海运进口单相关

        /// <summary>
        /// 获取全部海运进口单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlIsDocReturnDto> GetAllPlIsDoc([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlIsDocReturnDto();

            var dbSet = _DbContext.PlIsDocs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新海运进口单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlIsDocReturnDto> AddPlIsDoc(AddPlIsDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlIsDocReturnDto();
            var entity = model.PlIsDoc;
            entity.GenerateNewId();
            _DbContext.PlIsDocs.Add(model.PlIsDoc);
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            _DbContext.SaveChanges();
            result.Id = model.PlIsDoc.Id;
            return result;
        }

        /// <summary>
        /// 修改海运进口单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的海运进口单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlIsDocReturnDto> ModifyPlIsDoc(ModifyPlIsDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlIsDocReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlIsDoc })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的海运进口单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的海运进口单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlIsDocReturnDto> RemovePlIsDoc(RemovePlIsDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlIsDocReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlIsDocs;
            var item = dbSet.Find(id);
            if (item.Status > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion  海运进口单相关

        #region 海运出口单相关

        /// <summary>
        /// 获取全部海运出口单。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlEsDocReturnDto> GetAllPlEsDoc([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlEsDocReturnDto();

            var dbSet = _DbContext.PlEsDocs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新海运出口单。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddPlEsDocReturnDto> AddPlEsDoc(AddPlEsDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddPlEsDocReturnDto();
            var entity = model.PlEsDoc;
            entity.GenerateNewId();
            _DbContext.PlEsDocs.Add(model.PlEsDoc);
            entity.CreateBy = context.User.Id;
            entity.CreateDateTime = OwHelper.WorldNow;
            _DbContext.SaveChanges();
            result.Id = model.PlEsDoc.Id;
            return result;
        }

        /// <summary>
        /// 修改海运出口单信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的海运出口单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlEsDocReturnDto> ModifyPlEsDoc(ModifyPlEsDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPlEsDocReturnDto();
            if (!_EntityManager.Modify(new[] { model.PlEsDoc })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的海运出口单。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的海运出口单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlEsDocReturnDto> RemovePlEsDoc(RemovePlEsDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemovePlEsDocReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlEsDocs;
            var item = dbSet.Find(id);
            if (item.Status > 0) return BadRequest("业务已经开始，无法删除。");
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion  海运出口单相关

        #region 海运箱量相关

        /// <summary>
        /// 获取全部海运箱量。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllContainerKindCountReturnDto> GetAllContainerKindCount([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllContainerKindCountReturnDto();

            var dbSet = _DbContext.ContainerKindCounts;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新海运箱量。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddContainerKindCountReturnDto> AddContainerKindCount(AddContainerKindCountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddContainerKindCountReturnDto();
            var entity = model.ContainerKindCount;
            entity.GenerateNewId();
            _DbContext.ContainerKindCounts.Add(model.ContainerKindCount);
            _DbContext.SaveChanges();
            result.Id = model.ContainerKindCount.Id;
            return result;
        }

        /// <summary>
        /// 修改海运箱量信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的海运箱量不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyContainerKindCountReturnDto> ModifyContainerKindCount(ModifyContainerKindCountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyContainerKindCountReturnDto();
            if (!_EntityManager.Modify(new[] { model.ContainerKindCount })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的海运箱量。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的海运箱量不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveContainerKindCountReturnDto> RemoveContainerKindCount(RemoveContainerKindCountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveContainerKindCountReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.ContainerKindCounts;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 设置全量箱量表。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPut]
        public ActionResult<SetContainerKindCountReturnDto> SetContainerKindCount(SetContainerKindCountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetContainerKindCountReturnDto();
            EfHelper.NormalizeChildren(model.Items, model.ParentId);
            List<ContainerKindCount> list = new List<ContainerKindCount>();
            if (!EfHelper.SetChildren(model.Items, model.ParentId, _DbContext, list))
            {
                return BadRequest(OwHelper.GetLastErrorMessage());
            }
            result.Result.AddRange(list);
            return result;
        }
        #endregion  海运箱量相关
    }

}
