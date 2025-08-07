using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Caching.Memory;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 组织机构控制器。
    /// </summary>
    public class OrganizationController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext,
            OrgManager<PowerLmsUserDbContext> orgManager, IMapper mapper, EntityManager entityManager, DataDicManager dataManager, AuthorizationManager authorizationManager, ILogger<OrganizationController> logger)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _OrgManager = orgManager;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _DataManager = dataManager;
            _AuthorizationManager = authorizationManager;
            _Logger = logger;
        }

        readonly AccountManager _AccountManager;
        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly DataDicManager _DataManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly ILogger<OrganizationController> _Logger;

        /// <summary>
        /// 获取组织机构。暂不考虑分页。
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rootId">根组织机构的Id。或商户的Id。省略或为null时，对商管将返回该商户下多个组织机构;对一般用户会自动给出当前登录公司及其下属所有机构。
        /// 强行指定的Id，可以获取其省略时的子集，但不能获取到更多数据。</param>
        /// <param name="includeChildren">是否包含子机构。废弃，一定要包含子机构。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public ActionResult<GetOrgReturnDto> GetOrg(Guid token, Guid? rootId, bool includeChildren)
        {
            var result = new GetOrgReturnDto();
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!rootId.HasValue && !context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "只有商管可以获取全商户的机构");
            if (context.User.IsSuperAdmin)   //若是超管
            {
                // 如果没有指定rootId，超管可以查看所有组织机构
                if (!rootId.HasValue)
                {
                    // 获取所有顶级组织机构（没有父级的组织机构）
                    var allTopOrgs = _DbContext.PlOrganizations
                        .Where(o => o.ParentId == null)
                        .Include(o => o.Children)
                        .ToList();

                    result.Result.AddRange(allTopOrgs);
                }
                else
                {
                    // 如果指定了rootId，获取此组织机构及其所有子机构
                    var rootOrg = _DbContext.PlOrganizations
                        .Include(o => o.Children)
                        .FirstOrDefault(o => o.Id == rootId.Value);

                    if (rootOrg == null)
                    {
                        // 检查是否为商户ID
                        var merchantOrgs = _DbContext.PlOrganizations
                            .Where(o => o.MerchantId == rootId.Value && o.ParentId == null)
                            .Include(o => o.Children)
                            .ToList();

                        if (merchantOrgs.Any())
                        {
                            result.Result.AddRange(merchantOrgs);
                        }
                        else
                        {
                            return BadRequest($"找不到指定的组织机构或商户，Id={rootId}");
                        }
                    }
                    else
                    {
                        result.Result.Add(rootOrg);
                    }
                }

                return result;
            }
            var merchantId = _OrgManager.GetMerchantIdByOrgId(context.User.OrgId.Value);
            if (!merchantId.HasValue) return BadRequest("未知的商户Id");

            var allOrgItems = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs;
            if (_OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Merchant is not PlMerchant merch)    //若找不到商户
                return BadRequest("找不到用户所属的商户");
            var orgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs;  //获取其所有机构
            if (rootId.HasValue) //若指定了根机构
            {
                if (!orgs.TryGetValue(rootId.Value, out PlOrganization org) && rootId.Value != merch.Id) return BadRequest($"找不到指定的机构，Id={rootId}");
                if (context.User.IsMerchantAdmin)    //若是商管
                {
                    if (org is not null)
                        result.Result.Add(org);
                    else
                        result.Result.AddRange(orgs.Values.Where(c => c.ParentId is null));
                }
                else //非商管
                {
                    var currCo = _OrgManager.GetCurrentCompanyByUser(context.User);
                    if (currCo is null) return BadRequest("当前用户未登录到一个机构。");
                    if (OwHelper.GetAllSubItemsOfTree(currCo, c => c.Children).FirstOrDefault(c => c.Id == rootId.Value) is not PlOrganization currOrg)
                        return StatusCode((int)HttpStatusCode.Forbidden, "用户只能获取当前登录公司及其子机构");
                    result.Result.Add(currOrg);
                }
            }
            else //若没有指定根
            {
                if (context.User.IsMerchantAdmin)    //若是商管
                {
                    result.Result.AddRange(orgs.Values.Where(c => c.Parent is null));
                }
                else //非商管
                {
                    var currCo = _OrgManager.GetCurrentCompanyByUser(context.User);
                    if (currCo is null) return BadRequest("当前用户未登录到一个机构。");
                    result.Result.Add(currCo);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取组织机构列表，支持分页和条件过滤。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件。支持两种格式：
        /// 1. 直接使用PlOrganization的属性名作为键进行过滤，如Name、Description、MerchantId等
        /// 2. 使用"AccountPlOrganization.属性名"前缀进行关联过滤，如AccountPlOrganization.UserId
        /// 对于字符串类型会进行包含查询，其他类型进行精确匹配。范围查询格式为"min,max"。</param>
        /// <returns>组织机构列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">条件格式错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlOrganizationReturnDto> GetAllPlOrganization([FromQuery] PagingParamsDtoBase model,
            [FromQuery][ModelBinder(typeof(DotKeyDictionaryModelBinder))] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlOrganizationReturnDto();

            // 获取基础查询
            var dbSet = _DbContext.PlOrganizations;
            var query = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // 处理条件过滤
            if (conditional != null && conditional.Count > 0)
            {
                // 提取AccountPlOrganization过滤条件
                const string accountOrgPrefix = "AccountPlOrganization.";
                var accountOrgConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var orgConditions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // 分离两种类型的条件
                foreach (var condition in conditional)
                {
                    if (condition.Key.StartsWith(accountOrgPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // AccountPlOrganization前缀条件
                        string propName = condition.Key.Substring(accountOrgPrefix.Length);
                        accountOrgConditions.Add(propName, condition.Value);
                    }
                    else
                    {
                        // 直接的PlOrganization属性条件
                        orgConditions.Add(condition.Key, condition.Value);
                    }
                }

                // 应用PlOrganization直接属性条件
                if (orgConditions.Count > 0)
                {
                    var filteredQuery = EfHelper.GenerateWhereAnd(query, orgConditions);
                    if (filteredQuery == null)
                    {
                        return BadRequest(OwHelper.GetLastErrorMessage());
                    }
                    query = filteredQuery;
                }

                // 应用AccountPlOrganization关联条件
                if (accountOrgConditions.Count > 0)
                {
                    // 构建子查询，获取满足条件的OrgId
                    var accountOrgQuery = _DbContext.AccountPlOrganizations.AsQueryable();
                    var filteredAccountOrgQuery = EfHelper.GenerateWhereAnd(accountOrgQuery, accountOrgConditions);

                    if (filteredAccountOrgQuery == null)
                    {
                        return BadRequest(OwHelper.GetLastErrorMessage());
                    }

                    // 获取满足条件的OrgId
                    var orgIds = filteredAccountOrgQuery.Select(ao => ao.OrgId).Distinct();

                    // 应用到主查询
                    query = query.Where(org => orgIds.Contains(org.Id));
                }
            }
            try
            {
                // 获取分页数据
                var prb = _EntityManager.GetAll(query, model.StartIndex, model.Count);
                _Mapper.Map(prb, result, opt =>
                {
                    // 设置忽略的属性
                    opt.Items["IgnoreProps"] = new HashSet<string> { nameof(PlOrganization.Parent), nameof(PlOrganization.Children) };
                });
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "获取组织机构列表时发生错误");
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 增加一个组织机构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddOrgReturnDto> AddOrg(AddOrgParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddOrgReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.PlOrganizations.Add(model.Item);
            try
            {
                _DbContext.SaveChanges();
                result.Id = id;

                // 使相关商户缓存失效
                // 如果是顶层组织机构（ParentId为null），直接使用其MerchantId
                if (model.Item.ParentId == null && model.Item.MerchantId.HasValue)
                {
                    _OrgManager.InvalidateOrgCaches(model.Item.MerchantId.Value);
                }
                // 如果有父级，则通过父级查找关联的商户ID
                else if (model.Item.ParentId.HasValue)
                {
                    var merchantId = _OrgManager.GetMerchantIdByOrgId(model.Item.Id);
                    if (merchantId.HasValue)
                    {
                        _OrgManager.InvalidateOrgCaches(merchantId.Value);
                    }
                }

                if (model.IsCopyDataDic) //若需要复制字典
                {
                    var r = CopyDataDic(new CopyDataDicParamsDto { Token = model.Token, Id = id });

                    // 复制全局财务科目设置(OrgId=null)到新组织机构
                    var globalSubjectConfigs = _DbContext.SubjectConfigurations
                        .Where(c => c.OrgId == null && !c.IsDelete)
                        .AsNoTracking()
                        .ToList();

                    foreach (var globalConfig in globalSubjectConfigs)
                    {
                        var newConfig = new SubjectConfiguration
                        {
                            Id = Guid.NewGuid(),
                            OrgId = id, // 设置为新组织机构ID
                            Code = globalConfig.Code,
                            SubjectNumber = globalConfig.SubjectNumber,
                            DisplayName = globalConfig.DisplayName,
                            Remark = globalConfig.Remark,
                            IsDelete = false, // 新创建的不应被标记删除
                            CreateBy = context.User?.Id,
                            CreateDateTime = OwHelper.WorldNow
                        };
                        _DbContext.SubjectConfigurations.Add(newConfig);
                    }

                    if (globalSubjectConfigs.Any())
                    {
                        _DbContext.SaveChanges(); // 保存财务科目设置
                    }
                }
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
            return result;
        }

        /// <summary>
        /// 修改已有组织机构。不能修改父子关系。请使用 AddOrgRelation 和 RemoveOrgRelation修改。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyOrgReturnDto> ModifyOrg(ModifyOrgParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized(OwHelper.GetLastErrorMessage());

            string err;
            if (!_AuthorizationManager.Demand(out err, "B.1"))
                return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new ModifyOrgReturnDto();
            Dictionary<Guid, List<PlOrganization>> orgDict = new();

            foreach (var org in model.Items)
            {
                var no = _DbContext.PlOrganizations.Find(org.Id);
                if (no is null)
                {
                    _Logger.LogWarning("修改组织机构失败：找不到ID为 {orgId} 的组织机构", org.Id);
                    return NotFound($"找不到ID为 {org.Id} 的组织机构");
                }
                // 保存子机构到一个字典中
                orgDict[no.Id] = no.Children.ToList();
            }

            // 直接修改实体，不需要预先加载
            var list = new List<PlOrganization>();
            if (!_EntityManager.Modify(model.Items, list))
                return NotFound();

            try
            {
                // 保存修改但明确告知EF Core不要跟踪Parent和Children属性的变化
                foreach (var org in model.Items)
                {
                    var no = _DbContext.PlOrganizations.Find(org.Id);
                    var entry = _DbContext.Entry(no);
                    entry.Property(c => c.ParentId).IsModified = false;
                    //entry.Property(c => c.MerchantId).IsModified = false; //应该允许修改
                    entry.Navigation(nameof(PlOrganization.Parent)).IsModified = false;
                    //entry.Collection(nameof(PlOrganization.Children)).IsModified = false; //实测无法按预期工作
                    no.Children.Clear();
                    no.Children.AddRange(orgDict[no.Id]); // 恢复子机构
                }

                _DbContext.SaveChanges();

                // 使商户和组织机构缓存失效
                var merchIds = list
                    .Select(c => _OrgManager.GetMerchantIdByOrgId(c.Id))
                    .Where(id => id.HasValue)
                    .Distinct()
                    .Select(id => id.Value)
                    .ToArray();

                foreach (var merchId in merchIds)
                {
                    _OrgManager.InvalidateOrgCaches(merchId);
                }
            }
            catch (Exception excp)
            {
                return BadRequest(excp.Message);
            }

            return result;
        }

        /// <summary>
        /// 增加机构父子关系的功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">找不到指定Id的对象。</response>  
        [HttpPost]
        public ActionResult<AddOrgRelationReturnDto> AddOrgRelation(AddOrgRelationParamsDto model)
        {
            var result = new AddOrgRelationReturnDto();
            var parent = _DbContext.PlOrganizations.Find(model.ParentId);
            if (parent is null) return BadRequest($"找不到{model.ParentId}指定的机构对象");
            var child = _DbContext.PlOrganizations.Find(model.ChildId);
            if (child is null) return BadRequest($"找不到{model.ChildId}指定的机构对象");
            parent.Children.Add(child);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除机构父子关系的功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">找不到指定Id的对象 -或- 不是父对象的孩子 -或- 子对象还有孩子。</response> 
        [HttpDelete]
        public ActionResult<RemoveOrgRelationReturnDto> RemoveOrgRelation([FromBody] RemoveOrgRelationParamsDto model)
        {
            var result = new RemoveOrgRelationReturnDto();
            var parent = _DbContext.PlOrganizations.Find(model.ParentId);
            if (parent is null) return BadRequest($"找不到{model.ParentId}指定的机构对象");
            var child = parent.Children.FirstOrDefault(c => c.Id == model.ChildId);
            if (child is null || child.Children.Count > 0) return BadRequest($"找不到{model.ChildId}指定的机构对象或不是父对象的孩子。 -或- 子对象还有孩子");
            parent.Children.Remove(child);
            _DbContext.Remove(child);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除一个组织机构。该机构必须没有子组织机构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveOrgReturnDto> RemoveOrg([FromBody] RemoveOrgParamsDto model)
        {
            _Logger.LogInformation("开始执行删除组织机构操作，组织ID: {orgId}", model.Id);

            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
            {
                _Logger.LogWarning("删除组织机构失败：无效令牌 {token}", model.Token);
                return Unauthorized();
            }

            _Logger.LogDebug("用户 {userId} ({loginName}) 尝试删除组织机构 {orgId}",
                context.User.Id, context.User.LoginName, model.Id);

            var result = new RemoveOrgReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.PlOrganizations;

            // 查找组织机构
            var item = dbSet.Find(id);
            if (item is null)
            {
                _Logger.LogWarning("删除组织机构失败：找不到ID为 {orgId} 的组织机构", id);
                return BadRequest($"找不到ID为 {id} 的组织机构");
            }

            // 检查是否有子组织机构（非叶子节点不能删除）
            if (item.Children != null && item.Children.Count > 0)
            {
                _Logger.LogWarning("删除组织机构失败：组织机构 {orgId} ({orgName}) 包含 {childCount} 个子组织机构，需要先删除子组织机构",
                    id, item.Name_DisplayName, item.Children.Count);
                return BadRequest($"组织机构 '{item.Name_DisplayName}' 包含子组织机构，请先删除子组织机构");
            }

            // 检查是否有角色指向该组织机构
            var rolesPointingToOrg = _DbContext.PlRoles.Where(r => r.OrgId == id).ToList();
            if (rolesPointingToOrg.Any())
            {
                _Logger.LogWarning("删除组织机构失败：存在 {count} 个角色指向组织机构 {orgId} ({orgName})",
                    rolesPointingToOrg.Count, id, item.Name_DisplayName);
                return BadRequest($"组织机构 '{item.Name_DisplayName}' 有 {rolesPointingToOrg.Count} 个角色与其关联，请先删除这些角色");
            }

            // 检查是否有关联业务
            if (HasBusinessRelatedToOrg(id))
            {
                _Logger.LogWarning("删除组织机构失败：组织机构 {orgId} ({orgName}) 存在关联业务，无法删除",
                    id, item.Name_DisplayName);
                return BadRequest($"组织机构 '{item.Name_DisplayName}' 存在关联业务，无法删除");
            }

            try
            {
                // 在删除组织机构前，先获取关联的商户ID
                Guid? merchantId = null;

                // 如果是顶层组织机构（ParentId为null），直接使用其MerchantId
                if (item.ParentId == null && item.MerchantId.HasValue)
                {
                    merchantId = item.MerchantId;
                    _Logger.LogDebug("组织机构 {orgId} 是顶层组织机构，直接关联到商户 {merchantId}", id, merchantId);
                }
                // 否则尝试通过组织机构ID找到关联的商户ID
                else
                {
                    merchantId = _OrgManager.GetMerchantIdByOrgId(model.Id);
                    if (merchantId.HasValue)
                    {
                        _Logger.LogDebug("组织机构 {orgId} 关联到商户 {merchantId}", id, merchantId);
                    }
                    else
                    {
                        _Logger.LogWarning("无法确定组织机构 {orgId} 所属的商户ID", id);
                    }
                }

                // 记录删除前的组织机构信息
                _Logger.LogInformation("准备删除组织机构: ID={orgId}, 名称='{orgName}', 父ID={parentId}, 商户ID={merchantId}",
                    item.Id, item.Name_DisplayName, item.ParentId, merchantId);

                // 1. 删除用户-机构关系
                var userOrgRelations = _DbContext.AccountPlOrganizations
                    .Where(ao => ao.OrgId == id)
                    .ToList();

                if (userOrgRelations.Any())
                {
                    _Logger.LogInformation("删除组织机构 {orgId} 的 {count} 个用户-机构关系", id, userOrgRelations.Count);
                    _DbContext.AccountPlOrganizations.RemoveRange(userOrgRelations);
                    _DbContext.SaveChanges();
                }

                // 2. 执行删除组织机构操作
                _EntityManager.Remove(item);
                _DbContext.SaveChanges();

                _Logger.LogInformation("成功删除组织机构 {orgId}", id);

                // 删除成功后，如果有关联的商户ID，则使缓存失效
                if (merchantId.HasValue)
                {
                    _Logger.LogDebug("正在使商户 {merchantId} 的缓存失效", merchantId.Value);
                    _OrgManager.InvalidateOrgCaches(merchantId.Value);
                    _Logger.LogInformation("已成功使商户 {merchantId} 的缓存失效", merchantId.Value);
                }
            }
            catch (Exception ex)
            {
                // 捕获并记录所有异常
                _Logger.LogError(ex, "删除组织机构 {orgId} 时发生异常", id);

                // 记录更详细的上下文信息
                _Logger.LogError("异常上下文: 用户={userId}, 组织机构ID={orgId}, 组织机构名称='{orgName}'",
                    context.User.Id, id, item.Name_DisplayName);

                // 返回带有详细错误信息的BadRequest结果
                return BadRequest(new
                {
                    ErrorMessage = "删除组织机构时发生错误",
                    Details = ex.Message,
                    OrgId = id
                });
            }

            return result;
        }

        /// <summary>
        /// 检查组织机构是否有关联业务
        /// </summary>
        /// <param name="orgId">组织机构ID</param>
        /// <returns>true表示有关联业务，false表示没有关联业务</returns>
        private bool HasBusinessRelatedToOrg(Guid orgId)
        {
            _Logger.LogInformation("开始检查组织机构 {orgId} 的业务关联", orgId);

            try
            {
                // 检查业务总表多种关联方式
                if (_DbContext.PlJobs != null && _DbContext.PlJobs.Any(j => j.OrgId == orgId))
                {
                    _Logger.LogInformation("组织机构 {orgId} 在业务单中存在关联，无法删除", orgId);
                    return true;
                }

                // 检查客户资料关联
                if (_DbContext.PlCustomers != null && _DbContext.PlCustomers.Any(c => c.OrgId == orgId))
                {
                    _Logger.LogInformation("组织机构 {orgId} 在客户资料中存在关联，无法删除", orgId);
                    return true;
                }

                // 检查银行信息关联
                if (_DbContext.BankInfos != null && _DbContext.BankInfos.Any(b => b.ParentId == orgId))
                {
                    _Logger.LogInformation("组织机构 {orgId} 存在关联银行信息，无法删除", orgId);
                    return true;
                }

                _Logger.LogInformation("组织机构 {orgId} 没有发现任何关联业务", orgId);
                return false;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "检查组织机构 {orgId} 业务关联时发生异常", orgId);
                // 为安全起见，发生异常时阻止删除
                return true;
            }
        }

        /// <summary>
        /// 通过组织机构Id获取所属的商户Id。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="orgId">这个必须是组织机构Id，若是商户Id则返回错误。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>
        [HttpGet]
        public ActionResult<GetMerchantIdReturnDto> GetMerchantId(Guid token, Guid orgId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetMerchantIdReturnDto();
            var merchantId = _OrgManager.GetMerchantIdByOrgId(orgId);
            if (!merchantId.HasValue)
                return BadRequest();
            result.Result = merchantId;
            return result;
        }

        #region 用户和商户/组织机构的所属关系的CRUD

        /// <summary>
        /// 获取用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountPlOrganizationReturnDto> GetAllAccountPlOrganization([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAccountPlOrganizationReturnDto();

            var dbSet = _DbContext.AccountPlOrganizations;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "accountId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.UserId == id);
                }
                else if (string.Equals(item.Key, "orgId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.OrgId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">在同一类别同一组织机构下指定了重复的Code。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddAccountPlOrganizationReturnDto> AddAccountPlOrganization(AddAccountPlOrganizationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddAccountPlOrganizationReturnDto();
            _DbContext.AccountPlOrganizations.Add(model.Item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpDelete]
        public ActionResult<RemoveAccountPlOrganizationReturnDto> RemoveAccountPlOrganization(RemoveAccountPlOrganizationParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountPlOrganizationReturnDto();
            DbSet<AccountPlOrganization> dbSet = _DbContext.AccountPlOrganizations;
            var item = dbSet.Find(model.UserId, model.OrgId);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 用户和商户/组织机构的所属关系的CRUD

        /// <summary>
        /// 为指定机构复制一份所有数据字典的副本。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的机构不存在。</response>  
        [HttpPut]
        public ActionResult<CopyDataDicReturnDto> CopyDataDic(CopyDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new CopyDataDicReturnDto();
            var merch = _DbContext.PlOrganizations.Find(model.Id);
            if (merch == null) return NotFound();
            #region 复制简单字典
            var baseCatalogs = _DbContext.DD_DataDicCatalogs.Where(c => c.OrgId == null).AsNoTracking();  //基本字典目录集合
            foreach (var catalog in baseCatalogs)
            {
                _DataManager.CopyTo(catalog, model.Id);
            }
            _DataManager.CopyAllSpecialDataDicBase(model.Id);
            #endregion 复制简单字典
            _DbContext.SaveChanges();
            return result;
        }

        #region 开户行信息

        /// <summary>
        /// 获取全部客户开户行信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 ParentId(机构id)，Id,Number。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllBankInfoReturnDto> GetAllBankInfo([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllBankInfoReturnDto();

            var dbSet = _DbContext.BankInfos;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "ParentId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.ParentId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加新客户开户行信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddBankInfoReturnDto> AddBankInfo(AddBankInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddBankInfoReturnDto();
            model.BankInfo.GenerateNewId();
            _DbContext.BankInfos.Add(model.BankInfo);
            _DbContext.SaveChanges();
            result.Id = model.BankInfo.Id;
            return result;
        }

        /// <summary>
        /// 修改客户开户行信息信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的客户开户行信息不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyBankInfoReturnDto> ModifyBankInfo(ModifyBankInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyBankInfoReturnDto();
            if (!_EntityManager.Modify(new[] { model.BankInfo })) return NotFound();
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除指定Id的客户开户行信息。慎用！
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的客户开户行信息不存在。</response>  
        [HttpDelete]
        public ActionResult<RemoveBankInfoReturnDto> RemoveBankInfo(RemoveBankInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveBankInfoReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.BankInfos;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 开户行信息

    }

}
