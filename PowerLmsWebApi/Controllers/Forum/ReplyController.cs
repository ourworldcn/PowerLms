/*
 * 项目：PowerLms | 模块：论坛管理
 * 功能：论坛回复管理控制器，提供回复的CRUD操作和商户隔离
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
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers.Forum
{
    /// <summary>论坛回复控制器。</summary>
    public class ReplyController : PlControllerBase
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
        public ReplyController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext,
            OrgManager<PowerLmsUserDbContext> orgManager, IMapper mapper, EntityManager entityManager, AuthorizationManager authorizationManager, ILogger<ReplyController> logger)
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
        readonly ILogger<ReplyController> _Logger;

        /// <summary>
        /// 获取论坛回复列表，支持分页和条件过滤。
        /// 自动进行商户隔离，用户只能看到自己商户下帖子的回复。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持ParentId、AuthorId、Content等字段过滤。对于字符串类型会进行包含查询，其他类型进行精确匹配。</param>
        /// <returns>论坛回复列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">条件格式错误。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllOwReplyReturnDto> GetAllOwReply([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllOwReplyReturnDto();

            try
            {
                var dbSet = _DbContext.OwReplies;
                var query = dbSet.AsNoTracking();

                // 商户隔离：只显示当前用户商户下帖子的回复
                if (!context.User.IsSuperAdmin)
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (merchantId.HasValue)
                    {
                        var merchantIdString = merchantId.Value.ToString();
                        var allowedPostIds = (from post in _DbContext.OwPosts
                                            join category in _DbContext.OwForumCategories on post.ParentId equals category.Id
                                            where category.ParentId == merchantIdString
                                            select post.Id).ToList();

                        query = query.Where(r => allowedPostIds.Contains(r.ParentId));
                    }
                    else
                    {
                        // 如果获取不到商户ID，返回空结果
                        query = query.Where(r => false);
                    }
                }

                query = query.OrderBy(model.OrderFieldName, model.IsDesc);

                if (conditional != null && conditional.Count > 0)
                {
                    var filteredQuery = QueryHelper.GenerateWhereAnd(query, conditional);
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
                _Logger.LogError(ex, "获取论坛回复列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = ex.Message;
            }
            return result;
        }

        /// <summary>增加一个论坛回复。</summary>
        /// <param name="model">论坛回复参数</param>
        /// <returns>创建结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，指定的帖子不属于当前商户。</response>
        [HttpPost]
        public ActionResult<AddOwReplyReturnDto> AddOwReply(AddOwReplyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddOwReplyReturnDto();

            try
            {
                // 商户隔离：验证指定的帖子是否属于当前用户的商户
                var post = _DbContext.OwPosts.Find(model.Item.ParentId);
                if (post == null)
                {
                    return BadRequest("指定的帖子不存在");
                }

                if (!context.User.IsSuperAdmin)
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (merchantId.HasValue)
                    {
                        var merchantIdString = merchantId.Value.ToString();
                        var categoryExists = _DbContext.OwForumCategories
                            .Any(c => c.Id == post.ParentId && c.ParentId == merchantIdString);
                        
                        if (!categoryExists)
                        {
                            _Logger.LogWarning("用户 {UserId} 试图在不属于自己商户的帖子 {PostId} 下创建回复", 
                                context.User.Id, model.Item.ParentId);
                            return StatusCode(403, "权限不足，指定的帖子不属于当前商户");
                        }
                    }
                    else
                    {
                        return StatusCode(403, "权限不足，无法获取商户信息");
                    }
                }

                model.Item.GenerateNewId();
                model.Item.CreatedAt = OwHelper.WorldNow;
                model.Item.EditedAt = null;
                
                if (string.IsNullOrWhiteSpace(model.Item.AuthorId))
                    model.Item.AuthorId = context.User.Id.ToString();
                if (string.IsNullOrWhiteSpace(model.Item.AuthorDisplayName))
                    model.Item.AuthorDisplayName = context.User.DisplayName ?? context.User.LoginName;
                
                _DbContext.OwReplies.Add(model.Item);
                _DbContext.SaveChanges();
                result.Id = model.Item.Id;

                _Logger.LogInformation("用户 {UserId} 在帖子 {PostId} 成功创建回复 {ReplyId}", 
                    context.User.Id, model.Item.ParentId, model.Item.Id);
            }
            catch (Exception err)
            {
                _Logger.LogError(err, "创建论坛回复时发生错误");
                return BadRequest(err.Message);
            }
            return result;
        }

        /// <summary>修改已有论坛回复。</summary>
        /// <param name="model">论坛回复修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，无法修改其他商户的回复。</response>
        /// <response code="404">指定Id的回复不存在。</response>
        [HttpPut]
        public ActionResult<ModifyOwReplyReturnDto> ModifyOwReply(ModifyOwReplyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized(OwHelper.GetLastErrorMessage());
            var result = new ModifyOwReplyReturnDto();

            try
            {
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.OwReplies.Find(item.Id);
                    if (existing is null)
                    {
                        _Logger.LogWarning("修改论坛回复失败：找不到ID为 {replyId} 的回复", item.Id);
                        return NotFound($"找不到ID为 {item.Id} 的论坛回复");
                    }

                    // 商户隔离：验证用户是否有权限修改此回复
                    if (!context.User.IsSuperAdmin)
                    {
                        var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                        if (merchantId.HasValue)
                        {
                            var merchantIdString = merchantId.Value.ToString();
                            var isAllowed = (from reply in _DbContext.OwReplies
                                           join post in _DbContext.OwPosts on reply.ParentId equals post.Id
                                           join category in _DbContext.OwForumCategories on post.ParentId equals category.Id
                                           where reply.Id == existing.Id && category.ParentId == merchantIdString
                                           select reply.Id).Any();
                            
                            if (!isAllowed)
                            {
                                _Logger.LogWarning("用户 {UserId} 试图修改不属于自己商户的论坛回复 {ReplyId}", 
                                    context.User.Id, item.Id);
                                return StatusCode(403, "权限不足，无法修改其他商户的回复");
                            }
                        }
                        else
                        {
                            return StatusCode(403, "权限不足，无法获取商户信息");
                        }
                    }
                    
                    item.EditedAt = OwHelper.WorldNow;
                }

                var list = new List<OwReply>();
                if (!_EntityManager.Modify(model.Items, list))
                    return NotFound();

                // 保护核心属性不被修改
                foreach (var item in list)  // ✅ 修复：使用已跟踪的实体列表
                {
                    var entry = _DbContext.Entry(item);
                    entry.Property(r => r.CreatedAt).IsModified = false;
                    entry.Property(r => r.AuthorId).IsModified = false;
                    entry.Property(r => r.AuthorDisplayName).IsModified = false;
                    entry.Property(r => r.ParentId).IsModified = false; // 所属帖子不可修改
                    entry.Property(r => r.ParentReplyId).IsModified = false; // 父回复不可修改
                }
                
                _DbContext.SaveChanges();

                _Logger.LogInformation("用户 {UserId} 成功修改了 {Count} 个论坛回复", 
                    context.User.Id, model.Items.Count);
            }
            catch (Exception excp)
            {
                _Logger.LogError(excp, "修改论坛回复时发生错误");
                return BadRequest(excp.Message);
            }

            return result;
        }

        /// <summary>删除一个论坛回复。</summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，无法删除其他商户的回复。</response>
        [HttpDelete]
        public ActionResult<RemoveOwReplyReturnDto> RemoveOwReply([FromBody] RemoveOwReplyParamsDto model)
        {
            _Logger.LogInformation("开始执行删除论坛回复操作，回复ID: {replyId}", model.Id);

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("删除论坛回复失败：无效令牌 {token}", model.Token);
                return Unauthorized();
            }

            var result = new RemoveOwReplyReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.OwReplies;

            var item = dbSet.Find(id);
            if (item is null)
            {
                _Logger.LogWarning("删除论坛回复失败：找不到ID为 {replyId} 的回复", id);
                return BadRequest($"找不到ID为 {id} 的论坛回复");
            }

            try
            {
                // 商户隔离：验证用户是否有权限删除此回复
                if (!context.User.IsSuperAdmin)
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (merchantId.HasValue)
                    {
                        var merchantIdString = merchantId.Value.ToString();
                        var isAllowed = (from reply in _DbContext.OwReplies
                                       join post in _DbContext.OwPosts on reply.ParentId equals post.Id
                                       join category in _DbContext.OwForumCategories on post.ParentId equals category.Id
                                       where reply.Id == id && category.ParentId == merchantIdString
                                       select reply.Id).Any();
                        
                        if (!isAllowed)
                        {
                            _Logger.LogWarning("用户 {UserId} 试图删除不属于自己商户的论坛回复 {ReplyId}", 
                                context.User.Id, id);
                            return StatusCode(403, "权限不足，无法删除其他商户的回复");
                        }
                    }
                    else
                    {
                        return StatusCode(403, "权限不足，无法获取商户信息");
                    }
                }

                _Logger.LogInformation("准备删除论坛回复: ID={replyId}, 作者={authorId}",
                    item.Id, item.AuthorId);

                _EntityManager.Remove(item);
                _DbContext.SaveChanges();

                _Logger.LogInformation("成功删除论坛回复 {replyId}", id);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除论坛回复 {replyId} 时发生异常", id);
                return BadRequest(new
                {
                    ErrorMessage = "删除论坛回复时发生错误",
                    Details = ex.Message,
                    ReplyId = id
                });
            }

            return result;
        }

        /// <summary>通过回复Id获取详细信息。</summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="replyId">回复Id。</param>
        /// <returns>回复详细信息</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="400">指定实体的Id不存在。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足，无法访问其他商户的回复。</response>
        [HttpGet]
        public ActionResult<GetOwReplyByIdReturnDto> GetOwReplyById(Guid token, Guid replyId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetOwReplyByIdReturnDto();
            
            var reply = _DbContext.OwReplies.AsNoTracking().FirstOrDefault(r => r.Id == replyId);
            if (reply is null)
                return BadRequest("找不到指定的论坛回复");

            // 商户隔离：验证用户是否有权限访问此回复
            if (!context.User.IsSuperAdmin)
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (merchantId.HasValue)
                {
                    var merchantIdString = merchantId.Value.ToString();
                    var isAllowed = (from r in _DbContext.OwReplies
                                   join post in _DbContext.OwPosts on r.ParentId equals post.Id
                                   join category in _DbContext.OwForumCategories on post.ParentId equals category.Id
                                   where r.Id == replyId && category.ParentId == merchantIdString
                                   select r.Id).Any();
                    
                    if (!isAllowed)
                    {
                        _Logger.LogWarning("用户 {UserId} 试图访问不属于自己商户的论坛回复 {ReplyId}", 
                            context.User.Id, replyId);
                        return StatusCode(403, "权限不足，无法访问其他商户的回复");
                    }
                }
                else
                {
                    return StatusCode(403, "权限不足，无法获取商户信息");
                }
            }
                
            result.Result = reply;
            return result;
        }
    }
}