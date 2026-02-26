/*
 * 项目:PowerLms | 模块:海运出口分提单控制器
 * 功能:海运出口分提单(货代提单)的CRUD操作
 * 技术要点:依赖注入、权限验证、实体管理
 * 作者:zc | 创建:2026-02 | 修改:2026-02-23 初始创建
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Helpers;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using PowerLmsWebApi.Controllers;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 海运出口分提单(货代提单)相关控制器。
    /// </summary>
    public class EsHblController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly ILogger<EsHblController> _Logger;
        /// <summary>
        /// 构造函数。
        /// </summary>
        public EsHblController(AccountManager accountManager, IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper,
            AuthorizationManager authorizationManager, ILogger<EsHblController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }
        #region 海运出口分提单CRUD
        /// <summary>
        /// 获取全部海运出口分提单。
        /// </summary>
        /// <param name="model">分页查询参数</param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns>海运出口分提单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误,具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllEsHblReturnDto> GetAllEsHbl([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllEsHblReturnDto();
            try
            {
                var dbSet = _DbContext.EsHbls;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                _Logger.LogDebug("查询海运出口分提单成功,返回{Count}条记录", result.Result?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询海运出口分提单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询海运出口分提单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        /// <summary>
        /// 增加新海运出口分提单。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误,具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddEsHblReturnDto> AddEsHbl(AddEsHblParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D2.1.1.2"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddEsHblReturnDto();
            try
            {
                var entity = model.Item;
                entity.GenerateNewId();
                _DbContext.EsHbls.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;
                _Logger.LogInformation("海运出口分提单创建成功:ID={HblId}, 分提单号={HBLNo}, 用户={UserId}",
                    entity.Id, entity.HBLNo, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建海运出口分提单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建海运出口分提单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        /// <summary>
        /// 修改海运出口分提单信息。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误,具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的海运出口分提单不存在。</response>
        [HttpPut]
        public ActionResult<ModifyEsHblReturnDto> ModifyEsHbl(ModifyEsHblParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D2.1.1.3"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyEsHblReturnDto();
            try
            {
                if (!_EntityManager.Modify(model.Items))
                    return NotFound($"未找到ID为{model.Items[0].Id}的海运出口分提单");
                _DbContext.SaveChanges();
                _Logger.LogInformation("海运出口分提单修改成功:ID={HblId}, 用户={UserId}",
                    model.Items[0].Id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改海运出口分提单时发生错误,ID={HblId}", model.Items?.Count > 0 ? model.Items[0].Id : Guid.Empty);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改海运出口分提单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        /// <summary>
        /// 删除指定Id的海运出口分提单。慎用！
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误,具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的海运出口分提单不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveEsHblReturnDto> RemoveEsHbl(RemoveEsHblParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D2.1.1.4"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveEsHblReturnDto();
            try
            {
                var id = model.Id;
                var item = _DbContext.EsHbls.Find(id);
                if (item is null)
                    return NotFound($"未找到ID为{id}的海运出口分提单");
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                _Logger.LogInformation("海运出口分提单删除成功:ID={HblId}, 用户={UserId}",
                    id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除海运出口分提单时发生错误,ID={HblId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除海运出口分提单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }
        #endregion 海运出口分提单CRUD
    }
}
