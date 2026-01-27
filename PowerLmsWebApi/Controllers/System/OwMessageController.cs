using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 系统内消息功能控制器。
    /// </summary>
    public class OwMessageController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwMessageController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<OwMessageController> logger, IMapper mapper, AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger, OrgManager<PowerLmsUserDbContext> orgManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _SqlAppLogger = sqlAppLogger;
            _OrgManager = orgManager;
        }
        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly EntityManager _EntityManager;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly ILogger<OwMessageController> _Logger;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly OwSqlAppLogger _SqlAppLogger;
        private readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        #region 消息查询
        /// <summary>
        /// 获取消息列表，支持分页和筛选。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持通用查询接口。</param>
        /// <returns>消息列表及总数量</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllOwMessageReturnDto> GetAllOwMessage([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            // 验证令牌
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetAllOwMessageReturnDto();
            try
            {
                // 获取当前用户的消息
                var dbSet = _DbContext.Set<OwMessage>();
                var coll = dbSet.Where(m => m.UserId == context.User.Id).AsNoTracking();
                // 应用查询条件
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                // 应用排序
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);
                // 获取分页结果
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取消息列表时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取消息列表时发生异常: {ex.Message}";
                return result;
            }
        }
        /// <summary>
        /// 发送消息。用户可以向同一商户内的其他用户发送消息，除非是超级管理员。
        /// </summary>
        /// <param name="model">消息参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">请求参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">尝试向不同商户的用户发送消息。</response>  
        [HttpPost]
        [Description("发送消息给指定用户")]
        public ActionResult<SendOwMessageReturnDto> SendOwMessage(SendOwMessageParamsDto model)
        {
            // 验证令牌
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new SendOwMessageReturnDto();
            try
            {
                // 验证参数
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "消息标题不能为空";
                    return BadRequest(result);
                }
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "消息内容不能为空";
                    return BadRequest(result);
                }
                if (model.ReceiverIds == null || model.ReceiverIds.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "接收者不能为空";
                    return BadRequest(result);
                }
                // 从数据库中获取发送者的完整信息，包括商户ID
                var currentUser = _DbContext.Accounts.Find(context.User.Id);
                if (currentUser == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = "无法获取当前用户信息";
                    return StatusCode((int)HttpStatusCode.InternalServerError, result);
                }
                // 从数据库中获取当前用户所属的商户ID
                var senderMerchantId = currentUser.MerchantId;
                // 检查是否为超级管理员
                bool isSuperAdmin = _AccountManager.IsAdmin(currentUser);
                var orgManager = _ServiceProvider.GetService<OrgManager<PowerLmsUserDbContext>>();
                // 验证所有接收者都属于同一商户，除非是超级管理员
                if (!isSuperAdmin && senderMerchantId.HasValue)
                {
                    // 获取所有接收者账户
                    var receivers = _DbContext.Accounts
                        .Where(a => model.ReceiverIds.Contains(a.Id))
                        .ToList();
                    // 检查每个接收者
                    foreach (var receiver in receivers)
                    {
                        // 检查接收者不为空
                        if (receiver == null)
                        {
                            result.HasError = true;
                            result.ErrorCode = 400;
                            result.DebugMessage = "接收者不存在";
                            return BadRequest(result);
                        }
                        var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                        // 检查接收者不属于同一商户
                        if (merchantId != senderMerchantId)
                        {
                            result.HasError = true;
                            result.ErrorCode = 403;
                            result.DebugMessage = "只能向同一商户内的用户发送消息";
                            return StatusCode((int)HttpStatusCode.Forbidden, result);
                        }
                    }
                }
                // 创建消息实体
                var messages = new List<OwMessage>();
                var now = DateTime.UtcNow;
                foreach (var receiverId in model.ReceiverIds)
                {
                    var message = new OwMessage
                    {
                        UserId = receiverId,
                        Title = model.Title,
                        Content = model.Content,
                        CreateBy = context.User.Id,
                        CreateUtc = now,
                        IsSystemMessage = isSuperAdmin, // 只有超级管理员发送的消息才标记为系统消息
                    };
                    message.GenerateNewId();
                    messages.Add(message);
                }
                // 添加到数据库
                _DbContext.Set<OwMessage>().AddRange(messages);
                _DbContext.SaveChanges();
                // 记录日志
                _SqlAppLogger.LogGeneralInfo($"发送消息.{messages.Count}条");
                result.MessageIds = messages.Select(m => m.Id).ToList();
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "发送消息时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"发送消息时发生异常: {ex.Message}";
                return BadRequest(result);
            }
        }
        /// <summary>
        /// 批量标记消息为已读。
        /// 当 MarkAll=true 时，将标记用户所有未读消息为已读；
        /// 否则标记指定 MessageIds 中的消息为已读。
        /// </summary>
        /// <param name="model">参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">当 MarkAll=false 且 MessageIds 为空时返回此错误。</response>  
        [HttpPut]
        public ActionResult<MarkMessagesAsReadReturnDto> MarkMessagesAsRead(MarkMessagesAsReadParamsDto model)
        {
            // 验证令牌
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new MarkMessagesAsReadReturnDto();
            try
            {
                // 验证参数
                if (!model.MarkAll && (model.MessageIds == null || model.MessageIds.Count == 0))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "请指定要标记为已读的消息ID列表，或设置 MarkAll=true 标记所有未读消息";
                    return BadRequest(result);
                }
                // 标记消息为已读
                var now = DateTime.UtcNow;
                IQueryable<OwMessage> query;
                if (model.MarkAll)
                {
                    // 标记所有未读消息
                    query = _DbContext.Set<OwMessage>()
                        .Where(m => m.UserId == context.User.Id && m.ReadUtc == null);
                    // 记录日志操作类型
                    _SqlAppLogger.LogGeneralInfo("标记所有消息已读");
                }
                else
                {
                    // 标记指定消息
                    query = _DbContext.Set<OwMessage>()
                        .Where(m => model.MessageIds.Contains(m.Id) && m.UserId == context.User.Id && m.ReadUtc == null);
                    // 记录日志操作类型
                    _SqlAppLogger.LogGeneralInfo($"标记消息已读.{model.MessageIds.Count}条");
                }
                // 获取消息并标记为已读
                var messages = query.ToList();
                foreach (var message in messages)
                {
                    message.ReadUtc = now;
                }
                _DbContext.SaveChanges();
                result.MarkedCount = messages.Count;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "标记消息已读时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"标记消息已读时发生异常: {ex.Message}";
                return result;
            }
        }
        /// <summary>
        /// 批量删除消息。用户只能删除自己的消息。
        /// </summary>
        /// <param name="model">参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveAllOwMessageReturnDto> RemoveAllOwMessage(RemoveAllOwMessageParamsDto model)
        {
            // 验证令牌
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new RemoveAllOwMessageReturnDto();
            try
            {
                // 验证参数
                if (model.Ids == null || model.Ids.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "消息ID列表不能为空";
                    return BadRequest(result);
                }
                // 查找用户有权删除的消息
                var messages = _DbContext.Set<OwMessage>()
                    .Where(m => model.Ids.Contains(m.Id) && m.UserId == context.User.Id)
                    .ToList();
                // 删除消息
                _DbContext.Set<OwMessage>().RemoveRange(messages);
                _DbContext.SaveChanges();
                // 记录日志
                _SqlAppLogger.LogGeneralInfo($"批量删除消息.{messages.Count}条");
                result.RemovedCount = messages.Count;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量删除消息时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"批量删除消息时发生异常: {ex.Message}";
                return result;
            }
        }
        /// <summary>
        /// 获取当前用户未读消息数量。
        /// </summary>
        /// <param name="model">参数，仅需提供令牌</param>
        /// <returns>未读消息数量</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetUnreadMessageCountReturnDto> GetUnreadMessageCount([FromQuery]TokenDtoBase model)
        {
            // 验证令牌
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new GetUnreadMessageCountReturnDto();
            try
            {
                // 查询未读消息数量
                var unreadCount = _DbContext.Set<OwMessage>()
                    .Where(m => m.UserId == context.User.Id && m.ReadUtc == null)
                    .Count();
                result.UnreadCount = unreadCount;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取未读消息数量时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取未读消息数量时发生异常: {ex.Message}";
                return result;
            }
        }
        #endregion 消息查询
    }
}
