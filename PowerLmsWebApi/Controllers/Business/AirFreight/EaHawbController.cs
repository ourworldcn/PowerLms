/*
 * 项目：PowerLms | 模块:空运出口分单控制器
 * 功能：空运出口分单的CRUD操作
 * 技术要点：依赖注入、权限验证、实体管理、主子表关联
 * 作者：zc | 创建：2026-02-01
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
    /// 空运出口分单相关控制器。
    /// </summary>
    public partial class EaHawbController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly ILogger<EaHawbController> _Logger;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public EaHawbController(AccountManager accountManager, IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper,
            AuthorizationManager authorizationManager, ILogger<EaHawbController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        #region 空运出口分单CRUD

        /// <summary>
        /// 获取全部空运出口分单。
        /// </summary>
        /// <param name="model">分页查询参数</param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns>空运出口分单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllPlEaHawbReturnDto> GetAllPlEaHawb([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.2"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new GetAllPlEaHawbReturnDto();
            try
            {
                var dbSet = _DbContext.EaHawbs;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                _Logger.LogDebug("查询空运出口分单成功，返回{Count}条记录", result.Result?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询空运出口分单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询空运出口分单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加新空运出口分单。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddPlEaHawbReturnDto> AddPlEaHawb(AddPlEaHawbParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.1"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlEaHawbReturnDto();
            try
            {
                var entity = model.EaHawb;
                entity.GenerateNewId();
                entity.CreateBy = context.User.Id;
                entity.CreateDateTime = OwHelper.WorldNow;
                entity.OrgId = context.User.OrgId;
                _DbContext.EaHawbs.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;
                _Logger.LogInformation("空运出口分单创建成功：ID={HawbId}, 分单号={HBLNo}, 用户={UserId}",
                    entity.Id, entity.HBLNo, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建空运出口分单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建空运出口分单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改空运出口分单信息。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的空运出口分单不存在。</response>
        [HttpPut]
        public ActionResult<ModifyPlEaHawbReturnDto> ModifyPlEaHawb(ModifyPlEaHawbParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.3"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlEaHawbReturnDto();
            try
            {
                if (!_EntityManager.Modify(new[] { model.EaHawb }))
                    return NotFound($"未找到ID为{model.EaHawb.Id}的空运出口分单");
                _DbContext.SaveChanges();
                _Logger.LogInformation("空运出口分单修改成功：ID={HawbId}, 用户={UserId}",
                    model.EaHawb.Id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改空运出口分单时发生错误，ID={HawbId}", model.EaHawb.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改空运出口分单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的空运出口分单。慎用！
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">分单存在关联数据，无法删除。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的空运出口分单不存在。</response>
        [HttpDelete]
        public ActionResult<RemovePlEaHawbReturnDto> RemovePlEaHawb(RemovePlEaHawbParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.4"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlEaHawbReturnDto();
            try
            {
                var id = model.Id;
                var item = _DbContext.EaHawbs.Find(id);
                if (item is null)
                    return NotFound($"未找到ID为{id}的空运出口分单");
                var hasOtherCharge = _DbContext.EaHawbOtherCharges.Any(c => c.HawbId == id);
                var hasCubage = _DbContext.EaHawbCubages.Any(c => c.HawbId == id);
                if (hasOtherCharge || hasCubage)
                    return BadRequest("分单存在关联的子表数据，无法删除。请先删除子表数据。");
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                _Logger.LogInformation("空运出口分单删除成功：ID={HawbId}, 用户={UserId}",
                    id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除空运出口分单时发生错误，ID={HawbId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除空运出口分单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 空运出口分单CRUD
    }
}
