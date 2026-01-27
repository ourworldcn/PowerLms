using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 长时间运行任务的存储管理控制器。
    /// </summary>
    public class OwTaskStoreController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OwTaskStoreController(AccountManager accountManager, IServiceProvider serviceProvider, EntityManager entityManager,
            PowerLmsUserDbContext dbContext, ILogger<OwTaskStoreController> logger, IMapper mapper, AuthorizationManager authorizationManager, OwSqlAppLogger sqlAppLogger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _AuthorizationManager = authorizationManager;
            _SqlAppLogger = sqlAppLogger;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly EntityManager _EntityManager;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly ILogger<OwTaskStoreController> _Logger;
        private readonly IMapper _Mapper;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly OwSqlAppLogger _SqlAppLogger;

        #region 任务操作

        /// <summary>
        /// 获取全部任务记录。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns>任务记录列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllOwTaskStoreReturnDto> GetAllOwTaskStore([FromQuery] GetAllOwTaskStoreParamsDto model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllOwTaskStoreReturnDto();

            try
            {
                // 创建条件字典，使用不区分大小写的比较器
                var conditionDictionary = conditional ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // 使用EfHelper的动态条件生成功能一次性应用所有条件
                var dbSet = _DbContext.Set<OwTaskStore>().AsQueryable();
                dbSet = QueryHelper.GenerateWhereAnd(dbSet, conditionDictionary);

                // 应用排序和分页
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取任务记录时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取任务记录时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 增加新任务记录。
        /// </summary>
        /// <param name="model">包含新任务信息的参数对象</param>
        /// <returns>操作结果，包含新创建任务的ID</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddOwTaskStoreReturnDto> AddOwTaskStore(AddOwTaskStoreParamsDto model)
        {
            // 验证令牌和获取上下文
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("添加任务记录时提供了无效的令牌: {token}", model.Token);
                return Unauthorized();
            }

            var result = new AddOwTaskStoreReturnDto();

            try
            {
                // 创建新的OwTaskStore实体
                var entity = new OwTaskStore
                {
                    // 设置从DTO中接收的字段
                    ServiceTypeName = model.ServiceTypeName,
                    MethodName = model.MethodName,
                    Parameters = model.Parameters,
                    // 设置状态为待处理
                    Status = OwTaskStatus.Pending,
                    // 设置创建信息
                    CreatedUtc = DateTime.UtcNow,
                    CreatorId = context.User.Id,
                    TenantId = context.User.OrgId
                };

                // 添加实体到数据库上下文
                _DbContext.Add(entity);

                // 应用审计日志
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了任务ID:{entity.Id}，操作：AddOwTaskStore");

                // 保存更改到数据库
                _DbContext.SaveChanges();

                // 设置返回结果
                result.Id = entity.Id;

                _Logger.LogDebug("成功创建任务: {id}", entity.Id);
            }
            catch (Exception ex)
            {
                // 记录错误并设置返回错误信息
                _Logger.LogError(ex, "创建任务记录时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建任务记录时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 删除指定Id的任务记录。慎用！
        /// </summary>
        /// <param name="model">包含任务ID的参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的任务不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveOwTaskStoreReturnDto> RemoveOwTaskStore(RemoveOwTaskStoreParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveOwTaskStoreReturnDto();

            try
            {
                var id = model.Id;
                var item = _DbContext.Set<OwTaskStore>().Find(id);
                if (item is null) return NotFound();

                // 记录操作日志
                _DbContext.OwSystemLogs.Add(new OwSystemLog
                {
                    OrgId = context.User.OrgId,
                    ActionId = $"Delete.{nameof(OwTaskStore)}.{item.Id}",
                    ExtraGuid = context.User.Id,
                    WorldDateTime = OwHelper.WorldNow
                });

                // 删除任务
                _EntityManager.Remove(item);

                // 保存所有更改
                _DbContext.SaveChanges();

                _Logger.LogInformation($"成功删除任务 {id}");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除任务时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除任务时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 取消指定的任务。
        /// </summary>
        /// <param name="model">包含要取消的任务ID</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的任务不存在。</response>
        /// <response code="400">任务无法被取消（已完成或已失败）。</response>
        [HttpPost]
        public ActionResult<CancelTaskReturnDto> CancelTask(CancelTaskParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new CancelTaskReturnDto();

            try
            {
                var task = _DbContext.Set<OwTaskStore>().Find(model.Id);
                if (task is null) return NotFound();

                // 检查任务是否可以取消
                if (task.Status == OwTaskStatus.Completed || task.Status == OwTaskStatus.Failed)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = $"任务状态为 {task.Status}，无法取消";
                    return BadRequest(result);
                }

                // 更新任务状态为已失败（用失败状态表示被取消）
                task.Status = OwTaskStatus.Failed;
                task.CompletedUtc = DateTime.UtcNow;
                task.ErrorMessage = "用户取消任务";

                // 记录操作日志
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 取消了任务ID:{task.Id}，操作：CancelTask");

                _DbContext.SaveChanges();
                result.Success = true;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "取消任务时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"取消任务时发生错误: {ex.Message}";
            }

            return result;
        }

        #endregion
    }


}