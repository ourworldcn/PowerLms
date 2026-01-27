using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsServer.Helpers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 角色管理控制器，基于角色归属唯一性原则实现权限管理
    /// </summary>
    public class RoleController : PlControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="accountManager"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="entityManager"></param>
        /// <param name="roleManager"></param>
        /// <param name="orgManager"></param>
        /// <param name="logger"></param>
        public RoleController(PowerLmsUserDbContext dbContext, AccountManager accountManager,
            IServiceProvider serviceProvider, IMapper mapper, EntityManager entityManager,
            RoleManager roleManager, OrgManager<PowerLmsUserDbContext> orgManager, ILogger<RoleController> logger)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _RoleManager = roleManager;
            _OrgManager = orgManager;
            _Logger = logger;
        }

        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly RoleManager _RoleManager;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly ILogger<RoleController> _Logger;

        /// <summary>
        /// 获取角色列表，支持分页和条件过滤。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持两种格式：
        /// 1. 直接使用PlRole的属性名作为键进行过滤，如Name、OrgId等
        /// 2. 使用"AccountRole.属性名"前缀进行关联过滤，如AccountRole.UserId
        /// 对于字符串类型会进行包含查询，其他类型进行精确匹配。范围查询格式为"min,max"。</param>
        /// <returns>角色列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">条件格式错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetAllPlRoleReturnDto> GetAllPlRole([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetAllPlRoleReturnDto();

            try
            {
                IQueryable<PlRole> query;

                if (context.User.IsSuperAdmin)
                {
                    query = _DbContext.PlRoles.AsNoTracking();
                }
                else if (context.User.IsMerchantAdmin)
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    var allRoles = _RoleManager.GetOrLoadRolesByMerchantId(merchantId.Value);
                    
                    var orgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToHashSet();
                    orgIds.Add(merchantId.Value);

                    query = _DbContext.PlRoles
                        .Where(r => r.OrgId.HasValue && orgIds.Contains(r.OrgId.Value))
                        .AsNoTracking();
                }
                else
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    var roles = _RoleManager.GetOrLoadRolesByMerchantId(merchantId.Value);
                    
                    var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                    if (currentCompany == null)
                    {
                        result.Result = new List<PlRole>();
                        return result;
                    }
                    
                    var orgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id).ToHashSet();
                    orgIds.Add(merchantId.Value);

                    query = _DbContext.PlRoles
                        .Where(r => r.OrgId.HasValue && orgIds.Contains(r.OrgId.Value))
                        .AsNoTracking();
                }

                query = query.OrderBy(model.OrderFieldName, model.IsDesc);

                if (conditional != null && conditional.Count > 0)
                {
                    const string accountRolePrefix = "AccountRole.";
                    var accountRoleConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var roleConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var condition in conditional)
                    {
                        if (condition.Key.StartsWith(accountRolePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string propName = condition.Key.Substring(accountRolePrefix.Length);
                            accountRoleConditions.Add(propName, condition.Value);
                        }
                        else
                        {
                            roleConditions.Add(condition.Key, condition.Value);
                        }
                    }

                    if (roleConditions.Count > 0)
                    {
                        var filteredQuery = QueryHelper.GenerateWhereAnd(query, roleConditions);
                        if (filteredQuery == null)
                        {
                            return BadRequest(OwHelper.GetLastErrorMessage());
                        }
                        query = filteredQuery;
                    }

                    if (accountRoleConditions.Count > 0)
                    {
                        var accountRoleQuery = _DbContext.PlAccountRoles.AsQueryable();
                        var filteredAccountRoleQuery = QueryHelper.GenerateWhereAnd(accountRoleQuery, accountRoleConditions);

                        if (filteredAccountRoleQuery == null)
                        {
                            return BadRequest(OwHelper.GetLastErrorMessage());
                        }

                        var roleIds = filteredAccountRoleQuery.Select(ar => ar.RoleId).Distinct();
                        query = query.Where(role => roleIds.Contains(role.Id));
                    }
                }

                var prb = _EntityManager.GetAll(query, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                // ✅ 只保留异常日志
                _Logger.LogError(ex, "获取角色列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取角色列表失败: {ex.Message}";
                return StatusCode(500, result);
            }

            return result;
        }
    }
}