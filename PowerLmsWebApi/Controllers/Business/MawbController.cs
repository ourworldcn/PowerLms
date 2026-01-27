/*
 * 项目：PowerLms货运物流管理系统 | 模块：主单领用登记API
 * 功能：主单（MAWB）领用登记与台账管理控制器
 * 技术要点：
 *   - 主单号工具方法（校验、生成）
 *   - 主单领入/领出CRUD操作
 *   - 台账查询与管理
 *   - 业务关联查询
 *   - 权限验证与多租户隔离
 * 作者：zc | 创建：2025-01 | 修改：2025-01-17 初始创建
 */
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PowerLmsWebApi.Controllers.Business
{
    /// <summary>
    /// 主单（MAWB）领用登记与台账管理控制器。
    /// 路由前缀：/api/Mawb
    /// </summary>
    public class MawbController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public MawbController(
            AccountManager accountManager,
            IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext,
            MawbManager mawbManager,
            AuthorizationManager authorizationManager,
            ILogger<MawbController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _MawbManager = mawbManager;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly MawbManager _MawbManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ILogger<MawbController> _Logger;

        #region 主单号工具接口

        /// <summary>
        /// 校验主单号格式与校验位。
        /// </summary>
        /// <param name="model">校验参数</param>
        /// <returns>校验结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<ValidateMawbNoReturnDto> ValidateMawbNo(ValidateMawbNoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new ValidateMawbNoReturnDto();

            try
            {
                var (isValid, errorMsg) = _MawbManager.ValidateMawbNo(model.MawbNo);
                result.IsValid = isValid;
                result.ErrorMsg = errorMsg;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "校验主单号时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"校验主单号时发生异常: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 生成下一个主单号。
        /// </summary>
        /// <param name="model">生成参数</param>
        /// <returns>下一个主单号</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<GenerateNextMawbNoReturnDto> GenerateNextMawbNo(GenerateNextMawbNoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GenerateNextMawbNoReturnDto();

            try
            {
                result.NextMawbNo = _MawbManager.GenerateNextMawbNo(model.Prefix, model.CurrentNo);
                return result;
            }
            catch (ArgumentException ex)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = ex.Message;
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "生成下一个主单号时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"生成主单号时发生异常: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 批量生成主单号序列。
        /// </summary>
        /// <param name="model">批量生成参数</param>
        /// <returns>主单号列表</returns>
        /// <remarks>
        /// <strong>重要说明：</strong>
        /// - 前端传入的StartNo是<strong>本次批量生成的第一个号</strong>（不是已存在的号）
        /// - 返回的主单号序列<strong>从该号开始</strong>，包含该号本身，共Count个
        /// - 例如：传入Prefix="999", StartNo="12345670", Count=3
        /// - 返回：["999-12345670", "999-12345681", "999-12345692"]
        /// - <strong>注意：</strong>返回结果<strong>包含</strong>传入的"999-12345670"
        /// - <strong>性能优化：</strong>不查询数据库，直接基于输入号生成序列
        /// </remarks>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<BatchGenerateMawbNosReturnDto> BatchGenerateMawbNos(BatchGenerateMawbNosParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new BatchGenerateMawbNosReturnDto();

            try
            {
                result.MawbNos = _MawbManager.BatchGenerateMawbNos(model.Prefix, model.StartNo, model.Count);
                return result;
            }
            catch (ArgumentException ex)
            {
                result.HasError = true;
                result.ErrorCode = 400;
                result.DebugMessage = ex.Message;
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量生成主单号时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"批量生成主单号时发生异常: {ex.Message}";
                return result;
            }
        }

        #endregion 主单号工具接口

        #region 主单领入接口

        /// <summary>
        /// 获取全部主单领入记录。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">筛选条件（支持OrgId、AirlineId、TransferAgentId、MawbNo、RegisterDate等）</param>
        /// <returns>主单领入列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllMawbInboundReturnDto> GetAllMawbInbound([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            // 权限验证：D0.14.2（查看登记）
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.2"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);

            var result = new GetAllMawbInboundReturnDto();

            try
            {
                var query = _DbContext.PlEaMawbInbounds.AsQueryable();

                // 应用筛选条件
                if (conditional != null)
                {
                    query = QueryHelper.GenerateWhereAnd(query, conditional);
                }

                // 应用排序
                query = query.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                // 分页查询
                var total = query.Count();
                var items = query.Skip(model.StartIndex).Take(model.Count > 0 ? model.Count : total).ToList();

                result.Total = total;
                result.Result = items;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询主单领入列表时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询主单领入列表时发生异常: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 批量新增主单领入记录。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddMawbInboundReturnDto> AddMawbInbound(AddMawbInboundParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.1"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            var result = new AddMawbInboundReturnDto();
            try
            {
                if (model.MawbNos == null || model.MawbNos.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "主单号列表不能为空";
                    return BadRequest(result);
                }
                var (successCount, failureCount, failureDetails) = _MawbManager.CreateInbound(
                    model.SourceType,
                    model.AirlineId,
                    model.TransferAgentId,
                    model.RegisterDate,
                    model.Remark,
                    model.MawbNos,
                    context.User.OrgId.Value,
                    context.User.Id
                );
                result.SuccessCount = successCount;
                result.FailureCount = failureCount;
                result.FailureDetails = failureDetails;
                if (successCount == 0 && failureCount > 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "所有主单号创建失败";
                }
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "批量创建主单领入记录时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 修改主单领入记录。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的记录不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyMawbInboundReturnDto> ModifyMawbInbound(ModifyMawbInboundParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.3"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            var result = new ModifyMawbInboundReturnDto();
            try
            {
                var success = _MawbManager.UpdateInbound(
                    model.Id,
                    model.AirlineId,
                    model.TransferAgentId,
                    model.RegisterDate,
                    model.Remark,
                    context.User.OrgId.Value
                );
                if (!success)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "主单领入记录不存在或无权访问";
                    return NotFound(result);
                }
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改主单领入记录时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 删除主单领入记录。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的记录不存在。</response>  
        /// <response code="409">资源冲突（如主单已领出，不能删除）。</response>  
        [HttpDelete]
        public ActionResult<RemoveMawbInboundReturnDto> RemoveMawbInbound(RemoveMawbInboundParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.4"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            var result = new RemoveMawbInboundReturnDto();
            try
            {
                var (success, error) = _MawbManager.DeleteInbound(model.Id, context.User.OrgId.Value);
                if (!success)
                {
                    result.HasError = true;
                    if (error.Contains("不存在") || error.Contains("无权访问"))
                    {
                        result.ErrorCode = 404;
                        result.DebugMessage = error;
                        return NotFound(result);
                    }
                    if (error.Contains("已领出"))
                    {
                        result.ErrorCode = 409;
                        result.DebugMessage = error;
                        return StatusCode((int)HttpStatusCode.Conflict, result);
                    }
                    result.ErrorCode = 400;
                    result.DebugMessage = error;
                    return BadRequest(result);
                }
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除主单领入记录时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除失败: {ex.Message}";
                return result;
            }
        }

        #endregion 主单领入接口

        #region 主单领出接口

        /// <summary>
        /// 获取全部主单领出记录。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">筛选条件（支持OrgId、AgentId、MawbNo、IssueDate等）</param>
        /// <returns>主单领出列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllMawbOutboundReturnDto> GetAllMawbOutbound([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            // 权限验证：D0.14.6（查看领用）
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.6"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);

            var result = new GetAllMawbOutboundReturnDto();

            try
            {
                var query = _DbContext.PlEaMawbOutbounds.AsQueryable();

                // 应用筛选条件
                if (conditional != null)
                {
                    query = QueryHelper.GenerateWhereAnd(query, conditional);
                }

                // 应用排序
                query = query.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                // 分页查询
                var total = query.Count();
                var items = query.Skip(model.StartIndex).Take(model.Count > 0 ? model.Count : total).ToList();

                result.Total = total;
                result.Result = items;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询主单领出列表时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询主单领出列表时发生异常: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 新增主单领出记录。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">主单号不存在（需先领入）。</response>  
        /// <response code="409">资源冲突（如主单已领出）。</response>  
        [HttpPost]
        public ActionResult<AddMawbOutboundReturnDto> AddMawbOutbound(AddMawbOutboundParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.5"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            var result = new AddMawbOutboundReturnDto();
            try
            {
                var (success, error, id) = _MawbManager.CreateOutbound(
                    model.MawbNo,
                    model.AgentId.Value,
                    model.RecipientName,
                    model.IssueDate,
                    model.PlannedReturnDate,
                    model.Remark,
                    context.User.OrgId.Value,
                    context.User.Id
                );
                if (!success)
                {
                    result.HasError = true;
                    if (error.Contains("不存在") || error.Contains("先进行领入"))
                    {
                        result.ErrorCode = 404;
                        result.DebugMessage = error;
                        return NotFound(result);
                    }
                    if (error.Contains("已领出") || error.Contains("重复"))
                    {
                        result.ErrorCode = 409;
                        result.DebugMessage = error;
                        return StatusCode((int)HttpStatusCode.Conflict, result);
                    }
                    result.ErrorCode = 400;
                    result.DebugMessage = error;
                    return BadRequest(result);
                }
                result.Id = id.Value;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建主单领出记录时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 修改主单领出记录。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的记录不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyMawbOutboundReturnDto> ModifyMawbOutbound(ModifyMawbOutboundParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.7"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            var result = new ModifyMawbOutboundReturnDto();
            try
            {
                var success = _MawbManager.UpdateOutbound(
                    model.Id,
                    model.AgentId,
                    model.RecipientName,
                    model.IssueDate,
                    model.PlannedReturnDate,
                    model.ActualReturnDate,
                    model.Remark,
                    context.User.OrgId.Value
                );
                if (!success)
                {
                    result.HasError = true;
                    result.ErrorCode = 404;
                    result.DebugMessage = "主单领出记录不存在或无权访问";
                    return NotFound(result);
                }
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改主单领出记录时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 删除主单领出记录。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的记录不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveMawbOutboundReturnDto> RemoveMawbOutbound(RemoveMawbOutboundParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.8"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            var result = new RemoveMawbOutboundReturnDto();
            try
            {
                var (success, error) = _MawbManager.DeleteOutbound(model.Id, context.User.OrgId.Value);
                if (!success)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = error;
                    return BadRequest(result);
                }
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除主单领出记录时发生异常");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除失败: {ex.Message}";
                return result;
            }
        }

        #endregion 主单领出接口

        #region 台账查询接口

        /// <summary>
        /// 查询主单台账列表（含业务回查）。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">筛选条件（支持OrgId、UseStatus、MawbNo等）</param>
        /// <returns>主单台账列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetMawbLedgerListReturnDto> GetLedgerList([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            // 权限验证：D0.14.2（查看登记，复用）
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.2"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);

            var result = new GetMawbLedgerListReturnDto();

            // TODO: 实现台账查询逻辑
            // 1. Join PlEaMawbInbound获取领入信息
            // 2. Join PlEaMawbOutbound获取领出信息
            // 3. 根据MawbNo关联业务单据获取业务信息

            result.HasError = true;
            result.ErrorCode = 501;
            result.DebugMessage = "功能开发中";
            return result;
        }

        /// <summary>
        /// 获取未使用主单列表（供业务单据选择）。
        /// </summary>
        /// <param name="model">令牌参数</param>
        /// <returns>未使用主单列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetUnusedMawbListReturnDto> GetUnusedMawbList([FromQuery] TokenDtoBase model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            // 权限验证：D0.14.2（查看登记，复用）
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.2"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);

            var result = new GetUnusedMawbListReturnDto();

            // TODO: 实现未使用主单查询逻辑
            // 筛选：UseStatus=0（未使用）

            result.HasError = true;
            result.ErrorCode = 501;
            result.DebugMessage = "功能开发中";
            return result;
        }

        /// <summary>
        /// 作废主单。
        /// </summary>
        /// <param name="model">作废参数</param>
        /// <returns>作废结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<MarkMawbAsVoidReturnDto> MarkAsVoid(MarkMawbAsVoidParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            // 权限验证：D0.14.3（编辑登记，复用）
            if (!_AuthorizationManager.Demand(out string errorMessage, "D0.14.3"))
                return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);

            var result = new MarkMawbAsVoidReturnDto();

            // TODO: 实现作废逻辑
            // 1. 检查是否已使用
            // 2. 更新UseStatus=2
            // 3. 记录作废原因到Remark

            result.HasError = true;
            result.ErrorCode = 501;
            result.DebugMessage = "功能开发中";
            return result;
        }

        #endregion 台账查询接口

        #region 业务关联接口

        /// <summary>
        /// 根据主单号查询委托信息。
        /// </summary>
        /// <param name="mawbNo">主单号</param>
        /// <param name="token">令牌（从查询参数获取）</param>
        /// <returns>业务委托信息</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet("{mawbNo}")]
        public ActionResult<GetJobInfoByMawbNoReturnDto> GetJobInfo(string mawbNo, [FromQuery] Guid token)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetJobInfoByMawbNoReturnDto();

            // TODO: 实现业务关联查询逻辑
            // 根据MawbNo关联PlJob、DocAirExport获取业务信息

            result.HasError = true;
            result.ErrorCode = 501;
            result.DebugMessage = "功能开发中";
            return result;
        }

        #endregion 业务关联接口
    }
}
