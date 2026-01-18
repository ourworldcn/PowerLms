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
        private readonly IServiceProvider _ServiceProvider;
        private readonly PowerLmsUserDbContext _DbContext;
        private readonly AccountManager _AccountManager;
        private readonly AuthorizationManager _AuthorizationManager;
        private readonly IMapper _Mapper;
        private readonly EntityManager _EntityManager;
        private readonly OrgManager<PowerLmsUserDbContext> _OrgManager;
        private readonly CaptchaManager _CaptchaManager;
        private readonly RoleManager _RoleManager;
        private readonly OwSqlAppLogger _AppLogger;
        private readonly IMemoryCache _Cache;
        private readonly ILogger<AccountController> _Logger;
        private readonly PermissionManager _PermissionManager;
        #region 用户相关
        /// <summary>
        /// 获取账户信息。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持 通用查询条件。<br/>
        /// 特别地支持 IsAdmin("true"=限定超管,"false"=限定非超管) IsMerchantAdmin（"true"=限定商户管,"false"=限定非商户管）;
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
                var coll = _DbContext.Accounts.AsNoTracking(); // 初始查询
                if (context.User.IsSuperAdmin) // 根据用户角色限制查询范围
                {
                    // 超级管理员可以查看所有用户
                }
                else if (_AccountManager.IsMerchantAdmin(context.User))
                {
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue) return Unauthorized("未找到用户所属商户");
                    var orgIds = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Keys.ToList();
                    orgIds.Add(merchantId.Value); // 包含商户ID,使商管账户能被查询到
                    var userIds = _DbContext.AccountPlOrganizations // 查找所有与这些组织机构或商户关联的用户ID
                        .Where(c => orgIds.Contains(c.OrgId))
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToArray();
                    if (!userIds.Contains(context.User.Id)) // 添加商户管理员自身（防止遗漏）
                    {
                        userIds = userIds.Append(context.User.Id).ToArray();
                    }
                    coll = coll.Where(c => userIds.Contains(c.Id));
                }
                else
                {
                    var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User); // 获取当前用户登录的公司
                    if (currentCompany == null)
                    {
                        result.Result = new List<Account>(); // 如果用户未登录到任何公司，返回空结果
                        return result;
                    }
                    var companyAndChildrenOrgs = OwHelper.GetAllSubItemsOfTree(currentCompany, c => c.Children) // 获取当前公司及所有子机构的ID
                        .Select(c => c.Id)
                        .ToArray();
                    var userIds = _DbContext.AccountPlOrganizations // 查找这些组织机构下的所有用户ID
                        .Where(c => companyAndChildrenOrgs.Contains(c.OrgId))
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToArray();
                    if (!userIds.Contains(context.User.Id)) // 添加当前用户自身（防止遗漏）
                    {
                        userIds = userIds.Append(context.User.Id).ToArray();
                    }
                    coll = coll.Where(c => userIds.Contains(c.Id));
                }
                if (conditional != null && conditional.Count > 0) // 处理条件查询
                {
                    var specialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) // 需要特殊处理的条件键名
                    {
                        "OrgId", "IsAdmin", "IsMerchantAdmin", "Token",
                    };
                    var specialConditions = conditional // 提取需要特殊处理的条件
                        .Where(kvp => specialKeys.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
                    var standardConditions = conditional // 提取可以用标准方式处理的条件
                        .Where(kvp => !specialKeys.Contains(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
                    if (standardConditions.Count > 0) // 首先应用标准条件
                    {
                        coll = EfHelper.GenerateWhereAnd(coll, standardConditions);
                    }
                    foreach (var item in specialConditions) // 手动应用特殊条件
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
                coll = coll.OrderBy(model.OrderFieldName, model.IsDesc); // 应用排序
                var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count); // 获取分页结果
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
        /// 调用此接口后,需创建用户成功。否则无法正常使用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">参数错误,这里特指用户名或密码不正确。</response>  
        /// <response code="409">验证码错误。</response>  
        [HttpPost]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto model)
        {
            var result = new LoginReturnDto();
#if !DEBUG
            if (!_CaptchaManager.Verify(model.CaptchaId, model.Answer, _DbContext))
            {
                return Conflict("验证码错误或已过期");
            }
#endif
            Account user;
#if DEBUG
            switch (model.EvidenceType)
            {
                case 1:
                    user = _DbContext.Accounts.FirstOrDefault(c => c.LoginName == model.LoginName);
                    break;
                case 2:
                    user = _DbContext.Accounts.FirstOrDefault(c => c.EMail == model.LoginName);
                    break;
                case 4:
                    user = _DbContext.Accounts.FirstOrDefault(c => c.Mobile == model.LoginName);
                    break;
                default:
                    return BadRequest($"不认识的EvidenceType类型:{model.EvidenceType}");
            }
            if (user is null) return BadRequest($"用户名不存在: {model.LoginName}");
#else
            var pwdHash = Account.GetPwdHash(model.Pwd); // 生产环境: 完整的用户名和密码验证
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
#endif
            user = _AccountManager.GetOrLoadById(user.Id); // 用Id加载或获取用户对象(只读缓存)
            if (user is null)
                return BadRequest("用户数据结构损害。");
            result.Token = Guid.NewGuid();
            _AccountManager.UpdateToken(user.Id, result.Token);
            var orgIds = _DbContext.AccountPlOrganizations.Where(c => c.UserId == user.Id).Select(c => c.OrgId); // 设置直属组织机构信息
            result.Orgs.AddRange(_DbContext.PlOrganizations.Where(c => orgIds.Contains(c.Id)));
            result.User = user;
            if (_OrgManager.GetMerchantIdByUserId(user.Id) is Guid merchId) // 若找到商户Id
            {
                result.MerchantId = merchId;
            }
            if (_AccountManager.GetOrLoadContextByToken(result.Token, _ServiceProvider) is OwContext context)
                _AppLogger.LogGeneralInfo("登录");
            _Logger.LogInformation("用户 {LoginName} ({UserId}) 成功登录", user.LoginName, user.Id);
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
                _Logger.LogInformation("商管 {OperatorId} 创建商管账户 {LoginName} 时未指定机构，自动归属到商户 {MerchantId}",
              context.User.Id, model.Item.LoginName, currentMerchantId.Value);
            }
            else if (!context.User.IsSuperAdmin && (model.OrgIds == null || model.OrgIds.Count == 0))
            {
                // 🔧 Bug修复：商管创建普通用户但未指定组织机构时，自动关联到当前商户
                // 这是修复"用户消失"问题的关键逻辑
                if (!context.User.IsAdmin()) return BadRequest("仅超管和商管才可创建用户");
                var currentMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!currentMerchantId.HasValue) return Unauthorized("未找到用户所属商户");
                // 自动归属到当前商户
                merchantIdForNewAccount = currentMerchantId.Value;
                _Logger.LogInformation("商管 {OperatorId} 创建普通用户 {LoginName} 时未指定机构，自动归属到商户 {MerchantId}",
              context.User.Id, model.Item.LoginName, currentMerchantId.Value);
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
                // ✅ 修复：检查 OrgId 是否为 null
                if (account.OrgId.HasValue)
                {
                    if (_OrgManager.GetMerchantIdByOrgId(account.OrgId.Value) is Guid merchantId) //若找到商户Id
                    {
                        _OrgManager.InvalidateOrgCaches(merchantId); //失效商户缓存
                    }
                }
                else
                {
                    // ⚠️ 记录警告：非超管账户的 OrgId 不应该为 null
                    bool isAdmin = (account.State & 4) != 0; //是否为超管
                    if (!isAdmin)
                    {
                        _Logger.LogWarning("用户 {UserId} ({LoginName}) 的 OrgId 为 null，这可能导致权限和缓存失效问题",
                            account.Id, account.LoginName);
                    }
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
        /// <response code="403">权限不足，商管只能删除同商戶的商管。</response>  
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
                _Cache.Remove(OwCacheExtensions.GetCacheKeyFromId(id, ".CurrentOrgs")); //失效当前组织机构缓存
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new SetUserInfoReturnDto();
            // ✅ 步骤1: 验证和准备修改
            bool needSave = false;
            if (context.User.OrgId != model.CurrentOrgId)
            {
                var merchantId = _OrgManager.GetMerchantIdByOrgId(model.CurrentOrgId);
                if (!merchantId.HasValue)
                    return BadRequest("错误的当前组织机构Id。");
                var orgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs;
                if (!orgs.TryGetValue(model.CurrentOrgId, out var currentOrg))
                    return BadRequest("错误的当前组织机构Id。");
                if (currentOrg.Otc != 2)
                    return BadRequest("错误的当前组织机构Id——不是公司。");
                needSave = true;
            }
            if (context.User.CurrentLanguageTag != model.LanguageTag)
            {
                needSave = true;
            }
            // ✅ 步骤2: 如果需要保存,在范围DbContext中加载并修改
            if (needSave)
            {
                var user = _DbContext.Accounts.Find(context.User.Id);
                if (user == null)
                    return NotFound("用户不存在");
                var oldOrgId = user.OrgId;
                user.OrgId = model.CurrentOrgId;
                user.CurrentLanguageTag = model.LanguageTag;
                user.LastModifyDateTimeUtc = OwHelper.WorldNow;
                _DbContext.SaveChanges();
                // ✅ 步骤3: 失效缓存
                _AccountManager.InvalidateUserCache(context.User.Id);
                _Cache.Remove(OwCacheExtensions.GetCacheKeyFromId(context.User.Id, ".CurrentOrgs"));
                // 🔥 关键修复: 切换OrgId时,失效新旧两个商户的组织机构缓存
                // 确保下次获取机构详情时能加载到最新数据
                if (oldOrgId.HasValue)
                {
                    var oldMerchantId = _OrgManager.GetMerchantIdByOrgId(oldOrgId.Value);
                    if (oldMerchantId.HasValue)
                    {
                        _OrgManager.InvalidateOrgCaches(oldMerchantId.Value);
                        _Logger.LogInformation("用户 {UserId} 从机构 {OldOrgId} 切换,已失效旧商户 {MerchantId} 的缓存",
                            context.User.Id, oldOrgId.Value, oldMerchantId.Value);
                    }
                }
                var newMerchantId = _OrgManager.GetMerchantIdByOrgId(model.CurrentOrgId);
                if (newMerchantId.HasValue)
                {
                    _OrgManager.InvalidateOrgCaches(newMerchantId.Value);
                    _Logger.LogInformation("用户 {UserId} 切换到机构 {NewOrgId},已失效新商户 {MerchantId} 的缓存",
                        context.User.Id, model.CurrentOrgId, newMerchantId.Value);
                }
            }
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new NopReturnDto();
            // ✅ 使用AccountManager.UpdateToken方法更新令牌(内部已处理数据库保存和缓存失效)
            var newToken = _AccountManager.UpdateToken(context.User.Id, Guid.NewGuid());
            if (!newToken.HasValue)
                return BadRequest("更新令牌失败");
            result.NewToken = newToken.Value;
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            var result = new ModifyPwdReturnDto();
            // ✅ 步骤1: 验证旧密码(使用缓存的只读用户对象)
            if (!context.User.IsPwd(model.OldPwd))
                return BadRequest();
            // ✅ 步骤2: 在范围DbContext中加载用户并修改密码
            var user = _DbContext.Accounts.Find(context.User.Id);
            if (user == null)
                return NotFound("用户不存在");
            user.SetPwd(model.NewPwd);
            user.State &= 0b_1111_1101;
            // ✅ 步骤3: 保存
            _DbContext.SaveChanges();
            // ✅ 步骤4: 失效缓存
            _AccountManager.InvalidateUserCache(context.User.Id);
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
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();
            if (!context.User.IsAdmin())
                return StatusCode((int)HttpStatusCode.Forbidden, "只有超管或商管可以使用此功能");
            // ✅ 步骤1: 从缓存获取目标用户信息(只读,用于权限检查)
            var targetUser = _AccountManager.GetOrLoadById(model.Id);
            if (targetUser == null)
                return BadRequest("指定账号不存在。");
            // ✅ 步骤2: 权限检查
            if (context.User.IsSuperAdmin && !targetUser.IsMerchantAdmin)
                return BadRequest("超管不能重置普通用户的密码.");
            else if (context.User.IsMerchantAdmin && targetUser.IsAdmin())
                return BadRequest("商管只能重置普通用户的密码.");
            var result = new ResetPwdReturnDto { };
            // ✅ 步骤3: 生成密码
            Span<char> span = stackalloc char[8];
            for (int i = span.Length - 1; i >= 0; i--)
            {
                span[i] = (char)OwHelper.Random.Next('0', 'z' + 1);
            }
            result.Pwd = new string(span);
            // ✅ 步骤4: 在范围DbContext中加载并修改密码
            var userInDb = _DbContext.Accounts.Find(model.Id);
            if (userInDb == null)
                return BadRequest("指定账号不存在。");
            userInDb.SetPwd(result.Pwd);
            // ✅ 步骤5: 保存
            _DbContext.SaveChanges();
            // ✅ 步骤6: 失效缓存
            _AccountManager.InvalidateUserCache(model.Id);
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
            var cacheKey = userMerchantId.HasValue ? OwCacheExtensions.GetCacheKeyFromId(userMerchantId.Value, ".Orgs") : null;
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
            // ✅ 级联失效所有相关缓存
            // 1. 失效商户的组织缓存
            if (cacheKey != null)
            {
                var cts = _Cache.GetCancellationTokenSource(cacheKey);
                if (cts != null && !cts.IsCancellationRequested)
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch { /* 忽略可能的异常 */ }
                }
            }
            // 2. 失效用户的当前组织缓存
            if (merchantIds.Length > 0)
            {
                var currentOrgsCacheKey = OwCacheExtensions.GetCacheKeyFromId(model.UserId, ".CurrentOrgs");
                var currentOrgsCts = _Cache.GetCancellationTokenSource(currentOrgsCacheKey);
                if (currentOrgsCts != null && !currentOrgsCts.IsCancellationRequested)
                {
                    try
                    {
                        currentOrgsCts.Cancel();
                    }
                    catch { /* 忽略可能的异常 */ }
                }
            }
            // ✅ 3. 关键修复：失效用户的角色和权限缓存
            // 用户所属机构变化会影响角色范围和权限范围
            try
            {
                _RoleManager.InvalidateUserRolesCache(model.UserId);             // ✅ 失效角色缓存
                _PermissionManager.InvalidateUserPermissionsCache(model.UserId); // ✅ 失效权限缓存
                _AccountManager.InvalidateUserCache(model.UserId);               // 失效用户缓存
                _Logger.LogInformation("用户 {UserId} 的机构关系已更新，已失效角色和权限缓存", model.UserId);
            }
            catch (Exception ex)
            {
                _Logger.LogWarning(ex, "失效用户 {UserId} 的缓存时发生警告", model.UserId);
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
                return StatusCode((int)HttpStatusCode.Forbidden, "只有超管或商管可以设置用户角色");
            }
            try
            {
                var ids = new HashSet<Guid>(model.RoleIds); // 验证角色ID参数
                if (ids.Count != model.RoleIds.Count)
                    return BadRequest($"{nameof(model.RoleIds)}中有重复键值。");
                // ✅ 修复：允许传入空角色列表（用于清空用户的所有角色）
                if (ids.Count > 0)
                {
                    var existingRoleIds = _DbContext.PlRoles // 验证角色是否存在
                        .Where(c => ids.Contains(c.Id))
                        .Select(c => c.Id)
                        .ToHashSet();
                    if (existingRoleIds.Count != ids.Count)
                        return BadRequest($"{nameof(model.RoleIds)}中至少有一个组织角色不存在。");
                }
                var account = _AccountManager.GetOrLoadById(model.UserId); // 验证用户是否存在
                if (account == null)
                    return BadRequest($"{nameof(model.UserId)}指定用户不存在。");
                if (!context.User.IsSuperAdmin) // 超管权限验证
                {
                    var operatorMerchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id); // 获取当前用户所属商户
                    if (!operatorMerchantId.HasValue) return Unauthorized("未找到用户所属商户");
                    var targetUserMerchantId = _OrgManager.GetMerchantIdByUserId(model.UserId); // 检查目标用户所属商户
                    if (!targetUserMerchantId.HasValue) return Unauthorized("未找到目标用户所属商户");
                    if (operatorMerchantId.Value != targetUserMerchantId.Value) // 非同一商户用户无权操作
                        return StatusCode((int)HttpStatusCode.Forbidden, "无权修改其他商户用户的角色");
                    // ✅ 修复：只有当角色列表不为空时才验证角色所属商户
                    if (ids.Count > 0)
                    {
                        var rolesWithOrg = _DbContext.PlRoles // 验证角色的所属商户
                            .Where(r => ids.Contains(r.Id) && r.OrgId.HasValue)
                            .Select(r => new { r.Id, r.OrgId })
                            .ToList();
                        foreach (var role in rolesWithOrg)
                        {
                            var merchantId = _OrgManager.GetMerchantIdByOrgId(role.OrgId.Value);
                            if (!merchantId.HasValue || merchantId.Value != operatorMerchantId.Value)
                            {
                                return StatusCode((int)HttpStatusCode.Forbidden, "无权设置其他商户的角色");
                            }
                        }
                    }
                }
                var currentUserRoles = _DbContext.PlAccountRoles // 获取指定用户的当前所有角色
                    .Where(c => c.UserId == model.UserId)
                    .ToList();
                var currentRoleIdSet = currentUserRoles // 获取当前角色ID集合
                    .Select(r => r.RoleId)
                    .ToHashSet();
                var requestedRoleIdSet = new HashSet<Guid>(model.RoleIds); // 输入参数中的角色ID集合
                var roleIdsToAdd = requestedRoleIdSet // 计算需要添加的角色ID集合（在请求中存在但当前不存在的角色）
                    .Where(id => !currentRoleIdSet.Contains(id))
                    .ToArray();
                var rolesToDelete = currentUserRoles // 计算需要删除的角色（在当前存在但请求中不存在的角色）
                    .Where(r => !requestedRoleIdSet.Contains(r.RoleId))
                    .ToArray();
                if (roleIdsToAdd.Length == 0 && rolesToDelete.Length == 0) // 如果没有需要变更的角色，直接返回
                {
                    _Logger.LogInformation("用户 {UserId} 角色无需变更", model.UserId);
                    return result;
                }
                if (rolesToDelete.Length > 0) // 执行删除操作
                {
                    _DbContext.PlAccountRoles.RemoveRange(rolesToDelete);
                    _Logger.LogInformation("准备删除用户 {UserId} 的 {Count} 个角色关系", model.UserId, rolesToDelete.Length);
                }
                if (roleIdsToAdd.Length > 0) // 执行添加操作
                {
                    var rolesToAdd = roleIdsToAdd.Select(roleId => new AccountRole
                    {
                        UserId = model.UserId,
                        RoleId = roleId,
                        CreateBy = context.User.Id,
                        CreateDateTime = OwHelper.WorldNow
                    }).ToList(); // ✅ 修复：添加ToList()确保立即执行
                    _DbContext.PlAccountRoles.AddRange(rolesToAdd);
                    _Logger.LogInformation("准备添加用户 {UserId} 的 {Count} 个角色关系: {RoleIds}",
                        model.UserId, rolesToAdd.Count, string.Join(", ", roleIdsToAdd));
                }
                _DbContext.SaveChanges(); // 保存所有更改（一次性提交所有操作）
                _Logger.LogInformation("成功保存用户 {UserId} 的角色变更到数据库", model.UserId);
                _RoleManager.InvalidateUserRolesCache(model.UserId); // 清除相关缓存，确保数据一致性
                _PermissionManager.InvalidateUserPermissionsCache(model.UserId);
                _AccountManager.InvalidateUserCache(model.UserId);
                _Logger.LogInformation("用户 {UserId} 角色已更新: 添加 {AddCount} 个, 删除 {RemoveCount} 个",
                    model.UserId, roleIdsToAdd.Length, rolesToDelete.Length);
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
