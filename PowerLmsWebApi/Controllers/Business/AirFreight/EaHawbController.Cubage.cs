/*
 * 项目：PowerLms | 模块：空运出口分单控制器
 * 功能：空运出口分单委托明细的CRUD操作
 * 技术要点：分部类实现、主子表关联
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
    /// 空运出口分单控制器 - 委托明细部分。
    /// </summary>
    public partial class EaHawbController
    {
        #region 空运出口分单委托明细CRUD

        /// <summary>
        /// 获取全部空运出口分单委托明细。
        /// </summary>
        /// <param name="model">分页查询参数</param>
        /// <param name="conditional">查询条件字典。已支持通用查询——除个别涉及敏感信息字段外，所有实体字段都可作为条件。</param>
        /// <returns>空运出口分单委托明细列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetAllEaHawbCubageReturnDto> GetAllEaHawbCubage([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.2"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new GetAllEaHawbCubageReturnDto();
            try
            {
                var dbSet = _DbContext.EaHawbCubages;
                var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
                coll = QueryHelper.GenerateWhereAnd(coll, conditional);
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "查询空运出口分单委托明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"查询空运出口分单委托明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 增加新空运出口分单委托明细。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>新增结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        [HttpPost]
        public ActionResult<AddEaHawbCubageReturnDto> AddEaHawbCubage(AddEaHawbCubageParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.1"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddEaHawbCubageReturnDto();
            try
            {
                var entity = model.EaHawbCubage;
                entity.GenerateNewId();
                _DbContext.EaHawbCubages.Add(entity);
                _DbContext.SaveChanges();
                result.Id = entity.Id;
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建空运出口分单委托明细时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"创建空运出口分单委托明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 修改空运出口分单委托明细信息。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的空运出口分单委托明细不存在。</response>
        [HttpPut]
        public ActionResult<ModifyEaHawbCubageReturnDto> ModifyEaHawbCubage(ModifyEaHawbCubageParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.3"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyEaHawbCubageReturnDto();
            try
            {
                if (!_EntityManager.Modify(new[] { model.EaHawbCubage }))
                    return NotFound($"未找到ID为{model.EaHawbCubage.Id}的空运出口分单委托明细");
                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改空运出口分单委托明细时发生错误，ID={Id}", model.EaHawbCubage.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改空运出口分单委托明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 删除指定Id的空运出口分单委托明细。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>
        /// <response code="401">无效令牌。</response>
        /// <response code="403">权限不足。</response>
        /// <response code="404">指定Id的空运出口分单委托明细不存在。</response>
        [HttpDelete]
        public ActionResult<RemoveEaHawbCubageReturnDto> RemoveEaHawbCubage(RemoveEaHawbCubageParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "D0.16.4"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveEaHawbCubageReturnDto();
            try
            {
                var item = _DbContext.EaHawbCubages.Find(model.Id);
                if (item is null)
                    return NotFound($"未找到ID为{model.Id}的空运出口分单委托明细");
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();
                return result;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除空运出口分单委托明细时发生错误，ID={Id}", model.Id);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除空运出口分单委托明细时发生错误: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        #endregion 空运出口分单委托明细CRUD
    }
}
