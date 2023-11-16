/*
 * 账号相关功能控制器。
 * */
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPOI.OpenXmlFormats.Dml.Diagram;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 账号功能控制器。
    /// </summary>
    public class AccountController : OwControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="accountManager"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="mapper"></param>
        public AccountController(PowerLmsUserDbContext dbContext, AccountManager accountManager, IServiceProvider serviceProvider, IMapper mapper)
        {
            _DbContext = dbContext;
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _Mapper = mapper;
        }

        IServiceProvider _ServiceProvider { get; }
        PowerLmsUserDbContext _DbContext;
        AccountManager _AccountManager;
        IMapper _Mapper;

        /// <summary>
        /// 登录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">参数错误，这里特指用户名或密码不正确。</response>  
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto model)
        {
            //TODO:增加 手机号，邮箱支持
            var result = new LoginReturnDto();
            var user = _DbContext.Accounts.FirstOrDefault(c => c.LoginName == model.LoginName);
            if (user is null) return BadRequest();
            if (!user.IsPwd(model.Pwd)) return BadRequest();
            result.Token = Guid.NewGuid();
            user.LastModifyDateTimeUtc = OwHelper.WorldNow;
            user.Token = result.Token;
            //设置直属组织机构信息。
            var orgIds = _DbContext.AccountPlOrganizations.Where(c => c.UserId == user.Id).Select(c => c.OrgId);
            result.Orgs.AddRange(_DbContext.PlOrganizations.Where(c => orgIds.Contains(c.Id)));
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 创建账户。随后应调用 获取账号信息 和 设置账号信息功能。否则该账号无法正常使用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="400">登录名重复。</response>  
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), (int)HttpStatusCode.BadRequest)]
        [HttpPost]
        public ActionResult<CreateAccountReturnDto> CreateAccount(CreateAccountParamsDto model)
        {
            var result = new CreateAccountReturnDto();
            var pwd = model.Pwd;
            var b = _AccountManager.CreateNew(model.LoginName, ref pwd, out Guid id, _ServiceProvider);
            if (b)
            {
                result.Pwd = pwd;
                result.Id = id;
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
            var result = new GetAccountInfoReturnDto();
            result.Account = _DbContext.Accounts.Find(model.UserId);
            if (result.Account is null)
            {
                return NotFound();
            }
            return result;
        }

        /// <summary>
        /// 设置/修改账号信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        /// <response code="401">Token无效或无权限获取指定账号信息。</response>  
        /// <response code="404">指定的账号Id不存在。</response>  
        [HttpPost]
        public ActionResult<SetAccountInfoReturnDto> SetAccountInfo(SetAccountInfoParamsDto model)
        {
            var result = new SetAccountInfoReturnDto();
            var acount = _DbContext.Accounts.Find(model.Account.Id);
            if (acount is null) return NotFound();
            _DbContext.Entry(model.Account).CurrentValues.SetValues(model.Account);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 登陆后设置用的一些必要信息，如当前组织机构等信息，这个接口可能会逐步增加参数中属性。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。</response>  
        [HttpPost]
        public ActionResult<SetUserInfoReturnDto> SetUserInfo(SetUserInfoParams model)
        {
            var result = new SetUserInfoReturnDto();
            var context = _AccountManager.GetAccountFromToken(model.Token, _ServiceProvider);
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
        [HttpPost]
        public ActionResult<NopReturnDto> Nop(NopParamsDto model)
        {
            var result=new NopReturnDto();
            return result;
        }

        [HttpPost]
        public ActionResult ModifyPwd()
        {
            return Ok();
        }

        [HttpPost]
        public ActionResult ResetPwd()
        {
            return Ok();
        }
    }

    public class NopParamsDto : TokenDtoBase
    {
    }

    public class NopReturnDto : ReturnDtoBase
    {
        public Guid NewToken { get; set; }
    }
}
