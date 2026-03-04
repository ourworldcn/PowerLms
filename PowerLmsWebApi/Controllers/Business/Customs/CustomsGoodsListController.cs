/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关单货物明细子表的CRUD操作
 * 技术要点：依赖注入、权限验证、实体管理、ParentId归属保护
 * 作者：zc | 创建：2026-02
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Helpers;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Collections.Generic;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 报关单货物明细相关控制器。
    /// </summary>
    public class CustomsGoodsListController : PlControllerBase
    {
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly EntityManager _EntityManager;
        private readonly IMapper _Mapper;
        private readonly ILogger<CustomsGoodsListController> _Logger;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CustomsGoodsListController(AccountManager accountManager, IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper,
            ILogger<CustomsGoodsListController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _Logger = logger;
        }

        #region 报关单货物明细CRUD

        /// <summary>
        /// 获取全部报关单货物明细。支持分页、排序和通用字段条件查询。
        /// 通常配合 ParentId 条件查询指定报关单下的所有明细。
        /// </summary>
        /// <param name="model">分页查询参数。</param>
        /// <param name="conditional">通用查询条件字典。所有实体字段均可作为过滤条件，字符串类型为模糊查询，
        /// 数值/日期类型支持区间写法（如"2024-1-1,2024-12-31"）。常用条件：ParentId=报关单主表Id。</param>
        /// <returns>货物明细分页列表及总记录数。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllCustomsGoodsListReturnDto> GetAllCustomsGoodsList([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllCustomsGoodsListReturnDto();
            try
            {
                var dbSet = _DbContext.CustomsGoodsLists;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                _Logger.LogDebug("查询报关单货物明细成功，返回{Count}条记录", result.Result?.Count ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询报关单货物明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询报关单货物明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加新报关单货物明细。
        /// 创建前会验证 Item.ParentId 对应的报关单主表必须已存在，否则返回 400。
        /// 服务器自动生成新 Id，前端传入的 Id 会被忽略。
        /// </summary>
        /// <param name="model">新增参数。Item.ParentId 必须填写且对应的报关单主表必须已存在；
        /// Item.Id 可为任意值，返回时以 result.Id 为准。
        /// 注意：ParentId 为 null 或全零 Guid（Guid.Empty）均不是有效主表引用，服务器一并拒绝。</param>
        /// <returns>新增成功后返回新货物明细的 Id。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">Item.ParentId 为空或全零 Guid，或对应的报关单主表不存在。</response>
        /// <response code="401">无效令牌。</response>
        [HttpPost]
        public ActionResult<AddCustomsGoodsListReturnDto> AddCustomsGoodsList(AddCustomsGoodsListParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new AddCustomsGoodsListReturnDto();
            try
            {
                var entity = model.Item;
                if (entity.ParentId == null || entity.ParentId == Guid.Empty)
                    return BadRequest("ParentId 不能为空，必须指定所属报关单主表Id。");
                if (!_DbContext.CustomsDeclarations.Any(c => c.Id == entity.ParentId))
                    return BadRequest($"ParentId={entity.ParentId} 对应的报关单主表不存在。");
                entity.GenerateNewId();
                _DbContext.CustomsGoodsLists.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;
                _Logger.LogInformation("报关单货物明细创建成功：ID={Id}, ParentId={ParentId}, 用户={UserId}",
                    entity.Id, entity.ParentId, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建报关单货物明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建报关单货物明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改报关单货物明细信息。支持批量修改，Items 中每项必须包含有效的 Id。
        /// ParentId 字段受保护，即使前端传入新值也不会被写入数据库，
        /// 货物明细一旦创建后其所属报关单主表不可更改。
        /// </summary>
        /// <param name="model">修改参数。Items 列表不能为空；每个对象的 Id 必须是已存在的货物明细Id；
        /// Items[].ParentId 传入任何值均无效，服务器调用 EF Core 的 IsModified=false 忽略该字段的变更。</param>
        /// <returns>修改成功返回空结果对象；HasError=false 表示全部修改成功。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">Items 为 null 或空列表。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">Items 中至少有一个 Id 在数据库中不存在。</response>
        [HttpPut]
        public ActionResult<ModifyCustomsGoodsListReturnDto> ModifyCustomsGoodsList(ModifyCustomsGoodsListParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyCustomsGoodsListReturnDto();
            try
            {
                if (model.Items == null || model.Items.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "Items 不能为空。";
                    return BadRequest(result);
                }
                var modifiedEntities = new List<CustomsGoodsList>();
                if (!_EntityManager.Modify(model.Items, modifiedEntities))
                    return NotFound("未找到指定货物明细，Items 中至少有一个 Id 不存在。");
                foreach (var item in modifiedEntities)
                {
                    var entry = _DbContext.Entry(item);
                    entry.Property(c => c.ParentId).IsModified = false;
                }
                _DbContext.SaveChanges();
                _Logger.LogInformation("报关单货物明细修改成功：ID={Id}, 用户={UserId}", model.Items[0].Id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改报关单货物明细时发生错误，ID={Id}", model.Items?.Count > 0 ? model.Items[0].Id : Guid.Empty);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改报关单货物明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的报关单货物明细。
        /// 直接删除单条明细记录，无需检查子表（货物明细本身是叶子节点）。
        /// </summary>
        /// <param name="model">删除参数，传入要删除的货物明细 Id。</param>
        /// <returns>删除成功返回空结果对象。</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="404">指定Id的货物明细不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveCustomsGoodsListReturnDto> RemoveCustomsGoodsList(RemoveCustomsGoodsListParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new RemoveCustomsGoodsListReturnDto();
            try
            {
                var id = model.Id;
                var item = _DbContext.CustomsGoodsLists.Find(id);
                if (item is null)
                    return NotFound($"未找到ID为{id}的报关单货物明细");
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                _Logger.LogInformation("报关单货物明细删除成功：ID={Id}, 用户={UserId}", id, context.User.Id);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除报关单货物明细时发生错误，ID={Id}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除报关单货物明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 报关单货物明细CRUD
    }
}
