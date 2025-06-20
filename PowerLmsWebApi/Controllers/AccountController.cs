﻿/*
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
        /// <param name="dbContext"></param>
        /// <param name="accountManager"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="mapper"></param>
        /// <param name="entityManager"></param>
        /// <param name="organizationManager"></param>
        /// <param name="captchaManager"></param>
        /// <param name="authorizationManager"></param>
        /// <param name="merchantManager"></param>
        /// <param name="roleManager"></param>
        /// <param name="appLogger"></param>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        public AccountController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider, IMapper mapper,
            EntityManager entityManager, OrganizationManager organizationManager, CaptchaManager captchaManager, AuthorizationManager authorizationManager,
            MerchantManager merchantManager, RoleManager roleManager, OwSqlAppLogger appLogger, IMemoryCache cache, ILogger<AccountController> logger, PermissionManager permissionManager)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _OrganizationManager = organizationManager;
            _CaptchaManager = captchaManager;
            _AuthorizationManager = authorizationManager;
            _MerchantManager = merchantManager;
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
        readonly OrganizationManager _OrganizationManager;
        readonly CaptchaManager _CaptchaManager;
        readonly MerchantManager _MerchantManager;
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
                else if (_AccountManager.IsMerchantAdmin(context.User) && _MerchantManager.GetIdByUserId(context.User.Id, out var merchantId) && merchantId.HasValue)
                {
                    // 商户管理员可以查看该商户下的所有用户
                    var orgs = _OrganizationManager.GetOrLoadByMerchantId(merchantId.Value);
                    var orgIds = orgs.Keys.ToArray();

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
                        userIds.Length, merchantId.Value);
                }
                else
                {
                    // 普通用户只能查看当前登录公司及其子机构内的用户
                    // 获取当前用户登录的公司
                    var currentCompany = _OrganizationManager.GetCurrentCompanyByUser(context.User);
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

            if (_MerchantManager.GetIdByUserId(user.Id, out var merchId)) //若找到商户Id
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

            // 检查登录名、邮件、手机号的全局唯一性
            if (!string.IsNullOrEmpty(model.Item.LoginName) && _DbContext.Accounts.Any(a => a.LoginName == model.Item.LoginName))
            {
                return Conflict(nameof(model.Item.LoginName));
            }
            if (!string.IsNullOrEmpty(model.Item.EMail) && _DbContext.Accounts.Any(a => a.EMail == model.Item.EMail))
            {
                return Conflict(nameof(model.Item.EMail));
            }
            if (!string.IsNullOrEmpty(model.Item.Mobile) && _DbContext.Accounts.Any(a => a.Mobile == model.Item.Mobile))
            {
                return Conflict(nameof(model.Item.Mobile));
            }

            //检验机构/商户Id合规性
            Guid[] orgIds = null;
            if (model.OrgIds != null)
            {
                orgIds = model.OrgIds.Distinct().ToArray();
                if (orgIds.Length != model.OrgIds.Count) return BadRequest($"{nameof(model.OrgIds)} 存在重复键值。");
                if (orgIds.Length > 0)
                {
                    var merches = _DbContext.Merchants.Where(c => orgIds.Contains(c.Id)).ToArray();
                    var orgs = _DbContext.PlOrganizations.Where(c => orgIds.Contains(c.Id)).ToArray();
                    if (merches.Length + orgs.Length != orgIds.Length) return BadRequest($"{nameof(model.OrgIds)} 至少一个键值的实体不存在。");
                    if ((context.User.State & 4) == 0)  //若非超管
                        if ((context.User.State & 8) == 0)  //若非商管
                            return BadRequest("仅超管和商管才可创建用户。");
                        else //商管
                        {
                            if (!_MerchantManager.GetIdByUserId(context.User.Id, out var merchId)) return BadRequest("商管数据结构损坏——无法找到其所属商户");
                            if (!orgIds.All(c => _MerchantManager.TryGetIdByOrgOrMerchantId(c, out var mId) && mId == merchId)) return BadRequest("商户管理员仅可以设置商户和其下属的机构id。");
                        }
                }
            }
            var pwd = model.Pwd;
            var b = _AccountManager.CreateNew(model.Item.LoginName, ref pwd, out Guid id, _ServiceProvider, model.Item);
            if (b)
            {
                result.Pwd = pwd;
                result.Result = _DbContext.Accounts.Find(id);

                var b1 = _DbContext.PlOrganizations.Select(c => c.Id).Concat(_DbContext.Merchants.Select(c => c.Id)).All(c => model.OrgIds.Contains(c));
                var rela = orgIds?.Select(c => new AccountPlOrganization { UserId = result.Result.Id, OrgId = c });
                if (rela != null)
                    _DbContext.AccountPlOrganizations.AddRange(rela);
            }
            else
            {
                return BadRequest("登录名重复。");
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
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">Token无效或无权限获取指定账号信息。</response>  
        /// <response code="404">指定的账号Id不存在。</response>  
        /// <response code="451">权限不足。</response>  
        [HttpPut()]
        public ActionResult<ModifyAccountReturnDto> ModifyAccount(ModifyAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyAccountReturnDto();
            var list = new List<Account>();
            if (!_EntityManager.Modify(new[] { model.Item }, list)) return NotFound();

            //设置管理员
            var account = list[0];
            if (model.IsAdmin.HasValue)
            {
                if ((context.User.State & (255 - 4)) == 0)
                    return base.StatusCode((int)HttpStatusCode.UnavailableForLegalReasons);
                if (model.IsAdmin.Value)
                    account.State |= 4;
                else
                    account.State &= 255 - 4;
            }
            if (model.IsMerchantAdmin.HasValue)
            {
                if ((context.User.State & (255 - 4)) == 0 && (context.User.State & (255 - 8)) == 0)
                    return base.StatusCode((int)HttpStatusCode.UnavailableForLegalReasons);
                if (model.IsMerchantAdmin.Value)
                    account.State |= 8;
                else
                    account.State &= 255 - 8;
            }
            var entityAccount = _DbContext.Entry(account);
            entityAccount.Property(c => c.PwdHash).IsModified = false;
            entityAccount.Property(c => c.Token).IsModified = false;
            entityAccount.Property(c => c.NodeNum).IsModified = false;
            entityAccount.Property(c => c.OrgId).IsModified = false;
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除账户的功能。不能删除超管（需要先降低权限）。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">不能删除超管，需要先降低权限。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        [HttpDelete]
        public ActionResult<RemoveAccountReturnDto> RemoveAccount(RemoveAccountParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountReturnDto();
            var id = model.Id;
            var item = _DbContext.Accounts.Find(id);
            if (item is null) return NotFound();  //若没有指定id的对象。

            if ((item.State & 4) != 0) return BadRequest(); //不能删除超管
            _DbContext.Accounts.Remove(item);
            _DbContext.AccountPlOrganizations.RemoveRange(_DbContext.AccountPlOrganizations.Where(c => c.UserId == id));
            _DbContext.PlAccountRoles.RemoveRange(_DbContext.PlAccountRoles.Where(c => c.UserId == id));
            _DbContext.SaveChanges();

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
                if (!_MerchantManager.TryGetIdByOrgOrMerchantId(model.CurrentOrgId, out var merchantId)) return BadRequest("错误的当前组织机构Id。");
                var orgs = _OrganizationManager.GetOrLoadByMerchantId(merchantId.Value);
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
        public ActionResult<ModifyPwdReturnDto> ModifyPwd([FromBody]ModifyPwdParamsDto model)
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
            _MerchantManager.GetIdByUserId(model.UserId, out var userMerchantId);

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
        /// 设置用户的直属角色。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="400">参数错误。</response>  
        [HttpPut]
        public ActionResult<SetRolesReturnDto> SetRoles(SetRolesParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context)
                return Unauthorized();

            var result = new SetRolesReturnDto();

            // 验证角色ID参数
            var ids = new HashSet<Guid>(model.RoleIds);
            if (ids.Count != model.RoleIds.Count)
                return BadRequest($"{nameof(model.RoleIds)}中有重复键值。");

            // 验证角色是否存在
            var count = _DbContext.PlRoles.Count(c => ids.Contains(c.Id));
            if (count != ids.Count)
                return BadRequest($"{nameof(model.RoleIds)}中至少有一个组织角色不存在。");

            // 验证用户是否存在
            var account = _AccountManager.GetOrLoadById(model.UserId);
            if (account == null)
                return BadRequest($"{nameof(model.UserId)}指定用户不存在。");

            try
            {
                // 首先查询当前用户的所有角色关联
                var currentRoles = _DbContext.PlAccountRoles
                    .Where(c => c.UserId == model.UserId)
                    .ToList();

                // 计算需要删除的角色关联
                var currentRoleIds = currentRoles.Select(r => r.RoleId).ToHashSet();
                var rolesToRemove = currentRoles.Where(r => !ids.Contains(r.RoleId)).ToList();

                // 计算需要添加的角色关联
                var rolesToAdd = ids
                    .Where(id => !currentRoleIds.Contains(id))
                    .Select(roleId => new AccountRole { RoleId = roleId, UserId = model.UserId })
                    .ToList();

                // 删除旧角色关联
                if (rolesToRemove.Any())
                {
                    _DbContext.PlAccountRoles.RemoveRange(rolesToRemove);
                }

                // 添加新角色关联
                if (rolesToAdd.Any())
                {
                    _DbContext.PlAccountRoles.AddRange(rolesToAdd);
                }

                // 保存更改
                _DbContext.SaveChanges();

                // 使用RoleManager来使用户角色缓存失效
                _RoleManager.InvalidateUserRolesCache(model.UserId);

                // 由于角色变更会影响权限，也使用户权限缓存失效
                _PermissionManager.InvalidateUserPermissionsCache(model.UserId);

                _Logger.LogInformation($"已为用户 {model.UserId} 设置 {model.RoleIds.Count} 个角色，移除 {rolesToRemove.Count} 个角色，添加 {rolesToAdd.Count} 个角色");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, $"为用户 {model.UserId} 设置角色时发生错误");
                return BadRequest($"设置角色失败: {ex.Message}");
            }

            return result;
        }

    }

}
