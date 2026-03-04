/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关单主表的CRUD操作
 * 技术要点：依赖注入、权限验证、实体管理、OrgId多租户隔离
 * 作者：zc | 创建：2026-02
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
using System.Collections.Generic;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 报关单相关控制器。
    /// </summary>
    public class CustomsDeclarationController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly ILogger<CustomsDeclarationController> _Logger;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomsDeclarationController(AccountManager accountManager, IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper,
            AuthorizationManager authorizationManager, ILogger<CustomsDeclarationController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        #region 报关单主表CRUD

        /// <summary>
        /// 获取全部报关单。支持分页、排序和通用字段条件查询。
        /// </summary>
        /// <param name="model">分页查询参数。</param>
        /// <param name="conditional">通用查询条件字典。所有实体字段均可作为过滤条件，字符串类型为模糊查询，
        /// 数值/日期类型支持区间写法（如"2024-1-1,2024-12-31"）。</param>
        /// <returns>报关单分页列表及总记录数。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomsDeclarationReturnDto> GetAllCustomsDeclaration([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllCustomsDeclarationReturnDto();
            try
            {
                var dbSet = _DbContext.CustomsDeclarations;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                _Logger.LogDebug("查询报关单成功，返回{Count}条记录", result.Result?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询报关单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询报关单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加新报关单。
        /// 服务器自动生成新Id，并强制将 OrgId 设置为当前登录用户的所属机构Id，前端传入的 OrgId 会被忽略。
        /// </summary>
        /// <param name="model">新增参数。Item.Id 可为任意值，返回时以 result.Id 为准；Item.OrgId 无需填写，服务器自动赋值。</param>
        /// <returns>新增成功后返回新报关单的 Id。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddCustomsDeclarationReturnDto> AddCustomsDeclaration(AddCustomsDeclarationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new AddCustomsDeclarationReturnDto();
            try
            {
                var entity = model.Item;
                entity.GenerateNewId();
                entity.OrgId = context.User.OrgId;
                _DbContext.CustomsDeclarations.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;
                _Logger.LogInformation("报关单创建成功：ID={Id}, 用户={UserId}", entity.Id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建报关单时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建报关单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改报关单信息。支持批量修改，Items 中每项必须包含有效的 Id。
        /// OrgId、CreateBy、CreateDateTime 字段均受保护，前端传入的值不会写入数据库：
        /// - OrgId：由 EntityManager.Modify 内部通过反射自动设置 IsModified=false；
        /// - CreateBy/CreateDateTime：因 CustomsDeclaration 实现了 ICreatorInfo，EntityManager.Modify 内部自动保护。
        /// </summary>
        /// <param name="model">修改参数。Items 不能为空；每个对象的 Id 必须是已存在的报关单Id。</param>
        /// <returns>修改成功返回空结果对象；HasError=false 表示全部修改成功。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">Items 为 null 或空列表。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">Items 中至少有一个 Id 在数据库中不存在。</response>
        [HttpPut]
        public ActionResult<ModifyCustomsDeclarationReturnDto> ModifyCustomsDeclaration(ModifyCustomsDeclarationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyCustomsDeclarationReturnDto();
            try
            {
                if (model.Items == null || model.Items.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "Items 不能为空。";
                    return BadRequest(result);
                }
                var modifiedEntities = new List<CustomsDeclaration>();
                // EntityManager.Modify 内部已通过反射自动保护 OrgId（IsModified=false）；
                // 同时因 CustomsDeclaration 实现了 ICreatorInfo，CreateBy/CreateDateTime 也被自动保护。
                if (!_EntityManager.Modify(model.Items, modifiedEntities))
                    return NotFound("未找到指定报关单，Items 中至少有一个 Id 不存在。");
                _DbContext.SaveChanges();
                _Logger.LogInformation("报关单修改成功：ID={Id}, 用户={UserId}", model.Items[0].Id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改报关单时发生错误，ID={Id}", model.Items?.Count > 0 ? model.Items[0].Id : Guid.Empty);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改报关单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的报关单。慎用！
        /// 删除前会检查是否存在关联的货物明细（CustomsGoodsList.ParentId = 此报关单Id），
        /// 若存在子表数据则拒绝删除并返回 400，需前端先删除所有货物明细后再删除主表。
        /// </summary>
        /// <param name="model">删除参数，传入要删除的报关单 Id。</param>
        /// <returns>删除成功返回空结果对象。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">报关单下存在货物明细子表数据，禁止删除。请先删除所有货物明细。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的报关单不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomsDeclarationReturnDto> RemoveCustomsDeclaration(RemoveCustomsDeclarationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new RemoveCustomsDeclarationReturnDto();
            try
            {
                var id = model.Id;
                var item = _DbContext.CustomsDeclarations.Find(id);
                if (item is null)
                    return NotFound($"未找到ID为{id}的报关单");
                if (_DbContext.CustomsGoodsLists.Any(c => c.ParentId == id))
                    return BadRequest("报关单存在关联的货物明细，无法删除。请先删除货物明细。");
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                _Logger.LogInformation("报关单删除成功：ID={Id}, 用户={UserId}", id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除报关单时发生错误，ID={Id}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除报关单时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 报关单主表CRUD
    }
}
