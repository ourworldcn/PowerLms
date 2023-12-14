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
        public AccountController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider, IMapper mapper, EntityManager entityManager)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Mapper = mapper;
            _EntityManager = entityManager;
        }

        readonly IServiceProvider _ServiceProvider;
        readonly PowerLmsUserDbContext _DbContext;
        readonly AccountManager _AccountManager;
        readonly IMapper _Mapper;
        readonly EntityManager _EntityManager;

        #region 用户相关

        /// <summary>
        /// 获取账户信息。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <param name="conditional">查询的条件。支持 LoginName Mobile eMail DisplayName</param>
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
            return result;
        }

        /// <summary>
        /// 创建账户。随后应调用 获取账号信息 和 设置账号信息功能。否则该账号无法正常使用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">登录名重复。</response>  
        [HttpPost]
        public ActionResult<CreateAccountReturnDto> CreateAccount(CreateAccountParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new CreateAccountReturnDto();
            var pwd = model.Pwd;
            var b = _AccountManager.CreateNew(model.Item.LoginName, ref pwd, out Guid id, _ServiceProvider);
            if (b)
            {
                result.Pwd = pwd;
                result.Result = _DbContext.Accounts.Find(id);
            }
            else
            {
                return BadRequest("登录名重复。");
            }
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
        /// 设置/修改账号信息。不能用此接口修改敏感信息如密码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">Token无效或无权限获取指定账号信息。</response>  
        /// <response code="404">指定的账号Id不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyAccountReturnDto> ModifyAccount(ModifyAccountParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyAccountReturnDto();
            var acount = _DbContext.Accounts.Find(model.Item.Id);
            if (acount is null) return NotFound();
            _DbContext.Entry(model.Item).CurrentValues.SetValues(model.Item);
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
        /// 重置密码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ResetPwdReturnDto> ResetPwd(ResetPwdParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            return Ok();
        }

        #endregion 用户相关

        #region 用户和商户/组织机构的所属关系的CRUD

        /// <summary>
        /// 获取用户和商户/组织机构的所属关系。
        /// </summary>
        /// <param name="token">登录令牌。</param>
        /// <param name="accountId">账号Id。null表示不限定。和<paramref name="orgId"/>限定为与的关系。</param>
        /// <param name="orgId">商户及组织机构类别的Id,null表示不限定。和<paramref name="accountId"/>限定为与的关系。</param>
        /// <param name="startIndex">起始位置，从0开始。</param>
        /// <param name="count">最大返回数量。-1表示全返回。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllAccountPlOrganizationReturnDto> GetAllAccountPlOrganization(Guid token, Guid? accountId, Guid? orgId,
            [Range(0, int.MaxValue, ErrorMessage = "必须大于或等于0.")] int startIndex, [FromQuery][Range(-1, int.MaxValue)] int count = -1)
        {
            if (_AccountManager.GetAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllAccountPlOrganizationReturnDto();
            var coll = _DbContext.AccountPlOrganizations.AsNoTracking();
            if (accountId is not null)
                coll = coll.Where(c => c.UserId == accountId);
            if (orgId is not null)
                coll = coll.Where(c => c.OrgId == orgId);
            var prb = _EntityManager.GetAll(coll, startIndex, count);
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
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
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveAccountPlOrganizationReturnDto();
            DbSet<AccountPlOrganization> dbSet = _DbContext.AccountPlOrganizations;
            var item = dbSet.Find(model.UserId, model.OrgId);
            if (item is null) return BadRequest();
            _DbContext.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 用户和商户/组织机构的所属关系的CRUD

    }

    /// <summary>
    /// 获取用户和商户/组织机构的所属关系返回值封装类。
    /// </summary>
    public class GetAllAccountPlOrganizationReturnDto : PagingReturnDtoBase<AccountPlOrganization>
    {
    }

    /// <summary>
    /// 删除用户和商户/组织机构的所属关系的功能参数封装类。
    /// </summary>
    public class RemoveAccountPlOrganizationParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 用户的Id。
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 商户/组织机构的Id。
        /// </summary>
        public Guid OrgId { get; set; }
    }

    /// <summary>
    /// 删除用户和商户/组织机构的所属关系的功能返回值封装类。
    /// </summary>
    public class RemoveAccountPlOrganizationReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 增加用户和商户/组织机构的所属关系的功能参数封装类，
    /// </summary>
    public class AddAccountPlOrganizationParamsDto : AddParamsDtoBase<AccountPlOrganization>
    {
    }

    /// <summary>
    /// 增加用户和商户/组织机构的所属关系的功能返回值封装类。
    /// </summary>
    public class AddAccountPlOrganizationReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 获取用户和商户/组织机构的所属关系功能的返回值封装类。
    /// </summary>
    public class GetAllAccountReturnDto : PagingReturnDtoBase<Account>
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
