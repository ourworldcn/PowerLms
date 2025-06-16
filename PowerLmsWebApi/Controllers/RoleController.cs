using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OW.Data;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 角色控制器，处理与用户角色相关的API请求。
    /// </summary>
    public class RoleController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public RoleController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext,
            IMapper mapper, EntityManager entityManager)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _Mapper = mapper;
            _EntityManager = entityManager;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;

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
        [HttpGet]
        public ActionResult<GetAllPlRoleReturnDto> GetAllPlRole([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlRoleReturnDto();

            // 获取基础查询
            var dbSet = _DbContext.PlRoles;
            var query = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

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
            return result;
        }
    }


}