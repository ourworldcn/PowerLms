using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
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
                // 根据用户权限限制查询范围
                IQueryable<PlRole> query;

                if (context.User.IsSuperAdmin)
                {
                    // 超级管理员可以查看所有角色
                    query = _DbContext.PlRoles.AsNoTracking();
                    _Logger.LogDebug("超级管理员查询所有角色");
                }
                else if (context.User.IsMerchantAdmin)
                {
                    // 商户管理员只能查看其所属商户下的角色
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    var allRoles = _RoleManager.GetOrLoadRolesByMerchantId(merchantId.Value);
                    
                    // 获取商户下的所有机构ID
                    var orgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToHashSet();
                    orgIds.Add(merchantId.Value); // 添加商户ID本身

                    query = _DbContext.PlRoles
                        .Where(r => r.OrgId.HasValue && orgIds.Contains(r.OrgId.Value))
                        .AsNoTracking();

                    _Logger.LogDebug("商户管理员查询角色: 商户 {MerchantId} 下找到 {Count} 个相关机构",
                        merchantId, orgIds.Count);
                }
                else
                {
                    // 普通用户只能查看其当前有效机构下的角色
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    var roles = _RoleManager.GetOrLoadRolesByMerchantId(merchantId.Value);
                    
                    // 获取用户当前有效的机构
                    var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                    if (currentCompany == null)
                    {
                        result.Result = new List<PlRole>();
                        _Logger.LogDebug("普通用户未关联任何机构，返回空角色列表");
                        return result;
                    }
                    
                    var orgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id).ToHashSet();
                    orgIds.Add(merchantId.Value); // 支持直接归属商户的角色

                    query = _DbContext.PlRoles
                        .Where(r => r.OrgId.HasValue && orgIds.Contains(r.OrgId.Value))
                        .AsNoTracking();

                    _Logger.LogDebug("普通用户查询角色: 在 {Count} 个有效机构范围内查询",
                        orgIds.Count);
                }

                // 应用排序
                query = query.OrderBy(model.OrderFieldName, model.IsDesc);

                // 处理条件过滤
                if (conditional != null && conditional.Count > 0)
                {
                    // 提取AccountRole过滤条件
                    const string accountRolePrefix = "AccountRole.";
                    var accountRoleConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var roleConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // 分离两种类型的条件
                    foreach (var condition in conditional)
                    {
                        if (condition.Key.StartsWith(accountRolePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            // AccountRole前缀条件
                            string propName = condition.Key.Substring(accountRolePrefix.Length);
                            accountRoleConditions.Add(propName, condition.Value);
                        }
                        else
                        {
                            // 直接的PlRole属性条件
                            roleConditions.Add(condition.Key, condition.Value);
                        }
                    }

                    // 应用PlRole直接属性条件
                    if (roleConditions.Count > 0)
                    {
                        var filteredQuery = EfHelper.GenerateWhereAnd(query, roleConditions);
                        if (filteredQuery == null)
                        {
                            return BadRequest(OwHelper.GetLastErrorMessage());
                        }
                        query = filteredQuery;
                    }

                    // 应用AccountRole关联条件
                    if (accountRoleConditions.Count > 0)
                    {
                        // 构建子查询，获取满足条件的RoleId
                        var accountRoleQuery = _DbContext.PlAccountRoles.AsQueryable();
                        var filteredAccountRoleQuery = EfHelper.GenerateWhereAnd(accountRoleQuery, accountRoleConditions);

                        if (filteredAccountRoleQuery == null)
                        {
                            return BadRequest(OwHelper.GetLastErrorMessage());
                        }

                        // 获取满足条件的RoleId
                        var roleIds = filteredAccountRoleQuery.Select(ar => ar.RoleId).Distinct();

                        // 应用到主查询
                        query = query.Where(role => roleIds.Contains(role.Id));
                    }
                }

                // 获取分页数据
                var prb = _EntityManager.GetAll(query, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);

                _Logger.LogInformation("用户 {UserId} 查询角色列表，返回 {Count} 条记录",
                    context.User.Id, result.Result?.Count ?? 0);
            }
            catch (Exception ex)
            {
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