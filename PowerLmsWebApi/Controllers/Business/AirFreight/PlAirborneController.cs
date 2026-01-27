/*
 * 项目：PowerLms | 模块：空运业务控制器
 * 功能：空运进口单和空运出口单的CRUD操作
 * 技术要点：依赖注入、权限验证、实体管理
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 空运进口API恢复
 */

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 空运业务相关控制器。
    /// </summary>
    public class PlAirborneController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly ILogger<PlAirborneController> _Logger;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public PlAirborneController(AccountManager accountManager, IServiceProvider serviceProvider, 
            PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper, 
            AuthorizationManager authorizationManager, ILogger<PlAirborneController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        #region 空运进口单相关

        /// <summary>
        /// 获取全部空运进口单。
        /// </summary>
        /// <param name="model">分页查询参数</param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns>空运进口单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlIaDocReturnDto> GetAllPlIaDoc([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            var result = new GetAllPlIaDocReturnDto();

            try
            {
                var dbSet = _DbContext.PlIaDocs;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);

                _Logger.LogDebug("查询空运进口单成功，返回{Count}条记录", result.Result?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询空运进口单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询空运进口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加新空运进口单。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlIaDocReturnDto> AddPlIaDoc(AddPlIaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            // 权限验证：空运进口业务新增权限
            if (!_AuthorizationManager.Demand(out string err, "D1.1.1.2")) 
                return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddPlIaDocReturnDto();

            try
            {
                var entity = model.PlIaDoc;
                entity.GenerateNewId();
                entity.CreateBy = context.User.Id;
                entity.CreateDateTime = OwHelper.WorldNow;
                entity.Status = 0; // 初始状态

                _DbContext.PlIaDocs.Add(entity);
                _DbContext.SaveChanges();
                
                result.Id = entity.Id;

                _Logger.LogInformation("空运进口单创建成功：ID={IaDocId}, 用户={UserId}", 
                    entity.Id, context.User.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建空运进口单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建空运进口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改空运进口单信息。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的空运进口单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlIaDocReturnDto> ModifyPlIaDoc(ModifyPlIaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            // 权限验证：空运进口业务修改权限
            if (!_AuthorizationManager.Demand(out string err, "D1.1.1.3")) 
                return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new ModifyPlIaDocReturnDto();

            try
            {
                if (!_EntityManager.Modify(new[] { model.PlIaDoc })) 
                    return NotFound($"未找到ID为{model.PlIaDoc.Id}的空运进口单");
                
                _DbContext.SaveChanges();

                _Logger.LogInformation("空运进口单修改成功：ID={IaDocId}, 用户={UserId}", 
                    model.PlIaDoc.Id, context.User.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改空运进口单时发生错误，ID={IaDocId}", model.PlIaDoc.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改空运进口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的空运进口单。慎用！
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的空运进口单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlIaDocReturnDto> RemovePlIaDoc(RemovePlIaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            // 权限验证：空运进口业务删除权限
            if (!_AuthorizationManager.Demand(out string err, "D1.1.1.4")) 
                return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RemovePlIaDocReturnDto();

            try
            {
                var id = model.Id;
                var item = _DbContext.PlIaDocs.Find(id);
                
                if (item is null) 
                    return NotFound($"未找到ID为{id}的空运进口单");

                // 业务规则验证：只能删除初始状态的单据
                if (item.Status > 0) 
                    return BadRequest("业务已经开始，无法删除。");

                _EntityManager.Remove(item);
                _DbContext.SaveChanges();

                _Logger.LogInformation("空运进口单删除成功：ID={IaDocId}, 用户={UserId}", 
                    id, context.User.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除空运进口单时发生错误，ID={IaDocId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除空运进口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 空运进口单相关

        #region 空运出口单相关

        /// <summary>
        /// 获取全部空运出口单。
        /// </summary>
        /// <param name="model">分页查询参数</param>
        /// <param name="conditional">已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns>空运出口单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlEaDocReturnDto> GetAllPlEaDoc([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            var result = new GetAllPlEaDocReturnDto();

            try
            {
                var dbSet = _DbContext.PlEaDocs;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);

                _Logger.LogDebug("查询空运出口单成功，返回{Count}条记录", result.Result?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询空运出口单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询空运出口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加新空运出口单。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlEaDocReturnDto> AddPlEaDoc(AddPlEaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            // 权限验证：空运出口业务新增权限
            if (!_AuthorizationManager.Demand(out string err, "D0.1.1.2")) 
                return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new AddPlEaDocReturnDto();

            try
            {
                var entity = model.PlEaDoc;
                entity.GenerateNewId();
                entity.CreateBy = context.User.Id;
                entity.CreateDateTime = OwHelper.WorldNow;
                entity.Status = 0; // 初始状态

                _DbContext.PlEaDocs.Add(entity);
                _DbContext.SaveChanges();
                
                result.Id = entity.Id;

                _Logger.LogInformation("空运出口单创建成功：ID={EaDocId}, 用户={UserId}", 
                    entity.Id, context.User.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建空运出口单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建空运出口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改空运出口单信息。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的空运出口单不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyPlEaDocReturnDto> ModifyPlEaDoc(ModifyPlEaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            // 权限验证：空运出口业务修改权限
            if (!_AuthorizationManager.Demand(out string err, "D0.1.1.3")) 
                return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new ModifyPlEaDocReturnDto();

            try
            {
                if (!_EntityManager.Modify(new[] { model.PlEaDoc })) 
                    return NotFound($"未找到ID为{model.PlEaDoc.Id}的空运出口单");
                
                _DbContext.SaveChanges();

                _Logger.LogInformation("空运出口单修改成功：ID={EaDocId}, 用户={UserId}", 
                    model.PlEaDoc.Id, context.User.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改空运出口单时发生错误，ID={EaDocId}", model.PlEaDoc.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改空运出口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的空运出口单。慎用！
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">未找到指定的业务，或该业务不在初始创建状态——无法删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的空运出口单不存在。</response>  
        [HttpDelete]
        public ActionResult<RemovePlEaDocReturnDto> RemovePlEaDoc(RemovePlEaDocParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            
            // 权限验证：空运出口业务删除权限
            if (!_AuthorizationManager.Demand(out string err, "D0.1.1.4")) 
                return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RemovePlEaDocReturnDto();

            try
            {
                var id = model.Id;
                var item = _DbContext.PlEaDocs.Find(id);
                
                if (item is null) 
                    return NotFound($"未找到ID为{id}的空运出口单");

                // 业务规则验证：只能删除初始状态的单据
                if (item.Status > 0) 
                    return BadRequest("业务已经开始，无法删除。");

                _EntityManager.Remove(item);
                _DbContext.SaveChanges();

                _Logger.LogInformation("空运出口单删除成功：ID={EaDocId}, 用户={UserId}", 
                    id, context.User.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除空运出口单时发生错误，ID={EaDocId}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除空运出口单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 空运出口单相关
    }
}