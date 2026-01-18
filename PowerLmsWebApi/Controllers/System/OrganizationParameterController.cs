/*
 * 项目：PowerLmsWebApi | 模块：机构参数控制器
 * 功能：机构参数CRUD操作
 * 技术要点：独立权限控制、OrgId作为主键
 * 作者：zc | 创建：2025-01 | 修改：2025-01-27 简化为基础CRUD功能
 */
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;
namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 机构参数控制器，提供机构级别参数配置功能。
    /// </summary>
    public class OrganizationParameterController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationParameterController(
            AccountManager accountManager, 
            IServiceProvider serviceProvider, 
            PowerLmsUserDbContext dbContext,
            OrgManager<PowerLmsUserDbContext> orgManager, 
            IMapper mapper, 
            EntityManager entityManager, 
            ILogger<OrganizationParameterController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrgManager = orgManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _Logger = logger;
        }
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly ILogger<OrganizationParameterController> _Logger;
        #region 机构参数CRUD操作
        /// <summary>
        /// 获取机构参数列表。支持分页和条件过滤。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件，支持通用查询模式</param>
        /// <returns>机构参数列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllOrganizationParameterReturnDto> GetAllOrganizationParameter(
            [FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            var result = new GetAllOrganizationParameterReturnDto();
            // 获取用户有权限访问的机构ID列表
            var allowedOrgIds = GetOrgIds(context.User, _OrgManager);
            var dbSet = _DbContext.PlOrganizationParameters;
            var query = dbSet.Where(p => allowedOrgIds.Contains(p.OrgId))
                            .OrderBy(model.OrderFieldName, model.IsDesc)
                            .AsNoTracking();
            // 应用条件过滤
            if (conditional != null && conditional.Count > 0)
            {
                query = EfHelper.GenerateWhereAnd(query, conditional);
                if (query == null)
                {
                    return BadRequest(OwHelper.GetLastErrorMessage());
                }
            }
            try
            {
                var prb = _EntityManager.GetAll(query, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取机构参数列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = ex.Message;
            }
            return result;
        }
        /// <summary>
        /// 新增机构参数。
        /// </summary>
        /// <param name="model">新增参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddOrganizationParameterReturnDto> AddOrganizationParameter(
            AddOrganizationParameterParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            var result = new AddOrganizationParameterReturnDto();
            // 验证机构是否存在
            if (!_DbContext.PlOrganizations.Any(o => o.Id == model.Item.OrgId))
            {
                return BadRequest($"机构 {model.Item.OrgId} 不存在");
            }
            // 验证是否已存在参数
            if (_DbContext.PlOrganizationParameters.Any(p => p.OrgId == model.Item.OrgId))
            {
                return BadRequest($"机构 {model.Item.OrgId} 已存在参数配置");
            }
            // 验证用户权限
            var allowedOrgIds = GetOrgIds(context.User, _OrgManager);
            if (!allowedOrgIds.Contains(model.Item.OrgId))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "无权限为该机构添加参数");
            }
            try
            {
                // 设置默认账期为当前年月
                if (string.IsNullOrEmpty(model.Item.CurrentAccountingPeriod))
                {
                    model.Item.CurrentAccountingPeriod = DateTime.Now.ToString("yyyyMM");
                }
                _DbContext.PlOrganizationParameters.Add(model.Item);
                _DbContext.SaveChanges();
                result.OrgId = model.Item.OrgId;
                _Logger.LogInformation("用户 {userId} 为机构 {orgId} 创建了参数配置", 
                    context.User.Id, model.Item.OrgId);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "创建机构参数时发生错误，机构ID: {orgId}", model.Item.OrgId);
                return BadRequest($"创建机构参数失败: {ex.Message}");
            }
            return result;
        }
        /// <summary>
        /// 修改机构参数。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定的机构参数不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyOrganizationParameterReturnDto> ModifyOrganizationParameter(
            ModifyOrganizationParameterParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            var result = new ModifyOrganizationParameterReturnDto();
            // 验证用户权限
            var allowedOrgIds = GetOrgIds(context.User, _OrgManager);
            foreach (var item in model.Items)
            {
                if (!allowedOrgIds.Contains(item.OrgId))
                {
                    return StatusCode((int)HttpStatusCode.Forbidden, 
                        $"无权限修改机构 {item.OrgId} 的参数");
                }
            }
            try
            {
                foreach (var item in model.Items)
                {
                    var existing = _DbContext.PlOrganizationParameters.Find(item.OrgId);
                    if (existing == null)
                    {
                        return NotFound($"机构 {item.OrgId} 的参数不存在");
                    }
                    // 更新所有字段
                    existing.CurrentAccountingPeriod = item.CurrentAccountingPeriod;
                    existing.BillHeader1 = item.BillHeader1;
                    existing.BillHeader2 = item.BillHeader2;
                    existing.BillFooter = item.BillFooter;
                }
                _DbContext.SaveChanges();
                _Logger.LogInformation("用户 {userId} 修改了机构参数，涉及机构: {orgIds}", 
                    context.User.Id, string.Join(",", model.Items.Select(i => i.OrgId)));
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "修改机构参数时发生错误");
                return BadRequest($"修改机构参数失败: {ex.Message}");
            }
            return result;
        }
        /// <summary>
        /// 删除机构参数。慎用！
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>操作结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定的机构参数不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveOrganizationParameterReturnDto> RemoveOrganizationParameter(
            RemoveOrganizationParameterParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) 
                return Unauthorized();
            var result = new RemoveOrganizationParameterReturnDto();
            var parameter = _DbContext.PlOrganizationParameters.Find(model.OrgId);
            if (parameter == null)
            {
                return NotFound("指定的机构参数不存在");
            }
            // 验证用户权限
            var allowedOrgIds = GetOrgIds(context.User, _OrgManager);
            if (!allowedOrgIds.Contains(parameter.OrgId))
            {
                return StatusCode((int)HttpStatusCode.Forbidden, "无权限删除该机构参数");
            }
            try
            {
                _DbContext.PlOrganizationParameters.Remove(parameter);
                _DbContext.SaveChanges();
                _Logger.LogInformation("用户 {userId} 删除了机构 {orgId} 的参数配置", 
                    context.User.Id, parameter.OrgId);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "删除机构参数时发生错误，机构ID: {orgId}", model.OrgId);
                return BadRequest($"删除机构参数失败: {ex.Message}");
            }
            return result;
        }
        #endregion 机构参数CRUD操作
        #region 私有辅助方法
        /// <summary>
        /// 为指定机构创建默认参数。
        /// </summary>
        /// <param name="orgId">机构ID</param>
        /// <returns>创建的参数实体，如果机构不存在则返回null</returns>
        private PlOrganizationParameter CreateDefaultParameterForOrg(Guid orgId)
        {
            var org = _DbContext.PlOrganizations.Find(orgId);
            if (org == null)
            {
                return null;
            }
            var parameter = new PlOrganizationParameter
            {
                OrgId = orgId,
                CurrentAccountingPeriod = DateTime.Now.ToString("yyyyMM"),
                BillHeader1 = org.Name_Name ?? "",
                BillHeader2 = "",
                BillFooter = org.Name_Name ?? ""
            };
            _DbContext.PlOrganizationParameters.Add(parameter);
            _DbContext.SaveChanges();
            _Logger.LogInformation("为机构 {orgId} 自动创建了默认参数配置", orgId);
            return parameter;
        }
        #endregion 私有辅助方法
    }
}