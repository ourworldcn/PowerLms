/*
 * 账号相关功能控制器。
 * */
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PowerLmsServer.EfData;
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
        public AccountController(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 登录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="400">参数错误，这里特指用户名或密码不正确。</response>  
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<LoginReturnDto> Login(LoginParamsDto model)
        {
            var result = new LoginReturnDto();
            var user = _DbContext.Accounts.FirstOrDefault(c => c.LoginName == model.LoginName);
            if (user is null) return BadRequest();
            if (!user.IsPwd(model.Pwd)) return BadRequest();
            result.Token = Guid.NewGuid();
            return result;
        }
    }

}
