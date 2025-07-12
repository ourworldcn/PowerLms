/*
 * 账号相关功能控制器。
 * */
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NPOI.OpenXmlFormats.Dml.Diagram;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.SS.Formula.Functions;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using SixLabors.ImageSharp.Processing;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 账号功能控制器。
    /// </summary>
    public class AccountController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AccountController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider, IMapper mapper,
            EntityManager entityManager, OrgManager<PowerLmsUserDbContext> orgManager, CaptchaManager captchaManager, AuthorizationManager authorizationManager,
            RoleManager roleManager, OwSqlAppLogger appLogger, IMemoryCache cache, ILogger<AccountController> logger, PermissionManager permissionManager)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _OrgManager = orgManager;
            _CaptchaManager = captchaManager;
            _AuthorizationManager = authorizationManager;
            _RoleManager = roleManager;
            _AppLogger = appLogger;
            _Cache = cache;
            _Logger = logger;
            _PermissionManager = permissionManager;
        }

        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly AuthorizationManager _AuthorizationManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        readonly CaptchaManager _CaptchaManager;
        readonly RoleManager _RoleManager;
        OwSqlAppLogger _AppLogger;
        IMemoryCache _Cache;
        ILogger<AccountController> _Logger;
        PermissionManager _PermissionManager;

        #region 用户相关

        /// <summary>
        /// 获取账户信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 通用查询条件。<br/>
        /// 特别地支持 IsAdmin("true"=限定超管,"false"=限定非超管) IsMerchantAdmin（"true"=限定商户管,"false"=限定非商户管）；
        /// OrgId 指定其所属的组织机构Id(明确直属的组织机构Id)。<br/>
        /// 普通用户（非超管也非商管）最多只能看到当前登录的同一个公司及其下属机构/公司内的所有用户</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountReturnDto> GetAll([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new GetAllAccountReturnDto();

            try
            {
                // 初始查询
                var coll = _DbContext.Accounts.AsNoTracking();

                // 根据用户角色限制查询范围
                if (context.User.IsSuperAdmin)
                {
                    // 超级管理员可以查看所有用户，不做限制
                    _Logger.LogDebug("超级管理员查询所有用户");
                }
                else if (_AccountManager.IsMerchantAdmin(context.User))
                {
                    // 商户管理员可以查看该商户下的所有用户
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    var orgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToArray();

                    // 查找所有与这些组织机构关联的用户ID
                    var userIds = _DbContext.AccountPlOrganizations
                        .Where(c => orgIds.Contains(c.OrgId))
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToArray();

                    // 添加商户管理员自身（防止遗漏）
                    if (!userIds.Contains(context.User.Id))
                    {
                        userIds = userIds.Append(context.User.Id).ToArray();
                    }

                    coll = coll.Where(c => userIds.Contains(c.Id));
                    _Logger.LogDebug("商户管理员查询用户: 找到 {count} 个用户关联到商户 {merchantId}",
                        userIds.Length, merchantId);
                }
                else
                {
                    // 普通用户只能查看当前登录公司及其子机构内的用户
                    // 获取当前用户登录的公司
                    var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                    if (currentCompany == null)
                    {
                        // 如果用户未登录到任何公司，返回空结果
                        _Logger.LogDebug("普通用户未登录到任何公司，返回空结果");
                        result.Result = new List<Account>();
                        return result;
                    }

                    // 获取当前公司及所有子机构的ID
                    var companyAndChildrenOrgs = OwHelper.GetAllSubItemsOfTree(currentCompany, c => c.Children)
                        .Select(c => c.Id)
                        .ToArray();

                    // 查找这些组织机构下的所有用户ID
                    var userIds = _DbContext.AccountPlOrganizations
                        .Where(c => companyAndChildrenOrgs.Contains(c.OrgId))
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToArray();

                    // 添加当前用户自身（防止遗漏）
                    if (!userIds.Contains(context.User.Id))
                    {
                        userIds = userIds.Append(context.User.Id).ToArray();
                    }

                    coll = coll.Where(c => userIds.Contains(c.Id));
                    _Logger.LogDebug("普通用户查询: 当前公司 {companyName}(ID:{companyId}) 下找到 {count} 个用户",
                        currentCompany.Name?.DisplayName, currentCompany.Id, userIds.Length);
                }

                // 处理条件查询
                if (conditional != null && conditional.Count > 0)
                {
                    // 需要特殊处理的条件键名
                    var specialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "OrgId", "IsAdmin", "IsMerchantAdmin",
                        "Token",    //在查询账号实体时，Token不能参与过滤
                    };

                    // 提取需要特殊处理的条件
                    var specialConditions = conditional
                        .Where(kvp => specialKeys.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

                    // 提取可以用标准方式处理的条件
                    var standardConditions = conditional
                        .Where(kvp => !specialKeys.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

                    // 首先应用标准条件
                    if (standardConditions.Count > 0)
                    {
                        coll = EfHelper.GenerateWhereAnd(coll, standardConditions);
                    }

                    // 手动应用特殊条件
                    foreach (var item in specialConditions)
                    {
                        if (string.Equals(item.Key, "IsAdmin", StringComparison.OrdinalIgnoreCase))
                        {
                            if (bool.TryParse(item.Value, out var boolValue))
                            {
                                coll = coll.Where(c => boolValue ? (c.State & 4) != 0 : (c.State & 4) == 0);
                            }
                        }
                        else if (string.Equals(item.Key, "IsMerchantAdmin", StringComparison.OrdinalIgnoreCase))
                        {
                            if (bool.TryParse(item.Value, out var boolValue))
                            {
                                coll = coll.Where(c => boolValue ? (c.State & 8) != 0 : (c.State & 8) == 0);
                            }
                        }
                        else if (string.Equals(item.Key, "OrgId", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Guid.TryParse(item.Value, out var id))
                            {
                                coll = coll.Where(c => _DbContext.AccountPlOrganizations.Any(d => c.Id == d.UserId && d.OrgId == id));
                            }
                        }
                    }
                }

                // 应用排序
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc);

                // 获取分页结果
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
                _Mapper.Map(prb, result);
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取账户列表时出错: {ex.Message}";
                _Logger.LogError(ex, "获取账户列表时出错");
            }

            return result;
        }

        /// <summary>
        /// 登录。随后应调用Account/SetUserInfo。通过Account/GetAccountInfo可以获取自身信息。
        /// 调用此接口后，需创建用户成功。否则无法正常使用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">参数错误，这里特指用户名或密码不正确。</response>  
        /// <response code="409">验证码错误。</response>  
        [HttpPost]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto model)
        {
            var result = new LoginReturnDto();
#if !DEBUG
            if (!_CaptchaManager.Verify(model.CaptchaId, model.Answer, _DbContext))
            {
                return Conflict();
            }
#endif
            var pwdHash = Account.GetPwdHash(model.Pwd);
            Account user;
            switch (model.EvidenceType)
            {
                case 1:
                    user = _DbContext.Accounts.FirstOrDefault(c => (c.LoginName == model.LoginName) && c.PwdHash == pwdHash);
                    break;
                case 2:
                    user = _DbContext.Accounts.FirstOrDefault(c => (c.EMail == model.LoginName) && c.PwdHash == pwdHash);
                    break;
                case 4:
                    user = _DbContext.Accounts.FirstOrDefault(c => (c.Mobile == model.LoginName) && c.PwdHash == pwdHash);
                    break;
                default:
                    return BadRequest($"不认识的EvidenceType类型:{model.EvidenceType}");
            }
            if (user is null) return BadRequest("用户名或密码不正确。");
            if (!user.IsPwd(model.Pwd)) return BadRequest("用户名或密码不正确。");
            //用Id加载或获取用户对象
            user = _AccountManager.GetOrLoadById(user.Id);
            if (user is null)
                return BadRequest("用户数据结构损坏。");

            result.Token = Guid.NewGuid();
            _AccountManager.UpdateToken(user.Id, result.Token);
            user.CurrentLanguageTag = model.LanguageTag;
            //设置直属组织机构信息。
            var orgIds = _DbContext.AccountPlOrganizations.Where(c => c.UserId == user.Id).Select(c => c.OrgId);
            result.Orgs.AddRange(_DbContext.PlOrganizations.Where(c => orgIds.Contains(c.Id)));
            result.User = user;

            if (_OrgManager.GetMerchantIdByUserId(user.Id) is Guid merchId) //若找到商户Id
            {
                result.MerchantId = merchId;
                if (result.User.IsMerchantAdmin)
                    result.User.OrgId ??= merchId;
            }
            //_DbContext.SaveChanges();
            if (_AccountManager.GetOrLoadContextByToken(result.Token, _ServiceProvider) is OwContext context)
                _AppLogger.LogGeneralInfo("登录");
            return result;
        }

        /// <summary>
        /// 创建账户。随后应调用 获取账号信息 和 设置账号信息功能。否则该账号无法正常使用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">其它参数错误。</response>  
        /// <response code="409">登录名或手机号或邮箱重复，此时返回文本是字段名如"LoginName"，"EMail"，"Mobile"等等。</response>  
        [HttpPost]
        public ActionResult<CreateAccountReturnDto> CreateAccount(CreateAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new CreateAccountReturnDto();

            // 检查要创建的账户类型权限
            bool isCreatingAdmin = (model.Item.State & 4) != 0; // 是否要创建超管
            bool isCreatingMerchantAdmin = (model.Item.State & 8) != 0; // 是否要创建商管
            
            // 权限验证：只有超管可以创建超管
            if (isCreatingAdmin && !context.User.IsSuperAdmin)
                return BadRequest("仅超管可以创建超管账户");
            
            // 权限验证：只有超管或商管可以创建商管
            if (isCreatingMerchantAdmin && !context.User.IsAdmin())
                return BadRequest("仅超管或商管可以创建商管账户");

            // 检查登录名、邮件、手机号的全局唯一性
            if (!string.IsNullOrEmpty(model.Item.LoginName) && _DbContext.Accounts.Any(a => a.LoginName == model.Item.LoginName))
                return Conflict(nameof(model.Item.LoginName));
            if (!string.IsNullOrEmpty(model.Item.EMail) && _DbContext.Accounts.Any(a => a.EMail == model.Item.EMail))
                return Conflict(nameof(model.Item.EMail));
            if (!string.IsNullOrEmpty(model.Item.Mobile) && _DbContext.Accounts.Any(a => a.Mobile == model.Item.Mobile))
                return Conflict(nameof(model.Item.Mobile));

            // 处理组织机构ID验证和权限检查
            Guid[]? orgIds = null;
            Guid? merchantIdForNewAccount = null; // 新账户所属商户ID（用于商管账户）
            
            if (model.OrgIds != null && model.OrgIds.Count > 0)
            {
                orgIds = model.OrgIds.Distinct().ToArray();
                if (orgIds.Length != model.OrgIds.Count) return BadRequest($"{nameof(model.OrgIds)} 存在重复键值");

                // 验证所有ID是否存在（商户或组织机构）
                var merchantCount = _DbContext.Merchants.Count(c => orgIds.Contains(c.Id));
                var orgCount = _DbContext.PlOrganizations.Count(c => orgIds.Contains(c.Id));
                if (merchantCount + orgCount != orgIds.Length) 
                    return BadRequest($"{nameof(model.OrgIds)} 至少一个键值的实体不存在");

                // 非超管权限检查：只能操作自己商户范围内的组织机构
                if (!context.User.IsSuperAdmin)
                {
                    if (!context.User.IsAdmin()) return BadRequest("仅超管和商管才可创建用户");
                    
                    // 获取当前商管所属商户
                    var currentMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!currentMerchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    // 验证所有指定的组织机构ID都属于当前商户
                    bool allBelongToMerchant = orgIds.All(c => _OrgManager.GetMerchantIdByOrgId(c) == currentMerchantId);
                    if (!allBelongToMerchant) return BadRequest("商户管理员仅可以设置商户和其下属的机构id");
                    
                    merchantIdForNewAccount = currentMerchantId.Value; // 记录商户ID供后续使用
                }
            }
            else if (isCreatingMerchantAdmin && !context.User.IsSuperAdmin)
            {
                // 商管创建商管但未指定组织机构时，自动关联到当前商户
                var currentMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!currentMerchantId.HasValue) return Unauthorized("未找到用户所属商户");

                merchantIdForNewAccount = currentMerchantId.Value;
            }

            // 创建账户
            var pwd = model.Pwd;
            var created = _AccountManager.CreateNew(model.Item.LoginName, ref pwd, out Guid newUserId, _ServiceProvider, model.Item);
            if (!created) return StatusCode(OwHelper.GetLastError(), OwHelper.GetLastErrorMessage());

            result.Pwd = pwd;
            result.Result = _DbContext.Accounts.Find(newUserId);
            
            if (result.Result != null)
            {
                // 添加组织机构关联关系
                var organizationRelations = new List<AccountPlOrganization>();
                
                // 1. 添加明确指定的组织机构关联
                if (orgIds != null && orgIds.Length > 0)
                {
                    organizationRelations.AddRange(orgIds.Select(orgId => new AccountPlOrganization 
                    { 
                        UserId = newUserId, 
                        OrgId = orgId 
                    }));
                }
                
                // 2. 商管创建商管时，自动关联到调用者所属商户
                if (isCreatingMerchantAdmin && merchantIdForNewAccount.HasValue)
                {
                    // 避免重复添加相同的商户关联
                    bool alreadyLinkedToMerchant = organizationRelations.Any(r => r.OrgId == merchantIdForNewAccount.Value);
                    if (!alreadyLinkedToMerchant)
                    {
                        organizationRelations.Add(new AccountPlOrganization 
                        { 
                            UserId = newUserId, 
                            OrgId = merchantIdForNewAccount.Value 
                        });
                    }
                }
                
                // 批量添加组织机构关联关系
                if (organizationRelations.Count > 0)
                {
                    _DbContext.AccountPlOrganizations.AddRange(organizationRelations);
                }
            }

            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 获取账号信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">Token无效或无权限获取指定账号信息。</response>  
        /// <response code="404">未找到指定Id的用户。</response>  
        [HttpPost]
        public ActionResult<GetAccountInfoReturnDto> GetAccountInfo(GetAccountInfoParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAccountInfoReturnDto
            {
                Account = _DbContext.Accounts.Find(model.UserId)
            };
            if (result.Account is null)
            {
                return NotFound();
            }
            return result;
        }

        /// <summary>
        /// 设置/修改账号信息。不能用此接口修改敏感信息如密码。修改密码请使用ModifyPwd。
        /// 超管可修改超管和商管，商管可修改同商户下的商管和普通用户。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">Token无效或无权限获取指定账号信息。</response>  
        /// <response code="403">权限不足，无法修改指定账户。</response>  
        /// <response code="404">指定的账号Id不存在。</response>  
        /// <response code="451">权限不足。</response>  
        [HttpPut()]
        public ActionResult<ModifyAccountReturnDto> ModifyAccount(ModifyAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyAccountReturnDto();
            var list = new List<Account>();
            if (!_EntityManager.Modify(new[] { model.Item }, list)) return NotFound();

            var account = list[0];
            bool isTargetSuperAdmin = (account.State & 4) != 0; //目标是否为超管
            bool isTargetMerchantAdmin = (account.State & 8) != 0; //目标是否为商管
            
            // 权限检查：验证是否有权修改目标账户
            if (context.User.IsSuperAdmin) //超管权限
            {
                // 超管可以修改超管和商管，无限制
            }
            else if (context.User.IsMerchantAdmin) //商管权限
            {
                if (isTargetSuperAdmin) //商管不能修改超管
                    return StatusCode((int)HttpStatusCode.Forbidden, "商管不能修改超管账户");
                
                if (isTargetMerchantAdmin) //商管修改商管需要验证同商户
                {
                    var operatorMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id); //获取操作者商户
                    if (!operatorMerchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    var targetMerchantId = _OrgManager.GetMerchantIdByUserId(account.Id); //获取目标用户商户
                    if (!targetMerchantId.HasValue) return Unauthorized("未找到目标用户所属商户");
                    
                    if (operatorMerchantId.Value != targetMerchantId.Value) //验证是否同商户
                        return StatusCode((int)HttpStatusCode.Forbidden, "商管只能在同商户内设置商管权限");
                }
                // 商管可以修改普通用户，无需额外检查
            }
            else //普通用户权限
            {
                if (isTargetSuperAdmin || isTargetMerchantAdmin)
                    return StatusCode((int)HttpStatusCode.Forbidden, "只有管理员可以修改管理员账户");
            }

            //设置管理员权限
            if (model.IsAdmin.HasValue) //修改超管权限
            {
                if (!context.User.IsSuperAdmin) //只有超管可以设置超管权限
                    return base.StatusCode((int)HttpStatusCode.UnavailableForLegalReasons, "只有超管可以设置超管权限");
                
                if (model.IsAdmin.Value)
                    account.State |= 4; //设置为超管
                else
                    account.State &= 255 - 4; //取消超管
            }
            
            if (model.IsMerchantAdmin.HasValue) //修改商管权限
            {
                if (!context.User.IsAdmin()) //只有超管或商管可以设置商管权限
                    return base.StatusCode((int)HttpStatusCode.UnavailableForLegalReasons, "只有管理员可以设置商管权限");
                
                if (model.IsMerchantAdmin.Value)
                    account.State |= 8; //设置为商管
                else
                    account.State &= 255 - 8; //取消商管
            }
            
            var entityAccount = _DbContext.Entry(account);
            entityAccount.Property(c => c.PwdHash).IsModified = false; //密码不可修改
            entityAccount.Property(c => c.Token).IsModified = false; //令牌不可修改
            entityAccount.Property(c => c.NodeNum).IsModified = false; //节点号不可修改
            entityAccount.Property(c => c.OrgId).IsModified = false; //组织机构ID不可修改
            
            _DbContext.SaveChanges();
            
            // 缓存失效处理
            try
            {
                _AccountManager.InvalidateUserCache(account.Id); //失效用户缓存
                _RoleManager.InvalidateUserRolesCache(account.Id); //失效用户角色缓存
                _PermissionManager.InvalidateUserPermissionsCache(account.Id); //失效用户权限缓存
                
                if (_OrgManager.GetMerchantIdByOrgId(account.OrgId.Value) is Guid merchantId) //若找到商户Id
                {
                    _OrgManager.InvalidateOrgCaches(merchantId); //失效商户缓存
                }
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "修改用户 {UserId} 后缓存失效时发生警告", account.Id); //记录缓存失效警告
            }
            
            _Logger.LogInformation("用户 {OperatorId} 修改了账户 {TargetId} 的信息", context.User.Id, account.Id); //记录修改操作日志
            _AppLogger.LogGeneralInfo($"修改账户.{account.Id}"); //记录系统日志
            return result;
        }

        /// <summary>
        /// 删除账户的功能。超管可以删除超管和商管，商管只能删除同商户的商管。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">权限不足或不能删除自己。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足，商管只能删除同商户的商管。</response>  
        /// <response code="404">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        [HttpDelete]
        public ActionResult<RemoveAccountReturnDto> RemoveAccount(RemoveAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountReturnDto();
            var id = model.Id;
            var item = _DbContext.Accounts.Find(id);
            if (item is null) return NotFound(); //若没有指定id的对象

            if (id == context.User.Id) return BadRequest("不能删除自己的账户"); //不能删除自己
            
            bool isTargetSuperAdmin = (item.State & 4) != 0; //目标是否为超管
            bool isTargetMerchantAdmin = (item.State & 8) != 0; //目标是否为商管
            
            if (context.User.IsSuperAdmin) //超管权限检查
            {
                // 超管可以删除超管和商管，无限制
            }
            else if (context.User.IsMerchantAdmin) //商管权限检查
            {
                if (isTargetSuperAdmin) return BadRequest("商管不能删除超管"); //商管不能删除超管
                
                if (isTargetMerchantAdmin) //商管删除商管需要验证同商户
                {
                    var operatorMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id); //获取操作者商户
                    if (!operatorMerchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    var targetMerchantId = _OrgManager.GetMerchantIdByUserId(item.Id); //获取目标用户商户
                    if (!targetMerchantId.HasValue) return Unauthorized("未找到目标用户所属商户");
                    
                    if (operatorMerchantId.Value != targetMerchantId.Value) //验证是否同商户
                        return StatusCode((int)HttpStatusCode.Forbidden, "商管只能删除同商户的商管");
                }
            }
            else //普通用户权限检查
            {
                if (isTargetSuperAdmin || isTargetMerchantAdmin) 
                    return BadRequest("只有超管或商管可以删除管理员账户");
            }
            
            // 删除前记录用户相关信息（用于缓存失效）
            var userMerchantId = _OrgManager.GetMerchantIdByUserId(id); //获取用户所属商户
            
            _DbContext.Accounts.Remove(item); //删除账户
            _DbContext.AccountPlOrganizations.RemoveRange(_DbContext.AccountPlOrganizations.Where(c => c.UserId == id)); //删除组织机构关联
            _DbContext.PlAccountRoles.RemoveRange(_DbContext.PlAccountRoles.Where(c => c.UserId == id)); //删除角色关联
            _DbContext.SaveChanges();

            // 缓存失效处理
            try
            {
                // 使用AccountManager的缓存失效方法（避免循环引用）
                _AccountManager.InvalidateUserCache(id); //失效用户缓存
                
                // 失效角色相关缓存
                _RoleManager.InvalidateUserRolesCache(id); //失效用户角色缓存
                
                // 失效权限相关缓存  
                _PermissionManager.InvalidateUserPermissionsCache(id); //失效用户权限缓存
                
                // 如果用户属于某个商户，失效商户相关缓存
                if (userMerchantId.HasValue)
                {
                    _OrgManager.InvalidateOrgCaches(userMerchantId.Value); //失效商户缓存
                }
                
                // 失效当前用户的组织机构缓存
                _Cache.Remove(OwMemoryCacheExtensions.GetCacheKeyFromId(id, ".CurrentOrgs")); //失效当前组织机构缓存
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "删除用户 {UserId} 后缓存失效时发生警告", id); //记录缓存失效警告，不影响删除操作
            }

            _Logger.LogInformation("用户 {OperatorId} 删除了账户 {TargetId} ({LoginName})", 
                context.User.Id, id, item.LoginName); //记录删除操作日志
            _AppLogger.LogGeneralInfo($"删除账户.{id}"); //记录系统日志

            return result;
        }

        /// <summary>
        /// 登陆后设置用的一些必要信息，如当前组织机构等信息，这个接口可能会逐步增加参数中属性。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="permissionManager"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">指定的组织机构Id错误，可能不是公司。</response>  
        [HttpPut]
        public ActionResult<SetUserInfoReturnDto> SetUserInfo(SetUserInfoParams model, [FromServices] PermissionManager permissionManager)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetUserInfoReturnDto();

            if (context.User.OrgId != model.CurrentOrgId)
            {
                var merchantId = _OrgManager.GetMerchantIdByOrgId(model.CurrentOrgId);
                if (!merchantId.HasValue) return BadRequest("错误的当前组织机构Id。");
                var orgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs;
                if (!orgs.TryGetValue(model.CurrentOrgId, out var currentOrg)) return BadRequest("错误的当前组织机构Id。");
                if (currentOrg.Otc != 2)
                    return BadRequest("错误的当前组织机构Id——不是公司。");
                context.User.OrgId = model.CurrentOrgId;

                // 取消当前用户的组织机构缓存
                _Cache.Remove(OwMemoryCacheExtensions.GetCacheKeyFromId(context.User.Id, ".CurrentOrgs"));
            }

            context.User.CurrentLanguageTag = model.LanguageTag;
            context.Nop();
            context.SaveChanges();

            // 获取用户权限并添加到结果中
            var userPermissions = permissionManager.GetOrLoadUserCurrentPermissions(context.User);
            result.Permissions.AddRange(userPermissions.Values);
            return result;
        }

        /// <summary>
        /// 延续指定令牌的自动注销时间。会自动换一个新令牌并返回。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<NopReturnDto> Nop(NopParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new NopReturnDto();
            context.User.Token = Guid.NewGuid();
            context.Nop();
            context.SaveChanges();
            result.NewToken = context.User.Token.Value;
            return result;
        }

        /// <summary>
        /// 修改用户自己的密码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">旧密码不正确。</response>  
        [HttpPut]
        public ActionResult<ModifyPwdReturnDto> ModifyPwd([FromBody] ModifyPwdParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPwdReturnDto();
            if (!context.User.IsPwd(model.OldPwd)) return BadRequest();
            context.User.SetPwd(model.NewPwd);
            context.User.State &= 0b_1111_1101;
            lock (context.User.DbContext)
                context.User.DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 重置密码。只有超管或商管可以使用此功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">超管试图重置一般账号的密码。或指定登录名的账号不存在。</response>  
        /// <response code="403">权限不足，只有超管和商管可以使用此功能，且不能越级重置。</response>  
        [HttpPost]
        public ActionResult<ResetPwdReturnDto> ResetPwd(ResetPwdParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "只有超管或商管可以使用此功能");
            if (_DbContext.Accounts.FirstOrDefault(c => c.Id == model.Id) is not Account tmpUser)
                return BadRequest("指定账号不存在。");

            // 修改: 使用 GetOrLoadAccountById 替代 GetOrLoadCacheItemById
            var targetUser = _AccountManager.GetOrLoadById(tmpUser.Id);
            if (targetUser == null) return BadRequest("指定账号不存在。");

            if (context.User.IsSuperAdmin && !targetUser.IsMerchantAdmin) return BadRequest("超管不能重置普通用户的密码。");
            else if (context.User.IsMerchantAdmin && targetUser.IsAdmin()) return BadRequest("商管只能重置普通用户的密码。");

            var result = new ResetPwdReturnDto { };
            //生成密码
            Span<char> span = stackalloc char[8];
            for (int i = span.Length - 1; i >= 0; i--)
            {
                span[i] = (char)OwHelper.Random.Next('0', 'z' + 1);
            }
            result.Pwd = new string(span);

            targetUser.SetPwd(result.Pwd);
            lock (targetUser.DbContext)
                targetUser.DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 设置用户的直属机构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">参数错误。</response>  
        [HttpPut]
        public ActionResult<SetOrgsReturnDto> SetOrgs(SetOrgsParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetOrgsReturnDto();
            var ids = new HashSet<Guid>(model.OrgIds);
            if (ids.Count != model.OrgIds.Count) return BadRequest($"{nameof(model.OrgIds)}中有重复键值。");

            // 区分商户ID和组织机构ID
            var merchantIds = _DbContext.Merchants.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToArray();
            var orgIds = _DbContext.PlOrganizations.Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToArray();

            // 确保所有ID都是有效的商户或组织机构ID
            if (merchantIds.Length + orgIds.Length != ids.Count)
                return BadRequest($"{nameof(model.OrgIds)}中至少有一个ID既不是有效的组织机构ID也不是有效的商户ID。");

            // 获取用户关联的商户ID
            var userMerchantId = _OrgManager.GetMerchantIdByUserId(model.UserId);

            // 同时处理组织机构ID和商户ID
            var allValidIds = new HashSet<Guid>(orgIds.Concat(merchantIds));
            var cacheKey = userMerchantId.HasValue ? OwMemoryCacheExtensions.GetCacheKeyFromId(userMerchantId.Value, ".Orgs") : null;

            // 删除不在当前列表中的关联
            var removes = _DbContext.AccountPlOrganizations.Where(c => c.UserId == model.UserId && !allValidIds.Contains(c.OrgId));
            _DbContext.AccountPlOrganizations.RemoveRange(removes);

            // 添加新的关联
            var existingOrgIds = _DbContext.AccountPlOrganizations
                .Where(c => c.UserId == model.UserId)
                .Select(c => c.OrgId)
                .AsEnumerable();

            var adds = allValidIds.Except(existingOrgIds).ToArray();
            _DbContext.AccountPlOrganizations.AddRange(adds.Select(c => new AccountPlOrganization { OrgId = c, UserId = model.UserId }));

            _DbContext.SaveChanges();

            // 取消相关缓存
            if (cacheKey != null)
                _Cache.CancelSource(cacheKey);

            // 如果有修改过用户与商户的关联，也应该清除用户相关的缓存
            if (merchantIds.Length > 0)
            {
                _Cache.CancelSource(OwMemoryCacheExtensions.GetCacheKeyFromId(model.UserId, ".CurrentOrgs"));
            }

            return result;
        }

        #endregion 用户相关

        /// <summary>
        /// 设置用户的直属角色。仅超管或商管可以使用此功能。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">参数错误，如角色ID不存在或用户ID无效。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足，如尝试修改其他商户的用户角色。</response>  
        /// <response code="500">服务器内部错误。</response>  
        [HttpPut]
        public ActionResult<SetRolesReturnDto> SetRoles(SetRolesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new SetRolesReturnDto();

            if (!context.User.IsAdmin())
            {
                // 只有超管或商管可以设置用户角色
                return StatusCode((int)HttpStatusCode.Forbidden, "只有超管或商管可以设置用户角色");
            }
            try
            {
                // 验证角色ID参数
                var ids = new HashSet<Guid>(model.RoleIds);
                if (ids.Count != model.RoleIds.Count)
                    return BadRequest($"{nameof(model.RoleIds)}中有重复键值。");

                // 验证角色是否存在
                var existingRoleIds = _DbContext.PlRoles
                    .Where(c => ids.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToHashSet();

                if (existingRoleIds.Count != ids.Count)
                    return BadRequest($"{nameof(model.RoleIds)}中至少有一个组织角色不存在。");

                // 验证用户是否存在
                var account = _AccountManager.GetOrLoadById(model.UserId);
                if (account == null)
                    return BadRequest($"{nameof(model.UserId)}指定用户不存在。");

                // 超管权限验证
                if (!context.User.IsSuperAdmin)
                {
                    // 获取当前用户所属商户
                    var operatorMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!operatorMerchantId.HasValue) return Unauthorized("未找到用户所属商户");

                    // 检查目标用户所属商户
                    var targetUserMerchantId = _OrgManager.GetMerchantIdByUserId(model.UserId);
                    if (!targetUserMerchantId.HasValue) return Unauthorized("未找到目标用户所属商户");

                    // 非同一商户用户无权操作
                    if (operatorMerchantId.Value != targetUserMerchantId.Value)
                        return StatusCode((int)HttpStatusCode.Forbidden, "无权修改其他商户用户的角色");

                    // 验证角色的所属商户
                    var rolesWithOrg = _DbContext.PlRoles
                        .Where(r => ids.Contains(r.Id) && r.OrgId.HasValue)
                        .Select(r => new { r.Id, r.OrgId })
                        .ToList();

                    foreach (var role in rolesWithOrg)
                    {
                        var merchantId = _OrgManager.GetMerchantIdByOrgId(role.OrgId.Value);
                        if (!merchantId.HasValue || merchantId.Value != operatorMerchantId.Value)
                        {
                            _Logger.LogWarning("尝试设置其他商户的角色 {RoleId}", role.Id);
                            return StatusCode((int)HttpStatusCode.Forbidden, "无权设置其他商户的角色");
                        }
                    }
                }

                // 执行操作部分
                // 获取指定用户的当前所有角色
                var currentUserRoles = _DbContext.PlAccountRoles
                    .Where(c => c.UserId == model.UserId)
                    .ToList();

                // 获取当前角色ID集合
                var currentRoleIdSet = currentUserRoles
                    .Select(r => r.RoleId)
                    .AsEnumerable().ToHashSet();

                // 输入参数中的角色ID集合
                var requestedRoleIdSet = new HashSet<Guid>(model.RoleIds);

                // 计算需要添加的角色ID集合（在请求中存在但当前不存在的角色）
                var roleIdsToAdd = requestedRoleIdSet
                    .Where(id => !currentRoleIdSet.Contains(id))
                    .ToArray();

                // 计算需要删除的角色（在当前存在但请求中不存在的角色）
                var rolesToDelete = currentUserRoles
                    .Where(r => !requestedRoleIdSet.Contains(r.RoleId))
                    .ToArray();

                // 记录要执行的操作
                _Logger.LogInformation("用户 {UserId} 角色变更: 删除 {RemoveCount}, 添加 {AddCount}",
                    model.UserId, rolesToDelete.Length, roleIdsToAdd.Length);

                // 如果没有需要变更的角色，直接返回
                if (roleIdsToAdd.Length == 0 && rolesToDelete.Length == 0)
                {
                    _Logger.LogInformation("用户 {UserId} 的角色没有变化，不需要更新", model.UserId);
                    return result;
                }

                // 执行删除操作
                if (rolesToDelete.Length > 0)
                {
                    _DbContext.PlAccountRoles.RemoveRange(rolesToDelete);
                }

                // 执行添加操作
                if (roleIdsToAdd.Length > 0)
                {
                    var rolesToAdd = roleIdsToAdd.Select(roleId => new AccountRole
                    {
                        UserId = model.UserId,
                        RoleId = roleId,
                        CreateBy = context.User.Id,
                        CreateDateTime = OwHelper.WorldNow
                    });

                    _DbContext.PlAccountRoles.AddRange(rolesToAdd);
                }

                // 保存所有更改（一次性提交所有操作）
                _DbContext.SaveChanges();

                // 清除相关缓存，确保数据一致性
                _RoleManager.InvalidateUserRolesCache(model.UserId);
                _PermissionManager.InvalidateUserPermissionsCache(model.UserId);
                _AccountManager.InvalidateUserCache(model.UserId);
            }
            catch (DbUpdateException ex)
            {
                _Logger.LogError(ex, "数据库更新异常: {Message}", ex.InnerException?.Message ?? ex.Message);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"数据库更新失败: {ex.InnerException?.Message ?? ex.Message}";
                return StatusCode(500, result);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "为用户 {UserId} 设置角色时发生错误", model.UserId);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"设置角色失败: {ex.Message}";
                return StatusCode(500, result);
            }

            return result;
        }
    }

}
