/*
 * 项目：PowerLms | 模块：论坛管理
 * 功能：论坛板块管理控制器，提供板块的CRUD操作和商户隔离
 * 技术要点：依赖注入、实体框架、AutoMapper映射、业务规则控制、商户数据隔离
 * 作者：zc | 创建：2024-12 | 修改：2024-12-19 简化商户隔离逻辑
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Forum;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;
namespace PowerLmsWebApi.Controllers.Forum
{
    /// <summary>论坛板块控制器。</summary>
    public class ForumCategoryController : PlControllerBase
    {
        /// <summary>构造函数。</summary>
        /// <param name="accountManager">账号管理器</param>
        /// <param name="serviceProvider">服务提供程序</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="orgManager">组织机构管理器</param>
        /// <param name="mapper">对象映射器</param>
        /// <param name="entityManager">实体管理器</param>
        /// <param name="authorizationManager">权限管理器</param>
        /// <param name="logger">日志记录器</param>
        public ForumCategoryController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext,
            OrgManager<PowerLmsUserDbContext> orgManager, IMapper mapper, EntityManager entityManager, AuthorizationManager authorizationManager, ILogger<ForumCategoryController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrgManager = orgManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }
        /// <summary>账号管理器。</summary>
        readonly AccountManager _AccountManager;
        /// <summary>服务提供程序。</summary>
        readonly IServiceProvider _ServiceProvider;
        /// <summary>数据库上下文。</summary>
        readonly PowerLmsUserDbContext _DbContext;
        /// <summary>组织机构管理器。</summary>
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        /// <summary>对象映射器。</summary>
        readonly IMapper _Mapper;
        /// <summary>实体管理器。</summary>
        readonly EntityManager _EntityManager;
        /// <summary>权限管理器。</summary>
        readonly AuthorizationManager _AuthorizationManager;
        /// <summary>日志记录器。</summary>
        readonly ILogger<ForumCategoryController> _Logger;
        /// <summary>
        /// 获取论坛板块列表，支持分页和条件过滤。
        /// 自动进行商户隔离，用户只能看到自己商户的板块。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持Title、AuthorId、Remark等字段过滤。对于字符串类型会进行包含查询，其他类型进行精确匹配。</param>
        /// <returns>论坛板块列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">条件格式错误。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllOwForumCategoryReturnDto> GetAllOwForumCategory([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllOwForumCategoryReturnDto();
            try
            {
                var dbSet = _DbContext.OwForumCategories;
                var query = dbSet.AsNoTracking();
                // 商户隔离：只显示当前用户商户的板块
                if (!context.User.IsSuperAdmin)
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (merchantId.HasValue)
                    {
                        query = query.Where(c => c.ParentId == merchantId.Value.ToString());
                    }
                    else
                    {
                        // 如果获取不到商户ID，返回空结果
                        query = query.Where(c => false);
                    }
                }
                query = query.OrderBy(model.OrderFieldName, model.IsDesc);
                if (conditional != null && conditional.Count > 0)
                {
                    var filteredQuery = EfHelper.GenerateWhereAnd(query, conditional);
                    if (filteredQuery == null)
                    {
                        return BadRequest(OwHelper.GetLastErrorMessage());
                    }
                    query = filteredQuery;
                }
                var prb = _EntityManager.GetAll(query, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取论坛板块列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = ex.Message;
            }
            return result;
        }
        /// <summary>增加一个论坛板块。</summary>
        /// <param name="model">论坛板块参数</param>
        /// <returns>创建结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，无法在当前商户创建板块。</response>
        [HttpPost]
        public ActionResult<AddOwForumCategoryReturnDto> AddOwForumCategory(AddOwForumCategoryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddOwForumCategoryReturnDto();
            try
            {
                model.Item.GenerateNewId();
                model.Item.CreatedAt = OwHelper.WorldNow;
                model.Item.EditedAt = null;
                // 商户隔离：自动设置ParentId为当前用户的商户ID
                if (!context.User.IsSuperAdmin)
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue)
                    {
                        _Logger.LogWarning("用户 {UserId} 无法获取商户ID，无法创建论坛板块", context.User.Id);
                        return StatusCode(403, "权限不足，无法在当前商户创建板块");
                    }
                    model.Item.ParentId = merchantId.Value.ToString();
                }
                else if (context.User.IsSuperAdmin)
                {
                    // 超管可以手动指定ParentId，如果没有指定则使用默认值
                    if (string.IsNullOrEmpty(model.Item.ParentId))
                    {
                        return BadRequest("超级管理员必须指定ParentId（商户ID）");
                    }
                }
                if (string.IsNullOrWhiteSpace(model.Item.AuthorId))
                    model.Item.AuthorId = context.User.Id.ToString();
                if (string.IsNullOrWhiteSpace(model.Item.AuthorDisplayName))
                    model.Item.AuthorDisplayName = context.User.DisplayName ?? context.User.LoginName;
                _DbContext.OwForumCategories.Add(model.Item);
                _DbContext.SaveChanges();
                result.Id = model.Item.Id;
                _Logger.LogInformation("用户 {UserId} 成功创建论坛板块 {CategoryId}，商户 {MerchantId}",
                    context.User.Id, model.Item.Id, model.Item.ParentId);
            }
            catch (Exception err)
            {
                _Logger.LogError(err, "创建论坛板块时发生错误");
                return BadRequest(err.Message);
            }
            return result;
        }
        /// <summary>修改已有论坛板块。</summary>
        /// <param name="model">论坛板块修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，无法修改其他商户的板块。</response>
        /// <response code="404">指定Id的板块不存在。</response>
        [HttpPut]
        public ActionResult<ModifyOwForumCategoryReturnDto> ModifyOwForumCategory(ModifyOwForumCategoryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyOwForumCategoryReturnDto();
            try
            {
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.OwForumCategories.Find(item.Id);
                    if (existing is null)
                    {
                        _Logger.LogWarning("修改论坛板块失败：找不到ID为 {categoryId} 的板块", item.Id);
                        return NotFound($"找不到ID为 {item.Id} 的论坛板块");
                    }
                    // 商户隔离：验证用户是否有权限修改此板块
                    if (!context.User.IsSuperAdmin)
                    {
                        var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                        if (!merchantId.HasValue || existing.ParentId != merchantId.Value.ToString())
                        {
                            _Logger.LogWarning("用户 {UserId} 试图修改不属于自己商户的论坛板块 {CategoryId}",
                                context.User.Id, item.Id);
                            return StatusCode(403, "权限不足，无法修改其他商户的板块");
                        }
                    }
                    item.EditedAt = OwHelper.WorldNow;
                }
                var list = new List<OwForumCategory>();
                if (!_EntityManager.Modify(model.Items, list))
                    return NotFound();
                // 保护核心属性不被修改
                foreach (var item in list)  // ✅ 修复：使用已跟踪的实体列表
                {
                    var entry = _DbContext.Entry(item);
                    entry.Property(c => c.CreatedAt).IsModified = false;
                    entry.Property(c => c.ParentId).IsModified = false; // 商户归属不可修改
                }
                _DbContext.SaveChanges();
                _Logger.LogInformation("用户 {UserId} 成功修改了 {Count} 个论坛板块",
                    context.User.Id, model.Items.Count);
            }
            catch (Exception excp)
            {
                _Logger.LogError(excp, "修改论坛板块时发生错误");
                return BadRequest(excp.Message);
            }
            return result;
        }
        /// <summary>删除一个论坛板块。</summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，无法删除其他商户的板块。</response>
        [HttpDelete]
        public ActionResult<RemoveOwForumCategoryReturnDto> RemoveOwForumCategory([FromBody] RemoveOwForumCategoryParamsDto model)
        {
            _Logger.LogInformation("开始执行删除论坛板块操作，板块ID: {categoryId}", model.Id);
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("删除论坛板块失败：无效令牌 {token}", model.Token);
                return Unauthorized();
            }
            var result = new RemoveOwForumCategoryReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.OwForumCategories;
            var item = dbSet.Find(id);
            if (item is null)
            {
                _Logger.LogWarning("删除论坛板块失败：找不到ID为 {categoryId} 的板块", id);
                return BadRequest($"找不到ID为 {id} 的论坛板块");
            }
            try
            {
                // 商户隔离：验证用户是否有权限删除此板块
                if (!context.User.IsSuperAdmin)
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue || item.ParentId != merchantId.Value.ToString())
                    {
                        _Logger.LogWarning("用户 {UserId} 试图删除不属于自己商户的论坛板块 {CategoryId}",
                            context.User.Id, id);
                        return StatusCode(403, "权限不足，无法删除其他商户的板块");
                    }
                }
                _Logger.LogInformation("准备删除论坛板块: ID={categoryId}, 标题={title}, 商户={merchantId}",
                    item.Id, item.Title, item.ParentId);
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                _Logger.LogInformation("成功删除论坛板块 {categoryId}", id);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除论坛板块 {categoryId} 时发生异常", id);
                return BadRequest(new
                {
                    ErrorMessage = "删除论坛板块时发生错误",
                    Details = ex.Message,
                    CategoryId = id
                });
            }
            return result;
        }
        /// <summary>通过板块Id获取详细信息。</summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="categoryId">板块Id。</param>
        /// <returns>板块详细信息</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，无法访问其他商户的板块。</response>
        [HttpGet]
        public ActionResult<GetOwForumCategoryByIdReturnDto> GetOwForumCategoryById(Guid token, Guid categoryId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetOwForumCategoryByIdReturnDto();
            var category = _DbContext.OwForumCategories.AsNoTracking().FirstOrDefault(c => c.Id == categoryId);
            if (category is null)
                return BadRequest("找不到指定的论坛板块");
            // 商户隔离：验证用户是否有权限访问此板块
            if (!context.User.IsSuperAdmin)
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue || category.ParentId != merchantId.Value.ToString())
                {
                    _Logger.LogWarning("用户 {UserId} 试图访问不属于自己商户的论坛板块 {CategoryId}",
                        context.User.Id, categoryId);
                    return StatusCode(403, "权限不足，无法访问其他商户的板块");
                }
            }
            result.Result = category;
            return result;
        }
    }
}