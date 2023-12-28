/*
 * 账号相关功能控制器。
 * */
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Dml.Diagram;
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
        public AccountController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider, IMapper mapper, EntityManager entityManager,
            OrganizationManager organizationManager)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Mapper = mapper;
            _EntityManager = entityManager;
            _OrganizationManager = organizationManager;
        }

        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;
        OrganizationManager _OrganizationManager;

        #region 用户相关

        /// <summary>
        /// 获取账户信息。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 LoginName Mobile eMail DisplayName，
        /// IsAdmin("true"=限定超管,"false"=限定非超管) IsMerchantAdmin（"true"=限定商户管,"false"=限定非商户管）；
        /// OrgId 指定其所属的组织机构Id(需要时明确直属的组织机构Id)。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountReturnDto> GetAll(Guid token, [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex,
            [FromQuery][Range(-1, int.MaxValue)] int count = -1, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAccountReturnDto();
            var coll = _DbContext.Accounts.AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, "eMail", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.EMail.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, "LoginName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.LoginName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "Mobile", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.Mobile.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "IsAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    if (bool.TryParse(item.Value, out var boolValue))
                        if (boolValue)
                            coll = coll.Where(c => (c.State & 4) != 0);
                        else
                            coll = coll.Where(c => (c.State & 4) == 0);
                }
                else if (string.Equals(item.Key, "IsMerchantAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    if (bool.TryParse(item.Value, out var boolValue))
                        if (boolValue)
                            coll = coll.Where(c => (c.State & 8) != 0);
                        else
                            coll = coll.Where(c => (c.State & 8) == 0);
                }
                else if (string.Equals(item.Key, "OrgId", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                    {
                        coll = coll.Where(c => _DbContext.AccountPlOrganizations.Any(d => c.Id == d.UserId && d.OrgId == id));
                    }
                }
            var prb = _EntityManager.GetAll(coll, startIndex, count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 登录。随后应调用Account/SetUserInfo。通过Account/GetAccountInfo可以获取自身信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">参数错误，这里特指用户名或密码不正确。</response>  
        [HttpPost]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto model)
        {
            var result = new LoginReturnDto();
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
            if (user is null) return BadRequest();
            if (!user.IsPwd(model.Pwd)) return BadRequest();
            result.Token = Guid.NewGuid();
            user.LastModifyDateTimeUtc = OwHelper.WorldNow;
            user.Token = result.Token;
            user.CurrentLanguageTag = model.LanguageTag;
            _DbContext.SaveChanges();
            //设置直属组织机构信息。
            var orgIds = _DbContext.AccountPlOrganizations.Where(c => c.UserId == user.Id).Select(c => c.OrgId);
            result.Orgs.AddRange(_DbContext.PlOrganizations.Where(c => orgIds.Contains(c.Id)));
            result.User = user;
            if (_OrganizationManager.GetMerchantId(user.Id, out var merchId)) //若找到商户Id
                result.MerchantId = merchId;
            return result;
        }

        /// <summary>
        /// 创建账户。随后应调用 获取账号信息 和 设置账号信息功能。否则该账号无法正常使用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">登录名重复。或其它参数错误。</response>  
        [HttpPost]
        public ActionResult<CreateAccountReturnDto> CreateAccount(CreateAccountParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new CreateAccountReturnDto();
            //检验机构/商户Id合规性
            var orgIds = model.OrgIds.Distinct().ToArray();
            if (orgIds.Length != model.OrgIds.Count) return BadRequest($"{nameof(model.OrgIds)} 存在重复键值。");
            if (orgIds.Length > 0)
            {
                var merches = _DbContext.Merchants.Where(c => orgIds.Contains(c.Id)).ToArray();
                var orgs = _DbContext.PlOrganizations.Where(c => orgIds.Contains(c.Id)).ToArray();
                if (merches.Length + orgs.Length != orgIds.Length) return BadRequest($"{nameof(model.OrgIds)} 至少一个键值的实体不存在。");
                if ((context.User.State & 4) == 0)  //若非超管
                    if ((context.User.State & 8) == 0)  //若非商管管
                        return BadRequest("仅超管和商管才可创建用户。");
                    else //商管
                    {
                        if (!_OrganizationManager.GetMerchantId(context.User.Id, out var merchId)) return BadRequest("商管数据结构损坏——无法找到其所属商户");
                        if (!orgIds.All(c => _OrganizationManager.GetMerchantIdFromOrgId(c, out var mId) && mId == merchId)) return BadRequest("商户管理员尽可以设置商户和其下属的机构id。");
                    }
            }

            var pwd = model.Pwd;
            var b = _AccountManager.CreateNew(model.Item.LoginName, ref pwd, out Guid id, _ServiceProvider);
            if (b)
            {
                result.Pwd = pwd;
                result.Result = _DbContext.Accounts.Find(id);
                var b1 = _DbContext.PlOrganizations.Select(c => c.Id).Concat(_DbContext.Merchants.Select(c => c.Id)).All(c => model.OrgIds.Contains(c));
                var rela = orgIds.Select(c => new AccountPlOrganization { UserId = result.Result.Id, OrgId = c });
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        [HttpPut]
        public ActionResult<ModifyAccountReturnDto> ModifyAccount(ModifyAccountParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            _DbContext.Entry(account).Property(c => c.PwdHash).IsModified = false;
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        [HttpPut]
        public ActionResult<SetUserInfoReturnDto> SetUserInfo(SetUserInfoParams model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetUserInfoReturnDto();
            if (!_DbContext.AccountPlOrganizations.Any(c => c.UserId == context.User.Id && c.OrgId == model.CurrentOrgId)) return BadRequest("错误的当前组织机构Id。");
            context.User.OrgId = model.CurrentOrgId;
            context.User.CurrentLanguageTag = model.LanguageTag;
            context.Nop();
            context.SaveChanges();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
        public ActionResult<ModifyPwdReturnDto> ModifyPwd(ModifyPwdParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyPwdReturnDto();
            if (!context.User.IsPwd(model.OldPwd)) return BadRequest();
            context.User.SetPwd(model.NewPwd);
            context.User.State &= 0b_1111_1101;
            context.SaveChanges();
            return result;
        }

        /// <summary>
        /// 重置密码。暂未实现。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">暂未实现。</response>  
        [HttpPost]
        public ActionResult<ResetPwdReturnDto> ResetPwd(ResetPwdParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            return base.NotFound();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetOrgsReturnDto();
            var ids = new HashSet<Guid>(model.OrgIds);
            if (ids.Count != model.OrgIds.Count) return BadRequest($"{nameof(model.OrgIds)}中有重复键值。");

            var count = _DbContext.PlOrganizations.Count(c => ids.Contains(c.Id));
            if (count != ids.Count) return BadRequest($"{nameof(model.OrgIds)}中至少有一个组织机构不存在。");

            var removes = _DbContext.AccountPlOrganizations.Where(c => c.UserId == model.UserId && !ids.Contains(c.OrgId));
            _DbContext.AccountPlOrganizations.RemoveRange(removes);

            var adds = ids.Except(_DbContext.AccountPlOrganizations.Where(c => c.UserId == model.UserId).Select(c => c.OrgId).AsEnumerable()).ToArray();
            _DbContext.AccountPlOrganizations.AddRange(adds.Select(c => new AccountPlOrganization { OrgId = c, UserId = model.UserId }));
            _DbContext.SaveChanges();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new SetRolesReturnDto();
            var ids = new HashSet<Guid>(model.RoleIds);
            if (ids.Count != model.RoleIds.Count) return BadRequest($"{nameof(model.RoleIds)}中有重复键值。");

            var count = _DbContext.PlRoles.Count(c => ids.Contains(c.Id));
            if (count != ids.Count) return BadRequest($"{nameof(model.RoleIds)}中至少有一个组织角色不存在。");

            var removes = _DbContext.PlAccountRoles.Where(c => c.UserId == model.UserId && !ids.Contains(c.RoleId));
            _DbContext.PlAccountRoles.RemoveRange(removes);

            var adds = ids.Except(_DbContext.PlAccountRoles.Where(c => c.UserId == model.UserId).Select(c => c.RoleId).AsEnumerable()).ToArray();
            _DbContext.PlAccountRoles.AddRange(adds.Select(c => new AccountRole { RoleId = c, UserId = model.UserId }));
            _DbContext.SaveChanges();
            return result;
        }

    }

    /// <summary>
    /// 设置用户的直属角色功能的参数封装类。
    /// </summary>
    public class SetRolesParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 账号的Id。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 直属组织角色Id的集合。未在此集合指定的与角色的关系均被删除。
        /// </summary>
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 设置用户的直属角色功能的返回值封装类。
    /// </summary>
    public class SetRolesReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 设置用户的直属机构功能的参数封装类。
    /// </summary>
    public class SetOrgsParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 账号的Id。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 直属组织机构Id的集合。未在此集合指定的与机构的关系均被删除。
        /// </summary>
        public List<Guid> OrgIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 设置用户的直属机构功能的返回值封装类。
    /// </summary>
    public class SetOrgsReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 删除账户的功能参数封装类。
    /// </summary>
    public class RemoveAccountParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 删除账户的功能返回值封装类。
    /// </summary>
    public class RemoveAccountReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 重置密码功能的参数封装类。
    /// </summary>
    public class ResetPwdParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 重置密码功能的返回值封装类。
    /// </summary>
    public class ResetPwdReturnDto
    {
    }

    /// <summary>
    /// 修改用户自己的密码功能的参数封装类。
    /// </summary>
    public class ModifyPwdParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 原有密码。
        /// </summary>
        public string OldPwd { get; set; }

        /// <summary>
        /// 新密码。
        /// </summary>
        public string NewPwd { get; set; }
    }

    /// <summary>
    /// 修改用户自己的密码功能的返回值封装类。
    /// </summary>
    public class ModifyPwdReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 延迟令牌失效功能的参数封装类。
    /// </summary>
    public class NopParamsDto : TokenDtoBase
    {
    }

    /// <summary>
    /// 延迟令牌失效功能的返回值封装类。
    /// </summary>
    public class NopReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 新令牌。
        /// </summary>
        public Guid NewToken { get; set; }
    }
}
