using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 生成序号控制器。
    /// </summary>
    public class JobNumberController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public JobNumberController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, JobNumberManager jobNumber)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _JobNumber = jobNumber;
        }

        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;
        PowerLmsUserDbContext _DbContext;
        JobNumberManager _JobNumber;

        /// <summary>
        /// 用指定的编码规则生成一个新的编码。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<GeneratedJobNumberReturnDto> GeneratedJobNumber(GeneratedJobNumberParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GeneratedJobNumberReturnDto();
            if (_DbContext.DD_JobNumberRules.Find(model.RuleId) is not JobNumberRule jnr) return BadRequest($"指定的规则不存在，Id={model.RuleId}");
            using var dw = DisposeHelper.Create((key, timeout) => SingletonLocker.TryEnter(key, timeout), key => SingletonLocker.Exit(key), model.RuleId.ToString(), TimeSpan.FromSeconds(2)); //锁定该规则
            result.Result = _JobNumber.Generated(jnr, context?.User, OwHelper.WorldNow);
            context.Nop();
            _DbContext.SaveChanges();
            return result;
        }
    }

    /// <summary>
    /// 用指定的编码规则生成一个新的编码的功能返回值封装类。
    /// </summary>
    public class GeneratedJobNumberReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 返回的业务码。
        /// </summary>
        public string Result { get; set; }
    }

    /// <summary>
    /// 用指定的编码规则生成一个新的编码的功能参数封装类。
    /// </summary>
    public class GeneratedJobNumberParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 规则的Id.
        /// </summary>
        public Guid RuleId { get; set; }
    }
}
