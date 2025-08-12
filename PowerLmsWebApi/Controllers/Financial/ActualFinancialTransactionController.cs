/*
 * 项目：PowerLms财务系统
 * 模块：实际收付记录控制器
 * 文件说明：
 * - 功能1：实际收付记录的完整CRUD操作
 * - 功能2：支持软删除和恢复功能
 * 技术要点：
 * - 基于标准CRUD模式设计
 * - 实现软删除模式
 * - 完整的数据验证
 * - 支持多条件查询和分页
 * 作者：zc
 * 创建：2025-01
 * 修改：2025-01-27 修复删除操作的多租户验证和错误处理
 */

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers.Financial
{
    /// <summary>
    /// 实际收付记录控制器。
    /// 提供实际收付记录的完整CRUD功能，包括软删除和恢复。
    /// </summary>
    public class ActualFinancialTransactionController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ActualFinancialTransactionController(
            AccountManager accountManager, 
            IServiceProvider serviceProvider, 
            EntityManager entityManager,
            PowerLmsUserDbContext dbContext, 
            ILogger<ActualFinancialTransactionController> logger, 
            IMapper mapper, 
            OwSqlAppLogger sqlAppLogger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _EntityManager = entityManager;
            _DbContext = dbContext;
            _Logger = logger;
            _Mapper = mapper;
            _SqlAppLogger = sqlAppLogger;
        }

        private readonly AccountManager _AccountManager;
        private readonly IServiceProvider _ServiceProvider;
        private readonly EntityManager _EntityManager;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly ILogger<ActualFinancialTransactionController> _Logger;
        private readonly IMapper _Mapper;
        private readonly OwSqlAppLogger _SqlAppLogger;

        #region 基础CRUD操作

        /// <summary>
        /// 获取全部实际收付记录。
        /// </summary>
        /// <param name="model">分页和排序参数</param>
        /// <param name="conditional">查询条件。支持通用查询——所有实体字段都可作为条件。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。
        /// 额外支持：IsDelete 可以设置为 "true" 来查询已删除的记录，"false" 或省略则查询未删除的记录。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllActualFinancialTransactionReturnDto> GetAllActualFinancialTransaction(
            [FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new GetAllActualFinancialTransactionReturnDto();

            try
            {
                var dbSet = _DbContext.ActualFinancialTransactions;

                // 默认只显示未删除的记录，除非明确指定查询已删除的记录
                var coll = dbSet.Where(x => !x.IsDelete);

                // 处理软删除条件
                if (conditional != null && conditional.TryGetValue("IsDelete", out var isDeleteValue))
                {
                    if (bool.TryParse(isDeleteValue, out var isDelete))
                    {
                        if (isDelete)
                        {
                            // 如果明确要求查询已删除的记录
                            coll = dbSet.Where(x => x.IsDelete);
                        }
                        // 否则保持默认的未删除记录查询
                    }
                    
                    // 从条件中移除 IsDelete，避免重复处理
                    conditional = new Dictionary<string, string>(conditional, StringComparer.OrdinalIgnoreCase);
                    conditional.Remove("IsDelete");
                }

                // 应用其他查询条件
                coll = EfHelper.GenerateWhereAnd(coll, conditional);

                // 应用排序和分页
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取实际收付记录时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取实际收付记录时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 添加新的实际收付记录。
        /// </summary>
        /// <param name="model">包含新实际收付记录信息的参数对象</param>
        /// <returns>操作结果，包含新创建记录的ID</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddActualFinancialTransactionReturnDto> AddActualFinancialTransaction(
            AddActualFinancialTransactionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("添加实际收付记录时提供了无效的令牌: {token}", model.Token);
                return Unauthorized();
            }

            var result = new AddActualFinancialTransactionReturnDto();

            try
            {
                // 验证输入参数
                if (model.Item == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "实际收付记录数据不能为空";
                    return result;
                }

                // 获取要保存的实体并进行基础设置
                var entity = model.Item;
                entity.GenerateIdIfEmpty(); // 生成新的GUID

                // 设置创建信息
                entity.CreateBy = context.User.Id;
                entity.CreateDateTime = OwHelper.WorldNow;

                // 确保软删除标记为false（新建记录默认未删除）
                entity.IsDelete = false;

                // 添加实体到数据库上下文
                _DbContext.ActualFinancialTransactions.Add(entity);

                // 记录操作日志
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 创建了实际收付记录ID:{entity.Id}");

                // 保存更改到数据库
                _DbContext.SaveChanges();

                // 设置返回结果
                result.Id = entity.Id;

                _Logger.LogDebug("成功创建实际收付记录: {id}", entity.Id);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建实际收付记录时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建实际收付记录时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 修改实际收付记录信息。
        /// </summary>
        /// <param name="model">包含要修改的实际收付记录信息的参数对象</param>
        /// <returns>修改操作的结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">记录已被删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的实际收付记录不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyActualFinancialTransactionReturnDto> ModifyActualFinancialTransaction(
            ModifyActualFinancialTransactionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new ModifyActualFinancialTransactionReturnDto();

            try
            {
                // 使用EntityManager的软删除安全修改方法
                if (!_EntityManager.ModifyWithMarkDelete(model.Items))
                    return NotFound();

                // 记录操作日志
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 修改了 {model.Items.Count} 条实际收付记录");

                _DbContext.SaveChanges();

                _Logger.LogDebug("成功修改实际收付记录: {count} 条", model.Items.Count);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改实际收付记录时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改实际收付记录时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 删除指定Id的实际收付记录（软删除）。
        /// </summary>
        /// <param name="model">包含要删除记录Id的参数</param>
        /// <returns>删除操作的结果</returns>
        /// <response code="200">操作成功，记录已被标记为删除。</response>  
        /// <response code="400">记录已被删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的实际收付记录不存在。</response>  
        /// <response code="409">存在关联数据，无法删除。</response>  
        [HttpDelete]
        public ActionResult<RemoveActualFinancialTransactionReturnDto> RemoveActualFinancialTransaction(
            RemoveActualFinancialTransactionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new RemoveActualFinancialTransactionReturnDto();

            try
            {
                var id = model.Id;
                var item = _DbContext.ActualFinancialTransactions.Find(id);
                if (item == null)
                {
                    _Logger.LogWarning("尝试删除不存在的实际收付记录: {id}", id);
                    return NotFound("指定ID的实际收付记录不存在");
                }

                if (item.IsDelete)
                {
                    _Logger.LogWarning("尝试删除已被删除的实际收付记录: {id}", id);
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "记录已被删除";
                    return BadRequest(result.DebugMessage);
                }

                // 🔧 多租户数据隔离验证 - 确保用户只能删除自己租户的数据
                if (!_AccountManager.IsAdmin(context.User))
                {
                    // 通过ParentId获取关联的结算单，但由于PlInvoices没有OrgId字段，
                    // 我们通过其他方式验证权限：检查创建者是否属于同一组织
                    if (item.ParentId.HasValue)
                    {
                        var parentInvoice = _DbContext.PlInvoicess.AsNoTracking()
                            .FirstOrDefault(p => p.Id == item.ParentId.Value);
                        
                        if (parentInvoice != null && parentInvoice.CreateBy.HasValue)
                        {
                            // 验证结算单的创建者是否与当前用户属于同一组织
                            var creator = _DbContext.Accounts.AsNoTracking()
                                .FirstOrDefault(a => a.Id == parentInvoice.CreateBy.Value);
                            
                            if (creator != null && creator.OrgId != context.User.OrgId)
                            {
                                _Logger.LogWarning("用户 {userId} 尝试删除不属于其租户的实际收付记录: {id}", 
                                    context.User.Id, id);
                                result.HasError = true;
                                result.ErrorCode = 403;
                                result.DebugMessage = "权限不足，无法删除此记录";
                                return result;
                            }
                        }
                    }
                }

                // 🔧 检查是否存在业务关联约束
                if (!CheckCanDelete(item.Id))
                {
                    _Logger.LogWarning("实际收付记录 {id} 存在业务关联，无法删除", id);
                    result.HasError = true;
                    result.ErrorCode = 409;
                    result.DebugMessage = "记录存在业务关联，无法删除";
                    return result;
                }

                // 执行软删除
                _EntityManager.Remove(item);

                // 🔧 正确创建系统日志实体 - 确保包含主键ID和适当的ActionId长度
                var systemLog = new OwSystemLog
                {
                    Id = Guid.NewGuid(), // 必须设置主键ID
                    OrgId = context.User.OrgId,
                    // 🔧 缩短ActionId避免数据库字段长度限制（64字符）
                    // 原格式：Delete.ActualFinancialTransaction.{GUID} (约71字符)
                    // 新格式：Del.ActFinTrans.{GUID前8位} (约24字符)
                    ActionId = $"Del.ActFinTrans.{item.Id.ToString()[..8]}",
                    ExtraGuid = context.User.Id,
                    WorldDateTime = OwHelper.WorldNow,
                };
                _DbContext.OwSystemLogs.Add(systemLog);

                // 记录应用日志
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 删除了实际收付记录ID:{item.Id}");

                _DbContext.SaveChanges();

                _Logger.LogInformation("成功删除实际收付记录: {id}, 操作用户: {userId}", id, context.User.Id);
            }
            catch (DbUpdateException dbEx)
            {
                _Logger.LogError(dbEx, "删除实际收付记录时发生数据库错误，记录ID: {id}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = "删除记录时发生数据库错误，请检查数据完整性约束";
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除实际收付记录时发生未知错误，记录ID: {id}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除实际收付记录时发生错误: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 恢复指定的被删除实际收付记录。
        /// </summary>
        /// <param name="model">包含要恢复记录Id的参数</param>
        /// <returns>恢复操作的结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">指定实体的Id不存在或记录未被删除。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost("Restore")]
        public ActionResult<RestoreActualFinancialTransactionReturnDto> RestoreActualFinancialTransaction(
            RestoreActualFinancialTransactionParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();

            var result = new RestoreActualFinancialTransactionReturnDto();

            try
            {
                if (!_EntityManager.Restore<ActualFinancialTransaction>(model.Id))
                {
                    var errResult = new StatusCodeResult(OwHelper.GetLastError());
                    return errResult;
                }

                // 记录操作日志
                _SqlAppLogger.LogGeneralInfo($"用户 {context.User.Id} 恢复了实际收付记录ID:{model.Id}");

                _DbContext.SaveChanges();

                _Logger.LogDebug("成功恢复实际收付记录: {id}", model.Id);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "恢复实际收付记录时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"恢复实际收付记录时发生错误: {ex.Message}";
            }

            return result;
        }

        #endregion 基础CRUD操作

        #region 私有辅助方法

        /// <summary>
        /// 检查实际收付记录是否可以删除
        /// </summary>
        /// <param name="transactionId">收付记录ID</param>
        /// <returns>true表示可以删除，false表示存在约束</returns>
        private bool CheckCanDelete(Guid transactionId)
        {
            try
            {
                // 🔧 检查是否存在业务关联约束
                // 这里可以根据实际业务规则添加具体的约束检查
                // 例如：检查是否被审计记录引用、是否在特定状态下等

                // 当前实现：允许删除（软删除模式下通常可以删除）
                // 如果将来有具体的业务约束，可以在这里添加检查逻辑
                return true;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "检查删除约束时发生错误，记录ID: {id}", transactionId);
                return false; // 发生错误时，出于安全考虑，不允许删除
            }
        }

        #endregion 私有辅助方法
    }
}